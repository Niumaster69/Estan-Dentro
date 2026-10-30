using UnityEngine;

namespace EstanDentro.Mente
{
    /// <summary>Un lugar que rompe el colegio al pisarlo (una sola vez), p. ej. el pasillo al salir del 4°B.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GatilloZona : MonoBehaviour
    {
        public string id = "zona";
        public string pensamiento;
        public string resolverCon;

        void Awake() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider c)
        {
            if (!c.GetComponentInParent<Player.PlayerController>() || EstadoMente.Actual != EstadoMente.Estado.Normal) return;
            ControlMente.Recuerdo(pensamiento, resolverCon, id);
        }
    }
}
