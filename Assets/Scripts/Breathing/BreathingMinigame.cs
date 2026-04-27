using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.Stress;

namespace EstanDentro.Breathing
{
    [DefaultExecutionOrder(-30)]
    public class BreathingMinigame : MonoBehaviour
    {
        [Header("Activacion")]
        [SerializeField, Range(0f, 1f)] private float showAtStressNormalized = 0.4f;
        [SerializeField, Range(0f, 1f)] private float hideAtStressNormalized = 0.25f;
        [SerializeField] private bool alwaysVisibleForDebug = false;

        [Header("Ciclo (segundos)")]
        [SerializeField] private float inhaleSeconds = 4f;
        [SerializeField] private float exhaleSeconds = 4f;
        [SerializeField] private float pauseSeconds = 2f;
        [SerializeField, Tooltip("Tiempo minimo (s) de exhale sostenido para considerar ciclo OK.")]
        private float exhaleMinSustainSeconds = 2f;
        [SerializeField, Tooltip("Fallback teclado: tiempo minimo de Space presionado en INHALA para que el ciclo cuente.")]
        private float minInhaleHeldSeconds = 3f;

        [Header("Recompensa / castigo (puntos de estres)")]
        [SerializeField] private float stressDownOnSuccess = 9f;
        [SerializeField] private float stressUpOnFail = 1f;
        [SerializeField, Tooltip("Cantidad de ciclos al inicio donde fallar NO suma estres (modo aprendizaje).")]
        private int freeCyclesAtStart = 2;

        [Header("Tutorial primera aparicion")]
        [SerializeField] private bool showTutorialFirstTime = true;
        [SerializeField, TextArea(3, 8)] private string tutorialMessage =
            "El miedo subio tu estres.\n\n" +
            "Manten ESPACIO (teclado) o TRIANGLE (mando) mientras inhalas por la nariz.\n" +
            "Soltalo cuando exhalas suave por la boca.\n\n" +
            "Respira con el ritmo del circulo, no con tu reloj interno.";

        [Header("Visual")]
        [SerializeField] private float minScale = 0.45f;
        [SerializeField] private float maxScale = 1.0f;
        [SerializeField] private int circleSize = 220;
        [SerializeField] private float fadeSeconds = 1f;
        [SerializeField] private Color inhaleColor = new Color(0.4f, 0.65f, 0.85f, 0.85f);
        [SerializeField] private Color exhaleColor = new Color(0.55f, 0.85f, 0.6f, 0.85f);
        [SerializeField] private Color pauseColor = new Color(0.7f, 0.7f, 0.7f, 0.55f);
        [SerializeField] private Color labelColor = new Color(0.95f, 0.93f, 0.85f, 0.95f);
        [SerializeField] private Color freeCycleBadgeColor = new Color(0.4f, 0.7f, 0.45f, 0.95f);

        private enum Phase { Inhale, Exhale, Pause }
        private Phase phase = Phase.Inhale;
        private float phaseElapsed;
        private float exhaleSustained;
        private float inhaleHeldAccum;
        private bool cycleAlreadyScored;
        private int remainingFreeCycles;

        // UI
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform circleRT;
        private Image circleImg;
        private Text guideText;
        private Text hintText;
        private Text inputBadge;
        private Text freeCycleBadge;

        // Tutorial UI
        private GameObject tutorialPanel;
        private Text tutorialText;

        // State
        private bool visible;
        private bool tutorialPresentedThisSession;
        private bool tutorialActive;
        private float currentAlpha;
        private float targetAlpha;
        private float prevTimeScaleBeforeTutorial;
        private bool consumeTutorialInputThisFrame;

        private void Awake()
        {
            BuildUI();
            if (canvas != null) canvas.enabled = false;
            visible = false;
            currentAlpha = 0f;
            targetAlpha = 0f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            remainingFreeCycles = freeCyclesAtStart;
        }

        private void Update()
        {
            UpdateAlpha();
            UpdateVisibility();

            if (tutorialActive)
            {
                HandleTutorialInput();
                return;
            }

            if (!visible) return;
            TickCycle();
            ApplyVisuals();
        }

        // ---------- visibility / fade ----------

        private void UpdateAlpha()
        {
            if (canvasGroup == null) return;
            if (Mathf.Approximately(currentAlpha, targetAlpha)) return;
            float speed = fadeSeconds <= 0f ? 99f : (1f / fadeSeconds);
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.unscaledDeltaTime);
            canvasGroup.alpha = currentAlpha;

            if (currentAlpha <= 0.001f && targetAlpha == 0f && canvas != null && canvas.enabled)
                canvas.enabled = false;
        }

