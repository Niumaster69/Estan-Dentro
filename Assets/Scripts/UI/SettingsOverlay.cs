using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.Breathing;
using EstanDentro.Network;

namespace EstanDentro.UI
{
    public class SettingsOverlay : MonoBehaviour
    {
        private static SettingsOverlay instance;

        public enum SettingsTab { Audio, Video, Controles, Mic, Perfil }

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color panelColor = new Color(0.06f, 0.05f, 0.05f, 0.97f);
        [SerializeField] private Color panelBorderColor = new Color(0.85f, 0.7f, 0.28f, 0.55f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color labelColor = new Color(0.92f, 0.89f, 0.83f, 0.7f);
        [SerializeField] private Color sliderBgColor = new Color(0.12f, 0.10f, 0.10f, 0.9f);
        [SerializeField] private Color sliderFillColor = new Color(0.85f, 0.7f, 0.28f, 0.95f);
        [SerializeField] private Color sliderHandleColor = new Color(0.95f, 0.93f, 0.85f, 1f);
        [SerializeField] private Color buttonNormalColor = new Color(0.10f, 0.10f, 0.10f, 0.9f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color rowSelectedColor = new Color(0.85f, 0.7f, 0.28f, 0.18f);
        [SerializeField] private Color descColor = new Color(0.85f, 0.7f, 0.28f, 0.85f);

        [Header("Layout (fullscreen, sin caja modal)")]
        [SerializeField] private float contentWidth = 1100f;
        [SerializeField] private float rowHeight = 64f;
        [SerializeField] private float labelColWidth = 320f;
        [SerializeField] private float controlColWidth = 500f;
        [SerializeField] private float valueColWidth = 110f;
        [SerializeField] private float rowStaggerDelay = 0.04f;
        [SerializeField, Tooltip("Y donde empieza el titulo (positivo = arriba).")]
        private float titleY = 400f;
        [SerializeField, Tooltip("Y de la fila de tabs (entre titulo y contenido).")]
        private float tabsY = 270f;
        [SerializeField, Tooltip("Y donde empieza la primera fila del contenido del tab activo.")]
        private float firstRowY = 130f;
        [SerializeField, Tooltip("Y de la descripcion del control activo.")]
        private float descriptionY = -290f;
        [SerializeField, Tooltip("Y de los botones inferiores.")]
        private float bottomButtonsY = -380f;

        [Header("Tabs")]
        [SerializeField] private float tabWidth = 180f;
        [SerializeField] private float tabHeight = 100f;
        [SerializeField] private float tabSpacing = 14f;
        [SerializeField] private float tabCircleSize = 56f;
        [SerializeField, Tooltip("Circulo del tab inactivo (relleno oscuro casi transparente).")]
        private Color tabCircleInactiveColor = new Color(0.10f, 0.09f, 0.09f, 0.85f);
        [SerializeField, Tooltip("Circulo del tab cuando esta seleccionado por mouse/teclado pero no es el activo.")]
        private Color tabCircleHoverColor = new Color(0.20f, 0.16f, 0.10f, 0.9f);
        [SerializeField, Tooltip("Circulo del tab activo (relleno ambar pleno).")]
        private Color tabCircleActiveColor = new Color(0.85f, 0.7f, 0.28f, 1f);
        [SerializeField] private Color tabIconActiveColor = new Color(0.05f, 0.04f, 0.02f, 1f);
        [SerializeField] private Color tabIconHoverColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color tabIconInactiveColor = new Color(0.65f, 0.6f, 0.55f, 0.85f);
        [SerializeField] private Color tabLabelActiveColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color tabLabelHoverColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color tabLabelInactiveColor = new Color(0.6f, 0.55f, 0.5f, 0.7f);
        [SerializeField] private Color tabUnderlineColor = new Color(0.85f, 0.7f, 0.28f, 1f);

        // ---------- estado runtime ----------

        private struct Row
        {
            public GameObject rowGo;
            public Image bg;
            public Selectable control;
            public Text label;
            public Text valueLabel;
            public string description;
            public SettingsTab tab;
        }

        private struct TabButton
        {
            public SettingsTab tab;
            public Button button;
            public Image circleBg;
            public Text iconText;
            public Text labelText;
            public RectTransform underline;
        }

        private List<Row> rows = new List<Row>();
        private List<TabButton> tabs = new List<TabButton>();
        private SettingsTab currentTab = SettingsTab.Audio;
        private Canvas canvas;
        private Image bgImage;
        private RectTransform panel;
        private Slider volumeSlider, musicSlider, sfxSlider, cinematicSlider, voiceSlider;
        private Slider mouseSlider, gamepadSlider, brightnessSlider;
        private Text volumeValueText, musicValueText, sfxValueText, cinematicValueText, voiceValueText;
        private Text mouseValueText, gamepadValueText, brightnessValueText;
        private Toggle invertYToggle;
        private Image invertYBgImg;
        private RectTransform invertYIndRT;
        private const float SWITCH_OFF_X = 14f;
        private const float SWITCH_ON_X = 50f;

        // ---------- Perfil tab (embedded) ----------
        private GameObject perfilContent;
        private InputField perfilNombresInput;
        private Button perfilEditButton;
        private Text perfilEditButtonLabel;
        private Text perfilStatsText;
        private RectTransform perfilHistoryContent;
        private Text perfilHistoryEmptyText;
        private Text perfilStatusText;
        private bool perfilEditing;
        private string perfilNombresOriginal;
        private JugadorDto perfilCurrentJugador;
        private PartidaDto[] perfilCurrentPartidas;
        private LogroXPartidaDto[] perfilCurrentLogroXPartida;
        private int perfilPendingApiCalls;
        private bool perfilHadAnyError;
        // Colores para historial (mismo lenguaje que ProfileOverlay)
        private static readonly Color PERFIL_ROW_EVEN = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color PERFIL_ROW_ODD = new Color(1f, 1f, 1f, 0f);
        private static readonly Color PERFIL_COMPLETADA = new Color(0.55f, 0.85f, 0.45f, 1f);
        private static readonly Color PERFIL_ABANDONADA = new Color(0.85f, 0.45f, 0.45f, 1f);
        private static readonly Color PERFIL_ENCURSO = new Color(0.85f, 0.7f, 0.28f, 1f);
        private Button micTestButton;
        private Text micTestStatusText;
        private RectTransform micLevelBarFillRT;
        private float micLevelBarBaseWidth;
        private bool micTestActive;
        private Text descriptionText;
        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;
        private System.Action onCloseCallback;
        private static readonly Color SELECTED_LABEL_COLOR = new Color(0.96f, 0.85f, 0.42f, 1f);

        // ---------- public API ----------

        public static void Open(System.Action onClose = null, bool transparentBg = false)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;
            instance.LoadValuesIntoUI();
            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.prevCursorLock = Cursor.lockState;
            instance.prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (instance.bgImage != null)
                instance.bgImage.color = transparentBg ? new Color(0, 0, 0, 0) : instance.bgColor;
            instance.canvas.gameObject.SetActive(true);
            instance.consumeInputThisFrame = true;
            instance.onCloseCallback = onClose;
            OverlayBlocker.Register();
            // Siempre arrancar en tab Audio al abrir.
            instance.SwitchTab(SettingsTab.Audio);
            instance.StartCoroutine(instance.AnimateRowsIn());
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.gameObject.activeSelf) return;
            instance.StopMicTest();
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
            var go = new GameObject("__SettingsOverlay");
            instance = go.AddComponent<SettingsOverlay>();
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
            UpdateSelectionHighlight();
            UpdateMicTestBar();

            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }
            // No cerrar con ESC mientras editas el nombre del perfil
            if (perfilEditing) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && kb.escapeKey.wasPressedThisFrame)
                        || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (dismiss) Close();
        }

        // ---------- selection highlight + descripcion ----------

        private void UpdateSelectionHighlight()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            float lerp = Time.unscaledDeltaTime * 12f;

            // ----- Tabs: 3 estados (active / hover / inactive) -----
            for (int i = 0; i < tabs.Count; i++)
            {
                var tb = tabs[i];
                bool isActive = tb.tab == currentTab;
                bool isHover = !isActive && selected != null && tb.button != null && selected == tb.button.gameObject;

                Color targetCircle = isActive ? tabCircleActiveColor : (isHover ? tabCircleHoverColor : tabCircleInactiveColor);
                Color targetIcon = isActive ? tabIconActiveColor : (isHover ? tabIconHoverColor : tabIconInactiveColor);
                Color targetLabel = isActive ? tabLabelActiveColor : (isHover ? tabLabelHoverColor : tabLabelInactiveColor);

                if (tb.circleBg != null) tb.circleBg.color = Color.Lerp(tb.circleBg.color, targetCircle, lerp);
                if (tb.iconText != null) tb.iconText.color = Color.Lerp(tb.iconText.color, targetIcon, lerp);
                if (tb.labelText != null) tb.labelText.color = Color.Lerp(tb.labelText.color, targetLabel, lerp);

                if (tb.underline != null && tb.underline.gameObject.activeSelf != isActive)
                    tb.underline.gameObject.SetActive(isActive);
            }

            // ----- Rows -----
            string activeDesc = "";
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool isSel = selected != null && row.control != null && selected == row.control.gameObject;

                if (row.bg != null)
                {
                    Color target = isSel ? rowSelectedColor : new Color(rowSelectedColor.r, rowSelectedColor.g, rowSelectedColor.b, 0f);
                    row.bg.color = Color.Lerp(row.bg.color, target, lerp);
                }

                if (row.label != null)
                    row.label.color = isSel ? SELECTED_LABEL_COLOR : labelColor;
                if (row.valueLabel != null)
                    row.valueLabel.color = isSel ? SELECTED_LABEL_COLOR : textColor;

                if (isSel) activeDesc = row.description ?? "";
            }

            if (descriptionText != null)
                descriptionText.text = activeDesc;
        }

        // ---------- mic test ----------

        private void UpdateMicTestBar()
        {
            if (!micTestActive || micLevelBarFillRT == null || BreathingInputProvider.Instance == null) return;
            float rms = BreathingInputProvider.Instance.CurrentRms;
            float t = Mathf.Clamp01(rms / 0.04f);
            micLevelBarFillRT.sizeDelta = new Vector2(micLevelBarBaseWidth * t, micLevelBarFillRT.sizeDelta.y);
            if (micTestStatusText != null)
                micTestStatusText.text = $"Probando... RMS: {rms:F4}   (habla / sopla al mic)";
        }

        private void StartMicTest()
        {
            var prov = BreathingInputProvider.Instance;
            if (prov == null)
            {
                if (micTestStatusText != null)
                    micTestStatusText.text = "No hay BreathingInputProvider en esta escena. Solo funciona desde el juego.";
                return;
            }
            if (!prov.MicAvailable)
            {
                if (micTestStatusText != null)
                    micTestStatusText.text = "Windows no detecta ningun microfono.";
                return;
            }
            if (!prov.TryStartMic())
            {
                if (micTestStatusText != null)
                    micTestStatusText.text = "No pude abrir el microfono. Cerra otras apps que lo usen.";
                return;
            }
            micTestActive = true;
            if (micTestButton != null)
            {
                var t = micTestButton.GetComponentInChildren<Text>();
                if (t != null) t.text = "Detener prueba";
            }
        }

        private void StopMicTest()
        {
            if (!micTestActive) return;
            micTestActive = false;
            if (BreathingInputProvider.Instance != null)
                BreathingInputProvider.Instance.StopMic();
            if (micLevelBarFillRT != null)
                micLevelBarFillRT.sizeDelta = new Vector2(0f, micLevelBarFillRT.sizeDelta.y);
            if (micTestStatusText != null)
                micTestStatusText.text = "";
            if (micTestButton != null)
            {
                var t = micTestButton.GetComponentInChildren<Text>();
                if (t != null) t.text = "Probar microfono";
            }
        }

        private void ToggleMicTest()
        {
            if (micTestActive) StopMicTest();
            else StartMicTest();
        }

        // ---------- stagger ----------

        private IEnumerator AnimateRowsIn()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var rt = rows[i].rowGo.GetComponent<RectTransform>();
                if (rt == null) continue;
                rt.localScale = new Vector3(0.95f, 0.95f, 1f);
                var canvasGroup = rows[i].rowGo.GetComponent<CanvasGroup>();
                if (canvasGroup != null) canvasGroup.alpha = 0f;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                yield return new WaitForSecondsRealtime(rowStaggerDelay);
                var rt = rows[i].rowGo.GetComponent<RectTransform>();
                var canvasGroup = rows[i].rowGo.GetComponent<CanvasGroup>();
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / 0.25f);
                    if (rt != null) rt.localScale = Vector3.Lerp(new Vector3(0.95f, 0.95f, 1f), Vector3.one, p);
                    if (canvasGroup != null) canvasGroup.alpha = p;
                    yield return null;
                }
                if (rt != null) rt.localScale = Vector3.one;
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }
        }

        // ---------- handlers ----------

        private void LoadValuesIntoUI()
        {
            volumeSlider.SetValueWithoutNotify(Settings.MasterVolume);
            musicSlider.SetValueWithoutNotify(Settings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(Settings.SfxVolume);
            cinematicSlider.SetValueWithoutNotify(Settings.CinematicVolume);
            voiceSlider.SetValueWithoutNotify(Settings.VoiceVolume);
            mouseSlider.SetValueWithoutNotify(Settings.MouseSensitivity);
            gamepadSlider.SetValueWithoutNotify(Settings.GamepadSensitivity);
            brightnessSlider.SetValueWithoutNotify(Settings.Brightness);
            invertYToggle.SetIsOnWithoutNotify(Settings.InvertY);
            UpdateSwitchVisual(invertYToggle, invertYBgImg, invertYIndRT);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            volumeValueText.text = Mathf.RoundToInt(Settings.MasterVolume * 100f) + "%";
            musicValueText.text = Mathf.RoundToInt(Settings.MusicVolume * 100f) + "%";
            sfxValueText.text = Mathf.RoundToInt(Settings.SfxVolume * 100f) + "%";
            cinematicValueText.text = Mathf.RoundToInt(Settings.CinematicVolume * 100f) + "%";
            voiceValueText.text = Mathf.RoundToInt(Settings.VoiceVolume * 100f) + "%";
            mouseValueText.text = Settings.MouseSensitivity.ToString("F2");
            gamepadValueText.text = Mathf.RoundToInt(Settings.GamepadSensitivity).ToString();
            brightnessValueText.text = (Settings.Brightness * 100f).ToString("F0") + "%";
        }

        private void OnVolumeChanged(float v) { Settings.MasterVolume = v; UpdateValueLabels(); }
        private void OnMusicChanged(float v) { Settings.MusicVolume = v; UpdateValueLabels(); }
        private void OnSfxChanged(float v) { Settings.SfxVolume = v; UpdateValueLabels(); }
        private void OnCinematicChanged(float v) { Settings.CinematicVolume = v; UpdateValueLabels(); }
        private void OnVoiceChanged(float v) { Settings.VoiceVolume = v; UpdateValueLabels(); }
        private void OnMouseChanged(float v) { Settings.MouseSensitivity = v; UpdateValueLabels(); ApplyToActivePlayer(); }
        private void OnGamepadChanged(float v) { Settings.GamepadSensitivity = v; UpdateValueLabels(); ApplyToActivePlayer(); }
        private void OnBrightnessChanged(float v) { Settings.Brightness = v; UpdateValueLabels(); }
        private void OnInvertYChanged(bool v) { Settings.InvertY = v; ApplyToActivePlayer(); }

        private void OnRecalibrateClicked()
        {
            BreathingInputProvider.ClearStoredCalibration();
            var mc = FindFirstObjectByType<MicCalibration>();
            if (mc == null)
            {
                if (descriptionText != null) descriptionText.text = "Calibracion borrada. Se mostrara al volver al juego.";
                return;
            }
            onCloseCallback = null;
            Close();
            if (PauseMenuHandler.Instance != null && PauseMenuHandler.Instance.IsOpen)
                PauseMenuHandler.Instance.ClosePause();
            mc.RestartCalibration();
        }

        private void OnResetDefaultsClicked()
        {
            Settings.ResetToDefaults();
            LoadValuesIntoUI();
            ApplyToActivePlayer();
        }

        private void ApplyToActivePlayer()
        {
            var pc = FindFirstObjectByType<EstanDentro.Player.PlayerController>();
            if (pc != null) pc.ApplySettings();
        }

        // ---------- BUILD UI ----------

        private void Build()
        {
            BuildCanvas();
            BuildPanel();
            BuildTabs();
            BuildRows();
            BuildPerfilContent();
            BuildBottomButtons();
            SwitchTab(SettingsTab.Audio);
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Settings_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

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
        }

        private void BuildPanel()
        {
            // Panel transparente fullscreen (no caja modal). Solo contenedor para los hijos.
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panel = panelGo.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            // Sin Image background = transparente, se ve el fondo del menu detras

            // Title arriba con sombra para legibilidad
            var titleTxt = MakeText(panel, "Title", "AJUSTES", 64, FontStyle.Bold, titleColor,
                new Vector2(0, titleY), new Vector2(900f, 90f));
            var titleShadow = titleTxt.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(3f, -4f);

            // Linea decorativa fina debajo del titulo
            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(panel, false);
            lineRT.anchorMin = new Vector2(0.5f, 0.5f);
            lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(280f, 2f);
            lineRT.anchoredPosition = new Vector2(0, titleY - 60f);
            lineGo.AddComponent<Image>().color = panelBorderColor;

            // Description text abajo del contenido
            descriptionText = MakeText(panel, "Description", "", 18, FontStyle.Italic, descColor,
                new Vector2(0, descriptionY), new Vector2(1000f, 40f));
            var descShadow = descriptionText.gameObject.AddComponent<Shadow>();
            descShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            descShadow.effectDistance = new Vector2(1f, -1f);
        }

        private void BuildRows()
        {
            // Cada tab tiene su propia stack vertical empezando desde firstRowY.
            // Se construyen todas en sus posiciones; SwitchTab() activa/desactiva por tab.

            // ===== TAB AUDIO =====
            float yAudio = firstRowY;
            volumeSlider = AddSliderRow(panel, ref yAudio, "Audio general",
                "Volumen general (master) del juego.",
                0f, 1f, OnVolumeChanged, out volumeValueText, SettingsTab.Audio);

            musicSlider = AddSliderRow(panel, ref yAudio, "Musica",
                "Pistas de musica del menu y ambiente musical.",
                0f, 1f, OnMusicChanged, out musicValueText, SettingsTab.Audio);

            cinematicSlider = AddSliderRow(panel, ref yAudio, "Cinematicas",
                "Audio de las cinematicas (intro, flashback, outro).",
                0f, 1f, OnCinematicChanged, out cinematicValueText, SettingsTab.Audio);

            sfxSlider = AddSliderRow(panel, ref yAudio, "Efectos",
                "Sonidos de interaccion (papel, mecanismos, ambient stingers).",
                0f, 1f, OnSfxChanged, out sfxValueText, SettingsTab.Audio);

            voiceSlider = AddSliderRow(panel, ref yAudio, "Voces",
                "Susurros y dialogos en el juego.",
                0f, 1f, OnVoiceChanged, out voiceValueText, SettingsTab.Audio);

            // ===== TAB VIDEO =====
            float yVideo = firstRowY;
            brightnessSlider = AddSliderRow(panel, ref yVideo, "Brillo",
                "Brillo general del juego. Util si tu monitor es muy oscuro.",
                Settings.BRIGHTNESS_MIN, Settings.BRIGHTNESS_MAX, OnBrightnessChanged, out brightnessValueText,
                SettingsTab.Video);

            // ===== TAB CONTROLES =====
            float yCtrl = firstRowY;
            mouseSlider = AddSliderRow(panel, ref yCtrl, "Sensibilidad mouse",
                "Velocidad de la camara con el mouse. Subir para girar mas rapido.",
                Settings.MOUSE_SENS_MIN, Settings.MOUSE_SENS_MAX, OnMouseChanged, out mouseValueText,
                SettingsTab.Controles);

            gamepadSlider = AddSliderRow(panel, ref yCtrl, "Sensibilidad mando",
                "Velocidad de la camara con el stick derecho del mando.",
                Settings.GAMEPAD_SENS_MIN, Settings.GAMEPAD_SENS_MAX, OnGamepadChanged, out gamepadValueText,
                SettingsTab.Controles);

            invertYToggle = AddToggleRow(panel, ref yCtrl, "Invertir eje Y",
                "Si esta activado, mover el mouse hacia arriba mira hacia abajo.",
                OnInvertYChanged, SettingsTab.Controles);

            // ===== TAB MIC =====
            float yMic = firstRowY;
            AddMicTestRow(panel, ref yMic,
                "Prueba el microfono. La barra muestra el nivel de audio en tiempo real.",
                SettingsTab.Mic);
            // Boton de re-calibrar dentro del propio tab MIC (antes estaba abajo).
            AddRecalibrateRow(panel, ref yMic, SettingsTab.Mic);
        }

        private void AddRecalibrateRow(Transform parent, ref float y, SettingsTab tab)
        {
            // Una "row" que solo contiene un boton centrado, alineado al estilo de las otras rows.
            var rowGo = new GameObject("Row_Recalibrate", typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(contentWidth, rowHeight - 6f);
            rt.anchoredPosition = new Vector2(0f, y);
            rowGo.AddComponent<CanvasGroup>();
            var rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(rowSelectedColor.r, rowSelectedColor.g, rowSelectedColor.b, 0f);
            rowBg.raycastTarget = false;

            var btn = MakeButton(rowGo.transform, "Btn_Recalibrate", "Re-calibrar microfono",
                Vector2.zero, new Vector2(280f, 40f), OnRecalibrateClicked);

            // Registramos esta row para que SwitchTab la oculte cuando no estes en MIC.
            // No tiene control selectable separado — el btn ES el control.
            rows.Add(new Row {
                rowGo = rowGo,
                bg = rowBg,
                control = btn,
                label = null,
                description = "Vuelve a calibrar el microfono desde cero (ruido ambiental + nivel de exhalacion).",
                tab = tab
            });

            y -= rowHeight;
        }

        private void BuildBottomButtons()
        {
            // ----- Esquina top-right: solo icono de cerrar (Perfil se movio al tab PERFIL) -----
            BuildCornerIconButton("Btn_Close", "✕", new Vector2(-45f, -45f), 60f, () => Close());

            // ----- Bottom-right: botón discreto de "Restaurar defaults" (sin caja) -----
            BuildDiscreteTextButton("Btn_Reset", "restaurar defaults",
                new Vector2(440f, bottomButtonsY), OnResetDefaultsClicked);
        }

        private void BuildCornerIconButton(string name, string iconChar, Vector2 anchoredOffset, float size, System.Action onClick)
        {
            // Anchor top-right del panel; anchoredOffset es relativo a esa esquina (X negativo = entrar al panel).
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = anchoredOffset;

            // Caja redonda (mismo estilo que tabs)
            var img = go.AddComponent<Image>();
            img.sprite = CreateCircleSprite(96);
            img.color = tabCircleInactiveColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.4f, 1.3f, 1.05f, 1f);
            c.pressedColor = new Color(0.85f, 0.7f, 0.28f, 1f);
            c.selectedColor = new Color(1.2f, 1.15f, 0.95f, 1f);
            c.fadeDuration = 0.12f;
            btn.colors = c;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Icono Unicode (font size proporcional al boton)
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.SetParent(go.transform, false);
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
            var iconTxt = iconGo.AddComponent<Text>();
            iconTxt.font = GetDefaultFont();
            iconTxt.text = iconChar;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.fontSize = Mathf.RoundToInt(size * 0.55f);
            iconTxt.fontStyle = FontStyle.Bold;
            iconTxt.color = tabIconHoverColor;
            iconTxt.raycastTarget = false;
        }

        private void BuildDiscreteTextButton(string name, string label, Vector2 anchoredPos, System.Action onClick)
        {
            // Boton sin caja: el padre tiene una Image transparente para raycast,
            // y un Text como hijo (el targetGraphic del Button) que recibe el tint del hover.
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 28f);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.font = GetDefaultFont();
            lblTxt.text = label;
            lblTxt.alignment = TextAnchor.MiddleCenter;
            lblTxt.fontSize = 14;
            lblTxt.fontStyle = FontStyle.Italic;
            // Color base del texto: tinte cremoso semi-transparente.
            lblTxt.color = new Color(labelColor.r, labelColor.g, labelColor.b, 0.55f);
            lblTxt.raycastTarget = false;

            var btn = go.AddComponent<Button>();
            // El targetGraphic es el Text para que el tint del hover aplique al texto.
            btn.targetGraphic = lblTxt;
            var c = btn.colors;
            c.normalColor = Color.white; // multiplier 1: deja el color base tal cual
            c.highlightedColor = new Color(1.5f, 1.5f, 1.5f, 1.8f); // texto mas brillante
            c.pressedColor = new Color(0.9f, 0.7f, 0.3f, 1.8f);
            c.selectedColor = new Color(1.5f, 1.5f, 1.5f, 1.8f);
            c.fadeDuration = 0.12f;
            btn.colors = c;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        // ---------- tabs ----------

        private void BuildTabs()
        {
            // 5 tabs centrados con icono Unicode arriba y label abajo.
            // Total ancho = 5 * tabWidth + 4 * tabSpacing.
            const int N = 5;
            float total = N * tabWidth + (N - 1) * tabSpacing;
            float step = tabWidth + tabSpacing;
            float startX = -total * 0.5f + tabWidth * 0.5f;

            BuildTab(0, startX,             "♪", "AUDIO",     SettingsTab.Audio);
            BuildTab(1, startX + step,      "☀", "VIDEO",     SettingsTab.Video);
            BuildTab(2, startX + 2f * step, "◆", "CONTROLES", SettingsTab.Controles);
            BuildTab(3, startX + 3f * step, "◉", "MIC",       SettingsTab.Mic);
            BuildTab(4, startX + 4f * step, "☺", "PERFIL",    SettingsTab.Perfil);
        }

        private void BuildTab(int index, float xCenter, string iconChar, string labelText, SettingsTab tab,
            System.Action customOnClick = null)
        {
            var go = new GameObject("Tab_" + tab.ToString(), typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(panel, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(tabWidth, tabHeight);
            rt.anchoredPosition = new Vector2(xCenter, tabsY);

            // Boton transparente que cubre todo el tab (clickeable)
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            // El boton no usa color transitions — el hover lo manejamos manualmente en
            // UpdateSelectionHighlight para coordinarlo con el estado activo del tab.
            var btnColors = btn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = Color.white;
            btnColors.pressedColor = Color.white;
            btnColors.selectedColor = Color.white;
            btnColors.fadeDuration = 0f;
            btn.colors = btnColors;
            btn.transition = Selectable.Transition.None;
            var capturedTab = tab;
            var capturedOnClick = customOnClick;
            btn.onClick.AddListener(() => {
                if (capturedOnClick != null) capturedOnClick();
                else SwitchTab(capturedTab);
            });

            // Caja redonda detras del icono
            var circleGo = new GameObject("IconBg", typeof(RectTransform));
            var circleRT = circleGo.GetComponent<RectTransform>();
            circleRT.SetParent(go.transform, false);
            circleRT.anchorMin = new Vector2(0.5f, 0.5f);
            circleRT.anchorMax = new Vector2(0.5f, 0.5f);
            circleRT.pivot = new Vector2(0.5f, 0.5f);
            circleRT.sizeDelta = new Vector2(tabCircleSize, tabCircleSize);
            circleRT.anchoredPosition = new Vector2(0f, 18f);
            var circleImg = circleGo.AddComponent<Image>();
            circleImg.sprite = CreateCircleSprite(96);
            circleImg.color = tabCircleInactiveColor;
            circleImg.raycastTarget = false;

            // Icono Unicode encima del circulo
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.SetParent(go.transform, false);
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(tabCircleSize, tabCircleSize);
            iconRT.anchoredPosition = new Vector2(0f, 18f);
            var iconTxt = iconGo.AddComponent<Text>();
            iconTxt.font = GetDefaultFont();
            iconTxt.text = iconChar;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.fontSize = 28;
            iconTxt.fontStyle = FontStyle.Normal;
            iconTxt.color = tabIconInactiveColor;
            iconTxt.raycastTarget = false;

            // Label debajo del circulo
            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = new Vector2(0.5f, 0.5f);
            lblRT.anchorMax = new Vector2(0.5f, 0.5f);
            lblRT.pivot = new Vector2(0.5f, 0.5f);
            lblRT.sizeDelta = new Vector2(tabWidth, 22f);
            lblRT.anchoredPosition = new Vector2(0f, -28f);
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.font = GetDefaultFont();
            lblTxt.text = labelText;
            lblTxt.alignment = TextAnchor.MiddleCenter;
            lblTxt.fontSize = 14;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color = tabLabelInactiveColor;
            lblTxt.raycastTarget = false;

            // Underline (debajo del tab) — visible solo en tab activo
            var underGo = new GameObject("Underline", typeof(RectTransform));
            var underRT = underGo.GetComponent<RectTransform>();
            underRT.SetParent(go.transform, false);
            underRT.anchorMin = new Vector2(0.5f, 0.5f);
            underRT.anchorMax = new Vector2(0.5f, 0.5f);
            underRT.pivot = new Vector2(0.5f, 0.5f);
            underRT.sizeDelta = new Vector2(tabCircleSize + 8f, 2f);
            underRT.anchoredPosition = new Vector2(0f, -tabHeight * 0.5f + 2f);
            underGo.AddComponent<Image>().color = tabUnderlineColor;
            underGo.SetActive(false);

            tabs.Add(new TabButton {
                tab = tab,
                button = btn,
                circleBg = circleImg,
                iconText = iconTxt,
                labelText = lblTxt,
                underline = underRT
            });
        }

        private Sprite CreateCircleSprite(int size)
        {
            // Circulo lleno con borde suave (antialias por alpha en el borde).
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - d);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite roundedRectSpriteCache;

        private Sprite GetRoundedRectSprite()
        {
            // Sprite 9-sliced con esquinas redondeadas parcialmente. Cacheado para reusar.
            if (roundedRectSpriteCache != null) return roundedRectSpriteCache;

            // Textura cuadrada chica (32x32). Las esquinas tienen radius=8px (parcial).
            const int size = 32;
            const int radius = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distancia a la esquina mas cercana
                float dx = x < radius ? (radius - x) : (x >= size - radius ? x - (size - radius - 1) : 0);
                float dy = y < radius ? (radius - y) : (y >= size - radius ? y - (size - radius - 1) : 0);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = d > 0f ? Mathf.Clamp01(radius - d) : 1f;
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            // Border 9-slice: las esquinas se mantienen al tamaño del radius, el centro se estira.
            var spr = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            roundedRectSpriteCache = spr;
            return spr;
        }

        private void ApplyRounded(Image img)
        {
            // Helper para aplicar el sprite redondeado y configurar Image como Sliced.
            if (img == null) return;
            img.sprite = GetRoundedRectSprite();
            img.type = Image.Type.Sliced;
        }

        private void SwitchTab(SettingsTab newTab)
        {
            currentTab = newTab;

            // Activar/desactivar filas segun tab. Perfil es especial: no tiene rows
            // (su contenido es perfilContent que se activa aparte).
            Selectable firstOfTab = null;
            for (int i = 0; i < rows.Count; i++)
            {
                bool show = rows[i].tab == newTab;
                if (rows[i].rowGo != null) rows[i].rowGo.SetActive(show);
                if (show && firstOfTab == null) firstOfTab = rows[i].control;
            }

            // Activar/desactivar el contenido embebido del Perfil
            if (perfilContent != null)
                perfilContent.SetActive(newTab == SettingsTab.Perfil);

            if (newTab == SettingsTab.Perfil)
            {
                // Cargar datos cada vez que entras al tab (refresca info de cuenta y partidas)
                ResetPerfilUiState();
                LoadPerfilDataFromApi();
                if (perfilEditButton != null)
                    EventSystem.current?.SetSelectedGameObject(perfilEditButton.gameObject);
            }
            else if (firstOfTab != null)
            {
                EventSystem.current?.SetSelectedGameObject(firstOfTab.gameObject);
            }

            // El visual de los tabs (circle bg, icon color, label color, underline)
            // se actualiza cada frame en UpdateSelectionHighlight con Lerp.
        }

        // ---------- helpers de filas ----------

        private Slider AddSliderRow(Transform parent, ref float y, string labelText, string description,
            float min, float max, System.Action<float> onChanged, out Text valueText, SettingsTab tab)
        {
            var row = MakeRow(parent, y, labelText, description, tab, out var rowGo, out var rowBg, out var labelTxt);

            // Slider
            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            var sliderRT = sliderGo.GetComponent<RectTransform>();
            sliderRT.SetParent(rowGo.transform, false);
            sliderRT.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRT.pivot = new Vector2(0.5f, 0.5f);
            sliderRT.sizeDelta = new Vector2(controlColWidth - 20f, 22f);
            sliderRT.anchoredPosition = new Vector2(60f, 0f);

            var sBgGo = new GameObject("Bg", typeof(RectTransform));
            var sBgRT = sBgGo.GetComponent<RectTransform>();
            sBgRT.SetParent(sliderRT, false);
            sBgRT.anchorMin = Vector2.zero; sBgRT.anchorMax = Vector2.one;
            sBgRT.offsetMin = Vector2.zero; sBgRT.offsetMax = Vector2.zero;
            var sBgImg = sBgGo.AddComponent<Image>();
            sBgImg.color = sliderBgColor;
            ApplyRounded(sBgImg);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRT = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRT.SetParent(sliderRT, false);
            fillAreaRT.anchorMin = new Vector2(0f, 0f);
            fillAreaRT.anchorMax = new Vector2(1f, 1f);
            fillAreaRT.offsetMin = new Vector2(4f, 4f);
            fillAreaRT.offsetMax = new Vector2(-4f, -4f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            var fillRT = fillGo.GetComponent<RectTransform>();
            fillRT.SetParent(fillAreaRT, false);
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = sliderFillColor;
            ApplyRounded(fillImg);

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRT = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRT.SetParent(sliderRT, false);
            handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10f, 0f);
            handleAreaRT.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            var handleRT = handleGo.GetComponent<RectTransform>();
            handleRT.SetParent(handleAreaRT, false);
            // Handle perfectamente circular: anchor/pivot Y centrados (X lo sobrescribe Slider segun value).
            // sizeDelta cuadrado y preserveAspect=true evitan que el sprite se distorsione.
            handleRT.anchorMin = new Vector2(0.5f, 0.5f);
            handleRT.anchorMax = new Vector2(0.5f, 0.5f);
            handleRT.pivot = new Vector2(0.5f, 0.5f);
            handleRT.sizeDelta = new Vector2(32f, 32f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.sprite = CreateCircleSprite(128);
            handleImg.color = sliderHandleColor;
            handleImg.preserveAspect = true;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener((v) => onChanged?.Invoke(v));
            // Re-asegurar dimensiones del handle DESPUES de configurar Slider (que podria
            // resetear anchors al asignar handleRect). Mantiene el handle perfectamente cuadrado.
            handleRT.anchorMin = new Vector2(handleRT.anchorMin.x, 0.5f);
            handleRT.anchorMax = new Vector2(handleRT.anchorMax.x, 0.5f);
            handleRT.pivot = new Vector2(0.5f, 0.5f);
            handleRT.sizeDelta = new Vector2(32f, 32f);

            // Value label (a la derecha)
            // Posicion: 30px a la derecha del borde derecho del slider, fuera del fill
            // (sino al 100% el fill se metia sobre el texto).
            float sliderRightEdge = 60f + (controlColWidth - 20f) * 0.5f;        // = 300
            float valueCenterX = sliderRightEdge + 30f + valueColWidth * 0.5f;   // = 385
            valueText = MakeText(rowGo.transform, "Value", "", 18, FontStyle.Bold, textColor,
                new Vector2(valueCenterX, 0f), new Vector2(valueColWidth, 30f));
            valueText.alignment = TextAnchor.MiddleLeft; // % pegado al slider, no al borde derecho del rect

            row.control = slider;
            row.valueLabel = valueText;
            row.label = labelTxt;
            rows.Add(row);

            y -= rowHeight;
            return slider;
        }

        private Toggle AddToggleRow(Transform parent, ref float y, string labelText, string description,
            System.Action<bool> onChanged, SettingsTab tab)
        {
            var row = MakeRow(parent, y, labelText, description, tab, out var rowGo, out var rowBg, out var labelTxt);

            // Switch (estilo on/off moderno) — mucho mas visible que un checkbox sobre fondo negro.
            var toggleGo = new GameObject("Switch", typeof(RectTransform));
            var togRT = toggleGo.GetComponent<RectTransform>();
            togRT.SetParent(rowGo.transform, false);
            togRT.anchorMin = new Vector2(0.5f, 0.5f);
            togRT.anchorMax = new Vector2(0.5f, 0.5f);
            togRT.pivot = new Vector2(0.5f, 0.5f);
            togRT.sizeDelta = new Vector2(64f, 28f);
            togRT.anchoredPosition = new Vector2(60f - controlColWidth * 0.5f + 32f, 0f);

            // Fondo del switch (cambia de color segun estado), con esquinas redondeadas.
            var bgImg = toggleGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.22f, 0.22f, 1f); // off color, claro y visible
            ApplyRounded(bgImg);
            // Outline para que destaque sobre el fondo negro absoluto
            var outline = toggleGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.7f, 0.65f, 0.55f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Indicador (circulo que se desliza de izq a der)
            var indGo = new GameObject("Indicator", typeof(RectTransform));
            var indRT = indGo.GetComponent<RectTransform>();
            indRT.SetParent(togRT, false);
            indRT.anchorMin = new Vector2(0f, 0.5f);
            indRT.anchorMax = new Vector2(0f, 0.5f);
            indRT.pivot = new Vector2(0.5f, 0.5f);
            indRT.sizeDelta = new Vector2(22f, 22f);
            indRT.anchoredPosition = new Vector2(SWITCH_OFF_X, 0f);
            var indImg = indGo.AddComponent<Image>();
            indImg.sprite = CreateCircleSprite(64);
            indImg.color = new Color(0.95f, 0.93f, 0.85f, 1f);
            indImg.raycastTarget = false;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            // Sin Toggle.graphic — la sincronizacion visual la hacemos manualmente.
            toggle.graphic = null;
            var tColors = toggle.colors;
            tColors.normalColor = Color.white;
            tColors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            tColors.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            tColors.pressedColor = new Color(0.9f, 0.85f, 0.7f, 1f);
            tColors.fadeDuration = 0.1f;
            toggle.colors = tColors;
            toggle.onValueChanged.AddListener((v) => {
                onChanged?.Invoke(v);
                UpdateSwitchVisual(toggle, bgImg, indRT);
            });

            // Cachear refs para que LoadValuesIntoUI pueda refrescar el visual.
            invertYBgImg = bgImg;
            invertYIndRT = indRT;

            row.control = toggle;
            row.label = labelTxt;
            rows.Add(row);

            y -= rowHeight;
            return toggle;
        }

        private void UpdateSwitchVisual(Toggle toggle, Image bgImg, RectTransform indRT)
        {
            if (toggle == null || bgImg == null || indRT == null) return;
            bool on = toggle.isOn;
            indRT.anchoredPosition = new Vector2(on ? SWITCH_ON_X : SWITCH_OFF_X, 0f);
            bgImg.color = on
                ? new Color(sliderFillColor.r, sliderFillColor.g, sliderFillColor.b, 0.95f)
                : new Color(0.25f, 0.22f, 0.22f, 1f);
        }

        private void AddMicTestRow(Transform parent, ref float y, string description, SettingsTab tab)
        {
            var row = MakeRow(parent, y, "Microfono", description, tab, out var rowGo, out var rowBg, out var labelTxt);

            // Boton "Probar microfono"
            float btnW = 200f;
            var btnGo = new GameObject("MicTestBtn", typeof(RectTransform));
            var btnRT = btnGo.GetComponent<RectTransform>();
            btnRT.SetParent(rowGo.transform, false);
            btnRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.pivot = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(btnW, 36f);
            btnRT.anchoredPosition = new Vector2(60f - controlColWidth * 0.5f + btnW * 0.5f, 0f);
            var micBtnImg = btnGo.AddComponent<Image>();
            micBtnImg.color = buttonNormalColor;
            ApplyRounded(micBtnImg);

            micTestButton = btnGo.AddComponent<Button>();
            var bC = micTestButton.colors;
            bC.normalColor = buttonNormalColor;
            bC.highlightedColor = buttonHoverColor;
            bC.pressedColor = buttonPressedColor;
            bC.selectedColor = buttonHoverColor;
            bC.fadeDuration = 0.1f;
            micTestButton.colors = bC;
            micTestButton.onClick.AddListener(ToggleMicTest);

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(btnGo.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = GetDefaultFont();
            lbl.text = "Probar microfono";
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.fontSize = 16;
            lbl.color = textColor;
            lbl.raycastTarget = false;

            // Barra de nivel del mic (a la derecha del boton)
            float barW = 220f;
            var barBgGo = new GameObject("MicBarBg", typeof(RectTransform));
            var barBgRT = barBgGo.GetComponent<RectTransform>();
            barBgRT.SetParent(rowGo.transform, false);
            barBgRT.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRT.pivot = new Vector2(0.5f, 0.5f);
            barBgRT.sizeDelta = new Vector2(barW, 18f);
            barBgRT.anchoredPosition = new Vector2(60f - controlColWidth * 0.5f + btnW + 30f + barW * 0.5f, 0f);
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = sliderBgColor;
            ApplyRounded(barBgImg);

            var barFillGo = new GameObject("MicBarFill", typeof(RectTransform));
            micLevelBarFillRT = barFillGo.GetComponent<RectTransform>();
            micLevelBarFillRT.SetParent(barBgRT, false);
            micLevelBarFillRT.anchorMin = new Vector2(0f, 0f);
            micLevelBarFillRT.anchorMax = new Vector2(0f, 1f);
            micLevelBarFillRT.pivot = new Vector2(0f, 0.5f);
            micLevelBarFillRT.anchoredPosition = new Vector2(2f, 0f);
            micLevelBarBaseWidth = barW - 4f;
            micLevelBarFillRT.sizeDelta = new Vector2(0f, -4f);
            var barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.color = sliderFillColor;
            ApplyRounded(barFillImg);

            // Status text debajo del row mic test
            micTestStatusText = MakeText(rowGo.transform, "MicStatus", "", 13, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.65f),
                new Vector2(60f, -22f), new Vector2(controlColWidth + 100f, 18f));

            row.control = micTestButton;
            row.label = labelTxt;
            rows.Add(row);

            y -= rowHeight;
        }

        private Row MakeRow(Transform parent, float y, string labelText, string description, SettingsTab tab,
            out GameObject rowGo, out Image rowBg, out Text labelTextOut)
        {
            rowGo = new GameObject("Row_" + labelText, typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(contentWidth, rowHeight - 6f);
            rt.anchoredPosition = new Vector2(0f, y);

            // Background invisible para highlight
            rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(rowSelectedColor.r, rowSelectedColor.g, rowSelectedColor.b, 0f);
            rowBg.raycastTarget = false;

            // CanvasGroup para stagger animation
            rowGo.AddComponent<CanvasGroup>();

            // Label (columna izquierda)
            labelTextOut = MakeText(rowGo.transform, "Label", labelText, 20, FontStyle.Normal, labelColor,
                new Vector2(-contentWidth * 0.5f + labelColWidth * 0.5f + 20f, 0f),
                new Vector2(labelColWidth, rowHeight - 10f));
            labelTextOut.alignment = TextAnchor.MiddleLeft;
            // Sombra para legibilidad sobre la imagen del menu
            var lblShadow = labelTextOut.gameObject.AddComponent<Shadow>();
            lblShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            lblShadow.effectDistance = new Vector2(1f, -1f);

            return new Row { rowGo = rowGo, bg = rowBg, label = labelTextOut, description = description, tab = tab };
        }

        // ---------- helpers genericos ----------

        private Button MakeButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 sizeDelta, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var btnImg = go.AddComponent<Image>();
            btnImg.color = buttonNormalColor;
            ApplyRounded(btnImg);
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

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
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

        // ====================================================
        // PERFIL TAB — Cuenta + Estadisticas + Historial
        // (logica similar a ProfileOverlay pero embebida en el panel)
        // ====================================================

        private void BuildPerfilContent()
        {
            // Container que se activa/desactiva con SwitchTab(Perfil).
            var contGo = new GameObject("PerfilContent", typeof(RectTransform));
            perfilContent = contGo;
            var contRT = contGo.GetComponent<RectTransform>();
            contRT.SetParent(panel, false);
            contRT.anchorMin = Vector2.zero;
            contRT.anchorMax = Vector2.one;
            contRT.offsetMin = Vector2.zero;
            contRT.offsetMax = Vector2.zero;
            contGo.SetActive(false);

            Color accent = sliderFillColor;
            Color labelC = labelColor;
            Color textC = textColor;

            // Margen izquierdo unificado: TODOS los elementos left-aligned tienen su LEFT EDGE en este X.
            // anchoredPos.x del rect = LEFT_MARGIN + sizeDelta.x / 2 (porque pivot 0.5 centra).
            const float LEFT_MARGIN = -460f;
            const float CONTENT_WIDTH = 920f;

            // ----- TU CUENTA -----
            var secCuenta = MakeText(contRT, "Sec_Cuenta", "TU CUENTA", 22, FontStyle.Bold, accent,
                new Vector2(LEFT_MARGIN + 180f, 170f), new Vector2(360f, 30f));
            secCuenta.alignment = TextAnchor.MiddleLeft;

            var lblNombre = MakeText(contRT, "LblNombre", "Nombre:", 18, FontStyle.Normal, labelC,
                new Vector2(LEFT_MARGIN + 50f, 120f), new Vector2(100f, 30f));
            lblNombre.alignment = TextAnchor.MiddleLeft;

            // InputField del nombre — left edge justo despues de "Nombre:" + 20px de gap.
            // "Nombre:" ocupa LEFT_MARGIN..LEFT_MARGIN+100 = -460..-360. Input desde -340.
            var inputGo = new GameObject("NombresInput", typeof(RectTransform));
            var inputRT = inputGo.GetComponent<RectTransform>();
            inputRT.SetParent(contRT, false);
            inputRT.anchorMin = inputRT.anchorMax = new Vector2(0.5f, 0.5f);
            inputRT.pivot = new Vector2(0.5f, 0.5f);
            inputRT.sizeDelta = new Vector2(380f, 38f);
            inputRT.anchoredPosition = new Vector2(-150f, 120f); // left edge en -340
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.12f, 0.10f, 0.10f, 0.9f);
            ApplyRounded(inputBg);

            var textChildGo = new GameObject("Text", typeof(RectTransform));
            var textChildRT = textChildGo.GetComponent<RectTransform>();
            textChildRT.SetParent(inputRT, false);
            textChildRT.anchorMin = Vector2.zero; textChildRT.anchorMax = Vector2.one;
            textChildRT.offsetMin = new Vector2(10f, 4f); textChildRT.offsetMax = new Vector2(-10f, -4f);
            var textChild = textChildGo.AddComponent<Text>();
            textChild.font = GetDefaultFont();
            textChild.fontSize = 18;
            textChild.color = textC;
            textChild.alignment = TextAnchor.MiddleLeft;
            textChild.supportRichText = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            var phRT = phGo.GetComponent<RectTransform>();
            phRT.SetParent(inputRT, false);
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(10f, 4f); phRT.offsetMax = new Vector2(-10f, -4f);
            var ph = phGo.AddComponent<Text>();
            ph.font = GetDefaultFont();
            ph.fontSize = 18;
            ph.color = new Color(textC.r, textC.g, textC.b, 0.4f);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = "tu nombre";

            perfilNombresInput = inputGo.AddComponent<InputField>();
            perfilNombresInput.targetGraphic = inputBg;
            perfilNombresInput.textComponent = textChild;
            perfilNombresInput.placeholder = ph;
            perfilNombresInput.characterLimit = 50;
            perfilNombresInput.lineType = InputField.LineType.SingleLine;
            perfilNombresInput.interactable = false;

            // Boton Editar / Guardar — left edge justo despues del input + 20px gap.
            // Input ocupa -340..+40. Boton desde +60. Boton 140 wide -> center +130.
            perfilEditButton = MakePerfilButton(contRT, "BtnEdit", "Editar",
                new Vector2(130f, 120f), new Vector2(140f, 40f), OnPerfilEditClicked, out perfilEditButtonLabel);

            // ----- ESTADISTICAS -----
            var secStats = MakeText(contRT, "Sec_Stats", "ESTADISTICAS", 22, FontStyle.Bold, accent,
                new Vector2(LEFT_MARGIN + 180f, 70f), new Vector2(360f, 30f));
            secStats.alignment = TextAnchor.MiddleLeft;

            // Stats text con left edge alineado al margen izquierdo.
            perfilStatsText = MakeText(contRT, "StatsText", "", 18, FontStyle.Normal, textC,
                new Vector2(LEFT_MARGIN + CONTENT_WIDTH * 0.5f, 15f), new Vector2(CONTENT_WIDTH, 80f));
            perfilStatsText.alignment = TextAnchor.UpperLeft;
            perfilStatsText.supportRichText = true;

            // ----- HISTORIAL DE PARTIDAS -----
            var secHist = MakeText(contRT, "Sec_Hist", "HISTORIAL DE PARTIDAS", 22, FontStyle.Bold, accent,
                new Vector2(LEFT_MARGIN + 250f, -90f), new Vector2(500f, 30f));
            secHist.alignment = TextAnchor.MiddleLeft;

            // Header de columnas alineado al margen izquierdo, mismo ancho que la scroll.
            var headerCols = MakeText(contRT, "HistHeader",
                "<b>#</b>      <b>Fecha</b>                  <b>Estado</b>           <b>Tiempo</b>     <b>Logros</b>",
                13, FontStyle.Normal, labelC,
                new Vector2(LEFT_MARGIN + CONTENT_WIDTH * 0.5f, -125f), new Vector2(CONTENT_WIDTH, 22f));
            headerCols.alignment = TextAnchor.MiddleLeft;
            headerCols.supportRichText = true;

            // ScrollRect del historial — mismo ancho que el header, alineado al margen.
            var scrollGo = new GameObject("HistoryScroll", typeof(RectTransform));
            var scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.SetParent(contRT, false);
            scrollRT.anchorMin = scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);
            scrollRT.sizeDelta = new Vector2(CONTENT_WIDTH, 110f);
            scrollRT.anchoredPosition = new Vector2(LEFT_MARGIN + CONTENT_WIDTH * 0.5f, -195f);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.25f);
            ApplyRounded(scrollImg);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.SetParent(scrollRT, false);
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0);
            var mask = viewportGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var cGo = new GameObject("Content", typeof(RectTransform));
            perfilHistoryContent = cGo.GetComponent<RectTransform>();
            perfilHistoryContent.SetParent(viewportRT, false);
            perfilHistoryContent.anchorMin = new Vector2(0f, 1f);
            perfilHistoryContent.anchorMax = new Vector2(1f, 1f);
            perfilHistoryContent.pivot = new Vector2(0.5f, 1f);
            perfilHistoryContent.sizeDelta = Vector2.zero;
            perfilHistoryContent.anchoredPosition = Vector2.zero;
            var vlg = cGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 0f;
            vlg.padding = new RectOffset(8, 8, 4, 4);
            var fitter = cGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRT;
            scroll.content = perfilHistoryContent;

            perfilHistoryEmptyText = MakeText(scrollRT, "Empty", "", 16, FontStyle.Italic,
                new Color(textC.r, textC.g, textC.b, 0.6f),
                new Vector2(0f, 0f), new Vector2(CONTENT_WIDTH - 40f, 28f));

            // ----- Status text (debajo del scroll, alineado al margen) -----
            perfilStatusText = MakeText(contRT, "Status", "", 14, FontStyle.Italic,
                new Color(accent.r, accent.g, accent.b, 0.85f),
                new Vector2(LEFT_MARGIN + CONTENT_WIDTH * 0.5f, -265f), new Vector2(CONTENT_WIDTH, 22f));
            perfilStatusText.alignment = TextAnchor.MiddleLeft;
        }

        private Button MakePerfilButton(Transform parent, string name, string label, Vector2 pos, Vector2 size,
            Action onClick, out Text labelTextOut)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = buttonNormalColor;
            ApplyRounded(img);
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

        private void ResetPerfilUiState()
        {
            perfilEditing = false;
            EndPerfilEditMode();
            if (perfilStatsText != null) perfilStatsText.text = "";
            if (perfilHistoryEmptyText != null) {
                perfilHistoryEmptyText.gameObject.SetActive(true);
                perfilHistoryEmptyText.text = "Cargando...";
            }
            ClearPerfilHistoryRows();
            SetPerfilStatus("");
            perfilCurrentJugador = null;
            perfilCurrentPartidas = null;
            perfilCurrentLogroXPartida = null;
            perfilHadAnyError = false;
            perfilPendingApiCalls = 0;
        }

        private void LoadPerfilDataFromApi()
        {
            if (!GameSession.IsOnline)
            {
                SetPerfilStatus("Modo offline. No hay conexion a la API.");
                if (perfilHistoryEmptyText != null) perfilHistoryEmptyText.text = "Sin datos.";
                if (perfilNombresInput != null) {
                    perfilNombresInput.text = GameSession.CurrentNombres;
                    perfilNombresInput.interactable = false;
                }
                if (perfilEditButton != null) perfilEditButton.interactable = false;
                return;
            }

            if (perfilNombresInput != null) perfilNombresInput.interactable = false;
            if (perfilEditButton != null) perfilEditButton.interactable = true;

            perfilPendingApiCalls = 3;

            ApiClient.Instance.GetJugador(GameSession.CurrentJugadorId,
                j => { perfilCurrentJugador = j; OnPerfilApiCallDone(); },
                err => { perfilHadAnyError = true; Debug.LogWarning($"[Perfil] GetJugador err: {err}"); OnPerfilApiCallDone(); });

            ApiClient.Instance.GetAllPartidas(
                arr => { perfilCurrentPartidas = arr; OnPerfilApiCallDone(); },
                err => { perfilHadAnyError = true; Debug.LogWarning($"[Perfil] GetAllPartidas err: {err}"); OnPerfilApiCallDone(); });

            ApiClient.Instance.GetAllLogroXPartida(
                arr => { perfilCurrentLogroXPartida = arr; OnPerfilApiCallDone(); },
                err => { perfilHadAnyError = true; Debug.LogWarning($"[Perfil] GetAllLogroXPartida err: {err}"); OnPerfilApiCallDone(); });
        }

        private void OnPerfilApiCallDone()
        {
            perfilPendingApiCalls--;
            if (perfilPendingApiCalls > 0) return;

            if (perfilHadAnyError && perfilCurrentJugador == null)
            {
                SetPerfilStatus("No se pudo cargar el perfil. Reintenta mas tarde.");
                if (perfilHistoryEmptyText != null) perfilHistoryEmptyText.text = "Sin datos.";
                if (perfilEditButton != null) perfilEditButton.interactable = false;
                return;
            }

            string nombres = perfilCurrentJugador != null ? perfilCurrentJugador.nombres : GameSession.CurrentNombres;
            if (perfilNombresInput != null) {
                perfilNombresInput.text = nombres ?? "";
                perfilNombresOriginal = perfilNombresInput.text;
            }

            var misPartidas = PerfilFilterPartidasMias();
            var misLogroXPartida = PerfilFilterLogroXPartidaMios(misPartidas);

            int jugadas = misPartidas.Count;
            int completadas = PerfilCountCompletadas(misPartidas);
            var distinctLogros = new HashSet<int>();
            foreach (var lp in misLogroXPartida) distinctLogros.Add(lp.idLogro);
            int totalCatalogo = GameSession.LogroIdByCodigo.Count;
            int puntosTotales = PerfilSumarPuntos(distinctLogros);

            if (perfilStatsText != null)
                perfilStatsText.text =
                    $"<b>Partidas jugadas:</b> {jugadas}      " +
                    $"<b>Completadas:</b> {completadas}\n" +
                    $"<b>Logros:</b> {distinctLogros.Count} / {totalCatalogo}      " +
                    $"<b>Puntos:</b> {puntosTotales}";

            ClearPerfilHistoryRows();
            if (misPartidas.Count == 0)
            {
                if (perfilHistoryEmptyText != null) {
                    perfilHistoryEmptyText.gameObject.SetActive(true);
                    perfilHistoryEmptyText.text = "Aun no jugaste ninguna partida.";
                }
            }
            else
            {
                if (perfilHistoryEmptyText != null) perfilHistoryEmptyText.gameObject.SetActive(false);
                misPartidas.Sort((a, b) => string.Compare(b.fechaInicio, a.fechaInicio, StringComparison.Ordinal));
                int limit = Mathf.Min(misPartidas.Count, 20);
                for (int i = 0; i < limit; i++)
                {
                    var p = misPartidas[i];
                    int logrosDePartida = PerfilCountLogrosDeUnaPartida(p.idPartida, misLogroXPartida);
                    BuildPerfilHistoryRow(i, p, logrosDePartida);
                }
            }

            SetPerfilStatus(perfilHadAnyError ? "Algunos datos no cargaron. Resultados parciales." : "");
        }

        private List<PartidaDto> PerfilFilterPartidasMias()
        {
            var list = new List<PartidaDto>();
            if (perfilCurrentPartidas == null) return list;
            int mio = GameSession.CurrentJugadorId;
            foreach (var p in perfilCurrentPartidas)
                if (p.idJugador == mio) list.Add(p);
            return list;
        }

        private List<LogroXPartidaDto> PerfilFilterLogroXPartidaMios(List<PartidaDto> misPartidas)
        {
            var ids = new HashSet<int>();
            foreach (var p in misPartidas) ids.Add(p.idPartida);
            var list = new List<LogroXPartidaDto>();
            if (perfilCurrentLogroXPartida == null) return list;
            foreach (var lp in perfilCurrentLogroXPartida)
                if (ids.Contains(lp.idPartida)) list.Add(lp);
            return list;
        }

        private static int PerfilCountCompletadas(List<PartidaDto> partidas)
        {
            int n = 0;
            foreach (var p in partidas) if (p.estado == 1) n++;
            return n;
        }

        private static int PerfilCountLogrosDeUnaPartida(int idPartida, List<LogroXPartidaDto> logroxpartida)
        {
            int n = 0;
            foreach (var lp in logroxpartida) if (lp.idPartida == idPartida) n++;
            return n;
        }

        private static int PerfilSumarPuntos(HashSet<int> idsLogros)
        {
            int total = 0;
            foreach (var id in idsLogros)
                if (GameSession.LogroByIdCache.TryGetValue(id, out var l)) total += l.puntos;
            return total;
        }

        private void OnPerfilEditClicked()
        {
            if (!perfilEditing) BeginPerfilEditMode();
            else SavePerfilEdit();
        }

        private void BeginPerfilEditMode()
        {
            if (perfilCurrentJugador == null) return;
            perfilEditing = true;
            perfilNombresOriginal = perfilNombresInput.text;
            perfilNombresInput.interactable = true;
            EventSystem.current?.SetSelectedGameObject(perfilNombresInput.gameObject);
            if (perfilEditButtonLabel != null) perfilEditButtonLabel.text = "Guardar";
        }

        private void EndPerfilEditMode()
        {
            perfilEditing = false;
            if (perfilNombresInput != null) perfilNombresInput.interactable = false;
            if (perfilEditButtonLabel != null) perfilEditButtonLabel.text = "Editar";
        }

        private void SavePerfilEdit()
        {
            if (perfilCurrentJugador == null) { EndPerfilEditMode(); return; }
            string nuevo = (perfilNombresInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(nuevo)) { SetPerfilStatus("El nombre no puede estar vacio."); perfilNombresInput.text = perfilNombresOriginal; return; }
            if (nuevo.Length > 50) nuevo = nuevo.Substring(0, 50);
            if (nuevo == perfilNombresOriginal) { EndPerfilEditMode(); return; }

            perfilCurrentJugador.nombres = nuevo;
            perfilEditButton.interactable = false;
            SetPerfilStatus("Guardando...");

            ApiClient.Instance.UpdateJugador(perfilCurrentJugador,
                () =>
                {
                    GameSession.UpdateNombres(nuevo);
                    perfilNombresOriginal = nuevo;
                    EndPerfilEditMode();
                    perfilEditButton.interactable = true;
                    SetPerfilStatus("Guardado.");
                },
                err =>
                {
                    Debug.LogWarning($"[Perfil] UpdateJugador err: {err}");
                    perfilNombresInput.text = perfilNombresOriginal;
                    EndPerfilEditMode();
                    perfilEditButton.interactable = true;
                    SetPerfilStatus("No se pudo guardar el cambio.");
                });
        }

        private void SetPerfilStatus(string msg)
        {
            if (perfilStatusText != null) perfilStatusText.text = msg ?? "";
        }

        private void ClearPerfilHistoryRows()
        {
            if (perfilHistoryContent == null) return;
            for (int i = perfilHistoryContent.childCount - 1; i >= 0; i--)
                Destroy(perfilHistoryContent.GetChild(i).gameObject);
        }

        private void BuildPerfilHistoryRow(int index, PartidaDto p, int logrosCount)
        {
            if (p == null) { Debug.LogWarning($"[Perfil] BuildPerfilHistoryRow: PartidaDto null en index {index}, skip."); return; }
            if (perfilHistoryContent == null) { Debug.LogWarning("[Perfil] BuildPerfilHistoryRow: perfilHistoryContent null."); return; }

            var rowGo = new GameObject("Row_" + index, typeof(RectTransform));
            var rt = rowGo.GetComponent<RectTransform>();
            rt.SetParent(perfilHistoryContent, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 26f;
            var bg = rowGo.AddComponent<Image>();
            bg.color = (index % 2 == 0) ? PERFIL_ROW_EVEN : PERFIL_ROW_ODD;
            bg.raycastTarget = false;

            string fecha = PerfilFormatFecha(p.fechaInicio);
            string estado = PerfilEstadoLabel(p.estado);
            string tiempo = PerfilFormatTiempo(p.tiempoSegundos);
            Color estadoCol = PerfilEstadoColor(p.estado);
            string line = $"<b>#{p.idPartida:D3}</b>   {fecha}   <color=#{ColorUtility.ToHtmlStringRGB(estadoCol)}>{estado,-12}</color>   {tiempo}   {logrosCount} logros";

            var t = rowGo.AddComponent<Text>();
            if (t == null) { Debug.LogWarning("[Perfil] BuildPerfilHistoryRow: AddComponent<Text> retorno null."); Destroy(rowGo); return; }
            var font = GetDefaultFont();
            if (font != null) t.font = font;
            t.fontSize = 14;
            t.color = textColor;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.text = line;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static string PerfilFormatFecha(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "—";
            if (DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return iso;
        }

        private static string PerfilEstadoLabel(byte estado)
            => estado switch { 1 => "Completada", 2 => "Abandonada", _ => "EnCurso" };

        private static Color PerfilEstadoColor(byte estado)
            => estado switch { 1 => PERFIL_COMPLETADA, 2 => PERFIL_ABANDONADA, _ => PERFIL_ENCURSO };

        private static string PerfilFormatTiempo(int segundos)
        {
            int h = segundos / 3600;
            int m = (segundos % 3600) / 60;
            int s = segundos % 60;
            return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
        }
    }
}
