using UnityEngine;

namespace EstanDentro.Prologo
{
    /// <summary>
    /// La pantalla de bloqueo del celular de Mateo (imagen hecha en Blender: 02_Arte3D_Blender/scripts_blender/pantalla_celular.py):
    /// la hora, la fecha, el fondo con Pa en el estadio y la notificación del grupo. Brilla con luz propia; al tocar el
    /// celular se ilumina más (llega el mensaje) y luego baja.
    /// </summary>
    public class PantallaCelular : MonoBehaviour
    {
        public float brillo = 1400f, brilloMensaje = 3200f;   // nits (con la exposición de día nublado, EV 12.3)
        Material mt; float hasta;

        void Start()
        {
            mt = GetComponent<Renderer>().material;
            mt.SetColor("_EmissiveColor", Color.white * brillo);
        }

        public void Mensaje(float seg = 6f) { hasta = Time.time + seg; }

        void Update()
        {
            if (!mt) return;
            float b = Time.time < hasta ? brilloMensaje : brillo;
            mt.SetColor("_EmissiveColor", Color.white * Mathf.Lerp(mt.GetColor("_EmissiveColor").r, b, Time.deltaTime * 4f));
        }
    }
}
