using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Voces grabadas (ElevenLabs) de Pa y Mateo. Cada frase es un archivo en Resources/Voces/&lt;carpeta&gt;/&lt;clave&gt;,
    /// donde la clave sale del texto (Voz.Clave). Si el archivo no existe, el juego sigue solo con el subtítulo.
    /// El "lugar" ecualiza la voz: dentro del carro cerrado, en la cabeza de Mateo (pensamiento) o lejos (hospital).
    /// </summary>
    public static class Voz
    {
        public enum Lugar { Carro, Pensamiento, Lejos, Colegio, Recuerdo, CarroPa, CarroMateo }

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        // mientras suena una voz, la música y la radio se agachan (componente Agachar). Se lee desde el hilo de audio.
        static double hastaDsp;
        public static bool Hablando => AudioSettings.dspTime < hastaDsp;

        /// <summary>Nombre de archivo a partir del texto: minúsculas, sin tildes ni signos, máximo 48 letras.</summary>
        public static string Clave(string texto)
        {
            var sb = new StringBuilder();
            bool enEtiqueta = false;
            foreach (char c0 in texto.Normalize(NormalizationForm.FormD).ToLowerInvariant())
            {
                if (c0 == '<') { enEtiqueta = true; continue; }
                if (c0 == '>') { enEtiqueta = false; continue; }
                if (enEtiqueta || System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c0) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (c0 >= 'a' && c0 <= 'z' || c0 >= '0' && c0 <= '9') sb.Append(c0);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
                if (sb.Length >= 48) break;
            }
            return sb.ToString().Trim('_');
        }

        public static AudioClip Clip(string carpeta, string texto)
        {
            string ruta = $"Voces/{carpeta}/{Clave(texto)}";
            if (!cache.TryGetValue(ruta, out var c)) cache[ruta] = c = Resources.Load<AudioClip>(ruta);
            return c;
        }

        /// <summary>Suena la frase en ese punto con la ecualización del lugar. Devuelve la fuente (o null si no hay archivo).</summary>
        public static AudioSource Sonar(AudioClip clip, Vector3 pos, Lugar lugar, float vol = 1f, bool agachaMusica = true)
        {
            if (!clip) return null;
            var go = new GameObject("Voz_" + clip.name); go.transform.position = pos;
            var a = go.AddComponent<AudioSource>();
            a.clip = clip; a.volume = vol; a.pitch = 1f; a.playOnAwake = false;
            // la voz de Mateo es la del jugador: sale de su propia cabeza (sin izquierda/derecha); solo los demás suenan en el espacio
            a.spatialBlend = lugar == Lugar.Pensamiento || lugar == Lugar.Lejos || lugar == Lugar.CarroMateo ? 0f : 0.7f;
            a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = 1f; a.maxDistance = 12f;
            switch (lugar)
            {
                case Lugar.CarroPa:
                    go.AddComponent<EqCabina>().Iniciar(EqCabina.Perfil.VozPa);           // corrección medida de su voz + cabina
                    break;
                case Lugar.Carro:
                case Lugar.CarroMateo:
                    // carro cerrado: graves de cabina, algo de caja, agudos apagados y reflejos de 1–5 ms (ver EqCabina)
                    go.AddComponent<EqCabina>().Iniciar(EqCabina.Perfil.VozEnCabina);
                    break;
                case Lugar.Pensamiento:
                    // en la cabeza: cerca, seco y un poco más cálido; un poco más bajo que la voz hablada
                    go.AddComponent<EqCabina>().Iniciar(EqCabina.Perfil.Pensamiento); a.volume = vol * 0.8f;
                    break;
                case Lugar.Lejos:
                    // la voz de Pa desde el hospital: como a través de una pared, con eco largo
                    go.AddComponent<AudioHighPassFilter>().cutoffFrequency = 350f;
                    go.AddComponent<AudioLowPassFilter>().cutoffFrequency = 2200f;
                    go.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Hallway;
                    break;
                case Lugar.Recuerdo:
                    // un recuerdo: la voz llega desde adentro de la cabeza, sin cuerpo (pocos graves), un poco apagada,
                    // con una cola larga y suave que la deja flotando, y un leve vaivén (como algo que no está del todo ahí)
                    a.spatialBlend = 0f;
                    var hpR = go.AddComponent<AudioHighPassFilter>(); hpR.cutoffFrequency = 260f; hpR.highpassResonanceQ = 0.8f;
                    var lpR = go.AddComponent<AudioLowPassFilter>(); lpR.cutoffFrequency = 5200f; lpR.lowpassResonanceQ = 0.9f;
                    var ch = go.AddComponent<AudioChorusFilter>(); ch.dryMix = 0.85f; ch.wetMix1 = 0.18f; ch.wetMix2 = 0.12f; ch.wetMix3 = 0f;
                    ch.delay = 22f; ch.rate = 0.35f; ch.depth = 0.06f;
                    var rv = go.AddComponent<AudioReverbFilter>(); rv.reverbPreset = AudioReverbPreset.User;
                    rv.dryLevel = -250f; rv.room = -600f; rv.roomHF = -900f; rv.roomLF = -1200f;
                    rv.decayTime = 2.4f; rv.decayHFRatio = 0.5f;
                    rv.reflectionsLevel = -900f; rv.reflectionsDelay = 0.03f;
                    rv.reverbLevel = -250f; rv.reverbDelay = 0.045f;
                    rv.diffusion = 100f; rv.density = 100f; rv.hfReference = 4000f; rv.lfReference = 250f;
                    a.volume = vol * 0.9f;
                    break;
                default:
                    go.AddComponent<AudioHighPassFilter>().cutoffFrequency = 90f;
                    go.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Room;
                    break;
            }
            if (agachaMusica) hastaDsp = System.Math.Max(hastaDsp, AudioSettings.dspTime + clip.length + 0.2);
            a.Play();
            Object.Destroy(go, clip.length + (lugar == Lugar.Recuerdo ? 3.5f : 1.5f));   // deja terminar la cola del eco
            return a;
        }
    }
}
