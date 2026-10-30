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
            MaterialesCalle(calle1, lit);
            var calle2 = Poner("Prologo_Calle", "Calle_B");
            Transform Hijo(string n) => auto.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith(n));
            Bounds B(Transform t) { var rs = t.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

            // marcas puestas en Blender: ojos de Mateo, un punto al frente y el pie del parabrisas (limpiabrisas)
            Vector3 M(string n) => Hijo(n).position;
            Vector3 ojo = M("Marca_Ojos");
            Vector3 frenteTmp = M("Marca_Frente") - ojo; frenteTmp.y = 0f; ojo += frenteTmp.normalized * 0.05f;   // en los ojos: recostado contra el espaldar (el cuello ya está tapado)
            ojo.y = 1.21f;   // ojos de un muchacho de 16 años sentado: los hombros quedan ~22 cm abajo (con 1,17 parecía la cámara en el cuello)
            Vector3 fwd = M("Marca_Frente") - ojo; fwd.y = 0f;
            fwd = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z) ? new Vector3(Mathf.Sign(fwd.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(fwd.z));   // el carro va alineado con los ejes
            Vector3 der = Vector3.Cross(Vector3.up, fwd);
            // Mateo (el cuerpo del copiloto: mallas "Object_….001") y sus ojos, 6 cm más adelante en el asiento (Henry, 19-oct)
            // y 7 cm más arriba: estaba hundido y los muslos quedaban dentro del cojín (solo se veían las manos)
            const float MATEO_ADELANTE = 0f, MATEO_ARRIBA = 0f;   // ya va bien sentado desde Blender (prologo_mateo_sentado.py: 92 %, recostado, rodillas dobladas)
            Vector3 corrMateo = fwd * MATEO_ADELANTE + Vector3.up * MATEO_ARRIBA;
            foreach (var r in auto.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Object_") && r.name.EndsWith(".001")))
                r.transform.position += corrMateo;
            foreach (var n in new[] { "Prologo_Cuaderno", "Prologo_Celular" })                 // lo que tiene sobre las piernas sube con él
                Hijo(n).position += corrMateo;
            ojo += corrMateo;
            foreach (var t in auto.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Marca_")))
                if (t.GetComponent<Renderer>()) t.GetComponent<Renderer>().enabled = false;
            const float LARGO = 650f;
            // avenida de 2 carriles por sentido (3,5 m cada uno): el Corolla va por el carril de adentro de la derecha, no por el centro
            {
                var bc = B(calle1.transform);
                var amarilla = mats.Values.FirstOrDefault(m => m.name.Contains("pintura_blanca"));
                string rutaAm = ART + "/Materiales/Linea_Amarilla.mat";
                var matAm = AssetDatabase.LoadAssetAtPath<Material>(rutaAm);
                if (!matAm) { matAm = new Material(lit); AssetDatabase.CreateAsset(matAm, rutaAm); }
                matAm.SetColor("_BaseColor", new Color(0.75f, 0.55f, 0.08f)); matAm.SetFloat("_Smoothness", 0.5f); HDMaterial.ValidateMaterial(matAm);
                float z0 = Vector3.Dot(bc.min, fwd), z1 = Vector3.Dot(bc.max, fwd), zc = (z0 + z1) * 0.5f;
                Vector3 baseC = calle1.transform.position; baseC -= fwd * Vector3.Dot(baseC, fwd);
                void Linea(Transform padre, float x, float desde, float largo, Material m)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                    g.name = "Linea"; g.GetComponent<Renderer>().sharedMaterial = m;
                    g.transform.position = baseC + der * x + Vector3.up * 0.016f + fwd * (desde + largo * 0.5f);   // por encima de los charcos (1 cm): sin parpadeo
                    g.transform.rotation = Quaternion.LookRotation(fwd); g.transform.localScale = new Vector3(0.12f, 0.004f, largo);
                    g.transform.SetParent(padre, true);
                }
                foreach (var cl in new[] { calle1 })
                {
                    var lineas = new GameObject("Lineas_Carriles").transform; lineas.SetParent(cl.transform, true);
                    Linea(lineas, -0.12f, z0, z1 - z0, matAm); Linea(lineas, 0.12f, z0, z1 - z0, matAm);           // doble amarilla al centro
                    for (float z = z0; z < z1 - 3f; z += 9f) { Linea(lineas, -3.5f, z, 3f, amarilla); Linea(lineas, 3.5f, z, 3f, amarilla); }
                }
                calle1.transform.position -= der * 1.75f;
            }
            calle2.transform.position = calle1.transform.position + fwd * LARGO;
            UnityEngine.Object.DestroyImmediate(calle2); calle2 = UnityEngine.Object.Instantiate(calle1); calle2.name = "Calle_B";
            calle2.transform.position = calle1.transform.position + fwd * LARGO;

            // vidrios del Corolla (Material.004 = ventanas, klosz = faros): transparentes de verdad
            string rutaVidrio = ART + "/Materiales/Vidrio_Carro.mat";
            var vidrio = AssetDatabase.LoadAssetAtPath<Material>(rutaVidrio);
            if (!vidrio) { vidrio = new Material(lit); AssetDatabase.CreateAsset(vidrio, rutaVidrio); }
            vidrio.SetFloat("_SurfaceType", 1f); vidrio.SetColor("_BaseColor", new Color(0.62f, 0.68f, 0.7f, 0.1f));
            vidrio.SetFloat("_Smoothness", 0.97f); vidrio.SetFloat("_Metallic", 0f);
            vidrio.SetFloat("_EnableBlendModePreserveSpecularLighting", 0f);   // reflejaba el cielo rosado entero: el parabrisas parecía una pared
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
                    // molduras del tablero y la consola: venían blancas al 90 % y se veían como un contorno blanco; en el Corolla son gris plata oscuro
                    if (nm == "Material.016" || nm == "Material.010")
                    { copia.SetColor("_BaseColor", new Color(0.20f, 0.20f, 0.21f)); copia.SetFloat("_Smoothness", 0.35f); copia.SetFloat("_Metallic", 0.3f); }
                    // cromo de las molduras (franja del tablero, palanca, manijas): venía con brillo 1,0 y reflejaba el cielo como una raya blanca
                    if (nm == "M_dashboard.001")   // su textura es blanca fuera de los relojes: franja del tablero, consola y puertas
                    { copia.SetColor("_BaseColor", new Color(0.30f, 0.30f, 0.31f)); copia.SetFloat("_Smoothness", 0.45f); copia.SetFloat("_Metallic", 0.6f); }
                    if (nm == "Material.003")
                    { copia.SetColor("_BaseColor", new Color(0.30f, 0.30f, 0.31f)); copia.SetFloat("_Smoothness", 0.55f); copia.SetFloat("_Metallic", 1f); }
                    HDMaterial.ValidateMaterial(copia); EditorUtility.SetDirty(copia);
                    return opacas[x] = copia;
                }).ToArray();
            Debug.Log($"[Prologo] materiales del FBX copiados (opacos y de dos caras): {opacas.Count}");
            // las molduras de adentro (franja del tablero, consola, vasos, puertas) venían con la pintura BLANCA de la carrocería:
            // se veían como rayas blancas. Adentro van en gris plata satinado, como el Corolla de verdad
            {
                string rutaM = ART + "/Materiales/Moldura_Interior.mat";
                var moldura = AssetDatabase.LoadAssetAtPath<Material>(rutaM); if (!moldura) { moldura = new Material(lit); AssetDatabase.CreateAsset(moldura, rutaM); }
                moldura.SetColor("_BaseColor", new Color(0.24f, 0.24f, 0.25f)); moldura.SetFloat("_Metallic", 0.7f); moldura.SetFloat("_Smoothness", 0.5f);
                moldura.SetFloat("_DoubleSidedEnable", 1f); HDMaterial.ValidateMaterial(moldura);
                int nMold = 0;
                foreach (var r in auto.GetComponentsInChildren<Renderer>(true))
                {
                    if (!r.name.Contains("interior")) continue;
                    var ms = r.sharedMaterials; bool cambio = false;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith("Opaco_main_paint")) { ms[i] = moldura; cambio = true; nMold++; }
                    if (cambio) r.sharedMaterials = ms;
                }
                Debug.Log($"[Prologo] molduras de adentro en gris plata: {nMold}");
            }

            var camGO = new GameObject("Ojos_Mateo", typeof(Camera), typeof(AudioListener));
            camGO.transform.SetPositionAndRotation(ojo, Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(2f, 0f, 0f));   // mirando al frente, recostado
            var cam = camGO.GetComponent<Camera>(); cam.nearClipPlane = 0.03f; cam.fieldOfView = 68f; cam.tag = "MainCamera";
            camGO.AddComponent<HDAdditionalCameraData>();
            var pivote = new GameObject("Asiento_Mateo"); pivote.transform.SetPositionAndRotation(ojo, camGO.transform.rotation);
            camGO.transform.SetParent(pivote.transform, true);

            // colisionadores en lo que se toca (la radio es una caja invisible sobre la pantalla del Corolla)
            GameObject Tocable(string n) { var t = Hijo(n).gameObject; if (!t.GetComponent<Collider>()) t.AddComponent<BoxCollider>(); return t; }
            var cuaderno = Tocable("Prologo_Cuaderno"); var lonchera = Tocable("PROP_lonchera");
            var radio = Tocable("Prologo_Radio"); var celular = Tocable("Prologo_Celular");
            PantallaDelCelular(celular, lit);
            PantallaDeLaRadio(auto, lit);
            if (radio.GetComponent<Renderer>()) radio.GetComponent<Renderer>().enabled = false;
            // la lonchera llegó blanca (su material no estaba en la lista): lata verde, la MISMA del colegio (allá la reconoce), con el candadito rojo
            string rutaLon = ART + "/Materiales/Lonchera_Roja.mat";
            var matLon = AssetDatabase.LoadAssetAtPath<Material>(rutaLon);
            if (!matLon) { matLon = new Material(lit); AssetDatabase.CreateAsset(matLon, rutaLon); }
            matLon.SetColor("_BaseColor", new Color(0.17f, 0.25f, 0.15f)); matLon.SetFloat("_Smoothness", 0.45f); matLon.SetFloat("_Metallic", 0.3f); HDMaterial.ValidateMaterial(matLon);
            foreach (var r in lonchera.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterials.Any(m => m && m.GetColor("_BaseColor").grayscale > 0.7f))
                    r.sharedMaterials = r.sharedMaterials.Select(m => m && m.GetColor("_BaseColor").grayscale > 0.7f ? matLon : m).ToArray();
            // el candadito rojo de maleta (el mismo del colegio), exportado aparte desde Blender (prologo_candadito.py), hijo de la lonchera
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{ART}/Prologo_Candadito.fbx"))
            {
                var cand = Poner("Prologo_Candadito", "Candadito_Lonchera");
                var rojo = Pintura("Candadito_Rojo", new Color(0.62f, 0.05f, 0.04f), 0.6f);
                var negroCand = Pintura("Candadito_Negro", new Color(0.03f, 0.03f, 0.03f), 0.35f);
                foreach (var r in cand.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterials = r.sharedMaterials.Select(m => m && m.name.Contains("rojo") ? rojo : negroCand).ToArray();
                cand.transform.SetParent(lonchera.transform, true);
            }
            // la lonchera en el piso, frente a los pies de Mateo (Henry: entre las sillas no se notaba)
            {
                var cuerpo = auto.GetComponentsInChildren<Renderer>(true).Where(rr => rr.name.StartsWith("Object_") && rr.name.EndsWith(".001")).ToArray();
                if (cuerpo.Length > 0)
                {
                    var bc = cuerpo[0].bounds; foreach (var rr in cuerpo) bc.Encapsulate(rr.bounds);
                    float suelas = bc.min.y, puntas = Vector3.Dot(bc.max, fwd);
                    var bl = lonchera.GetComponentsInChildren<Renderer>().Aggregate(new Bounds(lonchera.transform.position, Vector3.zero), (a, rr) => { a.Encapsulate(rr.bounds); return a; });
                    Vector3 destino = new Vector3(ojo.x, 0f, 0f) + fwd * (puntas + 0.14f);
                    destino.y = suelas + (lonchera.transform.position.y - bl.min.y) + 0.004f;          // apoyada en el tapete
                    Vector3 offsetXZ = lonchera.transform.position - bl.center; offsetXZ.y = 0f;
                    lonchera.transform.position = new Vector3(destino.x, destino.y, 0f) + Vector3.Scale(destino, new Vector3(0, 0, 1)) + offsetXZ;
                    lonchera.transform.rotation = Quaternion.AngleAxis(-12f, Vector3.up) * lonchera.transform.rotation;   // un poco de lado, como quien la deja ahí
                    Debug.Log($"[Prologo] lonchera en el piso: {lonchera.transform.position:F2} (suelas {suelas:F2}, puntas {puntas:F2})");
                }
            }

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
                hoja.transform.localScale = new Vector3(0.55f, 0.014f, 0.016f); hoja.transform.localPosition = new Vector3(0.29f, 0f, 0.012f);
                if (negroMat) hoja.GetComponent<Renderer>().sharedMaterial = negroMat;
                // brazo metálico que sostiene la plumilla (del eje a la mitad de la hoja) y el eje
                var brazo = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.DestroyImmediate(brazo.GetComponent<Collider>());
                brazo.name = "Brazo"; brazo.transform.SetParent(piv, false);
                brazo.transform.localScale = new Vector3(0.36f, 0.012f, 0.008f); brazo.transform.localPosition = new Vector3(0.17f, 0f, 0.026f);
                if (negroMat) brazo.GetComponent<Renderer>().sharedMaterial = negroMat;
                var eje = GameObject.CreatePrimitive(PrimitiveType.Cylinder); UnityEngine.Object.DestroyImmediate(eje.GetComponent<Collider>());
                eje.name = "Eje"; eje.transform.SetParent(piv, false); eje.transform.localScale = new Vector3(0.03f, 0.015f, 0.03f);
                eje.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                if (negroMat) eje.GetComponent<Renderer>().sharedMaterial = negroMat;
                plumas[i] = piv;
            }

            // agua del parabrisas: copia del vidrio de adelante 4 mm por fuera, con UV proyectado; las plumillas la barren
            {
                var tris = new System.Collections.Generic.List<Vector3>();
                foreach (var mf in auto.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = mf.GetComponent<Renderer>(); if (!r || !mf.sharedMesh) continue;
                    var ms = mf.sharedMesh; var vs = ms.vertices;
                    for (int s = 0; s < ms.subMeshCount && s < r.sharedMaterials.Length; s++)
                    {
                        if (r.sharedMaterials[s] != vidrio) continue;
                        var idx = ms.GetTriangles(s);
                        for (int k = 0; k < idx.Length; k += 3)
                        {
                            Vector3 a = mf.transform.TransformPoint(vs[idx[k]]), b = mf.transform.TransformPoint(vs[idx[k + 1]]), c = mf.transform.TransformPoint(vs[idx[k + 2]]);
                            var nt = Vector3.Cross(b - a, c - a).normalized; var cen = (a + b + c) / 3f;
                            if (Mathf.Abs(Vector3.Dot(nt, normal)) < 0.75f || Mathf.Abs(Vector3.Dot(nt, fwd)) < 0.35f) continue;   // sin el techo corredizo
                            if (Vector3.Dot(cen - ojo, fwd) < 0.25f || Vector3.Dot(cen - ojo, fwd) > 1.6f || cen.y < ojo.y - 0.45f) continue;
                            tris.Add(a); tris.Add(b); tris.Add(c);
                        }
                    }
                }
                if (tris.Count > 0)
                {
                    Vector3 ejeV = Vector3.Cross(normal, der).normalized; if (ejeV.y < 0) ejeV = -ejeV;
                    var b0 = new Bounds(tris[0], Vector3.zero); foreach (var p in tris) b0.Encapsulate(p);
                    Vector3 cen = b0.center; float u0 = 1e9f, u1 = -1e9f, v0 = 1e9f, v1 = -1e9f;
                    foreach (var p in tris) { float u = Vector3.Dot(p - cen, der), v = Vector3.Dot(p - cen, ejeV); u0 = Mathf.Min(u0, u); u1 = Mathf.Max(u1, u); v0 = Mathf.Min(v0, v); v1 = Mathf.Max(v1, v); }
                    cen += der * (u0 + u1) * 0.5f + ejeV * (v0 + v1) * 0.5f;
                    float anU = u1 - u0, alV = v1 - v0;
                    var verts = new Vector3[tris.Count]; var uvs = new Vector2[tris.Count]; var ind = new int[tris.Count];
                    for (int k = 0; k < tris.Count; k += 3)
                    {
                        var nt = Vector3.Cross(tris[k + 1] - tris[k], tris[k + 2] - tris[k]).normalized;
                        bool alReves = Vector3.Dot(nt, normal) < 0f;
                        for (int j = 0; j < 3; j++)
                        {
                            int o = alReves ? (j == 0 ? 0 : 3 - j) : j;            // las caras miran hacia afuera y hacia adentro (dos caras en el material)
                            var p = tris[k + o] + normal * 0.004f;
                            verts[k + j] = auto.transform.InverseTransformPoint(p);
                            uvs[k + j] = new Vector2(Vector3.Dot(p - cen, der) / anU + 0.5f, Vector3.Dot(p - cen, ejeV) / alV + 0.5f);
                            ind[k + j] = k + j;
                        }
                    }
                    var malla = new Mesh { name = "Agua_Parabrisas" }; malla.vertices = verts; malla.uv = uvs; malla.triangles = ind;
                    malla.RecalculateNormals(); malla.RecalculateTangents(); malla.RecalculateBounds();
                    AssetDatabase.DeleteAsset(ART + "/Agua_Parabrisas.asset"); AssetDatabase.CreateAsset(malla, ART + "/Agua_Parabrisas.asset");
                    string rutaAgua = ART + "/Materiales/Agua_Parabrisas.mat";
                    var matAgua = AssetDatabase.LoadAssetAtPath<Material>(rutaAgua);
                    if (!matAgua) { matAgua = new Material(lit); AssetDatabase.CreateAsset(matAgua, rutaAgua); }
                    matAgua.SetFloat("_SurfaceType", 1f); matAgua.SetFloat("_DoubleSidedEnable", 1f); matAgua.SetFloat("_BlendMode", 0f);
                    matAgua.SetColor("_BaseColor", Color.white); matAgua.SetFloat("_Smoothness", 0.98f); matAgua.SetFloat("_Metallic", 0f);
                    matAgua.SetFloat("_NormalScale", 1.6f); matAgua.SetFloat("_TransparentSortPriority", 2f);
                    matAgua.SetFloat("_EnableBlendModePreserveSpecularLighting", 0f);
                    matAgua.SetFloat("_RefractionModel", 3f); matAgua.SetFloat("_Ior", 1.33f); matAgua.SetFloat("_Thickness", 0.003f);   // la gota deforma lo que hay detrás (modelo delgado)
                    matAgua.SetFloat("_UseEmissiveIntensity", 0f);    // el reflejo solo donde hay agua (si no, el vidrio entero se veía como espejo)
                    // sin jugar (editor, fotos) el vidrio queda limpio: textura transparente hasta que el script dibuja las gotas
                    var limpio = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "/Agua_Vacia.asset");
                    if (!limpio) { limpio = new Texture2D(4, 4, TextureFormat.RGBA32, false); limpio.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 0), 16).ToArray()); limpio.Apply(); AssetDatabase.CreateAsset(limpio, ART + "/Agua_Vacia.asset"); }
                    matAgua.SetTexture("_BaseColorMap", limpio);
                    // relieve plano y emisión desde ya, para que el juego compilado incluya esas variantes del shader
                    var plano = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "/Agua_Relieve_Plano.asset");
                    if (!plano) { plano = new Texture2D(4, 4, TextureFormat.RGBA32, false, true); plano.SetPixels32(Enumerable.Repeat(new Color32(255, 128, 0, 128), 16).ToArray()); plano.Apply(); AssetDatabase.CreateAsset(plano, ART + "/Agua_Relieve_Plano.asset"); }
                    matAgua.SetTexture("_NormalMap", plano);
                    matAgua.SetTexture("_EmissiveColorMap", limpio); matAgua.SetColor("_EmissiveColor", new Color(0.55f, 0.58f, 0.62f) * 30f);
                    HDMaterial.ValidateMaterial(matAgua);
                    var go = new GameObject("Agua_Parabrisas", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(auto.transform, false);
                    go.GetComponent<MeshFilter>().sharedMesh = malla; go.GetComponent<MeshRenderer>().sharedMaterial = matAgua;
                    go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var ag = go.AddComponent<AguaParabrisas>(); ag.plumillas = plumas;
                    ag.centro = auto.transform.InverseTransformPoint(cen); ag.ejeU = auto.transform.InverseTransformDirection(der); ag.ejeV = auto.transform.InverseTransformDirection(ejeV);
                    ag.anchoU = anU; ag.altoV = alV;
                    Debug.Log($"[Prologo] parabrisas: {tris.Count / 3} triángulos, {anU:F2} x {alV:F2} m");
                }
                else Debug.LogWarning("[Prologo] no encontré el parabrisas");
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
            // vehículos reales de Sketchfab (CC-BY, 02_Arte3D_Blender/scripts_blender/prologo_vehiculos.py): frente hacia +Z, apoyados en y=0
            GameObject Modelo(string n)
            {
                var pref = AssetDatabase.LoadAssetAtPath<GameObject>($"{ART}/Vehiculos/{n}.fbx");
                if (!pref) return null;
                var m = (GameObject)PrefabUtility.InstantiatePrefab(pref); m.name = n + "_modelo";
                PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var mr in m.GetComponentsInChildren<Renderer>()) mr.shadowCastingMode = ShadowCastingMode.On;
                // contenedor sin rotación: el control gira el contenedor y el FBX conserva su corrección de ejes
                // (antes se le pisaba la rotación y los buses y el camión quedaban mirando al piso)
                var g = new GameObject(n); m.transform.SetParent(g.transform, false);
                return g;
            }
            var camion = Modelo("camion_jumbo") ?? Vehiculo("Camion", 9f, 2.8f, 2.5f, Pintura("Cabina_Camion", new Color(0.55f, 0.08f, 0.06f), 0.6f), true).gameObject;
            camion.SetActive(false);                                                    // el control lo prende en el presagio (si no, envuelve al carro en el editor)
            var trafico = Vehiculo("Carro_Trafico", 4.3f, 1.4f, 1.8f, Pintura("Pintura_Trafico", new Color(0.12f, 0.16f, 0.22f), 0.7f), false);
            trafico.gameObject.SetActive(false);
            var moldes = new System.Collections.Generic.List<Transform>();
            var amarillo = Pintura("Taxi_Amarillo", new Color(0.95f, 0.72f, 0.04f), 0.65f);
            foreach (var n in new[] { "renault_logan", "chevrolet_spark", "chevrolet_aveo", "taxi", "taxi", "bus_sitp" })   // el taxi sale el doble (Bogotá)
            {
                // el taxi es un Spark con la pintura amarilla (el Picanto llegó sin texturas y parecía un huevo)
                var g = Modelo(n == "taxi" ? "chevrolet_spark" : n); if (!g) continue;
                if (n == "taxi")
                {
                    g.name = "taxi";
                    foreach (var mr in g.GetComponentsInChildren<Renderer>())
                        mr.sharedMaterials = mr.sharedMaterials.Select(m => m && m.name.StartsWith("primary") ? amarillo : m).ToArray();
                }
                // de día lluvioso todos van con las luces prendidas: dos luces sin sombra al frente
                foreach (float sx in new[] { -1f, 1f })
                {
                    var fl = new GameObject("Luz_Frente").AddComponent<Light>(); fl.transform.SetParent(g.transform, false);
                    var bb = g.GetComponentsInChildren<Renderer>().Aggregate(new Bounds(g.transform.position, Vector3.zero), (a, rr) => { a.Encapsulate(rr.bounds); return a; });
                    fl.transform.localPosition = new Vector3(sx * bb.extents.x * 0.65f, Mathf.Min(1.0f, bb.size.y * 0.42f), bb.extents.z + 0.05f);
                    fl.type = LightType.Spot; fl.spotAngle = 60f; fl.range = 40f; fl.color = new Color(1f, 0.95f, 0.85f);
                    fl.lightUnit = LightUnit.Lumen; fl.intensity = 900f; fl.shadows = LightShadows.None; fl.gameObject.AddComponent<HDAdditionalLightData>();
                }
                g.SetActive(false); moldes.Add(g.transform);
            }
            var faros = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var f = new GameObject("Faro_" + i); f.transform.SetParent(camion.transform, false);
                f.transform.localPosition = new Vector3(i == 0 ? -0.85f : 0.85f, 0.95f, 3.8f);   // en los faros del Jumbo (7,5 m de largo)
                var l = f.AddComponent<Light>(); l.type = LightType.Spot; l.spotAngle = 70f; l.range = 90f; l.color = new Color(1f, 0.95f, 0.85f);
                l.lightUnit = LightUnit.Lumen; l.intensity = 2000f; l.shadows = LightShadows.None;
                f.AddComponent<HDAdditionalLightData>();
                faros[i] = l;
            }

            // lluvia: partículas estiradas alrededor del carro (el carro está quieto; la calle se mueve)
            var lluvia = new GameObject("Lluvia").AddComponent<ParticleSystem>();
            lluvia.transform.position = ojo + Vector3.up * 9f + fwd * 6f;
            lluvia.transform.rotation = Quaternion.LookRotation(Vector3.down);
            var mn = lluvia.main; mn.startLifetime = 1.4f; mn.startSpeed = 12f; mn.startSize = 0.015f; mn.maxParticles = 14000;
            mn.simulationSpace = ParticleSystemSimulationSpace.World; mn.startColor = new Color(0.8f, 0.84f, 0.9f, 0.5f);
            var em = lluvia.emission; em.rateOverTime = 8000f;                                          // aguacero, no llovizna
            var sh = lluvia.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30f, 30f, 0.1f);
            var vel = lluvia.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-fwd.x * 11f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(-fwd.z * 11f);
            var pr = lluvia.GetComponent<ParticleSystemRenderer>(); pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 8f;
            string rutaGota = ART + "/Materiales/Gota.mat";
            var gota = AssetDatabase.LoadAssetAtPath<Material>(rutaGota);
            if (!gota) { gota = new Material(Shader.Find("HDRP/Unlit")); AssetDatabase.CreateAsset(gota, rutaGota); }
            gota.SetFloat("_SurfaceType", 1f); gota.SetColor("_UnlitColor", new Color(0.7f, 0.75f, 0.8f, 0.25f));
            HDMaterial.ValidateMaterial(gota); pr.sharedMaterial = gota;
            // lluvia lejana: gotas más gruesas en un área grande delante, se ve caer en cortinas sobre la calle y los edificios
            ParticleSystem lejos;
            {
                lejos = UnityEngine.Object.Instantiate(lluvia.gameObject).GetComponent<ParticleSystem>(); lejos.name = "Lluvia_Lejana";
                lejos.transform.position = ojo + Vector3.up * 22f + fwd * 70f;
                var m2 = lejos.main; m2.startLifetime = 2.4f; m2.startSpeed = 11f; m2.startSize = 0.035f; m2.maxParticles = 16000;
                m2.startColor = new Color(0.8f, 0.83f, 0.88f, 0.22f);
                var e2 = lejos.emission; e2.rateOverTime = 7000f;
                var s2 = lejos.shape; s2.scale = new Vector3(40f, 130f, 0.1f);
                var r2 = lejos.GetComponent<ParticleSystemRenderer>(); r2.lengthScale = 10f;
            }
            // no llueve dentro del carro: las gotas que entran a la caja de la cabina desaparecen (choca con el techo)
            {
                var bAuto = B(auto.transform);
                var techo = new GameObject("Techo_Lluvia", typeof(BoxCollider)); techo.layer = 2;              // Ignore Raycast: no estorba al tocar cosas
                techo.transform.position = bAuto.center; var bx = techo.GetComponent<BoxCollider>(); bx.isTrigger = true; bx.size = bAuto.size + Vector3.up * 0.1f;
                foreach (var ps in new[] { lluvia, lejos })      // las dos lluvias: ninguna gota atraviesa la cabina
                {
                    var trg = ps.trigger; trg.enabled = true; trg.AddCollider(bx);
                    trg.inside = ParticleSystemOverlapAction.Kill; trg.enter = ParticleSystemOverlapAction.Kill; trg.outside = ParticleSystemOverlapAction.Ignore;
                }
            }
            // reflejos de la cabina: sin esto todo lo de adentro reflejaba el cielo gris en los bordes (contorno blanco en manos, ropa, silla)
            {
                var bAuto = B(auto.transform);
                var sonda = new GameObject("Reflejos_Cabina", typeof(ReflectionProbe)); sonda.transform.position = bAuto.center;
                var rp = sonda.GetComponent<ReflectionProbe>(); rp.size = bAuto.size; rp.boxProjection = true;
                var hdr_s = sonda.AddComponent<HDAdditionalReflectionData>();
                hdr_s.mode = ProbeSettings.Mode.Realtime; hdr_s.realtimeMode = ProbeSettings.RealtimeMode.OnEnable;
                hdr_s.influenceVolume.shape = InfluenceShape.Box; hdr_s.influenceVolume.boxSize = bAuto.size;
                hdr_s.influenceVolume.boxBlendDistancePositive = Vector3.one * 0.15f; hdr_s.influenceVolume.boxBlendDistanceNegative = Vector3.one * 0.15f;
            }

            // cielo nublado de las 6:50 a. m. con lluvia
            string hdr = "Assets/Art/Edificio/Cielos/kloppenheim_06_puresky_4k.hdr";
            if (AssetImporter.GetAtPath(hdr) is TextureImporter ti && ti.textureShape != TextureImporterShape.TextureCube)
            { ti.textureShape = TextureImporterShape.TextureCube; ti.SaveAndReimport(); }
            string rutaPerfil = "Assets/Scenes/Prologo/Prologo_Volumen.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(rutaPerfil);
            // se rehace siempre: antes los componentes no quedaban guardados dentro del archivo (en el juego compilado no había niebla ni exposición)
            if (perfil) AssetDatabase.DeleteAsset(rutaPerfil);
            perfil = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(perfil, rutaPerfil);
            T C<T>() where T : VolumeComponent => perfil.Has<T>() ? perfil.components.OfType<T>().First() : perfil.Add<T>(true);
            // aguacero: cielo cerrado y gris (no el cielo despejado del HDRI); su luz tiñe la niebla y el ambiente
            C<VisualEnvironment>().skyType.Override((int)SkyType.Gradient);
            var gsky = C<GradientSky>();
            gsky.top.Override(new Color(0.50f, 0.53f, 0.57f)); gsky.middle.Override(new Color(0.64f, 0.66f, 0.69f)); gsky.bottom.Override(new Color(0.36f, 0.37f, 0.38f));
            gsky.gradientDiffusion.Override(1.2f);
            gsky.skyIntensityMode.Override(SkyIntensityMode.Exposure); gsky.exposure.Override(PROLOGO.cieloEV);
            if (perfil.Has<HDRISky>()) perfil.Remove<HDRISky>();
            var exp = C<Exposure>(); exp.mode.Override(ExposureMode.Fixed); exp.fixedExposure.Override(PROLOGO.ev100);
            var fog = C<Fog>(); fog.enabled.Override(true); fog.meanFreePath.Override(PROLOGO.nieblaMetros); fog.enableVolumetricFog.Override(false);   // la volumétrica llenaba el carro y dejaba el parabrisas blanco
            fog.maximumHeight.Override(140f); fog.baseHeight.Override(0f);     // alta: también tapa el fondo de la ciudad y el cielo
            fog.mipFogNear.Override(0f); fog.mipFogFar.Override(200f);
            var mb = C<MotionBlur>(); mb.intensity.Override(0f);                                     // girar la cabeza no debe embarrar la imagen
            fog.albedo.Override(new Color(0.7f, 0.74f, 0.78f));
            C<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var ca = C<ColorAdjustments>(); ca.saturation.Override(-35f); ca.contrast.Override(-6f); ca.colorFilter.Override(new Color(0.86f, 0.91f, 0.97f));   // frío y apagado
            var vi = C<Vignette>(); vi.intensity.Override(0.3f);
            var gr = C<FilmGrain>(); gr.type.Override(FilmGrainLookup.Medium1); gr.intensity.Override(0.22f);
            var ao = C<ScreenSpaceAmbientOcclusion>(); ao.intensity.Override(1f);
            foreach (var comp in perfil.components)
                if (comp && !AssetDatabase.Contains(comp)) { comp.name = comp.GetType().Name; comp.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(comp, perfil); }
            EditorUtility.SetDirty(perfil); AssetDatabase.SaveAssets();
            var vol = new GameObject("Volumen_Prologo").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = perfil;
            var sol = new GameObject("Luz_Nublada"); sol.transform.rotation = Quaternion.Euler(55f, 160f, 0f);
            var ls = sol.AddComponent<Light>(); ls.type = LightType.Directional; ls.color = new Color(0.8f, 0.86f, 1f);
            ls.lightUnit = LightUnit.Lux; ls.intensity = PROLOGO.solLux; ls.shadows = LightShadows.Soft;
            var hd = sol.AddComponent<HDAdditionalLightData>(); hd.angularDiameter = 40f;          // cielo cubierto: sombras muy suaves

            // control
            var ctl = new GameObject("_Prologo").AddComponent<ControlPrologo>();
            ctl.ojos = camGO.transform; ctl.calles = new[] { calle1.transform, calle2.transform }; ctl.largoCalle = LARGO; ctl.adelante = fwd;
            ctl.limpiabrisas = plumas; ctl.lucesCamion = faros; ctl.camion = camion.transform; ctl.carroTrafico = trafico; ctl.carrosTrafico = moldes.ToArray(); ctl.sueloY = 0f;
            ctl.cuaderno = cuaderno; ctl.lonchera = lonchera; ctl.radio = radio; ctl.celular = celular;

            EditorSceneManager.SaveScene(escena, ESCENA);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Prologo] escena armada: {ESCENA} | ojos en {ojo} | adelante {fwd}");
        }

        // ------------------------------------------------------------------ materiales de la avenida (prologo_calle.py)
        // Texturas reales de Poly Haven (02_Arte3D_Blender/texturas/polyhaven) con su tamaño en metros; la malla trae UV en metros.
        const string POLYHAVEN = "../../02_Arte3D_Blender/texturas/polyhaven";
        static readonly System.Collections.Generic.Dictionary<string, (string tex, float tam, Color tinte, float liso)> CALLE_PBR =
            new System.Collections.Generic.Dictionary<string, (string, float, Color, float)>
        {
            { "asfalto", ("asphalt_02", 3f, new Color(0.42f, 0.43f, 0.45f), 0.74f) },        // empapado: oscuro y brilla
            { "bordillo", ("dirty_concrete", 1.5f, new Color(0.78f, 0.78f, 0.76f), 0.25f) },
            { "anden", ("concrete_floor_worn_001", 2f, new Color(0.8f, 0.79f, 0.76f), 0.35f) },
            { "ladrillo", ("red_brick_plaster_patch_02", 3f, Color.white, 0.12f) },
            { "ladrillo_viejo", ("large_red_bricks", 2.5f, new Color(0.9f, 0.85f, 0.82f), 0.1f) },
            { "panete_crema", ("painted_plaster_wall", 3f, new Color(1f, 0.9f, 0.72f), 0.12f) },
            { "panete_azul", ("blue_plaster_wall", 3f, Color.white, 0.12f) },
            { "panete_beige", ("beige_wall_001", 3f, Color.white, 0.12f) },
            { "panete_verde", ("painted_plaster_wall", 3f, new Color(0.62f, 0.82f, 0.66f), 0.12f) },
            { "panete_salmon", ("painted_plaster_wall", 3f, new Color(1f, 0.7f, 0.6f), 0.12f) },
            { "concreto", ("dirty_concrete", 3f, new Color(0.85f, 0.85f, 0.83f), 0.15f) },
            { "lamina", ("corrugated_iron", 1.5f, new Color(0.72f, 0.74f, 0.76f), 0.35f) },
            { "poste", ("dirty_concrete", 2f, new Color(0.82f, 0.82f, 0.8f), 0.2f) },
        };
        static readonly System.Collections.Generic.Dictionary<string, (Color c, float liso, float metal, float nits)> CALLE_LISO =
            new System.Collections.Generic.Dictionary<string, (Color, float, float, float)>
        {
            { "charco", (new Color(0.02f, 0.02f, 0.025f), 0.97f, 0f, 0f) },
            { "marco", (new Color(0.12f, 0.11f, 0.10f), 0.3f, 0.2f, 0f) },
            { "vidrio_oscuro", (new Color(0.03f, 0.035f, 0.04f), 0.93f, 0f, 0f) },
            { "vidrio_luz", (new Color(0.9f, 0.75f, 0.5f), 0.8f, 0f, 260f) },              // ventanas y locales con la luz prendida a las 6:50
            { "reja", (new Color(0.05f, 0.05f, 0.05f), 0.4f, 0.6f, 0f) },
            { "puerta", (new Color(0.25f, 0.27f, 0.3f), 0.35f, 0.5f, 0f) },
            { "tierra", (new Color(0.12f, 0.09f, 0.06f), 0.15f, 0f, 0f) },
            { "pasto", (new Color(0.12f, 0.2f, 0.08f), 0.1f, 0f, 0f) },
            { "tronco", (new Color(0.15f, 0.11f, 0.08f), 0.1f, 0f, 0f) },
            { "follaje", (new Color(0.07f, 0.13f, 0.06f), 0.15f, 0f, 0f) },
            { "cable", (new Color(0.02f, 0.02f, 0.02f), 0.3f, 0f, 0f) },
            { "luminaria", (new Color(1f, 0.62f, 0.28f), 0.6f, 0f, 900f) },                // sodio, todavía prendida al amanecer
            { "metal_gris", (new Color(0.35f, 0.36f, 0.38f), 0.45f, 0.7f, 0f) },
            { "plastico_negro", (new Color(0.03f, 0.03f, 0.03f), 0.35f, 0f, 0f) },
            { "toldo_rojo", (new Color(0.5f, 0.06f, 0.05f), 0.2f, 0f, 0f) },
            { "toldo_verde", (new Color(0.08f, 0.3f, 0.12f), 0.2f, 0f, 0f) },
            { "toldo_azul", (new Color(0.06f, 0.15f, 0.42f), 0.2f, 0f, 0f) },
            { "toldo_amarillo", (new Color(0.75f, 0.58f, 0.05f), 0.2f, 0f, 0f) },
            { "aviso_rojo", (new Color(0.62f, 0.07f, 0.06f), 0.5f, 0f, 0f) },
            { "aviso_azul", (new Color(0.05f, 0.2f, 0.55f), 0.5f, 0f, 0f) },
            { "aviso_blanco", (new Color(0.85f, 0.85f, 0.82f), 0.5f, 0f, 0f) },
            { "aviso_amarillo", (new Color(0.85f, 0.65f, 0.05f), 0.5f, 0f, 0f) },
            { "aviso_verde", (new Color(0.05f, 0.4f, 0.18f), 0.5f, 0f, 0f) },
            { "letra_blanca", (new Color(0.92f, 0.92f, 0.9f), 0.4f, 0f, 0f) },
            { "letra_negra", (new Color(0.04f, 0.04f, 0.04f), 0.4f, 0f, 0f) },
            { "teja", (new Color(0.42f, 0.17f, 0.10f), 0.3f, 0f, 0f) },                   // tejado de barro
            { "pintura_blanca", (new Color(0.8f, 0.8f, 0.78f), 0.45f, 0f, 0f) },          // cebra
            { "semaforo", (new Color(0.06f, 0.06f, 0.05f), 0.4f, 0.3f, 0f) },
            { "luz_roja", (new Color(1f, 0.08f, 0.05f), 0.7f, 0f, 600f) },
            { "luz_verde", (new Color(0.1f, 1f, 0.45f), 0.7f, 0f, 600f) },
            { "malla", (new Color(0.3f, 0.32f, 0.33f), 0.4f, 0.6f, 0f) },
        };

        static Texture2D TexturaCalle(string tex, string mapa, bool normal)
        {
            string dir = Path.Combine(POLYHAVEN, tex);
            string origen = Directory.Exists(dir) ? Directory.GetFiles(dir, $"{tex}_{mapa}_2k.*").FirstOrDefault() : null;
            if (origen == null) return null;
            string destino = $"{ART}/Calle/Texturas/{Path.GetFileName(origen)}";
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            if (!File.Exists(destino)) { File.Copy(origen, destino); AssetDatabase.ImportAsset(destino); }
            if (normal && AssetImporter.GetAtPath(destino) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
            { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
        }

        static void MaterialesCalle(GameObject calle, Shader lit)
        {
            Directory.CreateDirectory(ART + "/Calle");
            var hechos = new System.Collections.Generic.Dictionary<string, Material>();
            Material Para(string nombre)
            {
                string n = nombre.Split('.')[0];
                if (hechos.TryGetValue(n, out var ya)) return ya;
                string ruta = $"{ART}/Calle/{n}.mat";
                var mt = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (!mt) { mt = new Material(lit); AssetDatabase.CreateAsset(mt, ruta); }
                mt.shader = lit;
                if (CALLE_PBR.TryGetValue(n, out var p))
                {
                    mt.SetTexture("_BaseColorMap", TexturaCalle(p.tex, "color", false));
                    var nor = TexturaCalle(p.tex, "normal", true);
                    if (nor) { mt.SetTexture("_NormalMap", nor); mt.SetFloat("_NormalScale", 1f); }
                    mt.SetColor("_BaseColor", p.tinte); mt.SetFloat("_Smoothness", p.liso); mt.SetFloat("_Metallic", 0f);
                    mt.mainTextureScale = new Vector2(1f / p.tam, 1f / p.tam);
                    mt.SetTextureScale("_BaseColorMap", new Vector2(1f / p.tam, 1f / p.tam));
                }
                else if (CALLE_LISO.TryGetValue(n, out var q))
                {
                    mt.SetTexture("_BaseColorMap", null); mt.SetTexture("_NormalMap", null);
                    mt.SetColor("_BaseColor", q.c); mt.SetFloat("_Smoothness", q.liso); mt.SetFloat("_Metallic", q.metal);
                    if (q.nits > 0f) { mt.SetColor("_EmissiveColor", q.c * q.nits); mt.SetFloat("_UseEmissiveIntensity", 0f); }
                    else mt.SetColor("_EmissiveColor", Color.black);
                }
                HDMaterial.ValidateMaterial(mt); EditorUtility.SetDirty(mt);
                return hechos[n] = mt;
            }
            foreach (var rr in calle.GetComponentsInChildren<Renderer>(true))
                rr.sharedMaterials = rr.sharedMaterials.Select(m => m ? Para(m.name) : m).ToArray();
            Debug.Log($"[Prologo] calle: {hechos.Count} materiales ({string.Join(", ", hechos.Keys)})");
        }

        /// <summary>Un plano sobre la cara de la pantalla del celular (la malla no trae UV); PantallaCelular le dibuja el bloqueo.</summary>
        static void PantallaDelCelular(GameObject cel, Shader lit)
        {
            var mf = cel.GetComponent<MeshFilter>(); var mr = cel.GetComponent<MeshRenderer>();
            if (!mf || !mr) return;
            var me = mf.sharedMesh; int sub = System.Array.FindIndex(mr.sharedMaterials, m => m && m.name.Contains("pantalla"));
            if (sub < 0 || sub >= me.subMeshCount) { Debug.LogWarning("[Prologo] el celular no tiene cara de pantalla"); return; }
            var vs = me.vertices; var tri = me.GetTriangles(sub);
            var b = new Bounds(vs[tri[0]], Vector3.zero); foreach (var i in tri) b.Encapsulate(vs[i]);
            int fino = b.size.x < b.size.y ? (b.size.x < b.size.z ? 0 : 2) : (b.size.y < b.size.z ? 1 : 2);
            Vector3 n = Vector3.zero; n[fino] = Mathf.Sign(b.center[fino] - me.bounds.center[fino]); if (n == Vector3.zero) n[fino] = 1f;
            int a1 = (fino + 1) % 3, a2 = (fino + 2) % 3; int largo = b.size[a1] > b.size[a2] ? a1 : a2, ancho = largo == a1 ? a2 : a1;
            Vector3 u = Vector3.zero; u[ancho] = b.extents[ancho] * 0.96f; Vector3 v = Vector3.zero; v[largo] = b.extents[largo] * 0.96f;
            Vector3 c = b.center + n * (b.extents[fino] + 0.0006f);
            var q = new Mesh { name = "Pantalla_Celular" };
            q.vertices = new[] { c - u - v, c + u - v, c + u + v, c - u + v };
            q.uv = new[] { new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) };   // volteada: se veía en espejo
            bool hacia = Vector3.Dot(Vector3.Cross(u, v), n) > 0f;
            q.triangles = hacia ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
            q.RecalculateNormals(); q.RecalculateBounds();
            AssetDatabase.DeleteAsset(ART + "/Pantalla_Celular.asset"); AssetDatabase.CreateAsset(q, ART + "/Pantalla_Celular.asset");
            var go = new GameObject("Pantalla_Celular", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(cel.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = q;
            string ruta = ART + "/Materiales/Pantalla_Celular.mat";
            var mt = AssetDatabase.LoadAssetAtPath<Material>(ruta); if (!mt) { mt = new Material(lit); AssetDatabase.CreateAsset(mt, ruta); }
            mt.SetColor("_BaseColor", Color.black); mt.SetFloat("_Smoothness", 0.92f); mt.SetFloat("_DoubleSidedEnable", 1f);
            var img = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "/Texturas/pantalla_celular.png");   // pantalla_celular.py (Blender)
            mt.SetTexture("_EmissiveColorMap", img); mt.SetTexture("_BaseColorMap", img); mt.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.08f));
            mt.SetColor("_EmissiveColor", Color.white * 140f); mt.SetFloat("_UseEmissiveIntensity", 0f);
            HDMaterial.ValidateMaterial(mt);
            go.GetComponent<MeshRenderer>().sharedMaterial = mt;
            go.AddComponent<PantallaCelular>();
            Debug.Log($"[Prologo] pantalla del celular: {b.size:F3} (normal eje {fino})");
        }

        /// <summary>La pantalla del radio del Corolla: un plano sobre su cara con la emisora (pantalla_radio.py); en el presagio dice "Buscando…".</summary>
        static void PantallaDeLaRadio(GameObject auto, Shader lit)
        {
            var mr = auto.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(rr => rr.name.Contains("dashboard.001_screen"));
            if (!mr) { Debug.LogWarning("[Prologo] no encontré la pantalla del radio"); return; }
            var me = mr.GetComponent<MeshFilter>().sharedMesh; var b = me.bounds;
            int fino = b.size.x < b.size.y ? (b.size.x < b.size.z ? 0 : 2) : (b.size.y < b.size.z ? 1 : 2);
            // la cara que mira a la cabina (hacia los ojos de Mateo, que van atrás)
            var ojos = auto.GetComponentsInChildren<Transform>(true).First(t => t.name.StartsWith("Marca_Ojos")).position;
            Vector3 n = Vector3.zero; n[fino] = 1f;
            if (Vector3.Dot(mr.transform.TransformDirection(n), ojos - mr.bounds.center) < 0f) n = -n;
            int a1 = (fino + 1) % 3, a2 = (fino + 2) % 3;
            Vector3 cu = Vector3.zero, cv = Vector3.zero;
            // horizontal = el eje que más se parece al "derecha" del mundo; vertical = el otro
            Vector3 derechaLocal = mr.transform.InverseTransformDirection(Vector3.Cross(Vector3.up, (ojos - mr.bounds.center).normalized)).normalized;
            int ancho = Mathf.Abs(derechaLocal[a1]) > Mathf.Abs(derechaLocal[a2]) ? a1 : a2, alto = ancho == a1 ? a2 : a1;
            cu[ancho] = b.extents[ancho] * 0.95f; cv[alto] = b.extents[alto] * 0.95f;
            Vector3 c = b.center + n * (b.extents[fino] + 0.0008f);
            var q = new Mesh { name = "Pantalla_Radio" };
            q.vertices = new[] { c - cu - cv, c + cu - cv, c + cu + cv, c - cu + cv };
            q.uv = new[] { new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 0) };   // girada 180°: se veía de cabeza
            q.triangles = Vector3.Dot(Vector3.Cross(cu, cv), n) > 0f ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
            q.RecalculateNormals(); q.RecalculateBounds();
            AssetDatabase.DeleteAsset(ART + "/Pantalla_Radio.asset"); AssetDatabase.CreateAsset(q, ART + "/Pantalla_Radio.asset");
            var go = new GameObject("Pantalla_Radio", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(mr.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = q;
            string ruta = ART + "/Materiales/Pantalla_Radio.mat";
            var mt = AssetDatabase.LoadAssetAtPath<Material>(ruta); if (!mt) { mt = new Material(lit); AssetDatabase.CreateAsset(mt, ruta); }
            var em = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "/Texturas/radio_emisora.png");
            mt.SetTexture("_EmissiveColorMap", em); mt.SetTexture("_BaseColorMap", em); mt.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.06f));
            mt.SetColor("_EmissiveColor", Color.white * 1200f); mt.SetFloat("_UseEmissiveIntensity", 0f); mt.SetFloat("_Smoothness", 0.9f); mt.SetFloat("_DoubleSidedEnable", 1f);
            HDMaterial.ValidateMaterial(mt);
            go.GetComponent<MeshRenderer>().sharedMaterial = mt;
            var pr = go.AddComponent<PantallaRadio>(); pr.emisora = em; pr.buscando = AssetDatabase.LoadAssetAtPath<Texture2D>(ART + "/Texturas/radio_buscando.png");
            Debug.Log($"[Prologo] pantalla del radio en {mr.name} ({b.size:F3})");
        }

        /// <summary>Luz de la mañana nublada (fácil de ajustar aquí).</summary>
        public static class PROLOGO
        {
            public const float cieloLux = 1800f, solLux = 300f, ev100 = 12.3f, nieblaMetros = 110f, cieloEV = 12.2f;   // aguacero: se ve hasta ~110 m
        }

        public static void ArmarBatch()
        {
            try { Armar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Prologo] " + e); EditorApplication.Exit(1); }
        }
    }
}
