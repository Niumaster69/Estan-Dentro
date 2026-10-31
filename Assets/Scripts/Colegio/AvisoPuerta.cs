using UnityEngine;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Delante de una puerta con candado: Mateo piensa en voz alta dónde puede estar la clave (sin decirla).
    /// Se repite cada <see cref="cada"/> segundos mientras la puerta siga cerrada.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class AvisoPuerta : MonoBehaviour
    {
        public Interaction.LockedDoor puerta;
        [TextArea] public string pensamiento;
        public float cada = 25f;
        float ultimo = -99f;

        void Awake() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider c)
        {
            if (!c.GetComponentInParent<Player.PlayerController>() || !puerta || !puerta.IsLocked || Exploracion.Activo) return;
            if (Time.time - ultimo < cada) return;
            ultimo = Time.time;
            ObjectiveHUD.Notify(pensamiento, 4.5f);
        }
    }
}
