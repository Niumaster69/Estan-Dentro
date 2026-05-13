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
    /// Trigger al final del laberinto de Ductos. Dispara la transicion Acto 2 -> Acto 3.
    ///
    /// Flujo: bloquea input, fade negro, slides cortos, setea GameSession.NextSpawnPointId
    /// (asi PlayerSpawner en EscenaUno-salonPrincipal sabe spawnear en la zona de salas finales en
    /// vez del salon inicial), y carga la escena del salon principal con loading screen.
    ///
    /// IMPORTANTE: la cinematica FINAL del juego (EndGameTrigger) va a otra zona de las salas finales,
    /// NO aqui. Este trigger es la transicion limpia para REENTRAR al salonPrincipal.
    ///
    /// Setup minimo:
    ///   - Empty GameObject con Collider isTrigger=true al final del laberinto de ductos.
    ///   - Agregar este script.
    ///   - En EscenaUno-salonPrincipal: PlayerSpawnPoint con id = "salas_finales" en la entrada
    ///     de las salas finales, y SkipWakeUp=true para que no corra el despertar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitDuctsTrigger : MonoBehaviour
    {
        [System.Serializable]
        public class Slide
        {
            [TextArea(1, 4)] public string text;
            public float durationSeconds = 2.5f;
            public AudioClip audio;
        }

        [Header("Slides")]
        [SerializeField] private Slide[] slides = new Slide[]
        {
            new Slide { text = "Saliste.", durationSeconds = 2f },
            new Slide { text = "Pero algo sigue mirandote.", durationSeconds = 3f },
        };

        [Header("Tiempos")]
        [SerializeField] private float fadeToBlackDuration = 1.2f;
        [SerializeField] private float slideFadeIn = 0.6f;
        [SerializeField] private float slideFadeOut = 0.6f;

        [Header("Estilo")]
        [SerializeField] private int slideFontSize = 40;
        [SerializeField] private Color slideTextColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color bgColor = Color.black;

        [Header("Audio (opcional)")]
        [SerializeField, Tooltip("Stinger / golpe ambiental al entrar al trigger.")]
        private AudioClip enterStinger;
        [SerializeField, Range(0f, 1f)] private float audioVolume = 0.85f;

        [Header("Video cinematica (opcional - reemplaza slides)")]
        [SerializeField, Tooltip("Si asignado, reproduce este video como overlay despues del fade a negro en lugar de los slides. Recomendado: '4.mov' (flashback del accidente al salir de ductos).")]
        private VideoClip overrideVideoClip;
        [SerializeField, Range(0f, 1f)] private float videoVolume = 1f;

        [Header("Transicion")]
        [SerializeField, Tooltip("Escena destino. Por default vuelve al salonPrincipal donde estan las salas finales.")]
        private string nextSceneName = "EscenaUno-salonPrincipal";
        [SerializeField, Tooltip("Id del PlayerSpawnPoint donde debe aparecer el player al cargar la siguiente escena.")]
        private string targetSpawnId = "salas_finales";
        [SerializeField, TextArea(1, 3)] private string loadingTip = "Volviste, pero no al mismo lugar.";

        [Header("Comportamiento")]
        [SerializeField] private bool oneShot = true;

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
            StartCoroutine(ExitSequence(pc));
        }

        private IEnumerator ExitSequence(PlayerController pc)
        {
            Debug.Log("[ExitDucts] Disparando transicion a salas finales.");

            CharacterController cc = pc.GetComponent<CharacterController>();
            pc.InputEnabled = false;
            pc.enabled = false;
            if (cc != null) cc.enabled = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            BuildOverlay();

            if (enterStinger != null) audioSrc.PlayOneShot(enterStinger, audioVolume);

            yield return FadeCanvasGroup(bgGroup, 0f, 1f, fadeToBlackDuration);

            // Video del accidente (si esta asignado) o slides en negro
            if (overrideVideoClip != null)
            {
                var videoGo = new GameObject("ExitDucts_Video");
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

            // Setear spawn point destino antes de cambiar de escena
            GameSession.NextSpawnPointId = targetSpawnId;

            Time.timeScale = 1f;
            SceneTransition.LoadScene(nextSceneName, tip: loadingTip);
        }

        // ---------- Overlay ----------

        private void BuildOverlay()
        {
            if (canvas != null) return;

            var go = new GameObject("ExitDucts_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;
            bgGroup = bgGo.AddComponent<CanvasGroup>();
            bgGroup.alpha = 0f;
            bgGroup.blocksRaycasts = true;

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
            Gizmos.color = new Color(0.3f, 0.6f, 0.95f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider b) Gizmos.DrawCube(b.center, b.size);
            else if (col is SphereCollider s) Gizmos.DrawSphere(s.center, s.radius);
        }
    }
}
