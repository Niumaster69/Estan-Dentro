using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// Dibujo a lápiz de cada objeto para la mochila (Henry: "los objetos deberían aparecer como dibujos, no como textos").
    /// Se fotografía su modelo (Resources/Objetos/&lt;id&gt;) una vez, sobre papel, y se pasa a grafito: gris + bordes marcados.
    /// </summary>
    public static class DibujosObjetos
    {
        const int CAPA = 31, TAM = 256;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Transform mundo; static Camera cam; static RenderTexture rt;
        static readonly Color papel = new Color(0.93f, 0.90f, 0.82f);

        /// <summary>El modelo de cada objeto (las llaves de cada puerta usan el mismo manojo).</summary>
        public static string Modelo(string id) => id != null && id.StartsWith("llave") ? "llaves" : id;

        public static Sprite De(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache.TryGetValue(id, out var s)) return s;
            return cache[id] = Dibujar(id);
        }

        static Sprite Dibujar(string id)
        {
            var molde = Resources.Load<GameObject>("Objetos/" + Modelo(id));
            if (!molde) return null;
            Preparar();
            // HDRP dibuja el objeto donde estaba al CREARSE (moverlo en el mismo cuadro no llega): todo se calcula antes,
            // el objeto se crea ya en su sitio y el encuadre se hace con la cámara (no escalando el objeto)
            var rot = Quaternion.Euler(-28f, 35f, 0f);
            bool hay = false; Bounds b = default;
            foreach (var mf in molde.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var m = Matrix4x4.Rotate(rot) * mf.transform.localToWorldMatrix; var mb = mf.sharedMesh.bounds;
                for (int k = 0; k < 8; k++)
                {
                    var q = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)));
                    if (!hay) { b = new Bounds(q, Vector3.zero); hay = true; } else b.Encapsulate(q);
                }
            }
            if (!hay) return null;
            var go = Object.Instantiate(molde, mundo.position - b.center, rot, mundo);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CAPA;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            cam.orthographicSize = Mathf.Max(b.extents.x, b.extents.y) * 1.3f;
            cam.transform.position = mundo.position - Vector3.forward * (b.extents.z + 1f);
            cam.nearClipPlane = 0.05f; cam.farClipPlane = b.size.z + 2.5f;
            // dos fotos, con fondo negro y con fondo blanco: lo que cambia entre ellas es fondo (no depende de la exposición)
            var negro = Foto(Color.black); var blanco = Foto(Color.white * 4f);
            Object.DestroyImmediate(go);                // al instante: si no, sale en el dibujo del siguiente objeto
            var dib = Grafito(negro, blanco);
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autoprueba") >= 0)
            {   // diagnóstico: las fotos crudas y el dibujo, junto al log del juego
                string d = System.IO.Path.Combine(Application.persistentDataPath, "dibujos"); System.IO.Directory.CreateDirectory(d);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(d, id + "_negro.png"), negro.EncodeToPNG());
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(d, id + "_dibujo.png"), dib.EncodeToPNG());
            }
            Object.Destroy(negro); Object.Destroy(blanco);
            return Sprite.Create(dib, new Rect(0, 0, TAM, TAM), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Foto -> lápiz: luminancia aclarada, bordes (Sobel) en grafito y fondo transparente donde es papel.</summary>
        /// <summary>Límites desde las mallas y las matrices (Renderer.bounds puede venir viejo sin un cuadro de por medio).</summary>
        static Bounds Limites(GameObject go)
        {
            bool hay = false; Bounds b = default;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh) continue;
                var m = mf.transform.localToWorldMatrix; var mb = mf.sharedMesh.bounds;
                for (int k = 0; k < 8; k++)
                {
                    var p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)));
                    if (!hay) { b = new Bounds(p, Vector3.zero); hay = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        static Texture2D Foto(Color fondoColor)
        {
            cam.GetComponent<HDAdditionalCameraData>().backgroundColorHDR = fondoColor;
            cam.enabled = true;                         // en HDRP una cámara apagada no dibuja con Render()
            cam.Render(); cam.Render(); cam.Render();
            cam.enabled = false;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(TAM, TAM, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, TAM, TAM), 0, 0); t.Apply();
            RenderTexture.active = prev;
            return t;
        }

        static Texture2D Grafito(Texture2D fn, Texture2D fb)
        {
            var px = fn.GetPixels(); var pb = fb.GetPixels(); int n = px.Length;
            var l = new float[n]; var fondo = new bool[n];
            float lmin = 9f, lmax = -9f;
            for (int i = 0; i < n; i++)
            {
                var c = px[i];
                fondo[i] = (pb[i].r + pb[i].g + pb[i].b) - (c.r + c.g + c.b) > 1.2f;
                l[i] = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                if (!fondo[i]) { lmin = Mathf.Min(lmin, l[i]); lmax = Mathf.Max(lmax, l[i]); }
            }
            float rango = Mathf.Max(0.05f, lmax - lmin);
            var o = new Color[n]; var grafito = new Color(0.16f, 0.15f, 0.17f);
            for (int y = 0; y < TAM; y++)
                for (int x = 0; x < TAM; x++)
                {
                    int i = y * TAM + x;
                    if (fondo[i]) { o[i] = new Color(grafito.r, grafito.g, grafito.b, 0f); continue; }
                    // contorno: pixel del objeto con algún vecino de fondo (a 2 px) = línea de tinta
                    bool contorno = false;
                    for (int dy = -2; dy <= 2 && !contorno; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int xx = x + dx, yy = y + dy;
                            if (xx < 0 || yy < 0 || xx >= TAM || yy >= TAM || fondo[yy * TAM + xx]) { contorno = true; break; }
                        }
                    float Ln(int dx, int dy) { int xx = Mathf.Clamp(x + dx, 0, TAM - 1), yy = Mathf.Clamp(y + dy, 0, TAM - 1); int j = yy * TAM + xx; return fondo[j] ? 1f : (l[j] - lmin) / rango; }
                    float gx = -Ln(-1, -1) - 2 * Ln(-1, 0) - Ln(-1, 1) + Ln(1, -1) + 2 * Ln(1, 0) + Ln(1, 1);
                    float gy = -Ln(-1, -1) - 2 * Ln(0, -1) - Ln(1, -1) + Ln(-1, 1) + 2 * Ln(0, 1) + Ln(1, 1);
                    float borde = Mathf.Clamp01(Mathf.Sqrt(gx * gx + gy * gy) * 0.9f);
                    float oscuro = 1f - (l[i] - lmin) / rango;                                   // 0 = lo más claro del objeto
                    float rayado = ((x + y) % 4 == 0) ? 1f : ((x - y + 512) % 6 == 0 && oscuro > 0.6f ? 0.8f : 0.25f);   // trazos cruzados en lo oscuro
                    float tinta = contorno ? 0.95f : Mathf.Clamp01(borde * 0.85f + oscuro * 0.55f * rayado + 0.06f);
                    o[i] = new Color(grafito.r, grafito.g, grafito.b, tinta);
                }
            var t = new Texture2D(TAM, TAM, TextureFormat.RGBA32, false); t.SetPixels(o); t.Apply();
            return t;
        }

        static void Preparar()
        {
            if (mundo) return;
            mundo = new GameObject("Dibujos_Mundo").transform;
            mundo.position = new Vector3(0f, -12000f, 0f);
            if (Application.isPlaying) Object.DontDestroyOnLoad(mundo.gameObject);
            rt = new RenderTexture(TAM, TAM, 24, RenderTextureFormat.ARGB32);
            var cg = new GameObject("Camara"); cg.layer = CAPA; cg.transform.SetParent(mundo, false);
            cg.transform.localPosition = new Vector3(0f, 0f, -2.2f);
            cam = cg.AddComponent<Camera>(); cam.enabled = false;
            cam.targetTexture = rt; cam.orthographic = true; cam.orthographicSize = 0.5f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 10f;
            cam.cullingMask = 1 << CAPA;
            var hd = cg.AddComponent<HDAdditionalCameraData>();
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hd.volumeLayerMask = 1 << CAPA;
            hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;      // el TAA mezclaba la foto de fondo negro con la de fondo blanco
            var vg = new GameObject("Volumen"); vg.layer = CAPA; vg.transform.SetParent(mundo, false);
            var vol = vg.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 101f;
            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            var exp = perfil.Add<Exposure>(true); exp.mode.Override(ExposureMode.Fixed); exp.fixedExposure.Override(5.5f);
            var fog = perfil.Add<Fog>(true); fog.enabled.Override(false);
            var tm = perfil.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            var bl = perfil.Add<Bloom>(true); bl.intensity.Override(0f);                 // sin resplandor: se comía los bordes
            var gr = perfil.Add<FilmGrain>(true); gr.intensity.Override(0f);
            var vi = perfil.Add<Vignette>(true); vi.intensity.Override(0f);
            vol.sharedProfile = perfil;
            foreach (var (p, lm) in new[] { (new Vector3(1.2f, 1.6f, -1.4f), 160f), (new Vector3(-1.5f, 0.4f, -1.0f), 60f) })
            {
                var g = new GameObject("Luz"); g.layer = CAPA; g.transform.SetParent(mundo, false); g.transform.localPosition = p;
                var luz = g.AddComponent<Light>(); luz.type = LightType.Point; luz.range = 5f; luz.lightUnit = LightUnit.Lumen; luz.intensity = lm; luz.shadows = LightShadows.None;
            }
        }
    }
}
