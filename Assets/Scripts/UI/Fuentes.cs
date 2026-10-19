using UnityEngine;

namespace EstanDentro.UI
{
    /// <summary>Las fuentes del menú (Special Elite y Creepster) disponibles en cualquier escena, sin pasar por el menú.</summary>
    public static class Fuentes
    {
        static Font maquina, titulo;

        public static Font Maquina => maquina ? maquina : (maquina = Cargar("Fuentes/SpecialElite-Regular"));
        public static Font Titulo => titulo ? titulo : (titulo = Cargar("Fuentes/Creepster-Regular"));

        static Font Cargar(string ruta)
        {
            var f = Resources.Load<Font>(ruta);
            if (f) return f;
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
