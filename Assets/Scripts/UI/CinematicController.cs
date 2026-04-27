using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace EstanDentro.UI
{
    /// <summary>
    /// Reproduce una secuencia de slides de texto (estilo "novela visual" / cinematica).
    /// Texto aparece letra por letra (typewriter), pausa, fade-out, siguiente slide.
    /// Skip con cualquier boton del teclado o gamepad.
    /// Al terminar, carga la escena `nextSceneName` con SceneTransition.
    /// </summary>
    public class CinematicController : MonoBehaviour
    {
        [System.Serializable]
        public class Slide
        {
            [TextArea(2, 8)] public string text;
            [Tooltip("Velocidad del typewriter. 0 = aparece de golpe.")]
            public float charsPerSecond = 28f;
            [Tooltip("Segundos a mostrar despues de terminar el typewriter.")]
            public float holdSeconds = 2.5f;
            [Tooltip("Segundos de fade out al terminar.")]
            public float fadeOutSeconds = 0.6f;
        }

        [Header("Slides")]
        [SerializeField]
        private Slide[] slides = new Slide[] {
            new Slide { text = "No recuerdo como llegue aqui.", charsPerSecond = 22f, holdSeconds = 2.8f },
            new Slide { text = "El aula esta vacia.\nSolo yo. Y lo que sea\nque este dentro de mi.", charsPerSecond = 24f, holdSeconds = 3f },
            new Slide { text = "Algo no esta bien.", charsPerSecond = 18f, holdSeconds = 2.2f },
            new Slide { text = "Antes de entrar... respira.", charsPerSecond = 22f, holdSeconds = 3f, fadeOutSeconds = 1f }
        };

        [Header("Siguiente escena")]
        [SerializeField, Tooltip("Escena a cargar al terminar la cinematica.")]
        private string nextSceneName = "Dev_Henry";
        [SerializeField, Tooltip("Tip que muestra la pantalla de carga al pasar a la siguiente escena.")]
        private string loadingTip = "Cuando entres, sosten el mando cerca de tu boca.";

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color skipHintColor = new Color(0.92f, 0.89f, 0.83f, 0.4f);
        [SerializeField] private int textSize = 38;
        [SerializeField, TextArea(2, 6)] private string skipHint = "[Pulsa cualquier tecla para saltar / siguiente]";

        [Header("Tipografia (opcional)")]
        [SerializeField] private Font textFont;

        private Canvas canvas;
        private CanvasGroup textCanvasGroup;
        private Text mainText;
        private Text skipHintText;
        private bool skipRequested;
        private bool finished;

        private void Awake()
        {
            EnsureEventSystemAndCamera();
            BuildUI();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            if (finished) return;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool any = (kb != null && kb.anyKey.wasPressedThisFrame)
                    || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                      || gp.buttonNorth.wasPressedThisFrame
                                      || gp.buttonEast.wasPressedThisFrame
                                      || gp.buttonWest.wasPressedThisFrame
                                      || gp.startButton.wasPressedThisFrame));
            if (any) skipRequested = true;
        }

        private IEnumerator PlaySequence()
        {
            // Fade in del primer slide
            textCanvasGroup.alpha = 0f;
            yield return FadeAlpha(0f, 1f, 0.5f);

            for (int i = 0; i < slides.Length; i++)
            {
                yield return PlaySlide(slides[i]);
            }

            finished = true;
            // Cargar la siguiente escena con loading screen
            SceneTransition.LoadScene(nextSceneName, tip: loadingTip);
        }

        private IEnumerator PlaySlide(Slide slide)
        {
            mainText.text = "";
            // Asegurar visibilidad del slide
            if (textCanvasGroup.alpha < 1f) yield return FadeAlpha(textCanvasGroup.alpha, 1f, 0.4f);

            // Typewriter
            skipRequested = false;
            if (slide.charsPerSecond <= 0f)
            {
                mainText.text = slide.text;
            }
            else
            {
                float charDelay = 1f / slide.charsPerSecond;
                int idx = 0;
                while (idx < slide.text.Length && !skipRequested)
                {
                    mainText.text = slide.text.Substring(0, idx + 1);
                    idx++;
                    yield return new WaitForSecondsRealtime(charDelay);
                }
                // Si pidio skip mid-typewriter, mostrar todo el texto inmediatamente
                if (skipRequested)
                {
                    mainText.text = slide.text;
                    skipRequested = false;
                    // Pequenia espera para que vea el texto completo
                    yield return new WaitForSecondsRealtime(0.4f);
                }
            }

            // Hold (con posibilidad de skip)
            float holdElapsed = 0f;
            while (holdElapsed < slide.holdSeconds && !skipRequested)
            {
                holdElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            skipRequested = false;

            // Fade out
            if (slide.fadeOutSeconds > 0f)
                yield return FadeAlpha(1f, 0f, slide.fadeOutSeconds);
            else
                textCanvasGroup.alpha = 0f;
        }

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                textCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            textCanvasGroup.alpha = to;
        }

        // ---------- build ----------

        private void EnsureEventSystemAndCamera()
        {
            // Camera vacia para no tener "Display 1 No camera rendering"
            var existingCam = FindFirstObjectByType<Camera>();
            if (existingCam == null)
            {
                var camGo = new GameObject("Cinematic_Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = bgColor;
                cam.cullingMask = 0;
                cam.orthographic = true;
                camGo.tag = "MainCamera";
                if (FindFirstObjectByType<AudioListener>() == null)
                    camGo.AddComponent<AudioListener>();
            }
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Cinematic_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo
            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Texto principal con CanvasGroup para fades
            var textGroupGo = new GameObject("TextGroup", typeof(RectTransform));
            var tgRT = textGroupGo.GetComponent<RectTransform>();
            tgRT.SetParent(canvas.transform, false);
            tgRT.anchorMin = new Vector2(0.5f, 0.5f);
            tgRT.anchorMax = new Vector2(0.5f, 0.5f);
            tgRT.pivot = new Vector2(0.5f, 0.5f);
            tgRT.sizeDelta = new Vector2(1400f, 400f);
            tgRT.anchoredPosition = Vector2.zero;
            textCanvasGroup = textGroupGo.AddComponent<CanvasGroup>();

            mainText = MakeText(textGroupGo.transform, "MainText", "", textSize, FontStyle.Italic, textColor,
                Vector2.zero, new Vector2(1400f, 400f));

            // Skip hint abajo
            skipHintText = MakeText(canvas.transform, "SkipHint", skipHint, 16, FontStyle.Normal, skipHintColor,
                new Vector2(0f, -460f), new Vector2(900f, 30f));
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
            t.font = textFont != null ? textFont : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }
    }
}
