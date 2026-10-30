using System.Collections;
using UnityEngine;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Casillero que se abre de verdad: E gira su(s) puerta(s) y lo que hay adentro SE VE (BotinVisible).
    /// Otra E coge la cosa; con el casillero vacío, E lo vuelve a cerrar (y otra vez lo abre).
    /// Si no cabe (manos y mochila llenas), lo de adentro se queda ahí para después.
    /// </summary>
    public class Casillero : Interactable
    {
        public string botinId = "";
        public string botinNombre = "";
        public AudioClip sonidoAbrir;
        public AudioClip sonidoBisagra;   // chirrido de la puerta de lata (se oye el primer segundo y medio)
        public AudioClip sonidoTomar;
        public float angulo = 105f;
        [Tooltip("portazo / cae / celular / susurro (vacío = sin susto). Ver Sustos.cs")]
        public string susto = "";
        bool abierto, tomado, girando;
        GameObject visible;
        public bool Abierto => abierto;
        bool Queda => !tomado && !string.IsNullOrEmpty(botinId);

        CombinationLock candado;

        void Awake()
        {
            candado = GetComponent<CombinationLock>();
            if (candado != null) candado.onSolved.AddListener(() => { if (!abierto) Interact(); });
        }

        public override string Accion =>
            candado != null && !candado.IsSolved ? "Mirar el candado"
            : !abierto ? "Abrir el casillero"
            : Queda ? "Tomar: " + (Recuerdos.Es(botinId) ? "papel" : botinNombre.ToLower())
            : "Cerrar el casillero";


        public override void Interact()
        {
            if (candado != null && !candado.IsSolved) { candado.Interact(); return; }   // primero el candado
            if (girando) return;
            if (!abierto)
            {
                abierto = true;
                StartCoroutine(Pausa(0.6f));
                AbrirPuertas(gameObject, angulo);
                EstanDentro.Audio.SonidoCorto.Play(sonidoAbrir, transform.position + Vector3.up, 0.8f, 1.6f);
                var b = EstanDentro.Audio.SonidoCorto.Play(sonidoBisagra, transform.position + Vector3.up * 1.2f, 0.6f, 1.5f);
                if (b) b.pitch = Random.Range(0.92f, 1.12f);                     // cada casillero chilla un poco distinto
                if (!string.IsNullOrEmpty(susto))
                {
                    var tipo = susto; susto = "";                              // el susto pasa una sola vez
                    StartCoroutine(SustoYDespues(tipo));
                    return;
                }
                MostrarBotin();
                return;
            }
            if (Queda) { Tomar(); return; }
            // vacío: se vuelve a cerrar
            abierto = false; StartCoroutine(Pausa(0.6f));
            CerrarPuertas(gameObject, 0.5f);
            EstanDentro.Audio.SonidoCorto.Play(sonidoAbrir, transform.position + Vector3.up, 0.6f, 1.2f);
        }

        IEnumerator Pausa(float s) { girando = true; yield return new WaitForSeconds(s); girando = false; }

        void MostrarBotin()
        {
            if (!Queda) { if (string.IsNullOrEmpty(botinId) && !tomado) { tomado = true; ObjectiveHUD.Show("Vacío.", 1.5f); } return; }
            if (!visible) visible = BotinVisible.Poner(botinId, gameObject);
        }

        IEnumerator SustoYDespues(string tipo)
        {
            yield return Sustos.Ejecutar(tipo, this);
            if (tipo == "portazo") abierto = false;                            // se cerró solo: se puede volver a abrir
            else MostrarBotin();
        }

        void Tomar()
        {
            if (!Queda) return;
            if (Recuerdos.Es(botinId))
            {
                // un recuerdo: va a PISTAS del cuaderno y se lee enseguida
                var (titulo, texto) = Recuerdos.De(botinId);
                Inventory.Inventory.Instance?.RegisterNote(titulo, texto);
                EstanDentro.Audio.SonidoCorto.Juego("objetos/papel_tomar", transform.position + Vector3.up, 0.7f, 0.5f);
                NoteOverlay.Show(titulo, texto);
                tomado = true; if (visible) Destroy(visible);
                return;
            }
            var inv = Inventory.Inventory.Instance;
            bool nuevo = inv != null && !inv.YaVisto(botinId);
            if (inv != null && !inv.RegisterItem(botinId, botinNombre))
            {
                ObjectiveHUD.Show(inv.MotivoLleno(), 3f);
                return;
            }
            tomado = true; if (visible) Destroy(visible);
            if (botinId == "moneda") EstanDentro.Audio.SonidoCorto.Juego("objetos/monedas", transform.position + Vector3.up, 0.7f, 0.5f);
            else if (sonidoTomar) AudioSource.PlayClipAtPoint(sonidoTomar, transform.position + Vector3.up, 0.7f);
            if (!nuevo) ObjectiveHUD.Show($"Tomaste: {botinNombre}" + (inv != null ? Cuantos(inv, botinId) : ""), 2f);
        }

        static string Cuantos(Inventory.Inventory inv, string id)
        {
            var it = inv.Buscar(id);
            return it != null && it.cantidad > 1 ? $"  (llevas {it.cantidad})" : "";
        }

        static readonly System.Collections.Generic.Dictionary<Transform, Quaternion> cerradas = new System.Collections.Generic.Dictionary<Transform, Quaternion>();

        /// <summary>Cierra de golpe las puertas (susto "portazo").</summary>
        public static void CerrarPuertas(GameObject casillero, float duracion = 0.12f)
        {
            var host = casillero.GetComponent<MonoBehaviour>();
            foreach (var d in casillero.GetComponentsInChildren<DatosBlender>())
                if (d.Get("prop_tipo") == "puerta_casillero" && cerradas.TryGetValue(d.transform, out var q) && host)
                    host.StartCoroutine(GirarA(d.transform, q, duracion));
        }

        static IEnumerator GirarA(Transform t, Quaternion fin, float d)
        {
            Quaternion ini = t.rotation;
            for (float k = 0f; k < d; k += Time.deltaTime) { t.rotation = Quaternion.Slerp(ini, fin, k / d); yield return null; }
            t.rotation = fin;
        }

        /// <summary>Gira las puertas hijas (prop 'puerta_casillero') hacia afuera. Sirve también para el casillero con candado.</summary>
        public static void AbrirPuertas(GameObject casillero, float angulo = 105f)
        {
            var host = casillero.GetComponent<MonoBehaviour>();
            var cuerpo = casillero.GetComponent<Renderer>();
            Vector3 centro = cuerpo ? cuerpo.bounds.center : casillero.transform.position;
            foreach (var d in casillero.GetComponentsInChildren<DatosBlender>())
            {
                if (d.gameObject == casillero || d.Get("prop_tipo") != "puerta_casillero") continue;
                var t = d.transform;
                if (!cerradas.ContainsKey(t)) cerradas[t] = t.rotation;
                var r = t.GetComponent<Renderer>();
                Vector3 c = r ? r.bounds.center : t.position;
                // eje VERTICAL del mundo: el 'arriba' de los modelos de Blender puede venir acostado (la puerta se iba al piso)
                Vector3 eje = Vector3.up;
                // hacia el lado en que la puerta se aleja del cuerpo del casillero
                Vector3 a = Quaternion.AngleAxis(angulo, eje) * (c - t.position) + t.position;
                Vector3 b = Quaternion.AngleAxis(-angulo, eje) * (c - t.position) + t.position;
                float signo = Vector3.Distance(a, centro) >= Vector3.Distance(b, centro) ? 1f : -1f;
                if (host) host.StartCoroutine(Girar(t, eje, signo * angulo));
            }
        }

        static IEnumerator Girar(Transform t, Vector3 eje, float grados)
        {
            Quaternion ini = t.rotation, fin = Quaternion.AngleAxis(grados, eje) * t.rotation;
            float d = 0.55f, k = 0f;
            while (k < d)
            {
                k += Time.deltaTime;
                float p = Mathf.Clamp01(k / d);
                t.rotation = Quaternion.Slerp(ini, fin, 1f - Mathf.Pow(1f - p, 3f));
                yield return null;
            }
            t.rotation = fin;
        }
    }
}
