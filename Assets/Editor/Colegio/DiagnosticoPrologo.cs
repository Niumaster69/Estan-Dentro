using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Revisa los objetos de Mateo en la escena del prólogo (las piernas no se ven).</summary>
    public static class DiagnosticoPrologo
    {
/// <summary>Pinta cada material de un color único y lee qué material hay en puntos de la foto 4 (para encontrar las rayas blancas).</summary>
        public static void Colores()
        {
            EditorSceneManager.OpenScene(ConstruirPrologo.ESCENA);
            var cam = Object.FindAnyObjectByType<Camera>();
            cam.transform.localRotation = cam.transform.localRotation * Quaternion.Euler(0f, -25f, 0f) * Quaternion.Euler(35f, 0f, 0f);
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if (mf.sharedMesh && mf.GetComponent<Renderer>() && !mf.GetComponent<MeshCollider>()) { foreach (var c0 in mf.GetComponents<Collider>()) Object.DestroyImmediate(c0); mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)) { var bm = new Mesh(); smr.BakeMesh(bm); smr.gameObject.AddComponent<MeshCollider>().sharedMesh = bm; }
            Physics.SyncTransforms();
            cam.targetTexture = new RenderTexture(1600, 900, 24);
            foreach (var (n, x, y) in new[] { ("franja", 1200, 330), ("palanca", 745, 600), ("puertaD", 1450, 440), ("puertaI", 40, 330), ("vaso", 560, 770), ("reloj", 470, 160), ("timon_borde", 255, 300) })
            {
                var ray = cam.ScreenPointToRay(new Vector3(x, 900 - y, 0));
                if (!Physics.Raycast(ray, out var h, 5f)) { Debug.Log($"[Rayo] {n}: nada"); continue; }
                var mc = h.collider as MeshCollider; var r = h.collider.GetComponent<Renderer>(); string mat = "?";
                if (mc && r && mc.sharedMesh)
                {
                    var me = mc.sharedMesh; int tri = h.triangleIndex;
                    for (int sm = 0; sm < me.subMeshCount; sm++) { var d = me.GetSubMesh(sm); if (tri * 3 >= d.indexStart && tri * 3 < d.indexStart + d.indexCount) { mat = r.sharedMaterials[Mathf.Min(sm, r.sharedMaterials.Length - 1)].name; break; } }
                    if (me.subMeshCount == 1) mat = r.sharedMaterial ? r.sharedMaterial.name : "null";
                }
                Debug.Log($"[Rayo] {n}: {h.collider.name} mat={mat} dist={h.distance:F2}");
            }
            EditorApplication.Exit(0);
        }

public static void Escaleras()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity");
            foreach (var d in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)) { }
            Physics.SyncTransforms();
            foreach (var pt in new[] { new Vector3(3f, 0, 30f), new Vector3(3f, 0, 32f), new Vector3(1f, 0, 31f), new Vector3(5f, 0, 32.5f) })
                foreach (var h in Physics.RaycastAll(pt + Vector3.up * 14f, Vector3.down, 20f, ~0, QueryTriggerInteraction.Collide).OrderBy(h => h.distance))
                {
                    var r = h.collider.GetComponent<Renderer>(); string mat = "?";
                    var mc = h.collider as MeshCollider;
                    if (mc && r && mc.sharedMesh && mc.sharedMesh.isReadable)
                    { var me = mc.sharedMesh; for (int sm = 0; sm < me.subMeshCount; sm++) { var dd = me.GetSubMesh(sm); if (h.triangleIndex * 3 >= dd.indexStart && h.triangleIndex * 3 < dd.indexStart + dd.indexCount) { mat = r.sharedMaterials[Mathf.Min(sm, r.sharedMaterials.Length - 1)].name; break; } } }
                    else if (r) mat = r.sharedMaterial ? r.sharedMaterial.name : "null";
                    Debug.Log($"[Esc] punto {pt.x},{pt.z} y={h.point.y:F2} {h.collider.name} trigger={h.collider.isTrigger} mat={mat}");
                }
            EditorApplication.Exit(0);
        }

        public static void Blancos()
        {
            EditorSceneManager.OpenScene(ConstruirPrologo.ESCENA);
            var auto = GameObject.Find("Carro_de_Pa");
            var vistos = new System.Collections.Generic.HashSet<Material>();
            foreach (var r in auto.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m && vistos.Add(m) && m.HasProperty("_BaseColor"))
                    {
                        var c = m.GetColor("_BaseColor"); var tx = m.GetTexture("_BaseColorMap");
                        Debug.Log($"[Blanco] {m.name} color={c} tex={(tx ? tx.name : "-")} suave={m.GetFloat("_Smoothness"):F2} en={r.name} ruta={AssetDatabase.GetAssetPath(m)}");
                    }
            EditorApplication.Exit(0);
        }

        public static void Batch()
        {
            EditorSceneManager.OpenScene(ConstruirPrologo.ESCENA);
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                   .Where(r => r.name.EndsWith(".001") && r.name.StartsWith("Object_")))
            {
                var mf = r.GetComponent<MeshFilter>(); var smr = r as SkinnedMeshRenderer;
                var mesh = mf ? mf.sharedMesh : smr ? smr.sharedMesh : null;
                Debug.Log($"[Diag] {r.name} activo={r.gameObject.activeInHierarchy} renderer={r.enabled} tipo={r.GetType().Name} " +
                          $"centro={r.bounds.center:F2} tamaño={r.bounds.size:F2} vertices={(mesh ? mesh.vertexCount : -1)} " +
                          $"submallas={(mesh ? mesh.subMeshCount : -1)} materiales={string.Join(",", r.sharedMaterials.Select(m => m ? m.name + "(surf " + m.GetFloat("_SurfaceType") + ", cut " + m.GetFloat("_AlphaCutoffEnable") + ")" : "null"))}");
            }
            // ¿qué tapa la cámara? lo que esté a menos de 40 cm de los ojos
            var cam = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c => c.name == "Ojos_Mateo");
            Debug.Log($"[Diag] camara en {cam.transform.position:F2} mira {cam.transform.forward:F2}");
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (r.enabled && r.bounds.SqrDistance(cam.transform.position) < 0.16f)
                    Debug.Log($"[Diag] CERCA {r.name} centro={r.bounds.center:F2} tam={r.bounds.size:F2} dist={Mathf.Sqrt(r.bounds.SqrDistance(cam.transform.position)):F2}");
            EditorApplication.Exit(0);
        }
    }
}
