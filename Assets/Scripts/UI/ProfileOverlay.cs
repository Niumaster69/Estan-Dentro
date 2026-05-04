using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.Network;

namespace EstanDentro.UI
{
    // Pantalla de Perfil: nombre editable + estadisticas + historial de partidas.
    // Aqui es donde el profe ve la BD relacional poblada con datos del jugador.
    // Detalle en Document/Flujo_API_Estan_Dentro.md seccion 5.
    public class ProfileOverlay : MonoBehaviour
    {
        private static ProfileOverlay instance;

        [Header("Visual (mismo lenguaje que SettingsOverlay)")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.96f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color labelColor = new Color(0.92f, 0.89f, 0.83f, 0.7f);
        [SerializeField] private Color accentColor = new Color(0.85f, 0.7f, 0.28f, 0.95f);
        [SerializeField] private Color buttonNormalColor = new Color(0.10f, 0.10f, 0.10f, 0.9f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color rowEvenColor = new Color(1f, 1f, 1f, 0.04f);
        [SerializeField] private Color rowOddColor = new Color(1f, 1f, 1f, 0.0f);
        [SerializeField] private Color rowCompletadaColor = new Color(0.55f, 0.85f, 0.45f, 1f);
        [SerializeField] private Color rowAbandonadaColor = new Color(0.85f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color rowEnCursoColor = new Color(0.85f, 0.7f, 0.28f, 1f);

        // ---------- runtime ----------

        private Canvas canvas;
        private Image bgImage;
        private RectTransform panel;

        // Nombre editable
        private InputField nombresInput;
        private Button editButton;
        private Text editButtonLabel;
        private bool editing;
        private string nombresOriginal;

        // Stats
        private Text statsText;

        // Historial
        private RectTransform historyContent;
        private Text historyEmptyText;

        // Status global
        private Text statusText;

        // Estado de carga
        private int pendingApiCalls;
        private JugadorDto currentJugador;
        private PartidaDto[] currentPartidas;
        private LogroXPartidaDto[] currentLogroXPartida;
        private bool hadAnyError;

        // Pause/cursor management
        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;
        private Action onCloseCallback;

        // ---------- public API ----------

        public static void Open(Action onClose = null)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;

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

            instance.ResetUiState();
            instance.LoadDataFromApi();
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

        // ---------- lifecycle ----------

        private static void EnsureInstance()
        {
            if (instance != null && instance.canvas != null) return;
            if (instance != null) Destroy(instance.gameObject);
            var go = new GameObject("__ProfileOverlay");
            instance = go.AddComponent<ProfileOverlay>();
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
            if (editing) return; // ESC no cierra mientras se edita el nombre, solo cancela

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && kb.escapeKey.wasPressedThisFrame)
                        || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (dismiss) Close();
        }

        // ---------- data loading ----------

        private void ResetUiState()
        {
            editing = false;
            EndEditMode();
            statsText.text = "";
            historyEmptyText.gameObject.SetActive(true);
            historyEmptyText.text = "Cargando...";
            ClearHistoryRows();
            SetStatus("");

            currentJugador = null;
            currentPartidas = null;
            currentLogroXPartida = null;
            hadAnyError = false;
            pendingApiCalls = 0;
        }

        private void LoadDataFromApi()
        {
            if (!GameSession.IsOnline)
            {
                SetStatus("Modo offline. No hay conexion a la API.");
                historyEmptyText.text = "Sin datos.";
                nombresInput.text = GameSession.CurrentNombres;
                nombresInput.interactable = false;
                editButton.interactable = false;
                return;
            }

            nombresInput.interactable = false; // se habilita al entrar en modo edit
            editButton.interactable = true;

            pendingApiCalls = 3;

            ApiClient.Instance.GetJugador(GameSession.CurrentJugadorId,
                j => { currentJugador = j; OnApiCallDone(); },
                err => { hadAnyError = true; Debug.LogWarning($"[ProfileOverlay] GetJugador err: {err}"); OnApiCallDone(); });

            ApiClient.Instance.GetAllPartidas(
                arr => { currentPartidas = arr; OnApiCallDone(); },
                err => { hadAnyError = true; Debug.LogWarning($"[ProfileOverlay] GetAllPartidas err: {err}"); OnApiCallDone(); });

            ApiClient.Instance.GetAllLogroXPartida(
                arr => { currentLogroXPartida = arr; OnApiCallDone(); },
                err => { hadAnyError = true; Debug.LogWarning($"[ProfileOverlay] GetAllLogroXPartida err: {err}"); OnApiCallDone(); });
        }

        private void OnApiCallDone()
        {
            pendingApiCalls--;
            if (pendingApiCalls > 0) return;

            if (hadAnyError && currentJugador == null)
            {
                SetStatus("No se pudo cargar el perfil. Reintenta mas tarde.");
                historyEmptyText.text = "Sin datos.";
                editButton.interactable = false;
                return;
            }

            // Llenar nombre
            string nombres = currentJugador != null ? currentJugador.nombres : GameSession.CurrentNombres;
            nombresInput.text = nombres ?? "";
            nombresOriginal = nombresInput.text;

            // Filtrar por jugador actual
            var misPartidas = FilterPartidasMias();
            var misLogroXPartida = FilterLogroXPartidaMios(misPartidas);

            // Stats
            int jugadas = misPartidas.Count;
            int completadas = CountCompletadas(misPartidas);
            var distinctLogros = new HashSet<int>();
            foreach (var lp in misLogroXPartida) distinctLogros.Add(lp.idLogro);
            int totalCatalogo = GameSession.LogroIdByCodigo.Count;
            int puntosTotales = SumarPuntosDeLogros(distinctLogros);

            statsText.text =
                $"<b>Partidas jugadas:</b> {jugadas}\n" +
                $"<b>Partidas completadas:</b> {completadas}\n" +
                $"<b>Logros desbloqueados:</b> {distinctLogros.Count} / {totalCatalogo}\n" +
                $"<b>Puntos totales:</b> {puntosTotales}";

            // Historial
            ClearHistoryRows();
            if (misPartidas.Count == 0)
            {
                historyEmptyText.gameObject.SetActive(true);
                historyEmptyText.text = "Aun no jugaste ninguna partida.";
            }
            else
            {
                historyEmptyText.gameObject.SetActive(false);
                misPartidas.Sort((a, b) => string.Compare(b.fechaInicio, a.fechaInicio, StringComparison.Ordinal));
                int limit = Mathf.Min(misPartidas.Count, 20); // mostrar hasta 20
                for (int i = 0; i < limit; i++)
                {
                    var p = misPartidas[i];
                    int logrosDePartida = CountLogrosDeUnaPartida(p.idPartida, misLogroXPartida);
                    BuildHistoryRow(i, p, logrosDePartida);
                }
            }

            SetStatus(hadAnyError ? "Algunos datos no cargaron. Resultados parciales." : "");
        }

        private List<PartidaDto> FilterPartidasMias()
        {
            var list = new List<PartidaDto>();
            if (currentPartidas == null) return list;
            int mio = GameSession.CurrentJugadorId;
            foreach (var p in currentPartidas)
                if (p.idJugador == mio) list.Add(p);
            return list;
        }

        private List<LogroXPartidaDto> FilterLogroXPartidaMios(List<PartidaDto> misPartidas)
        {
            var ids = new HashSet<int>();
            foreach (var p in misPartidas) ids.Add(p.idPartida);
            var list = new List<LogroXPartidaDto>();
            if (currentLogroXPartida == null) return list;
            foreach (var lp in currentLogroXPartida)
                if (ids.Contains(lp.idPartida)) list.Add(lp);
            return list;
        }

        private static int CountCompletadas(List<PartidaDto> partidas)
        {
            int n = 0;
            foreach (var p in partidas) if (p.estado == 1) n++;
            return n;
        }

        private static int CountLogrosDeUnaPartida(int idPartida, List<LogroXPartidaDto> logroxpartida)
        {
            int n = 0;
            foreach (var lp in logroxpartida) if (lp.idPartida == idPartida) n++;
            return n;
        }

        private static int SumarPuntosDeLogros(HashSet<int> idsLogros)
        {
            int total = 0;
            foreach (var id in idsLogros)
            {
                if (GameSession.LogroByIdCache.TryGetValue(id, out var l))
                    total += l.puntos;
            }
            return total;
        }

        // ---------- name edit ----------

        private void OnEditClicked()
        {
            if (!editing) BeginEditMode();
            else SaveEdit();
        }

        private void BeginEditMode()
        {
            if (currentJugador == null) return;
            editing = true;
            nombresOriginal = nombresInput.text;
            nombresInput.interactable = true;
            EventSystem.current?.SetSelectedGameObject(nombresInput.gameObject);
            editButtonLabel.text = "Guardar";
        }

        private void EndEditMode()
        {
            editing = false;
            nombresInput.interactable = false;
            if (editButtonLabel != null) editButtonLabel.text = "Editar";
        }

        private void SaveEdit()
        {
            if (currentJugador == null) { EndEditMode(); return; }
            string nuevo = (nombresInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(nuevo)) { SetStatus("El nombre no puede estar vacio."); nombresInput.text = nombresOriginal; return; }
            if (nuevo.Length > 50) nuevo = nuevo.Substring(0, 50);
            if (nuevo == nombresOriginal) { EndEditMode(); return; }

            currentJugador.nombres = nuevo;
            editButton.interactable = false;
            SetStatus("Guardando...");

            ApiClient.Instance.UpdateJugador(currentJugador,
                () =>
                {
                    GameSession.UpdateNombres(nuevo);
                    nombresOriginal = nuevo;
                    EndEditMode();
                    editButton.interactable = true;
                    SetStatus("Guardado.");
                },
                err =>
                {
                    Debug.LogWarning($"[ProfileOverlay] UpdateJugador err: {err}");
                    nombresInput.text = nombresOriginal;
                    EndEditMode();
                    editButton.interactable = true;
                    SetStatus("No se pudo guardar el cambio.");
                });
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg ?? "";
        }

        // ---------- BUILD UI ----------

        private void Build()
        {
            BuildCanvas();
            BuildHeader();
            BuildAccountSection();
            BuildStatsSection();
            BuildHistorySection();
            BuildBottomButtons();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Profile_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220; // por encima de SettingsOverlay (210)

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
            bgImage = bgGo.AddComponent<Image>();
            bgImage.color = bgColor;

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panel = panelGo.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero; panel.offsetMax = Vector2.zero;
        }

        private void BuildHeader()
        {
            var title = MakeText(panel, "Title", "PERFIL", 64, FontStyle.Bold, titleColor,
                new Vector2(0, 420f), new Vector2(900f, 90f));
            var sh = title.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(3f, -4f);

            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(panel, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(280f, 2f);
            lineRT.anchoredPosition = new Vector2(0, 365f);
            lineGo.AddComponent<Image>().color = accentColor;
        }

        private void BuildAccountSection()
        {
            // Section title
            var sec = MakeText(panel, "Sec_Cuenta", "TU CUENTA", 22, FontStyle.Bold, accentColor,
                new Vector2(-460f, 290f), new Vector2(360f, 30f));
            sec.alignment = TextAnchor.MiddleLeft;

            // Label "Nombre:"
            var lbl = MakeText(panel, "LblNombre", "Nombre:", 20, FontStyle.Normal, labelColor,
                new Vector2(-560f, 240f), new Vector2(160f, 30f));
            lbl.alignment = TextAnchor.MiddleRight;

            // InputField
            var inputGo = new GameObject("NombresInput", typeof(RectTransform));
            var inputRT = inputGo.GetComponent<RectTransform>();
            inputRT.SetParent(panel, false);
            inputRT.anchorMin = inputRT.anchorMax = new Vector2(0.5f, 0.5f);
            inputRT.pivot = new Vector2(0.5f, 0.5f);
            inputRT.sizeDelta = new Vector2(380f, 38f);
            inputRT.anchoredPosition = new Vector2(-260f, 240f);
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.12f, 0.10f, 0.10f, 0.9f);

            var textChildGo = new GameObject("Text", typeof(RectTransform));
            var textChildRT = textChildGo.GetComponent<RectTransform>();
            textChildRT.SetParent(inputRT, false);
            textChildRT.anchorMin = Vector2.zero; textChildRT.anchorMax = Vector2.one;
            textChildRT.offsetMin = new Vector2(10f, 4f); textChildRT.offsetMax = new Vector2(-10f, -4f);
            var textChild = textChildGo.AddComponent<Text>();
            textChild.font = GetDefaultFont();
            textChild.fontSize = 18;
            textChild.color = textColor;
            textChild.alignment = TextAnchor.MiddleLeft;
            textChild.supportRichText = false;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            var phRT = placeholderGo.GetComponent<RectTransform>();
            phRT.SetParent(inputRT, false);
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(10f, 4f); phRT.offsetMax = new Vector2(-10f, -4f);
            var ph = placeholderGo.AddComponent<Text>();
            ph.font = GetDefaultFont();
            ph.fontSize = 18;
            ph.color = new Color(textColor.r, textColor.g, textColor.b, 0.4f);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = "tu nombre";

            nombresInput = inputGo.AddComponent<InputField>();
            nombresInput.targetGraphic = inputBg;
            nombresInput.textComponent = textChild;
            nombresInput.placeholder = ph;
            nombresInput.characterLimit = 50;
            nombresInput.lineType = InputField.LineType.SingleLine;
            nombresInput.interactable = false;

            // Boton Editar / Guardar
            editButton = MakeButton(panel, "BtnEdit", "Editar",
                new Vector2(40f, 240f), new Vector2(140f, 40f), OnEditClicked, out editButtonLabel);
        }

        private void BuildStatsSection()
        {
            var sec = MakeText(panel, "Sec_Stats", "ESTADISTICAS", 22, FontStyle.Bold, accentColor,
                new Vector2(-460f, 175f), new Vector2(360f, 30f));
            sec.alignment = TextAnchor.MiddleLeft;

            statsText = MakeText(panel, "StatsText", "", 20, FontStyle.Normal, textColor,
                new Vector2(-300f, 110f), new Vector2(700f, 130f));
            statsText.alignment = TextAnchor.UpperLeft;
            statsText.supportRichText = true;
        }

        private void BuildHistorySection()
        {
            var sec = MakeText(panel, "Sec_Hist", "HISTORIAL DE PARTIDAS", 22, FontStyle.Bold, accentColor,
                new Vector2(-460f, 0f), new Vector2(500f, 30f));
            sec.alignment = TextAnchor.MiddleLeft;

            // Header de columnas
            var header = MakeText(panel, "HistHeader",
                "<b>#</b>     <b>Fecha</b>                     <b>Estado</b>           <b>Tiempo</b>      <b>Logros</b>",
                16, FontStyle.Normal, labelColor,
                new Vector2(0f, -40f), new Vector2(1100f, 24f));
            header.alignment = TextAnchor.MiddleLeft;
            header.supportRichText = true;

            // Scroll view
            var scrollGo = new GameObject("HistoryScroll", typeof(RectTransform));
            var scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.SetParent(panel, false);
            scrollRT.anchorMin = scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);
            scrollRT.sizeDelta = new Vector2(1100f, 240f);
            scrollRT.anchoredPosition = new Vector2(0f, -180f);

            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.25f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.SetParent(scrollRT, false);
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0);
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            historyContent = contentGo.GetComponent<RectTransform>();
            historyContent.SetParent(viewportRT, false);
            historyContent.anchorMin = new Vector2(0f, 1f);
            historyContent.anchorMax = new Vector2(1f, 1f);
            historyContent.pivot = new Vector2(0.5f, 1f);
            historyContent.sizeDelta = new Vector2(0f, 0f);
            historyContent.anchoredPosition = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 0f;
            vlg.padding = new RectOffset(8, 8, 4, 4);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRT;
            scroll.content = historyContent;

            historyEmptyText = MakeText(scrollRT, "Empty", "", 18, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(1000f, 30f));
        }

