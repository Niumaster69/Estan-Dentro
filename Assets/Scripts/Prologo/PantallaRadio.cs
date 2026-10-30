using UnityEngine;

namespace EstanDentro.Prologo
{
    /// <summary>
    /// La pantalla del radio del Corolla (imágenes de 02_Arte3D_Blender/scripts_blender/pantalla_radio.py): la emisora que suena
    /// y "Buscando…" cuando Pa cambia de emisora en el presagio (así se entiende por qué quita los ojos de la calle).
    /// </summary>
    public class PantallaRadio : MonoBehaviour
    {
        public Texture2D emisora, buscando;
        public float brillo = 1200f;
        Material mt;

        void Start()
        {
            mt = GetComponent<Renderer>().material;
            Poner(emisora);
        }

        public void Buscando() => Poner(buscando);

        void Poner(Texture2D t)
        {
            if (!mt || !t) return;
            mt.SetTexture("_EmissiveColorMap", t); mt.SetTexture("_BaseColorMap", t);
            mt.SetColor("_EmissiveColor", Color.white * brillo);
        }
    }
}
