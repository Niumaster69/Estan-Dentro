using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// Overlay para visualizar un item recogido como preview 3D.
    /// Renderiza un cubo (placeholder) rotando, o un modelo si se asigna por item.
    ///
    /// Uso: ItemPreviewOverlay.Show(itemId, displayName, onClose);
    ///
    /// Notas tecnicas:
    ///   - Singleton DontDestroyOnLoad.
    ///   - El "preview 3D" se renderiza con una camara separada que apunta a un cubo
    ///     ubicado MUY lejos del origen (-10000, -10000, -10000) para no chocar con la
    ///     escena del juego. El resultado va a una RenderTexture y se muestra en una
    ///     RawImage del canvas.
    /// </summary>
    public class ItemPreviewOverlay : MonoBehaviour
    {
        private static ItemPreviewOverlay instance;

        [Header("Visual")]
        [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.92f);
        [SerializeField] private Color titleColor = new Color(0.96f, 0.85f, 0.42f, 1f);
        [SerializeField] private Color descColor = new Color(0.95f, 0.93f, 0.85f, 0.95f);
        [SerializeField] private Color cubeColor = new Color(0.85f, 0.7f, 0.28f, 1f);
        [SerializeField] private float rotationSpeed = 35f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Text titleText;
        private Text descText;
        private RawImage previewImg;

        // 3D preview infrastructure
        private Transform previewWorldRoot;
        private Camera previewCamera;
        private GameObject previewCube;
        private RenderTexture renderTexture;

        private float prevTimeScale;
        private CursorLockMode prevCursorLock;
        private bool prevCursorVisible;
        private bool consumeInputThisFrame;
        private Action onCloseCallback;

        // ---------- API publica ----------

        public static void Show(string itemId, string displayName, Action onClose = null)
        {
            EnsureInstance();
            if (instance.canvas.gameObject.activeSelf) return;

            instance.titleText.text = displayName ?? itemId;
            instance.descText.text = GetDescription(itemId);

            instance.prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            instance.prevCursorLock = Cursor.lockState;
            instance.prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            instance.canvas.gameObject.SetActive(true);
            instance.consumeInputThisFrame = true;
            instance.onCloseCallback = onClose;
            OverlayBlocker.Register();

            // Activar la camara y el cubo solo cuando se muestra para no gastar GPU.
            if (instance.previewWorldRoot != null) instance.previewWorldRoot.gameObject.SetActive(true);
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.gameObject.activeSelf) return;
            instance.canvas.gameObject.SetActive(false);
            if (instance.previewWorldRoot != null) instance.previewWorldRoot.gameObject.SetActive(false);
            Time.timeScale = instance.prevTimeScale > 0f ? instance.prevTimeScale : 1f;
            Cursor.lockState = instance.prevCursorLock;
            Cursor.visible = instance.prevCursorVisible;
            OverlayBlocker.Unregister();
            var cb = instance.onCloseCallback;
            instance.onCloseCallback = null;
            cb?.Invoke();
        }

        // ---------- lifecycle ----------

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("__ItemPreviewOverlay");
            instance = go.AddComponent<ItemPreviewOverlay>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            BuildPreviewWorld();
            canvas.gameObject.SetActive(false);
            if (previewWorldRoot != null) previewWorldRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf) return;

            // Rotar el cubo continuamente
            if (previewCube != null)
                previewCube.transform.Rotate(new Vector3(0.3f, 1f, 0.2f).normalized,
                                             rotationSpeed * Time.unscaledDeltaTime, Space.Self);

            if (consumeInputThisFrame) { consumeInputThisFrame = false; return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool dismiss = (kb != null && (kb.escapeKey.wasPressedThisFrame
                                          || kb.eKey.wasPressedThisFrame
                                          || kb.spaceKey.wasPressedThisFrame))
                        || (gp != null && (gp.buttonEast.wasPressedThisFrame
                                          || gp.buttonSouth.wasPressedThisFrame));
            if (dismiss) Close();
        }

        // ---------- descripcion por item ----------

        private static string GetDescription(string itemId)
        {
            switch (itemId)
            {
                case "linterna":
                    return "Una linterna vieja del padre.\nLa luz tiembla un poco.\nUtil cuando se apagan las luces.";
                case "destornillador":
                    return "Un destornillador de cabeza plana.\nSirve para forzar tornillos\nde rejillas y ductos.";
                default:
                    return "(Sin descripcion disponible)";
            }
        }

        // ---------- build UI ----------

        private void Build()
        {
            var canvasGo = new GameObject("ItemPreview_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;

            // Backdrop
            var bgGo = new GameObject("BG", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Title arriba
            titleText = MakeText(canvas.transform, "Title", "ITEM", 56, FontStyle.Bold, titleColor,
                new Vector2(0f, 380f), new Vector2(1200f, 80f), useTitleFont: true);
            var titleShadow = titleText.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(2f, -3f);

            // Linea decorativa
            var lineGo = new GameObject("TitleLine", typeof(RectTransform));
            var lineRT = lineGo.GetComponent<RectTransform>();
            lineRT.SetParent(canvas.transform, false);
            lineRT.anchorMin = lineRT.anchorMax = new Vector2(0.5f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.sizeDelta = new Vector2(220f, 2f);
            lineRT.anchoredPosition = new Vector2(0f, 330f);
            lineGo.AddComponent<Image>().color = new Color(titleColor.r, titleColor.g, titleColor.b, 0.6f);

            // RawImage central — donde se va a renderizar el cubo 3D
            renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 2;
            renderTexture.Create();

            var imgGo = new GameObject("Preview", typeof(RectTransform));
            var imgRT = imgGo.GetComponent<RectTransform>();
            imgRT.SetParent(canvas.transform, false);
            imgRT.anchorMin = imgRT.anchorMax = new Vector2(0.5f, 0.5f);
            imgRT.pivot = new Vector2(0.5f, 0.5f);
            imgRT.sizeDelta = new Vector2(420f, 420f);
            imgRT.anchoredPosition = new Vector2(0f, 60f);
            previewImg = imgGo.AddComponent<RawImage>();
            previewImg.texture = renderTexture;
            previewImg.raycastTarget = false;

            // Description text abajo
            descText = MakeText(canvas.transform, "Description", "", 22, FontStyle.Italic, descColor,
                new Vector2(0f, -240f), new Vector2(900f, 130f), useTitleFont: false);

            // Close hint footer
            MakeText(canvas.transform, "Hint",
                "Esc / Circle / E para cerrar",
                14, FontStyle.Normal,
                new Color(descColor.r, descColor.g, descColor.b, 0.55f),
                new Vector2(0f, -420f), new Vector2(900f, 26f), useTitleFont: false);
        }

        // ---------- build 3D preview world ----------

        private void BuildPreviewWorld()
        {
            // Coloca el cubo + camara MUY lejos del origen para no aparecer en la escena.
            var rootGo = new GameObject("ItemPreview_3DWorld");
            previewWorldRoot = rootGo.transform;
            previewWorldRoot.position = new Vector3(0f, -10000f, 0f);

            // Camara
            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(previewWorldRoot, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -3f);
            camGo.transform.localRotation = Quaternion.identity;
            previewCamera = camGo.AddComponent<Camera>();
            previewCamera.targetTexture = renderTexture;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.03f, 0.025f, 0.02f, 1f); // marron muy oscuro
            previewCamera.fieldOfView = 35f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 50f;
            // No queremos que esta camara afecte audio
            var listener = camGo.GetComponent<AudioListener>();
            if (listener != null) DestroyImmediate(listener);

            // Luz direccional para que el cubo tenga sombras suaves
            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(previewWorldRoot, false);
            lightGo.transform.localPosition = new Vector3(2f, 3f, -2f);
            lightGo.transform.localRotation = Quaternion.Euler(45f, -25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f, 1f);
            light.intensity = 1.2f;

            // Luz de relleno suave del otro lado
            var fillGo = new GameObject("PreviewFillLight");
            fillGo.transform.SetParent(previewWorldRoot, false);
            fillGo.transform.localPosition = new Vector3(-2f, -1f, -2f);
            fillGo.transform.localRotation = Quaternion.Euler(-15f, 30f, 0f);
            var fillLight = fillGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.4f, 0.45f, 0.55f, 1f);
            fillLight.intensity = 0.4f;

            // Cubo placeholder
            previewCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewCube.name = "PreviewCube";
            previewCube.transform.SetParent(previewWorldRoot, false);
            previewCube.transform.localPosition = Vector3.zero;
            previewCube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            // Material color
            var renderer = previewCube.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var mat = new Material(renderer.sharedMaterial);
                mat.color = cubeColor;
                renderer.sharedMaterial = mat;
            }
            // Sin Collider (no lo necesitamos)
            var col = previewCube.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        // ---------- helpers ----------

        private Text MakeText(Transform parent, string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta, bool useTitleFont = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = useTitleFont ? GetTitleFont() : GetDefaultFont();
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Font GetTitleFont()
        {
            if (MainMenuController.SharedTitleFont != null) return MainMenuController.SharedTitleFont;
            return GetDefaultFont();
        }

        private static Font GetDefaultFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
