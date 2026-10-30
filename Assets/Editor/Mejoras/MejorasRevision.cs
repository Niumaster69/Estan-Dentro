using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace EstanDentro.EditorMejoras
{
    /// <summary>
    /// Herramientas de revisión de la rama de mejoras (Duvan + Claude, 30-oct): volcar dónde está cada cosa importante
    /// del capítulo 1, sacar una planta del piso 3 desde arriba y capturas de la ruta. Todo por línea de comandos:
    ///   Unity.exe -batchmode -projectPath . -executeMethod EstanDentro.EditorMejoras.MejorasRevision.TodoBatch -salida &lt;carpeta&gt;
    /// </summary>
    public static class MejorasRevision
    {
        const float P3 = 7.2f;

        static string Salida()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-salida");
            string dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "mejoras_revision"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [MenuItem("Estan Dentro/Mejoras/Revisión completa (volcado + planta + capturas)")]
        public static void Todo()
        {
            string dir = Salida();
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
            Volcar(dir);
            Planta(dir, P3, "planta_piso3");
            Planta(dir, 0f, "planta_piso1");
            Capturas(dir);
            // lo que ve el jugador la mayor parte del capítulo: después del apagón, solo la luna y la linterna
            Estado(dir, "apagon", pesadilla: false);
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);   // sin guardar: vuelve como estaba
            Estado(dir, "pesadilla", pesadilla: true);
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
        }

        [MenuItem("Estan Dentro/Mejoras/Capturas de apagón y Pesadilla")]
        public static void SoloEstados()
        {
            string dir = Salida();
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
            Estado(dir, "apagon", pesadilla: false);
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
            Estado(dir, "pesadilla", pesadilla: true);
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
        }

        public static void SoloEstadosBatch()
        {
            try { SoloEstados(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Mejoras] " + e); EditorApplication.Exit(1); }
        }

        static readonly int Emisivo = Shader.PropertyToID("_EmissiveColor");

        /// <summary>
        /// Apaga todas las luces menos la luna y los tubos (como BlackoutEvent + ApagarEmisivos) y pone una linterna en la
        /// cámara con los valores de Flashlight.cs. Con pesadilla=true además cambia cada material por su versión de
        /// Materials_Pesadilla (como MundoPesadilla) y enfría la luna. Solo en memoria: el que llama vuelve a abrir la escena.
        /// </summary>
        static void Estado(string dir, string sufijo, bool pesadilla)
        {
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { if (pesadilla) l.color = new Color(0.45f, 0.75f, 0.78f); continue; }
                l.enabled = false;
            }
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var mats = r.sharedMaterials; bool cambio = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (!m) continue;
                    if (pesadilla)
                    {
                        var p = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Edificio/Materials_Pesadilla/{m.name}_pesadilla.mat");
                        if (p) { mats[i] = m = p; cambio = true; }
                    }
                    if (m.HasProperty(Emisivo) && m.GetColor(Emisivo).maxColorComponent > 0.001f)
                    { var c = new Material(m); c.SetColor(Emisivo, Color.black); mats[i] = c; cambio = true; }
                }
                if (cambio) r.sharedMaterials = mats;
            }
            var go = new GameObject("CamCaptura");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 3000f;
            go.AddComponent<HDAdditionalCameraData>();
            var lin = new GameObject("Linterna"); lin.transform.SetParent(go.transform, false); lin.transform.localPosition = new Vector3(0.18f, -0.15f, 0.1f);
            var luz = lin.AddComponent<Light>(); luz.type = LightType.Spot; luz.spotAngle = 50f; luz.range = 10f;
            luz.lightUnit = UnityEngine.Rendering.LightUnit.Lumen; luz.intensity = 2000f; luz.color = new Color(1f, 0.9f, 0.72f); luz.shadows = LightShadows.Soft;
            lin.AddComponent<HDAdditionalLightData>();
            foreach (var (n, pos, rumbo, inc) in Puntos)
            {
                if (n.Contains("patio")) continue;
                go.transform.position = pos; go.transform.rotation = Quaternion.Euler(-inc, rumbo, 0f);
                Render(cam, Path.Combine(dir, $"{n}_{sufijo}.png"), 1600, 900);
            }
            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TodoBatch()
        {
            try { Todo(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Mejoras] " + e); EditorApplication.Exit(1); }
        }

        static string V(Vector3 v) => $"({v.x:F1}, {v.y:F1}, {v.z:F1})";

        static void Volcar(string dir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Volcado del capítulo 1 (Colegio.unity)");
            foreach (var n in new[] { "Recuerdo_Pasillo4B", "Recuerdo_AulaMusica", "Recuerdo_Rectoria", "Player" })
            {
                var g = GameObject.Find(n);
                sb.AppendLine($"{n}: {(g ? V(g.transform.position) : "no está")}");
            }
            sb.AppendLine("\n## Puntos de aparición");
            foreach (var s in UnityEngine.Object.FindObjectsByType<Player.PlayerSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                sb.AppendLine($"{s.name}: {V(s.transform.position)}");
            sb.AppendLine("\n## Puertas con llave o candado (piso, nombre, llave, posición)");
            foreach (var d in UnityEngine.Object.FindObjectsByType<Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(d => d.transform.position.y).ThenBy(d => d.name))
            {
                var r = d.GetComponent<Renderer>(); Vector3 c = r ? r.bounds.center : d.transform.position;
                sb.AppendLine($"{Piso(c.y)} {d.name} llave='{d.llaveId}' locked={d.IsLocked} {V(c)}");
            }
            sb.AppendLine("\n## Escondites");
            foreach (var e in UnityEngine.Object.FindObjectsByType<Colegio.Escondite>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(e => e.transform.position.y))
                sb.AppendLine($"{Piso(e.transform.position.y)} {e.name} {V(e.transform.position)}");
            sb.AppendLine("\n## Bloqueos (barricadas)");
            foreach (var b in UnityEngine.Object.FindObjectsByType<Colegio.Bloqueo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                sb.AppendLine($"{Piso(b.transform.position.y)} {b.name} {V(b.transform.position)}");
            sb.AppendLine("\n## Mente");
            foreach (var o in UnityEngine.Object.FindObjectsByType<Mente.Observador>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.AppendLine($"Observador {V(o.transform.position)}");
            foreach (var o in UnityEngine.Object.FindObjectsByType<Mente.GatilloRecuerdo>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.AppendLine($"GatilloRecuerdo {o.name} resolver='{o.resolverCon}' {V(o.transform.position)}");
            foreach (var o in UnityEngine.Object.FindObjectsByType<Mente.GatilloZona>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.AppendLine($"GatilloZona {o.id} {V(o.transform.position)}");
            foreach (var o in UnityEngine.Object.FindObjectsByType<Mente.SoloEnPesadilla>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.AppendLine($"SoloEnPesadilla {o.name} {V(o.transform.position)}");
            sb.AppendLine("\n## Salas del piso 3 (objetos raíz de los FBX que nombran salas)");
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(t => (t.name.StartsWith("SALA_") || t.name.StartsWith("PISO_") || t.name.Contains("Aula") || t.name.Contains("Salon")) && t.GetComponent<Renderer>())
                         .Take(120))
            {
                var b = t.GetComponent<Renderer>().bounds;
                sb.AppendLine($"{Piso(b.center.y)} {t.name} min={V(b.min)} max={V(b.max)}");
            }
            File.WriteAllText(Path.Combine(dir, "volcado.txt"), sb.ToString(), Encoding.UTF8);
            Debug.Log("[Mejoras] volcado listo");
        }

        static string Piso(float y) => y < -0.5f ? "S " : y < 3.4f ? "P1" : y < 6.9f ? "P2" : y < 10.3f ? "P3" : "AZ";

        /// <summary>Planta desde arriba: cámara ortográfica bajo el techo del piso, con luz plana para que se lea.</summary>
        static void Planta(string dir, float piso, string nombre)
        {
            var go = new GameObject("CamPlanta");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 34f;
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 3.4f;                    // corta el techo y lo de abajo
            go.transform.position = new Vector3(25f, piso + 3.0f, 22f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.AddComponent<HDAdditionalCameraData>();
            var luzGo = new GameObject("LuzPlanta"); var luz = luzGo.AddComponent<Light>(); luz.type = LightType.Directional;
            luzGo.transform.rotation = Quaternion.Euler(70f, 30f, 0f); luzGo.AddComponent<HDAdditionalLightData>(); luz.intensity = 8f;
            Render(cam, Path.Combine(dir, nombre + ".png"), 1800, 1800);
            UnityEngine.Object.DestroyImmediate(luzGo);
            UnityEngine.Object.DestroyImmediate(go);
        }

        static readonly (string, Vector3, float, float)[] Puntos =
        {
            ("c01_4B_desde_pupitre", new Vector3(27.0f, P3 + 1.6f, 34.6f), 225f, 6f),
            ("c02_pasillo_P3_oeste", new Vector3(11.5f, P3 + 1.6f, 27.6f), 270f, 2f),
            ("c03_pasillo_P3_este", new Vector3(11.5f, P3 + 1.6f, 27.6f), 90f, 2f),
            ("c04_aula_musica", new Vector3(6.0f, P3 + 1.6f, 23.5f), 270f, 4f),
            ("c05_patio_jacaranda", new Vector3(36.5f, 1.65f, 14.0f), -70f, -8f),
        };

        static void Capturas(string dir)
        {
            var go = new GameObject("CamCaptura");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 3000f;
            go.AddComponent<HDAdditionalCameraData>();
            foreach (var (n, pos, rumbo, inc) in Puntos)
            {
                go.transform.position = pos; go.transform.rotation = Quaternion.Euler(-inc, rumbo, 0f);
                Render(cam, Path.Combine(dir, n + ".png"), 1600, 900);
            }
            UnityEngine.Object.DestroyImmediate(go);
        }

        static void Render(Camera cam, string archivo, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            for (int i = 0; i < 24; i++) cam.Render();                                 // exposición automática y volumétricos
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(archivo, tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("[Mejoras] " + Path.GetFileName(archivo));
        }
    }
}
