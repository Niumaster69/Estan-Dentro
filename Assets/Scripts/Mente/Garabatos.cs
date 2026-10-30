using UnityEngine;

namespace EstanDentro.Mente
{
    /// <summary>
    /// Garabatos de lápiz (los del cuaderno de Mateo: espirales, ojos, tachones, rayas, "NO") hechos a mano por código.
    /// Tres versiones del mismo dibujo con el trazo un poco movido, una por canal (R, G, B): al alternarlas a 12 cuadros
    /// por segundo "hierven" como la animación dibujada a mano (los dibujos de Jinx en Arcane).
    /// Se concentran cerca de los bordes; el shader decide cuánto entran.
    /// </summary>
    public static class Garabatos
    {
        public static Texture2D Crear(int w, int h, int semilla)
        {
            var canales = new float[3][];
            for (int c = 0; c < 3; c++)
            {
                var px = new float[w * h];
                var rnd = new System.Random(semilla);                     // mismo dibujo en las 3...
                var temblor = new System.Random(semilla * 31 + c);       // ...con distinto pulso
                for (int i = 0; i < 70; i++) Figura(px, w, h, rnd, temblor);
                canales[c] = px;
            }
            var col = new Color32[w * h];
            for (int i = 0; i < col.Length; i++)
                col[i] = new Color32((byte)(Mathf.Clamp01(canales[0][i]) * 255), (byte)(Mathf.Clamp01(canales[1][i]) * 255), (byte)(Mathf.Clamp01(canales[2][i]) * 255), 255);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels32(col); t.Apply();
            return t;
        }

        static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        static Vector2 CercaDelBorde(System.Random r, int w, int h)
        {
            // en una franja del 22 % pegada a algún borde
            float bx = R(r, 0f, 1f), by = R(r, 0f, 1f);
            switch (r.Next(4))
            {
                case 0: bx = R(r, 0f, 0.22f); break;
                case 1: bx = R(r, 0.78f, 1f); break;
                case 2: by = R(r, 0f, 0.22f); break;
                default: by = R(r, 0.78f, 1f); break;
            }
            return new Vector2(bx * w, by * h);
        }

        static void Figura(float[] px, int w, int h, System.Random r, System.Random tb)
        {
            Vector2 c = CercaDelBorde(r, w, h);
            float tam = R(r, 18f, 70f), g = R(r, 1.2f, 2.6f);
            int tipo = r.Next(7);
            Vector2 J() => new Vector2(R(tb, -1.6f, 1.6f), R(tb, -1.6f, 1.6f));
            switch (tipo)
            {
                case 0: // espiral
                    { Vector2 ant = c; float vueltas = R(r, 2f, 4.5f);
                      for (float a = 0; a < vueltas * 6.283f; a += 0.12f) { float rr = tam * a / (vueltas * 6.283f); var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr + J(); Linea(px, w, h, ant, p, g); ant = p; } }
                    break;
                case 1: // garabato que da vueltas (rayado nervioso)
                    { Vector2 ant = c; float ang = R(r, 0, 6.28f);
                      for (int i = 0; i < 26; i++) { ang += R(r, 0.9f, 2.2f); var p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * tam * R(r, 0.5f, 1f) + J(); Linea(px, w, h, ant, p, g); ant = p; } }
                    break;
                case 2: // tachón (X fuerte, repasada)
                    for (int k = 0; k < 3; k++) { Linea(px, w, h, c + new Vector2(-tam, -tam) + J() * 3, c + new Vector2(tam, tam) + J() * 3, g); Linea(px, w, h, c + new Vector2(-tam, tam) + J() * 3, c + new Vector2(tam, -tam) + J() * 3, g); }
                    break;
                case 3: // rayas que entran desde el borde (como grietas)
                    { Vector2 al = new Vector2(w / 2f, h / 2f) - c; al.Normalize(); Vector2 ant = c;
                      for (int i = 0; i < 9; i++) { var p = ant + (al + new Vector2(R(r, -0.5f, 0.5f), R(r, -0.5f, 0.5f))).normalized * tam * 0.35f + J(); Linea(px, w, h, ant, p, g); ant = p; } }
                    break;
                case 4: // un ojo
                    { Vector2 ant = c + new Vector2(-tam, 0);
                      for (float a = 0; a <= 6.4f; a += 0.15f) { var p = c + new Vector2(-tam * Mathf.Cos(a), Mathf.Sin(a) * tam * 0.45f * Mathf.Sign(Mathf.Sin(a) + 0.0001f) * Mathf.Abs(Mathf.Sin(a))) + J(); Linea(px, w, h, ant, p, g); ant = p; }
                      ant = c + new Vector2(tam * 0.3f, 0);
                      for (float a = 0; a <= 6.4f; a += 0.3f) { var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * tam * 0.3f + J(); Linea(px, w, h, ant, p, g * 1.4f); ant = p; } }
                    break;
                case 5: // sombreado de rayitas
                    for (int i = 0; i < 12; i++) { var o = c + new Vector2(i * 4.5f - 27f, 0); Linea(px, w, h, o + new Vector2(-tam * 0.4f, tam * 0.4f) + J(), o + new Vector2(tam * 0.4f, -tam * 0.4f) + J(), g * 0.8f); }
                    break;
                default: // "NO" a mano
                    { float s = tam * 0.5f; Vector2 o = c;
                      Linea(px, w, h, o + J(), o + new Vector2(0, s * 2) + J(), g); Linea(px, w, h, o + new Vector2(0, s * 2) + J(), o + new Vector2(s, 0) + J(), g); Linea(px, w, h, o + new Vector2(s, 0) + J(), o + new Vector2(s, s * 2) + J(), g);
                      Vector2 cc = o + new Vector2(s * 2.2f, s); Vector2 ant = cc + new Vector2(s * 0.8f, 0);
                      for (float a = 0; a <= 6.5f; a += 0.3f) { var p = cc + new Vector2(Mathf.Cos(a) * s * 0.8f, Mathf.Sin(a) * s) + J(); Linea(px, w, h, ant, p, g); ant = p; } }
                    break;
            }
        }

        // trazo de lápiz: discos suaves a lo largo de la línea, con la presión que varía
        static void Linea(float[] px, int w, int h, Vector2 a, Vector2 b, float grosor)
        {
            float largo = Vector2.Distance(a, b); int pasos = Mathf.Max(1, (int)(largo / 0.8f));
            for (int i = 0; i <= pasos; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)pasos);
                float presion = 0.55f + 0.45f * Mathf.PerlinNoise(p.x * 0.05f, p.y * 0.05f);
                int r = Mathf.CeilToInt(grosor + 1);
                for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++)
                {
                    int X = (int)p.x + x, Y = (int)p.y + y; if (X < 0 || Y < 0 || X >= w || Y >= h) continue;
                    float d = Mathf.Sqrt(x * x + y * y);
                    float v = Mathf.Clamp01(grosor - d + 0.5f) * presion * 0.85f;
                    int k = Y * w + X; if (v > px[k]) px[k] = v;
                }
            }
        }
    }
}
