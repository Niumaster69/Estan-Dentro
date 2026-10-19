using UnityEngine;
using EstanDentro.Audio;
using EstanDentro.Player;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Los sonidos del cuerpo de Mateo: pasos según el piso y respiración que sube con el estrés.
    /// Por qué: es lo que más mete al jugador en el cuerpo; sin pasos el colegio se siente como flotar.
    ///   - Un paso cada ~0,6 m (más largos al correr, cortos y suaves agachado). 5 variaciones, nunca dos iguales seguidas.
    ///   - Baldosa por defecto; madera si el piso se llama madera/tarima/wood.
    ///   - Correr hace ruido (Ruido.Emitir): el monstruo lo puede oír. Agachado no.
    ///   - Respiración agitada: aparece con estrés > 35 % y se vuelve más fuerte hasta 100 %.
    ///   - Pitido en el oído al despertar (viene del choque del prólogo) que se apaga en unos segundos.
    ///   - Zumbido del tubo fluorescente más cercano que esté prendido: con el apagón se calla (el silencio asusta más).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CuerpoMateo : MonoBehaviour
    {
        public float zancada = 0.62f;
        public float volumenPasos = 0.5f;

        CharacterController cc; PlayerController pc;
        float recorrido; int ultimo = -1;
        AudioSource respiracion, zumbido;
        Light tuboCerca; float buscarTubo;

        void Awake()
        {
            cc = GetComponent<CharacterController>(); pc = GetComponent<PlayerController>();
            var clip = SonidoCorto.Clip("cuerpo/respiracion_miedo");
            if (clip)
            {
                respiracion = gameObject.AddComponent<AudioSource>();
                respiracion.clip = clip; respiracion.loop = true; respiracion.volume = 0f; respiracion.spatialBlend = 0f; respiracion.playOnAwake = false;
            }
        }

        void Start()
        {
            SonidoCorto.Juego("cuerpo/tinnitus", transform.position, 0.3f, 0f, 7f);
            var hum = SonidoCorto.Clip("ambiente/fluorescente");
            if (hum)
            {
                zumbido = new GameObject("ZumbidoTubo").AddComponent<AudioSource>();
                zumbido.clip = hum; zumbido.loop = true; zumbido.spatialBlend = 1f; zumbido.volume = 0f;
                zumbido.rolloffMode = AudioRolloffMode.Linear; zumbido.minDistance = 0.5f; zumbido.maxDistance = 7f;
                zumbido.Play();
            }
        }

        void Zumbido()
        {
            if (!zumbido) return;
            if (Time.time > buscarTubo)
            {
                buscarTubo = Time.time + 0.5f;
                tuboCerca = null; float dMin = 7f;
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.enabled && l.type == LightType.Rectangle && l.intensity > 0f)
                    {
                        float d = Vector3.Distance(l.transform.position, transform.position);
                        if (d < dMin) { dMin = d; tuboCerca = l; }
                    }
            }
            bool vivo = tuboCerca && tuboCerca.enabled && tuboCerca.gameObject.activeInHierarchy;
            if (vivo) zumbido.transform.position = tuboCerca.transform.position;
            zumbido.volume = Mathf.MoveTowards(zumbido.volume, vivo ? 0.35f : 0f, Time.deltaTime * (vivo ? 0.5f : 3f));
        }

        void Update()
        {
            Zumbido();
            Vector3 v = cc.velocity; v.y = 0f;
            bool agachado = pc && pc.Agachado, corriendo = pc && pc.Corriendo;
            if (cc.isGrounded && v.magnitude > 0.3f)
            {
                recorrido += v.magnitude * Time.deltaTime;
                float paso = zancada * (corriendo ? 1.35f : agachado ? 0.8f : 1f);
                if (recorrido >= paso) { recorrido = 0f; Paso(agachado, corriendo); }
            }
            else recorrido = Mathf.Min(recorrido, zancada * 0.5f);

            if (respiracion)
            {
                float estres = Stress.StressSystem.Instance ? Stress.StressSystem.Instance.Normalized : 0f;
                float meta = Mathf.InverseLerp(0.35f, 1f, estres) * 0.55f + (corriendo ? 0.08f : 0f);
                respiracion.volume = Mathf.MoveTowards(respiracion.volume, meta, Time.deltaTime * 0.25f);
                if (respiracion.volume > 0.01f && !respiracion.isPlaying) respiracion.Play();
                else if (respiracion.volume <= 0.01f && respiracion.isPlaying) respiracion.Pause();
            }
        }

        void Paso(bool agachado, bool corriendo)
        {
            string piso = "baldosa";
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                var r = hit.collider.GetComponent<Renderer>();
                string n = ((r && r.sharedMaterial) ? r.sharedMaterial.name : "") + hit.collider.name;
                n = n.ToLowerInvariant();
                if (n.Contains("madera") || n.Contains("tarima") || n.Contains("wood") || n.Contains("parquet")) piso = "madera";
            }
            int i; do i = Random.Range(0, 5); while (i == ultimo); ultimo = i;
            float vol = volumenPasos * (agachado ? 0.35f : corriendo ? 1.3f : 1f) * Random.Range(0.85f, 1f);
            SonidoCorto.Juego($"pasos/{piso}_{i}", transform.position + Vector3.up * 0.05f, vol, 0.3f);
            if (corriendo) Ruido.Emitir(transform.position, 8f, "pasos");
        }
    }
}
