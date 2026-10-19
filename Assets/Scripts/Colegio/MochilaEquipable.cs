using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>La mochila de Mateo: al tomarla se la pone y pasa de cargar 2 cosas (manos) a 6.</summary>
    public class MochilaEquipable : Interactable
    {
        public AudioClip sonido;

        public override void Interact()
        {
            var inv = Inventory.Inventory.Instance;
            if (inv == null) return;
            inv.PonerMochila();
            if (sonido) AudioSource.PlayClipAtPoint(sonido, transform.position);
            // la tarjeta de objeto ("Ahora puedes cargar 6 cosas") la abre Inventory.PonerMochila()
            gameObject.SetActive(false);
        }
    }
}
