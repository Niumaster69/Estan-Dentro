using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Cualquier silla del salón se puede AGARRAR (E), llevar caminando y SOLTAR (E). Si se suelta junto al casillero de la
    /// rejilla queda bien puesta, y desde ella Mateo se sube (E) y de ahí al casillero (ver Escalada).
    /// (Henry: "la silla se teletransporta por todo el salón… vamos a usar cualquier silla para subir")
    /// </summary>
    public class SillaArrastrable : Interactable
    {
        public Vector3 destino;          // junto al casillero (se rellena en ArreglosColegio.Escalada4B)
        public float rumbo;              // hacia dónde mira la silla puesta junto al casillero
        public float alturaAsiento = 0.46f;
        public float alcanceDestino = 1.1f;
        public string nombre = "la silla";       // "el pupitre" en el 4°B
        static SillaArrastrable cargada;
        /// <summary>La que se lleva ahora (sus choques van apagados: la mira no la ve, así que la E la suelta igual).</summary>
        public static SillaArrastrable Cargada => cargada;
        bool puesta;
        Player.PlayerController jugador;
        Collider[] cols;
        float pisoY;

        public override string Accion => cargada == this ? "Soltar " + nombre : puesta ? "Subirse " + (nombre.StartsWith("el ") ? "al " + nombre.Substring(3) : "a " + nombre) : "Arrastrar " + nombre;

        void Awake() { cols = GetComponentsInChildren<Collider>(); pisoY = transform.position.y; }

        public override void Interact()
        {
            if (cargada == this) { Soltar(); return; }
            if (cargada != null) { ObjectiveHUD.Show("Ya llevo una.", 1.5f); return; }
            if (puesta)
            {
                var j = Escalada.Jugador(); if (!j) return;
                var cc = Centro(); var top = Arriba();
                StartCoroutine(Escalada.Subir(j, new Vector3(cc.x, top, cc.z), false, ""));
                return;
            }
            jugador = Escalada.Jugador(); if (!jugador) return;
            cargada = this;
            foreach (var c in cols) c.enabled = false;
            jugador.FactorVelocidad = 0.55f;
            EstanDentro.Audio.SonidoCorto.Juego("casillero/lata_rueda", transform.position, 0.35f, 0.8f, 0.6f);
        }

        void Update()
        {
            if (cargada != this || !jugador) return;
            // va delante de Mateo, en el piso, mirando hacia donde camina
            // el mueble va delante de Mateo (se mueve por su CENTRO real: el origen de los pupitres no está en el mueble)
            Vector3 f = Vector3.ProjectOnPlane(jugador.transform.forward, Vector3.up).normalized;
            Vector3 quiero = jugador.transform.position + f * 0.9f;
            Vector3 c = Centro();
            Vector3 d = new Vector3(quiero.x - c.x, 0f, quiero.z - c.z);
            transform.position += d * Mathf.Clamp01(Time.deltaTime * 10f);
        }

        /// <summary>Centro real del mueble (por su malla), no su origen.</summary>
        Vector3 Centro() { var r = GetComponent<Renderer>(); return r ? r.bounds.center : transform.position; }
        /// <summary>Altura donde se para Mateo encima (la caja de choque).</summary>
        float Arriba() { float y = -1e9f; foreach (var c in cols) if (c) y = Mathf.Max(y, c.bounds.max.y); return y > -1e8f ? y : transform.position.y + alturaAsiento; }

        Vector3 baseEuler; bool baseLista;
        Vector3 Base() { if (!baseLista) { baseEuler = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z); baseLista = true; } return baseEuler; }

        void Soltar()
        {
            cargada = null;
            if (jugador) jugador.FactorVelocidad = 1f;
            Vector3 aqui = Centro();
            if (destino != Vector3.zero && Vector2.Distance(new Vector2(aqui.x, aqui.z), new Vector2(destino.x, destino.z)) < alcanceDestino)
            {
                // junto al casillero: queda bien puesta para subirse (su centro en el destino)
                transform.position += new Vector3(destino.x - aqui.x, 0f, destino.z - aqui.z);
                puesta = true;
                ObjectiveHUD.Show("Desde aquí alcanzo a subirme.", 2.5f);
            }
            else puesta = false;
            foreach (var c in cols) c.enabled = true;
            EstanDentro.Audio.SonidoCorto.Juego("casillero/lata_rueda", transform.position, 0.3f, 0.8f, 0.4f);
        }
    }
}