        private void UpdateVisibility()
        {
            if (alwaysVisibleForDebug) { TryShow(); return; }
            if (StressSystem.Instance == null) { TryHide(); return; }
            float t = StressSystem.Instance.Normalized;
            if (!visible && t >= showAtStressNormalized) TryShow();
            else if (visible && t <= hideAtStressNormalized) TryHide();
        }

        private void TryShow()
        {
            if (visible) return;
            visible = true;
            if (canvas != null) canvas.enabled = true;

            bool needsTutorial = showTutorialFirstTime && !tutorialPresentedThisSession;
            if (needsTutorial) StartTutorial();
            else BeginCycleNow();
        }

        private void TryHide()
        {
            if (!visible) return;
            visible = false;
            targetAlpha = 0f;
        }

        private void BeginCycleNow()
        {
            ResetCycle();
            targetAlpha = 1f;
            // sin pausar
        }

        private void ResetCycle()
        {
            phase = Phase.Inhale;
            phaseElapsed = 0f;
            exhaleSustained = 0f;
            inhaleHeldAccum = 0f;
            cycleAlreadyScored = false;
        }

        // ---------- tutorial ----------

        private void StartTutorial()
        {
            tutorialActive = true;
            consumeTutorialInputThisFrame = true;
            prevTimeScaleBeforeTutorial = Time.timeScale;
            Time.timeScale = 0f;

            tutorialPanel.SetActive(true);
            tutorialText.text = tutorialMessage + "\n\n[Pulsa cualquier tecla / boton para empezar]";

            // Fade-in inmediato del canvas (alpha 1)
            currentAlpha = 1f;
            targetAlpha = 1f;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void HandleTutorialInput()
        {
            if (consumeTutorialInputThisFrame) { consumeTutorialInputThisFrame = false; return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool any = (kb != null && kb.anyKey.wasPressedThisFrame)
                    || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                      || gp.buttonNorth.wasPressedThisFrame
                                      || gp.buttonEast.wasPressedThisFrame
                                      || gp.buttonWest.wasPressedThisFrame
                                      || gp.startButton.wasPressedThisFrame));
            if (!any) return;

            tutorialActive = false;
            tutorialPresentedThisSession = true;
            tutorialPanel.SetActive(false);
            Time.timeScale = prevTimeScaleBeforeTutorial > 0f ? prevTimeScaleBeforeTutorial : 1f;
            BeginCycleNow();
        }

        // ---------- cycle ----------

        private void TickCycle()
        {
            phaseElapsed += Time.deltaTime;
            var provider = BreathingInputProvider.Instance;
            bool isFallback = provider != null && provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback;

            switch (phase)
            {
                case Phase.Inhale:
                    if (isFallback && BreathingInputProvider.FallbackInhaleHeld())
                        inhaleHeldAccum += Time.deltaTime;
                    if (phaseElapsed >= inhaleSeconds) Advance(Phase.Exhale);
                    break;
                case Phase.Exhale:
                    bool exhaleAllowed = !isFallback || inhaleHeldAccum >= minInhaleHeldSeconds;
                    if (exhaleAllowed && provider != null && provider.IsExhalingNow())
                        exhaleSustained += Time.deltaTime;
                    if (!cycleAlreadyScored && exhaleSustained >= exhaleMinSustainSeconds)
                    {
                        ScoreSuccess();
                        cycleAlreadyScored = true;
                    }
                    if (phaseElapsed >= exhaleSeconds)
                    {
                        if (!cycleAlreadyScored) ScoreFail();
                        Advance(Phase.Pause);
                    }
                    break;
                case Phase.Pause:
                    if (phaseElapsed >= pauseSeconds) Advance(Phase.Inhale);
                    break;
            }
        }

        private void Advance(Phase next)
        {
            phase = next;
            phaseElapsed = 0f;
            if (next == Phase.Inhale)
            {
                exhaleSustained = 0f;
                inhaleHeldAccum = 0f;
                cycleAlreadyScored = false;
            }
        }

        private void ScoreSuccess()
        {
            if (StressSystem.Instance == null) return;
            StressSystem.Instance.Add(-stressDownOnSuccess);
            Debug.Log($"[Breathing] Ciclo OK -{stressDownOnSuccess}. Estres={StressSystem.Instance.CurrentStress:F0}");
        }

        private void ScoreFail()
        {
            if (StressSystem.Instance == null) return;
            if (remainingFreeCycles > 0)
            {
                remainingFreeCycles--;
                Debug.Log($"[Breathing] Ciclo FAIL (sin penalty - aprendizaje). Quedan {remainingFreeCycles} libres.");
                return;
            }
            StressSystem.Instance.Add(stressUpOnFail);
            Debug.Log($"[Breathing] Ciclo FAIL +{stressUpOnFail}. Estres={StressSystem.Instance.CurrentStress:F0}");
        }

        // ---------- visuals ----------

