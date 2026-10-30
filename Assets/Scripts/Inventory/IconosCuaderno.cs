using UnityEngine;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// Íconos de las pestañas del cuaderno, dibujados en el juego (sin archivos): mochila = objetos, lupa = pistas,
    /// diana = objetivos. Un ícono se entiende más rápido que una palabra girada.
    /// </summary>
    public static class IconosCuaderno
    {
        public enum Icono { Mochila, Lupa, Diana }
        const int N = 96;

        public static Sprite Crear(Icono i)
        {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    // coordenadas de -1 a 1; d < 0 = dentro de la forma (distancia con signo, en unidades de la textura)
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float d = i == Icono.Mochila ? Mochila(u, v) : i == Icono.Lupa ? Lupa(u, v) : Diana(u, v);
                    float a = Mathf.Clamp01(0.5f - d * N * 0.5f);              // borde suave de un pixel
                    px[y * N + x] = new Color32(242, 237, 217, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        static float Caja(float u, float v, float cx, float cy, float w, float h, float r)
        {
            float qx = Mathf.Abs(u - cx) - w + r, qy = Mathf.Abs(v - cy) - h + r;
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }
        static float Anillo(float u, float v, float cx, float cy, float r, float grosor)
            => Mathf.Abs(new Vector2(u - cx, v - cy).magnitude - r) - grosor;
        static float Linea(float u, float v, Vector2 a, Vector2 b, float grosor)
        {
            var p = new Vector2(u, v); var ab = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - a - ab * h).magnitude - grosor;
        }

        // mochila: cuerpo redondeado, asa arriba y bolsillo (hueco) adelante
        static float Mochila(float u, float v)
        {
            float cuerpo = Caja(u, v, 0f, -0.12f, 0.52f, 0.62f, 0.18f);
            float asa = Mathf.Max(Anillo(u, v, 0f, 0.5f, 0.2f, 0.07f), -(v - 0.5f));
            float bolsillo = Caja(u, v, 0f, -0.38f, 0.32f, 0.2f, 0.07f);
            float ranura = Mathf.Abs(bolsillo) - 0.045f;                       // la costura del bolsillo, calada en el cuerpo
            return Mathf.Min(Mathf.Max(cuerpo, -ranura), asa);
        }

        // lupa: aro y mango en diagonal
        static float Lupa(float u, float v)
            => Mathf.Min(Anillo(u, v, -0.15f, 0.15f, 0.42f, 0.09f), Linea(u, v, new Vector2(0.17f, -0.17f), new Vector2(0.68f, -0.68f), 0.11f));

        // diana: dos anillos y el centro
        static float Diana(float u, float v)
            => Mathf.Min(Mathf.Min(Anillo(u, v, 0f, 0f, 0.74f, 0.08f), Anillo(u, v, 0f, 0f, 0.42f, 0.08f)), new Vector2(u, v).magnitude - 0.15f);
    }
}
