using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Revisa los objetos de Mateo en la escena del prólogo (las piernas no se ven).</summary>
    public static class DiagnosticoPrologo
    {
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
            EditorApplication.Exit(0);
        }
    }
}
