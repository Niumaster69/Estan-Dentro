using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace EstanDentro.UI
{
    /// <summary>
    /// Pantalla de creditos con mensaje reflexivo previo. Diseñada para mostrarse al final
    /// del Capitulo 1 despues del video del twist.
    ///
    /// Estructura:
    ///   1. Fade-in de un mensaje reflexivo sobre respirar y pedir ayuda
    ///   2. Hold del mensaje
    ///   3. Fade-out
    ///   4. Lista de creditos con scroll automatico o estatico
    ///   5. Fade-out y onComplete (tipicamente carga MainMenu)
    ///
    /// Skip: cualquier tecla acelera la transicion (no salta completo, solo acorta el hold).
    /// </summary>
    public class CreditsScreen : MonoBehaviour
    {
        [Header("Mensaje reflexivo previo")]
        [SerializeField, TextArea(4, 10)]
        private string reflectiveMessage =
            "Si lograste cerrar los ojos en este viaje,\n" +
            "respira tambien al cerrar el juego.\n\n" +
            "Lo que viviste aqui es solo un eco.\n" +
            "Pero la respiracion que aprendiste\n" +
            "es real y siempre estara contigo.\n\n" +
            "Si en algun momento sentiste algo mas\n" +
            "que el juego, no estas solo. Habla con alguien.\n" +
            "Linea de salud mental Colombia: 192 opc. 4 / 106 (Bogota).";

        [SerializeField] private float reflectiveFadeIn = 2f;
        [SerializeField] private float reflectiveHold = 8f;
        [SerializeField] private float reflectiveFadeOut = 1.5f;
        [SerializeField] private int reflectiveFontSize = 24;

        [Header("Creditos")]
        [SerializeField, TextArea(8, 30)]
        private string creditsText =
            "ESTAN DENTRO\n" +
            "Capitulo 1 — prototipo\n\n\n" +
            "EQUIPO\n\n" +
            "Duvan — Mecanicas, programacion y FX\n" +
            "Henry — Modelado 3D y postprocesamiento\n" +
            "Carlos — Audio y QA\n\n\n" +
            "NARRATIVA\n\n" +
            "Idea original y diseño narrativo: el equipo\n\n\n" +
            "AUDIO\n\n" +
            "Freesound.org — efectos y ambientes\n" +
            "Soundtrack original\n\n\n" +
            "GRACIAS A\n\n" +
            "Profesor y companeros\n" +
            "A quien tomo un respiro durante el juego\n\n\n" +
            "2026";
        [SerializeField] private float creditsFadeIn = 1.5f;
        [SerializeField] private float creditsHold = 10f;
        [SerializeField] private float creditsFadeOut = 1.5f;
        [SerializeField] private int creditsFontSize = 26;

        [Header("Estilo")]
        [SerializeField] private Color textColor = new Color(0.94f, 0.88f, 0.7f, 1f);
        [SerializeField] private Color bgColor = Color.black;

        [Header("Activacion")]
        [SerializeField] private bool playOnStart = false;
        [SerializeField, Tooltip("Si true, cualquier tecla durante el reflective acelera al credits, y durante credits cierra.")]
        private bool allowSkip = true;

        [Header("Evento al terminar")]
        public UnityEvent onComplete;

        private Canvas canvas;
        private CanvasGroup reflectiveGroup;
        private Text reflectiveLabel;
        private CanvasGroup creditsGroup;
        private Text creditsLabel;
        private bool playing;

        private void Start()
        {
            if (playOnStart) Play();
        }

        public void Play()
        {
            if (playing) return;
            EnsureCameraExists();
            BuildOverlay();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            playing = true;

            // Fase 1: mensaje reflexivo
            reflectiveLabel.text = reflectiveMessage;
            yield return FadeGroup(reflectiveGroup, 0f, 1f, reflectiveFadeIn);

            float t = 0f;
            while (t < reflectiveHold)
            {
                if (allowSkip && AnyKeyPressed()) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return FadeGroup(reflectiveGroup, reflectiveGroup.alpha, 0f, reflectiveFadeOut);
            reflectiveGroup.gameObject.SetActive(false);

            // Pausa breve en negro
            yield return new WaitForSecondsRealtime(0.8f);

            // Fase 2: creditos
            creditsLabel.text = creditsText;
            yield return FadeGroup(creditsGroup, 0f, 1f, creditsFadeIn);

            t = 0f;
            while (t < creditsHold)
            {
                if (allowSkip && AnyKeyPressed()) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return FadeGroup(creditsGroup, creditsGroup.alpha, 0f, creditsFadeOut);

            // Pausa final en negro
            yield return new WaitForSecondsRealtime(0.6f);

            playing = false;
            onComplete?.Invoke();
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private bool AnyKeyPressed()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame)
                || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                || gp.buttonNorth.wasPressedThisFrame
                                || gp.buttonEast.wasPressedThisFrame
                                || gp.buttonWest.wasPressedThisFrame
                                || gp.startButton.wasPressedThisFrame));
        }

        private IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float duration)
        {
            if (cg == null) yield break;
            cg.alpha = from;
            float tt = 0f;
            while (tt < duration)
            {
                tt += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(tt / duration));
                yield return null;
            }
            cg.alpha = to;
        }

        private void BuildOverlay()
        {
            if (canvas != null) { canvas.gameObject.SetActive(true); return; }

            var canvasGo = new GameObject("CreditsScreen_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo negro full
            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // Reflective text
            var reflectiveGo = new GameObject("ReflectiveText", typeof(RectTransform));
            reflectiveGo.transform.SetParent(canvas.transform, false);
            var refRT = reflectiveGo.GetComponent<RectTransform>();
            refRT.anchorMin = new Vector2(0.5f, 0.5f);
            refRT.anchorMax = new Vector2(0.5f, 0.5f);
            refRT.pivot = new Vector2(0.5f, 0.5f);
            refRT.sizeDelta = new Vector2(1400f, 600f);
            refRT.anchoredPosition = Vector2.zero;
            reflectiveGroup = reflectiveGo.AddComponent<CanvasGroup>();
            reflectiveGroup.alpha = 0f;
            reflectiveLabel = reflectiveGo.AddComponent<Text>();
            reflectiveLabel.font = GetFont();
            reflectiveLabel.text = reflectiveMessage;
            reflectiveLabel.alignment = TextAnchor.MiddleCenter;
            reflectiveLabel.fontSize = reflectiveFontSize;
            reflectiveLabel.fontStyle = FontStyle.Italic;
            reflectiveLabel.color = textColor;
            reflectiveLabel.raycastTarget = false;
            reflectiveLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            reflectiveLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // Credits text
            var creditsGo = new GameObject("CreditsText", typeof(RectTransform));
            creditsGo.transform.SetParent(canvas.transform, false);
            var crRT = creditsGo.GetComponent<RectTransform>();
            crRT.anchorMin = new Vector2(0.5f, 0.5f);
            crRT.anchorMax = new Vector2(0.5f, 0.5f);
            crRT.pivot = new Vector2(0.5f, 0.5f);
            crRT.sizeDelta = new Vector2(1400f, 900f);
            crRT.anchoredPosition = Vector2.zero;
            creditsGroup = creditsGo.AddComponent<CanvasGroup>();
            creditsGroup.alpha = 0f;
            creditsLabel = creditsGo.AddComponent<Text>();
            creditsLabel.font = GetFont();
            creditsLabel.text = creditsText;
            creditsLabel.alignment = TextAnchor.MiddleCenter;
            creditsLabel.fontSize = creditsFontSize;
            creditsLabel.color = textColor;
            creditsLabel.raycastTarget = false;
            creditsLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsLabel.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static Font GetFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        private void EnsureCameraExists()
        {
            var existing = Camera.main;
            if (existing != null) return;
            var any = FindFirstObjectByType<Camera>();
            if (any != null && any.isActiveAndEnabled) return;

            var camGo = new GameObject("Credits_Camera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
            cam.cullingMask = 0;
            cam.orthographic = true;
            camGo.tag = "MainCamera";
            if (FindFirstObjectByType<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
        }
    }
}
