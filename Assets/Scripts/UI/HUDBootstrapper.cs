using UnityEngine;
using UnityEngine.UI;
using EstanDentro.Stress;

namespace EstanDentro.UI
{
    [DefaultExecutionOrder(-50)]
    public class HUDBootstrapper : MonoBehaviour
    {
        [Header("Layout barra estres")]
        [SerializeField] private Vector2 panelSize = new Vector2(260f, 30f);
        [SerializeField] private Vector2 panelOffset = new Vector2(28f, 28f);
        [SerializeField] private int padding = 3;

        [Header("Colores")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color labelColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color fillCalmColor = new Color(0.23f, 0.42f, 0.28f, 0.95f);
        [SerializeField] private Color fillTenseColor = new Color(0.86f, 0.71f, 0.28f, 0.95f);
        [SerializeField] private Color fillPanicColor = new Color(0.62f, 0.12f, 0.12f, 1f);

        [Header("Pulso al panico")]
        [SerializeField, Range(0f, 1f)] private float pulseStartNormalized = 0.7f;
        [SerializeField] private float pulseSpeed = 5f;
        [SerializeField, Range(0f, 0.3f)] private float pulseAmplitude = 0.08f;

        [Header("Tipografia")]
        [SerializeField] private int fontSize = 14;
        [SerializeField] private string labelPrefix = "ESTRES";

        private Canvas canvas;
        private RectTransform fillRect;
        private Image fillImage;
        private Text valueText;
        private float baseFillWidth;
        private bool subscribed;

        private void Awake()
        {
            BuildCanvas();
            BuildBar();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (subscribed && StressSystem.Instance != null)
                StressSystem.Instance.OnStressChanged -= HandleStressChanged;
            subscribed = false;
        }

        private void TrySubscribe()
        {
            if (subscribed || StressSystem.Instance == null) return;
            StressSystem.Instance.OnStressChanged += HandleStressChanged;
            subscribed = true;
            HandleStressChanged(StressSystem.Instance.CurrentStress, StressSystem.Instance.MaxStress);
        }

        private void BuildCanvas()
        {
            var go = new GameObject("HUD_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        private void BuildBar()
        {
            var bgGo = new GameObject("StressBar_BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0f);
            bgRT.anchorMax = new Vector2(0f, 0f);
            bgRT.pivot = new Vector2(0f, 0f);
            bgRT.anchoredPosition = panelOffset;
            bgRT.sizeDelta = panelSize;
            var bg = bgGo.AddComponent<Image>();
            bg.color = backgroundColor;

            baseFillWidth = panelSize.x - padding * 2;
            float fillHeight = panelSize.y - padding * 2;

            var fillGo = new GameObject("StressBar_Fill");
            fillGo.transform.SetParent(bgRT, false);
            fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(padding, 0f);
            fillRect.sizeDelta = new Vector2(0f, fillHeight);
            fillImage = fillGo.AddComponent<Image>();
            fillImage.color = fillCalmColor;

            var txtGo = new GameObject("StressBar_Label");
            txtGo.transform.SetParent(bgRT, false);
            var txtRT = txtGo.AddComponent<RectTransform>();
            txtRT.anchorMin = new Vector2(0f, 0f);
            txtRT.anchorMax = new Vector2(1f, 1f);
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            valueText = txtGo.AddComponent<Text>();
            valueText.font = GetDefaultFont();
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.fontSize = fontSize;
            valueText.color = labelColor;
            valueText.text = labelPrefix + "  0";
        }

        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        private void HandleStressChanged(float current, float max)
        {
            float t = Mathf.Clamp01(current / Mathf.Max(0.0001f, max));
            fillRect.sizeDelta = new Vector2(baseFillWidth * t, fillRect.sizeDelta.y);
            fillImage.color = StressColor(t);
            if (valueText != null)
                valueText.text = labelPrefix + "  " + Mathf.RoundToInt(current);
        }

        private Color StressColor(float t)
        {
            if (t < 0.5f) return Color.Lerp(fillCalmColor, fillTenseColor, t / 0.5f);
            return Color.Lerp(fillTenseColor, fillPanicColor, (t - 0.5f) / 0.5f);
        }

        private void Update()
        {
            if (StressSystem.Instance == null || fillImage == null) return;
            float t = StressSystem.Instance.Normalized;
            if (t < pulseStartNormalized)
            {
                fillImage.transform.localScale = Vector3.one;
                return;
            }
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            fillImage.transform.localScale = new Vector3(1f, pulse, 1f);
        }
    }
}
