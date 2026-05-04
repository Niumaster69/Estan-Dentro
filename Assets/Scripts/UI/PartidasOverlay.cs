using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.Network;

namespace EstanDentro.UI
{
    // Lista de partidas guardadas del jugador actual.
    //   - GET /api/Partidas (filtra cliente por GameSession.CurrentJugadorId).
    //   - Por cada partida: [Retomar] reusa el idPartida viejo. [Borrar] DELETE /api/Partidas/{id}.
    //   - Si no hay jugador (offline) o lista vacia: muestra mensaje.
    //
    // Open(onResume, onClose):
    //   - onResume(idPartida, fechaInicio, capituloAlcanzado): el caller arranca el flow de retomar.
    //   - onClose: que ejecutar al pulsar "Cerrar" (volver al MainMenu por ejemplo).
    public class PartidasOverlay : MonoBehaviour
    {
        private static PartidasOverlay instance;

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color titleLineColor = new Color(0.85f, 0.7f, 0.28f, 0.6f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color subTextColor = new Color(0.78f, 0.62f, 0.22f, 0.85f);
        [SerializeField] private Color rowBgColor = new Color(0.18f, 0.16f, 0.14f, 1f);
        [SerializeField] private Color buttonNormalColor = new Color(0.10f, 0.10f, 0.10f, 0.9f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color deleteButtonColor = new Color(0.50f, 0.10f, 0.10f, 0.9f);

        // ---------- runtime ----------

        private Canvas canvas;
        private RectTransform panel;
        private RectTransform listContent;
        private Text emptyMessage;
        private Text statusText;
        private Button volverButton;
        private readonly List<Button> rowRetomarButtons = new List<Button>();
        private readonly List<Button> rowBorrarButtons = new List<Button>();
        private Action<int, DateTime, int> onResumeCallback;
        private Action onCloseCallback;
        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;

        // ---------- public API ----------

        public static void Open(Action<int, DateTime, int> onResume, Action onClose)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;

            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.prevCursorLock = Cursor.lockState;
            instance.prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            instance.onResumeCallback = onResume;
            instance.onCloseCallback = onClose;
            instance.canvas.gameObject.SetActive(true);
            instance.consumeInputThisFrame = true;
            OverlayBlocker.Register();

            instance.LoadAndPopulate();
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
        }

        // ---------- lifecycle ----------

        private static void EnsureInstance()
        {
            if (instance != null && instance.canvas != null) return;
            if (instance != null) Destroy(instance.gameObject);
            var go = new GameObject("__PartidasOverlay");
            instance = go.AddComponent<PartidasOverlay>();
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

            // Esc (teclado) o Circle/B (gamepad) = volver al menu
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && kb.escapeKey.wasPressedThisFrame)
                        || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (dismiss)
            {
                Close();
                onCloseCallback?.Invoke();
            }
        }

        // ---------- carga de datos ----------

        private void LoadAndPopulate()
        {
            Debug.Log($"[Partidas] LoadAndPopulate. CurrentJugadorId={GameSession.CurrentJugadorId}, IsOnline={GameSession.IsOnline}");
            ClearChildren(listContent);
            rowRetomarButtons.Clear();
            rowBorrarButtons.Clear();
            emptyMessage.gameObject.SetActive(false);
            statusText.text = "Cargando partidas...";
            statusText.gameObject.SetActive(true);

            if (!GameSession.IsOnline)
            {
                ShowMessage("Sin conexion. Conectate a internet para ver tus partidas.");
                EventSystem.current?.SetSelectedGameObject(volverButton != null ? volverButton.gameObject : null);
                return;
            }

            ApiClient.Instance.GetAllPartidas(
                partidas =>
                {
                    statusText.gameObject.SetActive(false);
                    Debug.Log($"[Partidas] GET OK. Total recibidas={partidas.Length}");
                    var mias = FilterByJugador(partidas, GameSession.CurrentJugadorId);
                    Debug.Log($"[Partidas] Filtradas para idJugador={GameSession.CurrentJugadorId}: {mias.Count}");
                    if (mias.Count == 0)
                    {
                        ShowMessage("No tenes partidas guardadas todavia.");
                        EventSystem.current?.SetSelectedGameObject(volverButton != null ? volverButton.gameObject : null);
                        return;
                    }
                    foreach (var p in mias)
                    {
                        Debug.Log($"[Partidas] BuildRow idPartida={p.idPartida} estado={p.estado} cap={p.capituloAlcanzado}");
                        BuildRow(p);
                    }
                    SetupGridNavigation();
                    if (rowRetomarButtons.Count > 0)
                        EventSystem.current?.SetSelectedGameObject(rowRetomarButtons[0].gameObject);
                },
                error =>
                {
                    Debug.LogWarning($"[Partidas] GET fallo: {error}");
                    ShowMessage("Error cargando partidas: " + error);
                    EventSystem.current?.SetSelectedGameObject(volverButton != null ? volverButton.gameObject : null);
                }
            );
        }

