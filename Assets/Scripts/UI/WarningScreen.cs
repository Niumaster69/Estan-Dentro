using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace EstanDentro.UI
{
    /// <summary>
    /// Pantalla de advertencia tipo Outlast/Visage: fade-in de un texto sobre fondo negro,
    /// hold, fade-out, y luego dispara onComplete (tipicamente VideoCinematicPlayer.Play()).
    ///
    /// Tambien soporta un FLASH SUBLIMINAL opcional: a mitad del hold, parpadea otro texto
    /// (frase corta perturbadora) por una fraccion de segundo. Lo suficiente para que se
    /// "vea sin verse".
    ///
    /// Setup minimo:
    ///   - Empty GameObject en la escena.
    ///   - Add Component -> WarningScreen.
    ///   - Configurar warningText y opcionalmente subliminalText.
    ///   - Wirear onComplete a VideoCinematicPlayer.Play() (o lo que siga).
    ///   - Si VideoCinematicPlayer esta en la misma escena, recordar que su playOnStart = false.
    /// </summary>
    public class WarningScreen : MonoBehaviour
    {
        [Header("Texto principal de advertencia")]
        [SerializeField, TextArea(3, 10), Tooltip("Texto del disclaimer. Soporta saltos de linea.")]
        private string warningText =
            "Este juego contiene escenas y sonidos diseñados para generar tension.\n\n" +
            "Si en algun momento te sientes mal, incomodo o abrumado,\n" +
            "pausa el juego y busca ayuda.\n\n" +
            "Linea de salud mental Colombia: 192 opc. 4 / 106 (Bogota).";

        [Header("Mensaje subliminal (flash a mitad del hold)")]
        [SerializeField, TextArea(1, 3), Tooltip("Texto que flashea muy brevemente. Deja vacio para deshabilitar.")]
        private string subliminalText = "ya estan dentro";
        [SerializeField, Tooltip("Duracion del flash subliminal en segundos. ~0.08-0.15 es visible-sin-procesar.")]
        private float subliminalFlashDuration = 0.1f;
        [SerializeField, Range(0f, 1f), Tooltip("Alpha del flash subliminal. 0.7-1.0 para que se note.")]
        private float subliminalAlpha = 0.85f;
        [SerializeField, Tooltip("Tinte rojizo del subliminal para que se sienta perturbador.")]
        private Color subliminalColor = new Color(0.85f, 0.12f, 0.12f, 1f);
        [SerializeField] private int subliminalFontSize = 64;
        [SerializeField, Tooltip("Posicion vertical del subliminal. 1 = top, 0.5 = centro, 0 = bottom. Default 0.92 = pegado arriba como titulo.")]
        private float subliminalVerticalAnchor = 0.92f;
        [SerializeField, Tooltip("Altura del rect del subliminal en pixeles. Mas chico = ocupa menos espacio vertical.")]
        private float subliminalRectHeight = 120f;

        [Header("Tiempos del fade")]
        [SerializeField] private float fadeInDuration = 1.8f;
        [SerializeField, Tooltip("Tiempo que el texto principal queda visible al maximo alpha.")]
        private float holdDuration = 5.5f;
        [SerializeField] private float fadeOutDuration = 1.6f;
        [SerializeField, Tooltip("Pausa en negro DESPUES del fade-out, antes de onComplete.")]
        private float blackHoldAfterFade = 0.6f;

        [Header("Estilo del texto principal")]
        [SerializeField] private int fontSize = 30;
        [SerializeField] private Color textColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color bgColor = Color.black;
        [SerializeField, Tooltip("Si true, muestra 'Cualquier tecla para continuar' debajo.")]
        private bool showSkipHint = true;
        [SerializeField] private string skipHintText = "[Cualquier tecla para continuar]";
        [SerializeField] private int skipHintFontSize = 16;

        [Header("Activacion")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Tooltip("Si true, cualquier tecla salta directo al video.")]
        private bool allowSkip = true;

        [Header("Evento al terminar")]
        public UnityEvent onComplete;

        private Canvas canvas;
        private CanvasGroup mainTextGroup;
        private CanvasGroup subliminalGroup;
        private CanvasGroup hintGroup;
        private bool playing;
        private bool completed;

        private void Start()
        {
            if (playOnStart) Play();
        }

        public void Play()
        {
            if (playing) return;
            EnsureCameraExists();
            BuildOverlay();
            StartCoroutine(RunRoutine());
        }

        // Sin Camera en la escena, Unity dibuja "Display 1 No cameras rendering" encima del Canvas overlay.
        // Crea una camara dummy que solo limpia a negro (no renderiza mundo) si no hay ninguna.
        private void EnsureCameraExists()
        {
            var existing = Camera.main;
            if (existing != null) return;
            var any = FindFirstObjectByType<Camera>();
            if (any != null && any.isActiveAndEnabled) return;

            var camGo = new GameObject("Warning_Camera");
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

        private IEnumerator RunRoutine()
        {
            playing = true;
            completed = false;

            // Fade-in del texto principal
            yield return FadeGroup(mainTextGroup, 0f, 1f, fadeInDuration);
            if (showSkipHint && hintGroup != null)
                StartCoroutine(FadeGroup(hintGroup, 0f, 0.55f, 0.6f));

            // Hold con posibilidad de skip + flash subliminal a mitad
            float halfHold = holdDuration * 0.5f;
            float t = 0f;
            bool subliminalShown = false;
            while (t < holdDuration)
            {
                if (allowSkip && AnyKeyPressed()) break;
                t += Time.unscaledDeltaTime;
                // Disparar flash subliminal a mitad del hold (si esta configurado)
                if (!subliminalShown && t >= halfHold && !string.IsNullOrEmpty(subliminalText))
                {
                    subliminalShown = true;
                    StartCoroutine(FlashSubliminal());
                }
                yield return null;
            }

            // Fade-out
            if (hintGroup != null) StartCoroutine(FadeGroup(hintGroup, hintGroup.alpha, 0f, 0.4f));
            yield return FadeGroup(mainTextGroup, mainTextGroup.alpha, 0f, fadeOutDuration);

            // Pausa en negro
            if (blackHoldAfterFade > 0f)
                yield return new WaitForSecondsRealtime(blackHoldAfterFade);

            // Limpiar y disparar evento
            Complete();
        }

        private IEnumerator FlashSubliminal()
        {
            if (subliminalGroup == null) yield break;
            subliminalGroup.alpha = subliminalAlpha;
            yield return new WaitForSecondsRealtime(subliminalFlashDuration);
            subliminalGroup.alpha = 0f;
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
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
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            cg.alpha = to;
        }

        private void BuildOverlay()
        {
            if (canvas != null) { canvas.gameObject.SetActive(true); return; }

            var canvasGo = new GameObject("WarningScreen_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 350; // arriba del video player (300) por si solapan

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

            // Texto principal centrado
            var mainGo = new GameObject("MainText", typeof(RectTransform));
            mainGo.transform.SetParent(canvas.transform, false);
            var mainRT = mainGo.GetComponent<RectTransform>();
            mainRT.anchorMin = new Vector2(0.5f, 0.5f);
            mainRT.anchorMax = new Vector2(0.5f, 0.5f);
            mainRT.pivot = new Vector2(0.5f, 0.5f);
            mainRT.sizeDelta = new Vector2(1400f, 500f);
            mainRT.anchoredPosition = Vector2.zero;
            mainTextGroup = mainGo.AddComponent<CanvasGroup>();
            mainTextGroup.alpha = 0f;
            var mainText = mainGo.AddComponent<Text>();
            mainText.font = GetFont();
            mainText.text = warningText;
            mainText.alignment = TextAnchor.MiddleCenter;
            mainText.fontSize = fontSize;
            mainText.color = textColor;
            mainText.raycastTarget = false;
            mainText.horizontalOverflow = HorizontalWrapMode.Wrap;
            mainText.verticalOverflow = VerticalWrapMode.Overflow;

            // Subliminal text (oculto por default) — posicionado arriba como titulo
            if (!string.IsNullOrEmpty(subliminalText))
            {
                var subGo = new GameObject("SubliminalText", typeof(RectTransform));
                subGo.transform.SetParent(canvas.transform, false);
                var subRT = subGo.GetComponent<RectTransform>();
                float vAnchor = Mathf.Clamp01(subliminalVerticalAnchor);
                subRT.anchorMin = new Vector2(0.5f, vAnchor);
                subRT.anchorMax = new Vector2(0.5f, vAnchor);
                subRT.pivot = new Vector2(0.5f, 0.5f);
                subRT.sizeDelta = new Vector2(1600f, Mathf.Max(40f, subliminalRectHeight));
                subRT.anchoredPosition = Vector2.zero;
                subliminalGroup = subGo.AddComponent<CanvasGroup>();
                subliminalGroup.alpha = 0f;
                var subText = subGo.AddComponent<Text>();
                subText.font = GetFont();
                subText.text = subliminalText.ToUpperInvariant();
                subText.alignment = TextAnchor.MiddleCenter;
                subText.fontSize = subliminalFontSize;
                subText.fontStyle = FontStyle.Bold;
                subText.color = subliminalColor;
                subText.raycastTarget = false;
            }

            // Skip hint abajo
            if (showSkipHint && !string.IsNullOrEmpty(skipHintText))
            {
                var hintGo = new GameObject("SkipHint", typeof(RectTransform));
                hintGo.transform.SetParent(canvas.transform, false);
                var hRT = hintGo.GetComponent<RectTransform>();
                hRT.anchorMin = new Vector2(0.5f, 0f);
                hRT.anchorMax = new Vector2(0.5f, 0f);
                hRT.pivot = new Vector2(0.5f, 0f);
                hRT.sizeDelta = new Vector2(800f, 40f);
                hRT.anchoredPosition = new Vector2(0f, 60f);
                hintGroup = hintGo.AddComponent<CanvasGroup>();
                hintGroup.alpha = 0f;
                var hintTxt = hintGo.AddComponent<Text>();
                hintTxt.font = GetFont();
                hintTxt.text = skipHintText;
                hintTxt.alignment = TextAnchor.MiddleCenter;
                hintTxt.fontSize = skipHintFontSize;
                hintTxt.color = new Color(textColor.r, textColor.g, textColor.b, 0.55f);
                hintTxt.raycastTarget = false;
            }
        }

        private static Font GetFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
