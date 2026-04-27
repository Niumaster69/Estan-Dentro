using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using EstanDentro.Inventory;

namespace EstanDentro.UI
{
    public class PauseMenuHandler : MonoBehaviour
    {
        public static PauseMenuHandler Instance { get; private set; }

        [Header("Texto")]
        [SerializeField] private string titleText = "PAUSA";
        [SerializeField] private string continueLabel = "Continuar";
        [SerializeField] private string notesLabel = "Notas";
        [SerializeField] private string settingsLabel = "Ajustes";
        [SerializeField] private string mainMenuLabel = "Salir al menu";

        [Header("Carga")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Visual")]
        [SerializeField] private Color bgOverlayColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color panelColor = new Color(0.06f, 0.06f, 0.07f, 0.95f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color buttonNormalColor = new Color(0.12f, 0.12f, 0.13f, 0.85f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.7f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color buttonTextColor = new Color(0.95f, 0.93f, 0.85f, 1f);

        public bool IsOpen => active;
        private bool active;
        private float prevTimeScale = 1f;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;

        private Canvas canvas;
        private Button continueButton;
        private Button notesButton;
        private Button settingsButton;
        private Button mainMenuButton;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            EnsureEventSystem();
            BuildUI();
            canvas.gameObject.SetActive(false);
            Settings.ApplyAll();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (DetectPauseInput())
            {
                if (active) { ClosePause(); return; }
                // Si hay otro overlay modal abierto (nota, cerradura, calibracion, gameover), no abrir pause.
                if (OverlayBlocker.IsBlocking) return;
                if (Time.timeScale > 0f) OpenPause();
            }
        }

        private bool DetectPauseInput()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.escapeKey.wasPressedThisFrame)
                || (gp != null && gp.startButton.wasPressedThisFrame);
        }

        public void OpenPause()
        {
            if (active) return;
            active = true;
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            prevCursorLock = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canvas.gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(continueButton.gameObject);
        }

        public void ClosePause()
        {
            if (!active) return;
            active = false;
            canvas.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
            Cursor.lockState = prevCursorLock;
            Cursor.visible = prevCursorVisible;
        }

        // ---------- handlers ----------

        public void OnContinueClicked() => ClosePause();

        public void OnNotesClicked()
        {
            ClosePause();
            InventoryOverlay.Open(onClose: () => OpenPause());
        }

        public void OnSettingsClicked()
        {
            ClosePause();
            SettingsOverlay.Open(onClose: () => OpenPause());
        }

        public void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            ClosePause();
            SceneTransition.LoadScene(mainMenuSceneName, tip: "Volviendo al inicio.");
        }

        // ---------- BuildUI ----------

        private void BuildUI()
        {
            var canvasGo = new GameObject("Pause_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // BG (semi-transparente, bloquea raycast)
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = bgOverlayColor;
            bgImg.raycastTarget = true;

            // Panel central
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvas.transform, false);
            var panelRT = panelGo.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(480f, 460f);
            panelGo.AddComponent<Image>().color = panelColor;

            // Title
            MakeText(panelGo.transform, "Title", titleText, 36, FontStyle.Bold, titleColor,
                new Vector2(0, 175), new Vector2(420, 50));

            // Botones
            float startY = 80f;
            float btnH = 56f;
            float btnSpacing = 14f;
            continueButton = MakeButton(panelGo.transform, "Btn_Continue", continueLabel,
                new Vector2(0, startY), OnContinueClicked);
            notesButton = MakeButton(panelGo.transform, "Btn_Notes", notesLabel,
                new Vector2(0, startY - (btnH + btnSpacing)), OnNotesClicked);
            settingsButton = MakeButton(panelGo.transform, "Btn_Settings", settingsLabel,
                new Vector2(0, startY - 2 * (btnH + btnSpacing)), OnSettingsClicked);
            mainMenuButton = MakeButton(panelGo.transform, "Btn_MainMenu", mainMenuLabel,
                new Vector2(0, startY - 3 * (btnH + btnSpacing)), OnMainMenuClicked);

            SetupNavigation(continueButton, notesButton, settingsButton, mainMenuButton);
        }

        private Button MakeButton(Transform parent, string name, string label, Vector2 anchoredPos, System.Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(380f, 56f);
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
            var t = lblGo.AddComponent<Text>();
            t.font = GetDefaultFont();
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.color = buttonTextColor;
            t.raycastTarget = false;

            return btn;
        }

        private void SetupNavigation(params Button[] btns)
        {
            for (int i = 0; i < btns.Length; i++)
            {
                var nav = btns[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = btns[(i - 1 + btns.Length) % btns.Length];
                nav.selectOnDown = btns[(i + 1) % btns.Length];
                btns[i].navigation = nav;
            }
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
