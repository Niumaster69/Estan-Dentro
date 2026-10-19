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

        public override void Interact()
        {
            var inv = Inventory.Inventory.Instance;
            bool eraNuevo = inv != null && !inv.YaVisto(itemId);
            if (inv != null && !inv.RegisterItem(itemId, nombreVisible)) { ObjectiveHUD.Show(inv.MotivoLleno(), 2.5f); return; }
            inv?.RecordarOrigen(itemId, gameObject);
            // la primera vez lo explica la tarjeta de objeto; las siguientes basta un aviso corto
            if (!eraNuevo) ObjectiveHUD.Show($"Recogiste: {nombreVisible}", 1.8f);
            if (sonido) AudioSource.PlayClipAtPoint(sonido, transform.position);
            gameObject.SetActive(false);
        }
    }
}
