using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>Se registra (mochila, escritorio, archivador, lonchera...). Puede traer un objeto o estar vacío.</summary>
    public class Contenedor : Interactable
    {
        public string nombreVisible = "Contenedor";
        [Tooltip("Vacío = no hay nada. Se define en la tabla de botín de cada sala.")]
        public string botinId = "";
        public string botinNombre = "";
        public bool registrado;
        public AudioClip sonidoAbrir;

        public override void Interact()
        {
            if (sonidoAbrir) AudioSource.PlayClipAtPoint(sonidoAbrir, transform.position);
            if (registrado || string.IsNullOrEmpty(botinId))
            {
                ObjectiveHUD.Show(registrado ? "Ya lo revisaste." : $"Revisas {nombreVisible.ToLower()}... nada.", 1.6f);
                registrado = true;
                return;
            }
            var inv = Inventory.Inventory.Instance;
            if (inv != null && !inv.RegisterItem(botinId, botinNombre))
            {
                ObjectiveHUD.Show($"Hay {botinNombre.ToLower()} aquí. {inv.MotivoLleno()}", 3f);   // se queda adentro para después
                return;
            }
            registrado = true;
        }
    }
}
