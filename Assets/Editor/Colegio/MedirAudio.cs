using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Mide el nivel de cada audio del prólogo (RMS y pico en dBFS) para mezclarlos con niveles de juego:
    /// diálogo ≈ -12 dB RMS; música, ambiente y efectos ≈ -16 a -20 dB; picos por debajo de -3 dB.
    /// Escribe 04_Audio/niveles_prologo.txt. Batch: -executeMethod EstanDentro.EditorColegio.MedirAudio.Batch
    /// </summary>
    public static class MedirAudio
    {
        /// <summary>
        /// Iguala las voces del prólogo: nivel de habla (RMS solo donde hay voz, sin silencios) a -18 dB, el grito a -15,
        /// sin pasar picos de -1 dB. Escribe .wav en la misma carpeta y borra la copia .mp3 (los originales están en 04_Audio/voces).
        /// </summary>
        public static void NormalizarVoces()
        {
            const string CARPETA = "Assets/Resources/Voces";
            var log = new System.Text.StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { CARPETA }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ruta.EndsWith(".mp3")) continue;
                var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
                var s = imp.defaultSampleSettings; s.loadType = AudioClipLoadType.DecompressOnLoad; imp.defaultSampleSettings = s; imp.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta); clip.LoadAudioData();
                int ch = clip.channels, f = clip.frequency;
                var d = new float[clip.samples * ch]; clip.GetData(d, 0);
                // RMS con compuerta: bloques de 50 ms por encima de -45 dB
                int bloque = f / 20 * ch; double suma = 0; long n = 0; float pico = 0;
                for (int i = 0; i < d.Length; i += bloque)
                {
                    double sb = 0; int m = Mathf.Min(bloque, d.Length - i);
                    for (int j = 0; j < m; j++) { sb += d[i + j] * d[i + j]; pico = Mathf.Max(pico, Mathf.Abs(d[i + j])); }
                    if (Mathf.Sqrt((float)(sb / m)) > 0.0056f) { suma += sb; n += m; }
                }
                float rms = Mathf.Sqrt((float)(suma / Mathf.Max(1, n)));
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                float meta = nombre == "pa" ? -15f : -18f;
                float g = Mathf.Pow(10f, (meta - 20f * Mathf.Log10(rms + 1e-9f)) / 20f);
                g = Mathf.Min(g, 0.891f / Mathf.Max(pico, 1e-6f));        // pico máximo -1 dB
                string wav = Path.Combine(Path.GetDirectoryName(ruta), nombre + ".wav");
                using (var w = new BinaryWriter(File.Create(wav)))
                {
                    int bytes = d.Length * 2;
                    w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
                    w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(f); w.Write(f * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
                    w.Write("data".ToCharArray()); w.Write(bytes);
                    foreach (var v in d) w.Write((short)(Mathf.Clamp(v * g, -1f, 1f) * 32767f));
                }
                AssetDatabase.DeleteAsset(ruta);
                log.Append($"{nombre}\tantes {20f * Mathf.Log10(rms):F1} dB\tganancia {20f * Mathf.Log10(g):+0.0;-0.0} dB\n");
            }
            AssetDatabase.Refresh();
            File.WriteAllText(Path.GetFullPath("../../04_Audio/voces_igualadas.txt"), log.ToString());
            EditorApplication.Exit(0);
        }

        public static void Batch()
        {
            var carpetas = new[] { "Assets/Resources/Prologo", "Assets/Resources/Voces/Prologo", "Assets/Resources/SonidosJuego/prologo", "Assets/Resources/SonidosJuego/ambiente" };
            var salida = new System.Text.StringBuilder("archivo\tRMS_dB\tpico_dB\tseg\n");
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", carpetas))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
                var s = imp.defaultSampleSettings; var antes = s.loadType;
                if (antes != AudioClipLoadType.DecompressOnLoad) { s.loadType = AudioClipLoadType.DecompressOnLoad; imp.defaultSampleSettings = s; imp.SaveAndReimport(); }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                clip.LoadAudioData();
                var d = new float[clip.samples * clip.channels];
                clip.GetData(d, 0);
                double sum = 0; float pico = 0;
                foreach (var v in d) { sum += v * v; pico = Mathf.Max(pico, Mathf.Abs(v)); }
                float rms = Mathf.Sqrt((float)(sum / Mathf.Max(1, d.Length)));
                salida.Append($"{ruta.Replace("Assets/Resources/", "")}\t{20f * Mathf.Log10(rms + 1e-9f):F1}\t{20f * Mathf.Log10(pico + 1e-9f):F1}\t{clip.length:F1}\n");
                if (antes != AudioClipLoadType.DecompressOnLoad) { s.loadType = antes; imp.defaultSampleSettings = s; imp.SaveAndReimport(); }
            }
            File.WriteAllText(Path.GetFullPath("../../04_Audio/niveles_prologo.txt"), salida.ToString());
            EditorApplication.Exit(0);
        }
    }
}
