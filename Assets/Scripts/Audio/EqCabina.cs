using UnityEngine;

namespace EstanDentro.Audio
{
    /// <summary>
    /// Cómo suena algo dentro de un carro cerrado (se pone en la fuente de audio):
    /// - cabina diminuta y forrada: casi sin eco (RT60 ≈ 0,1 s), pero con reflejos muy cortos del vidrio y el tablero (1–5 ms);
    /// - la cabina refuerza los graves ("cabin gain") y suena un poco a caja (150–300 Hz);
    /// - tapiz, asientos y techo se comen los agudos (desde ≈ 5–6 kHz);
    /// - el motor y la lluvia tapan lo más grave: se corta debajo de ≈ 90 Hz para que la voz no se embarre.
    /// Filtros biquad (recetario de Robert Bristow-Johnson) aplicados en el hilo de audio.
    /// </summary>
    public class EqCabina : MonoBehaviour
    {
        public enum Perfil { VozEnCabina, Parlante, Pensamiento, VozPa }
        public Perfil perfil = Perfil.VozEnCabina;

        Biquad[,] f;               // [filtro, canal]
        float[][] linea;           // reflejos tempranos (línea de retardo por canal)
        int pos, rate;
        static readonly float[] tapsMs = { 1.6f, 3.1f, 4.7f };
        static readonly float[] tapsG = { 0.22f, 0.14f, 0.08f };

        /// <summary>Se llama justo después de AddComponent (Awake corre antes de poder elegir el perfil).</summary>
        public void Iniciar(Perfil p)
        {
            perfil = p;
            rate = AudioSettings.outputSampleRate;
            const int CH = 2;
            var lista = new System.Collections.Generic.List<System.Func<Biquad>>();
            switch (perfil)
            {
                case Perfil.VozEnCabina:      // Mateo en voz alta: su voz ya es pareja; la cabina solo aporta los reflejos
                    lista.Add(() => Biquad.PasaAltos(rate, 90f, 0.7f));
                    lista.Add(() => Biquad.EstanteAlto(rate, 7000f, -2f));
                    break;
                case Perfil.VozPa:
                    // medido (04_Audio/espectro_voces.js) contra la curva promedio del habla (LTASS): la voz de Pa trae ~3 dB de más
                    // en 300-500 Hz ("caja") y ~3 dB de menos en 3-6 kHz (claridad). Antes la ecualización de cabina le sumaba graves
                    // y le quitaba agudos: sonaba tapada. Corrección moderada + los reflejos de cabina.
                    lista.Add(() => Biquad.PasaAltos(rate, 110f, 0.7f));
                    lista.Add(() => Biquad.Pico(rate, 400f, 0.9f, -3.5f));
                    lista.Add(() => Biquad.Pico(rate, 3500f, 1.0f, 3.5f));
                    lista.Add(() => Biquad.EstanteAlto(rate, 6500f, 1.5f));
                    break;
                case Perfil.Parlante:      // parlante de puerta: cono pequeño, sin graves profundos ni brillo, medios al frente
                    lista.Add(() => Biquad.PasaAltos(rate, 110f, 0.8f));
                    lista.Add(() => Biquad.Pico(rate, 1400f, 0.9f, 2.5f));
                    lista.Add(() => Biquad.EstanteAlto(rate, 6000f, -7f));
                    lista.Add(() => Biquad.EstanteBajo(rate, 180f, 2f));
                    break;
                default:                   // en la cabeza: cercano, cálido, sin cabina
                    lista.Add(() => Biquad.PasaAltos(rate, 80f, 0.7f));
                    lista.Add(() => Biquad.EstanteBajo(rate, 220f, 2f));
                    lista.Add(() => Biquad.EstanteAlto(rate, 7000f, -3f));
                    break;
            }
            f = new Biquad[lista.Count, CH];
            for (int i = 0; i < lista.Count; i++) for (int c = 0; c < CH; c++) f[i, c] = lista[i]();
            int largo = Mathf.CeilToInt(rate * 0.006f) + 1;
            linea = new[] { new float[largo], new float[largo] };
        }

