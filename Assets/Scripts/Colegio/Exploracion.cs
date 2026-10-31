using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Modo exploración (botón EXPLORAR EL COLEGIO del menú): el colegio entero para recorrerlo sin la historia.
    /// Todas las puertas sin llave, sin barricadas ni portón, sin Observador ni Pesadillas ni estrés; Mateo empieza con la
    /// linterna y la mochila. En el mapa del cuaderno (M) se ve todo y con Espacio se viaja a la sala elegida.
    /// </summary>
    public static class Exploracion
    {
        public static bool Activo { get; private set; }

        public static void Empezar() { Activo = true; }
        public static void Terminar() { Activo = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Instalar()
        {
            SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
            AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void AlCargar(Scene s, LoadSceneMode modo)
        {
            if (s.name == "MainMenu") { Terminar(); Mente.Observador.Pausado = false; return; }
            if (!Activo || s.name != "Colegio") return;
            new GameObject("__ModoExploracion").AddComponent<Preparar>();
        }

        /// <summary>Lleva a Mateo a una sala del mapa, mirando hacia su centro.</summary>
        public static void IrA(MapaColegio.Sala s)
        {
            var j = Object.FindAnyObjectByType<Player.PlayerController>();
            if (!j || s == null) return;
            var pies = new Vector3(s.px, s.py, s.pz);
            j.Teletransportar(pies);
            var mira = new Vector3(s.cx - s.px, 0f, s.cz - s.pz);
            if (mira.sqrMagnitude > 0.5f) j.transform.rotation = Quaternion.LookRotation(mira);
            j.SetPitch(0f);
            MapaColegio.Visitar(s.id);
            ObjectiveHUD.Notify(s.nombre + " · " + MapaColegio.Pisos[s.piso].nombre, 2.5f);
        }

        class Preparar : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return null;                                       // que todos hagan su Start primero
                Mente.Observador.Pausado = true;
                var obs = FindAnyObjectByType<Mente.Observador>(FindObjectsInactive.Include); if (obs) obs.gameObject.SetActive(false);
                var j = FindAnyObjectByType<Player.PlayerController>();
                var despertar = j ? j.GetComponent<Player.PlayerWakeUp>() : null;
                if (despertar) despertar.AbortAndCleanup();
                if (j) j.InputEnabled = true;

                int puertas = 0, bloqueos = 0, gatillos = 0;
                foreach (var p in FindObjectsByType<Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (p.IsLocked) { p.Unlock(); puertas++; }
                foreach (var b in FindObjectsByType<Bloqueo>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { b.gameObject.SetActive(false); bloqueos++; }
                // lo que dispara la historia: recuerdos que rompen el colegio, el pasillo que da vueltas, el final del capítulo
                foreach (var g in FindObjectsByType<Mente.GatilloZona>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { g.gameObject.SetActive(false); gatillos++; }
                foreach (var g in FindObjectsByType<Mente.BuclePasillo>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { g.gameObject.SetActive(false); gatillos++; }
                foreach (var g in FindObjectsByType<RecuerdoAula>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { g.gameObject.SetActive(false); gatillos++; }
                foreach (var g in FindObjectsByType<Interaction.ObserverApparitionTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { g.enabled = false; gatillos++; }

                var inv = Inventory.Inventory.Instance;
                if (inv != null)
                {
                    inv.DarSinTarjeta("mochila", "Mochila");
                    if (!inv.HasItem("linterna")) inv.DarSinTarjeta("linterna", "Linterna");
                    inv.AddMission("explorar", "Recorre el colegio. [M] abre el mapa: elige una sala y [Espacio] te lleva.", Inventory.Inventory.MissionCategory.Principal);
                }
                Debug.Log($"[Exploracion] {puertas} puertas sin llave, {bloqueos} bloqueos quitados, {gatillos} gatillos de historia apagados");
                yield return new WaitForSeconds(0.8f);
                ObjectiveHUD.Notify("Modo exploración: todas las puertas abren.", 3.5f);
                GuiaTeclas.PrimeraVez("M", "Mapa del colegio (viaje rápido)");
                // sin estrés ni Pesadilla mientras se explora
                while (true)
                {
                    var st = Stress.StressSystem.Instance; if (st && st.Normalized > 0.05f) st.ResetTo(0f);
                    if (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal && !Mente.EstadoMente.EnTransicion) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Normal);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }
}
