using UnityEngine;

namespace EstanDentro.Prologo
{
    /// <summary>
    /// Gotas de lluvia sobre el parabrisas (por fuera). Caen, crecen, las grandes se escurren y las plumillas las barren.
    /// La malla del agua tiene UV proyectado sobre el vidrio: u a lo ancho, v hacia arriba del vidrio.
    /// Se dibuja en dos texturas pequeñas (alfa y relieve) que usa un material HDRP transparente.
    /// </summary>
    public class AguaParabrisas : MonoBehaviour
    {
        public Transform[] plumillas;            // pivotes de los limpiabrisas
        public float largoPlumilla = 0.55f;
        public Vector3 centro, ejeU, ejeV;       // proyección del vidrio (mundo, en el espacio del carro)
        public float anchoU = 1.4f, altoV = 0.8f;
        public float gotasPorSegundo = 70f;
        public float brillo = 10f;               // nits de la luz que recogen las gotas

        const int W = 256, H = 144;
        readonly float[] agua = new float[W * H];
        Texture2D alfa, relieve;
        Color32[] pa, pr;
        float[] angPrevio;
        Material mat;

        void Start()
        {
            alfa = new Texture2D(W, H, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp };
            relieve = new Texture2D(W, H, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp };
            pa = new Color32[W * H]; pr = new Color32[W * H];
            mat = GetComponent<Renderer>().material;
            mat.SetTexture("_BaseColorMap", alfa);
            mat.SetTexture("_NormalMap", relieve);
            mat.EnableKeyword("_NORMALMAP"); mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            // las gotas recogen la luz gris del cielo: brillan un poco aunque el carro por dentro esté oscuro
            mat.SetTexture("_EmissiveColorMap", alfa); mat.EnableKeyword("_EMISSIVE_COLOR_MAP");
            mat.SetColor("_EmissiveColor", new Color(0.55f, 0.58f, 0.62f) * brillo);
            angPrevio = new float[plumillas.Length];
            for (int i = 0; i < plumillas.Length; i++) angPrevio[i] = Angulo(i, out _, out _);
            for (int i = 0; i < 900; i++) Gota();                                // ya venía lloviendo
        }

        Vector2 UV(Vector3 mundo)
        {
            var d = transform.parent.InverseTransformPoint(mundo) - centro;
            return new Vector2(Vector3.Dot(d, ejeU) / anchoU + 0.5f, Vector3.Dot(d, ejeV) / altoV + 0.5f);
        }

        float Angulo(int i, out Vector2 piv, out float largo)
        {
            piv = UV(plumillas[i].position);
            var punta = UV(plumillas[i].TransformPoint(Vector3.right * largoPlumilla));
            var d = new Vector2((punta.x - piv.x) * W, (punta.y - piv.y) * H);
            largo = d.magnitude;
            return Mathf.Atan2(d.y, d.x);
        }

        void Gota(float r = -1f)
        {
            int cx = Random.Range(0, W), cy = Random.Range(0, H);
            if (r < 0f) r = Random.value < 0.85f ? Random.Range(0.6f, 1.6f) : Random.Range(1.6f, 3.2f);
            int ri = Mathf.CeilToInt(r), riy = Mathf.CeilToInt(r * 1.8f);   // el vidrio inclinado se ve aplastado: la gota se dibuja más alta
            for (int y = -riy; y <= riy; y++)
                for (int x = -ri; x <= ri; x++)
                {
                    int px = cx + x, py = cy + y; if (px < 0 || py < 0 || px >= W || py >= H) continue;
                    float yy = y / 1.8f, q = 1f - (x * x + yy * yy) / (r * r); if (q <= 0f) continue;
                    int k = py * W + px; agua[k] = Mathf.Min(1.4f, agua[k] + Mathf.Sqrt(q) * 0.55f);
                }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            int n = Mathf.RoundToInt(gotasPorSegundo * dt + Random.value);
            for (int i = 0; i < n; i++) Gota();

            // las gotas grandes pesan: se escurren hacia abajo (v baja); el viento del carro las empuja un poco hacia arriba
            for (int y = 1; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int k = y * W + x;
                    if (agua[k] > 1.05f && Random.value < 0.25f) { float m = (agua[k] - 0.9f); agua[k] -= m; agua[k - W] += m * 0.97f; }
                }

            // plumillas: lo que barren queda casi seco (una película fina al borde)
            for (int i = 0; i < plumillas.Length; i++)
            {
                float a1 = Angulo(i, out var piv, out float largo), a0 = angPrevio[i];
                angPrevio[i] = a1;
                float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                if (Mathf.Abs(da) < 1e-4f) continue;
                float lo = Mathf.Min(a0, a0 + da) - 0.045f, hi = Mathf.Max(a0, a0 + da) + 0.045f;   // el ancho de la goma
                float px0 = piv.x * W, py0 = piv.y * H;
                int R = Mathf.CeilToInt(largo * 1.15f) + 4;
                for (int y = Mathf.Max(0, (int)py0 - R); y < Mathf.Min(H, (int)py0 + R); y++)
                    for (int x = Mathf.Max(0, (int)px0 - R); x < Mathf.Min(W, (int)px0 + R); x++)
                    {
                        float dx = x - px0, dy = y - py0, rr = Mathf.Sqrt(dx * dx + dy * dy);
                        if (rr < largo * 0.04f || rr > largo * 1.15f + 2f) continue;      // toda la hoja, de punta a punta (con su goma)
                        float an = Mathf.Atan2(dy, dx);
                        an = a0 + Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, an * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        if (an >= lo && an <= hi) agua[y * W + x] *= 0.15f;                 // queda una película fina: el borde no queda como línea
                    }
            }

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int k = y * W + x; float h = agua[k];
                    float hx = agua[y * W + Mathf.Min(W - 1, x + 1)] - agua[y * W + Mathf.Max(0, x - 1)];
                    float hy = agua[Mathf.Min(H - 1, y + 1) * W + x] - agua[Mathf.Max(0, y - 1) * W + x];
                    // el agua es clara: casi transparente, deja ver el borde brillante (luz del cielo) donde la gota se curva
                    float q = Mathf.Clamp01(h * 0.9f), borde = Mathf.Clamp01((Mathf.Abs(hx) + Mathf.Abs(hy)) * 0.45f);   // brillo solo en la curva de cada gota
                    byte al = (byte)(q * 28f + borde * 34f);
                    byte br = (byte)(140 + borde * 115f);
                    pa[k] = new Color32(br, br, (byte)Mathf.Min(255, br + 10), al);
                    // relieve en el formato que lee HDRP (x en alfa, y en verde)
                    pr[k] = new Color32(255, (byte)Mathf.Clamp(128 - hy * 150f, 0, 255), 0, (byte)Mathf.Clamp(128 - hx * 150f, 0, 255));
                }
            alfa.SetPixels32(pa); alfa.Apply(false);
            relieve.SetPixels32(pr); relieve.Apply(false);
        }
    }
}
