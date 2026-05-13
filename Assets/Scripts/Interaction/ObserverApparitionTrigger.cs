using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EstanDentro.Player;
using EstanDentro.UI;
using EstanDentro.Breathing;
using EstanDentro.Network;
using EstanDentro.Stress;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Cinematica de PRIMERA APARICION del Observador. NO es el sistema completo de intrusion
    /// (deteccion de mirada) que vendra en Sprint 3 — es solo el momento scripted donde el
    /// jugador "lo siente" por primera vez. La silueta del SilhouetteManager.stalker ya esta
    /// en la periferia constantemente; este trigger marca el beat narrativo.
    ///
    /// Al entrar el player:
    ///   1. Stinger de audio.
    ///   2. Camera shake breve (sin bloquear movimiento — el horror viene de seguir caminando).
    ///   3. Sube stress de golpe.
    ///   4. Fuerza el minijuego de respiracion (queda libre tras forcedBreathingSeconds o stress baja).
    ///   5. Slide diegetico breve (no-modal, esquina) opcional.
    ///   6. Marca GameSession.ObserverTriggeredAtLeastOnce.
    ///
    /// Setup minimo:
    ///   - Empty con Collider isTrigger=true en el pasillo / zona donde queres el beat.
    ///   - Asignar stingerClip (opcional).
    ///   - One-shot por default (no se repite en la misma escena).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ObserverApparitionTrigger : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField, Tooltip("Stinger / golpe sonoro al disparar (drone, whisper, sub bass).")]
        private AudioClip stingerClip;
        [SerializeField, Range(0f, 1f)] private float stingerVolume = 0.9f;

        [Header("Stress")]
        [SerializeField, Tooltip("Cuanto stress agregar al disparar (sumar al StressSystem). 0 = no tocar.")]
        private float stressPunch = 35f;

        [Header("Respiracion forzada")]
        [SerializeField, Tooltip("Tras disparar, fuerza el minijuego de respiracion por X segundos. 0 = no forzar.")]
        private float forcedBreathingSeconds = 14f;

        [Header("Camera shake")]
        [SerializeField, Tooltip("Duracion total del shake (s).")]
        private float shakeDuration = 1.2f;
        [SerializeField, Tooltip("Amplitud maxima en grados del shake.")]
        private float shakeMaxAngle = 1.6f;
        [SerializeField] private float shakeFrequency = 14f;

        [Header("Slide diegetico (opcional, NO bloqueante)")]
        [SerializeField, TextArea(1, 3), Tooltip("Texto breve que parpadea en la esquina abajo. Vacio = no slide.")]
        private string slideText = "Hay alguien.";
        [SerializeField] private float slideFadeInSeconds = 0.5f;
        [SerializeField] private float slideHoldSeconds = 2.2f;
        [SerializeField] private float slideFadeOutSeconds = 1f;
        [SerializeField] private Color slideColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private int slideFontSize = 28;

        [Header("Comportamiento")]
        [SerializeField] private bool oneShot = true;

        private bool triggered;
        private AudioSource audioSrc;
        private Transform shakeTarget;
        private Coroutine shakeCo;

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
            Fire(pc);
        }

        private void Fire(PlayerController pc)
        {
            Debug.Log("[Observador] Primera aparicion disparada.");

            GameSession.ObserverTriggeredAtLeastOnce = true;

            if (stingerClip != null) audioSrc.PlayOneShot(stingerClip, stingerVolume);

            if (stressPunch > 0f && StressSystem.Instance != null)
                StressSystem.Instance.Add(stressPunch);

            if (forcedBreathingSeconds > 0f && BreathingMinigame.Instance != null)
            {
                BreathingMinigame.Instance.ForceShow();
                StartCoroutine(ReleaseForcedBreathingAfter(forcedBreathingSeconds));
            }

            shakeTarget = pc.CameraPivot;
            if (shakeTarget != null)
            {
                if (shakeCo != null) StopCoroutine(shakeCo);
                shakeCo = StartCoroutine(ShakeRoutine());
            }

            if (!string.IsNullOrEmpty(slideText)) StartCoroutine(ShowSlide());
        }

        private IEnumerator ReleaseForcedBreathingAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (BreathingMinigame.Instance != null) BreathingMinigame.Instance.ForceHide();
        }

        private IEnumerator ShakeRoutine()
        {
            float seed = Random.Range(0f, 100f);
            float t = 0f;
            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float falloff = 1f - Mathf.Clamp01(t / shakeDuration);
                float amp = shakeMaxAngle * falloff;
                float dx = (Mathf.PerlinNoise(seed, t * shakeFrequency) - 0.5f) * 2f * amp;
                float dy = (Mathf.PerlinNoise(seed + 50f, t * shakeFrequency) - 0.5f) * 2f * amp;
                float dz = (Mathf.PerlinNoise(seed + 100f, t * shakeFrequency) - 0.5f) * 2f * amp * 0.5f;
                var e = shakeTarget.localEulerAngles;
                shakeTarget.localEulerAngles = new Vector3(e.x + dx, e.y + dy, dz);
                yield return null;
            }
        }

        private IEnumerator ShowSlide()
        {
            var canvasGo = new GameObject("ObserverSlide_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var textGo = new GameObject("Slide", typeof(RectTransform));
            textGo.transform.SetParent(canvas.transform, false);
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(1000f, 60f);
            rt.anchoredPosition = new Vector2(0f, 160f);
            var txt = textGo.AddComponent<Text>();
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.font = f;
            txt.text = slideText;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = slideFontSize;
            txt.fontStyle = FontStyle.Italic;
            txt.color = slideColor;
            txt.raycastTarget = false;

            float t = 0f;
            while (t < slideFadeInSeconds) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Clamp01(t / slideFadeInSeconds); yield return null; }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(slideHoldSeconds);
            t = 0f;
            while (t < slideFadeOutSeconds) { t += Time.unscaledDeltaTime; cg.alpha = 1f - Mathf.Clamp01(t / slideFadeOutSeconds); yield return null; }
            cg.alpha = 0f;
            Destroy(canvasGo);
        }

        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = new Color(0.95f, 0.3f, 0.6f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider b) Gizmos.DrawCube(b.center, b.size);
            else if (col is SphereCollider s) Gizmos.DrawSphere(s.center, s.radius);
        }
    }
}
