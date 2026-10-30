using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EstanDentro.EditorMejoras
{
    /// <summary>Vuelca tamaños, orientación y jerarquía de modelos para colocarlos por código (sin abrir el editor).</summary>
    public static class MejorasDatos
    {
        static readonly string[] Modelos =
        {
            "Assets/Art/Hospital/Hospital_Props.fbx",
            "Assets/Art/Prologo/Prologo_Auto.fbx",
        };

        public static void Batch()
        {
            try
            {
                var sb = new StringBuilder();
                var rutas = Modelos.Concat(Directory.GetFiles("Assets/Art/Hospital/PolyHaven", "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')));
                foreach (var ruta in rutas)
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                    if (!go) { sb.AppendLine($"## {ruta}: NO CARGA"); continue; }
                    var inst = (GameObject)UnityEngine.Object.Instantiate(go);
                    var b = Limites(inst);
                    sb.AppendLine($"## {ruta}\n  tamaño {b.size:F2} centro {b.center:F2}");
                    int n = 0;
                    foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    {
                        if (n++ > 60) { sb.AppendLine("  …"); break; }
                        string camino = Camino(r.transform, inst.transform);
                        sb.AppendLine($"  {camino}  centro {r.bounds.center:F2} tamaño {r.bounds.size:F2}  mats [{string.Join(", ", r.sharedMaterials.Where(m => m).Select(m => m.name))}]");
                    }
                    UnityEngine.Object.DestroyImmediate(inst);
                }
                // Pa y Mateo dentro del carro: materiales de personaje (Mixamo Ch31 y Body/Top/Bottom/Shoes)
                var auto = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prologo/Prologo_Auto.fbx"));
                sb.AppendLine("\n## Personajes en Prologo_Auto");
                foreach (var r in auto.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterials.Any(m => m && System.Text.RegularExpressions.Regex.IsMatch(m.name, "Ch31|Body|Top|Bottom|Shoes|Hair|Eye")))
                        sb.AppendLine($"  {Camino(r.transform, auto.transform)} ({r.GetType().Name}) centro {r.bounds.center:F2} tamaño {r.bounds.size:F2} mats [{string.Join(", ", r.sharedMaterials.Where(m => m).Select(m => m.name))}]");
                UnityEngine.Object.DestroyImmediate(auto);

                // la cama: qué extremo es la cabecera (más alto) y dónde va el colchón (alturas más comunes del marco)
                var cama = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Hospital/PolyHaven/old_bed_frame/old_bed_frame_1k.fbx"));
                float altoMas = 0, altoMenos = 0; var alturas = new System.Collections.Generic.Dictionary<int, int>();
                foreach (var mf in cama.GetComponentsInChildren<MeshFilter>())
                    foreach (var v0 in mf.sharedMesh.vertices)
                    {
                        var v = mf.transform.TransformPoint(v0);
                        if (v.z > 0.8f) altoMas = Mathf.Max(altoMas, v.y);
                        if (v.z < -0.8f) altoMenos = Mathf.Max(altoMenos, v.y);
                        if (Mathf.Abs(v.z) < 0.6f) { int k = Mathf.RoundToInt(v.y * 50f); alturas[k] = alturas.TryGetValue(k, out var c) ? c + 1 : 1; }
                    }
                sb.AppendLine($"\n## Cama: alto en z+ {altoMas:F2}, alto en z- {altoMenos:F2}; alturas del centro más comunes: " +
                              string.Join(", ", alturas.OrderByDescending(kv => kv.Value).Take(6).Select(kv => $"{kv.Key / 50f:F2}m×{kv.Value}")));
                UnityEngine.Object.DestroyImmediate(cama);

                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "mejoras_revision"));
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "modelos.txt"), sb.ToString(), Encoding.UTF8);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogError("[Mejoras] " + e); EditorApplication.Exit(1); }
        }

        static string Camino(Transform t, Transform raiz)
        {
            var partes = new System.Collections.Generic.List<string>();
            for (var x = t; x && x != raiz; x = x.parent) partes.Insert(0, x.name);
            return string.Join("/", partes);
        }

        public static Bounds Limites(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
