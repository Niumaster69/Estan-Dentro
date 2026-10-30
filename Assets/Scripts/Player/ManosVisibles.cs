using System.Collections.Generic;
using UnityEngine;

namespace EstanDentro.Player
{
    /// <summary>
    /// Lo que Mateo lleva en las manos SE VE, abajo frente a la cámara (Henry: "que en físico estén ahí, estilo Hello
    /// Neighbor"). Usa los modelos de Resources/Objetos. La linterna no se duplica: ya tiene su propio modelo en la mano.
    /// Se mece un poco al caminar y desaparece mientras hay una ventana abierta (mochila, cuaderno, notas).
    /// </summary>
    public class ManosVisibles : MonoBehaviour
    {
        PlayerController jugador;
        Transform ancla;
        readonly Dictionary<string, GameObject> vistos = new Dictionary<string, GameObject>();
        string firma = "";
        Vector3 ultimaPos; float paso;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Enganchar()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => Poner();
            Poner();
        }

        static void Poner()
        {
            var j = FindAnyObjectByType<PlayerController>();
            if (j && !j.GetComponent<ManosVisibles>()) j.gameObject.AddComponent<ManosVisibles>();
        }

        void Start()
        {
            jugador = GetComponent<PlayerController>();
            var cam = GetComponentInChildren<Camera>();
            ancla = new GameObject("Manos_Objetos").transform;
            ancla.SetParent(cam ? cam.transform : (jugador.CameraPivot ? jugador.CameraPivot : transform), false);
            ultimaPos = transform.position;
        }

        void LateUpdate()
        {
            var inv = Inventory.Inventory.Instance;
            bool ver = inv != null && !UI.OverlayBlocker.IsBlocking && !Inventory.MochilaOverlay.EstaAbierta;
            ancla.gameObject.SetActive(ver);
            if (inv == null) return;
            var enMano = inv.CarriedItems.FindAll(x => x.enMano && !Inventory.Inventory.VaAlBolsillo(x.id) && x.id != "linterna");
            string f = string.Join("|", enMano.ConvertAll(x => x.id));
            if (f != firma) { firma = f; Rehacer(enMano, inv); }
            // mecerse al caminar
            float v = (transform.position - ultimaPos).magnitude / Mathf.Max(Time.deltaTime, 1e-4f); ultimaPos = transform.position;
            paso += Time.deltaTime * Mathf.Lerp(1.5f, 8f, Mathf.Clamp01(v / 3f));
            float amp = Mathf.Clamp01(v / 3f) * 0.012f;
            ancla.localPosition = new Vector3(Mathf.Sin(paso) * amp, Mathf.Abs(Mathf.Cos(paso)) * amp - 0.005f, 0f);
        }

        void Rehacer(List<Inventory.Inventory.ItemEntry> enMano, Inventory.Inventory inv)
        {
            foreach (var g in vistos.Values) if (g) Destroy(g);
            vistos.Clear();
            bool linternaEnMano = inv.EstaEnMano("linterna") && inv.HasItem("linterna");
            for (int i = 0; i < enMano.Count; i++)
            {
                var it = enMano[i];
                var molde = Resources.Load<GameObject>("Objetos/" + Inventory.DibujosObjetos.Modelo(it.id));
                if (!molde) continue;
                var go = Instantiate(molde, ancla);
                foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // tamaño de mano: lo más largo mide ~18 cm (lo de dos manos, ~40 cm al centro)
                bool doble = Inventory.Inventory.DosManos(it.id);
                var b = Limites(go);
                float largo = Mathf.Max(b.size.x, b.size.y, b.size.z);
                go.transform.localScale *= (doble ? 0.40f : 0.18f) / Mathf.Max(largo, 1e-4f);
                // la mano libre: si la linterna ocupa la derecha, lo demás va a la izquierda
                bool izquierda = linternaEnMano || i == 0;
                Vector3 pos = doble ? new Vector3(0f, -0.26f, 0.42f) : new Vector3(izquierda ? -0.21f : 0.21f, -0.24f, 0.40f);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(-15f, izquierda ? 30f : -30f, izquierda ? 12f : -12f);
                vistos[it.id] = go;
            }
        }

        static Bounds Limites(GameObject go)
        {
            bool hay = false; Bounds b = default;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
