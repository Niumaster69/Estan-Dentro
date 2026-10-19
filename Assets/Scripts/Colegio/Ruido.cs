using System;
using UnityEngine;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Ruidos que "ellos" pueden oír (forzar un candado, tirar algo, correr…). Hoy solo se avisan y se anotan;
    /// el monstruo del modo normal se suscribirá a Ruido.AlOir para ir a investigar.
    /// </summary>
    public static class Ruido
    {
        /// <summary>(dónde, qué tan lejos se oye en metros, qué fue)</summary>
        public static event Action<Vector3, float, string> AlOir;
        public static Vector3 Ultimo { get; private set; }
        public static float CuandoUltimo { get; private set; } = -999f;

        public static void Emitir(Vector3 donde, float radio, string que)
        {
            Ultimo = donde; CuandoUltimo = Time.time;
            Debug.Log($"[Ruido] {que} en {donde:F1} (se oye a {radio:F0} m)");
            AlOir?.Invoke(donde, radio, que);
        }
    }
}
