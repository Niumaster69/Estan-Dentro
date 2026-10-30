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
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                Anotar("Menú principal");
                yield return new WaitForSecondsRealtime(4f);
                yield return Captura("menu");
                Revisar(FindAnyObjectByType<UI.MainMenuController>() != null, "El menú principal carga");
                FindAnyObjectByType<UI.MainMenuController>()?.OnPrologoClicked();       // la prueba pasa también por el prólogo
                yield return EsperarEscena("Prologo", 30f);
                yield return new WaitForSecondsRealtime(1f);
            }
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
            // el cajón del escritorio del profe se abre y la nota está adentro
            var cajon = FindObjectsByType<Cajon>(FindObjectsSortMode.None).FirstOrDefault(c => c.GetComponentInChildren<Note>());
            Revisar(cajon != null, "El escritorio del profe tiene un cajón que se abre");
            Note escProfe = null;
            if (cajon)
            {
                yield return Acercarse(cajon);
                cajon.Interact(); yield return new WaitForSecondsRealtime(0.8f);
                yield return Captura("cajon_profe_abierto");
                escProfe = cajon.GetComponentInChildren<Note>();
            }
            yield return Interactuar(escProfe, "nota_cajon_profe", "Nota del cajón del profe (pista 142)");
            CerrarOverlays();

            // 1b. el tablero se mira de cerca
            var tab = FindAnyObjectByType<TableroInteractivo>();
            Revisar(tab != null, "El tablero se puede mirar");
            if (tab)
            {
                yield return Acercarse(tab);
                Revisar(Alcanza(tab), "Tablero: se puede mirar y alcanzar");
                tab.Interact(); yield return new WaitForSecondsRealtime(1.5f);
                yield return Captura("tablero_de_cerca");
                var kbT = UnityEngine.InputSystem.Keyboard.current;
                tab.SendMessage("Salir", SendMessageOptions.DontRequireReceiver);
                yield return new WaitForSecondsRealtime(1f);
            }

            // 2. cartas en los pupitres
            foreach (var n in FindObjectsByType<Note>(FindObjectsSortMode.None).Where(n => EnSalonInicio(n.transform.position) && n != escProfe).Take(3).ToList())
            { yield return Interactuar(n, "carta_" + n.name, "Carta " + n.name); CerrarOverlays(); }

            // 3. casillero 142 -> destornillador + carta de papá
            var cas = FindObjectsByType<CombinationLock>(FindObjectsSortMode.None)
                      .FirstOrDefault(c => c.CorrectCode.SequenceEqual(new[] { 1, 4, 2 }));
            yield return Candado(cas, "casillero", "Casillero con clave 142");
            yield return new WaitForSecondsRealtime(0.5f);
            CerrarOverlays();                                   // carta de Pa
            yield return Coger(cas, "casillero_142");           // el destornillador SE VE adentro: otra E para cogerlo
            Revisar(Inv() != null && Inv().HasItem("destornillador"), "Casillero entrega el destornillador");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Captura("tarjeta_destornillador");
            Revisar(Time.timeScale == 0f, "Al recoger el destornillador aparece su tarjeta (el juego espera)");
            yield return Captura("casillero_abierto_carta_papa");
            CerrarOverlays();

            // 4. rejilla sin destornillador ya probado arriba; lonchera 046 -> linterna + apagón
            var lon = FindObjectsByType<CombinationLock>(FindObjectsSortMode.None)
                      .FirstOrDefault(c => c.CorrectCode.SequenceEqual(new[] { 0, 4, 6 }));
            yield return Candado(lon, "lonchera", "Lonchera con clave 046");
            CerrarOverlays();
            yield return Coger(lon, "lonchera");
            CerrarOverlays();
            Revisar(Inv() != null && Inv().HasItem("linterna"), "Lonchera entrega la linterna");
            // capacidad: sin mochila, solo lo que cabe en las dos manos
            if (Inv() != null)
            {
                Revisar(Inv().Ocupados == 2 && Inv().Capacidad == 2, $"Manos llenas con destornillador y linterna ({Inv().Ocupados}/{Inv().Capacidad})");
                Revisar(!Inv().PuedeCargar("piedra"), "Un tercer objeto no cabe sin mochila");
                Revisar(!Inv().PuedeCargar("moneda"), "Sin mochila y con las manos llenas, ni una moneda cabe");
            }
            var mochila = FindAnyObjectByType<MochilaEquipable>();
            Revisar(mochila != null, "Está la mochila de Mateo en el 4°B");
            if (mochila)
            {
                yield return Interactuar(mochila, "mochila", "Mochila de Mateo");
                Revisar(Inv() != null && Inv().TieneMochila && Inv().Capacidad == 8, "Con la mochila puede cargar 8 cosas (2 manos + 6)");
                // la mochila nueva (I): Mateo se agacha y se ven los dibujos
                CerrarOverlays(); yield return new WaitForSecondsRealtime(0.5f);
                Inventory.MochilaOverlay.Abrir();
                yield return new WaitForSecondsRealtime(1.5f);
                Revisar(Inventory.MochilaOverlay.EstaAbierta, "Con I se abre la mochila");
                yield return Captura("mochila_abierta");
                Inventory.MochilaOverlay.Cerrar();
                yield return new WaitForSecondsRealtime(1f);
                Revisar(!Inventory.MochilaOverlay.EstaAbierta && !EstanDentro.UI.OverlayBlocker.IsBlocking, "La mochila se cierra y el juego sigue");
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
            if (lint) { Inv()?.Usar("linterna"); lint.SetEnabled(true); yield return new WaitForSecondsRealtime(0.5f); }
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
                yield return Captura("casillero_" + botin);
                casB.Interact();                                 // coger lo que se ve adentro
                yield return new WaitForSecondsRealtime(0.8f);
                CerrarOverlays();
                yield return new WaitForSecondsRealtime(0.4f);
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
                CerrarOverlays();
                cr.Interact();                                   // coger el papel que se ve adentro
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
            // (lejos del recuerdo del pasillo: si el robot cae encima, lo gasta antes de tiempo; un jugador no llega ahí sin la llave)
            var zonaPasillo = GameObject.Find("Recuerdo_Pasillo4B");
            var conCandado = FindObjectsByType<Casillero>(FindObjectsSortMode.None)
                             .Where(c => c.GetComponent<CombinationLock>() && !c.GetComponent<CombinationLock>().IsSolved)
                             .Where(c => !zonaPasillo || Vector3.Distance(c.transform.position, zonaPasillo.transform.position) > 6f)
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
            var rej = FindObjectsByType<RejillaDucto>(FindObjectsSortMode.None).FirstOrDefault(r => !string.IsNullOrEmpty(r.escenaDestino) && r.name != "REJILLA_P1_RECT");   // la del 4°B, no la de la rectoría
            Revisar(rej != null, "La rejilla del 4°B lleva a los ductos");
            if (rej)
            {
                // desde el piso no se alcanza: pupitre -> casillero -> rejilla
                var sp0 = GameObject.Find("SpawnPoint_4b_desde_ducto");
                if (sp0) { Jugador().Teletransportar(sp0.transform.position); yield return new WaitForSecondsRealtime(0.5f); }
                Mirar(rej.transform.position, 0f); yield return new WaitForSecondsRealtime(0.3f);
                rej.Interact(); yield return new WaitForSecondsRealtime(0.5f);
                Revisar(!rej.abierta, "Desde el piso la rejilla está muy alta");
                var silla = FindObjectsByType<SillaArrastrable>(FindObjectsSortMode.None).Where(x => x.transform.position.y > 6.5f).OrderBy(x => Vector3.Distance(x.transform.position, x.destino)).FirstOrDefault();
                var trepar = GameObject.Find("Trepar_Casillero")?.GetComponent<Escalada>();
                Revisar(silla && trepar, "Hay silla para arrastrar y casillero para trepar");
                if (silla && trepar)
                {
                    yield return Acercarse(silla);
                    silla.Interact(); yield return new WaitForSecondsRealtime(0.5f);                 // agarrarla
                    // llevarla: Mateo camina hasta quedar frente al casillero con la silla delante
                    Vector3 alCas = new Vector3(silla.destino.x - trepar.arriba.x, 0f, silla.destino.z - trepar.arriba.z).normalized;
                    Vector3 pie = silla.destino + alCas * 0.9f; pie.y = Jugador().transform.position.y;
                    Jugador().Teletransportar(pie); Jugador().transform.rotation = Quaternion.LookRotation(-alCas);
                    yield return new WaitForSecondsRealtime(1.2f);
                    silla.Interact(); yield return new WaitForSecondsRealtime(0.6f);                 // soltarla junto al casillero
                    yield return Captura("silla_arrastrada");
                    silla.Interact(); yield return new WaitForSecondsRealtime(1.2f);                 // subirse
                    Revisar(Jugador().transform.position.y > 7.5f, $"Mateo se sube a la silla (pies a {Jugador().transform.position.y:F2})");
                    trepar.Interact(); yield return new WaitForSecondsRealtime(1.2f);
                    Revisar(Jugador().transform.position.y > 8.6f, $"Mateo se sube al casillero (pies a {Jugador().transform.position.y:F2})");
                    Mirar(rej.transform.position, 0f); yield return new WaitForSecondsRealtime(0.5f);
                    yield return Captura("encima_casillero");
                }
                Revisar(Alcanza(rej), "Desde el casillero se alcanza la rejilla con la mira");
                rej.Interact(); yield return new WaitForSecondsRealtime(2f);
                Revisar(rej.abierta, "Con destornillador se quita la rejilla");
                Revisar(Inv() != null && Inv().EstaEnMano("destornillador"), "Mateo saca solo el destornillador de la mochila para usarlo");
                yield return Captura("rejilla_quitada");
                rej.Interact();
                yield return EsperarEscena("EsecenaUno-DuctosDeVentilacion", 25f);
                yield return new WaitForSecondsRealtime(6f);
                yield return Captura("ductos");

                // 6. salida de los ductos -> cae DENTRO de la rectoría (sin recargar el colegio)
                var salida = FindObjectsByType<ExitDuctsTrigger>(FindObjectsSortMode.None).FirstOrDefault();
                Revisar(salida != null, "Los ductos tienen salida");
                if (salida) { Teletransportar(salida.GetComponent<Collider>().bounds.center, Quaternion.identity); }
                yield return EsperarEscena("Colegio", 40f);
                float tv = Time.realtimeSinceStartup; while (ViajeDuctos.Activo && Time.realtimeSinceStartup - tv < 20f) yield return null;
                yield return new WaitForSecondsRealtime(2f);
                var pc2 = Jugador();
                var spR = GameObject.Find("SpawnPoint_rectoria_dentro");
                Revisar(pc2 && spR && Vector3.Distance(pc2.transform.position, spR.transform.position) < 1.5f,
                        $"Al salir del ducto cae dentro de la rectoría ({(pc2 ? pc2.transform.position.ToString("F1") : "sin jugador")})");
                yield return Captura("rectoria_por_dentro");
                Revisar(Inv() != null && Inv().HasItem("destornillador") && Inv().HasItem("linterna") && Inv().TieneMochila,
                        "Al volver de los ductos conserva el destornillador, la linterna y la mochila");
                Revisar(FindObjectsByType<Inventory.Inventory>(FindObjectsSortMode.None).Length == 1, "Hay un solo inventario en la partida");
                Revisar(cas != null && cas.IsSolved, "El colegio sigue como lo dejaste (el casillero 142 sigue abierto)");
            }

            // 7. rectoría: la foto del recuerdo rompe el colegio (Pesadilla); la llave del 4°B solo está en la Pesadilla
            Revisar(GameObject.Find("Llave_4B") == null, "Antes del recuerdo la llave del 4°B no está");
            var foto = GameObject.Find("Foto_Dia_Familia")?.GetComponent<Note>();
            yield return Interactuar(foto, "foto_recuerdo", "Foto del Día de la familia");
            CerrarOverlays();
            float tp = 0f;
            while (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Pesadilla && tp < 8f) { tp += Time.unscaledDeltaTime; yield return null; }
            Revisar(Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Pesadilla, "La foto dispara la Pesadilla");
            yield return new WaitForSecondsRealtime(4f);
            var obs = FindAnyObjectByType<Mente.Observador>();
            Revisar(obs && obs.GetComponentInChildren<Renderer>().enabled, "En la Pesadilla aparece el Observador" + (obs ? $" ({Vector3.Distance(obs.transform.position, Jugador().transform.position):F1} m)" : ""));
            yield return Captura("pesadilla_rectoria");
            if (obs)
            {
                // mirarlo fijo: te ve y te caza (Alterado). Apenas pasa se pausa, para que no atrape al robot.
                Vector3 alObs = obs.transform.position - Jugador().transform.position; alObs.y = 0f;
                Jugador().transform.rotation = Quaternion.LookRotation(alObs);
                Jugador().SetPitch(0f);
                float tm = 0f;
                while (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Alterado && tm < 5f) { tm += Time.unscaledDeltaTime; Jugador().transform.rotation = Quaternion.LookRotation(new Vector3(obs.transform.position.x - Jugador().transform.position.x, 0f, obs.transform.position.z - Jugador().transform.position.z)); yield return null; }
                Mente.Observador.Pausado = true;
                Revisar(Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Alterado, $"Mirarlo fijo dispara la persecución (Alterado) en {tm:F1} s");
                yield return new WaitForSecondsRealtime(0.4f); yield return Captura("alterado_persecucion");
                Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Pesadilla);
                tm = 0f; while (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Pesadilla && tm < 6f) { tm += Time.unscaledDeltaTime; yield return null; }
                Revisar(Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Pesadilla, "Al perderte vuelve la Pesadilla");
            }
            Mente.Observador.Pausado = true;                                   // que no persiga al robot de la prueba
            var llave = GameObject.Find("Llave_4B")?.GetComponent<Recogible>();
            yield return Interactuar(llave, "llave_4b", "Llave del 4°B (en la Pesadilla, en otro lugar)");
            CerrarOverlays(); yield return new WaitForSecondsRealtime(0.5f);
            Revisar(Inv() != null && Inv().HasItem("llave_4b"), "Se coge la llave del 4°B");
            tp = 0f;
            while (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal && tp < 10f) { tp += Time.unscaledDeltaTime; yield return null; }
            Revisar(Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Normal, "Con la llave la Pesadilla se va");
            yield return new WaitForSecondsRealtime(1f); yield return Captura("rectoria_otra_vez_normal");
            var fin0 = FindObjectsByType<EndGameTrigger>(FindObjectsSortMode.None).FirstOrDefault();
            Revisar(fin0 == null, "La rectoría ya no tiene el final viejo");

            // 8. de vuelta: silla -> archivadores -> rejilla -> ducto -> 4°B
            var trepR = GameObject.Find("Trepar_Archivadores")?.GetComponent<Escalada>();
            var sillaR = FindObjectsByType<SillaArrastrable>(FindObjectsSortMode.None).Where(x => x.transform.position.x > 10f && x.transform.position.x < 19.4f && x.transform.position.z < 7.2f && x.transform.position.y < 2f)
                         .OrderBy(x => Vector3.Distance(x.transform.position, x.destino)).FirstOrDefault();
            var rejR = FindObjectsByType<RejillaDucto>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "REJILLA_P1_RECT");
            Revisar(trepR && sillaR && rejR, "En la rectoría hay silla, archivadores para subir y rejilla abierta");
            if (trepR && sillaR && rejR)
            {
                yield return Acercarse(sillaR);
                sillaR.Interact(); yield return new WaitForSecondsRealtime(0.5f);
                Vector3 alA = new Vector3(sillaR.destino.x - trepR.arriba.x, 0f, sillaR.destino.z - trepR.arriba.z).normalized;
                Vector3 pieR = sillaR.destino + alA * 0.9f; pieR.y = Jugador().transform.position.y;
                Jugador().Teletransportar(pieR); Jugador().transform.rotation = Quaternion.LookRotation(-alA);
                yield return new WaitForSecondsRealtime(1.2f);
                sillaR.Interact(); yield return new WaitForSecondsRealtime(0.6f);
                sillaR.Interact(); yield return new WaitForSecondsRealtime(1.2f);
                trepR.Interact(); yield return new WaitForSecondsRealtime(1.2f);
                Revisar(Jugador().transform.position.y > 1.2f, $"Mateo se sube a los archivadores (pies a {Jugador().transform.position.y:F2})");
                Mirar(rejR.transform.position, 0f); yield return new WaitForSecondsRealtime(0.4f);
                yield return Captura("rectoria_rejilla");
                rejR.Interact();
                yield return EsperarEscena("EsecenaUno-DuctosDeVentilacion", 25f);
                yield return new WaitForSecondsRealtime(4f);
                var salida2 = FindObjectsByType<ExitDuctsTrigger>(FindObjectsSortMode.None).FirstOrDefault();
                if (salida2) Teletransportar(salida2.GetComponent<Collider>().bounds.center, Quaternion.identity);
                yield return EsperarEscena("Colegio", 40f);
                float tv2 = Time.realtimeSinceStartup; while (ViajeDuctos.Activo && Time.realtimeSinceStartup - tv2 < 20f) yield return null;
                yield return new WaitForSecondsRealtime(2f);
                var sp4 = GameObject.Find("SpawnPoint_4b_desde_ducto");
                Revisar(sp4 && Vector3.Distance(Jugador().transform.position, sp4.transform.position) < 1.5f, "Por el ducto vuelve al 4°B");
                yield return Captura("vuelta_al_4b");
            }

            // 9. la puerta del 4°B con la llave
            var puerta4b = FindObjectsByType<LockedDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.llaveId == "llave_4b");
            Revisar(puerta4b != null, "La puerta del 4°B se abre con llave");
            if (puerta4b)
            {
                yield return Acercarse(puerta4b);
                puerta4b.Interact(); yield return new WaitForSecondsRealtime(2f);
                Revisar(!puerta4b.IsLocked && puerta4b.IsOpen, "Con la llave se abre la puerta del 4°B");
                yield return Captura("puerta_4b_abierta");
            }
            Revisar(GameObject.Find("Barricada_NO") && GameObject.Find("Barricada_SE"), "Las escaleras del piso 3 tienen barricada");

            // 9b. al salir al pasillo el colegio se rompe otra vez (hasta llegar al aula de música)
            var zonaP = GameObject.Find("Recuerdo_Pasillo4B");
            Revisar(zonaP != null, "Hay recuerdo en el pasillo del 4°B");
            if (zonaP)
            {
                Teletransportar(zonaP.transform.position + Vector3.down * 1.25f, Jugador().transform.rotation);
                float tz = 0f;
                while (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Pesadilla && tz < 8f) { tz += Time.unscaledDeltaTime; yield return null; }
                Revisar(Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Pesadilla, "Salir al pasillo dispara la Pesadilla");
                yield return new WaitForSecondsRealtime(1f); yield return Captura("pasillo_pesadilla");
            }

            // 9c. (rama de mejoras) el pasillo da vueltas en la Pesadilla y respirar con Pa lo rompe
            var bucle = GameObject.Find("Mejoras_BuclePasillo");
            Revisar(bucle != null, "Hay bucle en el pasillo norte");
            if (bucle && Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Pesadilla)
            {
                Mente.Observador.Pausado = true;
                Vector3 enBucle = bucle.transform.position + Vector3.down * 1.25f;
                Teletransportar(enBucle, Quaternion.LookRotation(Vector3.left));
                yield return new WaitForSecondsRealtime(0.8f);
                float x1 = Jugador().transform.position.x;
                Revisar(x1 > bucle.transform.position.x + 6f, $"El pasillo devuelve a Mateo frente al 4°B (x {x1:F1})");
                yield return Captura("bucle_vuelta");
                var st = Stress.StressSystem.Instance;
                float e0 = st ? st.Normalized : 0f; int b0 = Concentrarse.CiclosBuenos;
                for (int c = 0; c < 2; c++)                                            // 4 inhala + 4 sostén (Q) · 6 exhala (suelta)
                {
                    Concentrarse.SimularQ = true; yield return new WaitForSecondsRealtime(8f);
                    if (c == 0) yield return Captura("respirar_con_pa");
                    Concentrarse.SimularQ = false; yield return new WaitForSecondsRealtime(6.1f);
                }
                Revisar(Concentrarse.CiclosBuenos - b0 >= 2, $"Respirar con Pa: ciclos bien hechos ({Concentrarse.CiclosBuenos - b0})");
                Revisar(st && st.Normalized < e0, $"Respirar con Pa baja el estrés ({e0:P0} → {(st ? st.Normalized : 0f):P0})");
                Teletransportar(enBucle, Quaternion.LookRotation(Vector3.left));
                yield return new WaitForSecondsRealtime(0.8f);
                float x2 = Jugador().transform.position.x;
                Revisar(x2 < bucle.transform.position.x + 3f, $"Después de respirar el pasillo ya no da vueltas (x {x2:F1})");
            }

            // 10. el aula de música: el recuerdo y el final del capítulo
            var rec = FindAnyObjectByType<RecuerdoAula>();
            Revisar(rec != null, "Existe el recuerdo del aula de música");
            if (rec)
            {
                Teletransportar(rec.transform.position + Vector3.down * 1.3f, Quaternion.LookRotation(Vector3.left));
                float ta = 0f;
                while ((Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal || Mente.EstadoMente.EnTransicion) && ta < 10f) { ta += Time.unscaledDeltaTime; yield return null; }
                Revisar(Mente.EstadoMente.Actual == Mente.EstadoMente.Estado.Normal, $"El aula de música deshace la Pesadilla ({ta:F1} s)");
                yield return new WaitForSecondsRealtime(8f); yield return Captura("recuerdo_aula_1");
                yield return new WaitForSecondsRealtime(12f); yield return Captura("recuerdo_aula_2");
                if (rec.finalHospital)
                {
                    // final del hospital: cinco textos, la respiración con Pa (el robot no respira: espera el tiempo límite),
                    // título y "Lo que aprendiste" (sigue sola a los 40 s)
                    yield return new WaitForSecondsRealtime(22f); yield return Captura("final_hospital_texto");
                    yield return new WaitForSecondsRealtime(28f); yield return Captura("final_hospital_respira");
                    float tf = 0f;
                    while (GameObject.Find("Final_Hospital") == null && tf < 5f) { tf += Time.unscaledDeltaTime; yield return null; }
                    Revisar(GameObject.Find("Final_Hospital") != null, "El final del hospital aparece después del recuerdo");
                    yield return new WaitForSecondsRealtime(70f); yield return Captura("final_lo_que_aprendiste");
                    yield return EsperarEscena("MainMenu", 120f);
                }
                else yield return EsperarEscena("MainMenu", 60f);
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

        /// <summary>Segunda E: coger lo que quedó a la vista dentro del contenedor (y fotografiarlo antes).</summary>
        IEnumerator Coger(CombinationLock cl, string captura)
        {
            if (!cl) yield break;
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Captura(captura + "_adentro");
            cl.Interact();
            yield return new WaitForSecondsRealtime(0.6f);
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
            foreach (var g in new[] { pr.cuaderno, pr.lonchera, pr.celular, pr.radio })   // la radio es lo último (lleva al choque)
            {
                // como el jugador: se espera a que nadie esté hablando (antes no se puede tocar)
                float tw = Time.realtimeSinceStartup;
                while (!pr.PuedeTocar && Time.realtimeSinceStartup - tw < 45f) yield return null;
                while (g == pr.radio && !pr.RadioLista && Time.realtimeSinceStartup - tw < 90f) yield return null;   // la radio se ofrece al final
                while (!pr.PuedeTocar && Time.realtimeSinceStartup - tw < 120f) yield return null;
                // mirar el objeto como lo haría el jugador y tocarlo
                var dir = (g.GetComponent<Collider>() ? g.GetComponent<Collider>().bounds.center : g.transform.position) - pr.ojos.position;
                pr.Apuntar(dir);
                yield return null; yield return null;
                // la cabeza gira desde el cuello: los ojos se movieron, se vuelve a apuntar desde donde quedaron
                dir = (g.GetComponent<Collider>() ? g.GetComponent<Collider>().bounds.center : g.transform.position) - pr.ojos.position;
                pr.Apuntar(dir); yield return null; yield return null;
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
                if (r.bounds.size.y > 0.15f) continue;                  // de pie: pegado con cinta en una puerta (no está "flotando")
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
