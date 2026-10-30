using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorMejoras
{
    /// <summary>
    /// Lo que la rama de mejoras pone en la escena del colegio. Se llama desde ArreglosColegio.Batch (así sobrevive cuando
    /// el equipo rearma la escena) y también se puede correr sola:
    ///   Unity.exe -batchmode -projectPath . -executeMethod EstanDentro.EditorMejoras.MejorasEscena.AplicarBatch
    /// Cada cosa se borra por nombre y se vuelve a crear: correrlo dos veces deja lo mismo.
    /// </summary>
    public static class MejorasEscena
    {
        const string ESCENA = "Assets/Scenes/Colegio/Colegio.unity";
        const string DIR = "Assets/Art/Mejoras";
        const float P3 = 7.2f;

        public static void Aplicar()
        {
            BuclePasilloNorte();
            JacarandaEnFlor();
        }

        [MenuItem("Estan Dentro/Mejoras/Aplicar a la escena del colegio")]
        public static void AplicarYGuardar()
        {
            var esc = EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
            Aplicar();
            EditorSceneManager.SaveScene(esc);
        }

        public static void AplicarBatch()
        {
            try { AplicarYGuardar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Mejoras] " + e); EditorApplication.Exit(1); }
        }

        /// <summary>
        /// Pasillo norte del piso 3 (sale del 4°B hacia el oeste, rumbo al aula de música): en la Pesadilla del recuerdo
        /// "pasillo_4b", al llegar cerca de la esquina noroeste Mateo vuelve 8 m atrás, otra vez frente a la puerta del 4°B
        /// (PUERTA_P3_009 en x 20,6). Se rompe respirando con Pa (Mente/BuclePasillo.cs).
        /// </summary>
        static void BuclePasilloNorte()
        {
            var viejo = GameObject.Find("Mejoras_BuclePasillo"); if (viejo) UnityEngine.Object.DestroyImmediate(viejo);
            var go = new GameObject("Mejoras_BuclePasillo");
            go.transform.position = new Vector3(12.6f, P3 + 1.3f, 27.3f);
            var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.8f, 2.6f, 4.2f);
            var b = go.AddComponent<Mente.BuclePasillo>(); b.retroceso = new Vector3(8f, 0f, 0f);
            Debug.Log("[Mejoras] bucle del pasillo norte en " + go.transform.position);
        }

        // ------------------------------------------------------------------ jacarandá en flor
        static readonly Color Lila = new Color(0.72f, 0.62f, 0.96f);           // lavanda del jacarandá (más claro que morado)
        const float Brillo = 2.4f;

        /// <summary>
        /// El jacarandá del patio florecido (POR_HACER F: "flores moradas"; DECISIONES: es un punto de referencia siempre
        /// visible). El modelo trae el follaje verde en una textura incrustada en el FBX: se lee desde la GPU, se pasa a
        /// lavanda conservando el recorte de las hojas y queda como textura y material propios en Assets/Art/Mejoras/.
        /// </summary>
        static void JacarandaEnFlor()
        {
            var raiz = GameObject.Find("Jacaranda");
            if (!raiz) { Debug.LogWarning("[Mejoras] no encontré el Jacaranda en la escena"); return; }
            if (!AssetDatabase.IsValidFolder(DIR)) AssetDatabase.CreateFolder("Assets/Art", "Mejoras");
            var hechos = new System.Collections.Generic.Dictionary<Material, Material>();
            int n = 0;
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials; bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (!m) continue;
                    if (!hechos.TryGetValue(m, out var flor)) hechos[m] = flor = Florecer(m);
                    if (flor && flor != m) { mats[i] = flor; cambio = true; }
                }
                if (cambio) { r.sharedMaterials = mats; EditorUtility.SetDirty(r); n++; }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mejoras] jacarandá en flor: {n} partes, {hechos.Count(kv => kv.Value)} materiales de follaje");
        }

        /// <summary>
        /// Follaje (verde) → material "_flor" con textura lavanda; corteza → null. Si el material ya era "_flor", rehace la
        /// textura desde la original (por si cambió el tono).
        /// </summary>
        static Material Florecer(Material m)
        {
            bool yaFlor = m.name.EndsWith("_flor");
            Material original = m;
            if (yaFlor)
            {
                var ruta = AssetDatabase.FindAssets("t:Material " + m.name.Substring(0, m.name.Length - 5)).Select(AssetDatabase.GUIDToAssetPath)
                    .Concat(new[] { "Assets/Art/Edificio/Jacaranda.fbx" })
                    .SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>())
                    .FirstOrDefault(x => x.name + "_flor" == m.name);
                if (ruta) original = ruta;
            }
            var tex = original.HasProperty("_BaseColorMap") ? original.GetTexture("_BaseColorMap") : original.mainTexture;
            Color base0 = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
            Color[] px = null; int w = 0, h = 0;
            if (tex) px = LeerGPU(tex, out w, out h);
            Color prom = px != null ? Promedio(px) * base0 : base0;
            if (!yaFlor && !(prom.g > prom.r * 1.08f && prom.g > prom.b * 1.08f)) return null;   // no es verde: corteza

            var flor = yaFlor ? m : new Material(original) { name = original.name + "_flor" };
            if (px != null)
            {
                for (int i = 0; i < px.Length; i++)
                {
                    var c = px[i]; float l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    var nc = Lila * Mathf.Clamp01(l * Brillo); px[i] = new Color(nc.r, nc.g, nc.b, c.a);
                }
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.SetPixels(px); t.Apply();
                string rutaTex = $"{DIR}/{original.name}_flor.png";
                File.WriteAllBytes(rutaTex, t.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(rutaTex);
                var imp = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
                if (imp) { imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
                var nueva = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
                if (flor.HasProperty("_BaseColorMap")) flor.SetTexture("_BaseColorMap", nueva); else flor.mainTexture = nueva;
                if (flor.HasProperty("_BaseColor")) flor.SetColor("_BaseColor", new Color(1f, 1f, 1f, base0.a));
            }
            else if (flor.HasProperty("_BaseColor")) flor.SetColor("_BaseColor", new Color(Lila.r, Lila.g, Lila.b, base0.a));
            if (!yaFlor)
            {
                string rutaMat = $"{DIR}/{flor.name}.mat";
                var viejo = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
                if (viejo) { viejo.CopyPropertiesFromMaterial(flor); flor = viejo; }
                else AssetDatabase.CreateAsset(flor, rutaMat);
            }
            EditorUtility.SetDirty(flor);
            return flor;
        }

        /// <summary>Lee los píxeles de cualquier textura (también las incrustadas en un FBX, que no se pueden leer como archivo).</summary>
        static Color[] LeerGPU(Texture tex, out int w, out int h)
        {
            w = Mathf.Min(tex.width, 2048); h = Mathf.Min(tex.height, 2048);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt);
            var antes = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = antes; RenderTexture.ReleaseTemporary(rt);
            var px = t.GetPixels(); UnityEngine.Object.DestroyImmediate(t);
            return px;
        }

        static Color Promedio(Color[] px)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            for (int i = 0; i < px.Length; i += 37) { if (px[i].a < 0.3f) continue; r += px[i].r; g += px[i].g; b += px[i].b; n++; }
            return n == 0 ? Color.gray : new Color((float)(r / n), (float)(g / n), (float)(b / n));
        }
    }
}
