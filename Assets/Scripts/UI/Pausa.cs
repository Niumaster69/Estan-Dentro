using System.Collections.Generic;
using UnityEngine;

namespace EstanDentro.UI
{
    /// <summary>
    /// Único dueño de Time.timeScale. Cada menú que congela el juego pide la pausa y la suelta al cerrarse;
    /// el tiempo solo vuelve a correr cuando nadie la tiene pedida (antes cada menú guardaba y restauraba
    /// su propio timeScale y, con dos menús abiertos a la vez, el juego quedaba congelado o corriendo detrás de una nota).
    /// </summary>
    public static class Pausa
    {
        static readonly HashSet<object> duenos = new HashSet<object>();

        public static bool Activa => duenos.Count > 0;

        public static void Pedir(object dueno)
        {
            if (dueno == null) return;
            duenos.Add(dueno);
            Time.timeScale = 0f;
        }

        public static void Soltar(object dueno)
        {
            duenos.Remove(dueno);
            duenos.RemoveWhere(d => d is Object o && o == null);   // menús destruidos sin cerrarse
            if (duenos.Count == 0) Time.timeScale = 1f;
        }

        /// <summary>Al cambiar de escena o reiniciar: nadie tiene la pausa.</summary>
        public static void Reiniciar()
        {
            duenos.Clear();
            Time.timeScale = 1f;
        }
    }
}
