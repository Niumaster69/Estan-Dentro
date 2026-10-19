using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EstanDentro.Colegio;
using EstanDentro.Interaction;
using EstanDentro.Player;
using EstanDentro.Stress;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Pasa la lógica del Capítulo 1 (escena vieja del salón) al colegio nuevo:
    ///   Acto 1 (Salón 4°B): casillero 142 -> destornillador; lonchera 046 -> linterna + apagón; cartas; mensajes ocultos;
    ///                        rejilla del techo con destornillador -> escena de ductos.
    ///   Acto 2: escena vieja de ductos; su salida ahora vuelve al colegio (spawn 'salas_finales').
    ///   Acto 3: puerta de la rectoría con clave 1705 -> final (EndGameTrigger).
    /// Se llama desde ConstruirColegio.Escena() con la escena nueva abierta.
    /// </summary>
    public static class PortarCapitulo1
    {
        const string ESCENA_VIEJA = "Assets/Scenes/Level-1/EscenaUno-salonPrincipal.unity";
        public const string ESCENA_DUCTOS = "Assets/Scenes/Level-1/EsecenaUno-DuctosDeVentilacion.unity";

        // Salón 4°B en coordenadas Unity (x, z) y piso
        static readonly Rect SALON = Rect.MinMaxRect(19.33f, 29f, 28.67f, 36f);
        const float PISO_P3 = 7.2f;

        public static void Portar()
        {
            var nueva = SceneManager.GetActiveScene();
            var vieja = EditorSceneManager.OpenScene(ESCENA_VIEJA, OpenSceneMode.Additive);
            GameObject V(string ruta) => BuscarEn(vieja, ruta);

            var props = Object.FindObjectsByType<DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                              .Where(d => d.gameObject.scene == nueva).ToList();
            List<DatosBlender> EnSalon(string tipo) => props.Where(d => d.Get("prop_tipo") == tipo && EnRect(d.transform.position, SALON, PISO_P3)).ToList();

            // ---------- casillero 142 -> destornillador
            var casillero = props.FirstOrDefault(d => d.Get("interactuable").StartsWith("casillero: candado 142"));
            var casVieja = V("Caasilleros/Casillero_REAL");
            if (casillero && casVieja)
            {
                ConvertirEnCerradura(casillero.gameObject, casVieja);
                // adentro está la carta de papá con la clave de la lonchera (0-4-6)
                var pista = V("Nota_PistaPa")?.GetComponent<Note>();
                if (pista)
                {
                    var sp = new SerializedObject(pista);
                    var sl = new SerializedObject(casillero.GetComponent<LockedContainer>());
                    sl.FindProperty("interiorNoteTitle").stringValue = sp.FindProperty("noteTitle").stringValue;
                    sl.FindProperty("interiorNoteBody").stringValue = sp.FindProperty("noteText").stringValue;
                    sl.FindProperty("autoShowNote").boolValue = true;
                    sl.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // ---------- lonchera 046 -> linterna + apagón
            var lonchera = props.FirstOrDefault(d => d.Get("interactuable").StartsWith("lonchera: candado 046"));
            var lonVieja = V("Lonchera_Linterna");
            if (lonchera && lonVieja)
            {
                ConvertirEnCerradura(lonchera.gameObject, lonVieja);
                var miedo = lonVieja.GetComponent<EnvironmentDarknessFear>();
                if (miedo) EditorUtility.CopySerialized(miedo, lonchera.gameObject.AddComponent<EnvironmentDarknessFear>());
            }

            // ---------- linterna: ya no está suelta en el escritorio, sale de la lonchera
            foreach (var d in props.Where(d => d.Get("interactuable").StartsWith("linterna"))) d.gameObject.SetActive(false);

            // ---------- cartas (textos originales del capítulo 1)
            CopiarNota(props.FirstOrDefault(d => d.Get("interactuable") == "carta 1"), V("Nota_Cuaderno_Matematicas"));
            CopiarNota(props.FirstOrDefault(d => d.Get("interactuable") == "carta 2"), V("Nota_Tarea"));
            // en el escritorio del profe: el cuaderno con "ejercicios pág 142" (clave del casillero) y la fecha 17/05
            var escProfe = EnSalon("escritorio_profesor").FirstOrDefault();
            CopiarNota(escProfe, V("EscritorioProfesor/Plane.031/Nota_Cuaderno_Compañero"));

            // ---------- mensajes ocultos (se revelan con la linterna) en las paredes del 4°B
            var mensajes = new[] { "HiddenMessage_Techo", "HiddenMessage_Techo (1)", "HiddenMessage_Techo (2)", "HiddenMessage_Techo (3)" };
            var spots = new[]
            {
                (new Vector3(SALON.xMin + 0.12f, PISO_P3 + 2.0f, 32.5f), 90f),
                (new Vector3(SALON.xMax - 0.12f, PISO_P3 + 1.6f, 31.0f), -90f),
                (new Vector3(24.0f, PISO_P3 + 2.6f, SALON.yMin + 0.12f), 0f),
                (new Vector3(26.5f, PISO_P3 + 1.2f, SALON.yMin + 0.12f), 0f),
            };
            for (int i = 0; i < mensajes.Length; i++)
            {
                var mv = V(mensajes[i]); if (!mv) continue;
                var c = Copiar(mv, nueva);
                c.transform.SetPositionAndRotation(spots[i].Item1, Quaternion.Euler(0f, spots[i].Item2, 0f));
            }

            // ---------- apagón, respiración, intrusiones, sonido global y ambiente del salón
            foreach (var r in new[] { "BlackoutManager", "_Breathing", "IntrusionManager", "Config. ", "Ambient_SalonPrincipal" })
            { var g = V(r); if (g) Copiar(g, nueva); }

            // ---------- rejilla del techo del 4°B: con destornillador -> ductos
            var rejilla = props.FirstOrDefault(d => d.Get("sala") == "Salón 4°B");
            if (rejilla)
            {
                var rj = rejilla.GetComponent<RejillaDucto>() ?? rejilla.gameObject.AddComponent<RejillaDucto>();
                rj.escenaDestino = "EsecenaUno-DuctosDeVentilacion";
                rj.tipCarga = "Inhala 4. Sostén 4. Sigue arriba.";
                rj.misionCompletada = "salir_salon";
            }

            // ---------- spawns: inicio en un pupitre del 4°B mirando al tablero; 'salas_finales' frente a la rectoría
            var pupitres = EnSalon("pupitre").OrderBy(d => Vector3.Distance(d.transform.position, new Vector3(24f, PISO_P3, 33.5f))).ToList();
            var tablero = EnSalon("tablero").FirstOrDefault();
            if (pupitres.Count > 0)
            {
                var p = pupitres[0].transform;
                Vector3 mira = tablero ? (tablero.transform.position - p.position) : Vector3.left;
                mira.y = 0;
                CrearSpawn("inicio_salon", p.position + Vector3.up * 0.05f - mira.normalized * 0.25f, Quaternion.LookRotation(mira), true, false);
            }
            // rectoría = ranura S1 del primer piso (x 10..19.33, z 0..7); su puerta da a la galería (z ≈ 7)
            var rectoria = Object.FindObjectsByType<LockedDoor>(FindObjectsSortMode.None)
                                .Where(d => d.name.StartsWith("PUERTA_P1") && d.transform.position.y < 3f &&
                                            d.transform.position.x > 10f && d.transform.position.x < 19.33f &&
                                            d.transform.position.z > 6f && d.transform.position.z < 8f)
                                .Select(d => d.GetComponent<DatosBlender>()).FirstOrDefault(d => d);
            // 'salas_finales': en la galería, mirando la puerta de la rectoría (con su cadena)
            var pieFinal = new Vector3(14.7f, 0.05f, 8.9f);
            Vector3 haciaPuerta = Vector3.back;
            if (rectoria)
            {
                var rr = rectoria.GetComponent<Renderer>();
                Vector3 cp = rr ? rr.bounds.center : rectoria.transform.position;
                pieFinal = new Vector3(cp.x, 0.05f, 8.9f);
                haciaPuerta = new Vector3(cp.x - pieFinal.x, 0f, cp.z - pieFinal.z);
            }
            CrearSpawn("salas_finales", pieFinal, Quaternion.LookRotation(haciaPuerta.sqrMagnitude > 0.01f ? haciaPuerta : Vector3.back), false, true);

            // ---------- acto 3: rectoría con clave 1705 y el final adentro
            if (rectoria)
            {
                var puerta = rectoria.GetComponent<LockedDoor>();
                var so = new SerializedObject(puerta);
                so.FindProperty("isLocked").boolValue = true;
                so.FindProperty("lockedHudMessage").stringValue = "La rectoría está cerrada. Tiene un candado de 4 dígitos.";
                so.ApplyModifiedPropertiesWithoutUndo();
                var lockViejo = V("Estructura/Estructura v2/Puerta2")?.GetComponent<CombinationLock>();
                var cl = rectoria.gameObject.AddComponent<CombinationLock>();
                if (lockViejo) EditorUtility.CopySerialized(lockViejo, cl);
                var sc = new SerializedObject(cl);
                var code = sc.FindProperty("correctCode"); code.arraySize = 4;
                int[] k = { 1, 7, 0, 5 }; for (int i = 0; i < 4; i++) code.GetArrayElementAtIndex(i).intValue = k[i];
                sc.FindProperty("contextualMissionId").stringValue = "abrir_rectoria";
                sc.FindProperty("contextualMissionText").stringValue = "Abre la rectoría";
                sc.ApplyModifiedPropertiesWithoutUndo();
                LimpiarEventos(cl);
            }
            var finViejo = V("EndGameTrigger");
            if (finViejo)
            {
                var fin = Copiar(finViejo, nueva);
                fin.transform.position = new Vector3(14.7f, 1.0f, 3.2f);       // dentro de la rectoría
                var sf = new SerializedObject(fin.GetComponent<EndGameTrigger>());
                sf.FindProperty("nextSceneName").stringValue = "Colegio";
                sf.ApplyModifiedPropertiesWithoutUndo();
            }
            // pistas de la clave en la secretaría y la rectoría (notas del final del capítulo)
            var papelesRect = props.Where(d => d.Get("prop_tipo") == "papel" && d.transform.position.y < 2f &&
                                               d.transform.position.z < 7f && d.transform.position.x > 28.67f && d.transform.position.x < 38f).ToList();   // secretaría (abierta)
            if (papelesRect.Count > 0) CopiarNota(papelesRect[0], V("papel"));
            if (papelesRect.Count > 1) CopiarNota(papelesRect[1], V("papel (1)"));

            // ---------- cada candado con su diseño (se reconocen en el mundo, en su pantalla y en las fotos)
            void Estilo(Component c, EstiloCandado e, string nombre)
            {
                var cl = c ? c.GetComponent<CombinationLock>() : null; if (!cl) return;
                cl.Estilo = e; cl.NombreVisible = nombre; EditorUtility.SetDirty(cl);
            }
            Estilo(casillero, EstiloCandado.Laton3, "Candado del casillero");
            Estilo(lonchera, EstiloCandado.Maleta3, "Candado de la lonchera");
            Estilo(rectoria, EstiloCandado.Cadena4, "Candado de la rectoría");
            if (rectoria)
            {
                var so = new SerializedObject(rectoria.GetComponent<LockedDoor>());
                so.FindProperty("lockedHudMessage").stringValue = "Tiene una cadena con un candado grande de 4 ruedas.";
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            int candados = 0;
            foreach (var d in props.Where(d => !string.IsNullOrEmpty(d.Get("candado_de"))))
            {
                var dueno = props.FirstOrDefault(x => x.name == d.Get("candado_de"))?.gameObject
                            ?? Object.FindObjectsByType<LockedDoor>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == d.Get("candado_de"))?.gameObject;
                var cl = dueno ? dueno.GetComponent<CombinationLock>() : null;
                if (!cl) { Debug.LogWarning("[Capitulo1] candado sin cerradura: " + d.name); continue; }
                foreach (var c in d.GetComponents<Interactable>()) Object.DestroyImmediate(c);
                var mc = d.GetComponent<MeshCollider>(); if (mc) Object.DestroyImmediate(mc);
                d.gameObject.AddComponent<BoxCollider>();
                d.gameObject.AddComponent<CandadoVisual>().cerradura = cl;
                candados++;
            }

            // ---------- la puerta del 4°B está trabada: la única salida es el ducto (si no, se baja directo a la rectoría)
            int trabadas = 0;
            foreach (var pu in Object.FindObjectsByType<LockedDoor>(FindObjectsSortMode.None))
            {
                var pp = pu.transform.position;
                var b = pu.GetComponent<Renderer>() ? pu.GetComponent<Renderer>().bounds.center : pp;
                if (!(b.y > PISO_P3 - 0.5f && b.y < PISO_P3 + 3f && b.x > SALON.xMin - 0.3f && b.x < SALON.xMax + 0.3f
                      && b.z > SALON.yMin - 0.4f && b.z < SALON.yMax + 0.4f)) continue;
                var so = new SerializedObject(pu);
                so.FindProperty("isLocked").boolValue = true;
                so.FindProperty("lockedHudMessage").stringValue = "No abre. Está trabada desde afuera.";
                so.ApplyModifiedPropertiesWithoutUndo();
                trabadas++;
            }
            Debug.Log($"[Capitulo1] puertas del 4°B trabadas: {trabadas}");

            // ---------- la mochila de Mateo (junto a su pupitre): sin ella solo carga 2 cosas, con ella 6
            var inicioPos = pupitres.Count > 0 ? pupitres[0].transform.position : new Vector3(24f, PISO_P3, 33.5f);
            var mochila = props.FirstOrDefault(d => d.Get("es_de") == "Mateo")
                          ?? EnSalon("mochila").Concat(EnSalon("mochila_azul"))
                                .OrderBy(d => Vector3.Distance(d.transform.position, inicioPos)).FirstOrDefault();
            if (mochila)
            {
                foreach (var c in mochila.GetComponents<Interactable>()) Object.DestroyImmediate(c);
                mochila.gameObject.AddComponent<MochilaEquipable>();
            }
            Debug.Log($"[Capitulo1] mochila de Mateo: {(mochila ? mochila.name : "NO HAY en el 4°B")}");

            // ---------- en el apagón también se apagan los tubos (material emisivo)
            if (!Object.FindAnyObjectByType<ApagarEmisivos>()) new GameObject("_ApagarEmisivos").AddComponent<ApagarEmisivos>();

            // ---------- noche: el apagón no apaga la luna
            var luna = GameObject.Find("Luna")?.GetComponent<Light>();
            foreach (var be in Object.FindObjectsByType<BlackoutEvent>(FindObjectsSortMode.None))
            {
                if (!luna || be.gameObject.scene != nueva) continue;
                var so = new SerializedObject(be);
                var ex = so.FindProperty("excludedLights");
                ex.arraySize++; ex.GetArrayElementAtIndex(ex.arraySize - 1).objectReferenceValue = luna;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ---------- jugador: Concentrarse (Q) y un despertar corto (empieza a jugar enseguida)
            // OJO: la escena vieja sigue abierta; hay que tomar el jugador de la escena NUEVA
            var jugador = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                .FirstOrDefault(p => p.gameObject.scene == nueva);
            if (jugador)
            {
                if (!jugador.GetComponent<Concentrarse>()) jugador.gameObject.AddComponent<Concentrarse>();
                if (!jugador.GetComponent<MarcaTiza>()) jugador.gameObject.AddComponent<MarcaTiza>();
                if (!jugador.GetComponent<CuerpoMateo>()) jugador.gameObject.AddComponent<CuerpoMateo>();   // pasos y respiración
                var wu = jugador.GetComponent<PlayerWakeUp>();
                if (wu)
                {
                    var so = new SerializedObject(wu);
                    so.FindProperty("blackHoldSeconds").floatValue = 1f;
                    so.FindProperty("blinks").intValue = 1;
                    so.FindProperty("blinkOpenSeconds").floatValue = 1.2f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // ---------- sistemas duplicados: un solo Inventory (en InventoryManager)
            var invs = Object.FindObjectsByType<Inventory.Inventory>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                             .Where(i => i.gameObject.scene == nueva).ToList();
            if (invs.Count > 1)
                foreach (var i in invs.Where(i => i.gameObject.name != "InventoryManager")) Object.DestroyImmediate(i);

            EditorSceneManager.CloseScene(vieja, true);
            SceneManager.SetActiveScene(nueva);
            SonidosColegio.Aplicar();
            TextosCapitulo1.Aplicar();                      // textos de la narrativa unificada (con la escena vieja ya cerrada)
            Debug.Log($"[Capitulo1] candados 3D={candados} luna={(luna ? "ok" : "NO")} inventarios={invs.Count}");
            Debug.Log($"[Capitulo1] casillero={(casillero ? "ok" : "NO")} lonchera={(lonchera ? "ok" : "NO")} rejilla={(rejilla ? "ok" : "NO")} rectoria={(rectoria ? rectoria.name : "NO")} pupitres={pupitres.Count}");
        }

        /// <summary>La salida de los ductos ahora vuelve al colegio nuevo.</summary>
        public static void RedirigirDuctos()
        {
            var esc = EditorSceneManager.OpenScene(ESCENA_DUCTOS, OpenSceneMode.Additive);
            foreach (var t in Object.FindObjectsByType<ExitDuctsTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.gameObject.scene != esc) continue;
                var so = new SerializedObject(t);
                so.FindProperty("nextSceneName").stringValue = "Colegio";
                so.FindProperty("targetSpawnId").stringValue = "salas_finales";
                so.ApplyModifiedPropertiesWithoutUndo();
                TextosCapitulo1.AplicarDuctos(t);
            }
            EditorSceneManager.SaveScene(esc);
            EditorSceneManager.CloseScene(esc, true);
        }

        // ------------------------------------------------------------------ utilidades
        static void ConvertirEnCerradura(GameObject destino, GameObject origen)
        {
            foreach (var c in destino.GetComponents<Contenedor>()) Object.DestroyImmediate(c);
            var lockO = origen.GetComponent<CombinationLock>();
            var contO = origen.GetComponent<LockedContainer>();
            var cl = destino.AddComponent<CombinationLock>();
            var lc = destino.AddComponent<LockedContainer>();
            if (lockO) EditorUtility.CopySerialized(lockO, cl);
            if (contO) EditorUtility.CopySerialized(contO, lc);
            var s = new SerializedObject(lc); s.FindProperty("animator").objectReferenceValue = null; s.ApplyModifiedPropertiesWithoutUndo();
            LimpiarEventos(cl);
            UnityEventTools.AddPersistentListener(cl.onSolved, lc.OpenContainer);
            if (!destino.GetComponent<Collider>()) destino.AddComponent<BoxCollider>();
        }

        static void LimpiarEventos(CombinationLock cl)
        {
            for (int i = cl.onSolved.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(cl.onSolved, i);
            for (int i = cl.onFailed.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(cl.onFailed, i);
        }

        static void CopiarNota(DatosBlender destino, GameObject origen)
        {
            if (!destino || !origen) return;
            var n = origen.GetComponent<Note>(); if (!n) return;
            foreach (var c in destino.GetComponents<Interactable>()) Object.DestroyImmediate(c);
            var nueva = destino.gameObject.AddComponent<Note>();
            EditorUtility.CopySerialized(n, nueva);
            // la nota queda en el mundo (antes se destruía al leerla: en el escritorio del profe borraba el escritorio entero)
            var so = new SerializedObject(nueva);
            so.FindProperty("disappearAfterRead").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!destino.GetComponent<Collider>()) destino.gameObject.AddComponent<BoxCollider>();
        }

        static GameObject Copiar(GameObject g, Scene destino)
        {
            var c = Object.Instantiate(g); c.name = g.name.Trim();
            SceneManager.MoveGameObjectToScene(c, destino);
            return c;
        }

        static void CrearSpawn(string id, Vector3 pos, Quaternion rot, bool porDefecto, bool saltarDespertar)
        {
            var g = new GameObject("SpawnPoint_" + id);
            g.transform.SetPositionAndRotation(pos, rot);
            var sp = g.AddComponent<PlayerSpawnPoint>();
            var so = new SerializedObject(sp);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("isDefault").boolValue = porDefecto;
            so.FindProperty("skipWakeUp").boolValue = saltarDespertar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool EnRect(Vector3 p, Rect r, float piso) => r.Contains(new Vector2(p.x, p.z)) && p.y > piso - 0.5f && p.y < piso + 3.4f;

        static GameObject BuscarEn(Scene s, string ruta)
        {
            var partes = ruta.Split('/');
            var root = s.GetRootGameObjects().FirstOrDefault(g => g.name == partes[0] || g.name.Trim() == partes[0].Trim());
            if (!root) { Debug.LogWarning("[Capitulo1] no encontré " + ruta); return null; }
            var t = root.transform;
            for (int i = 1; i < partes.Length && t; i++) t = t.Find(partes[i]);
            if (!t) Debug.LogWarning("[Capitulo1] no encontré " + ruta);
            return t ? t.gameObject : null;
        }
    }
}
