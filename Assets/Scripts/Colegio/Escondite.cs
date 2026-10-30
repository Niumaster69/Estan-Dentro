using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Casillero doble, cubículo, cuarto bajo la escalera: el jugador se esconde.
    /// AGUANTAR LA RESPIRACIÓN: si el Observador está a la vista y cerca, Mateo tiene que aguantar el aire (mantener Q).
    /// Respirar con él al lado lo delata; aguantar más de lo que dan los pulmones también (Mateo jadea). El Observador
    /// ronda el escondite y, si Mateo se queda callado, al rato se va. Es la otra cara de "respirar con Pa": controlar el
    /// cuerpo bajo presión, y soltar el aire despacio cuando el peligro pasa.
    /// </summary>
    public class Escondite : Interactable
    {
        public static Escondite Actual { get; private set; }
        /// <summary>Mateo hizo ruido: el Observador ya no lo cuenta como escondido.</summary>
        public static bool Delatado { get; private set; }
        public Transform puntoDentro;
        public float pulmones = 6f;            // segundos que aguanta el aire
        public float oyeA = 5f;                // metros a los que el Observador oye la respiración
        public float graciaRespirar = 1.6f;    // segundos respirando con él cerca antes de delatarse
        private Vector3 posAntes;
        private Transform jugador;

        float aire = 1f, respirandoCerca;
        bool avisado;
        static Canvas lienzo; static Image barra, fondoBarra; static Text texto;

        public override string Accion => "Esconderse";

        public override void Interact()
        {
            if (jugador == null)
            {
                var pc = FindAnyObjectByType<Player.PlayerController>();
                if (pc == null) return;
                jugador = pc.transform;
            }
            var cc = jugador.GetComponent<CharacterController>();
            if (Actual == this)
            {
                if (cc) cc.enabled = false;
                jugador.position = posAntes;
                if (cc) cc.enabled = true;
                Actual = null; Delatado = false;
                Mostrar(false);
                ObjectiveHUD.Show("Sales del escondite.", 1.2f);
                return;
            }
            posAntes = jugador.position;
            if (cc) cc.enabled = false;
            jugador.position = puntoDentro ? puntoDentro.position : transform.position + Vector3.up * 0.05f;
            if (cc) cc.enabled = true;
            Actual = this; Delatado = false; aire = 1f; respirandoCerca = 0f; avisado = false;
            ObjectiveHUD.Show("Aquí no me ven… no respires.", 1.8f);
        }

        void Update()
        {
            if (Actual != this || Time.timeScale == 0f) return;
            var obs = Mente.Observador.Instancia;
            var cam = Camera.main;
            float dist = obs && cam ? Vector3.Distance(obs.transform.position, cam.transform.position) : 99f;
            bool peligro = obs && obs.Presente && dist < oyeA * 1.6f;
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool aguanta = (kb != null && kb.qKey.isPressed) || (gp != null && gp.leftShoulder.isPressed);
            float dt = Time.deltaTime;

            if (aguanta) aire -= dt / pulmones;
            else aire = Mathf.Min(1f, aire + dt / (pulmones * 0.6f));               // soltar y recuperar el aire

            if (peligro && !avisado) { avisado = true; GuiaTeclas.PrimeraVez("Q", "Aguanta la respiración"); }
            if (peligro && !aguanta && dist < oyeA) respirandoCerca += dt; else respirandoCerca = Mathf.Max(0f, respirandoCerca - dt);

            if (!Delatado && aire <= 0f) Delatar(Resources.Load<AudioClip>("Sonidos/jadeo"), "¡No… no aguanto!");
            else if (!Delatado && respirandoCerca > graciaRespirar) Delatar(EstanDentro.Audio.SonidoCorto.Clip("cuerpo/respiracion_miedo"), "…Me oyó.");

            var st = Stress.StressSystem.Instance;
            if (st != null && peligro) st.AddSilencioso((aguanta ? 3f : 1.5f) * dt);   // aguantar el aire con él al lado asusta

            Mostrar(peligro || aire < 0.999f);
            if (barra)
            {
                barra.fillAmount = Mathf.Clamp01(aire);
                barra.color = aire > 0.35f ? new Color(0.66f, 0.84f, 0.86f, 0.9f) : new Color(0.85f, 0.25f, 0.2f, 0.95f);
                texto.text = peligro ? (aguanta ? "Aguantando el aire…" : "Mantén Q: aguanta la respiración") : "Suelta el aire despacio";
            }
        }

        void Delatar(AudioClip sonido, string pensamiento)
        {
            Delatado = true; aire = 0.4f; respirandoCerca = 0f;
            var cam = Camera.main;
            if (sonido) EstanDentro.Audio.SonidoCorto.Play(sonido, cam ? cam.transform.position : transform.position, 0.9f, 2.5f, 0f);
            ObjectiveHUD.Notify(pensamiento, 2f);
            var obs = Mente.Observador.Instancia;
            if (obs && jugador) obs.Delatar(jugador.position);
        }

        void OnDisable() { if (Actual == this) { Actual = null; Delatado = false; Mostrar(false); } }

        static void Mostrar(bool si)
        {
            if (si && !lienzo) Construir();
            if (lienzo) lienzo.enabled = si;
        }

        static void Construir()
        {
            var go = new GameObject("Escondite_Canvas"); DontDestroyOnLoad(go);
            lienzo = go.AddComponent<Canvas>(); lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 81;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            fondoBarra = Barra(go.transform, new Color(0f, 0f, 0f, 0.5f));
            barra = Barra(go.transform, Color.white);
            barra.type = Image.Type.Filled; barra.fillMethod = Image.FillMethod.Horizontal;
            var r = new GameObject("Texto", typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(go.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.anchoredPosition = new Vector2(0f, 215f); r.sizeDelta = new Vector2(900f, 40f);
            texto = r.gameObject.AddComponent<Text>();
            texto.font = Fuentes.Maquina ? Fuentes.Maquina : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 28; texto.alignment = TextAnchor.MiddleCenter; texto.color = new Color(0.93f, 0.89f, 0.82f, 0.95f); texto.raycastTarget = false;
            var sombra = r.gameObject.AddComponent<Shadow>(); sombra.effectColor = new Color(0, 0, 0, 0.8f); sombra.effectDistance = new Vector2(2, -2);
            lienzo.enabled = false;
        }

        static Image Barra(Transform padre, Color c)
        {
            var r = new GameObject("Aire", typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(padre, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.anchoredPosition = new Vector2(0f, 180f); r.sizeDelta = new Vector2(360f, 8f);
            var i = r.gameObject.AddComponent<Image>(); i.color = c; i.raycastTarget = false;
            var tex = Texture2D.whiteTexture; i.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return i;
        }
    }
}
