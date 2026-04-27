using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Interaction
{
    public class LockOverlay : MonoBehaviour
    {
        private static LockOverlay instance;

        [Header("Visual")]
        [SerializeField] private Color panelBgColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color panelInnerColor = new Color(0.08f, 0.07f, 0.05f, 0.97f);
        [SerializeField] private Color digitColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color selectedColor = new Color(0.86f, 0.71f, 0.28f, 1f);
        [SerializeField] private Color failColor = new Color(0.62f, 0.12f, 0.12f, 1f);
        [SerializeField] private float failFlashSeconds = 0.6f;

        private Canvas canvas;
        private RectTransform panel;
        private Text titleText;
        private Text statusText;
        private Text[] digitTexts;
        private RectTransform[] digitRects;
        private int[] currentValues;
        private int selectedIndex;
        private CombinationLock target;
        private float prevTimeScale;
        private float failFlashUntil;
        private bool consumeInputThisFrame;

        public static void Open(CombinationLock lockComp)
        {
            EnsureInstance();
            if (instance.canvas.enabled) return;
            instance.target = lockComp;
            instance.BuildDigits(lockComp.CorrectCode.Length);
            instance.selectedIndex = 0;
            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.canvas.enabled = true;
            instance.consumeInputThisFrame = true;
            instance.Refresh();
            OverlayBlocker.Register();
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.enabled) return;
            instance.canvas.enabled = false;
            Time.timeScale = instance.prevTimeScale > 0f ? instance.prevTimeScale : 1f;
            instance.target = null;
            OverlayBlocker.Unregister();
        }

        private void OnDestroy()
        {
            if (instance == this && canvas != null && canvas.enabled)
                OverlayBlocker.Unregister();
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("__LockOverlay");
            instance = go.AddComponent<LockOverlay>();
            instance.BuildCanvasOnly();
        }

        private void Update()
        {
            if (canvas == null || !canvas.enabled) return;
            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            if (gp != null && gp.buttonEast.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // Cambiar rueda seleccionada
            bool left = (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame))
                     || (gp != null && gp.dpad.left.wasPressedThisFrame);
            bool right = (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame))
                      || (gp != null && gp.dpad.right.wasPressedThisFrame);
            if (left) MoveSelection(-1);
            if (right) MoveSelection(+1);

            // Cambiar digito de la rueda seleccionada
            bool up = (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
                   || (gp != null && gp.dpad.up.wasPressedThisFrame);
            bool down = (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame))
                     || (gp != null && gp.dpad.down.wasPressedThisFrame);
            if (up) ChangeDigit(+1);
            if (down) ChangeDigit(-1);

            // Confirmar
            bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                        || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if (confirm) Submit();

            // Quitar flash de fail si paso el tiempo
            if (failFlashUntil > 0f && Time.unscaledTime >= failFlashUntil)
            {
                failFlashUntil = 0f;
                statusText.text = HintLine();
                Refresh();
            }
        }

        private void MoveSelection(int delta)
        {
            if (digitTexts == null || digitTexts.Length == 0) return;
            selectedIndex = (selectedIndex + delta + digitTexts.Length) % digitTexts.Length;
            Refresh();
        }

        private void ChangeDigit(int delta)
        {
            if (currentValues == null || currentValues.Length == 0) return;
            currentValues[selectedIndex] = (currentValues[selectedIndex] + delta + 10) % 10;
            Refresh();
        }

        private void Submit()
        {
            if (target == null) return;
            target.NotifyAttempt(currentValues);
            if (target != null && !target.IsSolved)
            {
                statusText.text = "INCORRECTO";
                statusText.color = failColor;
                failFlashUntil = Time.unscaledTime + failFlashSeconds;
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < digitTexts.Length; i++)
            {
                digitTexts[i].text = currentValues[i].ToString();
                digitTexts[i].color = (i == selectedIndex) ? selectedColor : digitColor;
                digitRects[i].localScale = (i == selectedIndex) ? new Vector3(1.2f, 1.2f, 1f) : Vector3.one;
            }
            if (failFlashUntil <= 0f)
            {
                statusText.color = digitColor;
                statusText.text = HintLine();
            }
        }

        private string HintLine()
        {
            return "Flechas / D-pad para mover y cambiar digitos\nEnter / Cross para confirmar - Esc / Circle para salir";
        }

        private void BuildCanvasOnly()
        {
            var co = new GameObject("Lock_Canvas");
            co.transform.SetParent(transform, false);
            canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190;
            var scaler = co.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            co.AddComponent<GraphicRaycaster>();
            canvas.enabled = false;

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = panelBgColor;

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvas.transform, false);
            panel = panelGo.AddComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(800f, 360f);
            panelGo.AddComponent<Image>().color = panelInnerColor;

            titleText = MakeText(panel, "Title", "CERRADURA", 28, FontStyle.Bold,
                new Vector2(0, 130), new Vector2(720, 40));
            titleText.color = digitColor;

            statusText = MakeText(panel, "Status", HintLine(), 16, FontStyle.Normal,
                new Vector2(0, -130), new Vector2(720, 50));
            statusText.color = digitColor;
        }

        private void BuildDigits(int count)
        {
            // Limpia ruedas previas (si la cerradura anterior tenia distinta cantidad)
            if (digitRects != null)
            {
                for (int i = 0; i < digitRects.Length; i++)
                    if (digitRects[i] != null) Destroy(digitRects[i].gameObject);
            }

            digitTexts = new Text[count];
            digitRects = new RectTransform[count];
            currentValues = new int[count];

            float spacing = 110f;
            float totalWidth = spacing * (count - 1);
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Digit_{i}");
                go.transform.SetParent(panel, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(90f, 130f);
                rt.anchoredPosition = new Vector2(startX + spacing * i, 0f);
                digitRects[i] = rt;

                // Marco
                var bg = go.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.06f);

                // Texto del digito
                var txtGo = new GameObject("Txt");
                txtGo.transform.SetParent(rt, false);
                var trt = txtGo.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                var t = txtGo.AddComponent<Text>();
                t.font = GetDefaultFont();
                t.text = "0";
                t.alignment = TextAnchor.MiddleCenter;
                t.fontSize = 70;
                t.fontStyle = FontStyle.Bold;
                t.color = digitColor;
                digitTexts[i] = t;
            }
        }

        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style,
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
            return t;
        }
    }
}
