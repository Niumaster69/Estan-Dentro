using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>Objeto que va al inventario (linterna, baterías, llaves, cartas, radio...).</summary>
    public class Recogible : Interactable
    {
        public string itemId = "objeto";
        public string nombreVisible = "Objeto";
        public AudioClip sonido;

        public override string Accion => "Recoger: " + nombreVisible;

        /// <summary>
        /// Lo delgado (la pata de cabra de canto: 2 cm) se le escapaba a la mira. La caja para mirarlo tiene al menos 8 cm
        /// por lado; lo que se ve no cambia.
        /// </summary>
        void Awake()
        {
            var bc = GetComponent<BoxCollider>();
            if (!bc)
            {
                // con malla de colisión (de Blender) se le pone además una caja a su medida
                var mf = GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) return;
                bc = gameObject.AddComponent<BoxCollider>(); bc.center = mf.sharedMesh.bounds.center; bc.size = mf.sharedMesh.bounds.size;
            }
            var s = transform.lossyScale;
            const float minimo = 0.08f;
            bc.size = Vector3.Max(bc.size, new Vector3(minimo / Mathf.Max(1e-4f, Mathf.Abs(s.x)), minimo / Mathf.Max(1e-4f, Mathf.Abs(s.y)), minimo / Mathf.Max(1e-4f, Mathf.Abs(s.z))));
        }


        public override void Interact()
        {
            var inv = Inventory.Inventory.Instance;
            bool eraNuevo = inv != null && !inv.YaVisto(itemId);
            if (inv != null && !inv.RegisterItem(itemId, nombreVisible)) { ObjectiveHUD.Show(inv.MotivoLleno(), 2.5f); return; }
            inv?.RecordarOrigen(itemId, gameObject);
            // la primera vez lo explica la tarjeta de objeto; las siguientes basta un aviso corto
            if (!eraNuevo) ObjectiveHUD.Show($"Recogiste: {nombreVisible}", 1.8f);
            if (sonido) AudioSource.PlayClipAtPoint(sonido, transform.position);
            // cada objeto que trae una tecla nueva la enseña la primera vez
            if (itemId == "linterna") GuiaTeclas.PrimeraVez("F", "Prender o apagar la linterna");
            else if (itemId == "tiza") GuiaTeclas.PrimeraVez("T", "Marcar la pared con tiza (para no perderte)");
            else if (itemId == "llave_4b") ObjectiveHUD.Notify("La llave del 4°B… Puedo volver por el ducto.", 3f);
            else if (itemId == "pata_de_cabra") GuiaTeclas.PrimeraVez("G", "Soltar lo que tienes en las manos");
            gameObject.SetActive(false);
        }
    }
}
