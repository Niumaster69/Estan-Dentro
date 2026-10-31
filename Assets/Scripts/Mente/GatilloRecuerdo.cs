using UnityEngine;

namespace EstanDentro.Mente
{
    /// <summary>En un objeto que es un recuerdo del accidente: al terminar de leerlo, el colegio se rompe.</summary>
    public class GatilloRecuerdo : MonoBehaviour
    {
        public string tituloNota;
        public string pensamiento = "¿Pa…? No, no, no…";
        public string resolverCon;
        bool hecho;

        /// <summary>Lo llama la nota al cerrarse (Note.Interact).</summary>
        public void Disparar()
        {
            Debug.Log($"[Mente] recuerdo '{tituloNota}' (hecho={hecho})");
            if (hecho || Colegio.Exploracion.Activo) return;
            hecho = true;
            ControlMente.Recuerdo(pensamiento, resolverCon);
        }
    }
}