        private void ApplyVisuals()
        {
            float scale;
            Color c;
            string guide;
            switch (phase)
            {
                case Phase.Inhale:
                    scale = Mathf.Lerp(minScale, maxScale, phaseElapsed / inhaleSeconds);
                    c = inhaleColor;
                    guide = "INHALA";
                    break;
                case Phase.Exhale:
                    scale = Mathf.Lerp(maxScale, minScale, phaseElapsed / exhaleSeconds);
                    c = exhaleColor;
                    guide = cycleAlreadyScored ? "EXHALA  ✓" : "EXHALA";
                    break;
                default:
                    scale = minScale;
                    c = pauseColor;
                    guide = "PAUSA";
                    break;
            }
            circleRT.localScale = new Vector3(scale, scale, 1f);
            circleImg.color = c;
            guideText.text = guide;

            UpdateInputBadge();
            UpdateHintAndFreeCycleBadge();
        }

        private void UpdateInputBadge()
        {
            var provider = BreathingInputProvider.Instance;
            bool isFallback = provider == null || provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback;
            if (!isFallback)
            {
                inputBadge.text = phase == Phase.Inhale ? "[ Inhala por la nariz ]"
                                : phase == Phase.Exhale ? "[ Exhala al MANDO ]"
                                : "";
                return;
            }
            // Fallback: mostrar tecla
            switch (phase)
            {
                case Phase.Inhale: inputBadge.text = "[ Mantener ESPACIO / TRIANGLE ]"; break;
                case Phase.Exhale: inputBadge.text = "[ Soltar ESPACIO / TRIANGLE ]"; break;
                default: inputBadge.text = ""; break;
            }
        }

        private void UpdateHintAndFreeCycleBadge()
        {
            var provider = BreathingInputProvider.Instance;
            hintText.text = provider != null && provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback
                ? "El miedo es real, pero no manda. Respira con el circulo."
                : "Sosten el mando cerca de tu boca. Inhala por la nariz, exhala al mando.";

            if (remainingFreeCycles > 0)
            {
                freeCycleBadge.gameObject.SetActive(true);
                freeCycleBadge.text = $"Aprendiendo - {remainingFreeCycles} ciclo(s) sin penalty";
                freeCycleBadge.color = freeCycleBadgeColor;
            }
            else
            {
                freeCycleBadge.gameObject.SetActive(false);
            }
        }

        // ---------- UI build ----------

        private void BuildUI()
        {
            var go = new GameObject("Breathing_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Circulo
            var circleGo = new GameObject("Breathing_Circle");
            circleGo.transform.SetParent(canvas.transform, false);
            circleRT = circleGo.AddComponent<RectTransform>();
            circleRT.anchorMin = new Vector2(0.5f, 0.5f);
            circleRT.anchorMax = new Vector2(0.5f, 0.5f);
            circleRT.pivot = new Vector2(0.5f, 0.5f);
            circleRT.sizeDelta = new Vector2(circleSize, circleSize);
            circleImg = circleGo.AddComponent<Image>();
            circleImg.sprite = CreateCircleSprite(128);
            circleImg.color = inhaleColor;

            guideText = MakeText(canvas.transform, "Guide", "INHALA",
                34, FontStyle.Bold,
                new Vector2(400f, 50f), Vector2.zero);

            inputBadge = MakeText(canvas.transform, "InputBadge", "",
                22, FontStyle.Bold,
                new Vector2(700f, 36f), new Vector2(0f, -150f));

            hintText = MakeText(canvas.transform, "Hint", "",
                18, FontStyle.Italic,
                new Vector2(900f, 28f), new Vector2(0f, -200f));

            freeCycleBadge = MakeText(canvas.transform, "FreeCycleBadge", "",
                16, FontStyle.Normal,
                new Vector2(500f, 24f), new Vector2(0f, 180f));
            freeCycleBadge.color = freeCycleBadgeColor;
            freeCycleBadge.gameObject.SetActive(false);

            BuildTutorialPanel();
        }

        private void BuildTutorialPanel()
        {
            tutorialPanel = new GameObject("Tutorial_Panel");
            tutorialPanel.transform.SetParent(canvas.transform, false);
            var rt = tutorialPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            tutorialPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

            tutorialText = MakeText(tutorialPanel.transform, "Tutorial_Text", "",
                26, FontStyle.Normal,
                new Vector2(1200f, 600f), Vector2.zero);
            tutorialText.alignment = TextAnchor.MiddleCenter;

            tutorialPanel.SetActive(false);
        }

        private Sprite CreateCircleSprite(int size)
        {
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

        private Text MakeText(Transform parent, string name, string content,
            int size, FontStyle style, Vector2 sizeDelta, Vector2 anchoredPos)
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = labelColor;
            return t;
        }
    }
}
