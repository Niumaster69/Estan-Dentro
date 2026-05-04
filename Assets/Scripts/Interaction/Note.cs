using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;
using EstanDentro.Inventory;

namespace EstanDentro.Interaction
{
    public abstract class Interactable : MonoBehaviour
    {
        public abstract void Interact();
    }

    public class Note : Interactable
    {
        [Header("Contenido")]
        [SerializeField] private string noteTitle = "Nota";
        [SerializeField, TextArea(3, 10)] private string noteText = "Si la sientes subir, respira despacio.";

        [Header("Comportamiento")]
        [SerializeField, Tooltip("Si true, el GameObject se destruye al cerrar el NoteOverlay. La nota queda registrada en el Inventory para releer con I.")]
        private bool disappearAfterRead = true;

        public override void Interact()
        {
            Debug.Log($"[Note] Interact en '{name}'. title='{noteTitle}'");
            if (Inventory.Inventory.Instance != null)
                Inventory.Inventory.Instance.RegisterNote(noteTitle, noteText);
            else
                Debug.LogWarning("[Note] Inventory.Instance es null! La nota no se registra.");

            if (disappearAfterRead)
                NoteOverlay.Show(noteTitle, noteText, onClose: () => { if (this != null) Destroy(gameObject); });
            else
                NoteOverlay.Show(noteTitle, noteText);
        }
    }

    public class NoteOverlay : MonoBehaviour
    {
        private static NoteOverlay instance;
        private Canvas canvas;
        private Text titleTxt;
        private Text bodyTxt;
        private float prevTimeScale;
        private bool consumeInputThisFrame;
        private System.Action onCloseCallback;

        public static void Show(string title, string body, System.Action onClose = null)
        {
            EnsureInstance();
            Debug.Log($"[NoteOverlay] Show llamado. title='{title}'. canvas.enabled antes={instance.canvas.enabled}");
            if (instance.canvas.enabled)
            {
                Debug.LogWarning($"[NoteOverlay] BLOQUEADO: canvas ya esta enabled. Title nuevo='{title}' fue ignorado.");
                return;
            }
            instance.titleTxt.text = title;
            instance.bodyTxt.text = body;
            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.canvas.enabled = true;
            instance.consumeInputThisFrame = true;
            instance.onCloseCallback = onClose;
            OverlayBlocker.Register();
            Debug.Log($"[NoteOverlay] Show OK. Canvas enabled, esperando input para cerrar.");
        }

        public static void Hide()
        {
            if (instance == null || !instance.canvas.enabled) return;
            Debug.Log("[NoteOverlay] Hide llamado.");
            instance.canvas.enabled = false;
            Time.timeScale = instance.prevTimeScale > 0f ? instance.prevTimeScale : 1f;
            OverlayBlocker.Unregister();
            var cb = instance.onCloseCallback;
            instance.onCloseCallback = null;
            cb?.Invoke();
        }

        private void OnDestroy()
        {
            if (instance == this && canvas != null && canvas.enabled)
                OverlayBlocker.Unregister();
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("__NoteOverlay");
            instance = go.AddComponent<NoteOverlay>();
            instance.Build();
        }

        private void Update()
        {
            if (canvas == null || !canvas.enabled) return;
            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && (kb.eKey.wasPressedThisFrame
                                          || kb.escapeKey.wasPressedThisFrame
                                          || kb.spaceKey.wasPressedThisFrame
                                          || kb.enterKey.wasPressedThisFrame))
                        || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                          || gp.buttonEast.wasPressedThisFrame));
            if (dismiss) Hide();
        }

        private void Build()
        {
            var co = new GameObject("Note_Canvas");
            co.transform.SetParent(transform, false);
            canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220; // por encima de InventoryOverlay (195) e ItemPreviewOverlay (200)
            var scaler = co.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            co.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvas.transform, false);
            var pRT = panelGo.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 0.5f);
            pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.sizeDelta = new Vector2(900f, 500f);
            panelGo.AddComponent<Image>().color = new Color(0.08f, 0.07f, 0.05f, 0.97f);

            titleTxt = MakeText(panelGo.transform, "Title", "Nota", 30, FontStyle.Bold,
                new Vector2(0, 200), new Vector2(820, 60));
            titleTxt.alignment = TextAnchor.MiddleCenter;

            bodyTxt = MakeText(panelGo.transform, "Body", "", 22, FontStyle.Italic,
                new Vector2(0, 0), new Vector2(820, 280));
            bodyTxt.alignment = TextAnchor.MiddleCenter;

            var hint = MakeText(panelGo.transform, "Hint", "(E / Cross / Esc para cerrar)",
                14, FontStyle.Normal,
                new Vector2(0, -210), new Vector2(820, 30));
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.92f, 0.89f, 0.83f, 0.55f);

            // CRITICAL: arrancar oculto. Si no, la guarda 'if (canvas.enabled) return' en Show()
            // hace que la primera llamada a NoteOverlay.Show salga inmediatamente sin mostrar nada.
            canvas.enabled = false;
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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = new Color(0.92f, 0.89f, 0.83f, 0.95f);
            return t;
        }
    }
}