        // Conecta los botones de filas y el "Volver" en una grilla 2-col x N-row
        // para que se pueda navegar arriba/abajo/izq/der con teclado y gamepad.
        private void SetupGridNavigation()
        {
            int n = rowRetomarButtons.Count;
            for (int i = 0; i < n; i++)
            {
                var retomar = rowRetomarButtons[i];
                var borrar = rowBorrarButtons[i];

                var navR = retomar.navigation;
                navR.mode = Navigation.Mode.Explicit;
                navR.selectOnRight = borrar;
                navR.selectOnUp = i > 0 ? rowRetomarButtons[i - 1] : volverButton;
                navR.selectOnDown = i < n - 1 ? rowRetomarButtons[i + 1] : volverButton;
                navR.selectOnLeft = null;
                retomar.navigation = navR;

                var navB = borrar.navigation;
                navB.mode = Navigation.Mode.Explicit;
                navB.selectOnLeft = retomar;
                navB.selectOnUp = i > 0 ? rowBorrarButtons[i - 1] : volverButton;
                navB.selectOnDown = i < n - 1 ? rowBorrarButtons[i + 1] : volverButton;
                navB.selectOnRight = null;
                borrar.navigation = navB;
            }

            if (volverButton != null)
            {
                var navV = volverButton.navigation;
                navV.mode = Navigation.Mode.Explicit;
                navV.selectOnUp = n > 0 ? rowRetomarButtons[n - 1] : null;
                navV.selectOnDown = n > 0 ? rowRetomarButtons[0] : null;
                navV.selectOnLeft = null;
                navV.selectOnRight = null;
                volverButton.navigation = navV;
            }
        }

        private static List<PartidaDto> FilterByJugador(PartidaDto[] all, int jugadorId)
        {
            var result = new List<PartidaDto>();
            foreach (var p in all)
                if (p.idJugador == jugadorId) result.Add(p);
            // mas reciente primero
            result.Sort((a, b) => string.CompareOrdinal(b.fechaInicio, a.fechaInicio));
            return result;
        }

        private void ShowMessage(string msg)
        {
            statusText.gameObject.SetActive(false);
            emptyMessage.text = msg;
            emptyMessage.gameObject.SetActive(true);
        }

        private void BuildRow(PartidaDto p)
        {
            // Posicionamiento manual sin LayoutGroup. Cada fila ocupa 100px (90 alto + 10 spacing).
            int index = listContent.childCount;
            const float rowHeight = 90f;
            const float rowSpacing = 10f;
            const float topPadding = 10f;

            var rowGo = new GameObject($"Row_{p.idPartida}", typeof(RectTransform));
            var rowRT = rowGo.GetComponent<RectTransform>();
            rowRT.SetParent(listContent, false);
            rowRT.anchorMin = new Vector2(0f, 1f);   // top stretch
            rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot = new Vector2(0.5f, 1f);     // top-center
            rowRT.sizeDelta = new Vector2(-24f, rowHeight); // -24 = padding 12 LR del Content
            rowRT.anchoredPosition = new Vector2(0f, -topPadding - index * (rowHeight + rowSpacing));
            var bg = rowGo.AddComponent<Image>();
            bg.color = rowBgColor;
            ApplyRounded(bg);

            // Sombra sutil debajo de la fila
            var shadow = rowGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // Info text (izquierda, ocupa todo menos los botones derechos)
            var infoGo = new GameObject("Info", typeof(RectTransform));
            infoGo.transform.SetParent(rowGo.transform, false);
            var infoRT = infoGo.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0f, 0f);
            infoRT.anchorMax = new Vector2(1f, 1f);
            infoRT.offsetMin = new Vector2(16f, 8f);
            infoRT.offsetMax = new Vector2(-300f, -8f); // 300 px reservados a la derecha para botones
            var info = infoGo.AddComponent<Text>();
            info.font = GetDefaultFont();
            info.fontSize = 16;
            info.color = textColor;
            info.alignment = TextAnchor.MiddleLeft;
            info.supportRichText = true;
            info.raycastTarget = false;
            info.text = BuildInfoText(p);

