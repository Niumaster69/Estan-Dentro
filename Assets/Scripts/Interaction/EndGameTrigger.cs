using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using EstanDentro.Player;
using EstanDentro.UI;
using EstanDentro.Network;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Disparador del FINAL DEL JUEGO. Pegalo a un GameObject con Collider (Is Trigger = true)
    /// al final del laberinto de Ductos. Cuando el Player entra:
    ///   1. Bloquea input del player.
    ///   2. Fade a negro.
    ///   3. Muestra slides del twist (1705, padre vivo, etc.) en negro.
    ///   4. Cierra la partida en la API (ChapterFlow.EndChapter(true)) y evalua logros.
    ///   5. Carga MainMenu con tip final.
    ///
    /// Todo en la misma escena: no carga otra escena para la cinematica.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EndGameTrigger : MonoBehaviour
    {
        [System.Serializable]
        public class FinalSlide
        {
            [TextArea(1, 4)] public string text;
            public float durationSeconds = 3f;
            public AudioClip audio;
        }

        [Header("Slides del twist")]
        [SerializeField] private FinalSlide[] slides = new FinalSlide[]
        {
            new FinalSlide { text = "Llegaste.", durationSeconds = 2.5f },
            new FinalSlide { text = "El codigo era 1705.", durationSeconds = 3f },
            new FinalSlide { text = "La fecha del accidente.", durationSeconds = 3f },
            new FinalSlide { text = "El dia que murio tu padre.", durationSeconds = 3.5f },
            new FinalSlide { text = "...o eso creiste.", durationSeconds = 3f },
            new FinalSlide { text = "El nunca murio.", durationSeconds = 3f },
            new FinalSlide { text = "Tu si.", durationSeconds = 4f },
        };

        [Header("Tiempos")]
        [SerializeField] private float fadeToBlackDuration = 1.5f;
        [SerializeField] private float slideFadeIn = 0.8f;
        [SerializeField] private float slideFadeOut = 0.8f;
        [SerializeField] private float beforeReturnToMenuDelay = 2f;

        [Header("Estilo")]
        [SerializeField] private int slideFontSize = 44;
        [SerializeField] private Color slideTextColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color bgColor = Color.black;

        [Header("Audio")]
        [SerializeField] private AudioClip ambientStingerOnEnter;
        [SerializeField, Range(0f, 1f)] private float audioVolume = 0.9f;

        [Header("Video cinematica (opcional - reemplaza slides)")]
        [SerializeField, Tooltip("Si asignado, reproduce este video en lugar de los slides del twist. Recomendado: '5 v1.mov' o '5 v2o.mov'.")]
        private VideoClip overrideVideoClip;
        [SerializeField, Range(0f, 1f)] private float videoVolume = 1f;

        [Header("Salida")]
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private string loadingTip = "Volves al menu... o algo te trae de vuelta.";
        [SerializeField, Tooltip("Si esta seteado, completa esta mision al disparar.")]
        private string completedMissionId = "salir_ductos";
        [SerializeField, Tooltip("Si true, muestra CreditsScreen despues del video del twist, antes de cargar MainMenu.")]
        private bool showCreditsAfterTwist = true;

        [Header("Comportamiento")]
        [SerializeField, Tooltip("Si true, solo se dispara una vez por ejecucion.")]
        private bool oneShot = true;

        private bool triggered;
        private AudioSource audioSrc;
        private Canvas canvas;
        private CanvasGroup bgGroup;
        private Text slideText;
        private CanvasGroup slideGroup;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null)
            {
                audioSrc = gameObject.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneShot && triggered) return;
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;
            triggered = true;
            StartCoroutine(EndGameSequence(pc));
        }

        private IEnumerator EndGameSequence(PlayerController pc)
        {
            Debug.Log("[EndGame] Disparando cinematica final.");

            // 1) Bloquear input + congelar CharacterController
            CharacterController cc = pc.GetComponent<CharacterController>();
            pc.InputEnabled = false;
            pc.enabled = false;
            if (cc != null) cc.enabled = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            BuildOverlay();

            // 2) Audio stinger opcional
            if (ambientStingerOnEnter != null)
                audioSrc.PlayOneShot(ambientStingerOnEnter, audioVolume);

            // 3) Fade a negro
            yield return FadeCanvasGroup(bgGroup, 0f, 1f, fadeToBlackDuration);

            // 4) Cinematica: video (si esta asignado) o slides
            if (overrideVideoClip != null)
            {
                var videoGo = new GameObject("EndGame_Video");
                var vcp = videoGo.AddComponent<VideoCinematicPlayer>();
                vcp.SetClip(overrideVideoClip, videoVolume);
                bool videoDone = false;
                vcp.onComplete = new UnityEngine.Events.UnityEvent();
                vcp.onComplete.AddListener(() => videoDone = true);
                vcp.Play();
                yield return new WaitUntil(() => videoDone);
                Destroy(videoGo);
            }
            else
            {
                slideGroup.alpha = 0f;
                foreach (var s in slides)
                {
                    slideText.text = s.text;
                    slideText.fontSize = slideFontSize;
                    if (s.audio != null) audioSrc.PlayOneShot(s.audio, audioVolume);
                    yield return FadeCanvasGroup(slideGroup, 0f, 1f, slideFadeIn);
                    yield return new WaitForSecondsRealtime(s.durationSeconds);
                    yield return FadeCanvasGroup(slideGroup, 1f, 0f, slideFadeOut);
                }
            }

            // 5) Completar mision (si aplica)
            if (!string.IsNullOrEmpty(completedMissionId) && Inventory.Inventory.Instance != null)
                Inventory.Inventory.Instance.CompleteMission(completedMissionId);

            // 6) Cerrar partida en API + evaluar logros finales
            yield return ChapterFlow.EndChapter(completado: true);

            // 7) Pequeno respiro en negro y vuelta al menu
            yield return new WaitForSecondsRealtime(beforeReturnToMenuDelay);

            // 8) Creditos (mensaje reflexivo + equipo) antes del MainMenu
            if (showCreditsAfterTwist)
            {
                var creditsGo = new GameObject("EndGame_Credits");
                var credits = creditsGo.AddComponent<EstanDentro.UI.CreditsScreen>();
                bool creditsDone = false;
                credits.onComplete = new UnityEngine.Events.UnityEvent();
                credits.onComplete.AddListener(() => creditsDone = true);
                credits.Play();
                yield return new WaitUntil(() => creditsDone);
                Destroy(creditsGo);
            }

            Time.timeScale = 1f;
            SceneTransition.LoadScene(nextSceneName, tip: loadingTip);
        }

        // ---------- Overlay ----------

        private void BuildOverlay()
        {
            if (canvas != null) return;

            var go = new GameObject("EndGame_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 260; // por encima de GameOver (250)

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            // Fondo negro con CanvasGroup propio para fade
            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;
            bgGroup = bgGo.AddComponent<CanvasGroup>();
            bgGroup.alpha = 0f;
            bgGroup.blocksRaycasts = true;

            // Texto del slide encima
            var textGo = new GameObject("Slide", typeof(RectTransform));
            textGo.transform.SetParent(canvas.transform, false);
            var tRT = textGo.GetComponent<RectTransform>();
            tRT.anchorMin = tRT.anchorMax = new Vector2(0.5f, 0.5f);
            tRT.pivot = new Vector2(0.5f, 0.5f);
            tRT.sizeDelta = new Vector2(1500f, 300f);
            tRT.anchoredPosition = Vector2.zero;

            slideGroup = textGo.AddComponent<CanvasGroup>();
            slideGroup.alpha = 0f;

            slideText = textGo.AddComponent<Text>();
            slideText.font = GetBodyFont();
            slideText.text = "";
            slideText.alignment = TextAnchor.MiddleCenter;
            slideText.fontSize = slideFontSize;
            slideText.fontStyle = FontStyle.Italic;
            slideText.color = slideTextColor;
            slideText.raycastTarget = false;
        }

        private static Font GetBodyFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            cg.alpha = to;
        }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = new Color(0.85f, 0.15f, 0.15f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider b) Gizmos.DrawCube(b.center, b.size);
            else if (col is SphereCollider s) Gizmos.DrawSphere(s.center, s.radius);
        }
    }
}
