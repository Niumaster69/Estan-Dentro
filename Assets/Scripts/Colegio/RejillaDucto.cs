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

        public override void Interact()
        {
            var inv = Inventory.Inventory.Instance;
            if (!abierta)
            {
                if (inv != null && !string.IsNullOrEmpty(requiereId) && !inv.Usar(requiereId))
                {
                    ObjectiveHUD.Show("Los tornillos no ceden. Necesitas un destornillador.", 2f);
                    return;
                }
                abierta = true;
                ObjectiveHUD.Show("Quitas la rejilla. Puedes subir al ducto.", 2f);
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
                SceneTransition.LoadScene(escenaDestino, tip: tipCarga);
                return;
            }
            ObjectiveHUD.Show($"El ducto lleva hacia {salida}... (aún no se puede entrar)", 2f);
        }

    }
}