            // Boton Borrar (extremo derecho)
            var borrarBtn = BuildRowButton(rowGo.transform, "Borrar", deleteButtonColor, 120f, 50f, -16f,
                () => HandleBorrar(p, rowGo));
            rowBorrarButtons.Add(borrarBtn);
            // Boton Retomar (al lado izquierdo del Borrar)
            var retomarBtn = BuildRowButton(rowGo.transform, "Retomar", buttonNormalColor, 140f, 50f, -16f - 120f - 12f,
                () => HandleRetomar(p));
            rowRetomarButtons.Add(retomarBtn);
        }

        // Crea un boton anclado al borde DERECHO de la fila (anchor 1, pivot 1).
        // offsetX es la posicion X relativa al borde derecho (negativo = hacia la izquierda).
        private Button BuildRowButton(Transform parent, string label, Color normalColor, float w, float h, float offsetX, Action onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(offsetX, 0f);
            var btnImg = go.AddComponent<Image>();
            // Image.color queda blanco (default). El ColorBlock del Button controla los estados:
            // si seteo color aqui, multiplicaria con el ColorBlock y el highlight se ve apagado.
            ApplyRounded(btnImg);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var c = btn.colors;
            c.normalColor = normalColor; c.highlightedColor = buttonHoverColor;
            c.pressedColor = buttonPressedColor; c.selectedColor = buttonHoverColor;
            c.fadeDuration = 0.1f;
            btn.colors = c;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = label;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontSize = 16;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = textColor;
            lbl.raycastTarget = false;
            return btn;
        }

        private string BuildInfoText(PartidaDto p)
        {
            string fecha = TryFormatDate(p.fechaInicio);
            string estadoStr = EstadoLabel(p.estado);
            string accent = "<color=#" + ColorUtility.ToHtmlStringRGB(subTextColor) + ">";
            string close = "</color>";
            return $"<b>Partida #{p.idPartida}</b>     {accent}{estadoStr}{close}\n" +
                   $"{fecha}   ·   Cap {p.capituloAlcanzado}   ·   {FormatTiempo(p.tiempoSegundos)}";
        }

        private static string EstadoLabel(byte estado)
        {
            switch (estado)
            {
                case 0: return "En curso";
                case 1: return "Completada";
                case 2: return "Abandonada";
                default: return "?";
            }
        }

