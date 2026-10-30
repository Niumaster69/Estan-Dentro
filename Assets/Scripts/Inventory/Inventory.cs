using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EstanDentro.UI;
using EstanDentro.Network;

namespace EstanDentro.Inventory
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        // Cantidad de notas distintas necesarias para desbloquear 'notas_completas'.
        // Si en el futuro hay mas notas pedagogicas en el capitulo, subir este numero.
        private const int NotasParaLogroCompletas = 3;

        [System.Serializable]
        public class NoteEntry
        {
            public string title;
            public string body;
            public NoteEntry(string title, string body) { this.title = title; this.body = body; }
        }

        [System.Serializable]
        public class ItemEntry
        {
            public string id;          // identificador estable, ej. "linterna", "destornillador"
            public string displayName; // texto para mostrar al jugador
            public bool enMano = true;  // en una mano (listo para usar) o guardado en la mochila
            public int cantidad = 1;     // monedas, pilas y tiza se apilan en un solo lugar
            public float ultimoUso;      // para decidir qué guardar cuando hay que sacar otra cosa
            public ItemEntry(string id, string name) { this.id = id; this.displayName = name; }
        }

        public enum MissionCategory { Principal, Secundaria }

        [System.Serializable]
        public class MissionEntry
        {
            public string id;
            public string text;
            public MissionCategory category;
            public bool completed;
            public MissionEntry(string id, string text, MissionCategory cat)
            {
                this.id = id; this.text = text; this.category = cat; this.completed = false;
            }
        }

        public List<NoteEntry> ReadNotes { get; private set; } = new List<NoteEntry>();
        public List<ItemEntry> CarriedItems { get; private set; } = new List<ItemEntry>();
        public List<MissionEntry> Missions { get; private set; } = new List<MissionEntry>();
        public bool HasAny => ReadNotes.Count > 0;
        public int Count => ReadNotes.Count;

        public event Action OnInventoryChanged;
        /// <summary>Primera vez que se recoge un objeto en la partida: la tarjeta de objeto lo explica.</summary>
        public event Action<string, string> OnObjetoNuevo;
        private readonly HashSet<string> vistos = new HashSet<string>();

        // ----- Una sola mochila para toda la partida -----
        // El inventario sobrevive a los cambios de escena (antes cada escena tenía el suyo y al entrar/salir de
        // los ductos se perdía todo). Se vacía con NuevaPartida(): partida nueva, reintentar o terminar el capítulo.
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AsegurarInventario()
        {
            if (Instance == null && FindAnyObjectByType<Inventory>() == null)
                new GameObject("InventoryManager").AddComponent<Inventory>();
        }

        /// <summary>Vacía el inventario y quita la mochila (partida nueva, reintentar, fin del capítulo).</summary>
        public static void NuevaPartida()
        {
            if (Instance == null) return;
            Instance.TieneMochila = false;
            Instance.vistos.Clear();
            Instance.Clear();
        }

        // ----- Capacidad: sin mochila solo lo que cabe en las manos -----
        public const int Manos = 2;
        public const int EspaciosMochila = 6;          // Henry 20-oct: sin bolsillo, todo va en manos o mochila (lo que se junta ocupa un lugar)
        /// <summary>Cosas pequeñas que van al bolsillo y no ocupan espacio.</summary>
        /// <summary>Objetos grandes: ocupan las DOS manos y no caben en la mochila (pata de cabra, cizalla, galón).</summary>
        public static bool DosManos(string id) => id == "pata_de_cabra" || id == "cizalla" || id == "galon";

        /// <summary>Se juntan en un solo lugar (3 monedas = un hueco; 4 pilas = un hueco).</summary>
        public static bool SeApila(string id) => id == "moneda" || id == "baterias" || id == "tiza";

        // ya no hay bolsillo para cosas físicas (monedas, tiza, llaves ocupan un lugar en la mochila): solo papeles y fotos,
        // que van al cuaderno
        public static bool VaAlBolsillo(string id) =>
            !string.IsNullOrEmpty(id) && (id.StartsWith("nota") || id.StartsWith("foto"));

        public bool TieneMochila { get; set; }
        public int Capacidad => Manos + (TieneMochila ? EspaciosMochila : 0);
        public int Ocupados { get { int n = 0; foreach (var it in CarriedItems) if (!VaAlBolsillo(it.id)) n++; return n; } }
        public bool PuedeCargar(string id)
        {
            if (HasItem(id) || VaAlBolsillo(id)) return true;
            if (DosManos(id)) return LiberarManos(simular: true);
            return ManosOcupadas < Manos || (TieneMochila && MochilaOcupada < EspaciosMochila);
        }

        /// <summary>Para un objeto de dos manos: guarda lo que haya en las manos en la mochila (si cabe).</summary>
        bool LiberarManos(bool simular)
        {
            var enMano = CarriedItems.FindAll(x => x.enMano && !VaAlBolsillo(x.id));
            if (enMano.Exists(x => DosManos(x.id))) return false;                 // ya carga algo grande
            if (enMano.Count == 0) return true;
            if (!TieneMochila || MochilaOcupada + enMano.Count > EspaciosMochila) return false;
            if (!simular) { foreach (var x in enMano) x.enMano = false; }
            return true;
        }

        // ----- Manos y mochila -----
        // Lo que se recoge va a una mano libre; si no hay, a la mochila. Cuando algo hace falta (la rejilla pide el
        // destornillador, F pide la linterna) Mateo lo saca solo con Usar(): sin menús ni pasos extra.
        public int ManosOcupadas { get { int n = 0; foreach (var it in CarriedItems) if (!VaAlBolsillo(it.id) && it.enMano) n += DosManos(it.id) ? 2 : 1; return n; } }
        public int MochilaOcupada { get { int n = 0; foreach (var it in CarriedItems) if (!VaAlBolsillo(it.id) && !it.enMano) n++; return n; } }
        public ItemEntry Buscar(string id) { foreach (var it in CarriedItems) if (it.id == id) return it; return null; }
        public bool EstaEnMano(string id) { var it = Buscar(id); return it != null && (it.enMano || VaAlBolsillo(id)); }

        /// <summary>Pasa un objeto de la mano a la mochila o al revés (Espacio en el cuaderno). Devuelve un mensaje para el jugador.</summary>
        public string Mover(string id)
        {
            var it = Buscar(id);
            if (it == null || VaAlBolsillo(id)) return "";
            if (it.enMano)
            {
                if (DosManos(id)) return "No cabe en la mochila. La suelto (G) si necesito las manos.";
                if (!TieneMochila) return "No tengo mochila donde guardarlo.";
                if (MochilaOcupada >= EspaciosMochila) return "La mochila está llena.";
                it.enMano = false; OnInventoryChanged?.Invoke();
                return $"Guardas {it.displayName.ToLower()} en la mochila.";
            }
            if (CarriedItems.Exists(x => x.enMano && DosManos(x.id)))
                return "Tengo las manos ocupadas con algo grande. Lo suelto primero (G).";
            if (ManosOcupadas >= Manos)
            {
                // las manos están llenas: se cambia por lo que menos se ha usado
                var guardar = MenosUsadoEnMano(id);
                if (guardar == null) return "Tengo las manos llenas.";
                guardar.enMano = false; it.enMano = true; it.ultimoUso = Time.time; OnInventoryChanged?.Invoke();
                return $"Guardas {guardar.displayName.ToLower()} y sacas {it.displayName.ToLower()}.";
            }
            it.enMano = true; it.ultimoUso = Time.time; OnInventoryChanged?.Invoke();
            return $"Sacas {it.displayName.ToLower()}.";
        }

        /// <summary>
        /// Asegura que el objeto esté en la mano para usarlo; si estaba en la mochila lo saca (y si hace falta guarda otro).
        /// Devuelve false si no lo tiene.
        /// </summary>
        public bool Usar(string id)
        {
            var it = Buscar(id);
            if (it == null) return false;
            it.ultimoUso = Time.time;
            if (it.enMano || VaAlBolsillo(id)) return true;
            string msg = Mover(id);
            if (!string.IsNullOrEmpty(msg)) ObjectiveHUD.Notify(msg, 2.5f);
            return it.enMano;
        }

        ItemEntry MenosUsadoEnMano(string excepto)
        {
            ItemEntry r = null;
            foreach (var x in CarriedItems)
                if (x.enMano && !VaAlBolsillo(x.id) && x.id != excepto && (r == null || x.ultimoUso < r.ultimoUso)) r = x;
            return r;
        }

        public void PonerMochila()
        {
            TieneMochila = true;
            OnInventoryChanged?.Invoke();
            if (vistos.Add("mochila")) OnObjetoNuevo?.Invoke("mochila", "Mochila");
        }

        /// <summary>True si el objeto ya se había recogido antes en esta partida (la tarjeta ya lo explicó).</summary>
        public bool YaVisto(string id) => vistos.Contains(id);

        /// <summary>Mensaje cuando no cabe: Mateo lo dice con sus palabras.</summary>
        public string MotivoLleno() => TieneMochila
            ? (CarriedItems.Exists(x => x.enMano && DosManos(x.id)) ? "Tengo las dos manos ocupadas. Suelto algo (G)." : "La mochila está llena. Tengo que soltar algo (G).")
            : "Tengo las manos llenas. Necesito una mochila… o soltar algo (G).";

        /// <summary>De dónde salió cada objeto, para poder soltarlo en el piso (G).</summary>
        private readonly Dictionary<string, GameObject> origen = new Dictionary<string, GameObject>();
        public void RecordarOrigen(string id, GameObject go) { if (!string.IsNullOrEmpty(id) && go) origen[id] = go; }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool YaLeida(string title) { foreach (var n in ReadNotes) if (n.title == title) return true; return false; }

        public void RegisterNote(string title, string body)
        {
            // Evita duplicados (por title). Si dos notas tienen el mismo title, la segunda
            // se descarta. Si esto pasa, probablemente olvidaste setear noteTitle/noteText
            // en el Inspector de uno de los GameObjects de Note.
            foreach (var n in ReadNotes)
            {
                if (n.title == title)
                {
                    Debug.LogWarning($"[Inventory] Nota '{title}' ya estaba registrada — DESCARTADA. " +
                                     $"Si esto no es lo esperado, revisa que cada GameObject de Note tenga un title unico en su Inspector.");
                    return;
                }
            }
            ReadNotes.Add(new NoteEntry(title, body));
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Nota leida: '{title}'. Total: {ReadNotes.Count}");

            // Logro 'notas_completas': leer al menos N notas distintas en la partida
            if (ReadNotes.Count >= NotasParaLogroCompletas)
                GameSession.TryUnlockLogro("notas_completas");
        }

        // ----- Items (linterna, destornillador, etc) -----

        /// <summary>Guarda el objeto si cabe. Devuelve false (y no lo guarda) si las manos o la mochila están llenas.</summary>
        public bool RegisterItem(string id, string displayName = null)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var it in CarriedItems)
                if (it.id == id)
                {
                    if (SeApila(id)) { it.cantidad++; OnInventoryChanged?.Invoke(); }
                    return true;
                }
            if (!PuedeCargar(id))
            {
                Debug.Log($"[Inventory] No cabe '{id}' ({Ocupados}/{Capacidad}).");
                return false;
            }
            if (DosManos(id) && ManosOcupadas > 0)
            {
                LiberarManos(simular: false);
                ObjectiveHUD.Notify("Guardo lo que tenía: esto necesita las dos manos.", 3f);
            }
            // a una mano libre; si no hay, a la mochila
            CarriedItems.Add(new ItemEntry(id, displayName ?? id) { enMano = VaAlBolsillo(id) || ManosOcupadas < Manos, ultimoUso = Time.time });
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Item recogido: '{id}'. Ocupados {Ocupados}/{Capacidad}");
            if (vistos.Add(id)) OnObjetoNuevo?.Invoke(id, displayName ?? id);
            return true;
        }

        /// <summary>Suelta en el piso un objeto concreto (G en el cuaderno). Solo los que vinieron del piso de esta escena.</summary>
        public bool Soltar(string id, Vector3 donde)
        {
            var it = Buscar(id);
            if (it == null || VaAlBolsillo(id)) return false;
            if (!origen.TryGetValue(id, out var go) || go == null)
            {
                ObjectiveHUD.Notify("Mejor no lo dejo aquí. Puede servir.", 2f);
                return false;
            }
            go.transform.position = donde; go.SetActive(true);
            CarriedItems.Remove(it); origen.Remove(id);
            OnInventoryChanged?.Invoke();
            ObjectiveHUD.Notify($"Soltaste: {it.displayName}", 2f);
            return true;
        }

        public Vector3 PuntoParaSoltar()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            Vector3 p = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 0.6f;
            if (Physics.Raycast(p, Vector3.down, out var suelo, 3f, ~0, QueryTriggerInteraction.Ignore)) p = suelo.point + Vector3.up * 0.02f;
            return p;
        }

        /// <summary>Suelta en el piso el último objeto recogido que se pueda soltar (G).</summary>
        public bool SoltarUltimo(Vector3 donde)
        {
            for (int i = CarriedItems.Count - 1; i >= 0; i--)
            {
                var it = CarriedItems[i];
                if (VaAlBolsillo(it.id)) continue;
                if (!origen.TryGetValue(it.id, out var go) || go == null) continue;   // vino de otra escena o de un contenedor
                go.transform.position = donde;
                go.SetActive(true);
                CarriedItems.RemoveAt(i);
                origen.Remove(it.id);
                OnInventoryChanged?.Invoke();
                ObjectiveHUD.Notify($"Soltaste: {it.displayName}", 2f);
                return true;
            }
            ObjectiveHUD.Notify("No tengo nada que pueda dejar aquí.", 2f);
            return false;
        }

        public bool HasItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var it in CarriedItems) if (it.id == id) return true;
            return false;
        }

        public void RemoveItem(string id)
        {
            for (int i = 0; i < CarriedItems.Count; i++)
            {
                if (CarriedItems[i].id == id)
                {
                    if (CarriedItems[i].cantidad > 1) { CarriedItems[i].cantidad--; OnInventoryChanged?.Invoke(); return; }
                    CarriedItems.RemoveAt(i);
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        // ----- Misiones (objetivos del puzzle) -----

        public void AddMission(string id, string text, MissionCategory category = MissionCategory.Principal)
        {
            if (string.IsNullOrEmpty(id)) return;
            // Evitar duplicados; si existe, solo actualiza texto
            foreach (var m in Missions)
            {
                if (m.id == id) { m.text = text; m.category = category; m.completed = false; OnInventoryChanged?.Invoke(); return; }
            }
            Missions.Add(new MissionEntry(id, text, category));
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Mision nueva: '{id}' ({category}). Total misiones: {Missions.Count}");
        }

        public void CompleteMission(string id)
        {
            foreach (var m in Missions)
            {
                if (m.id == id)
                {
                    m.completed = true;
                    OnInventoryChanged?.Invoke();
                    Debug.Log($"[Inventory] Mision completada: '{id}'.");
                    return;
                }
            }
        }

        public void RemoveMission(string id)
        {
            for (int i = 0; i < Missions.Count; i++)
            {
                if (Missions[i].id == id)
                {
                    Missions.RemoveAt(i);
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        public bool HasMission(string id)
        {
            foreach (var m in Missions) if (m.id == id) return true;
            return false;
        }

        public void Clear()
        {
            ReadNotes.Clear();
            CarriedItems.Clear();
            Missions.Clear();
            OnInventoryChanged?.Invoke();
        }

        private void Update()
        {
            // No abrir si hay algun overlay modal activo
            if (OverlayBlocker.IsBlocking || OverlayBlocker.WasJustDismissed || InventoryOverlay.EstaAbierto || MochilaOverlay.EstaAbierta) return;
            if (Time.timeScale <= 0f) return;

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool hit = (kb != null && kb.iKey.wasPressedThisFrame)
                    || (gp != null && gp.selectButton.wasPressedThisFrame);
            // I = la mochila (Mateo se agacha a mirarla); Tab = el cuaderno (pistas y misiones)
            if (hit) { UI.GuiaTeclas.Usada("I"); MochilaOverlay.Abrir(); }
            if (kb != null && kb.tabKey.wasPressedThisFrame) { UI.GuiaTeclas.Usada("Tab"); InventoryOverlay.Open(); }
            if (kb != null && kb.gKey.wasPressedThisFrame)
            {
                UI.GuiaTeclas.Usada("G");
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 p = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 0.6f;
                    if (Physics.Raycast(p, Vector3.down, out var suelo, 3f, ~0, QueryTriggerInteraction.Ignore)) p = suelo.point + Vector3.up * 0.02f;
                    SoltarUltimo(p);
                }
            }
        }
    }
}
