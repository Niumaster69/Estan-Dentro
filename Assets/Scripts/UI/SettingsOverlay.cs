using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using EstanDentro.Breathing;

namespace EstanDentro.UI
{
    public class SettingsOverlay : MonoBehaviour
    {
        private static SettingsOverlay instance;

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.96f);
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
        private float titleY = 380f;
        [SerializeField, Tooltip("Y donde empieza la primera fila.")]
        private float firstRowY = 230f;
        [SerializeField, Tooltip("Y de la descripcion del control activo.")]
        private float descriptionY = -290f;
        [SerializeField, Tooltip("Y de los botones inferiores.")]
        private float bottomButtonsY = -380f;

        // ---------- estado runtime ----------

        private struct Row
        {
            public GameObject rowGo;
            public Image bg;
            public Selectable control;
            public Text label;
            public Text valueLabel;
            public string description;
        }

        private List<Row> rows = new List<Row>();
        private Canvas canvas;
        private Image bgImage;
        private RectTransform panel;
        private Slider volumeSlider, mouseSlider, gamepadSlider, brightnessSlider;
        private Text volumeValueText, mouseValueText, gamepadValueText, brightnessValueText;
        private Toggle invertYToggle;
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
            EventSystem.current?.SetSelectedGameObject(instance.volumeSlider.gameObject);
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
            string activeDesc = "";
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool isSel = selected != null && row.control != null && selected == row.control.gameObject;

                // Background fade
                if (row.bg != null)
                {
                    Color target = isSel ? rowSelectedColor : new Color(rowSelectedColor.r, rowSelectedColor.g, rowSelectedColor.b, 0f);
                    row.bg.color = Color.Lerp(row.bg.color, target, Time.unscaledDeltaTime * 12f);
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
            mouseSlider.SetValueWithoutNotify(Settings.MouseSensitivity);
            gamepadSlider.SetValueWithoutNotify(Settings.GamepadSensitivity);
            brightnessSlider.SetValueWithoutNotify(Settings.Brightness);
            invertYToggle.SetIsOnWithoutNotify(Settings.InvertY);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            volumeValueText.text = Mathf.RoundToInt(Settings.MasterVolume * 100f) + "%";
            mouseValueText.text = Settings.MouseSensitivity.ToString("F2");
            gamepadValueText.text = Mathf.RoundToInt(Settings.GamepadSensitivity).ToString();
            brightnessValueText.text = (Settings.Brightness * 100f).ToString("F0") + "%";
        }

        private void OnVolumeChanged(float v) { Settings.MasterVolume = v; UpdateValueLabels(); }
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
            BuildRows();
            BuildBottomButtons();
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
            float startY = firstRowY;

            volumeSlider = AddSliderRow(panel, ref startY, "Volumen master",
                "Ajusta el volumen general del juego.",
                0f, 1f, OnVolumeChanged, out volumeValueText);

            mouseSlider = AddSliderRow(panel, ref startY, "Sensibilidad mouse",
                "Velocidad de la camara con el mouse. Subir para girar mas rapido.",
                Settings.MOUSE_SENS_MIN, Settings.MOUSE_SENS_MAX, OnMouseChanged, out mouseValueText);

            gamepadSlider = AddSliderRow(panel, ref startY, "Sensibilidad mando",
                "Velocidad de la camara con el stick derecho del mando.",
                Settings.GAMEPAD_SENS_MIN, Settings.GAMEPAD_SENS_MAX, OnGamepadChanged, out gamepadValueText);

            brightnessSlider = AddSliderRow(panel, ref startY, "Brillo",
                "Brillo general del juego. Util si tu monitor es muy oscuro.",
                Settings.BRIGHTNESS_MIN, Settings.BRIGHTNESS_MAX, OnBrightnessChanged, out brightnessValueText);

            invertYToggle = AddToggleRow(panel, ref startY, "Invertir eje Y",
                "Si esta activado, mover el mouse hacia arriba mira hacia abajo.",
                OnInvertYChanged);

