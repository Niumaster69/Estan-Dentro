using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using EstanDentro.Prologo;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Arma la escena del PRÓLOGO a partir de lo exportado por 02_Arte3D_Blender/scripts_blender/prologo_lib.py
    /// (Assets/Art/Prologo: carro por dentro con Pa y las cosas de Mateo, y la calle). Menú: Estan Dentro → 7. Prólogo.
    /// </summary>
    public static class ConstruirPrologo
    {
        public const string ESCENA = "Assets/Scenes/Prologo/Prologo.unity";
        const string ART = "Assets/Art/Prologo";

        [Serializable] class Mat { public string nombre; public float[] color; public float rugosidad, metalico; public float[] emision; public bool vidrio; }
        [Serializable] class Lista { public Mat[] items; }

        [MenuItem("Estan Dentro/7. Armar el prólogo (carro de Pa)")]
        public static void Armar()
        {
            Directory.CreateDirectory("Assets/Scenes/Prologo");
            Directory.CreateDirectory(ART + "/Materiales");
            // el Corolla, Pa y Mateo (Sketchfab, CC-BY) traen sus texturas en Art/Prologo/Texturas: máximo 2048 para no inflar el juego
            AssetDatabase.Refresh();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { ART + "/Texturas" }))
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) is TextureImporter t)
                {
                    bool cambio = false;
                    if (t.maxTextureSize > 2048) { t.maxTextureSize = 2048; cambio = true; }
                    // las texturas de relieve (…_normal) se leían como color: manchas en la ropa
                    if (t.assetPath.ToLower().Contains("normal") && t.textureType != TextureImporterType.NormalMap) { t.textureType = TextureImporterType.NormalMap; cambio = true; }
                    if (cambio) t.SaveAndReimport();
                }
            if (AssetImporter.GetAtPath(ART + "/Prologo_Auto.fbx") is ModelImporter mi && mi.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            { mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; mi.SaveAndReimport(); }
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // materiales HDRP desde el json de Blender
            var lit = Shader.Find("HDRP/Lit");
            var lista = JsonUtility.FromJson<Lista>(File.ReadAllText(ART + "/prologo_materiales.json"));
            var mats = lista.items.ToDictionary(m => m.nombre, m =>
            {
                string ruta = $"{ART}/Materiales/{m.nombre}.mat";
                var mt = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (!mt) { mt = new Material(lit); AssetDatabase.CreateAsset(mt, ruta); }
                mt.shader = lit;
                var c = new Color(m.color[0], m.color[1], m.color[2]);
                // la calle salió sin textura y casi blanca: asfalto mojado oscuro, andén gris, líneas blancas
                if (m.nombre.Contains("asfalto")) c = new Color(0.045f, 0.047f, 0.05f);
                else if (m.nombre.Contains("concreto")) c = new Color(0.30f, 0.30f, 0.29f);
                else if (m.nombre.Contains("ladrillo")) c = new Color(0.36f, 0.17f, 0.11f);
                if (m.vidrio)
                {
                    mt.SetFloat("_SurfaceType", 1f);
                    mt.SetColor("_BaseColor", m.nombre.Contains("oscuro") ? new Color(0.05f, 0.06f, 0.07f, 1f) : new Color(0.75f, 0.8f, 0.82f, 0.12f));
                    mt.SetFloat("_Smoothness", 0.96f);
                }
                else
                {
                    mt.SetColor("_BaseColor", c); mt.SetFloat("_Metallic", m.metalico); mt.SetFloat("_Smoothness", m.nombre.Contains("asfalto") ? 0.45f : Mathf.Clamp01(1f - m.rugosidad));   // asfalto mojado: brilla
                    if (m.emision != null && m.emision.Length == 3 && m.emision.Sum() > 0)
                    { mt.SetColor("_EmissiveColor", new Color(m.emision[0], m.emision[1], m.emision[2]) * 30f); mt.SetFloat("_UseEmissiveIntensity", 0f); }
                }
                HDMaterial.ValidateMaterial(mt); EditorUtility.SetDirty(mt);
                return mt;
            });

            GameObject Poner(string fbx, string nombre)
            {
                var pref = AssetDatabase.LoadAssetAtPath<GameObject>($"{ART}/{fbx}.fbx");
                var g = (GameObject)PrefabUtility.InstantiatePrefab(pref); g.name = nombre;
                PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                    r.sharedMaterials = r.sharedMaterials.Select(x => x && mats.TryGetValue(x.name, out var nm) ? nm : x).ToArray();
                return g;
            }
            var auto = Poner("Prologo_Auto", "Carro_de_Pa");
            var calle1 = Poner("Prologo_Calle", "Calle_A");
            var calle2 = Poner("Prologo_Calle", "Calle_B");
            Transform Hijo(string n) => auto.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith(n));
            Bounds B(Transform t) { var rs = t.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

            // marcas puestas en Blender: ojos de Mateo, un punto al frente y el pie del parabrisas (limpiabrisas)
            Vector3 M(string n) => Hijo(n).position;
            Vector3 ojo = M("Marca_Ojos");
            Vector3 frenteTmp = M("Marca_Frente") - ojo; frenteTmp.y = 0f; ojo += frenteTmp.normalized * 0.07f;   // un poco adelante: no se ve el cuello por dentro
            Vector3 fwd = M("Marca_Frente") - ojo; fwd.y = 0f;
            fwd = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z) ? new Vector3(Mathf.Sign(fwd.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(fwd.z));   // el carro va alineado con los ejes
            Vector3 der = Vector3.Cross(Vector3.up, fwd);
            foreach (var t in auto.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Marca_")))
                if (t.GetComponent<Renderer>()) t.GetComponent<Renderer>().enabled = false;
            const float LARGO = 650f;
            calle2.transform.position = calle1.transform.position + fwd * LARGO;

            // vidrios del Corolla (Material.004 = ventanas, klosz = faros): transparentes de verdad
            string rutaVidrio = ART + "/Materiales/Vidrio_Carro.mat";
            var vidrio = AssetDatabase.LoadAssetAtPath<Material>(rutaVidrio);
            if (!vidrio) { vidrio = new Material(lit); AssetDatabase.CreateAsset(vidrio, rutaVidrio); }
            vidrio.SetFloat("_SurfaceType", 1f); vidrio.SetColor("_BaseColor", new Color(0.62f, 0.68f, 0.7f, 0.1f));
            vidrio.SetFloat("_Smoothness", 0.97f); vidrio.SetFloat("_Metallic", 0f);
            HDMaterial.ValidateMaterial(vidrio);
            foreach (var r in auto.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(x => x && (x.name.StartsWith("Material.004") || x.name.StartsWith("klosz")) ? vidrio : x).ToArray();

            // la ropa de Pa y Mateo llegó como transparente (texturas con canal alfa) y no se veía: copias opacas
            var opacas = new System.Collections.Generic.Dictionary<Material, Material>();
            foreach (var r in auto.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(x =>
                {
                    // materiales que vienen dentro del FBX (Corolla, Pa, Mateo): copia editable, opaca y de dos caras
                    // (las piernas de Mateo traen las caras al revés y no se veían)
                    if (!x || x == vidrio || !AssetDatabase.GetAssetPath(x).EndsWith(".fbx")) return x;
                    if (opacas.TryGetValue(x, out var o)) return o;
                    string ruta = $"{ART}/Materiales/Opaco_{x.name.Replace('.', '_')}.mat";
                    var copia = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (!copia) { copia = new Material(x); AssetDatabase.CreateAsset(copia, ruta); } else copia.CopyPropertiesFromMaterial(x);
                    bool lente = x.name.Contains("Eyelash") || x.name.Contains("Eyewear");
                    if (!lente) { copia.SetFloat("_SurfaceType", 0f); var col = copia.GetColor("_BaseColor"); col.a = 1f; copia.SetColor("_BaseColor", col); }
                    copia.SetFloat("_DoubleSidedEnable", 1f); copia.doubleSidedGI = true;
                    if (!lente) { copia.SetFloat("_AlphaCutoffEnable", 0f); copia.DisableKeyword("_ALPHATEST_ON"); }     // la textura de Mateo recortaba las piernas
                    // ropa y piel de Pa y Mateo: sin brillo de plástico (venían con brillo especular alto)
                    string nm = x.name;
                    if (nm.StartsWith("Ch31") || nm.StartsWith("Bodymat") || nm.StartsWith("Topmat") || nm.StartsWith("Bottommat") || nm.StartsWith("Shoesmat"))
                    {
                        copia.SetFloat("_MaterialID", 1f); copia.SetFloat("_Metallic", 0f);
                        copia.SetTexture("_MaskMap", null); copia.SetTexture("_SpecularColorMap", null);   // brillo especular de Sketchfab mal convertido
                        copia.DisableKeyword("_MASKMAP"); copia.DisableKeyword("_SPECULARCOLORMAP");
                        copia.SetFloat("_Smoothness", nm.StartsWith("Shoes") ? 0.35f : 0.18f);
                        copia.SetFloat("_SmoothnessRemapMax", nm.StartsWith("Shoes") ? 0.35f : 0.18f);
                    }
                    HDMaterial.ValidateMaterial(copia); EditorUtility.SetDirty(copia);
                    return opacas[x] = copia;
                }).ToArray();
            Debug.Log($"[Prologo] materiales del FBX copiados (opacos y de dos caras): {opacas.Count}");

            var camGO = new GameObject("Ojos_Mateo", typeof(Camera), typeof(AudioListener));
            camGO.transform.SetPositionAndRotation(ojo, Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(6f, 0f, 0f));
            var cam = camGO.GetComponent<Camera>(); cam.nearClipPlane = 0.03f; cam.fieldOfView = 68f; cam.tag = "MainCamera";
            camGO.AddComponent<HDAdditionalCameraData>();
            var pivote = new GameObject("Asiento_Mateo"); pivote.transform.SetPositionAndRotation(ojo, camGO.transform.rotation);
            camGO.transform.SetParent(pivote.transform, true);

            // colisionadores en lo que se toca (la radio es una caja invisible sobre la pantalla del Corolla)
            GameObject Tocable(string n) { var t = Hijo(n).gameObject; if (!t.GetComponent<Collider>()) t.AddComponent<BoxCollider>(); return t; }
            var cuaderno = Tocable("Prologo_Cuaderno"); var lonchera = Tocable("PROP_lonchera");
            var radio = Tocable("Prologo_Radio"); var celular = Tocable("Prologo_Celular");
            if (radio.GetComponent<Renderer>()) radio.GetComponent<Renderer>().enabled = false;
            // la lonchera llegó blanca (su material no estaba en la lista): plástico rojo, la que Pa le arregló con candadito
            string rutaLon = ART + "/Materiales/Lonchera_Roja.mat";
            var matLon = AssetDatabase.LoadAssetAtPath<Material>(rutaLon);
            if (!matLon) { matLon = new Material(lit); AssetDatabase.CreateAsset(matLon, rutaLon); }
            matLon.SetColor("_BaseColor", new Color(0.55f, 0.09f, 0.07f)); matLon.SetFloat("_Smoothness", 0.55f); HDMaterial.ValidateMaterial(matLon);
            foreach (var r in lonchera.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterials.Any(m => m && m.GetColor("_BaseColor").grayscale > 0.7f))
                    r.sharedMaterials = r.sharedMaterials.Select(m => m && m.GetColor("_BaseColor").grayscale > 0.7f ? matLon : m).ToArray();

            // limpiabrisas: dos plumillas al pie del parabrisas
            Vector3 normal = (fwd * Mathf.Cos(55f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(55f * Mathf.Deg2Rad)).normalized;
            var negroMat = mats.Values.FirstOrDefault(m => m.name.Contains("plastico_negro"));
            var plumas = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var piv = new GameObject("Limpiabrisas_" + i).transform;
                piv.SetPositionAndRotation(M("Marca_Limpia_" + i), Quaternion.LookRotation(normal, Vector3.Cross(normal, der)));
                piv.SetParent(auto.transform, true);
                var hoja = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(hoja.GetComponent<Collider>());
                hoja.name = "Plumilla"; hoja.transform.SetParent(piv, false);
                hoja.transform.localScale = new Vector3(0.55f, 0.016f, 0.012f); hoja.transform.localPosition = new Vector3(0.27f, 0f, 0f);
                if (negroMat) hoja.GetComponent<Renderer>().sharedMaterial = negroMat;
                plumas[i] = piv;
            }

            // el camión del presagio y un carro de tráfico, armados con piezas (cabina, carga, ruedas, faros encendidos)
            Material Pintura(string n, Color c, float brillo = 0.5f)
            {
                string ruta = $"{ART}/Materiales/{n}.mat"; var mt = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (!mt) { mt = new Material(lit); AssetDatabase.CreateAsset(mt, ruta); }
                mt.SetColor("_BaseColor", c); mt.SetFloat("_Smoothness", brillo); HDMaterial.ValidateMaterial(mt); return mt;
            }
            Material Luz(string n, Color c)
            {
                var mt = Pintura(n, c, 0.9f); mt.SetColor("_EmissiveColor", c * 60f); mt.SetFloat("_UseEmissiveIntensity", 0f); HDMaterial.ValidateMaterial(mt); return mt;
            }
            GameObject Pieza(Transform padre, PrimitiveType t, Vector3 pos, Vector3 esc, Material m, Vector3 rot = default)
            {
                var g = GameObject.CreatePrimitive(t); UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                g.transform.SetParent(padre, false); g.transform.localPosition = pos; g.transform.localScale = esc; g.transform.localEulerAngles = rot;
                g.GetComponent<Renderer>().sharedMaterial = m; return g;
            }
            Transform Vehiculo(string nombre, float largo, float alto, float ancho, Material pintura, bool carga)
            {
                var v = new GameObject(nombre).transform;
                var llanta = Pintura("Llanta", new Color(0.03f, 0.03f, 0.03f), 0.2f);
                var vidrioOsc = Pintura("Vidrio_Vehiculo", new Color(0.05f, 0.07f, 0.09f), 0.95f);
                var faro = Luz("Faro_Encendido", new Color(1f, 0.95f, 0.85f));
                if (carga)
                {
                    Pieza(v, PrimitiveType.Cube, new Vector3(0, 1.0f + alto * 0.5f, -1.2f), new Vector3(ancho, alto, largo - 2.6f), Pintura("Carga_Camion", new Color(0.55f, 0.55f, 0.52f), 0.3f));
                    Pieza(v, PrimitiveType.Cube, new Vector3(0, 1.55f, largo * 0.5f - 1.2f), new Vector3(ancho, 2.1f, 2.2f), pintura);
                    Pieza(v, PrimitiveType.Cube, new Vector3(0, 2.0f, largo * 0.5f - 0.08f), new Vector3(ancho - 0.2f, 0.9f, 0.05f), vidrioOsc);
                }
                else
                {
                    Pieza(v, PrimitiveType.Cube, new Vector3(0, 0.65f, 0), new Vector3(ancho, 0.6f, largo), pintura);
                    Pieza(v, PrimitiveType.Cube, new Vector3(0, 1.15f, -0.2f), new Vector3(ancho - 0.15f, 0.5f, largo * 0.5f), vidrioOsc);
                }
                foreach (float sx in new[] { -1f, 1f })
                {
                    foreach (float sz in new[] { -0.33f, 0.33f })
                        Pieza(v, PrimitiveType.Cylinder, new Vector3(sx * ancho * 0.45f, 0.42f, sz * largo), new Vector3(0.84f, 0.12f, 0.84f), llanta, new Vector3(0, 0, 90));
                    Pieza(v, PrimitiveType.Cube, new Vector3(sx * ancho * 0.35f, carga ? 0.9f : 0.75f, largo * 0.5f + 0.01f), new Vector3(0.28f, 0.14f, 0.04f), faro);
                }
                return v;
            }
            var camion = Vehiculo("Camion", 9f, 2.8f, 2.5f, Pintura("Cabina_Camion", new Color(0.55f, 0.08f, 0.06f), 0.6f), true).gameObject;
            var trafico = Vehiculo("Carro_Trafico", 4.3f, 1.4f, 1.8f, Pintura("Pintura_Trafico", new Color(0.12f, 0.16f, 0.22f), 0.7f), false);
            trafico.gameObject.SetActive(false);
            var faros = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var f = new GameObject("Faro_" + i); f.transform.SetParent(camion.transform, false);
                f.transform.localPosition = new Vector3(i == 0 ? -0.9f : 0.9f, 0.9f, 4.6f);
                var l = f.AddComponent<Light>(); l.type = LightType.Spot; l.spotAngle = 70f; l.range = 90f; l.color = new Color(1f, 0.95f, 0.85f);
                l.lightUnit = LightUnit.Lumen; l.intensity = 2000f; l.shadows = LightShadows.None;
                f.AddComponent<HDAdditionalLightData>();
                faros[i] = l;
            }

            // lluvia: partículas estiradas alrededor del carro (el carro está quieto; la calle se mueve)
            var lluvia = new GameObject("Lluvia").AddComponent<ParticleSystem>();
            lluvia.transform.position = ojo + Vector3.up * 9f + fwd * 6f;
            lluvia.transform.rotation = Quaternion.LookRotation(Vector3.down);
            var mn = lluvia.main; mn.startLifetime = 1.4f; mn.startSpeed = 11f; mn.startSize = 0.012f; mn.maxParticles = 6000;
            mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.startColor = new Color(0.75f, 0.8f, 0.85f, 0.35f);
            var em = lluvia.emission; em.rateOverTime = 3500f;
            var sh = lluvia.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30f, 30f, 0.1f);
            var vel = lluvia.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-fwd.x * 11f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(-fwd.z * 11f);
            var pr = lluvia.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 6f;
            string rutaGota = ART + "/Materiales/Gota.mat";
            var gota = AssetDatabase.LoadAssetAtPath<Material>(rutaGota);
            if (!gota) { gota = new Material(Shader.Find("HDRP/Unlit")); AssetDatabase.CreateAsset(gota, rutaGota); }
            gota.SetFloat("_SurfaceType", 1f); gota.SetColor("_UnlitColor", new Color(0.7f, 0.75f, 0.8f, 0.25f));
            HDMaterial.ValidateMaterial(gota); pr.sharedMaterial = gota;

            // cielo nublado de las 6:50 a. m. con lluvia
            string hdr = "Assets/Art/Edificio/Cielos/kloppenheim_06_puresky_4k.hdr";
            if (AssetImporter.GetAtPath(hdr) is TextureImporter ti && ti.textureShape != TextureImporterShape.TextureCube)
            { ti.textureShape = TextureImporterShape.TextureCube; ti.SaveAndReimport(); }
            string rutaPerfil = "Assets/Scenes/Prologo/Prologo_Volumen.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(rutaPerfil);
            if (!perfil) { perfil = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(perfil, rutaPerfil); }
            T C<T>() where T : VolumeComponent => perfil.Has<T>() ? perfil.components.OfType<T>().First() : perfil.Add<T>(true);
            C<VisualEnvironment>().skyType.Override((int)SkyType.HDRI);
            var sky = C<HDRISky>(); sky.hdriSky.Override(AssetDatabase.LoadAssetAtPath<Cubemap>(hdr));
            sky.skyIntensityMode.Override(SkyIntensityMode.Lux); sky.desiredLuxValue.Override(PROLOGO.cieloLux);
            var exp = C<Exposure>(); exp.mode.Override(ExposureMode.Fixed); exp.fixedExposure.Override(PROLOGO.ev100);
            var fog = C<Fog>(); fog.enabled.Override(true); fog.meanFreePath.Override(PROLOGO.nieblaMetros); fog.enableVolumetricFog.Override(false);   // la volumétrica llenaba el carro y dejaba el parabrisas blanco
            fog.maximumHeight.Override(30f); fog.baseHeight.Override(0f);
            var mb = C<MotionBlur>(); mb.intensity.Override(0f);                                     // girar la cabeza no debe embarrar la imagen
            fog.albedo.Override(new Color(0.7f, 0.74f, 0.78f));
            C<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var ca = C<ColorAdjustments>(); ca.saturation.Override(-25f); ca.contrast.Override(-8f); ca.colorFilter.Override(new Color(0.9f, 0.95f, 1f));
            var vi = C<Vignette>(); vi.intensity.Override(0.3f);
            var gr = C<FilmGrain>(); gr.type.Override(FilmGrainLookup.Medium1); gr.intensity.Override(0.22f);
            var ao = C<ScreenSpaceAmbientOcclusion>(); ao.intensity.Override(1f);
            EditorUtility.SetDirty(perfil);
            var vol = new GameObject("Volumen_Prologo").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = perfil;
            var sol = new GameObject("Luz_Nublada"); sol.transform.rotation = Quaternion.Euler(55f, 160f, 0f);
            var ls = sol.AddComponent<Light>(); ls.type = LightType.Directional; ls.color = new Color(0.8f, 0.86f, 1f);
            ls.lightUnit = LightUnit.Lux; ls.intensity = PROLOGO.solLux; ls.shadows = LightShadows.Soft;
            var hd = sol.AddComponent<HDAdditionalLightData>(); hd.angularDiameter = 40f;          // cielo cubierto: sombras muy suaves

            // control
            var ctl = new GameObject("_Prologo").AddComponent<ControlPrologo>();
            ctl.ojos = camGO.transform; ctl.calles = new[] { calle1.transform, calle2.transform }; ctl.largoCalle = LARGO; ctl.adelante = fwd;
            ctl.limpiabrisas = plumas; ctl.lucesCamion = faros; ctl.camion = camion.transform; ctl.carroTrafico = trafico;
            ctl.cuaderno = cuaderno; ctl.lonchera = lonchera; ctl.radio = radio; ctl.celular = celular;

            EditorSceneManager.SaveScene(escena, ESCENA);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Prologo] escena armada: {ESCENA} | ojos en {ojo} | adelante {fwd}");
        }

        /// <summary>Luz de la mañana nublada (fácil de ajustar aquí).</summary>
        public static class PROLOGO
        {
            public const float cieloLux = 1800f, solLux = 700f, ev100 = 13.0f, nieblaMetros = 450f;
        }

        public static void ArmarBatch()
        {
            try { Armar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Prologo] " + e); EditorApplication.Exit(1); }
        }
    }
}
