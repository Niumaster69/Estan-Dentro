using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EstanDentro.Colegio;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Arreglos sobre la escena del colegio ya armada (sin reconstruirla), pedidos al jugarla (19-oct-2026):
    /// - Sillas, bancas y pupitres se pueden empujar (antes eran cajas fijas y no se podía pasar entre las filas).
    /// - El tablero del 4°B era casi negro y no invitaba a mirarlo: verde de tiza gastado y lo del día escrito con tiza.
    /// - Los sonidos del colegio (SonidosColegio).
    /// Batch: -executeMethod EstanDentro.EditorColegio.ArreglosColegio.Batch
    /// </summary>
    public static class ArreglosColegio
    {
        const string ESCENA = "Assets/Scenes/Colegio/Colegio.unity";
        static readonly Rect SALON = Rect.MinMaxRect(19.33f, 29f, 28.67f, 36f);    // 4°B (x, z), piso 3

        /// <summary>Lista lo que hay en el 4°B (para decidir qué fila quitar).</summary>
        public static void Listar()
        {
            EditorSceneManager.OpenScene(ESCENA);
            foreach (var d in Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = d.transform.position; if (!(SALON.Contains(new Vector2(p.x, p.z)) && p.y > 6.5f && p.y < 11f)) continue;
                var c = d.GetComponent<Collider>();
                string tipo = d.Get("prop_tipo"), caja = c ? c.bounds.size.ToString("F2") : "-";
                Debug.Log($"[Salon] {tipo} {d.name} activo={d.gameObject.activeSelf} pos={p:F2} caja={caja}");
            }
            foreach (var pc in Object.FindObjectsByType<Player.PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Debug.Log($"[Salon] JUGADOR {pc.transform.position:F2} mira {pc.transform.forward:F2}");
            EditorApplication.Exit(0);
        }

        public static void Batch()
        {
            ObjetosSueltos.Preparar();
            MaterialesTextos();                         // modelos de lo que se recoge (se ve dentro de casilleros, lonchera, cajones)
            var esc = EditorSceneManager.OpenScene(ESCENA);
            int sillas = SillasEmpujables(), tiza = Tablero();
            Salon4B();
            JugadorDelgado();
            MochilaALosPies();
            SonidosColegio.Aplicar();
            LimitesNivel1();
            Escalada4B();
            RutaRectoria();
            Piso3();
            MaterialesPesadilla();
            PapelesApoyados();
            CajonProfe();
            EstanDentro.EditorMejoras.MejorasEscena.Aplicar();   // rama de mejoras (Duvan + Claude, 30-oct): bucle del pasillo, etc.
            ConstruirColegio.PerfilColegio();                 // el ambiente (niebla, exposición, colores) no estaba guardado en el archivo
            EditorSceneManager.SaveScene(esc);
            Debug.Log($"[Arreglos] sillas quitadas para dejar pasillos={-sillas} tablero={tiza}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Las sillas son fijas como todo lo demás (Henry: mejor quitar sillas que moverlas). Se dejan pasillos de al menos
        /// 70 cm: una silla o pupitre cuya caja (agrandada 35 cm por lado) toque la de otra que ya se quedó, se desactiva
        /// (no se borra: se puede volver a prender). Se recorren de adelante hacia atrás de cada salón.
        /// </summary>
        static int SillasEmpujables()
        {
            var asientos = Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(d => { string t = d.Get("prop_tipo"); return !string.IsNullOrEmpty(t) && (t == "pupitre" || t.StartsWith("silla") || t.StartsWith("banca") || t == "taburete"); })
                .Where(d => d.GetComponent<Collider>()).ToList();
            foreach (var d in asientos)
            {
                var rb = d.GetComponent<Rigidbody>(); if (rb) Object.DestroyImmediate(rb);      // vuelven a ser fijas
                d.gameObject.SetActive(true);
            }
            // los que tienen algo encima (nota, pista, objeto para tocar) no se quitan nunca y van primero
            var cosas = Object.FindObjectsByType<EstanDentro.Interaction.Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                              .Where(i => !asientos.Any(a => a.gameObject == i.gameObject)).Select(i => i.transform.position).ToList();
            // y el pupitre donde despierta Mateo
            foreach (var pc in Object.FindObjectsByType<Player.PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) cosas.Add(pc.transform.position + Vector3.down * 0.3f);
            bool ConAlgo(DatosBlender d) { var b = d.GetComponent<Collider>().bounds; b.Expand(new Vector3(0.1f, 1.2f, 0.1f)); return cosas.Any(c => b.Contains(c)); }
            var quedan = new System.Collections.Generic.List<Bounds>();
            int quitadas = 0;
            foreach (var d in asientos.OrderByDescending(ConAlgo).ThenBy(d => Mathf.Round(d.transform.position.y)).ThenBy(d => d.transform.position.x).ThenBy(d => d.transform.position.z))
            {
                var b = d.GetComponent<Collider>().bounds;
                var grande = new Bounds(b.center, b.size + new Vector3(0.70f, 0f, 0.70f));
                if (!ConAlgo(d) && quedan.Any(q => q.Intersects(grande))) { d.gameObject.SetActive(false); quitadas++; continue; }
                quedan.Add(b);
            }
            return -quitadas;
        }

        /// <summary>La mochila en el piso, junto a los pies de Mateo (al despertar no se notaba dónde estaba).</summary>
        static void MochilaALosPies()
        {
            var moch = Object.FindObjectsByType<MochilaEquipable>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            var jug = Object.FindObjectsByType<Player.PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (!moch || !jug) { Debug.LogWarning("[Arreglos] no encontré la mochila o al jugador"); return; }
            Vector3 adelante = Vector3.ProjectOnPlane(jug.transform.forward, Vector3.up).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
            // al lado derecho del pupitre, un poco adelante de los pies: la tabla del pupitre no la tapa
            Vector3 p = jug.transform.position + adelante * 0.35f + derecha * 0.5f + Vector3.up * 1.5f;
            float suelo = Physics.Raycast(p, Vector3.down, out var h, 4f) ? h.point.y : jug.transform.position.y - 0.9f;
            var r = moch.GetComponentInChildren<Renderer>();
            float bajo = r ? moch.transform.position.y - r.bounds.min.y : 0f;              // apoyada, no flotando ni enterrada
            moch.transform.position = new Vector3(p.x, suelo + bajo + 0.002f, p.z);
            moch.transform.rotation = Quaternion.LookRotation(-derecha, Vector3.up) * Quaternion.Euler(0f, 18f, 0f);
            Debug.Log($"[Arreglos] mochila en {moch.transform.position:F2} (jugador en {jug.transform.position:F2})");
        }

        /// <summary>
        /// 4°B: entre el escritorio del profe y la primera fila quedaban 61 cm. Se quita la fila del fondo (junto a las ventanas)
        /// y las demás se corren 55 cm hacia el fondo, con lo que tienen encima. El pupitre de Mateo y Mateo se corren con ellas.
        /// Solo se hace una vez (marca: el objeto "Arreglo_4B").
        /// </summary>
        static void Salon4B()
        {
            if (GameObject.Find("Arreglo_4B")) return;
            const float CORRER = 0.55f;
            var pupitres = Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(d => d.Get("prop_tipo") == "pupitre" && SALON.Contains(new Vector2(d.transform.position.x, d.transform.position.z)) && d.transform.position.y > 6.5f && d.transform.position.y < 11f).ToList();
            if (pupitres.Count == 0) return;
            float fondo = pupitres.Max(d => d.transform.position.x);
            // lo que está encima de cada pupitre (notas, papeles, objetos) se mueve con él
            var sueltos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => (t.GetComponent<DatosBlender>() || t.GetComponent<EstanDentro.Interaction.Interactable>()) && !pupitres.Any(pp => pp.transform == t)).ToList();
            var mover = new System.Collections.Generic.HashSet<Transform>();
            int quitados = 0, corridos = 0;
            foreach (var d in pupitres)
            {
                var b = d.GetComponent<Collider>() ? d.GetComponent<Collider>().bounds : new Bounds(d.transform.position, Vector3.one * 0.7f);
                b.Expand(new Vector3(0.05f, 1.4f, 0.05f));
                var encima = sueltos.Where(t => b.Contains(t.position) && t.position.y > d.transform.position.y + 0.3f).ToList();
                if (Mathf.Abs(d.transform.position.x - fondo) < 0.3f && !encima.Any())
                { d.gameObject.SetActive(false); quitados++; continue; }
                mover.Add(d.transform); foreach (var t in encima) mover.Add(t);
                corridos++;
            }
            foreach (var t in mover.Where(t => !mover.Any(o => o != t && t.IsChildOf(o))))
                t.position += Vector3.right * CORRER;
            foreach (var pc in Object.FindObjectsByType<Player.PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                pc.transform.position += Vector3.right * CORRER;
            new GameObject("Arreglo_4B");
            Debug.Log($"[Arreglos] 4°B: fila del fondo quitada ({quitados}), {corridos} pupitres corridos {CORRER} m (con {mover.Count - corridos} cosas encima)");
        }

        /// <summary>
        /// Nivel 1 = 4°B → ductos → galería del primer piso → secretaría (pista) y rectoría (candado de cadena).
        /// Todo lo demás del primer piso y el sótano queda cerrado; la entrada principal tiene un portón cerrado por fuera.
        /// (Henry: "salir al patio ya es demasiado libre", "pude llegar al sótano", "por la puerta principal pude salir")
        /// </summary>
        public static void LimitesNivel1()
        {
            int cerradas = 0;
            foreach (var pu in Object.FindObjectsByType<EstanDentro.Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var r = pu.GetComponent<Renderer>(); Vector3 c = r ? r.bounds.center : pu.transform.position;
                if (c.y > 3.2f) continue;                                                      // pisos 2 y 3: no se llega
                bool rectoria = c.x > 10f && c.x < 19.33f && c.z > 6f && c.z < 8f;
                bool secretaria = c.x > 28.67f && c.x < 38f && c.z > 6f && c.z < 8f;
                if (rectoria || secretaria) continue;
                var so = new SerializedObject(pu);
                if (so.FindProperty("integratedLock").objectReferenceValue != null) continue;   // las que tienen su candado
                so.FindProperty("isLocked").boolValue = true;
                so.FindProperty("lockedHudMessage").stringValue = c.y < -1f ? "Cerrada. Del sótano no sale nada bueno." : "Está cerrada con llave.";
                so.FindProperty("forceBreathingSeconds").floatValue = 0f;                      // tocar una puerta no abre la respiración
                so.FindProperty("stressOnLockedTry").floatValue = 3f;
                so.ApplyModifiedPropertiesWithoutUndo();
                cerradas++;
            }
            // portón de la entrada principal (hueco de 3 m en la fachada sur, x 22,5..25,5)
            var viejo = GameObject.Find("Porton_Principal"); if (viejo) Object.DestroyImmediate(viejo);
            var porton = new GameObject("Porton_Principal");
            var madera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Edificio/Materials/ED_madera_marco.mat")
                         ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Edificio/Materials/ED_metal_verde.mat");
            for (int i = 0; i < 2; i++)
            {
                var hoja = GameObject.CreatePrimitive(PrimitiveType.Cube); hoja.name = "Hoja" + i;
                hoja.transform.SetParent(porton.transform, false);
                hoja.transform.position = new Vector3(22.5f + 0.75f + i * 1.5f, 1.4f, 0.0f);
                hoja.transform.localScale = new Vector3(1.48f, 2.8f, 0.06f);
                if (madera) hoja.GetComponent<Renderer>().sharedMaterial = madera;
                var b = hoja.AddComponent<Bloqueo>(); b.accion = "Empujar el portón";
                b.mensaje = "La puerta principal está cerrada con cadena por fuera.";
            }
            Debug.Log($"[Arreglos] nivel 1: {cerradas} puertas cerradas con llave + portón principal");
        }

        /// <summary>
        /// La rejilla del 4°B se alcanza subiéndose al casillero que está debajo, con una silla arrastrada hasta él.
        /// Las demás rejillas no llevan a ningún lado en este nivel: dejan de ser interactuables. La linterna suelta del
        /// escritorio del profe se quita (la de verdad está dentro de la lonchera).
        /// </summary>
        static void Escalada4B()
        {
            const float PISO = 7.22f;
            var rejillas = Object.FindObjectsByType<RejillaDucto>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RejillaDucto rej = rejillas.FirstOrDefault(x => !string.IsNullOrEmpty(x.escenaDestino) && x.name != "REJILLA_P1_RECT");
            if (rej) rej.salidaEn = "rectoria_dentro";                // del 4°B se cae dentro de la rectoría (sin recargar el colegio)
            int quitadas = 0;
            foreach (var x in rejillas) if (x != rej && x.name != "REJILLA_P1_RECT") { Object.DestroyImmediate(x); quitadas++; }
            foreach (var d in Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (d.Get("prop_tipo") == "linterna" && SALON.Contains(new Vector2(d.transform.position.x, d.transform.position.z)) && d.transform.position.y > 6.5f)
                    d.gameObject.SetActive(false);
            if (!rej) { Debug.LogWarning("[Arreglos] sin rejilla del 4°B"); return; }
            Vector3 rp = rej.transform.position;
            var cas = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .Where(d => (d.Get("prop_tipo") ?? "").StartsWith("casillero") && d.GetComponent<Renderer>() && Mathf.Abs(d.transform.position.y - PISO) < 0.5f)
                .OrderBy(d => Vector2.Distance(new Vector2(d.GetComponent<Renderer>().bounds.center.x, d.GetComponent<Renderer>().bounds.center.z), new Vector2(rp.x, rp.z)))
                .FirstOrDefault();
            if (!cas) { Debug.LogWarning("[Arreglos] sin casillero bajo la rejilla"); return; }
            var b = cas.GetComponent<Renderer>().bounds;
            // el frente del casillero: hacia el centro del salón, en el eje más cercano
            Vector3 alCentro = new Vector3(SALON.center.x - b.center.x, 0f, SALON.center.y - b.center.z);
            Vector3 frente = Mathf.Abs(alCentro.x) > Mathf.Abs(alCentro.z) ? new Vector3(Mathf.Sign(alCentro.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(alCentro.z));
            float medioFondo = Mathf.Abs(frente.x) > 0 ? b.extents.x : b.extents.z;
            var viejo = GameObject.Find("Trepar_Casillero"); if (viejo) Object.DestroyImmediate(viejo);
            var trepar = new GameObject("Trepar_Casillero");
            trepar.transform.position = new Vector3(b.center.x, b.max.y - 0.15f, b.center.z) + frente * (medioFondo + 0.06f);
            trepar.transform.rotation = Quaternion.LookRotation(frente);
            var bc = trepar.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(Mathf.Abs(frente.x) > 0 ? b.size.z : b.size.x, 0.4f, 0.12f);
            var es = trepar.AddComponent<Escalada>();
            es.arriba = new Vector3(b.center.x, b.max.y, b.center.z); es.pisoSalon = PISO; es.pisoMinimo = 0.3f;
            // CUALQUIER silla del salón se agarra y se lleva hasta el casillero (no hay silla copiada ni que se mueva sola)
            var viejaS = GameObject.Find("Silla_Suelta"); if (viejaS) Object.DestroyImmediate(viejaS);
            var sillas = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .Where(d => { var t = d.Get("prop_tipo") ?? ""; var q = d.transform.position; return t == "pupitre" && d.gameObject.activeInHierarchy && d.GetComponent<Collider>()
                              && SALON.Contains(new Vector2(q.x, q.z)) && Mathf.Abs(q.y - PISO) < 0.5f; })
                .OrderBy(d => Vector3.Distance(d.transform.position, b.center)).ToList();      // los PUPITRES de los estudiantes (la silla del profe se queda en su sitio)
            foreach (var silla in sillas)
            {
                foreach (var c in silla.GetComponents<EstanDentro.Interaction.Interactable>()) if (!(c is SillaArrastrable)) Object.DestroyImmediate(c);
                var sa = silla.GetComponent<SillaArrastrable>(); if (!sa) sa = silla.gameObject.AddComponent<SillaArrastrable>();
                sa.destino = new Vector3(b.center.x, silla.transform.position.y, b.center.z) + frente * (medioFondo + 0.55f);   // (el pupitre es más grande que una silla)
                sa.rumbo = Quaternion.LookRotation(-frente).eulerAngles.y; sa.nombre = "el pupitre";
                var col = silla.GetComponent<Collider>();
                sa.alturaAsiento = col ? Mathf.Clamp(col.bounds.max.y - silla.transform.position.y, 0.4f, 0.75f) : 0.46f;
            }
            var silla0 = sillas.FirstOrDefault();
            PintaAmarilla(b, frente, medioFondo, sillas.Take(1), cas.transform);   // pintura amarilla: por aquí se sube (como en The Last of Us)
            CrearSpawn("4b_desde_ducto", new Vector3(b.center.x, PISO + 0.05f, b.center.z) + frente * (medioFondo + 0.9f), Quaternion.LookRotation(-Vector3.Cross(Vector3.up, frente)));
            Debug.Log($"[Arreglos] escalada 4°B: {sillas.Count} sillas que se agarran -> casillero {cas.name} (arriba a {b.max.y:F2}), rejilla {rej.name}; {quitadas} rejillas sin salida quitadas");
        }

        /// <summary>
        /// El cajón de arriba de la cajonera del escritorio del profe (4°B) sale de verdad; la nota "del cajón" queda ADENTRO
        /// como un papel que se ve y se lee aparte. El escritorio deja de ser una nota.
        /// </summary>
        static void CajonProfe()
        {
            var esc = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .FirstOrDefault(d => d.Get("prop_tipo") == "escritorio_profesor" && SALON.Contains(new Vector2(d.transform.position.x, d.transform.position.z)) && d.transform.position.y > 6.5f);
            if (!esc) { Debug.LogWarning("[Arreglos] sin escritorio del profe en el 4°B"); return; }
            var nota = esc.GetComponent<EstanDentro.Interaction.Note>();
            GameObject temporal = null;                 // si ya se corrió antes, la nota está en el cajón viejo: se rescata
            var notaVieja = GameObject.Find("Nota_Cajon_Profe");
            if (!nota && notaVieja && notaVieja.GetComponent<EstanDentro.Interaction.Note>())
            { temporal = new GameObject("tmp_nota"); nota = temporal.AddComponent<EstanDentro.Interaction.Note>(); EditorUtility.CopySerialized(notaVieja.GetComponent<EstanDentro.Interaction.Note>(), nota); }
            var r = esc.GetComponent<Renderer>(); var b = r.bounds;
            float piso = b.min.y;
            // la cajonera está del lado del profesor (hacia el tablero, x menor) y hacia z menor (ver foto en Blender)
            float cara = b.min.x, z0 = b.min.z + 0.06f, z1 = z0 + 0.42f;
            var madera = r.sharedMaterials.FirstOrDefault(m => m);
            var negro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Edificio/Materials/ED_plastico_negro.mat");
            foreach (var g in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t && t.name.StartsWith("Cajon_Profe")).Select(t => t.gameObject).ToList())
                if (g) Object.DestroyImmediate(g);
            // los tres cajones de la cajonera: arriba la nota, en el del medio unas pilas, abajo nada
            var alturas = new[] { (0.50f, 0.69f, "nota"), (0.29f, 0.48f, "baterias"), (0.06f, 0.27f, "") };
            for (int k = 0; k < alturas.Length; k++)
            {
                float y0 = piso + alturas[k].Item1, y1 = piso + alturas[k].Item2; string dentro = alturas[k].Item3;
                var hueco = GameObject.CreatePrimitive(PrimitiveType.Quad); hueco.name = "Cajon_Profe_Hueco" + k;
                Object.DestroyImmediate(hueco.GetComponent<Collider>());
                hueco.transform.position = new Vector3(cara - 0.003f, (y0 + y1) / 2f, (z0 + z1) / 2f);
                hueco.transform.rotation = Quaternion.LookRotation(Vector3.right); hueco.transform.localScale = new Vector3(z1 - z0 - 0.02f, y1 - y0 - 0.02f, 1f);
                if (negro) hueco.GetComponent<Renderer>().sharedMaterial = negro;
                var caj = new GameObject("Cajon_Profe" + k);
                caj.transform.position = new Vector3(cara, y0, (z0 + z1) / 2f);
                GameObject Tabla(string n, Vector3 centroLocal, Vector3 tam)
                {
                    var t = GameObject.CreatePrimitive(PrimitiveType.Cube); t.name = n;
                    Object.DestroyImmediate(t.GetComponent<Collider>());
                    t.transform.SetParent(caj.transform, false); t.transform.localPosition = centroLocal; t.transform.localScale = tam;
                    if (madera) t.GetComponent<Renderer>().sharedMaterial = madera;
                    return t;
                }
                float ancho = z1 - z0, alto = y1 - y0, fondo = 0.42f;
                Tabla("Frente", new Vector3(-0.011f, alto / 2f, 0f), new Vector3(0.02f, alto, ancho));
                Tabla("Fondo", new Vector3(fondo / 2f, 0.01f, 0f), new Vector3(fondo, 0.012f, ancho - 0.03f));
                Tabla("LadoA", new Vector3(fondo / 2f, alto * 0.4f, ancho / 2f - 0.02f), new Vector3(fondo, alto * 0.8f, 0.012f));
                Tabla("LadoB", new Vector3(fondo / 2f, alto * 0.4f, -ancho / 2f + 0.02f), new Vector3(fondo, alto * 0.8f, 0.012f));
                Tabla("Tirador", new Vector3(-0.03f, alto * 0.6f, 0f), new Vector3(0.02f, 0.02f, 0.12f));
                var bc = caj.AddComponent<BoxCollider>(); bc.center = new Vector3(-0.011f, alto / 2f, 0f); bc.size = new Vector3(0.04f, alto, ancho);
                var cj = caj.AddComponent<Cajon>(); cj.salida = new Vector3(-0.34f, 0f, 0f);
                if (dentro == "nota" && nota)
                {
                    var molde = Resources.Load<GameObject>("Objetos/papel");
                    var papel = molde ? (GameObject)PrefabUtility.InstantiatePrefab(molde) : GameObject.CreatePrimitive(PrimitiveType.Quad);
                    papel.name = "Nota_Cajon_Profe";
                    if (molde) PrefabUtility.UnpackPrefabInstance(papel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    papel.transform.SetParent(caj.transform, false);
                    papel.transform.localPosition = new Vector3(0.16f, 0.02f, 0f); papel.transform.localRotation = Quaternion.Euler(0f, 80f, 0f);
                    var pb = papel.GetComponent<BoxCollider>(); if (!pb) pb = papel.AddComponent<BoxCollider>();
                    pb.center = new Vector3(0f, 0.03f, 0f); pb.size = new Vector3(0.30f, 0.08f, 0.34f);          // zona de toque generosa
                    var nn = papel.AddComponent<EstanDentro.Interaction.Note>(); EditorUtility.CopySerialized(nota, nn);
                    var so = new SerializedObject(nn); so.FindProperty("disappearAfterRead").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
                }
                else if (dentro == "baterias")
                {
                    var molde = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Objetos/baterias.prefab");
                    if (molde)
                    {
                        var bt = (GameObject)PrefabUtility.InstantiatePrefab(molde); bt.name = "Pilas_Cajon";
                        PrefabUtility.UnpackPrefabInstance(bt, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        bt.transform.SetParent(caj.transform, false); bt.transform.localPosition = new Vector3(0.18f, 0.016f, 0.02f);
                        var bx = bt.AddComponent<BoxCollider>(); bx.center = new Vector3(0f, 0.03f, 0f); bx.size = new Vector3(0.22f, 0.08f, 0.22f);
                        var rg = bt.AddComponent<Recogible>(); rg.itemId = "baterias"; rg.nombreVisible = "Pilas";
                    }
                }
            }
            if (nota) Object.DestroyImmediate(nota);
            if (temporal) Object.DestroyImmediate(temporal);
            Debug.Log($"[Arreglos] cajón del profe en {esc.name} (cara x={cara:F2}, z {z0:F2}..{z1:F2})");
        }

        /// <summary>Materiales de las letras a mano (Resources/Textos/*.png de textos_pared.py): Lit transparente, se ven
        /// solo con luz (de noche, con la linterna).</summary>
        static void MaterialesTextos()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Textos" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g); string clave = System.IO.Path.GetFileNameWithoutExtension(ruta);
                if (AssetImporter.GetAtPath(ruta) is TextureImporter ti && (!ti.alphaIsTransparency || ti.mipmapEnabled == false))
                { ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
                string rm = $"Assets/Resources/Textos/Mat_{clave}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(rm);
                if (!m) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, rm); }
                m.SetTexture("_BaseColorMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ruta));
                m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.05f); m.SetFloat("_Metallic", 0f);
                m.SetFloat("_SurfaceType", 1f); m.SetFloat("_BlendMode", 0f); m.SetFloat("_DoubleSidedEnable", 0f);
                m.SetFloat("_TransparentDepthPrepassEnable", 0f); m.SetFloat("_EnableFogOnTransparent", 1f);
                UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        static void CrearSpawn(string id, Vector3 pos, Quaternion rot)
        {
            var viejo = GameObject.Find("SpawnPoint_" + id); if (viejo) Object.DestroyImmediate(viejo);
            var g = new GameObject("SpawnPoint_" + id); g.transform.SetPositionAndRotation(pos, rot);
            var sp = g.AddComponent<EstanDentro.Player.PlayerSpawnPoint>();
            var so = new SerializedObject(sp);
            so.FindProperty("id").stringValue = id; so.FindProperty("isDefault").boolValue = false; so.FindProperty("skipWakeUp").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Rectoría (Henry 20-oct: 4°B → rectoría → 4°B): Mateo cae por la rejilla de encima de los archivadores (ya está abierta,
        /// se cayó con él). Sobre el escritorio: la llave del 4°B y una foto (pista del recuerdo). Para volver: una silla hasta
        /// los archivadores, subirse y meterse al ducto, que sale de nuevo en el 4°B.
        /// </summary>
        static void RutaRectoria()
        {
            var rej = Object.FindObjectsByType<RejillaDucto>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == "REJILLA_P1_RECT");
            if (!rej) { Debug.LogWarning("[Arreglos] sin la rejilla de la rectoría (correr ducto_rectoria.py en Blender)"); return; }
            rej.escenaDestino = ViajeDuctos.ESCENA_DUCTOS; rej.salidaEn = "4b_desde_ducto"; rej.requiereId = ""; rej.abierta = true;
            rej.tipCarga = "El aire sube. Arriba está el 4°B.";
            // el final viejo del capítulo estaba DENTRO de la rectoría: ahora se cae ahí a mitad de camino; el final es el aula de música
            foreach (var fin in Object.FindObjectsByType<EstanDentro.Interaction.EndGameTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(fin.gameObject);
            var mr = rej.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false;
            var arch = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .Where(d => d.Get("prop_tipo") == "archivador" && d.transform.position.x > 10f && d.transform.position.x < 19.4f && d.transform.position.z < 7.2f && d.transform.position.y < 1f).ToList();
            if (arch.Count == 0) { Debug.LogWarning("[Arreglos] sin archivadores en la rectoría"); return; }
            Bounds b = arch[0].GetComponent<Renderer>().bounds; foreach (var a in arch) b.Encapsulate(a.GetComponent<Renderer>().bounds);
            Vector3 frente = Vector3.left;                                                     // los archivadores miran al oeste (al salón)
            float medio = b.extents.x;
            if (mr)
            {
                // la rejilla que se cayó con Mateo, en el piso
                var viejaC = GameObject.Find("Rejilla_Rectoria_Caida"); if (viejaC) Object.DestroyImmediate(viejaC);
                var caida = new GameObject("Rejilla_Rectoria_Caida", typeof(MeshFilter), typeof(MeshRenderer));
                caida.GetComponent<MeshFilter>().sharedMesh = rej.GetComponent<MeshFilter>().sharedMesh;
                caida.GetComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                caida.transform.position = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z) + frente * (medio + 1.1f);
                caida.transform.rotation = rej.transform.rotation * Quaternion.Euler(0f, 0f, 17f);
                caida.transform.localScale = rej.transform.lossyScale;
            }
            CrearSpawn("rectoria_dentro", new Vector3(b.center.x, b.min.y + 0.05f, b.center.z) + frente * (medio + 0.7f), Quaternion.LookRotation(frente));
            var viejo = GameObject.Find("Trepar_Archivadores"); if (viejo) Object.DestroyImmediate(viejo);
            var trepar = new GameObject("Trepar_Archivadores");
            trepar.transform.position = new Vector3(b.center.x, b.max.y - 0.15f, b.center.z) + frente * (medio + 0.06f);
            var bc = trepar.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.12f, 0.4f, b.size.z);
            var es = trepar.AddComponent<Escalada>(); es.arriba = new Vector3(b.center.x, b.max.y, b.center.z); es.pisoSalon = b.min.y; es.pisoMinimo = 0.3f; es.agachado = false;
            var sillas = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .Where(d => (d.Get("prop_tipo") ?? "").StartsWith("silla") && d.transform.position.x > 10f && d.transform.position.x < 19.4f && d.transform.position.z < 7.2f && d.transform.position.y < 1f && d.GetComponent<Collider>())
                .OrderBy(d => Vector3.Distance(d.transform.position, b.center)).ToList();
            foreach (var silla in sillas)
            {
                foreach (var c in silla.GetComponents<EstanDentro.Interaction.Interactable>()) if (!(c is SillaArrastrable)) Object.DestroyImmediate(c);
                var sa = silla.GetComponent<SillaArrastrable>(); if (!sa) sa = silla.gameObject.AddComponent<SillaArrastrable>();
                sa.destino = new Vector3(b.center.x, silla.transform.position.y, b.center.z) + frente * (medio + 0.35f);
                sa.rumbo = Quaternion.LookRotation(-frente).eulerAngles.y;
                var col = silla.GetComponent<Collider>(); sa.alturaAsiento = col ? Mathf.Clamp(col.bounds.max.y - silla.transform.position.y, 0.4f, 0.75f) : 0.46f;
            }
            PintaAmarilla(b, frente, medio, sillas.Take(1));
            // en el escritorio: la llave del 4°B y la foto del recuerdo
            var esc = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .FirstOrDefault(d => d.Get("prop_tipo") == "escritorio_profesor" && d.transform.position.x > 10f && d.transform.position.x < 19.4f && d.transform.position.z < 7.2f && d.transform.position.y < 1f);
            if (esc)
            {
                var eb = esc.GetComponent<Renderer>().bounds;
                // la llave solo existe en la Pesadilla, y en otro lugar: en el piso, junto a la rejilla que se cayó con Mateo
                Vector3 pisoLlave = new Vector3(b.center.x, b.min.y + 0.003f, b.center.z) + frente * (medio + 1.55f) + Vector3.forward * 0.35f;
                Objeto("llaves", "Llave_4B", pisoLlave, 30f, g =>
                { var rg = g.AddComponent<Recogible>(); rg.itemId = "llave_4b"; rg.nombreVisible = "Llave del 4°B"; g.AddComponent<EstanDentro.Mente.SoloEnPesadilla>(); });
                Objeto("papel", "Foto_Dia_Familia", new Vector3(eb.center.x + 0.1f, eb.max.y + 0.003f, eb.center.z + 0.2f), -12f, g =>
                {
                    var n = g.AddComponent<EstanDentro.Interaction.Note>(); var so = new SerializedObject(n);
                    so.FindProperty("noteTitle").stringValue = "Una foto";
                    so.FindProperty("noteText").stringValue = "Una foto vieja, doblada en una esquina.\n\nPa con una guitarra prestada y yo, de nueve años, tapándome la cara.\nAtrás, a lápiz: «Día de la familia — Aula de música».\n\n…¿Por qué está esto aquí?";
                    so.FindProperty("disappearAfterRead").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
                    var gr = g.AddComponent<EstanDentro.Mente.GatilloRecuerdo>(); gr.tituloNota = "Una foto"; gr.resolverCon = "llave_4b";
                    gr.pensamiento = "¿Pa…? No… no, no, no. Otra vez no.";
                });
            }
            // la puerta del 4°B se abre con esa llave (desde adentro)
            int conLlave = 0;
            foreach (var pu in Object.FindObjectsByType<EstanDentro.Interaction.LockedDoor>(FindObjectsSortMode.None))
            {
                var c0 = pu.GetComponent<Renderer>() ? pu.GetComponent<Renderer>().bounds.center : pu.transform.position;
                if (c0.y > 6.5f && c0.y < 11f && c0.x > SALON.xMin - 0.5f && c0.x < SALON.xMax + 0.5f && c0.z > SALON.yMin - 0.5f && c0.z < SALON.yMax + 0.5f)
                { pu.llaveId = "llave_4b"; pu.llaveNombre = "la llave del 4°B"; EditorUtility.SetDirty(pu); conLlave++; }
            }
            Debug.Log($"[Arreglos] rectoría: rejilla abierta, {sillas.Count} sillas, llave del 4°B y foto en el escritorio; {conLlave} puertas del 4°B con llave");
        }

        static void Objeto(string molde, string nombre, Vector3 pos, float rumbo, System.Action<GameObject> conf)
        {
            var viejo = GameObject.Find(nombre); if (viejo) Object.DestroyImmediate(viejo);
            var m = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Resources/Objetos/{molde}.prefab"); if (!m) return;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(m); g.name = nombre;
            PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rumbo, 0f));
            var bx = g.AddComponent<BoxCollider>(); bx.center = new Vector3(0f, 0.03f, 0f); bx.size = new Vector3(0.25f, 0.08f, 0.3f);
            conf(g);
        }

        /// <summary>Pintura amarilla (la pista de "por aquí" de The Last of Us): un tarro volcado arriba del mueble, chorreones
        /// por su frente hasta un charco en el piso, y manchas en la silla que sirve para subir.</summary>
        static void PintaAmarilla(Bounds b, Vector3 frente, float medioFondo, IEnumerable<DatosBlender> sillas, Transform mueble = null)
        {
            string rm = "Assets/Art/Colegio_Extra/Pintura_Amarilla.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(rm);
            if (!mat) { mat = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(mat, rm); }
            mat.SetColor("_BaseColor", new Color(0.97f, 0.74f, 0.04f)); mat.SetFloat("_Smoothness", 0.82f); mat.SetFloat("_Metallic", 0f);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat); EditorUtility.SetDirty(mat);
            var lata = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Edificio/Materials/ED_acero.mat");
            string clave = $"Pintura_{b.center.x:F1}_{b.center.z:F1}";
            var vieja = GameObject.Find(clave); if (vieja) Object.DestroyImmediate(vieja);
            var raiz = new GameObject(clave);
            Vector3 lado = Vector3.Cross(Vector3.up, frente);
            float ancho = Mathf.Abs(lado.x) > 0.5f ? b.size.x : b.size.z;
            Vector3 borde = new Vector3(b.center.x, b.max.y, b.center.z) + frente * medioFondo;      // filo de arriba, al frente
            GameObject Pieza(PrimitiveType t, Vector3 pos, Vector3 esc, Quaternion rot, Material m)
            {
                var g = GameObject.CreatePrimitive(t); Object.DestroyImmediate(g.GetComponent<Collider>());
                g.transform.SetParent(raiz.transform, false); g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = esc;
                g.GetComponent<Renderer>().sharedMaterial = m; g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return g;
            }
            // el tarro volcado, asomado al filo, y la pintura que se regó arriba
            Pieza(PrimitiveType.Cylinder, borde - frente * 0.12f + lado * ancho * 0.18f + Vector3.up * 0.085f, new Vector3(0.17f, 0.1f, 0.17f), Quaternion.FromToRotation(Vector3.up, frente), lata ? lata : mat);
            Pieza(PrimitiveType.Cylinder, borde - frente * 0.10f + lado * ancho * 0.12f + Vector3.up * 0.003f, new Vector3(0.34f, 0.002f, 0.26f), Quaternion.identity, mat);
            // chorreones por el frente
            var rnd = new System.Random(7);
            float[] largos = { 1.55f, 0.9f, 1.25f, 0.45f, 1.7f, 0.7f };
            for (int i = 0; i < largos.Length; i++)
            {
                float l = Mathf.Min(largos[i], b.size.y - 0.05f), w = 0.018f + (float)rnd.NextDouble() * 0.03f;
                Vector3 x = lado * (ancho * (0.02f + i * 0.055f));
                var chorro = Pieza(PrimitiveType.Cube, borde + frente * 0.004f + x + Vector3.down * (l / 2f), new Vector3(w, l, 0.003f), Quaternion.LookRotation(frente), mat);
                var gota = Pieza(PrimitiveType.Sphere, borde + frente * 0.006f + x + Vector3.down * l, new Vector3(w * 1.6f, w * 2.2f, 0.01f), Quaternion.LookRotation(frente), mat);
                // el chorreón va pegado a la PUERTA que tiene detrás: si se abre la puerta, la pintura se va con ella
                var puerta = mueble ? mueble.GetComponentsInChildren<DatosBlender>().Where(d => d.Get("prop_tipo") == "puerta_casillero" && d.GetComponent<Renderer>())
                    .OrderBy(d => Vector3.Distance(Vector3.ProjectOnPlane(d.GetComponent<Renderer>().bounds.center - chorro.transform.position, Vector3.up), Vector3.zero)).FirstOrDefault() : null;
                if (puerta) { chorro.transform.SetParent(puerta.transform, true); gota.transform.SetParent(puerta.transform, true); }
            }
            // charco en el piso
            Pieza(PrimitiveType.Cylinder, new Vector3(b.center.x, b.min.y + 0.004f, b.center.z) + frente * (medioFondo + 0.18f) + lado * ancho * 0.12f, new Vector3(0.55f, 0.002f, 0.32f), Quaternion.identity, mat);
            // manchas en la silla que sirve (se mueven con ella)
            foreach (var s in sillas)
            {
                var c = s.GetComponent<Collider>(); if (!c) continue;
                var sb = c.bounds;
                // la superficie real de la tabla: rayos hacia abajo contra la malla (la caja de choque está recortada a 0,68 m)
                var mf = s.GetComponent<MeshFilter>(); MeshCollider tmp = null;
                if (mf && mf.sharedMesh) { tmp = s.gameObject.AddComponent<MeshCollider>(); tmp.sharedMesh = mf.sharedMesh; }
                Physics.SyncTransforms();
                Vector3 mejor = new Vector3(sb.center.x, sb.max.y, sb.center.z); float alto = -1f;
                var rb = s.GetComponent<Renderer>().bounds;
                for (int ix = 0; ix < 5; ix++) for (int iz = 0; iz < 5; iz++)
                {
                    var o = new Vector3(Mathf.Lerp(rb.min.x, rb.max.x, (ix + 0.5f) / 5f), rb.max.y + 0.5f, Mathf.Lerp(rb.min.z, rb.max.z, (iz + 0.5f) / 5f));
                    if (tmp && tmp.Raycast(new Ray(o, Vector3.down), out var h, 2f) && h.normal.y > 0.9f && h.point.y < rb.min.y + 0.85f && h.point.y > alto) { alto = h.point.y; mejor = h.point; }
                }
                if (tmp) Object.DestroyImmediate(tmp);
                for (int k = 0; k < 3; k++)
                {
                    var m = Pieza(PrimitiveType.Cylinder, mejor + new Vector3((k - 1) * 0.06f, 0.003f, (k % 2) * 0.05f), new Vector3(0.09f - k * 0.02f, 0.002f, 0.07f), Quaternion.Euler(0f, k * 40f, 0f), mat);
                    m.transform.SetParent(s.transform, true);
                }
            }
        }

        /// <summary>
        /// Piso 3 al salir del 4°B (Henry 20-oct): solo se recorre el pasillo; las escaleras (esquinas NO y SE) tienen una
        /// barricada de pupitres; todos los salones cerrados menos el aula de música, donde está el recuerdo y termina el capítulo.
        /// </summary>
        static void Piso3()
        {
            const float P3 = 7.2f;
            int cerradas = 0;
            foreach (var pu in Object.FindObjectsByType<EstanDentro.Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var r = pu.GetComponent<Renderer>(); Vector3 c = r ? r.bounds.center : pu.transform.position;
                if (c.y < P3 - 0.5f || c.y > P3 + 3f) continue;
                if (pu.name.StartsWith("PUERTA_P3_012")) continue;                                // aula de música: abierta
                if (!string.IsNullOrEmpty(pu.llaveId)) continue;                                  // 4°B: con su llave
                var so = new SerializedObject(pu);
                so.FindProperty("isLocked").boolValue = true;
                so.FindProperty("lockedHudMessage").stringValue = "Cerrado con llave.";
                so.FindProperty("forceBreathingSeconds").floatValue = 0f;
                so.FindProperty("stressOnLockedTry").floatValue = 2f;
                so.ApplyModifiedPropertiesWithoutUndo();
                cerradas++;
            }
            // el aula de música queda abierta (sin llave)
            var musica = Object.FindObjectsByType<EstanDentro.Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.name.StartsWith("PUERTA_P3_012"));
            if (musica) { var so = new SerializedObject(musica); so.FindProperty("isLocked").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            // barricadas de pupitres delante de las entradas a las escaleras
            var molde = Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.Get("prop_tipo") == "pupitre" && d.GetComponent<MeshFilter>());
            foreach (var (nombre, puerta, haciaPasillo) in new[] { ("Barricada_NO", "PUERTA_P3_006", Vector3.back), ("Barricada_SE", "PUERTA_P3_002", Vector3.forward) })
            {
                var vieja = GameObject.Find(nombre); if (vieja) Object.DestroyImmediate(vieja);
                var pu = Object.FindObjectsByType<EstanDentro.Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.name.StartsWith(puerta));
                if (!pu || !molde) continue;
                var pb = pu.GetComponent<Renderer>().bounds;
                Vector3 base0 = new Vector3(pb.center.x, P3 + 0.02f, pb.center.z) + haciaPasillo * 0.75f;
                var raiz = new GameObject(nombre); raiz.transform.position = base0;
                var rnd = new System.Random(nombre.GetHashCode());
                Vector3[] pos = { new Vector3(-0.45f, 0f, 0f), new Vector3(0.45f, 0f, 0.1f), new Vector3(0f, 0.72f, -0.05f), new Vector3(-0.3f, 1.35f, 0.1f), new Vector3(0.35f, 1.4f, -0.1f) };
                float[] giro = { 8f, -12f, 182f, 95f, -30f }, vuelco = { 0f, 0f, 175f, 20f, -65f };
                for (int i = 0; i < pos.Length; i++)
                {
                    var p = Object.Instantiate(molde.gameObject, raiz.transform);
                    foreach (var cc in p.GetComponents<EstanDentro.Interaction.Interactable>()) Object.DestroyImmediate(cc);
                    foreach (var cc in p.GetComponents<Collider>()) Object.DestroyImmediate(cc);
                    p.transform.position = base0 + pos[i];
                    p.transform.rotation = Quaternion.Euler(0f, giro[i] + (float)rnd.NextDouble() * 10f, vuelco[i]) * Quaternion.Euler(molde.transform.eulerAngles.x, 0f, 0f);
                }
                var bc = raiz.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, 1.1f, 0f); bc.size = new Vector3(1.9f, 2.2f, 0.9f);
                var bl = raiz.AddComponent<Bloqueo>(); bl.accion = "Revisar la barricada";
                bl.mensaje = "Pupitres apilados hasta arriba. Alguien no quería que nadie bajara… o que nada subiera.";
            }
            // el recuerdo: un volumen que cubre el aula de música (ala oeste, entre el aula de cómputo y la escalera)
            var viejo = GameObject.Find("Recuerdo_AulaMusica"); if (viejo) Object.DestroyImmediate(viejo);
            if (musica)
            {
                var mb = musica.GetComponent<Renderer>().bounds;
                var rec = new GameObject("Recuerdo_AulaMusica");
                rec.transform.position = new Vector3(3.6f, P3 + 1.4f, 23.5f);
                var bc = rec.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(5.6f, 2.6f, 9.2f);
                var ra = rec.AddComponent<RecuerdoAula>(); ra.puntoPa = new Vector3(1.6f, P3, 23.5f);
            }
            // al salir del 4°B el pasillo ya no es el mismo: Pesadilla hasta llegar al aula de música (el Observador en el pasillo largo)
            var vz = GameObject.Find("Recuerdo_Pasillo4B"); if (vz) Object.DestroyImmediate(vz);
            var p4b = Object.FindObjectsByType<EstanDentro.Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.llaveId == "llave_4b");
            if (p4b)
            {
                Vector3 pc = p4b.GetComponent<Renderer>() ? p4b.GetComponent<Renderer>().bounds.center : p4b.transform.position;
                // hacia afuera: el borde del salón más cercano a la puerta
                float dxMin = Mathf.Abs(pc.x - SALON.xMin), dxMax = Mathf.Abs(pc.x - SALON.xMax), dzMin = Mathf.Abs(pc.z - SALON.yMin), dzMax = Mathf.Abs(pc.z - SALON.yMax);
                float m = Mathf.Min(Mathf.Min(dxMin, dxMax), Mathf.Min(dzMin, dzMax));
                Vector3 afuera = m == dxMin ? Vector3.left : m == dxMax ? Vector3.right : m == dzMin ? Vector3.back : Vector3.forward;
                var zona = new GameObject("Recuerdo_Pasillo4B");
                zona.transform.position = new Vector3(pc.x, P3 + 1.3f, pc.z) + afuera * 1.8f;
                var zb = zona.AddComponent<BoxCollider>(); zb.isTrigger = true;
                zb.size = afuera.x != 0f ? new Vector3(2.2f, 2.6f, 6f) : new Vector3(6f, 2.6f, 2.2f);
                var gz = zona.AddComponent<EstanDentro.Mente.GatilloZona>(); gz.id = "pasillo_4b"; gz.resolverCon = "aula_musica";
                gz.pensamiento = "¿Este es mi pasillo…? Está mal. Todo está mal.";
                Debug.Log($"[Arreglos] recuerdo del pasillo afuera del 4°B en {zona.transform.position} (afuera {afuera})");
            }
            Debug.Log($"[Arreglos] piso 3: {cerradas} puertas cerradas, aula de música abierta, barricadas en las escaleras");
        }

        /// <summary>Papeles acostados que quedaron flotando sobre su mesa (p. ej. PROP_papel.004 a 9 cm): bajan hasta tocarla.</summary>
        static void PapelesApoyados()
        {
            int n = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.name.StartsWith("PROP_papel") || r.bounds.size.y > 0.15f || r.GetComponentInParent<Rigidbody>()) continue;
                var b = r.bounds; var o = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z);
                var hit = Physics.RaycastAll(o, Vector3.down, 0.6f).Where(h => !h.collider.transform.IsChildOf(r.transform)).OrderBy(h => h.distance).FirstOrDefault();
                if (!hit.collider || hit.distance < 0.02f) continue;
                r.transform.position += Vector3.down * (hit.distance - 0.012f); EditorUtility.SetDirty(r.transform); n++;
            }
            Debug.Log($"[Arreglos] {n} papeles bajados hasta su apoyo");
        }

        [System.Serializable] class MatP { public string nombre, tipo, textura_pesadilla; public float[] tinte, tinte_pesadilla; }
        [System.Serializable] class MatPLista { public List<MatP> items; }

        /// <summary>
        /// La versión Pesadilla de cada material del edificio (Henry 21-oct): las texturas dañadas que ya estaban elegidas en
        /// materiales.json (F2: pintura que se cae, humedad, óxido) con su tinte; lo liso, más oscuro y frío. Quedan en
        /// Materials_Pesadilla/ y un MundoPesadilla en la escena los cambia en el destello de la transición.
        /// </summary>
        static void MaterialesPesadilla()
        {
            const string RAIZ = "Assets/Art/Edificio", DEST = "Assets/Art/Edificio/Materials_Pesadilla";
            System.IO.Directory.CreateDirectory(DEST);
            var lista = JsonUtility.FromJson<MatPLista>(System.IO.File.ReadAllText(RAIZ + "/materiales.json"));
            var normales = new List<Material>(); var pesadillas = new List<Material>();
            Color frio = new Color(0.30f, 0.40f, 0.40f);
            foreach (var it in lista.items)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{RAIZ}/Materials/{it.nombre}.mat");
                if (!m || it.tipo == "vidrio") continue;
                string ruta = $"{DEST}/{it.nombre}_pesadilla.mat";
                var p = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (!p) { p = new Material(m); AssetDatabase.CreateAsset(p, ruta); } else p.CopyPropertiesFromMaterial(m);
                Color tp = it.tinte_pesadilla != null && it.tinte_pesadilla.Length >= 3 ? new Color(it.tinte_pesadilla[0], it.tinte_pesadilla[1], it.tinte_pesadilla[2]) : Color.white;
                string tex = it.textura_pesadilla;
                var col = string.IsNullOrEmpty(tex) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{RAIZ}/Texturas/{tex}/{tex}_antirep_color.png")
                          ?? AssetDatabase.LoadAssetAtPath<Texture2D>($"{RAIZ}/Texturas/{tex}/{tex}_color_2k.jpg");
                if (col)
                {
                    p.SetTexture("_BaseColorMap", col);
                    var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RAIZ}/Texturas/{tex}/{tex}_antirep_normal.png");
                    if (nor) { if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(nor)) is TextureImporter tn && tn.textureType != TextureImporterType.NormalMap) { tn.textureType = TextureImporterType.NormalMap; tn.SaveAndReimport(); } p.SetTexture("_NormalMap", nor); }
                    var mask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{RAIZ}/Texturas/{tex}/{tex}_antirep_rugosidad_mask.png");
                    if (mask) p.SetTexture("_MaskMap", mask);
                    p.SetColor("_BaseColor", tp * 0.85f);
                }
                else
                {
                    var c = p.HasProperty("_BaseColor") ? p.GetColor("_BaseColor") : Color.white;
                    p.SetColor("_BaseColor", Color.Lerp(c, frio, 0.45f) * 0.7f * tp);
                    if (p.HasProperty("_Smoothness")) p.SetFloat("_Smoothness", Mathf.Max(0.05f, p.GetFloat("_Smoothness") * 0.6f));
                }
                if (p.HasProperty("_EmissiveColor")) p.SetColor("_EmissiveColor", p.GetColor("_EmissiveColor") * 0.25f);   // las luces de techo casi apagadas
                UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(p); EditorUtility.SetDirty(p);
                normales.Add(m); pesadillas.Add(p);
            }
            AssetDatabase.SaveAssets();
            var viejo = GameObject.Find("Mundo_Pesadilla"); if (viejo) Object.DestroyImmediate(viejo);
            var mpGo = new GameObject("Mundo_Pesadilla"); mpGo.AddComponent<EstanDentro.Mente.ControlMente>();
            var mp = mpGo.AddComponent<EstanDentro.Mente.MundoPesadilla>();
            mp.normales = normales.ToArray(); mp.pesadillas = pesadillas.ToArray();
            Debug.Log($"[Arreglos] Pesadilla: {pesadillas.Count} materiales con su versión dañada");
            ElObservador();
        }

        /// <summary>El Observador (02_Arte3D_Blender/scripts_blender/observador.py): piel casi negra y ojos de luz fría.</summary>
        static void ElObservador()
        {
            const string RUTA = "Assets/Art/Observador";
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA + "/Observador.fbx");
            var viejo = GameObject.Find("Observador"); if (viejo) Object.DestroyImmediate(viejo);
            if (!fbx) { Debug.LogWarning("[Arreglos] falta Observador.fbx (correr observador.py en Blender)"); return; }
            var lit = Shader.Find("HDRP/Lit");
            Material Mat(string nombre, System.Action<Material> conf)
            {
                string ruta = $"{RUTA}/{nombre}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (!m) { m = new Material(lit); AssetDatabase.CreateAsset(m, ruta); }
                conf(m); UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m); EditorUtility.SetDirty(m); return m;
            }
            var piel = Mat("OB_piel", m => { m.SetColor("_BaseColor", new Color(0.012f, 0.011f, 0.014f)); m.SetFloat("_Smoothness", 0.62f); m.SetFloat("_Metallic", 0f); });
            var ojos = Mat("OB_ojos", m => { m.SetColor("_BaseColor", Color.white); m.SetColor("_EmissiveColor", new Color(0.8f, 0.95f, 1f) * 15f); m.SetFloat("_UseEmissiveIntensity", 0f); });
            var raiz = new GameObject("Observador");
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(fbx); modelo.name = "Cuerpo";
            PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            modelo.transform.SetParent(raiz.transform, false);
            foreach (var rr in modelo.GetComponentsInChildren<Renderer>())
            {
                var ms = rr.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) ms[i] = ms[i] && ms[i].name.Contains("ojos") ? ojos : piel;
                rr.sharedMaterials = ms; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            foreach (var c in modelo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            raiz.AddComponent<EstanDentro.Mente.Observador>();
            var bnd = modelo.GetComponentInChildren<Renderer>().bounds;
            Debug.Log($"[Arreglos] Observador listo: alto {bnd.size.y:F2} m, frente del modelo {modelo.transform.forward}");
        }

        /// <summary>Hasta dónde se camina desde la salida de los ductos (cuadrícula de 40 cm, cápsula del jugador).</summary>
        public static void RevisarAlcanceBatch()
        {
            EditorSceneManager.OpenScene(ESCENA);
            Physics.SyncTransforms();
            var spawn = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name.Contains("salas_finales"));
            Vector3 ini = spawn ? spawn.position : new Vector3(14.7f, 0.05f, 8.9f);
            const float paso = 0.4f;
            var vistos = new System.Collections.Generic.HashSet<Vector2Int>(); var cola = new System.Collections.Generic.Queue<Vector2Int>();
            Vector2Int K(Vector3 v) => new Vector2Int(Mathf.RoundToInt(v.x / paso), Mathf.RoundToInt(v.z / paso));
            float Suelo(Vector2Int k)
            {
                var o = new Vector3(k.x * paso, 1.2f, k.y * paso);
                return Physics.Raycast(o, Vector3.down, out var h, 6f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : -99f;
            }
            bool Cabe(Vector2Int k, float y) => !Physics.CheckCapsule(new Vector3(k.x * paso, y + 0.3f + 0.25f, k.y * paso), new Vector3(k.x * paso, y + 1.6f, k.y * paso), 0.24f, ~0, QueryTriggerInteraction.Ignore);
            var k0 = K(ini); vistos.Add(k0); cola.Enqueue(k0);
            var fugas = new System.Collections.Generic.List<string>();
            while (cola.Count > 0 && vistos.Count < 60000)
            {
                var k = cola.Dequeue(); float y = Suelo(k);
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = k + d; if (vistos.Contains(n)) continue;
                    float yn = Suelo(n);
                    if (Mathf.Abs(yn - y) > 0.35f) { if (yn < -50f || Mathf.Abs(yn) > 1.0f) fugas.Add($"borde/escalón en ({n.x * paso:F1},{yn:F1},{n.y * paso:F1})"); continue; }
                    if (!Cabe(n, yn)) continue;
                    vistos.Add(n); cola.Enqueue(n);
                    float x = n.x * paso, z = n.y * paso;
                    if (x < -0.5f || x > 48.5f || z < -0.5f || z > 36.5f) fugas.Add($"AFUERA ({x:F1},{z:F1})");
                }
            }
            var afuera = fugas.Where(f => f.StartsWith("AFUERA")).Take(8);
            Debug.Log($"[Alcance] celdas={vistos.Count} afuera={fugas.Count(f => f.StartsWith("AFUERA"))} ej: {string.Join(" | ", afuera)}");
            Debug.Log($"[Alcance] bordes (escaleras/huecos) {fugas.Count(f => !f.StartsWith("AFUERA"))}: {string.Join(" | ", fugas.Where(f => !f.StartsWith("AFUERA")).Take(15))}");
            // salas a las que se llega
            float minX = vistos.Min(v => v.x) * paso, maxX = vistos.Max(v => v.x) * paso, minZ = vistos.Min(v => v.y) * paso, maxZ = vistos.Max(v => v.y) * paso;
            Debug.Log($"[Alcance] caja alcanzada x {minX:F1}..{maxX:F1}  z {minZ:F1}..{maxZ:F1}");
            EditorApplication.Exit(0);
        }

        /// <summary>El jugador más delgado (radio 0,25 m: pasa por pasillos de 60 cm) y sin poder trepar sillas ni pupitres.</summary>
        static void JugadorDelgado()
        {
            foreach (var cc in Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                cc.radius = 0.25f; cc.stepOffset = 0.2f; cc.slopeLimit = 40f; cc.skinWidth = 0.03f;
                Debug.Log($"[Arreglos] jugador {cc.name}: radio {cc.radius}, escalón {cc.stepOffset}, pendiente {cc.slopeLimit}");
            }
        }

        static PhysicsMaterial Friccion()
        {
            const string ruta = "Assets/Art/Colegio_Extra/Patas_Goma.asset";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(ruta);
            if (!m) { m = new PhysicsMaterial("Patas_Goma"); AssetDatabase.CreateAsset(m, ruta); }
            m.dynamicFriction = 0.9f; m.staticFriction = 1.1f; m.bounciness = 0f;
            m.frictionCombine = PhysicsMaterialCombine.Maximum; m.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(m); return m;
        }

        static int Tablero()
        {
            // verde de tablero de tiza real (gastado, mate), no casi negro
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Edificio/Materials/ED_pizarra.mat");
            if (mat)
            {
                mat.SetColor("_BaseColor", new Color(0.30f, 0.44f, 0.34f));
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
                EditorUtility.SetDirty(mat);
            }
            // tiza escrita en el tablero del 4°B: la fecha y la tarea (las mismas pistas del cuaderno)
            int n = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!r.sharedMaterials.Any(m => m && m.name.Contains("pizarra"))) continue;
                var b = r.bounds; var c = b.center;
                if (!(SALON.Contains(new Vector2(c.x, c.z)) && c.y > 6.5f && c.y < 11f)) continue;
                if (b.size.x * b.size.z < 0.0001f && b.size.y < 0.6f) continue;
                if (r.transform.Find("Tiza")) continue;
                // la cara del tablero mira al centro del salón
                bool delgadoX = b.size.x < b.size.z;
                Vector3 hacia = delgadoX ? new Vector3(Mathf.Sign(SALON.center.x - c.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(SALON.center.y - c.z));
                float ancho = delgadoX ? b.size.z : b.size.x;
                if (ancho < 1.2f) continue;
                Vector3 frente = c + hacia * ((delgadoX ? b.extents.x : b.extents.z) + 0.006f);
                // lo escrito con tiza: imagen de letra a mano (textos_pared.py) en un plano pegado al tablero
                // (la malla de tiza vieja salía aplastada: se veía como una raya)
                var mtT = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Textos/Mat_tablero_4b.mat");
                if (!mtT) continue;
                var tiza = GameObject.CreatePrimitive(PrimitiveType.Quad); tiza.name = "Tiza";
                Object.DestroyImmediate(tiza.GetComponent<Collider>());
                Vector3 derecha = Vector3.Cross(Vector3.up, -hacia);                     // la derecha de quien mira el tablero
                tiza.transform.rotation = Quaternion.LookRotation(-hacia, Vector3.up);
                float anchoT = ancho * 0.72f, altoT = anchoT * 0.5f;
                tiza.transform.position = frente + hacia * 0.002f + Vector3.up * (b.extents.y * 0.05f);
                tiza.transform.localScale = new Vector3(anchoT, Mathf.Min(altoT, b.size.y * 0.85f), 1f);
                tiza.transform.SetParent(r.transform, true);
                var tr = tiza.GetComponent<MeshRenderer>(); tr.sharedMaterial = mtT; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // el tablero se mira de cerca (aunque no se tenga tiza) y lee la pista del 142
                if (!r.GetComponent<Collider>()) r.gameObject.AddComponent<BoxCollider>();
                foreach (var otro in r.GetComponentsInParent<EstanDentro.Interaction.Interactable>(true)) if (!(otro is TableroInteractivo)) Object.DestroyImmediate(otro);   // que no le gane otro "tocar"
                var ti = r.GetComponent<TableroInteractivo>(); if (!ti) ti = r.gameObject.AddComponent<TableroInteractivo>();
                ti.mirarA = c; ti.puntoCamara = c + hacia * 1.6f;
                ti.lectura = "«17 de mayo»… el día del accidente. Y la tarea: «ejercicios, pág. 142». Ese número me suena.";
                // un pedazo de tiza en la repisa del tablero: así se aprende pronto a marcar las paredes (T)
                var molde = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Objetos/tiza.prefab");
                if (molde && !GameObject.Find("Tiza_Repisa"))
                {
                    var tz = (GameObject)PrefabUtility.InstantiatePrefab(molde); tz.name = "Tiza_Repisa";
                    PrefabUtility.UnpackPrefabInstance(tz, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Vector3 p0 = c + hacia * ((delgadoX ? b.extents.x : b.extents.z) + 0.07f) + Vector3.down * (b.extents.y - 0.05f) + derecha * ancho * 0.25f;
                    float yApoyo = Physics.Raycast(p0 + Vector3.up * 0.05f, Vector3.down, out var h, 1.5f) ? h.point.y : p0.y - 0.06f;
                    tz.transform.position = new Vector3(p0.x, yApoyo + 0.002f, p0.z);
                    tz.transform.rotation = Quaternion.LookRotation(derecha);
                    var bx = tz.AddComponent<BoxCollider>(); bx.size = new Vector3(0.12f, 0.05f, 0.12f); bx.center = new Vector3(0f, 0.02f, 0f);
                    var rg = tz.AddComponent<Recogible>(); rg.itemId = "tiza"; rg.nombreVisible = "Tiza";
                }
                n++;
            }
            return n;
        }

    }
}
