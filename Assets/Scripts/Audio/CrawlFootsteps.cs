using UnityEngine;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Pasos al gatear / arrastrarse por un ducto. Reproduce un clip random del pool en un
    /// ritmo cuando el jugador se mueve. Volume y velocidad del ritmo escalan con la velocidad
    /// del CharacterController. Pensado para que se sienta organico, no como pasos normales.
    ///
    /// Setup:
    ///   - Pegar en el mismo GO del Player (donde esta CharacterController) o en un hijo.
    ///   - Si no encuentra CharacterController, busca uno con FindFirst.
    ///   - Asignar crawlClips (mejor 3-5 variantes de sonido para que no se repita).
    /// </summary>
    public class CrawlFootsteps : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField, Tooltip("Pool de sonidos de paso al gatear. Mejor 3-5 variantes para evitar repeticion.")]
        private AudioClip[] crawlClips;
        [SerializeField, Range(0f, 1f), Tooltip("Volumen base. Se multiplica por la velocidad normalizada.")]
        private float baseVolume = 0.45f;
        [SerializeField, Tooltip("Variacion de pitch entre pasos para que no sean iguales.")]
        private float pitchJitter = 0.12f;

        [Header("Ritmo del paso")]
        [SerializeField, Tooltip("Intervalo base entre pasos (segundos) cuando el player se mueve a velocidad full.")]
        private float baseStepInterval = 0.7f;
        [SerializeField, Tooltip("Velocidad minima (m/s) del CharacterController para considerar 'movimiento'. Por debajo: no se reproduce.")]
        private float minSpeed = 0.15f;
        [SerializeField, Tooltip("Velocidad de referencia (m/s) para escalar volumen y ritmo. Velocidades mas altas no aumentan mas alla.")]
        private float referenceSpeed = 2.5f;

        [Header("Target")]
        [SerializeField, Tooltip("CharacterController del player. Si vacio, se busca en el mismo GO o con FindFirst.")]
        private CharacterController target;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private AudioSource src;
        private float stepTimer;

        private void Awake()
        {
            if (target == null) target = GetComponent<CharacterController>();
            if (target == null) target = FindFirstObjectByType<CharacterController>();
            src = GetComponent<AudioSource>();
            if (src == null)
            {
                src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D, mas cercano al "tu propio cuerpo"
                src.loop = false;
            }
        }

        private void Update()
        {
            if (target == null || crawlClips == null || crawlClips.Length == 0) return;

            // Velocidad horizontal del player (sin gravedad)
            Vector3 v = target.velocity;
            v.y = 0f;
            float speed = v.magnitude;

            if (speed < minSpeed)
            {
                // Sin movimiento: resetear timer para que el primer paso suene rapido al arrancar
                stepTimer = baseStepInterval * 0.4f;
                return;
            }

            float speedNorm = Mathf.Clamp01(speed / referenceSpeed);
            float interval = baseStepInterval * Mathf.Lerp(1.4f, 0.7f, speedNorm); // mas lento si mas lento
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayStep(speedNorm);
                stepTimer = interval;
            }
        }

        private void PlayStep(float speedNorm)
        {
            var clip = crawlClips[Random.Range(0, crawlClips.Length)];
            if (clip == null) return;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            float vol = baseVolume * Mathf.Lerp(0.55f, 1f, speedNorm);
            src.PlayOneShot(clip, vol);
            if (debugLog) Debug.Log($"[CrawlFootsteps] step (speedNorm={speedNorm:F2}, vol={vol:F2})");
        }
    }
}
