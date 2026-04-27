using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace EstanDentro.Interaction
{
    [DefaultExecutionOrder(-30)]
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float maxDistance = 3f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private Camera sourceCamera;
        [SerializeField, Tooltip("Offset del origen del raycast adelante de la camara para evitar auto-colision con el Player.")]
        private float originForwardOffset = 0.2f;
        [SerializeField] private bool debugLogTargets = false;
        private GameObject lastLoggedTarget;

        [Header("Reticle")]
        [SerializeField] private Vector2 reticleSize = new Vector2(10f, 10f);
        [SerializeField] private float reticleActiveScale = 1.8f;
        [SerializeField] private Color reticleIdleColor = new Color(0.9f, 0.88f, 0.83f, 0.55f);
        [SerializeField] private Color reticleActiveColor = new Color(0.95f, 0.93f, 0.85f, 0.95f);

        private Canvas canvas;
        private Image reticleImage;
        private RectTransform reticleRT;
        private Interactable currentTarget;

        private void Awake()
        {
            BuildReticle();
            if (sourceCamera == null) sourceCamera = Camera.main;
            Debug.Log($"[InteractionSystem] Awake. sourceCamera={(sourceCamera != null ? sourceCamera.name : "NULL")}");
        }

        private void Update()
        {
            if (sourceCamera == null) sourceCamera = Camera.main;
            if (sourceCamera == null) return;
            currentTarget = ResolveTarget();
            UpdateReticleVisual();
            HandleInput();
        }

        private Interactable ResolveTarget()
        {
            Vector3 origin = sourceCamera.transform.position + sourceCamera.transform.forward * originForwardOffset;
            Ray ray = new Ray(origin, sourceCamera.transform.forward);
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan);
            if (Physics.Raycast(ray, out var hit, maxDistance, hitLayers, QueryTriggerInteraction.Collide))
            {
                if (debugLogTargets && hit.collider.gameObject != lastLoggedTarget)
                {
                    lastLoggedTarget = hit.collider.gameObject;
                    var found = hit.collider.GetComponentInParent<Interactable>();
                    Debug.Log($"[Interaction] Hit '{hit.collider.name}' a {hit.distance:F2}m. Interactable={(found != null ? found.GetType().Name : "no")}");
                }
                return hit.collider.GetComponentInParent<Interactable>();
            }
            if (debugLogTargets && lastLoggedTarget != null)
            {
                lastLoggedTarget = null;
                Debug.Log("[Interaction] Sin target");
            }
            return null;
        }

        private void UpdateReticleVisual()
        {
            bool active = currentTarget != null;
            reticleImage.color = active ? reticleActiveColor : reticleIdleColor;
            reticleRT.localScale = active ? Vector3.one * reticleActiveScale : Vector3.one;
        }

        private void HandleInput()
        {
            if (currentTarget == null) return;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool pressed = (kb != null && kb.eKey.wasPressedThisFrame)
                        || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if (pressed) currentTarget.Interact();
        }

        private void BuildReticle()
        {
            var go = new GameObject("Reticle_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();

            var dotGo = new GameObject("Reticle_Dot");
            dotGo.transform.SetParent(canvas.transform, false);
            reticleRT = dotGo.AddComponent<RectTransform>();
            reticleRT.anchorMin = new Vector2(0.5f, 0.5f);
            reticleRT.anchorMax = new Vector2(0.5f, 0.5f);
            reticleRT.pivot = new Vector2(0.5f, 0.5f);
            reticleRT.sizeDelta = reticleSize;
            reticleImage = dotGo.AddComponent<Image>();
            reticleImage.sprite = MakeCircleSprite(32);
            reticleImage.color = reticleIdleColor;
        }

        private Sprite MakeCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
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
    }
}
