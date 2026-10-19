using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>Casillero doble, cubículo, cuarto bajo la escalera: el jugador se esconde.</summary>
    public class Escondite : Interactable
    {
        public static Escondite Actual { get; private set; }
        public Transform puntoDentro;
        private Vector3 posAntes;
        private Transform jugador;

        public override void Interact()
        {
            if (jugador == null)
            {
                var pc = FindAnyObjectByType<Player.PlayerController>();
                if (pc == null) return;
                jugador = pc.transform;
            }
            var cc = jugador.GetComponent<CharacterController>();
            if (Actual == this)
            {
                if (cc) cc.enabled = false;
                jugador.position = posAntes;
                if (cc) cc.enabled = true;
                Actual = null;
                ObjectiveHUD.Show("Sales del escondite.", 1.2f);
                return;
            }
            posAntes = jugador.position;
            if (cc) cc.enabled = false;
            jugador.position = puntoDentro ? puntoDentro.position : transform.position + Vector3.up * 0.05f;
            if (cc) cc.enabled = true;
            Actual = this;
            ObjectiveHUD.Show("Escondido. Contén la respiración.", 1.8f);
        }
    }
}
