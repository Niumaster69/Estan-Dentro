using System.Collections;
using UnityEngine;
using EstanDentro.UI;

namespace EstanDentro.Mente
{
    /// <summary>
    /// Qué dispara cada estado (Henry 21-oct, 01_Documentacion/DECISIONES_E_IDEAS.md):
    ///   Normal → Pesadilla: un recuerdo del accidente (anclada: no se sale hasta resolverla) o el estrés ≥ 80 % unos 10 s.
    ///   Pesadilla → Normal: respirar (Q) hasta bajar el estrés (si no está anclada) o resolver lo que esconde.
    ///   Pesadilla ↔ Alterado: lo maneja el Observador (te ve → te caza; te escondes → vuelve la Pesadilla).
    /// </summary>
    public class ControlMente : MonoBehaviour
    {
        public static ControlMente Instancia { get; private set; }
        public float estresQuiebre = 0.8f, segundosQuiebre = 10f, estresCalma = 0.25f;

        float tAlto, tCalma;
        bool anclada, calmando;
        readonly System.Collections.Generic.HashSet<string> usados = new System.Collections.Generic.HashSet<string>();
        string resolverCon;
        Observador observador;

        void Awake() { Instancia = this; }

        void Start()
        {
            observador = FindAnyObjectByType<Observador>(FindObjectsInactive.Include);
            var inv = Inventory.Inventory.Instance;
            if (inv != null) inv.OnObjetoNuevo += (id, _) => { if (!string.IsNullOrEmpty(resolverCon) && id == resolverCon) StartCoroutine(Resuelta("Ya… ya pasó. Respira, Mateo.", 1.5f)); };
        }

        void OnDestroy() { if (Instancia == this) Instancia = null; }

        /// <summary>Un recuerdo del accidente rompe el colegio. Si "resolver" trae un id, la Pesadilla no se va hasta tomar ese objeto.</summary>
        public static void Recuerdo(string pensamiento, string resolver, string id = null)
        {
            var c = Instancia; Debug.Log($"[Mente] recuerdo {id} (control={(c ? "sí" : "NO")}, resolver={resolver})"); if (!c) return;
            if (!string.IsNullOrEmpty(id) && !c.usados.Add(id)) return;                      // cada recuerdo una sola vez
            c.anclada = !string.IsNullOrEmpty(resolver); c.resolverCon = resolver;
            if (!string.IsNullOrEmpty(pensamiento)) ObjectiveHUD.Notify(pensamiento, 3.5f);
            EstadoMente.Ir(EstadoMente.Estado.Pesadilla);
        }

        /// <summary>Suelta la Pesadilla anclada (tecla de prueba F8).</summary>
        public static void Soltar() { if (Instancia) { Instancia.anclada = false; Instancia.resolverCon = null; } }

        /// <summary>Se llegó a lo que resolvía la Pesadilla sin tomar un objeto (p. ej. entrar al aula de música).</summary>
        public static bool Resolver(string id, string pensamiento = null)
        {
            var c = Instancia; if (!c || c.resolverCon != id) return false;
            c.StartCoroutine(c.Resuelta(pensamiento, 0f));
            return true;
        }

        IEnumerator Resuelta(string pensamiento, float espera)
        {
            resolverCon = null; anclada = false; calmando = true;
            if (observador) observador.Activo = false;                                     // ya no hay a quién temerle
            while (EstadoMente.EnTransicion) yield return null;
            if (espera > 0f) yield return new WaitForSeconds(espera);
            if (!string.IsNullOrEmpty(pensamiento)) ObjectiveHUD.Notify(pensamiento, 3f);
            Stress.StressSystem.Instance?.AddSilencioso(-30f);
            EstadoMente.Ir(EstadoMente.Estado.Normal);
            while (EstadoMente.Actual != EstadoMente.Estado.Normal || EstadoMente.EnTransicion) yield return null;
            calmando = false;
        }

        void Update()
        {
            var st = Stress.StressSystem.Instance;
            var e = EstadoMente.Actual;
            if (observador) observador.Activo = e != EstadoMente.Estado.Normal && !calmando;
            if (!st || EstadoMente.EnTransicion || Time.timeScale == 0f || calmando) return;
            if (e == EstadoMente.Estado.Normal)
            {
                tAlto = st.Normalized >= estresQuiebre ? tAlto + Time.deltaTime : 0f;
                if (tAlto >= segundosQuiebre)
                {
                    tAlto = 0f; anclada = false;
                    ObjectiveHUD.Notify("No… otra vez no. Todo se está rompiendo.", 3f);
                    EstadoMente.Ir(EstadoMente.Estado.Pesadilla);
                }
            }
            else if (e == EstadoMente.Estado.Pesadilla)
            {
                if (st.Normalized < 0.7f) st.AddSilencioso(0.6f * Time.deltaTime);          // la Pesadilla pesa
                tCalma = !anclada && st.Normalized < estresCalma ? tCalma + Time.deltaTime : 0f;
                if (tCalma > 1.5f)
                {
                    tCalma = 0f;
                    ObjectiveHUD.Notify("Respira… Ya pasó.", 2.5f);
                    EstadoMente.Ir(EstadoMente.Estado.Normal);
                }
            }
        }
    }
}
