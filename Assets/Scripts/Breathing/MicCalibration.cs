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
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.92f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color levelColor = new Color(0.85f, 0.71f, 0.28f, 1f);

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
            titleText.text = "CALIBRACION DE RESPIRACION";
            instructionText.text = "Sosten el mando con ambas manos cerca de tu boca,\na unos 10-15 cm. El microfono esta entre los gatillos,\napuntalo hacia tu cara.";
            statusText.text = "Pulsa ENTER (o Cross del mando) para empezar.\nPulsa ESC para usar la tecla ESPACIO en su lugar.";
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
                        StartPhase(Phase.CaptureNoise);
                        instructionText.text = "Respira NORMAL por la nariz durante 3 segundos.\nNO soples al mando, solo respira tranquilo.";
                    }
                    break;

                case Phase.CaptureNoise:
                    AccumulateRms(ref noiseAccum, ref noiseSamples);
                    UpdateProgress(noiseCaptureSeconds);
                    if (phaseElapsed >= noiseCaptureSeconds)
                    {
                        StartPhase(Phase.CaptureExhale);
                        instructionText.text = "Ahora EXHALA suave por la boca\nhacia el mando, durante 3 segundos.";
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

            // Fondo
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = panelColor;

            // Titulo
            titleText = MakeText(canvas.transform, "Title", "CALIBRACION DE MICROFONO",
                32, FontStyle.Bold,
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(900f, 50f));

            // Instruccion
            instructionText = MakeText(canvas.transform, "Instruction", "",
                22, FontStyle.Normal,
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(1100f, 110f));

            // Barra de progreso
            var barGo = new GameObject("LevelBar_BG");
            barGo.transform.SetParent(canvas.transform, false);
            var barRT = barGo.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.5f, 0.42f);
            barRT.anchorMax = new Vector2(0.5f, 0.42f);
            barRT.pivot = new Vector2(0.5f, 0.5f);
            barRT.sizeDelta = new Vector2(600f, 26f);
            barGo.AddComponent<Image>().color = new Color(1, 1, 1, 0.18f);

            var fillGo = new GameObject("LevelBar_Fill");
            fillGo.transform.SetParent(barRT, false);
            levelFill = fillGo.AddComponent<RectTransform>();
            levelFill.anchorMin = new Vector2(0f, 0f);
            levelFill.anchorMax = new Vector2(0f, 1f);
            levelFill.pivot = new Vector2(0f, 0.5f);
            levelFill.anchoredPosition = new Vector2(2f, 0f);
            levelBaseWidth = 596f;
            levelFill.sizeDelta = new Vector2(0f, -4f);
            fillGo.AddComponent<Image>().color = levelColor;

            // Status
            statusText = MakeText(canvas.transform, "Status", "",
                20, FontStyle.Normal,
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(1100f, 90f));
        }

        private Text MakeText(Transform parent, string name, string content,
            int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = textColor;
            return t;
        }
    }
}
