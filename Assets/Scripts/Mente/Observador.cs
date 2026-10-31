using System.Collections;
using UnityEngine;
using EstanDentro.UI;

namespace EstanDentro.Mente
{
    /// <summary>
    /// El Observador: solo existe en la Pesadilla. Se para lejos, en el borde de la vista, y te mira.
    ///   Acechando → si lo miras fijo (~2 s), te le acercas o corres cerca, te ve: Alterado y te caza.
    ///               Si no lo miras un rato, cambia de lugar (nunca delante de tus ojos).
    ///   Cazando   → camina hacia ti, lento y rígido (rama de mejoras: antes iba a trompicones); atraviesa las paredes,
    ///               pero solo sabe dónde te vio por última vez. Te pierde si te escondes, si le cortas la vista ~5 s o si
    ///               te alejas mucho. Caminando se le gana; corriendo, de sobra.
    /// Las animaciones (Caminar, Quieto) salen de Herramientas/blender/observador_camina.py; el Animator lo pone
    /// Editor/Mejoras/MejorasEscena.ObservadorCamina. Sin Animator sigue funcionando (se desliza).
    ///   Atrapado  → su cara encima de la tuya y Mateo colapsa.
    /// </summary>
    public class Observador : MonoBehaviour
    {
        public bool Activo { get; set; }
        public static bool Pausado;                     // la prueba automática lo apaga
        public float velocidad = 3.5f, miradaParaVerte = 2.0f, distanciaSentirte = 3.2f;
        [Tooltip("Metros por segundo caminando (Mateo camina a 2,8 y corre a 4,5).")]
        public float velocidadCaminar = 2.1f;
        [Tooltip("Metros por segundo que avanza la animación Caminar a velocidad 1 (para que los pies no patinen).")]
        public float pasoAnimacion = 1.15f;
        [Tooltip("Segundos que ronda el escondite (con Mateo callado) antes de irse.")]
        public float rondaEscondite = 4.5f;

        public static Observador Instancia { get; private set; }
        /// <summary>Está a la vista y buscando (para el escondite: hay que aguantar la respiración).</summary>
        public bool Presente => modo == Modo.Acechando || modo == Modo.Cazando;
        public bool Cazando => modo == Modo.Cazando;

        /// <summary>Mateo hizo ruido dentro del escondite (jadeó o respiró con él al lado): sabe dónde está.</summary>
        public void Delatar(Vector3 donde)
        {
            if (modo == Modo.Atrapando || modo == Modo.Fuera) return;
            ultimaVez = donde; sinVerte = 0f; quieto = 0f;
            if (modo != Modo.Cazando) { modo = Modo.Cazando; proximoSalto = Time.time + 0.3f; EstadoMente.Ir(EstadoMente.Estado.Alterado); }
        }

        enum Modo { Fuera, Acechando, Cazando, Atrapando }
        Modo modo = Modo.Fuera;
        float quieto;                                    // tiempo con Mateo escondido y callado
        Transform cuerpo;
        Renderer[] rs;
        Camera cam;
        Player.PlayerController jugador;
        AudioSource susurro;
        float mirada, sinVerlo, sinVerte, proximoSalto, cuandoPuede;
        Vector3 ultimaVez;

        void Awake() { Instancia = this; }
        void OnDestroy() { if (Instancia == this) Instancia = null; }

        Animator anim;
        string animActual;

        /// <summary>Cambia de animación solo si es otra (con un fundido corto); la velocidad ajusta el paso al avance.</summary>
        void Anim(string estado, float velocidadAnim = 1f)
        {
            if (!anim) return;
            anim.speed = velocidadAnim;
            if (animActual == estado) return;
            animActual = estado;
            anim.CrossFadeInFixedTime(estado, 0.25f);
        }

        void Start()
        {
            cuerpo =transform.childCount > 0 ? transform.GetChild(0) : transform;
            anim = GetComponentInChildren<Animator>(true);
            rs = GetComponentsInChildren<Renderer>(true);
            susurro = gameObject.AddComponent<AudioSource>();
            susurro.clip = Resources.Load<AudioClip>("Sonidos/susurro"); susurro.loop = true; susurro.spatialBlend = 1f;
            susurro.minDistance = 1.5f; susurro.maxDistance = 14f; susurro.rolloffMode = AudioRolloffMode.Linear; susurro.volume = 0.55f;
            Mostrar(false);
        }

