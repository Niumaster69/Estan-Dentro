using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace EstanDentro.Audio
{
    [System.Serializable]
    public class SubliminalLayer
    {
        public string nombre = "Capa";
        public AudioClip clip;
        [Range(0f, 1f)] public float volumenBase = 1f;
        [Range(0f, 0.5f)] public float variacionVolumen = 0.1f;
        [Range(0f, 0.1f)] public float variacionPitch = 0.02f;
        public bool loop = true;
        [Tooltip("Segundos de silencio aleatorio antes de reiniciar si no es loop continuo")]
        public Vector2 rangoPausaSiNoLoop = new Vector2(4f, 12f);

        [HideInInspector] public AudioSource source;
    }

    public class SubliminalLoopController : MonoBehaviour
    {
        [Header("Mixer")]
        [Tooltip("Grupo del AudioMixer al que se enrutan todas las capas subliminales (ya atenuado a -20 dB)")]
        [SerializeField] private AudioMixerGroup grupoSubliminal;

        [Header("Capas del loop subliminal")]
        [SerializeField] private SubliminalLayer[] capas;

        [Header("General")]
        [Tooltip("Se inicia solo al habilitarse")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("Fade in en segundos")]
        [SerializeField] private float fadeIn = 3f;

        private void OnEnable()
        {
            if (autoStart) Iniciar();
        }

        private void OnDisable()
        {
            Detener();
        }

        public void Iniciar()
        {
            foreach (var c in capas)
            {
                if (c == null || c.clip == null) continue;
                var go = new GameObject($"Subliminal_{c.nombre}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.clip = c.clip;
                src.outputAudioMixerGroup = grupoSubliminal;
                src.loop = c.loop;
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.volume = 0f;
                src.pitch = 1f + Random.Range(-c.variacionPitch, c.variacionPitch);
                c.source = src;
                src.Play();
                StartCoroutine(FadeVolumen(src, c.volumenBase, fadeIn));
                if (!c.loop) StartCoroutine(RelanzarConPausas(c));
                else StartCoroutine(ModularVolumen(c));
            }
        }

        public void Detener()
        {
            StopAllCoroutines();
            foreach (var c in capas)
            {
                if (c != null && c.source != null)
                {
                    Destroy(c.source.gameObject);
                    c.source = null;
                }
            }
        }

        private IEnumerator FadeVolumen(AudioSource src, float objetivo, float duracion)
        {
            float t = 0f;
            float inicio = src.volume;
            while (t < duracion && src != null)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(inicio, objetivo, t / duracion);
                yield return null;
            }
            if (src != null) src.volume = objetivo;
        }

        private IEnumerator ModularVolumen(SubliminalLayer c)
        {
            while (c.source != null)
            {
                float objetivo = c.volumenBase + Random.Range(-c.variacionVolumen, c.variacionVolumen);
                objetivo = Mathf.Clamp01(objetivo);
                float dur = Random.Range(2f, 5f);
                float t = 0f;
                float inicio = c.source.volume;
                while (t < dur && c.source != null)
                {
                    t += Time.deltaTime;
                    c.source.volume = Mathf.Lerp(inicio, objetivo, t / dur);
                    yield return null;
                }
            }
        }

        private IEnumerator RelanzarConPausas(SubliminalLayer c)
        {
            while (c.source != null)
            {
                yield return new WaitWhile(() => c.source != null && c.source.isPlaying);
                float pausa = Random.Range(c.rangoPausaSiNoLoop.x, c.rangoPausaSiNoLoop.y);
                yield return new WaitForSeconds(pausa);
                if (c.source == null) yield break;
                c.source.pitch = 1f + Random.Range(-c.variacionPitch, c.variacionPitch);
                c.source.Play();
            }
        }
    }
}
