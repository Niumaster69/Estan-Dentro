using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>Máquina de snacks / gaseosas / agua: con una moneda entrega un objeto.</summary>
    public class Dispensador : Interactable
    {
        public string requiereId = "moneda";
        public string entregaId = "gaseosa";
        public string entregaNombre = "Gaseosa";

        public override string Accion => "Usar";


        public override void Interact()
        {
            var inv = Inventory.Inventory.Instance;
            if (inv == null) return;
            if (!string.IsNullOrEmpty(requiereId) && !inv.HasItem(requiereId))
            {
                ObjectiveHUD.Show("Necesito una moneda.", 1.6f);
                return;
            }
            if (!string.IsNullOrEmpty(requiereId)) inv.RemoveItem(requiereId);
            if (!inv.RegisterItem(entregaId, entregaNombre))
            {
                if (!string.IsNullOrEmpty(requiereId)) inv.RegisterItem(requiereId, "Moneda");   // devuelve la moneda
                ObjectiveHUD.Show(inv.MotivoLleno(), 2.5f);
                return;
            }
            ObjectiveHUD.Show($"La máquina suelta: {entregaNombre}", 2f);
        }
    }
}
