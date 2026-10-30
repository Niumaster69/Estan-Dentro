using System.Collections;
using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Subir hasta la rejilla del techo del 4°B (Henry: "que se arrastre la silla cercana, poderme subir a la silla, luego
    /// encima del armario y luego meterme a la ventila"). Una silla se arrastra hasta el casillero (E), se sube a ella (E),
    /// desde ahí al casillero (E, agachado porque no cabe de pie) y desde arriba se alcanza la rejilla.
    /// </summary>
    /// <summary>Un lugar al que se trepa (encima del casillero). Solo si el jugador ya está en algo alto (la silla).</summary>
    public class Escalada : Interactable
    {
        public Vector3 arriba;            // dónde quedan los pies
        public float pisoMinimo = 0.3f;   // los pies deben estar al menos esto sobre el piso del salón
        public float pisoSalon;
        public bool agachado = true;

        bool Alcanza => Jugador() && Jugador().transform.position.y > pisoSalon + pisoMinimo;
        public override string Accion => Alcanza ? "Subirse al casillero" : "Está muy alto";

        public override void Interact()
        {
            var j = Jugador(); if (!j) return;
            if (!Alcanza) { ObjectiveHUD.Show("Está muy alto. Necesito algo para subirme.", 2.2f); return; }
            StartCoroutine(Subir(j, arriba, agachado, "Aquí arriba no quepo de pie."));
        }

        public static Player.PlayerController Jugador() => FindAnyObjectByType<Player.PlayerController>();

        public static IEnumerator Subir(Player.PlayerController j, Vector3 pies, bool agacharse, string aviso)
        {
            j.InputEnabled = false;
            if (agacharse) j.ForzarAgachado = true;
            Vector3 p0 = j.transform.position;
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.7f);
                Vector3 p = Vector3.Lerp(p0, pies, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.25f;   // un saltico al subir
                j.Teletransportar(p);
                yield return null;
            }
            j.Teletransportar(pies + Vector3.up * 0.02f);
            j.InputEnabled = true;
            if (!string.IsNullOrEmpty(aviso)) ObjectiveHUD.Show(aviso, 2.5f);
            if (agacharse) j.StartCoroutine(SoltarAgachadoAlBajar(j, pies.y));
        }

        static IEnumerator SoltarAgachadoAlBajar(Player.PlayerController j, float alto)
        {
            while (j && j.transform.position.y > alto - 0.5f) yield return null;      // mientras siga arriba
            if (j) j.ForzarAgachado = false;
        }
    }
}
