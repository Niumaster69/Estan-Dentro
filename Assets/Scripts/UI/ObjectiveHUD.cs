using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EstanDentro.Inventory;

namespace EstanDentro.UI
{
    /// <summary>
    /// HUD minimalista con DOS elementos:
    ///
    ///   1) CIRCULO top-right (siempre visible, no invasivo)
    ///      - Click: abre el InventoryOverlay con tab MISIONES
    ///      - Pulsa cuando se agrega/cambia algo
    ///
    ///   2) TOAST bottom-center (auto-desaparece)
    ///      Notify("..."): slide up + hold + slide down
    ///
    /// API:
    ///   - ObjectiveHUD.Notify("Has encontrado un destornillador", 4f)
    ///   - ObjectiveHUD.PulseCircle()  -> attention pulse
    ///
    /// Para misiones u objetivos persistentes: usar Inventory.Instance.AddMission(...)
    /// directamente. La lista se ve cuando el jugador abre el inventario clickeando el circulo.
    ///
    /// Aliases backwards-compat (ahora delegan a Inventory.Missions):
    ///   - SetObjective(text)   -> Inventory.AddMission("legacy", text, Principal)
    ///   - SetPersistent(text)  -> idem
    ///   - ClearObjective()     -> Inventory.RemoveMission("legacy")
    ///   - Show(text, time)     -> Notify
    ///   - Hide()               -> ClearObjective + cancel toast
    /// </summary>
    public class ObjectiveHUD : MonoBehaviour
    {
        private static ObjectiveHUD instance;

        [Header("Paleta")]
        [SerializeField] private Color amber = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color amberSoft = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color textCream = new Color(0.95f, 0.93f, 0.85f, 1f);
        [SerializeField] private Color circleIdleBg = new Color(0.10f, 0.09f, 0.07f, 0.9f);
        [SerializeField] private Color circleHoverBg = new Color(0.20f, 0.16f, 0.10f, 1f);
        [SerializeField] private Color toastPanelColor = new Color(0.10f, 0.09f, 0.07f, 0.95f);

        [Header("Circulo (top-right)")]
        [SerializeField] private float circleSize = 60f;
        [SerializeField] private Vector2 circleOffset = new Vector2(-30f, -30f);
        [SerializeField] private float circlePulseScale = 1.4f;
        [SerializeField] private float circlePulseDuration = 1.0f;

        [Header("Toast (bottom-center)")]
        [SerializeField] private float toastWidth = 640f;
        [SerializeField] private float toastHeight = 80f;
        [SerializeField] private float toastBottomY = 140f;
        [SerializeField] private float toastFadeIn = 0.35f;
        [SerializeField] private float toastFadeOut = 0.7f;
        [SerializeField] private float toastDefaultDisplayTime = 4f;
        [SerializeField] private float toastSlideDistance = 60f;

        // ---------- runtime ----------

        private Canvas canvas;
        private RectTransform circleRT;
        private Image circleBg;
        private Text circleIcon;
        private Button circleBtn;
        private RectTransform toastPanelRT;
        private CanvasGroup toastGroup;
        private Text toastText;
        private Coroutine toastRoutine;
        private Coroutine pulseRoutine;

        // ---------- API publica ----------

        public static void Notify(string text, float displayTime = -1f)
        {
            EnsureInstance();
            instance.NotifyInternal(text, displayTime);
        }

        public static void PulseCircle()
        {
            EnsureInstance();
            instance.PulseInternal();
        }

        // ---------- aliases backwards-compat ----------

        public static void SetObjective(string text)
        {
            EnsureInstance();
            if (Inventory.Inventory.Instance != null)
                Inventory.Inventory.Instance.AddMission("legacy_objective", text, Inventory.Inventory.MissionCategory.Principal);
            instance.PulseInternal();
            Notify(text, 5f);
        }

        public static void SetPersistent(string text) => SetObjective(text);

        public static void ClearObjective()
        {
            if (Inventory.Inventory.Instance != null)
                Inventory.Inventory.Instance.RemoveMission("legacy_objective");
        }

