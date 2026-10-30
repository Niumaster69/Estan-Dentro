using UnityEngine;
using EstanDentro.UI;

namespace EstanDentro.Mente
{
    /// <summary>
    /// El pasillo que se repite (Fenómeno Siniestro, DECISIONES "salida falsa y bucles"): en la Pesadilla, al cruzar este
    /// volumen Mateo aparece otra vez más atrás en el mismo pasillo, mirando hacia el mismo lado, sin corte. Solo se rompe
    /// respirando con Pa: con <see cref="ciclosParaRomper"/> ciclos bien hechos desde que empezó la Pesadilla, el pasillo
    /// deja de dar vueltas. Enseña lo mismo que el juego entero: dar vueltas a lo mismo no saca de la angustia; detenerse
    /// y respirar, sí.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BuclePasillo : MonoBehaviour
    {
        [Tooltip("Cuánto retrocede Mateo al cruzar (en el eje del pasillo).")]
        public Vector3 retroceso = new Vector3(-12f, 0f, 0f);
        public int ciclosParaRomper = 2;
        public int maxVueltas = 6;               // por si el jugador no entiende: a la sexta se rompe sola

        int vueltas, baseCiclos;
        bool roto;
        float ultimo = -9f;                       // Mateo tiene dos colliders (controlador y cuerpo): un solo cruce por vez
        EstadoMente.Estado antes = EstadoMente.Estado.Normal;

        void Awake() { GetComponent<BoxCollider>().isTrigger = true; }

        void Update()
        {
            var e = EstadoMente.Actual;
            if (e != EstadoMente.Estado.Normal && antes == EstadoMente.Estado.Normal) { baseCiclos = Colegio.Concentrarse.CiclosBuenos; vueltas = 0; }
            if (e == EstadoMente.Estado.Normal) roto = false;                         // en otra Pesadilla puede volver a pasar
            antes = e;
        }

        void OnTriggerEnter(Collider c)
        {
            var j = c.GetComponentInParent<Player.PlayerController>();
            if (!j || roto || EstadoMente.Actual != EstadoMente.Estado.Pesadilla || EstadoMente.EnTransicion) return;
            if (Time.time - ultimo < 0.6f) return;
            ultimo = Time.time;
            if (Colegio.Concentrarse.CiclosBuenos - baseCiclos >= ciclosParaRomper || vueltas >= maxVueltas)
            {
                roto = true;
                ObjectiveHUD.Notify("…El pasillo sigue. Ya no da vueltas.", 2.5f);
                return;
            }
            vueltas++;
            var cc = j.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            j.transform.position += retroceso;
            if (cc) cc.enabled = true;
            Stress.StressSystem.Instance?.AddSilencioso(6f);
            if (vueltas == 1) ObjectiveHUD.Notify("¿Otra vez la puerta del 4°B…?", 2.5f);
            else if (vueltas == 2) ObjectiveHUD.Notify("Es el mismo pasillo. Estoy dando vueltas.", 2.5f);
            else if (vueltas == 3) { ObjectiveHUD.Notify("Pa decía: cuando todo da vueltas, para y respira.", 3.5f); GuiaTeclas.PrimeraVez("Q", "Respirar con Pa"); }
        }
    }
}