        private void ClearHistoryRows()
        {
            if (historyContent == null) return;
            for (int i = historyContent.childCount - 1; i >= 0; i--)
                Destroy(historyContent.GetChild(i).gameObject);
        }

        private void BuildHistoryRow(int index, PartidaDto p, int logrosCount)
        {
            var rowGo = new GameObject("Row_" + index, typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(historyContent, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 30f;
            var bg = rowGo.AddComponent<Image>();
            bg.color = (index % 2 == 0) ? rowEvenColor : rowOddColor;
            bg.raycastTarget = false;

            string fecha = FormatFecha(p.fechaInicio);
            string estado = EstadoLabel(p.estado);
            string tiempo = FormatTiempo(p.tiempoSegundos);
            string line = $"<b>#{p.idPartida:D3}</b>   {fecha}   <color=#{ColorUtility.ToHtmlStringRGB(EstadoColor(p.estado))}>{estado,-12}</color>   {tiempo}   {logrosCount} logros";

            var t = rowGo.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.fontSize = 16;
            t.color = textColor;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.text = line;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static string FormatFecha(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "—";
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return iso;
        }

        private static string EstadoLabel(byte estado)
            => estado switch { 1 => "Completada", 2 => "Abandonada", _ => "EnCurso" };

        private Color EstadoColor(byte estado)
            => estado switch { 1 => rowCompletadaColor, 2 => rowAbandonadaColor, _ => rowEnCursoColor };

        private static string FormatTiempo(int segundos)
        {
            int h = segundos / 3600;
            int m = (segundos % 3600) / 60;
            int s = segundos % 60;
            return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }

        private void BuildBottomButtons()
        {
            statusText = MakeText(panel, "Status", "", 16, FontStyle.Italic,
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.85f),
                new Vector2(0f, -350f), new Vector2(900f, 24f));

            MakeButton(panel, "Btn_Cerrar", "Cerrar",
                new Vector2(0f, -400f), new Vector2(280f, 48f), Close, out _);
        }

        // ---------- helpers (mismo estilo que SettingsOverlay) ----------

        private Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Action onClick, out Text labelTextOut)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            go.AddComponent<Image>().color = buttonNormalColor;
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.normalColor = buttonNormalColor;
            c.highlightedColor = buttonHoverColor;
            c.pressedColor = buttonPressedColor;
            c.selectedColor = buttonHoverColor;
            c.fadeDuration = 0.1f;
            btn.colors = c;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            labelTextOut = lblGo.AddComponent<Text>();
            labelTextOut.font = GetDefaultFont();
            labelTextOut.text = label;
            labelTextOut.alignment = TextAnchor.MiddleCenter;
            labelTextOut.fontSize = 16;
            labelTextOut.fontStyle = FontStyle.Bold;
            labelTextOut.color = textColor;
            labelTextOut.raycastTarget = false;
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
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
