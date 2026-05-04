using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.UI;
using EstanDentro.Interaction;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// Overlay unificado: ITEMS + NOTAS + MISIONES en una sola UI con tabs.
    /// Se abre con I / Tab / Touchpad o desde el circulo del ObjectiveHUD.
    /// Animacion: scale desde la esquina top-right (donde esta el circulo).
    /// </summary>
    public class InventoryOverlay : MonoBehaviour
    {
        private static InventoryOverlay instance;

        public enum Tab { Items, Notas, Misiones }

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.88f);
        [SerializeField] private Color titleColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color amberSoft = new Color(0.85f, 0.7f, 0.28f, 0.75f);
        [SerializeField] private Color cardBgColor = new Color(0.10f, 0.09f, 0.08f, 0.95f);
        [SerializeField] private Color iconBgColor = new Color(0.15f, 0.13f, 0.10f, 1f);
        [SerializeField] private Color labelColor = new Color(0.95f, 0.93f, 0.85f, 0.95f);
        [SerializeField] private Color emptyTextColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color tabActiveColor = new Color(0.85f, 0.7f, 0.28f, 1f);
        [SerializeField] private Color tabInactiveColor = new Color(0.5f, 0.46f, 0.4f, 0.85f);
        [SerializeField] private Color missionPrincipalColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color missionSecundariaColor = new Color(0.55f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color missionCompletedColor = new Color(0.5f, 0.46f, 0.4f, 0.7f);

        [Header("Layout")]
        [SerializeField] private float cardWidth = 230f;
        [SerializeField] private float cardHeight = 280f;
        [SerializeField] private float cardSpacing = 28f;
        [SerializeField] private int columns = 4;

        [Header("Animacion")]
        [SerializeField] private float openDuration = 0.35f;
        [SerializeField] private float closeDuration = 0.22f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform contentRT;
        private RectTransform tabsContainer;
        private RectTransform gridContainer;
        private Text emptyText;
        private Tab currentTab = Tab.Items;

        // Tab buttons
        private struct TabBtn { public Tab tab; public Image circleBg; public Text iconText; public Text labelText; public RectTransform underline; }
        private System.Collections.Generic.List<TabBtn> tabButtons = new System.Collections.Generic.List<TabBtn>();

        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;
        private System.Action onCloseCallback;
        private Coroutine animRoutine;
        private Sprite roundedSpriteCache;
        private Sprite circleSpriteCache;
        private int blockerCountWhenOpened; // si se abre algo encima (NoteOverlay), count sube y no procesamos dismiss

        // ---------- API ----------

        public static void Open(System.Action onClose = null) => OpenTab(Tab.Items, onClose);

        public static void OpenTab(Tab tab, System.Action onClose = null)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;
            instance.currentTab = tab;
            instance.RebuildContent();
            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.prevCursorLock = Cursor.lockState;
            instance.prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            instance.canvas.gameObject.SetActive(true);
            instance.consumeInputThisFrame = true;
            instance.onCloseCallback = onClose;
            OverlayBlocker.Register();
            instance.blockerCountWhenOpened = OverlayBlocker.Count; // capturamos para detectar overlays encima
            // Animacion de apertura
            if (instance.animRoutine != null) instance.StopCoroutine(instance.animRoutine);
            instance.animRoutine = instance.StartCoroutine(instance.OpenAnimation());
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.gameObject.activeSelf) return;
            if (instance.animRoutine != null) instance.StopCoroutine(instance.animRoutine);
            instance.animRoutine = instance.StartCoroutine(instance.CloseAnimation());
        }

        // ---------- lifecycle ----------

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("__InventoryOverlay");
            instance = go.AddComponent<InventoryOverlay>();
            instance.Build();
            instance.canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this && canvas != null && canvas.gameObject.activeSelf)
                OverlayBlocker.Unregister();
        }

        private void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf) return;
            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }

            // Si hay otro overlay ENCIMA (NoteOverlay, ItemPreviewOverlay, etc), no procesamos input.
            // Asi cuando el jugador presiona Esc en una nota abierta desde el inventario, solo se
            // cierra la nota y el inventario queda visible.
            if (OverlayBlocker.Count > blockerCountWhenOpened) return;
            // Si algo se acaba de cerrar ESTE frame (la misma Esc cerro la nota), tampoco procesamos.
            // Sin esto, la misma Esc cerraria la nota Y el inventario en cadena.
            if (OverlayBlocker.WasJustDismissed) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            // Cambio de tab: SOLO Q/E (teclado) o L1/R1 (mando).
            // Las flechas / D-pad se reservan para navegar dentro de las cards del tab actual.
            bool prevTab = (kb != null && kb.qKey.wasPressedThisFrame)
                        || (gp != null && gp.leftShoulder.wasPressedThisFrame);
            bool nextTab = (kb != null && kb.eKey.wasPressedThisFrame)
                        || (gp != null && gp.rightShoulder.wasPressedThisFrame);

            if (prevTab) CycleTab(-1);
            else if (nextTab) CycleTab(+1);

            // Cerrar inventario
            bool dismiss = (kb != null && (kb.iKey.wasPressedThisFrame
                                          || kb.tabKey.wasPressedThisFrame
                                          || kb.escapeKey.wasPressedThisFrame))
                        || (gp != null && (gp.buttonEast.wasPressedThisFrame
                                          || gp.selectButton.wasPressedThisFrame));
            if (dismiss) Close();
        }

        private void CycleTab(int delta)
        {
            int count = System.Enum.GetValues(typeof(Tab)).Length;
            int newIdx = ((int)currentTab + delta + count) % count;
            SwitchTab((Tab)newIdx);
        }

        // ---------- animaciones ----------

        private System.Collections.IEnumerator OpenAnimation()
        {
            // Empieza chico desde top-right
            canvasGroup.alpha = 0f;
            contentRT.localScale = new Vector3(0.4f, 0.4f, 1f);
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / openDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f); // ease out cubic
                canvasGroup.alpha = eased;
                contentRT.localScale = Vector3.Lerp(new Vector3(0.4f, 0.4f, 1f), Vector3.one, eased);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            contentRT.localScale = Vector3.one;
            animRoutine = null;
        }

        private System.Collections.IEnumerator CloseAnimation()
        {
            float t = 0f;
            Vector3 startScale = contentRT.localScale;
            while (t < closeDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / closeDuration);
                float eased = p * p * p;
                canvasGroup.alpha = 1f - eased;
                contentRT.localScale = Vector3.Lerp(startScale, new Vector3(0.4f, 0.4f, 1f), eased);
                yield return null;
            }
            canvas.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
            Cursor.lockState = prevCursorLock;
            Cursor.visible = prevCursorVisible;
            OverlayBlocker.Unregister();
            var cb = onCloseCallback;
            onCloseCallback = null;
            cb?.Invoke();
            animRoutine = null;
        }

        // ---------- tabs + content rebuild ----------

        private void SwitchTab(Tab tab)
        {
            currentTab = tab;
            UpdateTabVisuals();
            RebuildContent();
        }

        private void UpdateTabVisuals()
        {
            foreach (var t in tabButtons)
            {
                bool active = t.tab == currentTab;
                if (t.iconText != null) t.iconText.color = active ? tabActiveColor : tabInactiveColor;
                if (t.labelText != null) t.labelText.color = active ? tabActiveColor : tabInactiveColor;
                if (t.underline != null) t.underline.gameObject.SetActive(active);
            }
        }

        private void RebuildContent()
        {
            UpdateTabVisuals();
            for (int i = gridContainer.childCount - 1; i >= 0; i--)
                Destroy(gridContainer.GetChild(i).gameObject);
            navigatableButtons.Clear();

            switch (currentTab)
            {
                case Tab.Items: RebuildItemsTab(); break;
                case Tab.Notas: RebuildNotasTab(); break;
                case Tab.Misiones: RebuildMisionesTab(); break;
            }

            // Setup navigation entre las cards y auto-seleccionar la primera
            SetupGridNavigation();
            if (navigatableButtons.Count > 0)
                EventSystem.current?.SetSelectedGameObject(navigatableButtons[0].gameObject);
        }

        private readonly System.Collections.Generic.List<Button> navigatableButtons = new System.Collections.Generic.List<Button>();

        private void SetupGridNavigation()
        {
            int n = navigatableButtons.Count;
            if (n == 0) return;

            // En Misiones cada elemento ocupa una fila completa: navegacion 1 columna.
            // En Items/Notas usa el grid de 'columns' columnas.
            int effectiveCols = (currentTab == Tab.Misiones) ? 1 : columns;

            for (int i = 0; i < n; i++)
            {
                int col = i % effectiveCols;

                int leftIdx = (col == 0) ? -1 : i - 1;
                int rightIdx = (col == effectiveCols - 1 || i == n - 1) ? -1 : i + 1;
                int upIdx = i - effectiveCols;
                int downIdx = i + effectiveCols;
                if (upIdx < 0) upIdx = -1;
                if (downIdx >= n) downIdx = -1;

                var nav = navigatableButtons[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = leftIdx >= 0 ? navigatableButtons[leftIdx] : null;
                nav.selectOnRight = rightIdx >= 0 ? navigatableButtons[rightIdx] : null;
                nav.selectOnUp = upIdx >= 0 ? navigatableButtons[upIdx] : null;
                nav.selectOnDown = downIdx >= 0 ? navigatableButtons[downIdx] : null;
                navigatableButtons[i].navigation = nav;
            }
        }

        private void RebuildItemsTab()
        {
            int total = Inventory.Instance != null ? Inventory.Instance.CarriedItems.Count : 0;
            if (total == 0) { ShowEmpty("No tenes items todavia."); return; }
            emptyText.gameObject.SetActive(false);

            int idx = 0;
            foreach (var item in Inventory.Instance.CarriedItems)
            {
                CreateItemCard(item, idx++);
            }
        }

        private void RebuildNotasTab()
        {
            int total = Inventory.Instance != null ? Inventory.Instance.ReadNotes.Count : 0;
            Debug.Log($"[InventoryOverlay] Rebuild Notas: {total} notas registradas en Inventory.");
            if (total == 0) { ShowEmpty("No has leido ninguna nota."); return; }
            emptyText.gameObject.SetActive(false);

            int idx = 0;
            foreach (var note in Inventory.Instance.ReadNotes)
            {
                CreateNoteCard(note, idx++);
            }
        }

        private void RebuildMisionesTab()
        {
            int total = Inventory.Instance != null ? Inventory.Instance.Missions.Count : 0;
            if (total == 0) { ShowEmpty("Sin misiones activas."); return; }
            emptyText.gameObject.SetActive(false);

            // Las misiones se muestran como FILAS (no grid), con bullet de color
            int idx = 0;
            foreach (var m in Inventory.Instance.Missions)
            {
                CreateMissionRow(m, idx++);
            }
        }

        private void ShowEmpty(string text)
        {
            emptyText.gameObject.SetActive(true);
            emptyText.text = text;
        }

        // ---------- cards ----------

        private void CreateItemCard(Inventory.ItemEntry item, int gridIndex)
        {
            var btn = CreateCardBase("Item_" + item.id, gridIndex, item.displayName, GetIconForItem(item.id));
            string capId = item.id;
            string capName = item.displayName;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[InventoryOverlay] Click en item card: '{capId}'");
                ItemPreviewOverlay.Show(capId, capName);
            });
            navigatableButtons.Add(btn);
        }

        private void CreateNoteCard(Inventory.NoteEntry note, int gridIndex)
        {
            var btn = CreateCardBase("Note_" + note.title, gridIndex, note.title, "📜");
            string title = note.title;
            string body = note.body;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[InventoryOverlay] Click en note card: '{title}'");
                NoteOverlay.Show(title, body);
            });
            navigatableButtons.Add(btn);
        }

        private void CreateMissionRow(Inventory.MissionEntry m, int gridIndex)
        {
            // Las misiones son filas anchas, no cards. Layout: una columna que ocupa el ancho del grid.
            var rowGo = new GameObject("Mission_" + m.id, typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(gridContainer, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            float rowWidth = columns * cardWidth + (columns - 1) * cardSpacing;
            rt.sizeDelta = new Vector2(rowWidth, 50f);
            rt.anchoredPosition = new Vector2(0f, -gridIndex * 60f);

            // Boton clickeable para abrir el detalle
            var bg = rowGo.AddComponent<Image>();
            bg.color = cardBgColor;
            bg.raycastTarget = true;
            ApplyRounded(bg);

            var outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(amberSoft.r, amberSoft.g, amberSoft.b, 0.3f);
            outline.effectDistance = new Vector2(1f, -1f);

            var btn = rowGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            var c = btn.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.4f, 1.3f, 1.05f, 1f);
            c.pressedColor = new Color(0.85f, 0.7f, 0.28f, 1f);
            c.selectedColor = new Color(1.2f, 1.15f, 0.95f, 1f);
            c.fadeDuration = 0.12f;
            btn.colors = c;

            string capText = m.text;
            string capId = m.id;
            var capCategory = m.category;
            bool capCompleted = m.completed;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[InventoryOverlay] Click en mision: '{capId}'");
                string body = BuildMissionDetailBody(capId, capText, capCategory, capCompleted);
                NoteOverlay.Show(capText, body);
            });
            navigatableButtons.Add(btn);

            // Bullet de color segun categoria
            var bulletGo = new GameObject("Bullet", typeof(RectTransform));
            var bulletRT = bulletGo.GetComponent<RectTransform>();
            bulletRT.SetParent(rt, false);
            bulletRT.anchorMin = new Vector2(0f, 0.5f);
            bulletRT.anchorMax = new Vector2(0f, 0.5f);
            bulletRT.pivot = new Vector2(0f, 0.5f);
            bulletRT.sizeDelta = new Vector2(14f, 14f);
            bulletRT.anchoredPosition = new Vector2(20f, 0f);
            var bulletImg = bulletGo.AddComponent<Image>();
            bulletImg.sprite = GetCircleSprite();
            bulletImg.color = m.completed ? missionCompletedColor :
                              (m.category == Inventory.MissionCategory.Principal ? missionPrincipalColor : missionSecundariaColor);
            bulletImg.raycastTarget = false;

            // Label categoria (PRINCIPAL / SECUNDARIA)
            var catGo = new GameObject("Cat", typeof(RectTransform));
            var catRT = catGo.GetComponent<RectTransform>();
            catRT.SetParent(rt, false);
            catRT.anchorMin = new Vector2(0f, 0.5f);
            catRT.anchorMax = new Vector2(0f, 0.5f);
            catRT.pivot = new Vector2(0f, 0.5f);
            catRT.sizeDelta = new Vector2(140f, 18f);
            catRT.anchoredPosition = new Vector2(45f, 0f);
            var catTxt = catGo.AddComponent<Text>();
            catTxt.font = GetDefaultFont();
            catTxt.text = m.category == Inventory.MissionCategory.Principal ? "PRINCIPAL" : "SECUNDARIA";
            catTxt.alignment = TextAnchor.MiddleLeft;
            catTxt.fontSize = 13;
            catTxt.fontStyle = FontStyle.Bold;
            catTxt.color = m.completed ? missionCompletedColor :
                           (m.category == Inventory.MissionCategory.Principal ? missionPrincipalColor : missionSecundariaColor);
            catTxt.raycastTarget = false;

            // Texto de la mision
            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.SetParent(rt, false);
            textRT.anchorMin = new Vector2(0f, 0f);
            textRT.anchorMax = new Vector2(1f, 1f);
            textRT.offsetMin = new Vector2(190f, 4f);
            textRT.offsetMax = new Vector2(-20f, -4f);
            var t = textGo.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.fontSize = 22;
            t.fontStyle = m.completed ? FontStyle.Italic : FontStyle.Bold;
            t.color = m.completed ? missionCompletedColor : labelColor;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = true;
            t.text = m.completed ? $"<s>{m.text}</s>" : m.text;
            t.raycastTarget = false;
        }

        private Button CreateCardBase(string goName, int gridIndex, string label, string iconChar)
        {
            int row = gridIndex / columns;
            int col = gridIndex % columns;

            var go = new GameObject(goName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(gridContainer, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(cardWidth, cardHeight);
            float totalWidth = columns * cardWidth + (columns - 1) * cardSpacing;
            float xStart = -totalWidth * 0.5f + cardWidth * 0.5f;
            rt.anchoredPosition = new Vector2(xStart + col * (cardWidth + cardSpacing),
                                              -row * (cardHeight + cardSpacing));

            var bg = go.AddComponent<Image>();
            bg.color = cardBgColor;
            ApplyRounded(bg);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = amberSoft;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var c = btn.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.4f, 1.3f, 1.05f, 1f);
            c.pressedColor = new Color(0.85f, 0.7f, 0.28f, 1f);
            c.selectedColor = new Color(1.2f, 1.15f, 0.95f, 1f);
            c.fadeDuration = 0.12f;
            btn.colors = c;

            // Icon area
            var iconBgGo = new GameObject("IconBg", typeof(RectTransform));
            var iconBgRT = iconBgGo.GetComponent<RectTransform>();
            iconBgRT.SetParent(rt, false);
            iconBgRT.anchorMin = new Vector2(0.5f, 1f);
            iconBgRT.anchorMax = new Vector2(0.5f, 1f);
            iconBgRT.pivot = new Vector2(0.5f, 1f);
            iconBgRT.sizeDelta = new Vector2(cardWidth - 30f, cardWidth - 30f);
            iconBgRT.anchoredPosition = new Vector2(0f, -15f);
            var iconBgImg = iconBgGo.AddComponent<Image>();
            iconBgImg.color = iconBgColor;
            iconBgImg.raycastTarget = false;
            ApplyRounded(iconBgImg);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.SetParent(iconBgRT, false);
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
            var iconTxt = iconGo.AddComponent<Text>();
            iconTxt.font = GetDefaultFont();
            iconTxt.text = iconChar;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.fontSize = 100;
            iconTxt.color = labelColor;
            iconTxt.raycastTarget = false;

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(1f, 0f);
            lblRT.pivot = new Vector2(0.5f, 0f);
            lblRT.sizeDelta = new Vector2(0f, 40f);
            lblRT.anchoredPosition = new Vector2(0f, 12f);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = label;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontSize = 20;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = labelColor;
            lbl.raycastTarget = false;
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;

            return btn;
        }

        private static string GetIconForItem(string itemId)
        {
            switch (itemId)
            {
                case "linterna": return "💡";
                case "destornillador": return "🔧";
                default: return "📦";
            }
        }

        private static string BuildMissionDetailBody(string id, string text, Inventory.MissionCategory cat, bool completed)
        {
            string categoria = cat == Inventory.MissionCategory.Principal ? "PRINCIPAL" : "SECUNDARIA";
            string estado = completed ? "COMPLETADA" : "ACTIVA";
            string descripcion = GetMissionDescription(id);
            return $"Mision {categoria}\nEstado: {estado}\n\n{descripcion}";
        }

        private static string GetMissionDescription(string id)
        {
            // Descripciones especificas para las misiones del puzzle. Si no hay match, default.
            switch (id)
            {
                case "salir_salon":
                    return "Estas atrapado en el salon de clases. Encuentra una forma de salir.";
                case "buscar_codigo_armario":
                    return "El armario del fondo tiene una cerradura. " +
                           "Busca pistas en el salon (papeles, numeros, fechas) para descifrar el codigo.";
                case "decifrar_codigo_lonchera":
                    return "La lonchera de Pa esta cerrada con un codigo. " +
                           "Quiza haya algo que te de la pista cuando abras el armario.";
                case "legacy_objective":
                    return "Mision activa.";
                default:
                    return "Sigue las pistas del salon para descubrir como avanzar.";
            }
        }

        // ---------- build UI ----------

        private void Build()
        {
            var canvasGo = new GameObject("Inventory_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 195;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // Backdrop
            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Content root (este es el que se anima — escala desde 0.4 a 1.0)
            // Anchor full-screen, pivot CENTERED para que la animacion sea uniforme.
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.SetParent(canvas.transform, false);
            contentRT.anchorMin = Vector2.zero; contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = Vector2.zero; contentRT.offsetMax = Vector2.zero;
            contentRT.pivot = new Vector2(0.5f, 0.5f);

            // Title — centrado horizontalmente, arriba (y=380)
            MakeText(contentRT, "Title", "INVENTARIO", 56, FontStyle.Bold, titleColor,
                new Vector2(0f, 380f), new Vector2(900f, 80f), useTitleFont: true);

            // Linea decorativa debajo del titulo
            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(contentRT, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(220f, 2f);
            lineRT.anchoredPosition = new Vector2(0f, 330f);
            lineGo.AddComponent<Image>().color = amberSoft;

            // Tabs centrados en y=240
            BuildTabs(contentRT);

            // Grid container — centrado horizontalmente, arranca en y=120 y se expande hacia abajo
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridContainer = gridGo.GetComponent<RectTransform>();
            gridContainer.SetParent(contentRT, false);
            gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
            gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
            gridContainer.pivot = new Vector2(0.5f, 1f); // pivot top-center -> cards bajan desde aqui
            float totalWidth = columns * cardWidth + (columns - 1) * cardSpacing;
            gridContainer.sizeDelta = new Vector2(totalWidth, 600f);
            gridContainer.anchoredPosition = new Vector2(0f, 120f);

            // Empty placeholder — al centro vertical (cuando no hay items/notas/misiones)
            emptyText = MakeText(contentRT, "Empty", "", 24, FontStyle.Italic,
                emptyTextColor, new Vector2(0f, -100f), new Vector2(900f, 60f), useTitleFont: false);
            emptyText.gameObject.SetActive(false);

            // Hint footer — centrado al pie
            MakeText(contentRT, "Hint",
                "Q/E o L1/R1 = cambiar tab    ·    Flechas / D-pad = mover entre cards    ·    Enter / Cross = abrir    ·    I / Tab / Esc = cerrar",
                15, FontStyle.Normal,
                new Color(labelColor.r, labelColor.g, labelColor.b, 0.65f),
                new Vector2(0f, -460f), new Vector2(1700f, 26f), useTitleFont: false);
        }

        private void BuildTabs(Transform parent)
        {
            var tabsGo = new GameObject("Tabs", typeof(RectTransform));
            tabsContainer = tabsGo.GetComponent<RectTransform>();
            tabsContainer.SetParent(parent, false);
            tabsContainer.anchorMin = new Vector2(0.5f, 0.5f);
            tabsContainer.anchorMax = new Vector2(0.5f, 0.5f);
            tabsContainer.pivot = new Vector2(0.5f, 0.5f);
            tabsContainer.sizeDelta = new Vector2(900f, 70f);
            tabsContainer.anchoredPosition = new Vector2(0f, 240f);

            BuildOneTab(tabsContainer, Tab.Items, "📦", "ITEMS", new Vector2(-300f, 0f));
            BuildOneTab(tabsContainer, Tab.Notas, "📜", "NOTAS", new Vector2(0f, 0f));
            BuildOneTab(tabsContainer, Tab.Misiones, "✦", "MISIONES", new Vector2(300f, 0f));

            UpdateTabVisuals();
        }

        private void BuildOneTab(Transform parent, Tab tab, string iconChar, string labelText, Vector2 anchoredPos)
        {
            var go = new GameObject("Tab_" + tab.ToString(), typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 60f);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var capturedTab = tab;
            btn.onClick.AddListener(() => SwitchTab(capturedTab));

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.SetParent(rt, false);
            iconRT.anchorMin = new Vector2(0f, 0.5f);
            iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0f, 0.5f);
            iconRT.sizeDelta = new Vector2(40f, 40f);
            iconRT.anchoredPosition = new Vector2(20f, 0f);
            var iconTxt = iconGo.AddComponent<Text>();
            iconTxt.font = GetDefaultFont();
            iconTxt.text = iconChar;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.fontSize = 34;
            iconTxt.color = tabInactiveColor;
            iconTxt.raycastTarget = false;

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(rt, false);
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(1f, 1f);
            lblRT.offsetMin = new Vector2(70f, 0f);
            lblRT.offsetMax = new Vector2(-10f, 0f);
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.font = GetDefaultFont();
            lblTxt.text = labelText;
            lblTxt.alignment = TextAnchor.MiddleLeft;
            lblTxt.fontSize = 22;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color = tabInactiveColor;
            lblTxt.raycastTarget = false;

            // Underline
            var underGo = new GameObject("Underline", typeof(RectTransform));
            var underRT = underGo.GetComponent<RectTransform>();
            underRT.SetParent(rt, false);
            underRT.anchorMin = new Vector2(0.5f, 0f);
            underRT.anchorMax = new Vector2(0.5f, 0f);
            underRT.pivot = new Vector2(0.5f, 0f);
            underRT.sizeDelta = new Vector2(180f, 2f);
            underRT.anchoredPosition = new Vector2(0f, -2f);
            underGo.AddComponent<Image>().color = tabActiveColor;
            underGo.SetActive(false);

            tabButtons.Add(new TabBtn { tab = tab, circleBg = null, iconText = iconTxt, labelText = lblTxt, underline = underRT });
        }

        // ---------- helpers ----------

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta, bool useTitleFont = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            // Anchor + pivot centrados para que las posiciones (anchoredPos) sean relativas al CENTRO del padre.
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = useTitleFont ? GetTitleFont() : GetDefaultFont();
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private void ApplyRounded(Image img)
        {
            if (roundedSpriteCache == null)
            {
                const int size = 32;
                const int radius = 8;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x < radius ? (radius - x) : (x >= size - radius ? x - (size - radius - 1) : 0);
                    float dy = y < radius ? (radius - y) : (y >= size - radius ? y - (size - radius - 1) : 0);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = d > 0f ? Mathf.Clamp01(radius - d) : 1f;
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
                tex.SetPixels(px);
                tex.Apply();
                roundedSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            }
            img.sprite = roundedSpriteCache;
            img.type = Image.Type.Sliced;
        }

        private Sprite GetCircleSprite()
        {
            if (circleSpriteCache != null) return circleSpriteCache;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[s * s];
            float cx = s * 0.5f, cy = s * 0.5f;
            float r = s * 0.5f - 1f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d);
                px[y * s + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return circleSpriteCache;
        }

        private static Font GetTitleFont()
        {
            if (MainMenuController.SharedTitleFont != null) return MainMenuController.SharedTitleFont;
            return GetDefaultFont();
        }

        private static Font GetDefaultFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
