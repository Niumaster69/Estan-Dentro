using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Revisa los vehículos del prólogo dentro de la escena (con su luz y su cielo): una foto de cada uno y sus materiales.</summary>
    public static class FotosVehiculos
    {
        public static void Batch()
        {
            string carpeta = System.Environment.GetCommandLineArgs() is var a && System.Array.IndexOf(a, "-fotos") is int i && i >= 0 ? a[i + 1] : "FotosVehiculos";
            Directory.CreateDirectory(carpeta);
            EditorSceneManager.OpenScene(ConstruirPrologo.ESCENA);
            foreach (var cam0 in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam0.enabled = false;
            var moldes = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                               .Where(t => t.parent == null && new[] { "renault_logan", "chevrolet_spark", "chevrolet_aveo", "kia_picanto", "bus_sitp", "camion_jumbo" }.Contains(t.name)).ToList();
            var go = new GameObject("CamFoto", typeof(Camera)); var cam = go.GetComponent<Camera>();
            go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.fieldOfView = 40f;
            int k = 0;
            foreach (var m in moldes)
            {
                foreach (var o in moldes) o.gameObject.SetActive(o == m);
                var pos = new Vector3(40f, 0f, 60f + 30f * k++);
                m.position = pos; m.rotation = Quaternion.identity;
                var b = m.GetComponentsInChildren<Renderer>().Aggregate(new Bounds(pos, Vector3.zero), (x, r) => { x.Encapsulate(r.bounds); return x; });
                Debug.Log($"[Veh] TAMAÑO {m.name} {b.size:F2} escala {m.lossyScale:F3}");
                go.transform.position = b.center + new Vector3(b.size.z * 0.55f, b.size.y * 0.3f, b.size.z * 0.75f);
                go.transform.LookAt(b.center);
                for (int f = 0; f < 3; f++) cam.Render();
                RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(carpeta, m.name + ".png"), tex.EncodeToPNG());
                foreach (var r in m.GetComponentsInChildren<Renderer>())
                    foreach (var mt in r.sharedMaterials.Where(x => x))
                        Debug.Log($"[Veh] {m.name} {mt.name} shader={mt.shader.name} color={(mt.HasProperty("_BaseColor") ? mt.GetColor("_BaseColor").ToString("F2") : "-")} mapa={(mt.HasProperty("_BaseColorMap") && mt.GetTexture("_BaseColorMap") ? mt.GetTexture("_BaseColorMap").name : "NINGUNO")} superficie={(mt.HasProperty("_SurfaceType") ? mt.GetFloat("_SurfaceType") : -1)}");
            }
            EditorApplication.Exit(0);
        }
    }
}
