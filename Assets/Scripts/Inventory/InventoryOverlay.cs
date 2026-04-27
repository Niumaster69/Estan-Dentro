using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.UI;
using EstanDentro.Interaction;

namespace EstanDentro.Inventory
{
    public class InventoryOverlay : MonoBehaviour
    {
        private static InventoryOverlay instance;

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.07f, 0.05f, 0.97f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color entryNormalColor = new Color(0.12f, 0.12f, 0.13f, 0.9f);
        [SerializeField] private Color entryHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.6f);
        [SerializeField] private Color entryPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.85f);
        [SerializeField] private Color entryTextColor = new Color(0.95f, 0.93f, 0.85f, 0.95f);

        private Canvas canvas;
        private RectTransform panel;
        private Transform entriesContainer;
        private Text emptyText;
        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;
        private System.Action onCloseCallback;

        public static void Open(System.Action onClose = null)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;
            instance.RebuildList();
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
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.gameObject.activeSelf) return;
            instance.canvas.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
            Time.timeScale = instance.prevTimeScale > 0f ? instance.prevTimeScale : 1f;
            Cursor.lockState = instance.prevCursorLock;
            Cursor.visible = instance.prevCursorVisible;
            OverlayBlocker.Unregister();
            var cb = instance.onCloseCallback;
            instance.onCloseCallback = null;
            cb?.Invoke();
        }

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

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && (kb.iKey.wasPressedThisFrame
                                          || kb.escapeKey.wasPressedThisFrame))
                        || (gp != null && (gp.buttonEast.wasPressedThisFrame
                                          || gp.selectButton.wasPressedThisFrame));
            if (dismiss) Close();
        }

        // ---------- list ----------

        private void RebuildList()
        {
            // Limpia entradas previas
            for (int i = entriesContainer.childCount - 1; i >= 0; i--)
                Destroy(entriesContainer.GetChild(i).gameObject);

            if (Inventory.Instance == null || Inventory.Instance.Count == 0)
            {
                emptyText.gameObject.SetActive(true);
                return;
            }

            emptyText.gameObject.SetActive(false);

            var buttons = new System.Collections.Generic.List<Button>();
            int i_idx = 0;
            foreach (var note in Inventory.Instance.ReadNotes)
            {
                buttons.Add(CreateEntry(note, i_idx));
                i_idx++;
            }

            // Navegacion entre entradas (teclado/gamepad)
            for (int i = 0; i < buttons.Count; i++)
            {
                var nav = buttons[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = buttons[(i - 1 + buttons.Count) % buttons.Count];
                nav.selectOnDown = buttons[(i + 1) % buttons.Count];
                buttons[i].navigation = nav;
            }

            // Auto-seleccionar la primera para que gamepad/teclado puedan operar
            if (buttons.Count > 0)
                EventSystem.current?.SetSelectedGameObject(buttons[0].gameObject);
        }

        private Button CreateEntry(Inventory.NoteEntry note, int index)
        {
            var go = new GameObject("Entry_" + note.title);
            go.transform.SetParent(entriesContainer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(720f, 48f);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -index * 56f);

            var img = go.AddComponent<Image>();
            img.color = entryNormalColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = entryNormalColor;
            colors.highlightedColor = entryHoverColor;
            colors.pressedColor = entryPressedColor;
            colors.selectedColor = entryHoverColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            string title = note.title;
            string body = note.body;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"[Inventory] Click en nota '{title}'");
                // Capturamos el callback original (ej. "abrir pause") para propagarlo
                // cuando reabramos el inventario despues de cerrar la nota.
                var originalCallback = onCloseCallback;
                onCloseCallback = null; // evitar que Close() lo dispare ahora
                Close();
                NoteOverlay.Show(title, body, onClose: () => InventoryOverlay.Open(originalCallback));
            });

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(20f, 0f);
            lblRT.offsetMax = new Vector2(-20f, 0f);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = note.title;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.fontSize = 22;
            lbl.fontStyle = FontStyle.Italic;
            lbl.color = entryTextColor;
            lbl.raycastTarget = false;

            return btn;
        }

        // ---------- build ----------

        private void Build()
        {
            var canvasGo = new GameObject("Inventory_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 195;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Panel
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvas.transform, false);
            panel = panelGo.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(820f, 600f);
            panelGo.AddComponent<Image>().color = panelColor;

            // Title
            MakeText(panel, "Title", "DIARIO DE NOTAS", 32, FontStyle.Bold, titleColor,
                new Vector2(0, 250), new Vector2(740, 50));

            // Container para las entradas. Cuidado: agregar RectTransform PRIMERO
            // (si guardas transform antes de AddComponent<RectTransform>, queda destruido).
            var contGo = new GameObject("Entries");
            var contRT = contGo.AddComponent<RectTransform>();
            contRT.SetParent(panel, false);
            contRT.anchorMin = new Vector2(0.5f, 0.5f);
            contRT.anchorMax = new Vector2(0.5f, 0.5f);
            contRT.pivot = new Vector2(0.5f, 0.5f);
            contRT.sizeDelta = new Vector2(740f, 420f);
            contRT.anchoredPosition = new Vector2(0f, -10f);
            entriesContainer = contRT;

            // Empty placeholder
            emptyText = MakeText(panel, "Empty", "Aun no recogiste ninguna nota.", 20, FontStyle.Italic,
                new Color(entryTextColor.r, entryTextColor.g, entryTextColor.b, 0.5f),
                new Vector2(0, -10), new Vector2(740, 40));
            emptyText.gameObject.SetActive(false);

            // Hint
            MakeText(panel, "Hint", "Click para releer - I / Touchpad / Esc / Circle para cerrar",
                14, FontStyle.Normal,
                new Color(entryTextColor.r, entryTextColor.g, entryTextColor.b, 0.55f),
                new Vector2(0, -260), new Vector2(740, 26));
        }

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
