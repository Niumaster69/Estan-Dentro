using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Recorta un pedazo de un audio del proyecto y lo guarda como WAV (para mandarlo a ElevenLabs).
    /// Uso en batch: -executeMethod EstanDentro.EditorColegio.RecortarAudio.Batch -audio Assets/... -salida C:/.../x.wav -desde 97.4 -hasta 110.6
    /// </summary>
    public static class RecortarAudio
    {
        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

[Serializable] class Corte { public string archivo; public float desde, hasta; }
        [Serializable] class Cortes { public Corte[] items; }

        /// <summary>
        /// Varios cortes de una toma larga (04_Audio/cortes_toma.js) a WAV, con fundido de 10 ms y el volumen igualado
        /// (RMS de la voz a -18 dB, pico máximo -1 dB), como las demás voces.
        /// -audio Assets/... -cortes C:/.../cortes.json -carpeta Assets/Resources/Voces/Colegio
        /// </summary>
        public static void Varios()
        {
            try
            {
                string ruta = Arg("-audio"), carpeta = Arg("-carpeta");
                var lista = JsonUtility.FromJson<Cortes>("{\"items\":" + File.ReadAllText(Arg("-cortes")) + "}");
                var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
                var s = imp.defaultSampleSettings; s.loadType = AudioClipLoadType.DecompressOnLoad; imp.defaultSampleSettings = s; imp.SaveAndReimport();
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta); clip.LoadAudioData();
                int f = clip.frequency, ch = clip.channels;
                var todo = new float[clip.samples * ch]; clip.GetData(todo, 0);
                Directory.CreateDirectory(carpeta);
                foreach (var c in lista.items)
                {
                    int i0 = Mathf.Clamp((int)(c.desde * f), 0, clip.samples), i1 = Mathf.Clamp((int)(c.hasta * f), 0, clip.samples), n = i1 - i0;
                    var m = new float[n];
                    for (int i = 0; i < n; i++) { float v = 0f; for (int k = 0; k < ch; k++) v += todo[(i0 + i) * ch + k]; m[i] = v / ch; }
                    // volumen: RMS de lo que suena (ventanas de 20 ms sobre -45 dB) a -18 dB
                    int vn = Mathf.Max(1, f / 50); double suma = 0; int cuenta = 0;
                    for (int i = 0; i + vn <= n; i += vn) { double e = 0; for (int k = 0; k < vn; k++) e += m[i + k] * m[i + k]; e /= vn; if (e > 3.2e-5) { suma += e; cuenta++; } }
                    float rms = cuenta > 0 ? (float)Math.Sqrt(suma / cuenta) : 0.05f, pico = 1e-4f;
                    float g = Mathf.Pow(10f, -18f / 20f) / Mathf.Max(rms, 1e-4f);
                    foreach (var v in m) pico = Mathf.Max(pico, Mathf.Abs(v * g));
                    if (pico > 0.89f) g *= 0.89f / pico;
                    int fade = f / 100;
                    for (int i = 0; i < n; i++) { float a = Mathf.Min(1f, Mathf.Min(i, n - 1 - i) / (float)fade); m[i] *= g * a; }
                    string salida = Path.Combine(carpeta, c.archivo + ".wav");
                    using (var w = new BinaryWriter(File.Create(salida)))
                    {
                        int bytes = n * 2;
                        w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
                        w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(f); w.Write(f * 2); w.Write((short)2); w.Write((short)16);
                        w.Write("data".ToCharArray()); w.Write(bytes);
                        foreach (var v in m) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
                    }
                    Debug.Log($"[Recorte] {c.archivo}.wav {c.desde:F2}-{c.hasta:F2} s ganancia {20f * Mathf.Log10(g):F1} dB");
                }
                AssetDatabase.Refresh();
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError("[Recorte] " + e); EditorApplication.Exit(1); }
        }

        public static void Batch()
        {
            try
            {
                string ruta = Arg("-audio"), salida = Arg("-salida");
                float desde = float.Parse(Arg("-desde"), System.Globalization.CultureInfo.InvariantCulture);
                float hasta = float.Parse(Arg("-hasta"), System.Globalization.CultureInfo.InvariantCulture);
                var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
                var s = imp.defaultSampleSettings;
                if (s.loadType != AudioClipLoadType.DecompressOnLoad) { s.loadType = AudioClipLoadType.DecompressOnLoad; imp.defaultSampleSettings = s; imp.SaveAndReimport(); }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                clip.LoadAudioData();
                int f = clip.frequency, ch = clip.channels;
                int i0 = Mathf.Clamp((int)(desde * f), 0, clip.samples), n = Mathf.Clamp((int)(hasta * f), 0, clip.samples) - i0;
                var datos = new float[n * ch];
                clip.GetData(datos, i0);
                using (var w = new BinaryWriter(File.Create(salida)))
                {
                    int bytes = datos.Length * 2;
                    w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
                    w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(f); w.Write(f * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
                    w.Write("data".ToCharArray()); w.Write(bytes);
                    foreach (var v in datos) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
                }
                s.loadType = AudioClipLoadType.CompressedInMemory; imp.defaultSampleSettings = s; imp.SaveAndReimport();   // en el juego no ocupa 48 MB
                Debug.Log($"[Recorte] {salida} {desde}-{hasta} s ({n} muestras, {ch} canales, {f} Hz)");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError("[Recorte] " + e); EditorApplication.Exit(1); }
        }
    }
}