        void OnAudioFilterRead(float[] data, int canales)
        {
            if (f == null) return;
            int n = f.GetLength(0), largo = linea[0].Length;
            bool reflejos = perfil != Perfil.Pensamiento;
            for (int i = 0; i < data.Length; i += canales)
            {
                for (int c = 0; c < canales; c++)
                {
                    int cc = Mathf.Min(c, 1);
                    float x = data[i + c];
                    for (int k = 0; k < n; k++) x = f[k, cc].Paso(x);
                    if (reflejos)
                    {
                        var l = linea[cc]; l[pos] = x;
                        float y = x;
                        for (int t = 0; t < tapsMs.Length; t++)
                        {
                            int d = (int)(tapsMs[t] * 0.001f * rate);
                            y += l[(pos - d + largo) % largo] * tapsG[t];
                        }
                        x = y * 0.85f;
                    }
                    data[i + c] = x;
                }
                pos = (pos + 1) % largo;
            }
        }

        struct Biquad
        {
            float b0, b1, b2, a1, a2, z1, z2;
            public float Paso(float x) { float y = b0 * x + z1; z1 = b1 * x - a1 * y + z2; z2 = b2 * x - a2 * y; return y; }
            static Biquad Norm(float b0, float b1, float b2, float a0, float a1, float a2)
                => new Biquad { b0 = b0 / a0, b1 = b1 / a0, b2 = b2 / a0, a1 = a1 / a0, a2 = a2 / a0 };
            public static Biquad PasaAltos(int fs, float f0, float q)
            {
                float w = 2 * Mathf.PI * f0 / fs, cs = Mathf.Cos(w), al = Mathf.Sin(w) / (2 * q);
                return Norm((1 + cs) / 2, -(1 + cs), (1 + cs) / 2, 1 + al, -2 * cs, 1 - al);
            }
            public static Biquad Pico(int fs, float f0, float q, float db)
            {
                float A = Mathf.Pow(10, db / 40), w = 2 * Mathf.PI * f0 / fs, cs = Mathf.Cos(w), al = Mathf.Sin(w) / (2 * q);
                return Norm(1 + al * A, -2 * cs, 1 - al * A, 1 + al / A, -2 * cs, 1 - al / A);
            }
            public static Biquad EstanteBajo(int fs, float f0, float db)
            {
                float A = Mathf.Pow(10, db / 40), w = 2 * Mathf.PI * f0 / fs, cs = Mathf.Cos(w), al = Mathf.Sin(w) / 2 * Mathf.Sqrt(2), r = 2 * Mathf.Sqrt(A) * al;
                return Norm(A * ((A + 1) - (A - 1) * cs + r), 2 * A * ((A - 1) - (A + 1) * cs), A * ((A + 1) - (A - 1) * cs - r),
                            (A + 1) + (A - 1) * cs + r, -2 * ((A - 1) + (A + 1) * cs), (A + 1) + (A - 1) * cs - r);
            }
            public static Biquad EstanteAlto(int fs, float f0, float db)
            {
                float A = Mathf.Pow(10, db / 40), w = 2 * Mathf.PI * f0 / fs, cs = Mathf.Cos(w), al = Mathf.Sin(w) / 2 * Mathf.Sqrt(2), r = 2 * Mathf.Sqrt(A) * al;
                return Norm(A * ((A + 1) + (A - 1) * cs + r), -2 * A * ((A - 1) + (A + 1) * cs), A * ((A + 1) + (A - 1) * cs - r),
                            (A + 1) - (A - 1) * cs + r, 2 * ((A - 1) - (A + 1) * cs), (A + 1) - (A - 1) * cs - r);
            }
        }
    }

    /// <summary>
    /// "Agacharse" cuando alguien habla (ducking): la música y la radio bajan ≈ 8 dB mientras suena una voz,
    /// entran rápido (0,15 s) y vuelven despacio (0,8 s). Se pone en la fuente de música o de radio.
    /// </summary>
    public class Agachar : MonoBehaviour
    {
        public float dbAbajo = -8f;
        float g = 1f, dt;
        void Awake() { dt = 1f / AudioSettings.outputSampleRate; }
        void OnAudioFilterRead(float[] data, int canales)
        {
            float meta = Voz.Hablando ? Mathf.Pow(10f, dbAbajo / 20f) : 1f;
            float k = meta < g ? dt / 0.15f : dt / 0.8f;
            for (int i = 0; i < data.Length; i += canales)
            {
                g += (meta - g) * Mathf.Min(1f, k * 5f);
                for (int c = 0; c < canales; c++) data[i + c] *= g;
            }
        }
    }
}
