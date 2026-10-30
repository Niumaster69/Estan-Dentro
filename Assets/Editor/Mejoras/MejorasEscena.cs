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
            CuartoHospital();
        }

        // ------------------------------------------------------------------ cuarto del hospital (final del capítulo)
        const string HOSP = "Assets/Art/Hospital";
        const string PH = HOSP + "/PolyHaven";
        // el aula de música (piso 3, ala oeste): x 0,8–6,4 · z 18,9–28,1 (Recuerdo_AulaMusica)
        public static readonly Vector3 AulaCentro = new Vector3(3.6f, P3, 23.5f);

        /// <summary>
        /// El final (NARRATIVA_UNIFICADA §2.3): el aula de música se vuelve el cuarto del hospital. Pa sentado junto a la cama,
        /// de espaldas; Mateo dormido bajo la sábana; el monitor con su pulso; las flores que Pa trae cada cumpleaños y una
        /// foto de los dos. Muebles CC0 de Poly Haven (Art/Hospital/PolyHaven, ver CREDITOS.md), la cama vestida y el monitor
        /// hechos en Blender (Herramientas/blender/hospital_props.py), Pa sacado del carro del prólogo.
        /// Queda apagado: RecuerdoAula lo enciende en el final y apaga los muebles del aula.
        /// </summary>
        static void CuartoHospital()
        {
            // está apagado: GameObject.Find no lo ve, así que se buscan todas las copias entre las raíces de la escena
            foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name == "Mejoras_CuartoHospital"))
                UnityEngine.Object.DestroyImmediate(g);
            var raiz = new GameObject("Mejoras_CuartoHospital").transform;
            float yf = Piso(AulaCentro);
            raiz.position = new Vector3(AulaCentro.x, yf, AulaCentro.z);

            // la cama a lo largo del aula, cabecera contra la pared oeste
            var cama = new Vector3(2.0f, yf, 24.3f);
            Poner(raiz, PH + "/old_bed_frame/old_bed_frame_1k.fbx", cama, 90f, nombre: "Cama");
            var vestida = Poner(raiz, HOSP + "/Hospital_Props.fbx", cama + Vector3.up * 0.50f, 180f, nombre: "Cama_Vestida");
            Solo(vestida, "Colchon", "Sabana", "Almohada", "Cabeza", "Pelo");
            var monitor = Poner(raiz, HOSP + "/Hospital_Props.fbx", new Vector3(1.25f, yf, 25.3f), 150f, nombre: "Monitor");
            Solo(monitor, "Monitor_");
            var suero = Poner(raiz, HOSP + "/Hospital_Props.fbx", new Vector3(1.75f, yf, 25.05f), 200f, nombre: "Suero");
            Solo(suero, "Suero_");
            MaterialesHospital(raiz);

            // mesa de noche con las flores de Pa, la foto de los dos y una lámpara
            Poner(raiz, PH + "/ClassicNightstand_01/ClassicNightstand_01_1k.fbx", new Vector3(1.15f, yf, 23.2f), 90f, nombre: "MesaNoche");
            float mesa = yf + 0.70f;
            Poner(raiz, PH + "/ceramic_vase_01/ceramic_vase_01_1k.fbx", new Vector3(1.05f, mesa, 23.05f), 0f, 0.8f, "Jarron");
            Poner(raiz, PH + "/flower_gazania/flower_gazania_1k.fbx", new Vector3(1.05f, mesa + 0.27f, 23.05f), 35f, 0.6f, "Flores");
            Poner(raiz, PH + "/standing_picture_frame_01/standing_picture_frame_01_1k.fbx", new Vector3(1.22f, mesa, 23.38f), 125f, 1f, "Foto");
            var lampara = Poner(raiz, PH + "/desk_lamp_arm_01/desk_lamp_arm_01_1k.fbx", new Vector3(1.0f, mesa, 23.4f), 60f, 0.6f, "Lampara");

            // Pa en una silla plástica, junto a la cama, de espaldas a la puerta
            var silla = new Vector3(2.1f, yf, 22.75f);
            Poner(raiz, PH + "/plastic_monobloc_chair_01/plastic_monobloc_chair_01_1k.fbx", silla, -28f, nombre: "Silla_Pa");
            PonerPa(raiz, silla + new Vector3(0f, 0.47f, 0.02f), -28f);   // girado hacia la cabecera: mira a Mateo

            // lo que hace que sea un cuarto: silla de ruedas en la esquina y el reloj en la pared
            Poner(raiz, PH + "/wheelchair_01/wheelchair_01_1k.fbx", new Vector3(5.6f, yf, 27.3f), 205f, nombre: "SillaRuedas");
            Poner(raiz, PH + "/wall_clock/wall_clock_1k.fbx", new Vector3(3.2f, yf + 2.2f, 28.03f), 180f, nombre: "Reloj");

            MaterialesPolyHaven(raiz);

            // luz: la lámpara (cálida, la única de verdad), el brillo verde del monitor y un relleno frío muy bajo
            Luz(raiz, "Luz_Lampara", (lampara ? lampara.transform.position : new Vector3(1.0f, mesa, 23.4f)) + new Vector3(0.15f, 0.5f, 0f), new Color(1f, 0.74f, 0.48f), 240f, 4f, true);
            Luz(raiz, "Luz_Monitor", new Vector3(1.4f, yf + 1.2f, 25.05f), new Color(0.3f, 1f, 0.6f), 55f, 2.5f, false);
            Luz(raiz, "Luz_Relleno", new Vector3(3.6f, yf + 2.6f, 23.0f), new Color(0.5f, 0.6f, 0.85f), 35f, 7f, false);

            // desde dónde se ve: cerca de la puerta, mirando la cama por detrás de Pa
            var vista = new GameObject("Vista_Final").transform; vista.SetParent(raiz, false);
            vista.position = new Vector3(4.9f, yf, 20.9f);
            Vector3 mira = new Vector3(2.0f, yf + 0.9f, 24.0f) - (vista.position + Vector3.up * 1.6f);
            vista.rotation = Quaternion.LookRotation(new Vector3(mira.x, 0f, mira.z));

            raiz.gameObject.SetActive(false);
            Debug.Log($"[Mejoras] cuarto del hospital armado en el aula de música (piso en y {yf:F2}), apagado hasta el final");
        }

        static float Piso(Vector3 p)
        {
            // el golpe más cercano a la altura del piso (no la silla ni el atril que haya encima)
            var golpes = Physics.RaycastAll(new Vector3(p.x, p.y + 2.5f, p.z), Vector3.down, 4f).Where(h => !h.collider.isTrigger).ToList();
            return golpes.Count == 0 ? p.y : golpes.OrderBy(h => Mathf.Abs(h.point.y - p.y)).First().point.y;
        }

        static GameObject Poner(Transform padre, string ruta, Vector3 pos, float rotY, float escala = 1f, string nombre = null)
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (!modelo) { Debug.LogWarning("[Mejoras] falta " + ruta); return null; }
            var g = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            g.name = nombre ?? modelo.name;
            g.transform.SetParent(padre, true);
            // la rotación y la escala propias de la raíz del FBX se conservan (Poly Haven trae ×100 y −90° en X en las mallas sueltas)
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f) * modelo.transform.localRotation);
            g.transform.localScale = modelo.transform.localScale * escala;
            return g;
        }

        /// <summary>De una copia de Hospital_Props deja solo las piezas cuyo nombre empieza por alguno de los prefijos.</summary>
        static void Solo(GameObject g, params string[] prefijos)
        {
            if (!g) return;
            foreach (Transform t in g.transform)
                t.gameObject.SetActive(prefijos.Any(p => t.name.StartsWith(p)));
        }

        static void Luz(Transform padre, string nombre, Vector3 pos, Color color, float lumenes, float rango, bool sombras)
        {
            var g = new GameObject(nombre); g.transform.SetParent(padre, false); g.transform.position = pos;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.range = rango;
            l.lightUnit = UnityEngine.Rendering.LightUnit.Lumen; l.intensity = lumenes;
            l.shadows = sombras ? LightShadows.Soft : LightShadows.None;
            g.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
        }

        /// <summary>Pa sale del carro del prólogo: las piezas con su ropa y piel (Body/Top/Bottom/Shoes/Eyewear) y sus materiales opacos.</summary>
        static void PonerPa(Transform padre, Vector3 asiento, float rotY)
        {
            var auto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prologo/Prologo_Auto.fbx"));
            PrefabUtility.UnpackPrefabInstance(auto, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            auto.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var pa = new GameObject("Pa").transform;
            pa.position = new Vector3(-0.39f, 0.33f, 0.25f);           // la cadera de Pa en el asiento del conductor
            var piezas = auto.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterials.Any(m => m && System.Text.RegularExpressions.Regex.IsMatch(m.name, "^(Body|Top|Bottom|Shoes)mat"))).ToList();   // sin las gafas del carro
            foreach (var r in piezas)
            {
                r.transform.SetParent(pa, true);
                r.sharedMaterials = r.sharedMaterials.Select(m =>
                {
                    if (!m) return m;
                    var o = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Prologo/Materiales/Opaco_{m.name.Replace('.', '_')}.mat");
                    return o ? o : m;
                }).ToArray();
            }
            UnityEngine.Object.DestroyImmediate(auto);
            pa.SetParent(padre, true);
            pa.SetPositionAndRotation(asiento, Quaternion.Euler(0f, rotY, 0f));
            Debug.Log($"[Mejoras] Pa: {piezas.Count} piezas");
        }

        /// <summary>
        /// Los materiales que el importador de FBX hace para los modelos de Poly Haven salen invisibles en HDRP. Se rehacen
        /// como HDRP/Lit con sus texturas: color (diff), normal (nor_gl) y la suavidad y el metal promedio de sus mapas.
        /// </summary>
        static void MaterialesPolyHaven(Transform raiz)
        {
            var hechos = new System.Collections.Generic.Dictionary<string, Material>();
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                var origen = PrefabUtility.GetCorrespondingObjectFromSource(r.gameObject);
                string rutaModelo = origen ? AssetDatabase.GetAssetPath(origen) : null;
                if (rutaModelo == null || !rutaModelo.Contains("/PolyHaven/")) continue;
                string dirTex = Path.GetDirectoryName(rutaModelo).Replace('\\', '/') + "/textures";
                if (!AssetDatabase.IsValidFolder(dirTex)) continue;
                var difusas = AssetDatabase.FindAssets("_diff_ t:Texture2D", new[] { dirTex }).Select(AssetDatabase.GUIDToAssetPath).ToList();
                if (difusas.Count == 0) continue;
                r.sharedMaterials = r.sharedMaterials.Select(m =>
                {
                    string nombre = m ? m.name : "";
                    // la textura cuyo prefijo coincide con el material; si no, la primera de la carpeta
                    string dif = difusas.FirstOrDefault(d => Path.GetFileName(d).StartsWith(nombre, StringComparison.OrdinalIgnoreCase) && nombre.Length > 2) ?? difusas[0];
                    string prefijo = Path.GetFileName(dif); prefijo = prefijo.Substring(0, prefijo.IndexOf("_diff_", StringComparison.Ordinal));
                    if (hechos.TryGetValue(prefijo, out var hecho)) return hecho;
                    return hechos[prefijo] = LitPolyHaven(prefijo, dirTex, dif);
                }).ToArray();
            }
            Debug.Log($"[Mejoras] materiales Poly Haven: {hechos.Count}");
        }

        static Material LitPolyHaven(string prefijo, string dirTex, string rutaDif)
        {
            string Buscar(string tipo) => AssetDatabase.FindAssets($"{prefijo}_{tipo}_ t:Texture2D", new[] { dirTex }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
            var m = Mat("PH_" + prefijo, Color.white, 0.4f);
            m.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaDif));
            string nor = Buscar("nor_gl");
            if (nor != null)
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(nor);
                if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
                m.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(nor));
                m.SetFloat("_NormalScale", 1f);
            }
            float Promedio(string ruta) { if (ruta == null) return -1f; var px = LeerGPU(AssetDatabase.LoadAssetAtPath<Texture2D>(ruta), out _, out _); return px.Average(c => c.r); }
            float rugoso = Promedio(Buscar("rough")), metal = Promedio(Buscar("metal"));
            if (rugoso >= 0f) m.SetFloat("_Smoothness", Mathf.Clamp01(1f - rugoso) * 0.85f);
            if (metal >= 0f) m.SetFloat("_Metallic", metal > 0.5f ? 1f : 0f);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Mat(string nombre, Color color, float suave, float metal = 0f)
        {
            string dir = HOSP + "/Materiales";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(HOSP, "Materiales");
            string ruta = $"{dir}/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (!m) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, ruta); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", suave); m.SetFloat("_Metallic", metal);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void MaterialesHospital(Transform raiz)
        {
            var sabana = Mat("Hosp_Sabana", new Color(0.80f, 0.82f, 0.84f), 0.15f);
            var colchon = Mat("Hosp_Colchon", new Color(0.42f, 0.52f, 0.56f), 0.3f);
            var piel = Mat("Hosp_Piel", new Color(0.66f, 0.47f, 0.38f), 0.35f);
            var pelo = Mat("Hosp_Pelo", new Color(0.04f, 0.035f, 0.03f), 0.25f);
            var plastico = Mat("Hosp_Plastico", new Color(0.74f, 0.74f, 0.72f), 0.45f);
            var metal = Mat("Hosp_Metal", new Color(0.62f, 0.63f, 0.65f), 0.6f, 1f);
            var bolsa = Mat("Hosp_Bolsa", new Color(0.86f, 0.9f, 0.92f), 0.85f);
            var pantalla = Pantalla();
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                Material m = n.StartsWith("Colchon") ? colchon : n.StartsWith("Sabana") || n.StartsWith("Almohada") ? sabana
                    : n.StartsWith("Cabeza") ? piel : n.StartsWith("Pelo") ? pelo : n.StartsWith("Monitor_Cuerpo") ? plastico
                    : n.StartsWith("Monitor_Pantalla") ? pantalla : n.StartsWith("Suero_Bolsa") || n.StartsWith("Suero_Tubo") ? bolsa
                    : n.StartsWith("Monitor_") || n.StartsWith("Suero_") ? metal : null;
                if (!m) continue;
                r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray();
                if (n.StartsWith("Monitor_Pantalla") && !r.GetComponent<Colegio.MonitorECG>()) r.gameObject.AddComponent<Colegio.MonitorECG>();
            }
        }

        /// <summary>La pantalla del monitor: textura con 4 latidos (línea verde sobre negro) que MonitorECG hace correr.</summary>
        static Material Pantalla()
        {
            string rutaTex = HOSP + "/ecg.png";
            const int W = 1024, H = 256;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 0.025f, 0.015f, 1f);
            // rejilla tenue
            for (int x = 0; x < W; x += 32) for (int y = 0; y < H; y++) px[y * W + x] = new Color(0f, 0.07f, 0.04f, 1f);
            for (int y = 0; y < H; y += 32) for (int x = 0; x < W; x++) px[y * W + x] = new Color(0f, 0.07f, 0.04f, 1f);
            float Pulso(float f)   // f: 0..1 dentro de un latido → altura 0..1
            {
                float b = 0.42f;
                b += 0.06f * Mathf.Exp(-Mathf.Pow((f - 0.18f) / 0.035f, 2));                  // onda P
                b -= 0.08f * Mathf.Exp(-Mathf.Pow((f - 0.30f) / 0.008f, 2));                  // Q
                b += 0.48f * Mathf.Exp(-Mathf.Pow((f - 0.32f) / 0.010f, 2));                  // R
                b -= 0.16f * Mathf.Exp(-Mathf.Pow((f - 0.345f) / 0.010f, 2));                 // S
                b += 0.10f * Mathf.Exp(-Mathf.Pow((f - 0.55f) / 0.06f, 2));                   // onda T
                return b;
            }
            int yAntes = -1;
            for (int x = 0; x < W; x++)
            {
                float f = (x * 4f / W) % 1f;
                int y = Mathf.RoundToInt(Pulso(f) * (H - 1));
                int y0 = yAntes < 0 ? y : Mathf.Min(y, yAntes), y1 = yAntes < 0 ? y : Mathf.Max(y, yAntes);
                for (int yy = y0 - 2; yy <= y1 + 2; yy++)
                {
                    if (yy < 0 || yy >= H) continue;
                    float borde = (yy < y0 || yy > y1) ? 0.45f : 1f;
                    px[yy * W + x] = Color.Lerp(px[yy * W + x], new Color(0.35f, 1f, 0.6f, 1f), borde);
                }
                yAntes = y;
            }
            t.SetPixels(px); t.Apply();
            File.WriteAllBytes(rutaTex, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(rutaTex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
            imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = false; imp.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);

            string ruta = HOSP + "/Materiales/Hosp_Pantalla.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (!m) { m = new Material(Shader.Find("HDRP/Unlit")); AssetDatabase.CreateAsset(m, ruta); }
            m.SetTexture("_UnlitColorMap", tex);
            m.SetColor("_UnlitColor", new Color(1.6f, 1.6f, 1.6f, 1f));                     // un poco más que blanco: se ve encendida
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);
            return m;
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
