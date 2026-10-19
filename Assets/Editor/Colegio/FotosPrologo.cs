using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Fotos de la escena del prólogo desde los ojos de Mateo, sin abrir el juego (para revisar luz, Pa y piernas).</summary>
    public static class FotosPrologo
    {
        public static void FotosBatch()
        {
            string carpeta = System.Environment.GetCommandLineArgs() is var a && System.Array.IndexOf(a, "-fotos") is int i && i >= 0 && i + 1 < a.Length ? a[i + 1] : "FotosPrologo";
            Directory.CreateDirectory(carpeta);
            EditorSceneManager.OpenScene(ConstruirPrologo.ESCENA);
            var cam = Object.FindAnyObjectByType<Camera>();
            var rot0 = cam.transform.localRotation;
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            void Foto(string nombre, float yaw, float pitch)
            {
                cam.transform.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
                for (int k = 0; k < 8; k++) cam.Render();                     // varias pasadas: HDRP acumula exposición y sombras
                RenderTexture.active = rt;
                var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply();
                File.WriteAllBytes(Path.Combine(carpeta, nombre + ".png"), t.EncodeToPNG());
                RenderTexture.active = null;
            }
            Foto("1_al_frente", 0f, 0f);
            Foto("2_hacia_Pa", -60f, 8f);
            Foto("3_piernas", -5f, 69f);
            Foto("4_radio_lonchera", -25f, 35f);
            // las manos de Pa en el timón, de cerca (desde el asiento de Mateo, inclinándose)
            var p0 = cam.transform.position; var r0 = cam.transform.rotation;
            var timon = System.Linq.Enumerable.FirstOrDefault(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None), t => t.name.StartsWith("corolla_e180_steering_wheel"))?.gameObject;
            if (timon)
            {
                var b = new Bounds(timon.transform.position, Vector3.zero);
                foreach (var r in timon.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                cam.transform.position = Vector3.Lerp(p0, b.center, 0.45f) + Vector3.up * 0.05f;
                cam.transform.LookAt(b.center);
                cam.fieldOfView = 55f;
                for (int k = 0; k < 8; k++) cam.Render();
                RenderTexture.active = rt;
                var t2 = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false); t2.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t2.Apply();
                File.WriteAllBytes(Path.Combine(carpeta, "5_manos_de_cerca.png"), t2.EncodeToPNG()); RenderTexture.active = null;
            }
            cam.transform.SetPositionAndRotation(p0, r0);
            cam.targetTexture = null;
            Debug.Log("[FotosPrologo] listas en " + carpeta);
            EditorApplication.Exit(0);
        }
    }
}
