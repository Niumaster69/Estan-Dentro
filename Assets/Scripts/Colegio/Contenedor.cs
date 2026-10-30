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

        GameObject visible;

        public override string Accion => visible ? "Tomar: " + botinNombre.ToLower() : registrado ? "Ya lo revisé" : "Revisar " + nombreVisible.ToLower();

        public override void Interact()
        {
            // primera E: se revisa y lo que haya SE VE; segunda E: se coge
            if (visible) { Tomar(); return; }
            if (sonidoAbrir) AudioSource.PlayClipAtPoint(sonidoAbrir, transform.position);
            if (registrado || string.IsNullOrEmpty(botinId))
            {
                ObjectiveHUD.Show(registrado ? "Ya lo revisé." : $"Reviso {nombreVisible.ToLower()}... nada.", 1.6f);
                registrado = true;
                return;
            }
            visible = BotinVisible.Poner(botinId, gameObject);
            if (!visible) Tomar();
        }

        void Tomar()
        {
            var inv = Inventory.Inventory.Instance;
            if (inv != null && !inv.RegisterItem(botinId, botinNombre))
            {
                ObjectiveHUD.Show(inv.MotivoLleno(), 3f);   // se queda adentro para después
                return;
            }
            registrado = true;
            if (visible) Destroy(visible);
        }
    }
}
