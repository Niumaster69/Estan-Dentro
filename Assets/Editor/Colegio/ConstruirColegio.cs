using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using EstanDentro.Colegio;
using EstanDentro.Interaction;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Arma el colegio en Unity a partir de lo exportado por Blender (02_Arte3D_Blender/scripts_blender/exportar_unity.py).
    /// Menú: Estan Dentro / ...   (también se puede correr en batch con -executeMethod).
    /// </summary>
    public static class ConstruirColegio
    {
        const string RAIZ = "Assets/Art/Edificio";
        const string MATS = RAIZ + "/Materials";          // "Local" para SearchAndRemapMaterials
        const string ESCENA = "Assets/Scenes/Colegio/Colegio.unity";
        const string ESCENA_SISTEMAS = "Assets/Scenes/Level-1/EscenaUno-salonPrincipal.unity";

        [Serializable] class MatItem
        {
            public string nombre, tipo, textura, color_tex, normal_tex, rug_tex, textura_pesadilla;
            public float[] tinte, emision, tinte_pesadilla;
            public float rugosidad_mult, rugosidad, metalico, emision_fuerza;
        }
        [Serializable] class MatLista { public List<MatItem> items; }

        // ------------------------------------------------------------------ todo junto
        [MenuItem("Estan Dentro/0. Construir TODO el colegio")]
        public static void Todo()
        {
            Materiales();
            RemapearFBX();
            Escena();
        }

        // ------------------------------------------------------------------ 1. materiales HDRP
        [MenuItem("Estan Dentro/1. Materiales HDRP")]
        public static void Materiales()
        {
            Directory.CreateDirectory(MATS);
            var lista = JsonUtility.FromJson<MatLista>(File.ReadAllText(RAIZ + "/materiales.json"));
            var lit = Shader.Find("HDRP/Lit");
            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var it in lista.items)
                    if (it.tipo == "pbr") PrepararTexturas(it);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();

            foreach (var it in lista.items)
            {
                string ruta = $"{MATS}/{it.nombre}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, ruta); }
                m.shader = lit;
                Color tinte = Col(it.tinte);
                if (it.tipo == "pbr")
                {
                    m.SetTexture("_BaseColorMap", Tex(it.color_tex));
                    m.SetColor("_BaseColor", tinte);
                    m.SetTexture("_NormalMap", Tex(it.normal_tex));
                    m.SetFloat("_NormalScale", 1f);
                    m.SetTexture("_MaskMap", Tex(RutaMascara(it)));
                    m.SetFloat("_Metallic", 0f);
                    m.SetFloat("_SmoothnessRemapMin", 0f);
                    m.SetFloat("_SmoothnessRemapMax", 1f);
                }
                else if (it.tipo == "vidrio")
                {
                    m.SetFloat("_SurfaceType", 1f);
                    m.SetColor("_BaseColor", new Color(0.85f, 0.9f, 0.88f, 0.18f));
                    m.SetFloat("_Smoothness", 0.95f);
                }
                else
                {
                    m.SetColor("_BaseColor", tinte);
                    m.SetFloat("_Metallic", it.metalico);
                    m.SetFloat("_Smoothness", 1f - it.rugosidad);
                    if (it.emision_fuerza > 0)
                    {
                        m.SetColor("_EmissiveColor", Col(it.emision) * it.emision_fuerza * 40f);
                        m.SetFloat("_UseEmissiveIntensity", 0f);
                    }
                }
                HDMaterial.ValidateMaterial(m);
                EditorUtility.SetDirty(m);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Colegio] {n} materiales HDRP listos en {MATS}");
        }

        static Color Col(float[] c) => c != null && c.Length >= 3 ? new Color(c[0], c[1], c[2], 1f) : Color.white;
        static Texture2D Tex(string rel) => string.IsNullOrEmpty(rel) ? null :
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{RAIZ}/Texturas/{rel.Replace('\\', '/')}");
        static string RutaMascara(MatItem it) => $"{it.textura}/{Path.GetFileNameWithoutExtension(it.rug_tex)}_mask.png";

        /// <summary>Normal map como Normal; rugosidad lineal; y la máscara HDRP (R metal, G AO, B detalle, A suavidad).</summary>
        static void PrepararTexturas(MatItem it)
        {
            string nor = $"{RAIZ}/Texturas/{it.normal_tex}";
            if (AssetImporter.GetAtPath(nor) is TextureImporter tn && tn.textureType != TextureImporterType.NormalMap)
            { tn.textureType = TextureImporterType.NormalMap; tn.SaveAndReimport(); }
            string rug = $"{RAIZ}/Texturas/{it.rug_tex}";
            string mask = $"{RAIZ}/Texturas/{RutaMascara(it)}";
            if (File.Exists(mask)) return;
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(rug));
            int w = Mathf.Min(1024, src.width), h = Mathf.Min(1024, src.height);
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float r = src.GetPixelBilinear((x + 0.5f) / w, (y + 0.5f) / h).r * it.rugosidad_mult;
                    px[y * w + x] = new Color32(0, 255, 0, (byte)(Mathf.Clamp01(1f - r) * 255));
                }
            dst.SetPixels32(px);
            File.WriteAllBytes(mask, dst.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(src); UnityEngine.Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(mask);
            if (AssetImporter.GetAtPath(mask) is TextureImporter tm) { tm.sRGBTexture = false; tm.SaveAndReimport(); }
        }

        // ------------------------------------------------------------------ 2. FBX usan los materiales HDRP
        [MenuItem("Estan Dentro/2. Asignar materiales a los FBX")]
        public static void RemapearFBX()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { RAIZ }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var mi = (ModelImporter)AssetImporter.GetAtPath(p);
                mi.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
                mi.SaveAndReimport();
            }
            Debug.Log("[Colegio] FBX remapeados a los materiales HDRP");
        }

        // ------------------------------------------------------------------ 3. escena jugable
        [MenuItem("Estan Dentro/3. Construir escena del colegio")]
        public static void Escena()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ESCENA));
            var esc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var raiz = new GameObject("COLEGIO");
            int puertas = 0, inter = 0, luces = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { RAIZ }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
                string nombre = Path.GetFileNameWithoutExtension(p);
                bool fijo = nombre.StartsWith("Edificio_") || nombre == "Entorno" || nombre == "Barrio" || nombre == "Fondo_lejano";
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    // ANTIFUGAS: la estructura no tiene caras ocultas (se quitaron en Blender), así que proyecta
                    // sombra por las dos caras para que el sol no se cuele por uniones muro-losa.
                    if (nombre.StartsWith("Edificio_") || nombre.StartsWith("Puertas_"))
                        mr.shadowCastingMode = ShadowCastingMode.TwoSided;
                    if (fijo || nombre.StartsWith("Muebles_"))
                        GameObjectUtility.SetStaticEditorFlags(mr.gameObject, fijo ? (StaticEditorFlags)~0 :
                            StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    if (nombre == "Fondo_lejano" || nombre == "Jacaranda") continue;
                    if (mr.GetComponent<Collider>() || nombre.StartsWith("Puertas_")) continue;
                    // asientos: caja sólida hasta el espaldar; con malla, sus barras servían de escalón y el jugador se subía
                    var db = mr.GetComponent<DatosBlender>();
                    var mf = mr.GetComponent<MeshFilter>();
                    if (db && mf && mf.sharedMesh && EsAsiento(db.Get("prop_tipo")))
                    {
                        var bc = mr.gameObject.AddComponent<BoxCollider>();
                        // hasta 0,68 m: imposible subirse (el paso del jugador es 0,3 m) y lo que está sobre la tabla (0,70 m) sigue alcanzable
                        // (el eje "arriba" de la malla puede ser Y o Z según cómo llegó de Blender: se busca cuál apunta al cielo)
                        var b = mf.sharedMesh.bounds;
                        Vector3 upL = mr.transform.InverseTransformDirection(Vector3.up);
                        int eje = Mathf.Abs(upL.x) > Mathf.Abs(upL.y) ? (Mathf.Abs(upL.x) > Mathf.Abs(upL.z) ? 0 : 2) : (Mathf.Abs(upL.y) > Mathf.Abs(upL.z) ? 1 : 2);
                        float escala = Mathf.Abs(mr.transform.lossyScale[eje]) > 1e-5f ? Mathf.Abs(mr.transform.lossyScale[eje]) : 1f;
                        Vector3 min = b.min, max = b.max;
                        if (upL[eje] >= 0f) max[eje] = Mathf.Min(max[eje], min[eje] + 0.68f / escala);
                        else min[eje] = Mathf.Max(min[eje], max[eje] - 0.68f / escala);
                        bc.center = (min + max) * 0.5f; bc.size = max - min;
                    }
                    else mr.gameObject.AddComponent<MeshCollider>();
                }
                if (nombre.StartsWith("Puertas_")) puertas += ConfigurarPuertas(go);
                if (nombre.StartsWith("Muebles_") || nombre == "Entorno") inter += ConfigurarObjetos(go);
                if (nombre == "Luces") { luces = CrearLuces(go); }
            }
            CielosYVolumen();
            CopiarSistemasYJugador();
            PortarCapitulo1.Portar();
            var inicio = GameObject.Find("SpawnPoint_inicio_salon");
            var jug = UnityEngine.Object.FindAnyObjectByType<Player.PlayerController>();
            if (inicio && jug) jug.transform.SetPositionAndRotation(inicio.transform.position, inicio.transform.rotation);

            EditorSceneManager.SaveScene(esc, ESCENA);
            PortarCapitulo1.RedirigirDuctos();
            var lista = EditorBuildSettings.scenes.ToList();
            foreach (var ruta in new[] { ESCENA, PortarCapitulo1.ESCENA_DUCTOS })
                if (!lista.Any(s => s.path == ruta)) lista.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = lista.ToArray();
            Debug.Log($"[Colegio] Escena lista: {ESCENA} | puertas {puertas} | interactuables {inter} | luces {luces}");
        }

        static int ConfigurarPuertas(GameObject go)
        {
            int n = 0;
            foreach (var d in go.GetComponentsInChildren<DatosBlender>(true))
            {
                if (d.Get("tipo") != "puerta") continue;
                var t = d.transform;
                var mf = t.GetComponent<MeshFilter>();
                var bc = t.gameObject.AddComponent<BoxCollider>();
                if (mf && mf.sharedMesh) { bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size; }
                var puerta = t.gameObject.AddComponent<LockedDoor>();
                var so = new SerializedObject(puerta);
                so.FindProperty("isLocked").boolValue = false;
                so.FindProperty("useCodeAnimationFallback").boolValue = true;
                so.FindProperty("codeRotateInWorldSpace").boolValue = true;
                float sentido = float.TryParse(d.Get("bisagra_sentido", "1"), out var s) ? s : 1f;
                so.FindProperty("codeOpenEulerDelta").vector3Value = new Vector3(0f, -95f * sentido, 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }

        static int ConfigurarObjetos(GameObject go)
        {
            int n = 0;
            foreach (var d in go.GetComponentsInChildren<DatosBlender>(true))
            {
                var g = d.gameObject;
                string fun = d.Get("funcion"), tipo = d.Get("prop_tipo"), txt = d.Get("interactuable");
                string nombre = string.IsNullOrEmpty(txt) ? tipo.Replace('_', ' ') : txt.Split(':')[0];
                switch (fun)
                {
                    case "recogible":
                        if (tipo == "papel" && !string.IsNullOrEmpty(d.Get("pista_texto")))
                        {
                            // papelito con la clave de un casillero cercano (amueblar.candados_casilleros)
                            var nota = g.AddComponent<EstanDentro.Interaction.Note>();
                            var sn = new SerializedObject(nota);
                            sn.FindProperty("noteTitle").stringValue = d.Get("pista_titulo");
                            sn.FindProperty("noteText").stringValue = d.Get("pista_texto").Replace("\\n", "\n");
                            sn.FindProperty("disappearAfterRead").boolValue = false;
                            sn.ApplyModifiedPropertiesWithoutUndo();
                            break;
                        }
                        var r = g.AddComponent<Recogible>(); r.itemId = tipo; r.nombreVisible = Capital(nombre); break;
                    case "contenedor":
                        var c = g.AddComponent<Contenedor>(); c.nombreVisible = Capital(nombre); break;
                    case "escondite":
                        g.AddComponent<Escondite>(); break;
                    case "casillero":
                        // se abre de verdad (puerta con bisagra) y trae pilas, tiza, monedas o nada (amueblar.botin_casilleros)
                        var cs = g.AddComponent<Casillero>();
                        cs.botinId = d.Get("botin");
                        cs.botinNombre = NombreObjeto(cs.botinId);
                        cs.susto = d.Get("susto");
                        cs.sonidoAbrir = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambient/casillero-abriendose.mp3");
                        cs.sonidoTomar = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambient/Pickup item.wav");
                        if (!string.IsNullOrEmpty(d.Get("clave")))
                        {
                            // casillero con candado: la clave está en un papelito cerca; también se puede forzar
                            var cl = g.AddComponent<EstanDentro.Interaction.CombinationLock>();
                            var sl = new SerializedObject(cl);
                            var code = sl.FindProperty("correctCode"); string clave = d.Get("clave");
                            code.arraySize = clave.Length;
                            for (int k = 0; k < clave.Length; k++) code.GetArrayElementAtIndex(k).intValue = clave[k] - '0';
                            sl.FindProperty("grantsCerraduraPrimera").boolValue = false;
                            sl.FindProperty("digitClickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ambient/CombinationLock click digito.wav");
                            sl.ApplyModifiedPropertiesWithoutUndo();
                            cl.Estilo = EstanDentro.Interaction.EstiloCandado.Laton3;
                            cl.NombreVisible = "Candado de un casillero";
                        }
                        break;
                    case "dispensador":
                        var di = g.AddComponent<Dispensador>(); di.entregaId = d.Get("dispensa", "gaseosa");
                        di.entregaNombre = Capital(di.entregaId); if (di.entregaId == "agua") di.requiereId = ""; break;
                    case "ducto":
                        var rj = g.AddComponent<RejillaDucto>(); rj.salida = d.Get("ducto_salida"); rj.salidaAlterna = d.Get("ducto_salida_alterna"); break;
                    case "empujable":
                        // solo se empuja lo que sirve para trancar puertas; pupitres y sillas quedan fijos
                        if (d.Get("bloquea_puerta") == "True")
                        {
                            var mc = g.GetComponent<MeshCollider>(); if (mc) mc.convex = true;
                            var hp = g.AddComponent<HeavyProp>();
                            var so = new SerializedObject(hp);
                            so.FindProperty("lockInPlace").boolValue = false;
                            so.FindProperty("mass").floatValue = 120f;
                            so.FindProperty("linearDrag").floatValue = 12f;
                            so.FindProperty("maxLinearVelocity").floatValue = 0.35f;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                        if (d.Get("tambien_contenedor") == "True") { var c2 = g.AddComponent<Contenedor>(); c2.nombreVisible = Capital(nombre); }
                        break;
                    default:
                        if (!string.IsNullOrEmpty(txt)) { var c3 = g.AddComponent<Contenedor>(); c3.nombreVisible = Capital(nombre); }
                        else continue;
                        break;
                }
                n++;
            }
            return n;
        }

        static string NombreObjeto(string id) => id switch
        {
            "baterias" => "Pilas", "moneda" => "Moneda", "tiza" => "Tiza", "" => "", _ => Capital(id)
        };

        static bool EsAsiento(string tipo) =>
            !string.IsNullOrEmpty(tipo) && (tipo == "pupitre" || tipo.StartsWith("silla") || tipo.StartsWith("banca") || tipo == "taburete");

        static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        /// <summary>Valores de la noche en un solo lugar para ajustarlos rápido.</summary>
        public static class NOCHE
        {
            public const float ev100 = 4.0f;          // exposición fija
            public const float cieloLux = 6f;         // brillo del cielo nocturno
            public const float lunaLux = 9f;          // luna (proyecta sombras por las ventanas)
            public const float fluorescente = 900f;   // lúmenes por tubo
            public const float bombillo = 220f;
            public const float rebote = 60f;          // luz de rebote bajo cada tubo (ambientación)
            public const float nieblaMetros = 38f;    // distancia de la niebla: más corta = aire más denso
        }

        static int CrearLuces(GameObject go)
        {
            int n = 0;
            foreach (var d in go.GetComponentsInChildren<DatosBlender>(true))
            {
                string tipo = d.Get("luz_tipo");
                var g = d.gameObject;
                if (tipo == "sol") continue;   // de noche: la luna la crea CielosYVolumen
                var luz = g.AddComponent<Light>();
                var c = d.Get("color", "1,1,1").Split(',').Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                luz.color = new Color(c[0], c[1], c[2]);
                if (tipo == "fluorescente")
                {
                    luz.type = LightType.Rectangle; luz.areaSize = new Vector2(1.2f, 0.25f);
                    g.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                    luz.lightUnit = LightUnit.Lumen; luz.intensity = NOCHE.fluorescente; luz.range = 8f; luz.shadows = LightShadows.None;
                    luz.color = Color.Lerp(luz.color, new Color(1f, 0.86f, 0.68f), 0.5f);   // tubo viejo, cálido
                    luz.lightmapBakeType = LightmapBakeType.Mixed;
                    // REBOTE: sin luz indirecta lo que el tubo no toca queda negro puro (mucho contraste).
                    // Una luz puntual suave y cálida, sin sombras, 1,3 m más abajo, simula el rebote del piso y las paredes.
                    var reb = new GameObject("Rebote");
                    reb.transform.SetParent(g.transform, false);
                    reb.transform.position = g.transform.position + Vector3.down * 1.3f;
                    var lr = reb.AddComponent<Light>();
                    lr.type = LightType.Point; lr.lightUnit = LightUnit.Lumen; lr.intensity = NOCHE.rebote; lr.range = 5f;
                    lr.color = new Color(1f, 0.78f, 0.58f); lr.shadows = LightShadows.None;
                    var hdr = reb.AddComponent<HDAdditionalLightData>();
                    hdr.affectSpecular = false;
                }
                else
                {
                    luz.type = LightType.Point; luz.lightUnit = LightUnit.Lumen; luz.intensity = NOCHE.bombillo; luz.range = 6f;
                    luz.shadows = LightShadows.Soft;
                }
                if (!g.GetComponent<HDAdditionalLightData>()) g.AddComponent<HDAdditionalLightData>();
                n++;
            }
            return n;
        }

        static void CielosYVolumen()
        {
            string hdr = RAIZ + "/Cielos/kloppenheim_02_puresky_4k.hdr";   // noche (decisión de Henry, 19-oct)
            if (AssetImporter.GetAtPath(hdr) is TextureImporter ti && ti.textureShape != TextureImporterShape.TextureCube)
            { ti.textureShape = TextureImporterShape.TextureCube; ti.SaveAndReimport(); }
            var cubo = AssetDatabase.LoadAssetAtPath<Cubemap>(hdr);

            string rutaPerfil = "Assets/Scenes/Colegio/Colegio_Recuerdo_Volumen.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(rutaPerfil);
            if (perfil == null) { perfil = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(perfil, rutaPerfil); }
            var ve = perfil.Has<VisualEnvironment>() ? perfil.components.OfType<VisualEnvironment>().First() : perfil.Add<VisualEnvironment>(true);
            ve.skyType.Override((int)SkyType.HDRI);
            var sky = perfil.Has<HDRISky>() ? perfil.components.OfType<HDRISky>().First() : perfil.Add<HDRISky>(true);
            sky.hdriSky.Override(cubo);
            sky.skyIntensityMode.Override(SkyIntensityMode.Lux);
            sky.desiredLuxValue.Override(NOCHE.cieloLux);
            // exposición FIJA: la automática subía la imagen hasta dejarla lavada (y el menú es oscuro)
            var exp = perfil.Has<Exposure>() ? perfil.components.OfType<Exposure>().First() : perfil.Add<Exposure>(true);
            exp.mode.Override(ExposureMode.Fixed);
            exp.fixedExposure.Override(NOCHE.ev100);
            var fog = perfil.Has<Fog>() ? perfil.components.OfType<Fog>().First() : perfil.Add<Fog>(true);
            fog.enabled.Override(true); fog.meanFreePath.Override(90f); fog.enableVolumetricFog.Override(true);
            fog.albedo.Override(new Color(0.62f, 0.58f, 0.55f));
            fog.meanFreePath.Override(NOCHE.nieblaMetros);
            fog.depthExtent.Override(40f);
            // ETALONAJE (ambientación, Henry 19-oct: "mucho contraste, poca ambientación")
            T Comp<T>() where T : VolumeComponent => perfil.Has<T>() ? perfil.components.OfType<T>().First() : perfil.Add<T>(true);
            var tm = Comp<Tonemapping>(); tm.mode.Override(TonemappingMode.Neutral);                 // menos duro que ACES
            var ca = Comp<ColorAdjustments>(); ca.contrast.Override(-12f); ca.saturation.Override(-15f);
            ca.colorFilter.Override(new Color(1f, 0.97f, 0.93f));                                     // cálido, como el menú
            var smh = Comp<ShadowsMidtonesHighlights>();
            smh.shadows.Override(new Vector4(1.0f, 0.88f, 0.86f, 0.06f));                             // sombras levantadas y rojizas
            var ao = Comp<ScreenSpaceAmbientOcclusion>(); ao.intensity.Override(1.2f); ao.radius.Override(1.2f);
            var bl = Comp<Bloom>(); bl.intensity.Override(0.18f); bl.scatter.Override(0.75f);
            var vi = Comp<Vignette>(); vi.intensity.Override(0.32f); vi.smoothness.Override(0.45f); vi.color.Override(new Color(0.08f, 0f, 0f));
            var gr = Comp<FilmGrain>(); gr.type.Override(FilmGrainLookup.Medium1); gr.intensity.Override(0.28f);
            // ANTIFUGAS: toda la resolución de la sombra del sol se usa cerca de la cámara (adentro del colegio)
            var sh = perfil.Has<HDShadowSettings>() ? perfil.components.OfType<HDShadowSettings>().First() : perfil.Add<HDShadowSettings>(true);
            sh.maxShadowDistance.Override(70f);
            sh.cascadeShadowSplitCount.Override(4);
            var cs = perfil.Has<ContactShadows>() ? perfil.components.OfType<ContactShadows>().First() : perfil.Add<ContactShadows>(true);
            cs.enable.Override(true); cs.length.Override(0.35f); cs.distanceScaleFactor.Override(0.5f);
            EditorUtility.SetDirty(perfil);

            var vol = new GameObject("Volumen_Recuerdo").AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = perfil;
            var sol = new GameObject("Luna");
            sol.transform.rotation = Quaternion.Euler(38f, 210f, 0f);
            var l = sol.AddComponent<Light>(); l.type = LightType.Directional; l.color = new Color(0.55f, 0.66f, 1f);
            l.lightUnit = LightUnit.Lux; l.intensity = NOCHE.lunaLux; l.shadows = LightShadows.Soft;
            l.shadowNormalBias = 0.2f; l.shadowBias = 0.02f;
            var hdSol = sol.AddComponent<HDAdditionalLightData>();
            hdSol.SetShadowResolutionOverride(true);
            hdSol.SetShadowResolution(4096);
            hdSol.useContactShadow.useOverride = true;
            hdSol.useContactShadow.@override = true;
        }

        /// <summary>Copia del salón del capítulo 1 los sistemas (inventario, HUD, audio, estrés...) y el jugador.</summary>
        static void CopiarSistemasYJugador()
        {
            var destino = SceneManager.GetActiveScene();
            var origen = EditorSceneManager.OpenScene(ESCENA_SISTEMAS, OpenSceneMode.Additive);
            Type[] buscar = { typeof(Inventory.Inventory), typeof(Audio.AudioManager), typeof(Stress.StressSystem),
                              typeof(UI.HUDBootstrapper), typeof(Player.PlayerController) };
            var copiados = new HashSet<GameObject>();
            foreach (var t in buscar)
                foreach (var c in UnityEngine.Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var comp = (Component)c;
                    if (comp.gameObject.scene != origen) continue;
                    var raizGO = comp.transform.root.gameObject;
                    if (!copiados.Add(raizGO)) continue;
                    var copia = UnityEngine.Object.Instantiate(raizGO);
                    copia.name = raizGO.name;
                    SceneManager.MoveGameObjectToScene(copia, destino);
                }
            var nombresCopiados = copiados.Select(g => g.name).ToList();
            EditorSceneManager.CloseScene(origen, true);
            SceneManager.SetActiveScene(destino);

            var pc = UnityEngine.Object.FindAnyObjectByType<Player.PlayerController>();
            Debug.Log($"[Colegio] Sistemas copiados: {string.Join(", ", nombresCopiados)} | jugador en {(pc ? pc.transform.position.ToString() : "NO ENCONTRADO")}");
        }

        // ------------------------------------------------------------------ batch
        public static void EscenaBatch()
        {
            try { Escena(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Colegio] " + e); EditorApplication.Exit(1); }
        }

        public static void TodoBatch()
        {
            try { Todo(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Colegio] " + e); EditorApplication.Exit(1); }
        }
    }
}
