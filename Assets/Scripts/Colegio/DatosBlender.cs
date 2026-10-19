using System.Collections.Generic;
using UnityEngine;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Propiedades personalizadas que el objeto traía desde Blender (funcion, interactuable, prop_tipo,
    /// bisagra_sentido, ducto_salida...). Las llena el importador del FBX.
    /// </summary>
    public class DatosBlender : MonoBehaviour
    {
        public List<string> claves = new List<string>();
        public List<string> valores = new List<string>();

        public string Get(string clave, string porDefecto = "")
        {
            int i = claves.IndexOf(clave);
            return i >= 0 ? valores[i] : porDefecto;
        }

        public bool Tiene(string clave) => claves.Contains(clave);

        public void Set(string clave, string valor)
        {
            int i = claves.IndexOf(clave);
            if (i >= 0) valores[i] = valor;
            else { claves.Add(clave); valores.Add(valor); }
        }
    }
}
