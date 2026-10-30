using System.Collections;
using UnityEngine;
using EstanDentro.Interaction;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Cajón que sale de verdad (escritorio del profe): E lo abre y lo que hay adentro se ve y se coge aparte; E lo cierra.
    /// (Henry: "del escritorio del profe antes yo tenía que funcionaran los cajones donde se guardaban las cosas")
    /// </summary>
    public class Cajon : Interactable
    {
        public Vector3 salida = new Vector3(-0.32f, 0f, 0f);   // cuánto sale (en el mundo)
        bool abierto, moviendo;
        Vector3 cerrado;

        public override string Accion => abierto ? "Cerrar el cajón" : "Abrir el cajón";

        void Awake() { cerrado = transform.position; }

        public override void Interact()
        {
            if (moviendo) return;
            abierto = !abierto;
            StartCoroutine(Mover(abierto ? cerrado + salida : cerrado));
            EstanDentro.Audio.SonidoCorto.Juego("casillero/pestillo", transform.position, 0.5f, 0.8f, 0.8f);
        }

        IEnumerator Mover(Vector3 fin)
        {
            moviendo = true;
            Vector3 ini = transform.position;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(ini, fin, 1f - Mathf.Pow(1f - t / 0.4f, 3f));
                yield return null;
            }
            transform.position = fin; moviendo = false;
        }
    }
}
