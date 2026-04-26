using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

namespace EstanDentro.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Texto")]
        [SerializeField] private string gameTitle = "ESTAN DENTRO";
        [SerializeField] private string gameSubtitle = "Lo que no puedo ver";
        [SerializeField] private string versionLabel = "Capitulo 1 - prototipo";

        [Header("Carga")]
        [SerializeField] private string playSceneName = "Dev_Henry";

        [Header("Paleta")]
        [SerializeField] private Color backgroundColor = new Color(0.04f, 0.04f, 0.05f, 1f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color subtitleColor = new Color(0.85f, 0.7f, 0.28f, 0.85f);
        [SerializeField] private Color bodyColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color buttonNormalColor = new Color(0f, 0f, 0f, 0.5f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.85f);
        [SerializeField] private Color buttonTextColor = new Color(0.95f, 0.93f, 0.85f, 1f);

        [Header("Layout")]
        [SerializeField] private float buttonWidth = 360f;
        [SerializeField] private float buttonHeight = 60f;
        [SerializeField] private float buttonSpacing = 18f;

        private Canvas canvas;
        private Button playButton;
        private Button settingsButton;
        private Button quitButton;

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();
            BuildUI();
            Settings.ApplyAll();
        }

        private void EnsureCamera()
        {
            var existing = FindObjectOfType<Camera>();
            if (existing != null) return;
            var go = new GameObject("MainMenu_Camera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.cullingMask = 0; // no renderiza nada del mundo, solo evita el mensaje
            cam.orthographic = true;
            go.tag = "MainCamera";
            if (FindObjectOfType<AudioListener>() == null)
                go.AddComponent<AudioListener>();
        }

        private void Start()
        {
            // Selecciona el primer boton para navegacion con teclado/gamepad
            EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
            // Asegura tiempo normal y mouse visible en el menu
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ---------- handlers ----------

        public void OnPlayClicked()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(playSceneName);
        }

        public void OnSettingsClicked()
        {
            SettingsOverlay.Open();
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- build ----------

        private void EnsureEventSystem()
        {
            var existing = FindObjectOfType<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Input System UI module (porque el proyecto usa Input System, no el viejo)
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("MainMenu_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = backgroundColor;

            // Vignette sutil — un Image con gradiente generado
            BuildVignette(canvas.transform);

            // Title
            MakeText("Title", gameTitle, 84, FontStyle.Bold, titleColor,
                new Vector2(0, 220), new Vector2(1500, 110));

            // Subtitle
            MakeText("Subtitle", gameSubtitle, 30, FontStyle.Italic, subtitleColor,
                new Vector2(0, 145), new Vector2(1200, 50));

            // Botones
            float startY = -20f;
            playButton = MakeButton("Btn_Play", "JUGAR",
                new Vector2(0, startY), OnPlayClicked);
            settingsButton = MakeButton("Btn_Settings", "AJUSTES",
                new Vector2(0, startY - (buttonHeight + buttonSpacing)), OnSettingsClicked);
            quitButton = MakeButton("Btn_Quit", "SALIR",
                new Vector2(0, startY - 2 * (buttonHeight + buttonSpacing)), OnQuitClicked);

            // Setup nav
            SetupNavigation(playButton, settingsButton, quitButton);

            // Footer / version
            MakeText("Footer", versionLabel, 14, FontStyle.Normal, new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.45f),
                new Vector2(0, -480), new Vector2(800, 28));
        }

        private void BuildVignette(Transform parent)
        {
            var vGo = new GameObject("Vignette");
            vGo.transform.SetParent(parent, false);
            var rt = vGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = vGo.AddComponent<Image>();
            img.sprite = MakeRadialVignetteSprite(256, new Color(0, 0, 0, 0.6f));
            img.raycastTarget = false;
        }

        private Sprite MakeRadialVignetteSprite(int size, Color edge)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / r;
                float dy = (y - cy) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01((d - 0.4f) / 0.6f); // 0 en centro, 1 en el borde
                px[y * size + x] = new Color(edge.r, edge.g, edge.b, edge.a * t);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Button MakeButton(string name, string label, Vector2 anchoredPos, System.Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = buttonNormalColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHoverColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            colors.fadeDuration = 0.12f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Label
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.font = GetDefaultFont();
            lblTxt.text = label;
            lblTxt.alignment = TextAnchor.MiddleCenter;
            lblTxt.fontSize = 26;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color = buttonTextColor;
            lblTxt.raycastTarget = false;

            return btn;
        }

        private void SetupNavigation(Button a, Button b, Button c)
        {
            SetVerticalNav(a, prev: c, next: b);
            SetVerticalNav(b, prev: a, next: c);
            SetVerticalNav(c, prev: b, next: a);
        }

        private void SetVerticalNav(Button btn, Button prev, Button next)
        {
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = prev;
            nav.selectOnDown = next;
            btn.navigation = nav;
        }

        private Text MakeText(string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
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
