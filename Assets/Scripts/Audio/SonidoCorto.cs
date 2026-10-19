using UnityEngine;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Suena un clip en un punto y lo corta con un fundido a los maxSeg segundos.
    /// Sirve para clips largos que solo necesitamos al principio (casillero de 17 s, stinger de 28 s).
    /// </summary>
    public class SonidoCorto : MonoBehaviour
    {
        AudioSource a; float fin, vol0;
        const float Fundido = 0.35f;

        public static AudioSource Play(AudioClip clip, Vector3 pos, float vol, float maxSeg = 999f, float espacial = 0.8f, bool bucle = false)
        {
            if (!clip) return null;
            var go = new GameObject("Sonido_" + clip.name); go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.volume = vol; a.loop = bucle; a.spatialBlend = espacial; a.rolloffMode = AudioRolloffMode.Linear;
            a.minDistance = 1.5f; a.maxDistance = 25f; a.pitch = Random.Range(0.96f, 1.04f);
            a.Play();
            var s = go.AddComponent<SonidoCorto>(); s.a = a; s.vol0 = vol;
            s.fin = Time.unscaledTime + (bucle ? maxSeg : Mathf.Min(maxSeg, clip.length));
            return a;
        }

        static readonly System.Collections.Generic.Dictionary<string, AudioClip> cache = new System.Collections.Generic.Dictionary<string, AudioClip>();

        /// <summary>Clip de Resources/SonidosJuego (los preparados con 04_Audio/preparar_sonidos.py). Ej: "objetos/papel_tomar".</summary>
        public static AudioClip Clip(string nombre)
        {
            if (!cache.TryGetValue(nombre, out var c)) cache[nombre] = c = Resources.Load<AudioClip>("SonidosJuego/" + nombre);
            return c;
        }

        /// <summary>Suena un sonido del juego. espacial 0 = "dentro de la cabeza" de Mateo; 1 = sale de ese punto del mundo.</summary>
        public static AudioSource Juego(string nombre, Vector3 pos, float vol = 1f, float espacial = 0.8f, float maxSeg = 999f)
            => Play(Clip(nombre), pos, vol, maxSeg, espacial);

        void Update()
        {
            float queda = fin - Time.unscaledTime;
            if (queda < Fundido) a.volume = vol0 * Mathf.Clamp01(queda / Fundido);
            if (queda <= 0f) Destroy(gameObject);
        }
    }
}
