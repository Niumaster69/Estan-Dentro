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

        [Header("Audio (recoger nota / item)")]
        [SerializeField, Tooltip("SFX al recoger la nota. Se reproduce por AudioManager.PlaySFX.")]
        private AudioClip pickupClip;
        [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.85f;

        public override void Interact()
        {
            Debug.Log($"[Note] Interact en '{name}'. title='{noteTitle}'");
            if (Inventory.Inventory.Instance != null)
                Inventory.Inventory.Instance.RegisterNote(noteTitle, noteText);
            else
                Debug.LogWarning("[Note] Inventory.Instance es null! La nota no se registra.");

            if (pickupClip != null)
            {
                var am = EstanDentro.Audio.AudioManager.Instance;
                if (am != null) am.PlaySFX(pickupClip, pickupVolume);
                else AudioSource.PlayClipAtPoint(pickupClip, transform.position, pickupVolume);
            }

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
                // en fila: se muestra al cerrar la actual (antes se perdía, p. ej. la carta dentro del casillero)
                instance.fila.Enqueue((title, body, onClose));
                return;
            }
            instance.titleTxt.text = title;
            instance.bodyTxt.text = body;
            EstanDentro.UI.Pausa.Pedir(instance);
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
            EstanDentro.UI.Pausa.Soltar(instance);
            OverlayBlocker.Unregister();
            var cb = instance.onCloseCallback;
            instance.onCloseCallback = null;
            cb?.Invoke();
            if (instance.fila.Count > 0)
            {
                var (t, b, c) = instance.fila.Dequeue();
                Show(t, b, c);
            }
        }

        private readonly System.Collections.Generic.Queue<(string, string, System.Action)> fila =
            new System.Collections.Generic.Queue<(string, string, System.Action)>();

        public static bool Abierta => instance != null && instance.canvas != null && instance.canvas.enabled;

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
            pRT.sizeDelta = new Vector2(820f, 620f);
            pRT.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            panelGo.AddComponent<Image>().color = new Color(0.90f, 0.86f, 0.76f, 1f);   // papel
            // renglones y margen rojo de cuaderno
            for (int r = 0; r < 15; r++)
            {
                var ln = new GameObject("Renglon", typeof(RectTransform)).GetComponent<RectTransform>();
                ln.SetParent(panelGo.transform, false);
                ln.anchorMin = ln.anchorMax = ln.pivot = new Vector2(0.5f, 0.5f);
                ln.sizeDelta = new Vector2(820f, 2f); ln.anchoredPosition = new Vector2(0f, 230f - r * 34f);
                ln.gameObject.AddComponent<Image>().color = new Color(0.27f, 0.43f, 0.63f, 0.25f);
            }
            var mg = new GameObject("Margen", typeof(RectTransform)).GetComponent<RectTransform>();
            mg.SetParent(panelGo.transform, false);
            mg.anchorMin = mg.anchorMax = mg.pivot = new Vector2(0.5f, 0.5f);
            mg.sizeDelta = new Vector2(2f, 620f); mg.anchoredPosition = new Vector2(-330f, 0f);
            mg.gameObject.AddComponent<Image>().color = new Color(0.72f, 0.16f, 0.12f, 0.5f);

            titleTxt = MakeText(panelGo.transform, "Title", "Nota", 32, FontStyle.Normal,
                new Vector2(30, 250), new Vector2(680, 60));
            titleTxt.color = new Color(0.62f, 0.12f, 0.12f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;

            bodyTxt = MakeText(panelGo.transform, "Body", "", 26, FontStyle.Normal,
                new Vector2(30, -10), new Vector2(660, 440));
            bodyTxt.alignment = TextAnchor.MiddleLeft;
            bodyTxt.lineSpacing = 1.15f;
            bodyTxt.color = new Color(0.16f, 0.14f, 0.12f, 1f);

            var hint = MakeText(panelGo.transform, "Hint", "(E / Cross / Esc para cerrar)",
                14, FontStyle.Normal,
                new Vector2(30, -282), new Vector2(680, 30));
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.16f, 0.14f, 0.12f, 0.45f);

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
            t.font = EstanDentro.UI.Fuentes.Maquina;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = new Color(0.92f, 0.89f, 0.83f, 0.95f);
            return t;
        }
    }
}