        public static void Show(string text, float displayTime = -1f) => Notify(text, displayTime);

        public static void Hide()
        {
            ClearObjective();
            if (instance != null && instance.toastRoutine != null)
                instance.StartCoroutine(instance.ToastFadeOutOnly());
        }

        // ---------- lifecycle ----------

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("__ObjectiveHUD");
            instance = go.AddComponent<ObjectiveHUD>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
            if (toastGroup != null) toastGroup.alpha = 0f;
        }

        // ---------- circulo ----------

        private void OnCircleClicked()
        {
            // Abre el InventoryOverlay en el tab MISIONES
            InventoryOverlay.OpenTab(InventoryOverlay.Tab.Misiones);
        }

        private void PulseInternal()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float t = 0f;
            float half = circlePulseDuration * 0.5f;
            Vector3 baseScale = Vector3.one;
            Vector3 peak = Vector3.one * circlePulseScale;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                circleRT.localScale = Vector3.Lerp(baseScale, peak, EaseOutCubic(t / half));
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                circleRT.localScale = Vector3.Lerp(peak, baseScale, EaseOutCubic(t / half));
                yield return null;
            }
            circleRT.localScale = baseScale;
            pulseRoutine = null;
        }

        // ---------- toast ----------

        private void NotifyInternal(string text, float displayTime)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (displayTime < 0f) displayTime = toastDefaultDisplayTime;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(text, displayTime));
        }

        private IEnumerator ToastRoutine(string text, float displayTime)
        {
            toastText.text = text;

            Vector2 endPos = new Vector2(0f, toastBottomY);
            Vector2 startPos = endPos + new Vector2(0f, -toastSlideDistance);
            toastPanelRT.anchoredPosition = startPos;

            float t = 0f;
            while (t < toastFadeIn)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / toastFadeIn);
                float eased = EaseOutCubic(p);
                toastGroup.alpha = eased;
                toastPanelRT.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                yield return null;
            }
            toastGroup.alpha = 1f;
            toastPanelRT.anchoredPosition = endPos;

            yield return new WaitForSecondsRealtime(displayTime);

            Vector2 outPos = endPos + new Vector2(0f, -toastSlideDistance * 0.5f);
            t = 0f;
            while (t < toastFadeOut)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / toastFadeOut);
                toastGroup.alpha = 1f - p;
                toastPanelRT.anchoredPosition = Vector2.Lerp(endPos, outPos, p);
                yield return null;
            }
            toastGroup.alpha = 0f;
            toastRoutine = null;
        }

        private IEnumerator ToastFadeOutOnly()
        {
            float t = 0f;
            float startAlpha = toastGroup.alpha;
            while (t < toastFadeOut)
            {
                t += Time.unscaledDeltaTime;
                toastGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / toastFadeOut));
                yield return null;
            }
            toastGroup.alpha = 0f;
            toastRoutine = null;
        }

        // ---------- helpers ----------

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        // ---------- build UI ----------

        private void BuildUI()
        {
            var canvasGo = new GameObject("ObjectiveHUD_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 175;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            BuildCircle(canvas.transform);
            BuildToastPanel(canvas.transform);
        }

        private void BuildCircle(Transform parent)
        {
            var circleGo = new GameObject("ObjectiveCircle", typeof(RectTransform));
            circleRT = circleGo.GetComponent<RectTransform>();
            circleRT.SetParent(parent, false);
            circleRT.anchorMin = new Vector2(1f, 1f);
            circleRT.anchorMax = new Vector2(1f, 1f);
            circleRT.pivot = new Vector2(1f, 1f);
            circleRT.sizeDelta = new Vector2(circleSize, circleSize);
            circleRT.anchoredPosition = circleOffset;

            circleBg = circleGo.AddComponent<Image>();
            circleBg.sprite = GetCircleSprite();
            circleBg.color = circleIdleBg;
            circleBg.raycastTarget = true;
            var outline = circleGo.AddComponent<Outline>();
            outline.effectColor = amberSoft;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            circleBtn = circleGo.AddComponent<Button>();
            circleBtn.targetGraphic = circleBg;
            var c = circleBtn.colors;
            c.normalColor = circleIdleBg;
            c.highlightedColor = circleHoverBg;
            c.pressedColor = amber;
            c.selectedColor = circleHoverBg;
            c.fadeDuration = 0.15f;
            circleBtn.colors = c;
            circleBtn.onClick.AddListener(OnCircleClicked);

            // Icono
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.SetParent(circleRT, false);
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
            circleIcon = iconGo.AddComponent<Text>();
            circleIcon.font = GetBodyFont();
            circleIcon.text = "✦";
            circleIcon.alignment = TextAnchor.MiddleCenter;
            circleIcon.fontSize = Mathf.RoundToInt(circleSize * 0.5f);
            circleIcon.fontStyle = FontStyle.Bold;
            circleIcon.color = amber;
            circleIcon.raycastTarget = false;
        }

        private void BuildToastPanel(Transform parent)
        {
            var panelGo = new GameObject("ToastPanel", typeof(RectTransform));
            toastPanelRT = panelGo.GetComponent<RectTransform>();
            toastPanelRT.SetParent(parent, false);
            toastPanelRT.anchorMin = new Vector2(0.5f, 0f);
            toastPanelRT.anchorMax = new Vector2(0.5f, 0f);
            toastPanelRT.pivot = new Vector2(0.5f, 0f);
            toastPanelRT.sizeDelta = new Vector2(toastWidth, toastHeight);
            toastPanelRT.anchoredPosition = new Vector2(0f, toastBottomY);

            toastGroup = panelGo.AddComponent<CanvasGroup>();
            toastGroup.interactable = false;
            toastGroup.blocksRaycasts = false;

            var bg = panelGo.AddComponent<Image>();
            bg.color = toastPanelColor;
            bg.raycastTarget = false;
            ApplyRounded(bg);

            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = amberSoft;
            outline.effectDistance = new Vector2(2f, -2f);

            // Icono check
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.SetParent(toastPanelRT, false);
            iconRT.anchorMin = new Vector2(0f, 0.5f);
            iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0f, 0.5f);
            iconRT.sizeDelta = new Vector2(48f, 48f);
            iconRT.anchoredPosition = new Vector2(16f, 0f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = GetCircleSprite();
            iconImg.color = amber;
            iconImg.raycastTarget = false;

            var iconCharGo = new GameObject("IconChar", typeof(RectTransform));
            var iconCharRT = iconCharGo.GetComponent<RectTransform>();
            iconCharRT.SetParent(iconRT, false);
            iconCharRT.anchorMin = Vector2.zero; iconCharRT.anchorMax = Vector2.one;
            iconCharRT.offsetMin = Vector2.zero; iconCharRT.offsetMax = Vector2.zero;
            var iconChar = iconCharGo.AddComponent<Text>();
            iconChar.font = GetBodyFont();
            iconChar.text = "✓";
            iconChar.alignment = TextAnchor.MiddleCenter;
            iconChar.fontSize = 32;
            iconChar.fontStyle = FontStyle.Bold;
            iconChar.color = new Color(0.05f, 0.04f, 0.02f, 1f);
            iconChar.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.SetParent(toastPanelRT, false);
            textRT.anchorMin = new Vector2(0f, 0f);
            textRT.anchorMax = new Vector2(1f, 1f);
            textRT.offsetMin = new Vector2(80f, 8f);
            textRT.offsetMax = new Vector2(-20f, -8f);
            toastText = textGo.AddComponent<Text>();
            toastText.font = GetBodyFont();
            toastText.text = "";
            toastText.alignment = TextAnchor.MiddleLeft;
            toastText.fontSize = 22;
            toastText.fontStyle = FontStyle.Bold;
            toastText.color = textCream;
            toastText.raycastTarget = false;
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        // ---------- sprites ----------

        private Sprite roundedSpriteCache;
        private Sprite circleSpriteCache;

        private void ApplyRounded(Image img)
        {
            if (roundedSpriteCache == null)
            {
                const int size = 32;
                const int radius = 10;
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

        private static Font GetBodyFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
