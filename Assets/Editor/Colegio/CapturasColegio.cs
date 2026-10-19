using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace EstanDentro.EditorColegio
{
    /// <summary>Toma capturas de la escena del colegio desde puntos fijos (para revisar sin abrir el editor).</summary>
    public static class CapturasColegio
    {
        public static string Carpeta = "../../01_Documentacion/avances_edificio/unity";

        // nombre, posición (Unity: x, altura, z), rumbo en grados (0 = +Z), inclinación
        static readonly (string, Vector3, float, float)[] Puntos =
        {
            ("01_salon4B_inicio", new Vector3(27.0f, 7.2f + 1.65f, 34.6f), 225f, 8f),
            ("02_pasillo_P3", new Vector3(11.5f, 7.2f + 1.65f, 27.6f), 90f, 2f),
            ("03_patio", new Vector3(36.5f, 1.65f, 14.0f), -70f, -8f),
            ("04_fachada", new Vector3(16.0f, 1.7f, -16.0f), 20f, -6f),
            ("05_cafeteria", new Vector3(44.5f, 1.8f, 27.6f), 180f, 8f),
            ("06_biblioteca", new Vector3(11.0f, 3.6f + 1.6f, 6.2f), 120f, 10f),
        };

        // puntos para revisar fugas de luz: uniones muro-techo y esquinas con sol directo
        static readonly (string, Vector3, float, float)[] PuntosFugas =
        {
            ("fuga_01_salon4B_esquina", new Vector3(26.6f, 7.2f + 1.6f, 34.4f), 300f, 35f),
            ("fuga_02_salon4B_techo", new Vector3(24.0f, 7.2f + 1.6f, 32.5f), 0f, 60f),
            ("fuga_03_cafeteria_techo", new Vector3(44.5f, 1.6f, 22.0f), 90f, 40f),
            ("fuga_04_pasillo_techo", new Vector3(20.0f, 7.2f + 1.6f, 27.4f), 180f, 55f),
            ("fuga_05_biblioteca_rincon", new Vector3(36.0f, 3.6f + 1.6f, 5.5f), 160f, 35f),
            ("fuga_06_aula_lejos", new Vector3(21.0f, 3.6f + 1.6f, 34.6f), 120f, 25f),
        };

        [MenuItem("Estan Dentro/5. Capturas de fugas de luz")]
        public static void CapturarFugas() => CapturarPuntos(PuntosFugas, "unity_fugas");

        public static void CapturarFugasBatch()
        {
            try { CapturarFugas(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Captura] " + e); EditorApplication.Exit(1); }
        }

        [MenuItem("Estan Dentro/4. Capturas de revisión")]
        public static void Capturar() => CapturarPuntos(Puntos, "unity");

        static void CapturarPuntos((string, Vector3, float, float)[] puntos, string sub)
        {
            Carpeta = "../../01_Documentacion/avances_edificio/" + sub;
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Carpeta));
            Directory.CreateDirectory(dir);
            var go = new GameObject("CamCaptura");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 3000f;
            var hd = go.AddComponent<HDAdditionalCameraData>();
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            cam.targetTexture = rt;
            foreach (var (nombre, pos, rumbo, inc) in puntos)
            {
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(-inc, rumbo, 0f);
                for (int i = 0; i < 24; i++) cam.Render();          // varios cuadros: exposición automática y volumétricos
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, nombre + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log($"[Captura] {nombre}");
            }
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"[Captura] listas en {dir}");
        }

        public static void CapturarBatch()
        {
            try { Capturar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Captura] " + e); EditorApplication.Exit(1); }
        }
    }
}