            // Mic test row (boton + barra de nivel)
            AddMicTestRow(panel, ref startY,
                "Prueba el microfono. La barra muestra el nivel de audio en tiempo real.");
        }

        private void BuildBottomButtons()
        {
            float y = bottomButtonsY;
            // Volver (centrado)
            MakeButton(panel, "Btn_Close", "Volver",
                new Vector2(0f, y), new Vector2(280f, 48f), () => Close());
            // Reset y Recalibrar a los lados, mas pequenos y discretos
            MakeButton(panel, "Btn_Reset", "Restaurar defaults",
                new Vector2(-320f, y), new Vector2(260f, 44f), OnResetDefaultsClicked);
            MakeButton(panel, "Btn_Recalibrate", "Re-calibrar microfono",
                new Vector2(320f, y), new Vector2(260f, 44f), OnRecalibrateClicked);
        }

        // ---------- helpers de filas ----------

        private Slider AddSliderRow(Transform parent, ref float y, string labelText, string description,
            float min, float max, System.Action<float> onChanged, out Text valueText)
        {
            var row = MakeRow(parent, y, labelText, description, out var rowGo, out var rowBg, out var labelTxt);

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
            sBgGo.AddComponent<Image>().color = sliderBgColor;

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

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRT = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRT.SetParent(sliderRT, false);
            handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10f, 0f);
            handleAreaRT.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform));
            var handleRT = handleGo.GetComponent<RectTransform>();
            handleRT.SetParent(handleAreaRT, false);
            handleRT.sizeDelta = new Vector2(20f, 30f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = sliderHandleColor;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener((v) => onChanged?.Invoke(v));

            // Value label (a la derecha)
            valueText = MakeText(rowGo.transform, "Value", "", 18, FontStyle.Bold, textColor,
                new Vector2(controlColWidth * 0.5f + 60f, 0f), new Vector2(valueColWidth, 30f));

            row.control = slider;
            row.valueLabel = valueText;
            row.label = labelTxt;
            rows.Add(row);

            y -= rowHeight;
            return slider;
        }

        private Toggle AddToggleRow(Transform parent, ref float y, string labelText, string description,
            System.Action<bool> onChanged)
        {
            var row = MakeRow(parent, y, labelText, description, out var rowGo, out var rowBg, out var labelTxt);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform));
            var togRT = toggleGo.GetComponent<RectTransform>();
            togRT.SetParent(rowGo.transform, false);
            togRT.anchorMin = new Vector2(0.5f, 0.5f);
            togRT.anchorMax = new Vector2(0.5f, 0.5f);
            togRT.pivot = new Vector2(0.5f, 0.5f);
            togRT.sizeDelta = new Vector2(34f, 34f);
            togRT.anchoredPosition = new Vector2(60f - controlColWidth * 0.5f + 17f, 0f);

            var bgImg = toggleGo.AddComponent<Image>();
            bgImg.color = sliderBgColor;

            var checkGo = new GameObject("Check", typeof(RectTransform));
            var checkRT = checkGo.GetComponent<RectTransform>();
            checkRT.SetParent(togRT, false);
            checkRT.anchorMin = Vector2.zero; checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(5f, 5f);
            checkRT.offsetMax = new Vector2(-5f, -5f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = sliderFillColor;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            var tColors = toggle.colors;
            tColors.normalColor = sliderBgColor;
            tColors.highlightedColor = buttonHoverColor;
            tColors.selectedColor = buttonHoverColor;
            tColors.pressedColor = buttonPressedColor;
            tColors.fadeDuration = 0.1f;
            toggle.colors = tColors;
            toggle.onValueChanged.AddListener((v) => onChanged?.Invoke(v));

            row.control = toggle;
            row.label = labelTxt;
            rows.Add(row);

            y -= rowHeight;
            return toggle;
        }

        private void AddMicTestRow(Transform parent, ref float y, string description)
        {
            var row = MakeRow(parent, y, "Microfono", description, out var rowGo, out var rowBg, out var labelTxt);

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
            btnGo.AddComponent<Image>().color = buttonNormalColor;

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
            barBgGo.AddComponent<Image>().color = sliderBgColor;

            var barFillGo = new GameObject("MicBarFill", typeof(RectTransform));
            micLevelBarFillRT = barFillGo.GetComponent<RectTransform>();
            micLevelBarFillRT.SetParent(barBgRT, false);
            micLevelBarFillRT.anchorMin = new Vector2(0f, 0f);
            micLevelBarFillRT.anchorMax = new Vector2(0f, 1f);
            micLevelBarFillRT.pivot = new Vector2(0f, 0.5f);
            micLevelBarFillRT.anchoredPosition = new Vector2(2f, 0f);
            micLevelBarBaseWidth = barW - 4f;
            micLevelBarFillRT.sizeDelta = new Vector2(0f, -4f);
            barFillGo.AddComponent<Image>().color = sliderFillColor;

            // Status text debajo del row mic test
            micTestStatusText = MakeText(rowGo.transform, "MicStatus", "", 13, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.65f),
                new Vector2(60f, -22f), new Vector2(controlColWidth + 100f, 18f));

            row.control = micTestButton;
            row.label = labelTxt;
            rows.Add(row);

            y -= rowHeight;
        }

        private Row MakeRow(Transform parent, float y, string labelText, string description,
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

            return new Row { rowGo = rowGo, bg = rowBg, label = labelTextOut, description = description };
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
    }
}
