using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>Algo que cierra el paso en este nivel (portón principal, reja de la escalera): solo dice por qué no se puede.</summary>
    public class Bloqueo : Interactable
    {
        public string accion = "Empujar";
        [TextArea] public string mensaje = "No abre.";
        public AudioClip sonido;

        public override string Accion => accion;

        public override void Interact()
        {
            ObjectiveHUD.Show(mensaje, 2.5f);
            if (sonido) AudioSource.PlayClipAtPoint(sonido, transform.position, 0.8f);
            else EstanDentro.Audio.SonidoCorto.Juego("candado/no_abre", transform.position, 0.8f, 0.6f);
        }
    }
}