        private static string TryFormatDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "-";
            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return iso;
        }

        private static string FormatTiempo(int segundos)
        {
            if (segundos < 0) segundos = 0;
            int h = segundos / 3600;
            int m = (segundos % 3600) / 60;
            int s = segundos % 60;
            return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }

        // ---------- handlers ----------

        private void HandleRetomar(PartidaDto p)
        {
            DateTime fecha = DateTime.UtcNow;
            DateTime.TryParse(p.fechaInicio, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out fecha);
            int idPartida = p.idPartida;
            int capituloAlcanzado = p.capituloAlcanzado;
            DateTime fechaInicio = fecha;
            // Cerrar overlay y delegar al caller (MainMenuController) para que arranque el flow
            Close();
            onResumeCallback?.Invoke(idPartida, fechaInicio, capituloAlcanzado);
        }

        private void HandleBorrar(PartidaDto p, GameObject rowGo)
        {
            statusText.text = $"Borrando partida #{p.idPartida}...";
            statusText.gameObject.SetActive(true);
            ApiClient.Instance.DeletePartida(p.idPartida,
                () =>
                {
                    Destroy(rowGo);
                    statusText.gameObject.SetActive(false);
                    Debug.Log($"[Partidas] Partida #{p.idPartida} borrada.");
                },
                err =>
                {
                    statusText.text = "Error al borrar: " + err;
                    Debug.LogWarning($"[Partidas] Fallo borrar #{p.idPartida}: {err}");
                });
        }

        private void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }

        // ---------- BUILD UI ----------

        private void Build()
        {
            BuildCanvas();
            BuildHeader();
            BuildList();
            BuildBottomButton();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Partidas_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 215;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panel = panelGo.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero; panel.offsetMax = Vector2.zero;
        }

        private void BuildHeader()
        {
            // Title con sombra (mismo estilo que SettingsOverlay)
            var titleTxt = MakeText(panel, "Title", "PARTIDAS", 64, FontStyle.Bold, titleColor,
                new Vector2(0f, 400f), new Vector2(900f, 90f));
            var titleShadow = titleTxt.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(3f, -4f);

            // Linea decorativa fina debajo del titulo
            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(panel, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(280f, 2f);
            lineRT.anchoredPosition = new Vector2(0f, 340f);
            lineGo.AddComponent<Image>().color = titleLineColor;

            statusText = MakeText(panel, "Status", "", 18, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.7f),
                new Vector2(0f, 305f), new Vector2(1000f, 30f));
            statusText.gameObject.SetActive(false);

            emptyMessage = MakeText(panel, "Empty", "", 22, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(1000f, 60f));
            emptyMessage.gameObject.SetActive(false);
        }

        private void BuildList()
        {
            // Container simple del listado. Sin scroll ni layout group (filas posicionadas
            // a mano en BuildRow). Para el alcance academico (max ~5 partidas) no hace falta scroll.
            var contentGo = new GameObject("ListContent", typeof(RectTransform));
            listContent = contentGo.GetComponent<RectTransform>();
            listContent.SetParent(panel, false);
            listContent.anchorMin = listContent.anchorMax = new Vector2(0.5f, 0.5f);
            listContent.pivot = new Vector2(0.5f, 1f); // top-center: anchoredPosition es el TOP del container
            listContent.sizeDelta = new Vector2(1100f, 500f);
            listContent.anchoredPosition = new Vector2(0f, 250f); // top en y=250 (parent center+250)

            var bg = contentGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);
            ApplyRounded(bg);
        }

        private void BuildBottomButton()
        {
            var go = new GameObject("Btn_Volver", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 56f);
            rt.anchoredPosition = new Vector2(0f, -340f);
            var volverImg = go.AddComponent<Image>();
            // Image.color blanco para que el ColorBlock controle el hover/normal/pressed sin atenuarlo
            ApplyRounded(volverImg);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = volverImg;
            var c = btn.colors;
            c.normalColor = buttonNormalColor; c.highlightedColor = buttonHoverColor;
            c.pressedColor = buttonPressedColor; c.selectedColor = buttonHoverColor;
            c.fadeDuration = 0.12f;
            btn.colors = c;
            btn.onClick.AddListener(() => { Close(); onCloseCallback?.Invoke(); });
            volverButton = btn;

            var volverShadow = go.AddComponent<Shadow>();
            volverShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            volverShadow.effectDistance = new Vector2(2f, -2f);

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = "Volver";
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontSize = 18;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = textColor;
            lbl.raycastTarget = false;

            // Hint debajo del boton: [Esc] tambien cierra
            MakeText(panel, "BackHint", "[Esc] / [Circle] para volver", 14, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.5f),
                new Vector2(0f, -395f), new Vector2(500f, 24f));
        }

        // ---------- helpers ----------

        private Button MakeButton(Transform parent, string label, Vector2 size, Color normalColor, Action onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size.x; le.preferredHeight = size.y;
            go.AddComponent<Image>().color = normalColor;
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.normalColor = normalColor; c.highlightedColor = buttonHoverColor;
            c.pressedColor = buttonPressedColor; c.selectedColor = buttonHoverColor;
            c.fadeDuration = 0.1f;
            btn.colors = c;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = label;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontSize = 16;
            lbl.fontStyle = FontStyle.Bold;
            lbl.color = textColor;
            lbl.raycastTarget = false;
            return btn;
        }

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color, Vector2 pos, Vector2 size2)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size2;
            rt.anchoredPosition = pos;
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
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        // ---------- sprite redondeado procedural (mismo patron que SettingsOverlay) ----------

        private Sprite roundedRectSpriteCache;

        private Sprite GetRoundedRectSprite()
        {
            if (roundedRectSpriteCache != null) return roundedRectSpriteCache;
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
            roundedRectSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedRectSpriteCache;
        }

        private void ApplyRounded(Image img)
        {
            if (img == null) return;
            img.sprite = GetRoundedRectSprite();
            img.type = Image.Type.Sliced;
        }
    }
}
