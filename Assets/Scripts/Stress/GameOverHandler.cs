using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using EstanDentro.UI;

namespace EstanDentro.Stress
{
    public class GameOverHandler : MonoBehaviour
    {
        [Header("Mensaje")]
        [SerializeField] private string titleText = "TE DEJASTE LLEVAR";
        [SerializeField, TextArea(2, 6)] private string bodyText =
            "El miedo te tragó.\nRespira. Empezá otra vez.";
        [SerializeField] private string hintText = "[Pulsa CROSS o ENTER para reiniciar]";

        [Header("Comportamiento")]
        [SerializeField, Tooltip("Segundos a esperar antes de aceptar input (para que el jugador procese).")]
        private float acceptInputAfterSeconds = 1.5f;
        [SerializeField, Tooltip("Si true, los gamepad sticks tambien reinician (puede dispararse accidentalmente).")]
        private bool acceptStickAsInput = false;

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.96f);
        [SerializeField] private Color titleColor = new Color(0.7f, 0.18f, 0.18f, 1f);
        [SerializeField] private Color bodyColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color hintColor = new Color(0.92f, 0.89f, 0.83f, 0.55f);

        private Canvas canvas;
        private Text titleLabel;
        private Text bodyLabel;
        private Text hintLabel;
        private float prevTimeScale = 1f;
        private float showStartedAt;
        private bool active;

        private void Awake()
        {
            BuildUI();
            canvas.enabled = false;
        }

        private void Start()
        {
            if (StressSystem.Instance != null)
                StressSystem.Instance.OnCollapse += HandleCollapse;
        }

        private void OnDestroy()
        {
            if (StressSystem.Instance != null)
                StressSystem.Instance.OnCollapse -= HandleCollapse;
        }

        private void HandleCollapse()
        {
            Show();
        }

        private void Show()
        {
            if (active) return;
            active = true;
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            canvas.enabled = true;
            showStartedAt = Time.unscaledTime;
            titleLabel.text = titleText;
            bodyLabel.text = bodyText;
            hintLabel.text = hintText;
            OverlayBlocker.Register();
        }

        private void Hide()
        {
            if (!active) return;
            active = false;
            canvas.enabled = false;
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
            OverlayBlocker.Unregister();
        }

        private void Update()
        {
            if (!active) return;

            // Si el jugador des-colapso por debug (R o J), oculta el overlay
            if (StressSystem.Instance != null && !StressSystem.Instance.IsCollapsed)
            {
                Hide();
                return;
            }

            if (Time.unscaledTime - showStartedAt < acceptInputAfterSeconds) return;

            if (HasRestartInput()) RestartScene();
        }

        private bool HasRestartInput()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool kbHit = kb != null && (kb.enterKey.wasPressedThisFrame
                                       || kb.numpadEnterKey.wasPressedThisFrame
                                       || kb.spaceKey.wasPressedThisFrame
                                       || kb.anyKey.wasPressedThisFrame);
            bool gpHit = gp != null && (gp.buttonSouth.wasPressedThisFrame
                                       || gp.buttonNorth.wasPressedThisFrame
                                       || gp.buttonEast.wasPressedThisFrame
                                       || gp.buttonWest.wasPressedThisFrame
                                       || gp.startButton.wasPressedThisFrame);
            if (acceptStickAsInput && gp != null)
                gpHit = gpHit || gp.leftStick.ReadValue().sqrMagnitude > 0.1f;
            return kbHit || gpHit;
        }

        private void RestartScene()
        {
            Time.timeScale = 1f;
            EstanDentro.UI.SceneTransition.Reload(tip: "Respira. Empieza otra vez.");
        }

        private void BuildUI()
        {
            var go = new GameObject("GameOver_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            // Fondo
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Title
            titleLabel = MakeText("Title", titleText, 56, FontStyle.Bold,
                new Vector2(0, 120), new Vector2(1400, 90), titleColor);

            // Body
            bodyLabel = MakeText("Body", bodyText, 26, FontStyle.Italic,
                new Vector2(0, 0), new Vector2(1100, 160), bodyColor);

            // Hint
            hintLabel = MakeText("Hint", hintText, 18, FontStyle.Normal,
                new Vector2(0, -160), new Vector2(900, 36), hintColor);
        }

        private Text MakeText(string name, string content, int size, FontStyle style,
            Vector2 anchoredPos, Vector2 sizeDelta, Color color)
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            return t;
        }
    }
}
