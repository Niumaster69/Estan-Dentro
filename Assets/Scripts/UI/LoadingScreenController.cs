using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EstanDentro.UI
{
    /// <summary>
    /// Pantalla de carga reutilizable. Singleton DontDestroyOnLoad.
    /// Se auto-crea cuando SceneTransition la necesita por primera vez.
    /// Construye toda su UI por codigo (sin escena dedicada).
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        public static LoadingScreenController Instance { get; private set; }

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 0.9f);
        [SerializeField] private Color tipColor = new Color(0.85f, 0.7f, 0.28f, 0.9f);
        [SerializeField] private Color smallTextColor = new Color(0.92f, 0.89f, 0.83f, 0.5f);

        [Header("Tips por defecto")]
        [SerializeField, TextArea(2, 4)] private string[] defaultTips = new string[] {
            "Inhala 4. Exhala 4. Pausa 2. El miedo no manda.",
            "El terror viene de lo que falta, no de lo que aparece.",
            "Cuando aprendes a respirar, vuelves a ver lo que estaba ahi todo el tiempo.",
            "Tu cuerpo es real. Tu miedo no decide.",
            "No estaba en el aula. Estaba dentro de mi.",
            "Si la sientes subir, respira despacio."
        };

        [Header("Tiempos")]
        [SerializeField] private float fadeInSeconds = 0.4f;
        [SerializeField] private float fadeOutSeconds = 0.5f;
        [SerializeField] private float minDisplayTime = 1.5f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Text titleText;
        private Text tipText;
        private Text loadingText;
        private float dotsAnimAccum;

        public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

        // ---------- bootstrap ----------

        public static LoadingScreenController GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("__LoadingScreenController");
            Instance = go.AddComponent<LoadingScreenController>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
            canvasGroup.alpha = 0f;
            canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsVisible || loadingText == null) return;
            // Animacion de "Cargando..." con dots crecientes
            dotsAnimAccum += Time.unscaledDeltaTime;
            int dots = ((int)(dotsAnimAccum * 2f)) % 4;
            loadingText.text = "Cargando" + new string('.', dots);
        }

        // ---------- API publica ----------

        public IEnumerator FadeIn()
        {
            canvas.gameObject.SetActive(true);
            float t = 0f;
            while (t < fadeInSeconds)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeInSeconds);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        public IEnumerator FadeOut()
        {
            float t = 0f;
            while (t < fadeOutSeconds)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutSeconds);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvas.gameObject.SetActive(false);
        }

        public void SetTip(string tip)
        {
            if (string.IsNullOrEmpty(tip))
            {
                if (defaultTips != null && defaultTips.Length > 0)
                    tip = defaultTips[Random.Range(0, defaultTips.Length)];
                else
                    tip = "";
            }
            if (tipText != null) tipText.text = tip;
        }

        public IEnumerator LoadSceneRoutine(string sceneName, float minDisplay, string tip)
        {
            SetTip(tip);
            yield return FadeIn();

            // Asegurar que el cursor este libre durante la carga
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;

            var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            float startTime = Time.unscaledTime;
            float effectiveMin = Mathf.Max(minDisplay, minDisplayTime);

            // Esperar a que la escena este lista (>= 0.9 progress) Y haya pasado el min display time
            while (asyncOp.progress < 0.9f || Time.unscaledTime - startTime < effectiveMin)
                yield return null;

            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone) yield return null;

            // Frame extra para que la nueva escena se inicialice
            yield return null;
            yield return FadeOut();
        }

        // ---------- build UI ----------

        private void BuildUI()
        {
            var canvasGo = new GameObject("LoadingScreen_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000; // encima de absolutamente todo

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;

            // Fondo negro fullscreen
            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Title pequenio arriba
            titleText = MakeText(canvas.transform, "Title", "ESTAN DENTRO", 28, FontStyle.Bold, titleColor,
                new Vector2(0, 320f), new Vector2(800f, 50f), TextAnchor.MiddleCenter);
            var titleShadow = titleText.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // Tip central grande
            tipText = MakeText(canvas.transform, "Tip", "", 24, FontStyle.Italic, tipColor,
                new Vector2(0, 0f), new Vector2(1200f, 200f), TextAnchor.MiddleCenter);

            // "Cargando..." abajo a la derecha
            loadingText = MakeText(canvas.transform, "Loading", "Cargando", 18, FontStyle.Normal, smallTextColor,
                new Vector2(0, -380f), new Vector2(600f, 30f), TextAnchor.MiddleCenter);
        }

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta, TextAnchor align)
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = content;
            t.alignment = align;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }
    }
}
