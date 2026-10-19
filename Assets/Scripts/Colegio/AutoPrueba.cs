using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using EstanDentro.Interaction;
using EstanDentro.Player;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Jugador automático: recorre el Capítulo 1 dentro del ejecutable y anota qué funciona.
    /// Solo corre si el juego se abre con  -autoprueba "carpeta_de_capturas".
    /// Resultado: capturas numeradas + autoprueba.txt en esa carpeta.
    /// </summary>
    public class AutoPrueba : MonoBehaviour
    {
        string carpeta;
        int nCaptura;
        int fallas;
        readonly StringBuilder informe = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-autoprueba");
            if (i < 0) return;
            var go = new GameObject("_AutoPrueba");
            DontDestroyOnLoad(go);
            var a = go.AddComponent<AutoPrueba>();
            a.carpeta = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1]
                       : Path.Combine(Application.persistentDataPath, "autoprueba");
            Directory.CreateDirectory(a.carpeta);
        }

        void Start()
        {
            Application.runInBackground = true;
            StartCoroutine(Recorrido());
            StartCoroutine(Vigilante());
        }

        /// <summary>Si un menú congela el tiempo más de 6 s, anota cuál fue y lo destraba (un jugador real lo cerraría).</summary>
        IEnumerator Vigilante()
        {
            float congelado = 0f;
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                congelado = Time.timeScale == 0f ? congelado + 1f : 0f;
                if (congelado < 6f) continue;
                var bm = Breathing.BreathingMinigame.Instance;
                var cal = FindAnyObjectByType<Breathing.MicCalibration>();
                Anotar($"AVISO: el tiempo quedó congelado 6 s (overlay={EstanDentro.UI.OverlayBlocker.IsBlocking}, respiración={(bm && bm.IsVisible)}, " +
                       $"calibración mic={(cal && cal.isActiveAndEnabled)}, estrés={(Stress.StressSystem.Instance ? Stress.StressSystem.Instance.CurrentStress : -1):F0}). Lo destrabo.");
                yield return Captura("congelado");
                CerrarOverlays();
                Stress.StressSystem.Instance?.ResetTo(0f);
                if (bm) bm.ForceHide();
                Time.timeScale = 1f;
                congelado = 0f;
            }
        }

        // ------------------------------------------------------------------ recorrido
        IEnumerator Recorrido()
        {
            if (SceneManager.GetActiveScene().name == "Prologo") yield return PruebaPrologo();
            Anotar("Inicio de la prueba automática del Capítulo 1");
            yield return new WaitForSecondsRealtime(2f);
            FotoPapeles();
            yield return Captura("despertar");
            yield return new WaitForSecondsRealtime(14f);
            var pc = Jugador();
            Revisar(pc != null, "Hay jugador en la escena");
            if (pc == null) { Terminar(); yield break; }
            Anotar($"Jugador aparece en {pc.transform.position:F2}");
            Revisar(pc.GetComponent<CuerpoMateo>() != null, "Mateo tiene pasos y respiración (CuerpoMateo)");
            Revisar(Audio.SonidoCorto.Clip("pasos/baldosa_0") && Audio.SonidoCorto.Clip("objetos/papel_tomar") && Audio.SonidoCorto.Clip("sustos/susurro"), "Los sonidos nuevos cargan");
            yield return Captura("inicio_en_pupitre");

            // sillas y pupitres no deben deslizarse al chocarlos
            var pupitre = Datos("pupitre").OrderBy(d => Vector3.Distance(d.transform.position, pc.transform.position)).FirstOrDefault();
            if (pupitre)
            {
                Vector3 antes = pupitre.transform.position;
                var cc = pc.GetComponent<CharacterController>();
                Vector3 dir = pupitre.transform.position - pc.transform.position; dir.y = 0; dir.Normalize();
                for (int k = 0; k < 90; k++) { cc.Move(dir * 4f * Time.deltaTime + Vector3.down * 0.1f); yield return null; }
                float mov = Vector3.Distance(antes, pupitre.transform.position);
                Revisar(mov < 0.01f, $"El pupitre no se desliza al empujarlo (se movió {mov * 100f:F1} cm)");
            }

            // 1. nota del cajón del profe -> clave 142
            var escProfe = Datos("escritorio_profesor").Select(d => d.GetComponent<Note>()).FirstOrDefault(n => n && EnSalonInicio(n.transform.position));
            yield return Interactuar(escProfe, "nota_cajon_profe", "Nota del cajón del profe (pista 142)");
            CerrarOverlays();

            // 2. cartas en los pupitres
            foreach (var n in FindObjectsByType<Note>(FindObjectsSortMode.None).Where(n => EnSalonInicio(n.transform.position) && n != escProfe).Take(3).ToList())
            { yield return Interactuar(n, "carta_" + n.name, "Carta " + n.name); CerrarOverlays(); }

            // 3. casillero 142 -> destornillador + carta de papá
            var cas = FindObjectsByType<CombinationLock>(FindObjectsSortMode.None)
                      .FirstOrDefault(c => c.CorrectCode.SequenceEqual(new[] { 1, 4, 2 }));
            yield return Candado(cas, "casillero", "Casillero con clave 142");
            Revisar(Inv() != null && Inv().HasItem("destornillador"), "Casillero entrega el destornillador");
            yield return new WaitForSecondsRealtime(0.5f);
            CerrarOverlays();                                   // carta de Pa
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Captura("tarjeta_destornillador");
            Revisar(Time.timeScale == 0f, "Al recoger el destornillador aparece su tarjeta (el juego espera)");
            yield return Captura("casillero_abierto_carta_papa");
            CerrarOverlays();

            // 4. rejilla sin destornillador ya probado arriba; lonchera 046 -> linterna + apagón
            var lon = FindObjectsByType<CombinationLock>(FindObjectsSortMode.None)
                      .FirstOrDefault(c => c.CorrectCode.SequenceEqual(new[] { 0, 4, 6 }));
            yield return Candado(lon, "lonchera", "Lonchera con clave 046");
            Revisar(Inv() != null && Inv().HasItem("linterna"), "Lonchera entrega la linterna");
            // capacidad: sin mochila, solo lo que cabe en las dos manos
            if (Inv() != null)
            {
                Revisar(Inv().Ocupados == 2 && Inv().Capacidad == 2, $"Manos llenas con destornillador y linterna ({Inv().Ocupados}/{Inv().Capacidad})");
                Revisar(!Inv().PuedeCargar("piedra"), "Un tercer objeto no cabe sin mochila");
                Revisar(Inv().PuedeCargar("moneda"), "Una moneda sí cabe (va al bolsillo)");
            }
            var mochila = FindAnyObjectByType<MochilaEquipable>();
            Revisar(mochila != null, "Está la mochila de Mateo en el 4°B");
            if (mochila)
            {
                yield return Interactuar(mochila, "mochila", "Mochila de Mateo");
                Revisar(Inv() != null && Inv().TieneMochila && Inv().Capacidad == 6, "Con la mochila puede cargar 6 cosas");
                CerrarOverlays();
                // el cuaderno: manos / mochila, y pasar de una a otra
                Inventory.InventoryOverlay.Open();
                yield return new WaitForSecondsRealtime(1f);
                yield return Captura("cuaderno_objetos");
                Inventory.InventoryOverlay.OpenTab(Inventory.InventoryOverlay.Tab.Notas);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Captura("cuaderno_pistas");
                Inventory.InventoryOverlay.Close();
                string m = Inv().Mover("destornillador");
                Revisar(!Inv().EstaEnMano("destornillador"), $"Espacio guarda el destornillador en la mochila ({m})");
            }
            yield return new WaitForSecondsRealtime(1.5f);
            Revisar(BlackoutEvent.Instance != null && BlackoutEvent.Instance.IsBlackedOut, "Se van las luces al abrir la lonchera");
            CerrarOverlays();
            yield return Captura("apagon");
            var lint = Jugador()?.GetComponentsInChildren<Flashlight>(true).FirstOrDefault(f => f.GetComponent<Light>());
            if (lint) { lint.SetEnabled(true); yield return new WaitForSecondsRealtime(0.5f); }
            Revisar(lint && lint.IsUnlocked && lint.IsOn, "La linterna se puede encender");
            foreach (var hm in FindObjectsByType<HiddenMessage>(FindObjectsSortMode.None).Take(2))
            { Mirar(hm.transform.position, 2.2f); yield return new WaitForSecondsRealtime(2.5f); yield return Captura("mensaje_oculto"); }
            if (Stress.StressSystem.Instance) Anotar($"Estrés tras el apagón: {Stress.StressSystem.Instance.CurrentStress:F0}");
            if (EstanDentro.UI.OverlayBlocker.IsBlocking) Anotar("AVISO: hay un minijuego/overlay bloqueando (posible respiración por estrés)");
            Stress.StressSystem.Instance?.ResetTo(0f);
            CerrarOverlays();

            // 4b. casilleros que se abren: monedas y tiza (y la puerta queda abierta)
            var jug = Jugador();
            foreach (var botin in new[] { "moneda", "tiza" })
            {
                var casB = FindObjectsByType<Casillero>(FindObjectsSortMode.None)
                          .Where(c => c.botinId == botin && !c.GetComponent<CombinationLock>() && Mathf.Abs(c.transform.position.y - jug.transform.position.y) < 2f)
                          .OrderBy(c => Vector3.Distance(c.transform.position, jug.transform.position)).FirstOrDefault();
                if (!casB) { Revisar(false, $"Hay un casillero con {botin} en este piso"); continue; }
                var puerta = casB.GetComponentsInChildren<DatosBlender>().FirstOrDefault(x => x.Get("prop_tipo") == "puerta_casillero");
                Quaternion antes = puerta ? puerta.transform.rotation : Quaternion.identity;
                float altoAntes = puerta ? puerta.GetComponent<Renderer>().bounds.center.y : 0f;
                yield return Acercarse(casB);
                Revisar(Alcanza(casB), $"Casillero con {botin}: se puede mirar y alcanzar");
                casB.Interact();
                yield return new WaitForSecondsRealtime(1.4f);
                CerrarOverlays();
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Captura("casillero_" + botin);
                Revisar(puerta && Quaternion.Angle(antes, puerta.transform.rotation) > 60f, $"La puerta del casillero gira ({(puerta ? Quaternion.Angle(antes, puerta.transform.rotation) : 0):F0}°)");
                Revisar(Inv().HasItem(botin), $"El casillero entrega {botin}");
                float altoDespues = puerta ? puerta.GetComponent<Renderer>().bounds.center.y : 0f;
                Revisar(puerta && Mathf.Abs(altoDespues - altoAntes) < 0.1f, $"La puerta abierta sigue de pie (su centro se movió {Mathf.Abs(altoDespues - altoAntes) * 100f:F0} cm en altura)");
            }
            var tiza = Jugador().GetComponent<MarcaTiza>();
            Revisar(tiza != null, "El jugador puede marcar con tiza");
            if (tiza)
            {
                Mirar(Jugador().transform.position + Jugador().transform.forward * 1.5f + Vector3.up * 1.2f, 0f);
                yield return new WaitForSecondsRealtime(0.3f);
                tiza.IntentarMarcar();
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Captura("marca_tiza");
                Revisar(tiza.MarcasHechas == 1, "La tiza deja una marca en la pared");
            }

            // 4b'. un recuerdo y un susto en casilleros
            var conRecuerdo = FindObjectsByType<Casillero>(FindObjectsSortMode.None).Where(c => Recuerdos.Es(c.botinId)).ToList();
            var conSusto = FindObjectsByType<Casillero>(FindObjectsSortMode.None).Where(c => !string.IsNullOrEmpty(c.susto)).ToList();
            Revisar(conRecuerdo.Count == 10, $"Hay 10 recuerdos en casilleros ({conRecuerdo.Count})");
            Revisar(conSusto.Count >= 4, $"Hay casilleros con susto ({conSusto.Count})");
            // lejos de los papelitos con clave: una puerta abierta podría taparlos para la prueba siguiente
            var papelitos = FindObjectsByType<DatosBlender>(FindObjectsSortMode.None).Where(d => !string.IsNullOrEmpty(d.Get("pista_de"))).Select(d => d.transform.position).ToList();
            bool Lejos(Casillero c) => papelitos.All(q => Vector3.Distance(q, c.transform.position) > 4f);
            conRecuerdo = conRecuerdo.OrderBy(c => Lejos(c) ? 0 : 1).ToList();
            conSusto = conSusto.OrderBy(c => Lejos(c) ? 0 : 1).ToList();
            if (conRecuerdo.Count > 0)
            {
                var cr = conRecuerdo[0];
                int notasAntes = Inv().ReadNotes.Count;
                yield return Acercarse(cr);
                cr.Interact();
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Captura("recuerdo");
                CerrarOverlays();
                Revisar(Inv().ReadNotes.Count == notasAntes + 1, $"El recuerdo «{Recuerdos.De(cr.botinId).titulo}» queda en Pistas");
            }
            if (conSusto.Count > 0)
            {
                var cs = conSusto[0];
                string tipo = cs.susto;
                float antesSusto = Sustos.UltimoSusto;
                yield return Acercarse(cs);
                cs.Interact();
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Captura("susto_" + tipo);
                yield return new WaitForSecondsRealtime(4.5f);
                CerrarOverlays();
                Revisar(Sustos.UltimoSusto > antesSusto, $"El casillero da su susto ({tipo})");
                Revisar(string.IsNullOrEmpty(cs.susto), "El susto pasa una sola vez");
                Stress.StressSystem.Instance?.ResetTo(0f);
            }

            // 4c. casillero con candado: la clave está en un papelito cerca
            var conCandado = FindObjectsByType<Casillero>(FindObjectsSortMode.None)
                             .Where(c => c.GetComponent<CombinationLock>() && !c.GetComponent<CombinationLock>().IsSolved)
                             .OrderBy(c => Vector3.Distance(c.transform.position, Jugador().transform.position)).ToList();
            Revisar(conCandado.Count >= 2, $"Hay casilleros con candado ({conCandado.Count})");
            if (conCandado.Count >= 2)
            {
                var objetivo = conCandado[0];
                var clObj = objetivo.GetComponent<CombinationLock>();
                var pista = FindObjectsByType<DatosBlender>(FindObjectsSortMode.None).FirstOrDefault(d => d.Get("pista_de") == objetivo.name);
                Revisar(pista && pista.GetComponent<Note>(), "Su pista (un papelito) está cerca y se puede leer");
                if (pista)
                {
                    Revisar(Vector3.Distance(pista.transform.position, objetivo.transform.position) < 7f, $"La pista está a {Vector3.Distance(pista.transform.position, objetivo.transform.position):F1} m del casillero");
                    yield return Interactuar(pista.GetComponent<Note>(), "pista_casillero", "Papelito con la clave");
                    CerrarOverlays();
                    string clave = string.Join("", clObj.CorrectCode);
                    var so = pista.GetComponent<Note>();
                    var txt = so ? typeof(Note).GetField("noteText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(so) as string : "";
                    string soloDigitos = new string((txt ?? "").Where(char.IsDigit).ToArray());
                    Revisar(soloDigitos.Contains(clave), $"El papelito trae la clave correcta ({clave})");
                }
                yield return Acercarse(objetivo);
                objetivo.Interact();                                   // abre la cámara sobre el candado
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Captura("candado_casillero_pista");
                Revisar(InspeccionCandado.Abierta, "Mirar el casillero acerca la cámara a su candado");
                clObj.NotifyAttempt(clObj.CorrectCode.ToArray());
                yield return new WaitForSecondsRealtime(2.2f);
                CerrarOverlays();
                Revisar(clObj.IsSolved, "La clave del papelito abre el candado");

                // pata de cabra: dos manos, y fuerza otro candado (con ruido)
                var pata = FindObjectsByType<Recogible>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r.itemId == "pata_de_cabra");
                Revisar(pata != null, "Está la pata de cabra (cuarto del celador)");
                if (pata)
                {
                    yield return Interactuar(pata, "pata_de_cabra", "Pata de cabra");
                    CerrarOverlays();
                    Revisar(Inv().EstaEnMano("pata_de_cabra") && Inv().ManosOcupadas == 2, "La pata de cabra ocupa las dos manos");
                    Revisar(!Inv().EstaEnMano("linterna") && Inv().HasItem("linterna"), "Lo que tenía en las manos pasó solo a la mochila");
                    var otro = conCandado[1];
                    var clOtro = otro.GetComponent<CombinationLock>();
                    float antesRuido = Ruido.CuandoUltimo;
                    yield return Acercarse(otro);
                    otro.Interact();
                    yield return new WaitForSecondsRealtime(1.2f);
                    InspeccionCandado.ForzarActual();
                    yield return new WaitForSecondsRealtime(2.4f);
                    CerrarOverlays();
                    yield return Captura("candado_forzado");
                    Revisar(clOtro.IsSolved, "La pata de cabra fuerza el candado sin la clave");
                    Revisar(Ruido.CuandoUltimo > antesRuido, "Forzar hace ruido (lo oirían «ellos»)");
                    Revisar(Inv().Soltar("pata_de_cabra", Inv().PuntoParaSoltar()), "G suelta la pata de cabra para seguir");
                }
            }

            // 5. rejilla del techo -> ductos
            var rej = FindObjectsByType<RejillaDucto>(FindObjectsSortMode.None).FirstOrDefault(r => !string.IsNullOrEmpty(r.escenaDestino));
            Revisar(rej != null, "La rejilla del 4°B lleva a los ductos");
            if (rej)
            {
                yield return Acercarse(rej);
                Revisar(Alcanza(rej), "Desde el piso se alcanza la rejilla con la mira");
                rej.Interact(); yield return new WaitForSecondsRealtime(2f);
                Revisar(rej.abierta, "Con destornillador se quita la rejilla");
                Revisar(Inv() != null && Inv().EstaEnMano("destornillador"), "Mateo saca solo el destornillador de la mochila para usarlo");
                yield return Captura("rejilla_quitada");
                rej.Interact();
                yield return EsperarEscena("EsecenaUno-DuctosDeVentilacion", 25f);
                yield return new WaitForSecondsRealtime(6f);
                yield return Captura("ductos");

                // 6. salida de los ductos -> vuelve al colegio frente a la rectoría
                var salida = FindObjectsByType<ExitDuctsTrigger>(FindObjectsSortMode.None).FirstOrDefault();
                Revisar(salida != null, "Los ductos tienen salida");
                if (salida) { Teletransportar(salida.GetComponent<Collider>().bounds.center, Quaternion.identity); }
                yield return EsperarEscena("Colegio", 40f);
                yield return new WaitForSecondsRealtime(5f);
                var pc2 = Jugador();
                Revisar(pc2 && Vector3.Distance(pc2.transform.position, (GameObject.Find("SpawnPoint_salas_finales") ? GameObject.Find("SpawnPoint_salas_finales").transform.position : new Vector3(14.7f, 0f, 8.9f))) < 2.5f,
                        $"Al salir de los ductos aparece frente a la rectoría ({(pc2 ? pc2.transform.position.ToString("F1") : "sin jugador")})");
                yield return Captura("vuelta_al_colegio");
                Revisar(Inv() != null && Inv().HasItem("destornillador") && Inv().HasItem("linterna") && Inv().TieneMochila,
                        "Al volver de los ductos conserva el destornillador, la linterna y la mochila");
                Revisar(FindObjectsByType<Inventory.Inventory>(FindObjectsSortMode.None).Length == 1, "Hay un solo inventario en la partida");
            }

            // 7. pistas del 1705 en la secretaría
            foreach (var n in FindObjectsByType<Note>(FindObjectsSortMode.None).Where(n => n.transform.position.x > 28.67f && n.transform.position.z < 7f && n.transform.position.y < 2f).Take(2).ToList())
            { yield return Interactuar(n, "pista_1705", "Pista del 1705 en la secretaría"); CerrarOverlays(); }

            // 8. rectoría 1705 -> final
            var rect = FindObjectsByType<CombinationLock>(FindObjectsSortMode.None)
                       .FirstOrDefault(c => c.CorrectCode.SequenceEqual(new[] { 1, 7, 0, 5 }) && c.GetComponent<LockedDoor>());
            if (rect)
            {
                var puerta = rect.GetComponent<LockedDoor>();
                Revisar(puerta.IsLocked, "La rectoría empieza cerrada");
                yield return Candado(rect, "rectoria", "Puerta de la rectoría con clave 1705");
                yield return new WaitForSecondsRealtime(0.5f);
                Revisar(!puerta.IsLocked, "La clave 1705 desbloquea la rectoría");
                puerta.Interact(); yield return new WaitForSecondsRealtime(2f);
                Revisar(puerta.IsOpen, "La puerta de la rectoría se abre");
                yield return Captura("rectoria_abierta");
            }
            else Revisar(false, "Existe la puerta de la rectoría con clave 1705");
            var fin = FindObjectsByType<EndGameTrigger>(FindObjectsSortMode.None).FirstOrDefault();
            Revisar(fin != null, "Hay un final dentro de la rectoría");
            if (fin)
            {
                Teletransportar(fin.GetComponent<Collider>().bounds.center, Quaternion.identity);
                for (int k = 0; k < 4; k++) { yield return new WaitForSecondsRealtime(4f); yield return Captura("final"); }
            }
            Terminar();
        }

        // ------------------------------------------------------------------ pasos
        IEnumerator Interactuar(Interactable obj, string captura, string que)
        {
            if (!obj) { Revisar(false, que + ": no existe en la escena"); yield break; }
            yield return Acercarse(obj);
            Revisar(Alcanza(obj), que + ": se puede mirar y alcanzar");
            obj.Interact();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Captura(captura);
            Revisar(true, que + ": se interactuó");
        }

        IEnumerator Candado(CombinationLock cl, string captura, string que)
        {
            if (!cl) { Revisar(false, que + ": no existe en la escena"); yield break; }
            yield return Acercarse(cl);
            Revisar(Alcanza(cl), que + ": se puede mirar y alcanzar");
            cl.Interact();
            yield return new WaitForSecondsRealtime(1f);
            yield return Captura(captura + "_candado");
            cl.NotifyAttempt(cl.CorrectCode.ToArray());
            LockOverlay.Close();
            yield return new WaitForSecondsRealtime(1.5f);
            Revisar(cl.IsSolved, que + ": se resuelve con la clave correcta");
        }

        IEnumerator Acercarse(Component obj)
        {
            CerrarOverlays();
            var b = Limites(obj);
            Vector3 c = b.center;
            // busca, en una cuadrícula de 10 cm, el lugar libre más cercano (0,5 a 2,2 m) desde donde un jugador de 0,7 m
            // de ancho, parado en el piso de ese mismo salón, vea el objeto sin nada en medio
            var candidatos = new List<Vector3>();
            for (float dx = -2.2f; dx <= 2.2f; dx += 0.1f)
                for (float dz = -2.2f; dz <= 2.2f; dz += 0.1f)
                {
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d >= 0.5f && d <= 2.2f) candidatos.Add(new Vector3(c.x + dx, 0f, c.z + dz));
                }
            candidatos.Sort((u, v) => ((u.x - c.x) * (u.x - c.x) + (u.z - c.z) * (u.z - c.z)).CompareTo((v.x - c.x) * (v.x - c.x) + (v.z - c.z) * (v.z - c.z)));
            foreach (var cand in candidatos)
            {
                var pie = cand;
                if (!Physics.Raycast(new Vector3(pie.x, c.y, pie.z), Vector3.down, out var suelo, 4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (suelo.normal.y < 0.9f) continue;
                pie.y = suelo.point.y;
                if (pie.y > b.min.y + 0.2f || pie.y < b.min.y - 3.6f) continue;      // mismo piso que el objeto
                if (Physics.CheckCapsule(pie + Vector3.up * 0.4f, pie + Vector3.up * 1.65f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) continue;
                Vector3 ojo = pie + Vector3.up * 1.65f;
                if (Vector3.Distance(ojo, c) > 2.9f) continue;
                if (Physics.Linecast(ojo, c, out var h, ~0, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(obj.transform) && h.transform != obj.transform
                    && !(h.collider.GetComponent<CandadoVisual>() is CandadoVisual cv && cv.cerradura && cv.cerradura.transform == obj.transform)) continue;
                Teletransportar(pie, Quaternion.identity);
                Mirar(c, 0f);
                yield return new WaitForSecondsRealtime(0.6f);
                yield break;
            }
            Anotar($"AVISO: no hay ningún lugar libre desde donde alcanzar '{obj.name}' (¿muebles demasiado juntos?)");
        }

        // ------------------------------------------------------------------ utilidades
        PlayerController Jugador() => FindAnyObjectByType<PlayerController>();
        Inventory.Inventory Inv() => Inventory.Inventory.Instance;

        static IEnumerable<DatosBlender> Datos(string tipo) =>
            FindObjectsByType<DatosBlender>(FindObjectsSortMode.None).Where(d => d.Get("prop_tipo") == tipo);

        static bool EnSalonInicio(Vector3 p) => p.x > 19.33f && p.x < 28.67f && p.z > 29f && p.z < 36f && p.y > 6.5f && p.y < 11f;

        static Bounds Limites(Component c)
        {
            var r = c.GetComponentInChildren<Renderer>();
            if (r) return r.bounds;
            var col = c.GetComponentInChildren<Collider>();
            return col ? col.bounds : new Bounds(c.transform.position, Vector3.one * 0.2f);
        }

        void Teletransportar(Vector3 pie, Quaternion rot)
        {
            var pc = Jugador(); if (!pc) return;
            var cc = pc.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            pc.transform.position = pie + Vector3.up * 0.05f;
            if (cc) cc.enabled = true;
            Physics.SyncTransforms();
        }

        void Mirar(Vector3 punto, float subir)
        {
            var pc = Jugador(); if (!pc) return;
            var cam = Camera.main ? Camera.main.transform : pc.CameraPivot;
            Vector3 d = punto + Vector3.up * subir - cam.position;
            Vector3 plano = new Vector3(d.x, 0f, d.z);
            if (plano.sqrMagnitude > 1e-4f) pc.transform.rotation = Quaternion.LookRotation(plano);
            pc.SetPitch(-Mathf.Atan2(d.y, plano.magnitude) * Mathf.Rad2Deg);
            if (pc.CameraPivot) pc.CameraPivot.localRotation = Quaternion.Euler(-Mathf.Atan2(d.y, plano.magnitude) * Mathf.Rad2Deg, 0f, 0f);
        }

        bool Alcanza(Component obj)
        {
            var cam = Camera.main; if (!cam) return false;
            Vector3 c = Limites(obj).center;
            var golpes = Physics.RaycastAll(cam.transform.position, (c - cam.transform.position).normalized, 3.2f, ~0, QueryTriggerInteraction.Ignore)
                                .Where(x => x.collider.transform.root != cam.transform.root).OrderBy(x => x.distance).ToList();
            if (golpes.Count == 0)
            { Anotar($"   (la mira no toca nada a 3,2 m de {obj.name}; distancia {Vector3.Distance(cam.transform.position, c):F2} m)"); return false; }
            var h = golpes[0];
            var cv = h.collider.GetComponent<CandadoVisual>();
            bool ok = h.transform == obj.transform || h.transform.IsChildOf(obj.transform) || obj.transform.IsChildOf(h.transform)
                      || (cv && cv.cerradura && cv.cerradura.transform == obj.transform);
            if (!ok) Anotar($"   (la mira choca primero con '{h.collider.name}' a {h.distance:F2} m)");
            return ok;
        }

        void CerrarOverlays()
        {
            try { NoteOverlay.Hide(); } catch { }
            try { LockOverlay.Close(); } catch { }
            try { Inventory.ItemPreviewOverlay.Close(); } catch { }
        }

        IEnumerator EsperarEscena(string nombre, float max)
        {
            float t = 0f;
            while (SceneManager.GetActiveScene().name != nombre && t < max) { t += Time.unscaledDeltaTime; yield return null; }
            Revisar(SceneManager.GetActiveScene().name == nombre, $"Carga la escena '{nombre}'");
        }

        IEnumerator Captura(string nombre)
        {
            yield return new WaitForEndOfFrame();
            nCaptura++;
            string ruta = Path.Combine(carpeta, $"{nCaptura:00}_{nombre}.png");
            ScreenCapture.CaptureScreenshot(ruta);
            yield return null;
        }

        void Revisar(bool ok, string que)
        {
            if (!ok) fallas++;
            Anotar((ok ? "OK    " : "FALLA ") + que);
        }

        void Anotar(string s)
        {
            Debug.Log("[AutoPrueba] " + s);
            informe.AppendLine(s);
            try { File.WriteAllText(Path.Combine(carpeta, "autoprueba.txt"), informe.ToString()); } catch { }
        }

        // ------------------------------------------------------------------ prólogo en el carro
        IEnumerator PruebaPrologo()
        {
            Anotar("Inicio del prólogo (carro de Pa)");
            yield return new WaitForSecondsRealtime(3f);
            var pr = Prologo.ControlPrologo.Instancia;
            Revisar(pr != null, "El prólogo arranca");
            if (pr == null) yield break;
            yield return Captura("prologo_inicio");
            yield return new WaitForSecondsRealtime(8f);
            yield return Captura("prologo_mirando_al_frente");
            foreach (var g in new[] { pr.cuaderno, pr.lonchera, pr.radio, pr.celular })
            {
                // mirar el objeto como lo haría el jugador y tocarlo
                var dir = (g.GetComponent<Collider>() ? g.GetComponent<Collider>().bounds.center : g.transform.position) - pr.ojos.position;
                pr.Apuntar(dir);
                yield return null; yield return null;
                bool alcanza = pr.Mirando == g;
                if (!alcanza) Anotar($"   (desde los ojos, {g.name} está a {dir.magnitude:F2} m; la mira ve '{pr.LoQueVe()}')");
                Revisar(alcanza, $"Prólogo: se alcanza a mirar {g.name}");
                yield return Captura("prologo_" + g.name);
                pr.Tocar(g);
                yield return new WaitForSecondsRealtime(7f);
                yield return Captura("prologo_" + g.name + "_dialogo");
                if (pr.Tocados.Count == 2) { yield return new WaitForSecondsRealtime(9f); yield return Captura("prologo_eleccion"); pr.Elegir(1); }
            }
            Revisar(pr.Tocados.Count == 4, $"Prólogo: se tocaron las 4 cosas ({pr.Tocados.Count})");
            float t0 = Time.realtimeSinceStartup;
            while (!pr.Termino && Time.realtimeSinceStartup - t0 < 120f) yield return null;
            Revisar(pr.Termino, "Prólogo: llega el presagio");
            yield return new WaitForSecondsRealtime(5.5f);
            yield return Captura("prologo_camion_a_lo_lejos");
            yield return new WaitForSecondsRealtime(1.6f);
            yield return Captura("prologo_camion_se_mete");
            t0 = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name == "Prologo" && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Revisar(SceneManager.GetActiveScene().name == "Colegio", "Prólogo: después del choque y la campana se despierta en el colegio");
        }

        // Henry: «los papeles creo que tienen físicas y se caen». Foto de dónde está cada papel al empezar y revisión al final.
        readonly Dictionary<Transform, Vector3> papeles = new Dictionary<Transform, Vector3>();
        static bool EsPapel(Transform t) { var d = t.GetComponent<DatosBlender>(); return (d && d.Get("prop_tipo").Contains("papel")) || t.GetComponent<Note>(); }
        void FotoPapeles()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (EsPapel(t)) papeles[t] = t.position;
            int conFisica = papeles.Keys.Count(t => t.GetComponentInParent<Rigidbody>() && !t.GetComponentInParent<Rigidbody>().isKinematic);
            int flotando = 0;
            foreach (var t in papeles.Keys)
            {
                var r = t.GetComponent<Renderer>(); if (!r) continue;
                var b = r.bounds; var o = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z);
                var hits = Physics.RaycastAll(o, Vector3.down, 3f).Where(h => !h.collider.transform.IsChildOf(t)).OrderBy(h => h.distance).ToList();
                if (hits.Count == 0 || hits[0].distance > 0.05f) { flotando++; if (flotando <= 5) Anotar($"   (papel flotando: '{t.name}' a {(hits.Count > 0 ? hits[0].distance : 3f):F2} m del apoyo, en {t.position:F2})"); }
            }
            Revisar(conFisica == 0, $"Ningún papel tiene física que lo haga caer ({conFisica} de {papeles.Count})");
            Revisar(flotando == 0, $"Ningún papel flota en el aire ({flotando} de {papeles.Count})");
        }

        void Terminar()
        {
            int movidos = 0;
            foreach (var kv in papeles)
                if (kv.Key && Vector3.Distance(kv.Key.position, kv.Value) > 0.05f) { movidos++; if (movidos <= 5) Anotar($"   (papel movido: '{kv.Key.name}' de {kv.Value:F2} a {kv.Key.position:F2})"); }
            Revisar(movidos == 0, $"Los papeles no se caen ni se mueven solos ({movidos} de {papeles.Count})");
            // guardado local (antes era la API de somee)
            Revisar(!Network.ApiClient.EnNube, "Guarda en local (sin URL de nube)");
            Revisar(File.Exists(Network.AlmacenLocal.Ruta), $"Existe el archivo de guardado ({Network.AlmacenLocal.Ruta})");
            Revisar(Network.AlmacenLocal.Partidas().Length > 0 && Network.AlmacenLocal.Jugadores().Length > 0, "Hay jugador y partida guardados");
            Revisar(Network.AlmacenLocal.LogrosXPartida().Length > 0, $"Quedó guardado algún logro ({Network.AlmacenLocal.LogrosXPartida().Length})");
            Anotar($"Fin. Fallas: {fallas}");
            StartCoroutine(Salir());
        }

        IEnumerator Salir() { yield return new WaitForSecondsRealtime(2f); Application.Quit(); }
    }
}