        void Mostrar(bool si) { foreach (var r in rs) if (r) r.enabled = si; if (susurro) { if (si && !susurro.isPlaying) susurro.Play(); if (!si) susurro.Stop(); } }

        void Update()
        {
            if (!cam) cam = Camera.main;
            if (!jugador) jugador = FindAnyObjectByType<Player.PlayerController>();
            if (!cam || !jugador || Time.timeScale == 0f) return;
            if (Pausado) { if (modo != Modo.Fuera) { modo = Modo.Fuera; Mostrar(false); } return; }
            if (!Activo && modo != Modo.Fuera && modo != Modo.Atrapando) { modo = Modo.Fuera; Mostrar(false); return; }
            switch (modo)
            {
                case Modo.Fuera:
                    if (Activo && Time.time > cuandoPuede && !EstadoMente.EnTransicion && Reubicar()) { modo = Modo.Acechando; Mostrar(true); mirada = 0f; sinVerlo = 0f; }
                    break;
                case Modo.Acechando: Acechar(); break;
                case Modo.Cazando: Cazar(); break;
            }
        }

        Vector3 Pecho => transform.position + Vector3.up * 1.6f;
        Vector3 Pies => jugador.transform.position;

        bool LoVe(out bool enPantalla)
        {
            Vector3 d = Pecho - cam.transform.position;
            var vp = cam.WorldToViewportPoint(Pecho);
            enPantalla = vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.1f && vp.y < 1.1f && !Tapado(cam.transform.position, Pecho);
            return enPantalla && Vector3.Angle(cam.transform.forward, d) < 20f;
        }

        bool Tapado(Vector3 desde, Vector3 hasta)
        {
            Vector3 d = hasta - desde; float largo = d.magnitude;
            foreach (var h in Physics.RaycastAll(desde, d / largo, largo - 0.3f, ~0, QueryTriggerInteraction.Ignore))
                if (!h.collider.transform.IsChildOf(jugador.transform) && !h.collider.transform.IsChildOf(transform)) return true;
            return false;
        }

