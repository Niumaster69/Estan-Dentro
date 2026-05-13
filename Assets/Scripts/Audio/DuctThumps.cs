using System.Collections;
using UnityEngine;
using EstanDentro.Player;
using EstanDentro.Stress;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Reproduce golpes aleatorios (thumps) en el ducto: el jugador escucha algo golpeando
    /// el exterior del ducto desde lejos. Cada thump:
    ///   - Toca un AudioClip random del array
    ///   - Sacude la camara del player brevemente
    ///   - Opcionalmente suma estres (pequeno spike)
    ///
    /// Pensado para `EsecenaUno-DuctosDeVentilacion`. Crea un GameObject vacio y pegale esto.
    /// El componente se auto-busca el CameraPivot del PlayerController.
    /// </summary>
    public class DuctThumps : MonoBehaviour
    {
        [Header("Clips")]
        [SerializeField, Tooltip("Pool de sonidos de golpe. Se elige uno al azar por evento. Si vacio, no suena.")]
        private AudioClip[] thumpClips;
        [SerializeField, Range(0f, 1f)] private float thumpVolume = 0.85f;
        [SerializeField, Tooltip("Variacion de pitch para que no suenen iguales repetidos.")]
        private float thumpPitchJitter = 0.08f;

        [Header("Intervalo entre thumps (segundos)")]
        [SerializeField] private float intervalMin = 15f;
        [SerializeField] private float intervalMax = 40f;
        [SerializeField, Tooltip("Delay antes del primer thump (segundos). Permite ambientar antes del susto.")]
        private float initialDelay = 8f;

        [Header("Camara — shake brevemente al thump")]
        [SerializeField, Tooltip("Amplitud del shake (m). 0.04 = 4cm de sacudida.")]
        private float shakeAmplitude = 0.04f;
        [SerializeField, Tooltip("Duracion del shake en segundos.")]
        private float shakeDuration = 0.4f;
        [SerializeField, Tooltip("Frecuencia del shake (Hz). Mas alto = mas vibracion rapida.")]
        private float shakeFrequency = 22f;

        [Header("Estres")]
        [SerializeField, Tooltip("Estres que suma cada thump. 0 = no suma.")]
        private float stressOnThump = 6f;

        [Header("Activacion")]
        [SerializeField] private bool autoStart = true;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private AudioSource src;
        private Transform cameraPivot;
        private Vector3 cameraPivotBaseLocalPos;
        private Coroutine shakeCo;
        private Coroutine loopCo;
        private bool running;

        private void Awake()
        {
            src = GetComponent<AudioSource>();
            if (src == null)
            {
                src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D global — se siente envolvente
                src.loop = false;
            }
        }

        private void OnEnable()
        {
            if (autoStart) StartThumps();
        }

        private void OnDisable()
        {
            StopThumps();
        }

        public void StartThumps()
        {
            if (running) return;
            running = true;
            loopCo = StartCoroutine(ThumpLoop());
        }

        public void StopThumps()
        {
            running = false;
            if (loopCo != null) { StopCoroutine(loopCo); loopCo = null; }
            if (shakeCo != null) { StopCoroutine(shakeCo); shakeCo = null; }
            RestoreCameraPivot();
        }

        private IEnumerator ThumpLoop()
        {
            if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);
            while (running)
            {
                float wait = Random.Range(intervalMin, intervalMax);
                yield return new WaitForSeconds(wait);
                if (!running) yield break;
                FireThump();
            }
        }

        private void FireThump()
        {
            // Audio
            if (thumpClips != null && thumpClips.Length > 0 && src != null)
            {
                var clip = thumpClips[Random.Range(0, thumpClips.Length)];
                if (clip != null)
                {
                    src.pitch = 1f + Random.Range(-thumpPitchJitter, thumpPitchJitter);
                    src.PlayOneShot(clip, thumpVolume);
                }
            }

            // Estres
            if (stressOnThump > 0f && StressSystem.Instance != null)
                StressSystem.Instance.Add(stressOnThump);

            // Shake
            if (shakeAmplitude > 0f && shakeDuration > 0f)
            {
                if (shakeCo != null) StopCoroutine(shakeCo);
                shakeCo = StartCoroutine(ShakeRoutine());
            }

            if (debugLog) Debug.Log("[DuctThumps] THUMP!");
        }

        private IEnumerator ShakeRoutine()
        {
            if (cameraPivot == null) ResolveCameraPivot();
            if (cameraPivot == null) yield break;

            Vector3 basePos = cameraPivot.localPosition;
            cameraPivotBaseLocalPos = basePos;
            float seedX = Random.Range(0f, 100f);
            float seedY = Random.Range(0f, 100f);
            float t = 0f;
            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float p = t / shakeDuration;
                float falloff = 1f - p;
                float dx = (Mathf.PerlinNoise(seedX, t * shakeFrequency) - 0.5f) * 2f * shakeAmplitude * falloff;
                float dy = (Mathf.PerlinNoise(seedY, t * shakeFrequency) - 0.5f) * 2f * shakeAmplitude * falloff;
                cameraPivot.localPosition = basePos + new Vector3(dx, dy, 0f);
                yield return null;
            }
            cameraPivot.localPosition = basePos;
        }

        private void ResolveCameraPivot()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null && pc.CameraPivot != null)
            {
                cameraPivot = pc.CameraPivot;
                cameraPivotBaseLocalPos = cameraPivot.localPosition;
            }
        }

        private void RestoreCameraPivot()
        {
            if (cameraPivot != null) cameraPivot.localPosition = cameraPivotBaseLocalPos;
        }
    }
}
