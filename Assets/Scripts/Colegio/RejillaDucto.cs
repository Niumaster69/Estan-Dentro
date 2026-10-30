using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>Rejilla del techo: con destornillador se abre; la segunda vez se entra al ducto (escena aparte).</summary>
    public class RejillaDucto : Interactable
    {
        public string salida = "";
        public string salidaAlterna = "";
        public string requiereId = "destornillador";
        public bool abierta;

        [Header("Si lleva a otra escena")]
        public string escenaDestino = "";
        public string tipCarga = "";
        public string misionCompletada = "";
        [Tooltip("Dónde aparece Mateo al salir del ducto (PlayerSpawnPoint del colegio). Vacío = como antes (recarga).")]
        public string salidaEn = "";

        [Tooltip("Para alcanzarla, los ojos deben estar a menos de esto (m) de la rejilla (hay que subirse a algo)")]
        public float alcance = 1.3f;
        bool Alcanza { get { var c = Camera.main; return !c || Mathf.Abs(transform.position.y - c.transform.position.y) < alcance; } }

        public override string Accion => !Alcanza ? "Rejilla del techo (muy alta)" : abierta ? "Entrar al ducto" : "Quitar la rejilla";


        public override void Interact()
        {
            var inv = Inventory.Inventory.Instance;
            if (!Alcanza) { ObjectiveHUD.Show("Está muy alta. Tengo que subirme a algo.", 3f); return; }
            if (!abierta)
            {
                if (inv != null && !string.IsNullOrEmpty(requiereId) && !inv.Usar(requiereId))
                {
                    ObjectiveHUD.Show("Los tornillos no ceden… necesito un destornillador.", 2f);
                    return;
                }
                abierta = true;
                ObjectiveHUD.Show("Los tornillos ceden… la rejilla se cae. Arriba está oscuro.", 2.5f);
                // la rejilla cae (copia física) y el hueco queda para subir
                var cae = new GameObject(name + "_caida");
                cae.transform.SetPositionAndRotation(transform.position, transform.rotation);
                cae.transform.localScale = transform.lossyScale;
                var mf = GetComponent<MeshFilter>(); var mr = GetComponent<MeshRenderer>();
                if (mf && mr)
                {
                    cae.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    cae.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                    cae.AddComponent<MeshCollider>().convex = true;
                    var rb = cae.AddComponent<Rigidbody>(); rb.mass = 2f;
                    rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
                    mr.enabled = false;
                }
                return;
            }
            if (!string.IsNullOrEmpty(escenaDestino))
            {
                if (!string.IsNullOrEmpty(misionCompletada) && inv != null) inv.CompleteMission(misionCompletada);
                if (!string.IsNullOrEmpty(salidaEn)) ViajeDuctos.Entrar(salidaEn);       // ida y vuelta sin perder lo hecho
                else SceneTransition.LoadScene(escenaDestino, tip: tipCarga);
                return;
            }
            ObjectiveHUD.Show($"El ducto lleva hacia {salida}... (aún no se puede entrar)", 2f);
        }

    }
}