        void MirarAlJugador(float rapidez)
        {
            Vector3 d = Pies - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), rapidez * Time.deltaTime);
            // la cabeza "piensa": tics pequeños
            float tic = Mathf.PerlinNoise(Time.time * 3f, 7f) > 0.82f ? Random.Range(-6f, 6f) : 0f;
            cuerpo.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.7f) * 1.5f, tic, Mathf.Sin(Time.time * 0.5f) * 2f);
        }

        void Acechar()
        {
            Anim("Quieto");
            MirarAlJugador(2f);
            bool visto = LoVe(out bool enPantalla);
            mirada = visto ? mirada + Time.deltaTime : Mathf.Max(0f, mirada - Time.deltaTime * 0.4f);
            if (visto) Stress.StressSystem.Instance?.AddSilencioso(5f * Time.deltaTime);
            sinVerlo = enPantalla ? 0f : sinVerlo + Time.deltaTime;
            float dist = Vector3.Distance(transform.position, Pies);
            bool cerca = dist < distanciaSentirte || (jugador.Corriendo && dist < 9f && !Tapado(cam.transform.position, Pecho));
            if (mirada >= miradaParaVerte || cerca)
            {
                modo = Modo.Cazando; ultimaVez = Pies; sinVerte = 0f; proximoSalto = Time.time + 0.4f;
                EstadoMente.Ir(EstadoMente.Estado.Alterado);
                return;
            }
            if (sinVerlo > 7f && Reubicar()) sinVerlo = 0f;
        }

        void Cazar()
        {
            if (EstadoMente.EnTransicion) { MirarAlJugador(6f); return; }
            bool teVe = !Tapado(Pecho, cam.transform.position) && Escondido() == false;
            if (teVe) { ultimaVez = Pies; sinVerte = 0f; } else sinVerte += Time.deltaTime;
            float dist = Vector3.Distance(transform.position, Pies);
            // escondido no basta: se acerca a donde te vio y ronda; si Mateo aguanta callado, al rato se va
            quieto = Escondido() ? quieto + Time.deltaTime : 0f;
            bool perdido = Escondido() && dist > 1.4f && quieto > rondaEscondite || !Escondido() && sinVerte > 5f || dist > 22f;
            if (perdido) { StartCoroutine(Irse()); return; }
            if (dist < 1.05f && !Escondido()) { StartCoroutine(Atrapar()); return; }
            // camina: sin saltos, con un ritmo que no es del todo humano (acelera y frena un poco, como dudando)
            Vector3 meta = ultimaVez; Vector3 d = meta - transform.position; d.y = 0f;
            MirarAlJugador(5f);
            float v = velocidadCaminar * Mathf.Lerp(0.8f, 1.2f, Mathf.PerlinNoise(Time.time * 0.6f, 3.1f));
            if (d.magnitude > 0.05f)
            {
                transform.position += d.normalized * Mathf.Min(v * Time.deltaTime, d.magnitude);
                AlPiso();
                Anim("Caminar", v / Mathf.Max(0.1f, pasoAnimacion));
            }
            else Anim("Quieto");
            if (!teVe && d.magnitude < 0.3f) sinVerte += Time.deltaTime * 2f;    // llegó a donde te vio y no estás
        }

        bool Escondido() => Colegio.Escondite.Actual != null && !Colegio.Escondite.Delatado;

        IEnumerator Irse()
        {
            modo = Modo.Fuera; cuandoPuede = Time.time + 9f;
            for (int i = 0; i < 6; i++) { Mostrar(i % 2 == 1); yield return new WaitForSeconds(0.06f); }
            Mostrar(false);
            if (Escondido()) ObjectiveHUD.Notify("…Se fue. No respires todavía.", 2.5f);
            if (EstadoMente.Actual == EstadoMente.Estado.Alterado) EstadoMente.Ir(EstadoMente.Estado.Pesadilla);
        }

        IEnumerator Atrapar()
        {
            modo = Modo.Atrapando;
            Anim("Quieto");
            jugador.enabled = false;
            Vector3 d = transform.position - Pies; d.y = 0f;
            jugador.transform.rotation = Quaternion.LookRotation(d);
            transform.position = Pies + d.normalized * 0.5f; transform.rotation = Quaternion.LookRotation(-d);
            float cara = cam.transform.position.y - 2.05f;                       // los ojos a la altura de los de Mateo
            transform.position += Vector3.up * cara;
            var golpe = Resources.Load<AudioClip>("Sonidos/golpe");
            if (golpe) AudioSource.PlayClipAtPoint(golpe, cam.transform.position, 1f);
            yield return new WaitForSeconds(0.9f);
            Stress.StressSystem.Instance?.Add(1000f);                            // colapso → game over
            yield return new WaitForSeconds(0.5f);
            jugador.enabled = true; modo = Modo.Fuera; Mostrar(false);
        }

        /// <summary>Lejos (7–16 m; en un cuarto chico, en la esquina más lejana, a 3.5 m o más), en el mismo piso, a la vista desde donde está Mateo pero fuera de lo que mira ahora.</summary>
        bool Reubicar()
        {
            Vector3 ojo = cam.transform.position;
            float yaw = cam.transform.eulerAngles.y;
            for (int i = 0; i < 70; i++)
            {
                float minimo = i < 40 ? 7.5f : 3.5f;
                float a = yaw + (Random.value < 0.5f ? -1f : 1f) * Random.Range(45f, 160f);
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                float largo = 16f;
                if (Physics.Raycast(ojo, dir, out var h, 16f, ~0, QueryTriggerInteraction.Ignore)) largo = h.distance;
                if (largo < minimo) continue;
                Vector3 p = ojo + dir * (i < 40 ? Random.Range(7f, largo - 0.7f) : largo - 0.55f);
                if (!Physics.Raycast(p, Vector3.down, out var piso, 2.6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(piso.point.y - Pies.y) > 0.6f) continue;
                transform.position = piso.point;
                Vector3 m = Pies - transform.position; m.y = 0f; transform.rotation = Quaternion.LookRotation(m);
                return true;
            }
            return false;
        }

        void AlPiso()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 1.2f, Vector3.down, out var h, 3f, ~0, QueryTriggerInteraction.Ignore) && !h.collider.transform.IsChildOf(jugador.transform))
                transform.position = new Vector3(transform.position.x, h.point.y, transform.position.z);
        }
    }
}
