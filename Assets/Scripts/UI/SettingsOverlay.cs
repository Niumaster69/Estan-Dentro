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
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color panelColor = new Color(0.08f, 0.07f, 0.05f, 0.97f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color labelColor = new Color(0.92f, 0.89f, 0.83f, 0.7f);
        [SerializeField] private Color sliderBgColor = new Color(0.12f, 0.12f, 0.13f, 0.85f);
        [SerializeField] private Color sliderFillColor = new Color(0.85f, 0.7f, 0.28f, 0.85f);
        [SerializeField] private Color sliderHandleColor = new Color(0.95f, 0.93f, 0.85f, 1f);
        [SerializeField] private Color buttonNormalColor = new Color(0.12f, 0.12f, 0.13f, 0.85f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);

        private Canvas canvas;
        private Slider volumeSlider;
        private Text volumeValueText;
        private Slider mouseSlider;
        private Text mouseValueText;
        private Slider gamepadSlider;
        private Text gamepadValueText;
        private Toggle invertYToggle;
        private Text recalibrateStatusText;
        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;
        private System.Action onCloseCallback;

        private System.Collections.Generic.List<(Selectable ctrl, Text label, Text valueLabel)> rows
            = new System.Collections.Generic.List<(Selectable, Text, Text)>();
        private static readonly Color SELECTED_LABEL_COLOR = new Color(0.96f, 0.85f, 0.42f, 1f);

        public static void Open(System.Action onClose = null)
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
            instance.canvas.gameObject.SetActive(true);
            instance.consumeInputThisFrame = true;
            instance.onCloseCallback = onClose;
            OverlayBlocker.Register();
            EventSystem.current?.SetSelectedGameObject(instance.volumeSlider.gameObject);
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

            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && kb.escapeKey.wasPressedThisFrame)
                        || (gp != null && gp.buttonEast.wasPressedThisFrame);
            if (dismiss) Close();
        }

        private void UpdateSelectionHighlight()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            foreach (var row in rows)
            {
                bool isSel = selected != null && row.ctrl != null && selected == row.ctrl.gameObject;
                if (row.label != null)
                    row.label.color = isSel ? SELECTED_LABEL_COLOR : labelColor;
                if (row.valueLabel != null)
                    row.valueLabel.color = isSel ? SELECTED_LABEL_COLOR : textColor;
            }
        }

        private void LoadValuesIntoUI()
        {
            volumeSlider.SetValueWithoutNotify(Settings.MasterVolume);
            mouseSlider.SetValueWithoutNotify(Settings.MouseSensitivity);
            gamepadSlider.SetValueWithoutNotify(Settings.GamepadSensitivity);
            invertYToggle.SetIsOnWithoutNotify(Settings.InvertY);
            UpdateValueLabels();
            recalibrateStatusText.text = "";
        }

        private void UpdateValueLabels()
        {
            volumeValueText.text = Mathf.RoundToInt(Settings.MasterVolume * 100f) + "%";
            mouseValueText.text = Settings.MouseSensitivity.ToString("F2");
            gamepadValueText.text = Mathf.RoundToInt(Settings.GamepadSensitivity).ToString();
        }

        // ---------- handlers ----------

        private void OnVolumeChanged(float v)
        {
            Settings.MasterVolume = v;
            UpdateValueLabels();
        }

        private void OnMouseSensChanged(float v)
        {
            Settings.MouseSensitivity = v;
            UpdateValueLabels();
            ApplyToActivePlayer();
        }

        private void OnGamepadSensChanged(float v)
        {
            Settings.GamepadSensitivity = v;
            UpdateValueLabels();
            ApplyToActivePlayer();
        }

        private void OnInvertYChanged(bool v)
        {
            Settings.InvertY = v;
            ApplyToActivePlayer();
        }

        private void OnRecalibrateClicked()
        {
            BreathingInputProvider.ClearStoredCalibration();

            var mc = FindObjectOfType<MicCalibration>();
            if (mc == null)
            {
                recalibrateStatusText.text = "Se hara al volver al juego.";
                return;
            }

            // Cerrar Settings sin disparar callback (porque vamos a abrir calibracion)
            onCloseCallback = null;
            Close();
            // Cerrar Pause si esta abierto
            if (PauseMenuHandler.Instance != null && PauseMenuHandler.Instance.IsOpen)
                PauseMenuHandler.Instance.ClosePause();
            // Lanzar calibracion ya
            mc.RestartCalibration();
        }

        private void ApplyToActivePlayer()
        {
            // Aplica los nuevos valores al PlayerController activo si esta en escena.
            var pc = FindObjectOfType<EstanDentro.Player.PlayerController>();
            if (pc != null) pc.ApplySettings();
        }

        // ---------- build ----------

        private void Build()
        {
            var canvasGo = new GameObject("Settings_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // BG
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
            var panelRT = panelGo.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(720f, 640f);
            panelGo.AddComponent<Image>().color = panelColor;

            // Title
            MakeText(panelRT, "Title", "AJUSTES", 32, FontStyle.Bold, titleColor,
                new Vector2(0, 270), new Vector2(660, 50));

            // Sliders + toggle
            float y = 180f;
            float rowSpacing = 78f;

            Text volLabel, mouseLabel, gpLabel, invLabel;
            volumeSlider = MakeSlider(panelRT, "Volume", "Volumen master", new Vector2(0, y), 0f, 1f, OnVolumeChanged, out volumeValueText, out volLabel);
            rows.Add((volumeSlider, volLabel, volumeValueText));
            y -= rowSpacing;
            mouseSlider = MakeSlider(panelRT, "MouseSens", "Sensibilidad mouse", new Vector2(0, y), Settings.MOUSE_SENS_MIN, Settings.MOUSE_SENS_MAX, OnMouseSensChanged, out mouseValueText, out mouseLabel);
            rows.Add((mouseSlider, mouseLabel, mouseValueText));
            y -= rowSpacing;
            gamepadSlider = MakeSlider(panelRT, "GamepadSens", "Sensibilidad mando", new Vector2(0, y), Settings.GAMEPAD_SENS_MIN, Settings.GAMEPAD_SENS_MAX, OnGamepadSensChanged, out gamepadValueText, out gpLabel);
            rows.Add((gamepadSlider, gpLabel, gamepadValueText));
            y -= rowSpacing;
            invertYToggle = MakeToggle(panelRT, "InvertY", "Invertir eje Y", new Vector2(0, y), OnInvertYChanged, out invLabel);
            rows.Add((invertYToggle, invLabel, null));
            y -= rowSpacing;

            // Re-calibrar
            Text recalibLabel;
            var btnRecalib = MakeButton(panelRT, "Btn_Recalibrate", "Re-calibrar microfono", new Vector2(0, y - 8f), OnRecalibrateClicked, out recalibLabel);
            rows.Add((btnRecalib, recalibLabel, null));
            recalibrateStatusText = MakeText(panelRT, "RecalibStatus", "", 14, FontStyle.Italic,
                new Color(textColor.r, textColor.g, textColor.b, 0.6f),
                new Vector2(0, y - 56f), new Vector2(640, 24));

            // Cerrar
            Text closeLabel;
            var btnClose = MakeButton(panelRT, "Btn_Close", "Cerrar", new Vector2(0, -270f), () => Close(), out closeLabel);
            rows.Add((btnClose, closeLabel, null));

            // Setup navigation explicit entre todos los controles
            var ctrls = new System.Collections.Generic.List<Selectable>
            {
                volumeSlider, mouseSlider, gamepadSlider, invertYToggle, btnRecalib, btnClose
            };
            for (int i = 0; i < ctrls.Count; i++)
            {
                var nav = ctrls[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = ctrls[(i - 1 + ctrls.Count) % ctrls.Count];
                nav.selectOnDown = ctrls[(i + 1) % ctrls.Count];
                ctrls[i].navigation = nav;
            }

            // Hint
            MakeText(panelRT, "Hint", "Esc / Circle para cerrar - cambios se guardan automaticamente",
                12, FontStyle.Normal,
                new Color(textColor.r, textColor.g, textColor.b, 0.45f),
                new Vector2(0, -300f), new Vector2(640, 22));
        }

        // ---------- builders ----------

        private Slider MakeSlider(Transform parent, string name, string label, Vector2 anchoredPos, float min, float max, System.Action<float> onChanged, out Text valueText, out Text labelText)
        {
            // Label arriba-izquierda
            labelText = MakeText(parent, name + "_Label", label, 18, FontStyle.Normal, labelColor,
                anchoredPos + new Vector2(-180f, 24f), new Vector2(380, 24));

            // Valor arriba-derecha
            valueText = MakeText(parent, name + "_Value", "", 18, FontStyle.Bold, textColor,
                anchoredPos + new Vector2(220f, 24f), new Vector2(120, 24));

            // Slider Container
            var sliderGo = new GameObject(name + "_Slider");
            sliderGo.transform.SetParent(parent, false);
            var rt = sliderGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 22);
            rt.anchoredPosition = anchoredPos;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = sliderBgColor;

            // Fill area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRT = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0f);
            fillAreaRT.anchorMax = new Vector2(1f, 1f);
            fillAreaRT.offsetMin = new Vector2(4f, 4f);
            fillAreaRT.offsetMax = new Vector2(-4f, -4f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = sliderFillColor;

            // Handle
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRT = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10f, 0f);
            handleAreaRT.offsetMax = new Vector2(-10f, 0f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRT = handleGo.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(18f, 28f);
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

            return slider;
        }

        private Toggle MakeToggle(Transform parent, string name, string label, Vector2 anchoredPos, System.Action<bool> onChanged, out Text labelText)
        {
            var go = new GameObject(name + "_Toggle");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 36f);
            rt.anchoredPosition = anchoredPos;

            // Label
            labelText = MakeText(rt, "Label", label, 18, FontStyle.Normal, labelColor,
                new Vector2(-180f, 0f), new Vector2(380, 28));

            // Toggle box
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(rt, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.5f, 0.5f);
            bgRT.anchorMax = new Vector2(0.5f, 0.5f);
            bgRT.pivot = new Vector2(0.5f, 0.5f);
            bgRT.sizeDelta = new Vector2(28f, 28f);
            bgRT.anchoredPosition = new Vector2(220f, 0f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = sliderBgColor;

            // Checkmark
            var checkGo = new GameObject("Check");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRT = checkGo.AddComponent<RectTransform>();
            checkRT.anchorMin = Vector2.zero;
            checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(4f, 4f);
            checkRT.offsetMax = new Vector2(-4f, -4f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = sliderFillColor;

            var toggle = go.AddComponent<Toggle>();
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

            return toggle;
        }

        private Button MakeButton(Transform parent, string name, string label, Vector2 anchoredPos, System.Action onClick, out Text labelText)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 44f);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = buttonNormalColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHoverColor;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;
            labelText = lblGo.AddComponent<Text>();
            labelText.font = GetDefaultFont();
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.fontSize = 18;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = textColor;
            labelText.raycastTarget = false;

            return btn;
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
