using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Breathing
{
    [DefaultExecutionOrder(-40)]
    public class MicCalibration : MonoBehaviour
    {
        [Header("Tiempos (segundos)")]
        [SerializeField] private float noiseCaptureSeconds = 3f;
        [SerializeField] private float exhaleCaptureSeconds = 3f;

        [Header("Visual")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color levelColor = new Color(0.85f, 0.71f, 0.28f, 1f);
        [SerializeField] private Color accentColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color labelColor = new Color(0.65f, 0.6f, 0.55f, 0.85f);
        [SerializeField] private Color stepInactiveBg = new Color(0.10f, 0.09f, 0.09f, 0.85f);
        [SerializeField] private Color stepActiveBg = new Color(0.85f, 0.7f, 0.28f, 1f);
        [SerializeField] private Color buttonNormalColor = new Color(0.10f, 0.10f, 0.10f, 0.9f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);

        public event Action<bool> OnCalibrationDone;

        private enum Phase { WaitStart, CaptureNoise, CaptureExhale, Done }
        private Phase phase = Phase.WaitStart;
        private float phaseElapsed;
        private float noiseAccum, exhaleAccum;
        private int noiseSamples, exhaleSamples;
        private float prevTimeScale;

        private Canvas canvas;
        private Text titleText;
        private Text instructionText;
        private Text statusText;
        private RectTransform levelFill;
        private float levelBaseWidth;
        // Step indicator visual
        private Image step1Bg, step2Bg;
        private Text step1Lbl, step2Lbl;
        private Image stepLine;
        // Botones
        private Button btnComenzar, btnSaltar;
        private Sprite roundedRectCache;

        private void Awake()
        {
            BuildUI();
            HideUntilStarted();
        }

        private void Start()
        {
            if (BreathingInputProvider.Instance != null && BreathingInputProvider.Instance.HasStoredCalibration())
            {
                bool ok = BreathingInputProvider.Instance.TryLoadStoredCalibration();
                if (ok)
                {
                    Debug.Log("[Calibration] Calibracion previa cargada. Salto la pantalla.");
                    phase = Phase.Done;
                    if (canvas != null) canvas.enabled = false;
                    OnCalibrationDone?.Invoke(BreathingInputProvider.Instance.Mode == BreathingInputProvider.InputMode.KeyboardFallback);
                    return;
                }
            }
            BeginCalibration();
        }

        private void HideUntilStarted()
        {
            // arranca oculto, lo activa BeginCalibration
            if (canvas != null) canvas.enabled = false;
        }

        public void RestartCalibration()
        {
            // Si ya esta corriendo, no hacer nada
            if (canvas != null && canvas.enabled) return;
            // Limpia mic anterior si estaba activo
            if (BreathingInputProvider.Instance != null)
                BreathingInputProvider.Instance.StopMic();
            // Resetea acumuladores
            noiseAccum = 0f; exhaleAccum = 0f;
            noiseSamples = 0; exhaleSamples = 0;
            phaseElapsed = 0f;
            BeginCalibration();
        }

        public void BeginCalibration()
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            canvas.enabled = true;
            OverlayBlocker.Register();

            if (BreathingInputProvider.Instance == null)
            {
                Debug.LogWarning("[Calibration] No hay BreathingInputProvider en escena. Salto a fallback.");
                Finish(false, withCalibration: false);
                return;
            }

            if (!BreathingInputProvider.Instance.MicAvailable)
            {
                statusText.text = "No detectamos microfono.\nVamos a usar la tecla ESPACIO.";
                phase = Phase.Done;
                Invoke(nameof(FallbackFinish), 1.6f);
                return;
            }

            if (!BreathingInputProvider.Instance.TryStartMic())
            {
                statusText.text = "No pude abrir el microfono.\nVamos a usar la tecla ESPACIO.";
                phase = Phase.Done;
                Invoke(nameof(FallbackFinish), 1.6f);
                return;
            }

            phase = Phase.WaitStart;
            titleText.text = "CALIBRACION";
            instructionText.text = "Sosten el mando con ambas manos cerca de tu boca,\na unos 10-15 cm. El microfono esta entre los gatillos,\napuntalo hacia tu cara.";
            statusText.text = "Pulsa COMENZAR (o ENTER / Cross) cuando estes listo.";
            UpdateStepVisuals();
            // Mostrar boton Comenzar solo en WaitStart
            if (btnComenzar != null) btnComenzar.gameObject.SetActive(true);
            if (btnSaltar != null) btnSaltar.gameObject.SetActive(true);
        }

        private void FallbackFinish() => Finish(true, withCalibration: false);

        private void Update()
        {
            if (canvas == null || !canvas.enabled) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Finish(true, withCalibration: false);
                return;
            }

            switch (phase)
            {
                case Phase.WaitStart:
                    if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                        || (gp != null && gp.buttonSouth.wasPressedThisFrame))
                    {
                        OnComenzarClicked();
                    }
                    break;

                case Phase.CaptureNoise:
                    AccumulateRms(ref noiseAccum, ref noiseSamples);
                    UpdateProgress(noiseCaptureSeconds);
                    if (phaseElapsed >= noiseCaptureSeconds)
                    {
                        StartPhase(Phase.CaptureExhale);
                        instructionText.text = "Ahora EXHALA suave por la boca\nhacia el mando, durante 3 segundos.";
                        UpdateStepVisuals();
                    }
                    break;

                case Phase.CaptureExhale:
                    AccumulateRms(ref exhaleAccum, ref exhaleSamples);
                    UpdateProgress(exhaleCaptureSeconds);
                    if (phaseElapsed >= exhaleCaptureSeconds)
                    {
                        ApplyAndFinish();
                    }
                    break;
            }
        }

        private void StartPhase(Phase next)
        {
            phase = next;
            phaseElapsed = 0f;
        }

        private void AccumulateRms(ref float accum, ref int count)
        {
            phaseElapsed += Time.unscaledDeltaTime;
            float rms = BreathingInputProvider.Instance.CurrentRms;
            accum += rms;
            count++;
        }

        private void UpdateProgress(float total)
        {
            float t = Mathf.Clamp01(phaseElapsed / total);
            if (levelFill != null)
                levelFill.sizeDelta = new Vector2(levelBaseWidth * t, levelFill.sizeDelta.y);
            statusText.text = $"{Mathf.CeilToInt(total - phaseElapsed)} s restantes\nRMS actual: {BreathingInputProvider.Instance.CurrentRms:F4}";
        }

        private void ApplyAndFinish()
        {
            float noiseAvg = noiseSamples > 0 ? noiseAccum / noiseSamples : 0f;
            float exhaleAvg = exhaleSamples > 0 ? exhaleAccum / exhaleSamples : 0f;
            BreathingInputProvider.Instance.ApplyCalibration(noiseAvg, exhaleAvg);
            bool useFallback = BreathingInputProvider.Instance.Mode == BreathingInputProvider.InputMode.KeyboardFallback;
            Finish(useFallback, withCalibration: !useFallback);
        }

        private void Finish(bool useFallback, bool withCalibration)
        {
            if (phase == Phase.Done) return;
            bool wasShowingPanel = canvas != null && canvas.enabled;
            phase = Phase.Done;
            if (useFallback && BreathingInputProvider.Instance != null)
                BreathingInputProvider.Instance.ForceFallback("usuario o calibracion fallida");
            Time.timeScale = prevTimeScale;
            if (canvas != null) canvas.enabled = false;
            if (wasShowingPanel) OverlayBlocker.Unregister();
            OnCalibrationDone?.Invoke(useFallback);
        }

        private void BuildUI()
        {
            var go = new GameObject("Calibration_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            // Fondo negro 100% solido
            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = panelColor;

            // ----- Titulo -----
            titleText = MakeCenteredText(canvas.transform, "Title", "CALIBRACION",
                64, FontStyle.Bold, accentColor,
                new Vector2(0f, 380f), new Vector2(900f, 90f));
            var titleShadow = titleText.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(3f, -4f);

            // Linea decorativa fina debajo del titulo
            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(canvas.transform, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(280f, 2f);
            lineRT.anchoredPosition = new Vector2(0f, 320f);
            lineGo.AddComponent<Image>().color = accentColor;

            // ----- Step indicator (1) ----- (2) -----
            BuildStepIndicator();

            // ----- Caja de instruccion (rounded) -----
            var instrBgGo = new GameObject("InstrBg", typeof(RectTransform));
            var instrBgRT = instrBgGo.GetComponent<RectTransform>();
            instrBgRT.SetParent(canvas.transform, false);
            instrBgRT.anchorMin = instrBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            instrBgRT.pivot = new Vector2(0.5f, 0.5f);
            instrBgRT.sizeDelta = new Vector2(1000f, 130f);
            instrBgRT.anchoredPosition = new Vector2(0f, 70f);
            var instrBgImg = instrBgGo.AddComponent<Image>();
            instrBgImg.color = new Color(1f, 1f, 1f, 0.04f);
            ApplyRoundedToImage(instrBgImg);

            instructionText = MakeCenteredText(instrBgGo.transform, "Instruction", "",
                22, FontStyle.Normal, textColor,
                Vector2.zero, new Vector2(940f, 110f));

            // ----- Barra de nivel/progreso (rounded) -----
            var barBgGo = new GameObject("LevelBar_BG", typeof(RectTransform));
            var barBgRT = barBgGo.GetComponent<RectTransform>();
            barBgRT.SetParent(canvas.transform, false);
            barBgRT.anchorMin = barBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRT.pivot = new Vector2(0.5f, 0.5f);
            barBgRT.sizeDelta = new Vector2(600f, 24f);
            barBgRT.anchoredPosition = new Vector2(0f, -40f);
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.18f, 0.16f, 0.14f, 0.9f);
            ApplyRoundedToImage(barBgImg);

            var fillGo = new GameObject("LevelBar_Fill", typeof(RectTransform));
            levelFill = fillGo.GetComponent<RectTransform>();
            levelFill.SetParent(barBgRT, false);
            levelFill.anchorMin = new Vector2(0f, 0f);
            levelFill.anchorMax = new Vector2(0f, 1f);
            levelFill.pivot = new Vector2(0f, 0.5f);
            levelFill.anchoredPosition = new Vector2(2f, 0f);
            levelBaseWidth = 596f;
            levelFill.sizeDelta = new Vector2(0f, -4f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = levelColor;
            ApplyRoundedToImage(fillImg);

            // ----- Status text -----
            statusText = MakeCenteredText(canvas.transform, "Status", "",
                18, FontStyle.Italic, labelColor,
                new Vector2(0f, -100f), new Vector2(1100f, 60f));

            // ----- Botones inferiores -----
            btnComenzar = BuildButton(canvas.transform, "BtnComenzar", "Comenzar",
                new Vector2(-130f, -200f), new Vector2(220f, 50f), () => OnComenzarClicked());
            btnSaltar = BuildButton(canvas.transform, "BtnSaltar", "Saltar (usar teclado)",
                new Vector2(140f, -200f), new Vector2(280f, 50f), () => OnSaltarClicked());
        }

        private void BuildStepIndicator()
        {
            // 2 circulos numerados conectados por una linea, debajo del titulo
            float spacing = 260f;
            float yPos = 220f;

            step1Bg = BuildStepCircle("Step1", new Vector2(-spacing * 0.5f, yPos), "1", out step1Lbl);

            // Linea entre los dos
            var lineGo = new GameObject("StepLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(canvas.transform, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(spacing - 90f, 3f);
            lineRT.anchoredPosition = new Vector2(0f, yPos);
            stepLine = lineGo.AddComponent<Image>();
            stepLine.color = stepInactiveBg;

            step2Bg = BuildStepCircle("Step2", new Vector2(spacing * 0.5f, yPos), "2", out step2Lbl);

            // Etiquetas de cada paso
            MakeCenteredText(canvas.transform, "Step1Label", "ruido ambiente",
                14, FontStyle.Normal, labelColor,
                new Vector2(-spacing * 0.5f, yPos - 50f), new Vector2(180f, 20f));
            MakeCenteredText(canvas.transform, "Step2Label", "exhalacion",
                14, FontStyle.Normal, labelColor,
                new Vector2(spacing * 0.5f, yPos - 50f), new Vector2(180f, 20f));
        }

        private Image BuildStepCircle(string name, Vector2 anchoredPos, string number, out Text labelOut)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(60f, 60f);
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.sprite = GetCircleSprite();
            img.color = stepInactiveBg;

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            labelOut = lblGo.AddComponent<Text>();
            labelOut.font = GetBuiltinFont();
            labelOut.text = number;
            labelOut.alignment = TextAnchor.MiddleCenter;
            labelOut.fontSize = 26;
            labelOut.fontStyle = FontStyle.Bold;
            labelOut.color = textColor;
            labelOut.raycastTarget = false;
            return img;
        }

        private void UpdateStepVisuals()
        {
            // Step 1 (CaptureNoise) activo cuando phase == CaptureNoise.
            // Step 2 (CaptureExhale) activo cuando phase == CaptureExhale.
            // Ambos completados (ambar) en Done.
            bool s1Active = phase == Phase.CaptureNoise || phase == Phase.CaptureExhale || phase == Phase.Done;
            bool s2Active = phase == Phase.CaptureExhale || phase == Phase.Done;

            if (step1Bg != null) step1Bg.color = s1Active ? stepActiveBg : stepInactiveBg;
            if (step2Bg != null) step2Bg.color = s2Active ? stepActiveBg : stepInactiveBg;
            if (stepLine != null) stepLine.color = s2Active ? stepActiveBg : stepInactiveBg;
            if (step1Lbl != null) step1Lbl.color = s1Active ? new Color(0.05f, 0.04f, 0.02f, 1f) : textColor;
            if (step2Lbl != null) step2Lbl.color = s2Active ? new Color(0.05f, 0.04f, 0.02f, 1f) : textColor;
        }

        private Button BuildButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Vector2 size, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = buttonNormalColor;
            ApplyRoundedToImage(img);
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
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.font = GetBuiltinFont();
            lblTxt.text = label;
            lblTxt.alignment = TextAnchor.MiddleCenter;
            lblTxt.fontSize = 18;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color = textColor;
            lblTxt.raycastTarget = false;
            return btn;
        }

        private void OnComenzarClicked()
        {
            // Solo tiene efecto en WaitStart (avanza a CaptureNoise)
            if (phase != Phase.WaitStart) return;
            StartPhase(Phase.CaptureNoise);
            instructionText.text = "Respira NORMAL por la nariz durante 3 segundos.\nNo soples al mando, solo respira tranquilo.";
            UpdateStepVisuals();
            if (btnComenzar != null) btnComenzar.gameObject.SetActive(false);
        }

        private void OnSaltarClicked()
        {
            Finish(true, withCalibration: false);
        }

        private Text MakeCenteredText(Transform parent, string name, string content,
            int size, FontStyle style, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = GetBuiltinFont();
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Font GetBuiltinFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        private Sprite circleSpriteCache;

        private Sprite GetCircleSprite()
        {
            if (circleSpriteCache != null) return circleSpriteCache;
            const int size = 96;
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
            circleSpriteCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circleSpriteCache;
        }

        private void ApplyRoundedToImage(Image img)
        {
            if (img == null) return;
            if (roundedRectCache == null)
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
                roundedRectCache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            }
            img.sprite = roundedRectCache;
            img.type = Image.Type.Sliced;
        }
    }
}
