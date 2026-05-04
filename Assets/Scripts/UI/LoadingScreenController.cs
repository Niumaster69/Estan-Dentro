using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EstanDentro.UI
{
    /// <summary>
    /// Pantalla de carga. Singleton DontDestroyOnLoad.
    /// UI procedural: titulo + reloj animado (parado en 17:00, manecillas giran durante carga) + tip rotativo.
    /// El reloj a las 17:00 es un motivo del Capitulo 1 (hora del accidente). Verlo "moverse"
    /// durante la carga es metafora de "el tiempo se desbloquea por un instante".
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        public static LoadingScreenController Instance { get; private set; }

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 0.9f);
        [SerializeField] private Color tipColor = new Color(0.85f, 0.7f, 0.28f, 0.95f);
        [SerializeField] private Color smallTextColor = new Color(0.92f, 0.89f, 0.83f, 0.5f);
        [SerializeField] private Color clockRingColor = new Color(0.85f, 0.7f, 0.28f, 0.85f);
        [SerializeField] private Color clockMarkColor = new Color(0.92f, 0.89f, 0.83f, 0.85f);
        [SerializeField] private Color clockHourHandColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color clockMinuteHandColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color clockCenterDotColor = new Color(0.85f, 0.2f, 0.2f, 1f);

        [Header("Reloj")]
        [SerializeField] private float clockSize = 240f;
        [SerializeField] private float clockRingThickness = 5f;
        [SerializeField] private int clockNumberFontSize = 26;
        [SerializeField, Tooltip("Velocidad del minutero en grados/segundo durante la carga.")]
        private float minuteHandSpeed = 15f;
        [SerializeField, Tooltip("Velocidad de la manecilla de hora (12x mas lento que minutos en un reloj real).")]
        private float hourHandSpeed = 15f / 12f;

        [Header("Tips por defecto")]
        [SerializeField, TextArea(2, 4)] private string[] defaultTips = new string[] {
            // Pedagogicos / respiracion (Diseño Narrativo Cap 1, sec 6)
            "La respiracion lenta calma al sistema nervioso.",
            "Inhalar 4. Sostener 4. Exhalar 6.",
            "Mirar a 5 metros relaja la vision periferica.",
            "Si el corazon se acelera, no es peligro. Es energia.",
            // Atmosfericos / narrativos
            "Inhala 4. Exhala 4. Pausa 2. El miedo no manda.",
            "El terror viene de lo que falta, no de lo que aparece.",
            "Cuando aprendes a respirar, vuelves a ver lo que estaba ahi todo el tiempo.",
            "Tu cuerpo es real. Tu miedo no decide.",
            "No estaba en el aula. Estaba dentro de mi.",
            "Si la sientes subir, respira despacio."
        };

        [Header("Tiempos")]
        [SerializeField] private float fadeInSeconds = 0.5f;
        [SerializeField] private float fadeOutSeconds = 0.6f;
        [SerializeField] private float minDisplayTime = 4.5f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Text titleText;
        private Text tipText;

        // Reloj
        private RectTransform clockHourHand;
        private RectTransform clockMinuteHand;
        // Estado inicial: 17:00 (5:00 PM). En convenciones de pantalla:
        //   - 12 esta arriba (rotation = 0)
        //   - clockwise positivo en mundo, pero negativo en localRotation Z (Unity)
        //   - 5 oclock = -150 grados (la manecilla de hora apunta a 150 grados clockwise desde 12)
        private const float HOUR_INITIAL_ROT = -150f;
        private const float MINUTE_INITIAL_ROT = 0f;
        private float hourRot;
        private float minuteRot;

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
            if (!IsVisible) return;

            // Animacion del reloj — manecillas giran clockwise (negativo en Z).
            minuteRot -= minuteHandSpeed * Time.unscaledDeltaTime;
            hourRot   -= hourHandSpeed   * Time.unscaledDeltaTime;
            if (clockMinuteHand != null)
                clockMinuteHand.localRotation = Quaternion.Euler(0f, 0f, minuteRot);
            if (clockHourHand != null)
                clockHourHand.localRotation = Quaternion.Euler(0f, 0f, hourRot);
        }

        // ---------- API publica ----------

        public IEnumerator FadeIn()
        {
            // Reset del reloj a 17:00 cada vez que se muestra la pantalla.
            hourRot = HOUR_INITIAL_ROT;
            minuteRot = MINUTE_INITIAL_ROT;
            if (clockHourHand != null) clockHourHand.localRotation = Quaternion.Euler(0, 0, hourRot);
            if (clockMinuteHand != null) clockMinuteHand.localRotation = Quaternion.Euler(0, 0, minuteRot);

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
            if (asyncOp == null)
            {
                Debug.LogError($"[LoadingScreen] No se pudo cargar la escena '{sceneName}'. " +
                               $"Verifica File -> Build Settings -> Scenes In Build.");
                if (tipText != null) tipText.text = $"No se pudo cargar '{sceneName}'.";
                yield return new WaitForSecondsRealtime(2f);
                yield return FadeOut();
                yield break;
            }
            asyncOp.allowSceneActivation = false;

            float startTime = Time.unscaledTime;
            float effectiveMin = Mathf.Max(minDisplay, minDisplayTime);

            while (asyncOp.progress < 0.9f || Time.unscaledTime - startTime < effectiveMin)
                yield return null;

            asyncOp.allowSceneActivation = true;
            while (!asyncOp.isDone) yield return null;

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
            canvas.sortingOrder = 32000;

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

            // Title arriba (usa la fuente Title del menu — Creepster u otra que el menu tenga asignada)
            titleText = MakeText(canvas.transform, "Title", "ESTAN DENTRO", 36, FontStyle.Bold, titleColor,
                new Vector2(0f, 360f), new Vector2(900f, 50f), TextAnchor.MiddleCenter, useTitleFont: true);
            var titleShadow = titleText.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // Linea decorativa fina debajo del titulo
            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(canvas.transform, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(180f, 2f);
            lineRT.anchoredPosition = new Vector2(0f, 320f);
            lineGo.AddComponent<Image>().color = new Color(clockRingColor.r, clockRingColor.g, clockRingColor.b, 0.6f);

            // ---- RELOJ procedural ----
            BuildClock(canvas.transform, new Vector2(0f, 60f));

            // Tip central grande, debajo del reloj
            tipText = MakeText(canvas.transform, "Tip", "", 24, FontStyle.Italic, tipColor,
                new Vector2(0f, -210f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter, useTitleFont: false);
        }

        private void BuildClock(Transform parent, Vector2 anchoredCenter)
        {
            // Container del reloj — todo se posiciona relativo a su centro.
            var clockGo = new GameObject("Clock", typeof(RectTransform));
            var clockRT = clockGo.GetComponent<RectTransform>();
            clockRT.SetParent(parent, false);
            clockRT.anchorMin = clockRT.anchorMax = new Vector2(0.5f, 0.5f);
            clockRT.pivot = new Vector2(0.5f, 0.5f);
            clockRT.sizeDelta = new Vector2(clockSize, clockSize);
            clockRT.anchoredPosition = anchoredCenter;

            // Aro exterior (ring)
            var ringGo = new GameObject("Ring", typeof(RectTransform));
            var ringRT = ringGo.GetComponent<RectTransform>();
            ringRT.SetParent(clockRT, false);
            ringRT.anchorMin = Vector2.zero; ringRT.anchorMax = Vector2.one;
            ringRT.offsetMin = Vector2.zero; ringRT.offsetMax = Vector2.zero;
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.sprite = CreateRingSprite(256, clockRingThickness / clockSize);
            ringImg.color = clockRingColor;
            ringImg.raycastTarget = false;

            // Numeros 12, 3, 6, 9 en las posiciones cardinales
            float numberRadius = clockSize * 0.5f - 28f;
            MakeClockNumber(clockRT, "12", new Vector2(0f,  numberRadius));
            MakeClockNumber(clockRT, "3",  new Vector2( numberRadius, 0f));
            MakeClockNumber(clockRT, "6",  new Vector2(0f, -numberRadius));
            MakeClockNumber(clockRT, "9",  new Vector2(-numberRadius, 0f));

            // Marcas de 4 horas extras (1, 2, 4, 5, 7, 8, 10, 11) — opcional, pero da peso.
            // Las hago como tics pequeños en lugar de numeros.
            float tickRadius = clockSize * 0.5f - 12f;
            float tickInner = clockSize * 0.5f - 22f;
            for (int h = 0; h < 12; h++)
            {
                if (h % 3 == 0) continue; // estos son los numeros
                float angleDeg = h * 30f - 90f; // 0 horas = 12 oclock, en angulos: -90
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float midR = (tickRadius + tickInner) * 0.5f;
                Vector2 pos = new Vector2(Mathf.Cos(angleRad) * midR, -Mathf.Sin(angleRad) * midR);
                MakeClockTick(clockRT, pos, angleDeg);
            }

            // Manecilla de hora — corta, mas gruesa
            clockHourHand = MakeClockHand(clockRT, "HourHand", clockSize * 0.32f, 6f, clockHourHandColor);
            clockHourHand.localRotation = Quaternion.Euler(0, 0, HOUR_INITIAL_ROT);

            // Manecilla de minutero — larga, mas fina
            clockMinuteHand = MakeClockHand(clockRT, "MinuteHand", clockSize * 0.44f, 4f, clockMinuteHandColor);
            clockMinuteHand.localRotation = Quaternion.Euler(0, 0, MINUTE_INITIAL_ROT);

            // Punto central — encima de las manecillas
            var dotGo = new GameObject("CenterDot", typeof(RectTransform));
            var dotRT = dotGo.GetComponent<RectTransform>();
            dotRT.SetParent(clockRT, false);
            dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.sizeDelta = new Vector2(12f, 12f);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.sprite = CreateCircleSprite(64);
            dotImg.color = clockCenterDotColor;
            dotImg.raycastTarget = false;
        }

        private void MakeClockNumber(Transform parent, string label, Vector2 anchoredPos)
        {
            var go = new GameObject("N_" + label, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(40f, 40f);
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = GetBodyFont();
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = clockNumberFontSize;
            t.fontStyle = FontStyle.Bold;
            t.color = clockMarkColor;
            t.raycastTarget = false;
        }

        private void MakeClockTick(Transform parent, Vector2 anchoredPos, float angleDeg)
        {
            var go = new GameObject("Tick", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(2f, 8f);
            rt.anchoredPosition = anchoredPos;
            // Rotar tic para que apunte hacia el centro (radial)
            rt.localRotation = Quaternion.Euler(0f, 0f, -angleDeg + 90f);
            var img = go.AddComponent<Image>();
            img.color = new Color(clockMarkColor.r, clockMarkColor.g, clockMarkColor.b, 0.5f);
            img.raycastTarget = false;
        }

        private RectTransform MakeClockHand(Transform parent, string name, float length, float thickness, Color color)
        {
            // Pivot en (0.5, 0) — bottom-center. Permite rotar alrededor del centro del reloj
            // si la posicion de la mano es (0,0) y el largo apunta hacia arriba (Y+).
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(thickness, length);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            // Usar el mismo sprite redondeado que las cajas — terminales suaves.
            img.color = color;
            img.raycastTarget = false;
            return rt;
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

        private Sprite CreateRingSprite(int size, float thicknessFraction)
        {
            // Anillo: alpha=1 entre rOuter y rInner, alpha=0 fuera/dentro. thicknessFraction es 0..1 del radio.
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float rOuter = size * 0.5f - 1f;
            float rInner = rOuter * (1f - Mathf.Clamp01(thicknessFraction));
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float aOut = Mathf.Clamp01(rOuter - d);
                float aIn = Mathf.Clamp01(d - rInner);
                float a = Mathf.Min(aOut, aIn);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta, TextAnchor align, bool useTitleFont = false)
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
            t.font = useTitleFont ? GetTitleFont() : GetBodyFont();
            t.text = content;
            t.alignment = align;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Font GetTitleFont()
        {
            // Fuente del titulo del menu (Creepster). Si MainMenu nunca corrio en esta sesion,
            // o si no esta asignada, fallback a body, despues a built-in.
            if (MainMenuController.SharedTitleFont != null) return MainMenuController.SharedTitleFont;
            return GetBodyFont();
        }

        private static Font GetBodyFont()
        {
            // Fuente del body del menu (SpecialElite). Fallback a built-in.
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
