using System.Collections;
using UnityEngine;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Reproduce un loop ambiental continuo (ej. brisa, viento) + eventos puntuales aleatorios
    /// (ej. crujidos de madera, golpes de tuberia, ruido lejano) con pausas variables.
    ///
    /// Pensado para la ambientacion del salon despues del despertar: brisa de fondo subliminal
    /// + crujidos esporadicos que generan tension sin ser invasivos.
    ///
    /// Setup minimo:
    ///   - Empty GameObject en la escena.
    ///   - Add Component -> AmbientLoopWithRandomEvents.
    ///   - Asignar loopClip (brisa) y el array randomEventClips (crujidos).
    ///   - Configurar volumenes y rangos de pausa.
    ///   - playOnStart = true para que arranque solo al cargar la escena.
    /// </summary>
    public class AmbientLoopWithRandomEvents : MonoBehaviour
    {
        [Header("Loop continuo (brisa / aire / drone bajito)")]
        [SerializeField] private AudioClip loopClip;
        [SerializeField, Range(0f, 1f)] private float loopVolume = 0.5f;
        [SerializeField, Tooltip("Pequena variacion de pitch del loop para que no se sienta plano.")]
        private float loopPitchJitter = 0.04f;

        [Header("Eventos aleatorios (crujidos / golpes esporadicos)")]
        [SerializeField, Tooltip("Lista de clips de eventos puntuales. Cada disparo elige uno al azar.")]
        private AudioClip[] randomEventClips;
        [SerializeField, Range(0f, 1f)] private float randomEventVolume = 0.7f;
        [SerializeField, Tooltip("Rango de pausa (segundos) entre eventos. Ej (4, 12) = un evento cada 4 a 12s.")]
        private Vector2 randomEventPauseRange = new Vector2(4f, 12f);
        [SerializeField, Tooltip("Variacion de pitch por evento para que no suenen iguales repetidos.")]
        private float randomEventPitchJitter = 0.08f;
        [SerializeField, Tooltip("Delay antes del PRIMER evento random (segundos). Util para no saturar el inicio de la escena.")]
        private float initialDelayBeforeFirstEvent = 4f;

        [Header("Activacion")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Tooltip("Fade-in del loop (segundos).")]
        private float fadeInSeconds = 2.5f;

        private AudioSource loopSrc;
        private AudioSource eventSrc;
        private Coroutine randomEventsCo;
        private bool playing;
        private bool wasPlaying;

        private float lastRestartTime = -999f;
        private bool inFadeIn = false;
        private void Update()
        {
            if (!playing || loopSrc == null || loopSrc.clip == null) return;

            // FORZAR VOLUMEN solo cuando NO estamos en fade-in (sino el fade se sobreescribe)
            if (!inFadeIn && Mathf.Abs(loopSrc.volume - loopVolume) > 0.005f)
                loopSrc.volume = loopVolume;

            // FORZAR mute = false (por si algo lo silencio)
            if (loopSrc.mute) loopSrc.mute = false;

            // FORZAR priority alto
            if (loopSrc.priority != 0) loopSrc.priority = 0;

            // AUTO-RESTART: si por algun motivo no esta tocando, lo arrancamos
            if (!loopSrc.isPlaying)
            {
                loopSrc.Play();
                if (Time.realtimeSinceStartup - lastRestartTime > 1f)
                {
                    Debug.Log($"[Ambient] AUTO-RESTART del loop. Clip={loopSrc.clip.name} vol={loopSrc.volume} mute={loopSrc.mute} enabled={loopSrc.enabled}");
                    lastRestartTime = Time.realtimeSinceStartup;
                }
            }
        }

        private void Awake()
        {
            BuildSources();
        }

        private void Start()
        {
            if (playOnStart) StartAmbience();
        }

        private void BuildSources()
        {
            loopSrc = gameObject.AddComponent<AudioSource>();
            loopSrc.loop = true;
            loopSrc.playOnAwake = false;
            loopSrc.spatialBlend = 0f;
            loopSrc.volume = 0f;
            loopSrc.pitch = 1f + Random.Range(-loopPitchJitter, loopPitchJitter);
            loopSrc.priority = 0; // priority 0 = mas alta, no se le hace voice stealing

            eventSrc = gameObject.AddComponent<AudioSource>();
            eventSrc.loop = false;
            eventSrc.playOnAwake = false;
            eventSrc.spatialBlend = 0f;
            eventSrc.volume = 1f; // PlayOneShot multiplica esto
            eventSrc.priority = 64; // prioridad media (default es 128, pero queremos que no nos roben los crujidos facilmente)
        }

        public void StartAmbience()
        {
            if (playing) return;
            playing = true;
            Debug.Log($"[Ambient] StartAmbience. loopClip={(loopClip != null ? loopClip.name : "NULL")}, randomEvents={(randomEventClips != null ? randomEventClips.Length : 0)}, AudioListener.volume={AudioListener.volume:F2}");
            if (loopClip != null)
            {
                loopSrc.clip = loopClip;
                if (fadeInSeconds > 0f)
                {
                    loopSrc.volume = 0f;
                    loopSrc.Play();
                    StartCoroutine(FadeInLoop());
                }
                else
                {
                    loopSrc.volume = loopVolume;
                    loopSrc.Play();
                }
            }
            else Debug.LogWarning("[Ambient] loopClip es NULL, no se va a tocar la brisa.");
            if (randomEventClips != null && randomEventClips.Length > 0)
                randomEventsCo = StartCoroutine(RandomEventsRoutine());
            else Debug.LogWarning("[Ambient] randomEventClips vacio, no se van a tocar crujidos.");
        }

        public void StopAmbience(float fadeOutSeconds = 1.5f)
        {
            if (!playing) return;
            playing = false;
            if (randomEventsCo != null) StopCoroutine(randomEventsCo);
            StartCoroutine(FadeLoop(loopSrc.volume, 0f, fadeOutSeconds, stopAfter: true));
        }

        private IEnumerator FadeInLoop()
        {
            inFadeIn = true;
            float t = 0f;
            while (t < fadeInSeconds && playing)
            {
                t += Time.deltaTime;
                loopSrc.volume = Mathf.Lerp(0f, loopVolume, Mathf.Clamp01(t / fadeInSeconds));
                yield return null;
            }
            if (playing) loopSrc.volume = loopVolume;
            inFadeIn = false;
        }

        private IEnumerator FadeLoop(float from, float to, float duration, bool stopAfter = false)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                loopSrc.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            loopSrc.volume = to;
            if (stopAfter) loopSrc.Stop();
        }

        private IEnumerator RandomEventsRoutine()
        {
            // Delay inicial antes del primer evento (no saturar arranque)
            if (initialDelayBeforeFirstEvent > 0f)
                yield return new WaitForSeconds(initialDelayBeforeFirstEvent);

            while (playing)
            {
                float wait = Random.Range(randomEventPauseRange.x, randomEventPauseRange.y);
                yield return new WaitForSeconds(wait);
                if (!playing) yield break;
                var clip = randomEventClips[Random.Range(0, randomEventClips.Length)];
                if (clip == null) continue;
                eventSrc.pitch = 1f + Random.Range(-randomEventPitchJitter, randomEventPitchJitter);
                eventSrc.PlayOneShot(clip, randomEventVolume);
                Debug.Log($"[Ambient] Crujido tocado: '{clip.name}' vol={randomEventVolume}");
            }
        }
    }
}
