using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// RESPIRAR CON PA (Q o LB). Antes era "mantener Q y el estrés baja solo"; ahora respirar es seguir un ritmo, el mismo
    /// que Pa le cuenta a Mateo desde el hospital: INHALA 4 s (mantén Q) · SOSTÉN 4 s (sigue manteniendo) · EXHALA 6 s
    /// (suelta). A tiempo baja el estrés; a destiempo casi no sirve y hasta lo sube un poco. Un ciclo completo bien hecho
    /// da un alivio extra y, si Mateo ya está calmado, lo que se puede usar cerca brilla unos segundos (nuestro "Revelio").
    /// Con micrófono calibrado, en EXHALA también vale soplar de verdad.
    /// La guía (anillo que crece y se encoge, abajo al centro) solo aparece mientras se respira.
    /// </summary>
    public class Concentrarse : MonoBehaviour
    {
        // ritmo 4-4-6 (la frase de Pa: "Respira conmigo, mijo. Cuatro… sostén…")
        public float inhala = 4f, sostiene = 4f, exhala = 6f;
        public float bajaPorSegundo = 6.5f;     // estrés que se va por segundo respirando a tiempo
        public float subeADestiempo = 1.5f;     // estrés que sube por segundo respirando a destiempo
        public float alivioCiclo = 10f;         // extra al cerrar un ciclo bien hecho
        public float precisionCiclo = 0.75f;    // fracción del ciclo a tiempo para contarlo bien hecho
        public float esperaFin = 2.5f;          // sin Q tras exhalar: se termina la respiración

        // revelar lo usable cerca (como antes)
        public float radio = 7f;
        public float duracion = 2.8f;
        public float espera = 6f;
        public float estresMaximo = 0.6f;       // normalizado; por encima no se ve más, solo se calma

        /// <summary>Para el resumen del final ("Lo que aprendiste") y la prueba automática.</summary>
        public static int CiclosBuenos, CiclosTotales;
        public static bool Respirando { get; private set; }
        /// <summary>La prueba automática "mantiene Q" con esto (no tiene teclado de verdad).</summary>
        public static bool SimularQ;

        Camera cam;
        Canvas lienzo, lienzoMarcas;
        Image velo, anillo, progreso, nucleo;
        Text fase, sub;
        Sprite punto, aro;
        float listoEn, t, aTiempo, total, sinQ, rojo;
        int ciclosSesion;
        bool qAntes, saludo;
        AudioSource respiro;
        AudioClip vozInicio, vozCuatro, vozSosten;
        static float vozInicioEn = -999f;

        /// <summary>Otro script ya dijo "Respira conmigo, mijo" (p. ej. el final): no repetirlo al empezar a respirar.</summary>
        public static void YaInvito() { vozInicioEn = Time.time; }

        static Concentrarse inst;
        /// <summary>El final del hospital tapa el HUD (capa 170): la guía de respirar tiene que quedar encima de él.</summary>
        public static void GuiaEncima(bool si) { if (inst && inst.lienzo) inst.lienzo.sortingOrder = si ? 172 : 80; }
        Player.PlayerController jugador;
        bool frenado;
        readonly List<(Transform t, Vector3 centro, Image img)> marcas = new List<(Transform, Vector3, Image)>();

        float Ciclo => inhala + sostiene + exhala;
        enum Fase { Inhala, Sostiene, Exhala }
        Fase FaseDe(float x) => x < inhala ? Fase.Inhala : x < inhala + sostiene ? Fase.Sostiene : Fase.Exhala;

        void Start()
        {
            inst = this;
            cam = GetComponentInChildren<Camera>() ?? Camera.main;
            vozInicio = Resources.Load<AudioClip>("Voces/Respirar/pa_respira_conmigo");
            vozCuatro = Resources.Load<AudioClip>("Voces/Respirar/pa_cuatro");
            vozSosten = Resources.Load<AudioClip>("Voces/Respirar/pa_sosten");
            Construir();
        }

        void OnDisable() { Terminar(); }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            ActualizarMarcas();
            if (!jugador) jugador = GetComponentInParent<Player.PlayerController>();
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool puede = !OverlayBlocker.IsBlocking && Time.timeScale > 0f && Escondite.Actual == null;   // escondido, Q es aguantar el aire
            bool q = puede && ((kb != null && kb.qKey.isPressed) || (gp != null && gp.leftShoulder.isPressed) || SimularQ);

            if (!Respirando)
            {
                if (q && !qAntes) Empezar();
                qAntes = q;
                return;
            }
            qAntes = q;
            if (!puede) { Dibujar(); return; }                                  // pausa, nota abierta…: la guía se queda quieta

            float dt = Time.deltaTime;
            Fase antes = FaseDe(t);
            t += dt;
            if (t >= Ciclo) { CerrarCiclo(); t -= Ciclo; }
            Fase f = FaseDe(t);
            if (f != antes) { AlEntrar(f); if (f == Fase.Inhala) sinQ = 0f; }       // al volver a inhalar se cuenta de nuevo

            // ¿va a tiempo? Inhala y sostén = Q mantenida; exhala = Q suelta (o soplar al micrófono)
            bool bien = f == Fase.Exhala ? (!q || MicExhalando()) : q;
            total += dt; if (bien) aTiempo += dt;
            var estres = Stress.StressSystem.Instance;
            if (estres != null) estres.AddSilencioso(bien ? -bajaPorSegundo * dt : subeADestiempo * dt);
            rojo = bien ? Mathf.Max(0f, rojo - dt * 3f) : Mathf.Min(1f, rojo + dt * 4f);

            // se acaba la respiración si en la inhalación Q no se mantiene (no volvió a inhalar, o la soltó a mitad)
            sinQ = q ? 0f : sinQ + dt;
            if (f == Fase.Inhala && sinQ > esperaFin) { Terminar(); return; }

            if (q) GuiaTeclas.Usada("Q");
            Dibujar();
        }

        void Empezar()
        {
            Respirando = true; t = 0f; aTiempo = 0f; total = 0f; sinQ = 0f; rojo = 0f; ciclosSesion = 0;
            Frenar(true);
            Respirar(true);
            lienzo.enabled = true;
            // Pa invita a respirar (no cada vez: cansaría). Si habla, el "cuatro" del primer ciclo va solo en subtítulo.
            saludo = Time.time - vozInicioEn > 45f && vozInicio;
            if (saludo) { vozInicioEn = Time.time; Decir(vozInicio, "Respira conmigo, mijo. Cuatro…"); }
            else AlEntrar(Fase.Inhala);
        }

        void Terminar()
        {
            if (!Respirando) return;
            Respirando = false;
            Frenar(false);
            Respirar(false);
            if (lienzo) lienzo.enabled = false;
            Velo(0f);
        }

        void AlEntrar(Fase f)
        {
            if (ciclosSesion >= 2) return;                                             // Pa cuenta en los dos primeros ciclos
            switch (f)
            {
                case Fase.Inhala: if (vozCuatro) Decir(vozCuatro, "Cuatro…"); else Sub("Cuatro…"); break;
                case Fase.Sostiene: if (vozSosten) Decir(vozSosten, "sostén…"); else Sub("sostén…"); break;
                case Fase.Exhala: Sub("…y suelta despacio. Seis."); break;
            }
        }

        void CerrarCiclo()
        {
            ciclosSesion++; CiclosTotales++;
            bool bueno = total > 0f && aTiempo / total >= precisionCiclo;
            aTiempo = 0f; total = 0f;
            if (!bueno) return;
            CiclosBuenos++;
            var estres = Stress.StressSystem.Instance;
            if (estres != null) estres.AddSilencioso(-alivioCiclo);
            if (estres != null && estres.Normalized <= estresMaximo && Time.time >= listoEn)
            {
                listoEn = Time.time + espera;
                StartCoroutine(Revelar());
            }
        }

        static bool MicExhalando()
        {
            var mic = Breathing.BreathingInputProvider.Instance;
            return mic != null && mic.Mode == Breathing.BreathingInputProvider.InputMode.Microphone && mic.IsExhalingNow();
        }

        void Frenar(bool si)
        {
            if (!jugador || frenado == si) return;                                     // solo al cambiar: no pisa a la silla arrastrable
            frenado = si;
            jugador.FactorVelocidad = si ? 0.35f : 1f;
        }

        void Decir(AudioClip clip, string texto)
        {
            Audio.Voz.Sonar(clip, cam ? cam.transform.position : transform.position, Audio.Voz.Lugar.Lejos, 0.9f);
            Sub(texto);
        }

        string subTexto = "";
        float subAlfa;
        void Sub(string texto) { subTexto = texto; subAlfa = 1f; PintarSub(); }

        // "Pa:" y la frase se desvanecen juntos (antes el nombre quedaba fijo y la frase se iba sola)
        void PintarSub()
        {
            if (!sub) return;
            int a = Mathf.RoundToInt(Mathf.Clamp01(subAlfa) * 255f);
            sub.text = $"<color=#d9b45a{a:X2}>Pa:</color> <color=#ece4d2{a:X2}>{subTexto}</color>";
        }

        /// <summary>El sonido de la respiración lenta mientras dura.</summary>
        void Respirar(bool si)
        {
            if (si && !respiro)
            {
                respiro = EstanDentro.Audio.SonidoCorto.Juego("cuerpo/respiracion_miedo", cam ? cam.transform.position : transform.position, 0.3f, 0f);
                if (respiro) { respiro.loop = true; respiro.pitch = 0.78f; respiro.transform.SetParent(transform, true); }
            }
            else if (!si && respiro) { Destroy(respiro.gameObject); respiro = null; }
        }

        void Dibujar()
        {
            Fase f = FaseDe(t);
            float k = f == Fase.Inhala ? t / inhala : f == Fase.Sostiene ? 1f : 1f - (t - inhala - sostiene) / exhala;
            k = Mathf.SmoothStep(0f, 1f, k);
            float esc = Mathf.Lerp(0.55f, 1f, k);
            anillo.rectTransform.localScale = new Vector3(esc, esc, 1f);
            nucleo.rectTransform.localScale = new Vector3(esc * 0.8f, esc * 0.8f, 1f);
            progreso.fillAmount = t / Ciclo;
            Color c = Color.Lerp(new Color(0.66f, 0.84f, 0.86f), new Color(0.85f, 0.25f, 0.2f), rojo);
            anillo.color = new Color(c.r, c.g, c.b, 0.9f);
            progreso.color = new Color(c.r, c.g, c.b, 0.55f);
            nucleo.color = new Color(c.r, c.g, c.b, 0.22f + 0.2f * k);
            float resta = f == Fase.Inhala ? inhala - t : f == Fase.Sostiene ? inhala + sostiene - t : Ciclo - t;
            fase.text = (f == Fase.Inhala ? "INHALA" : f == Fase.Sostiene ? "SOSTÉN" : "EXHALA") + "  " + Mathf.CeilToInt(Mathf.Max(0.01f, resta));
            if (subAlfa > 0f) { subAlfa = Mathf.Max(0f, subAlfa - Time.deltaTime * 0.25f); PintarSub(); }
            Velo(0.18f + 0.12f * k);
        }

        void Construir()
        {
            lienzo = Lienzo("Respirar_Canvas", 80);
            var v = Rect("Velo", lienzo.transform); v.anchorMin = Vector2.zero; v.anchorMax = Vector2.one; v.offsetMin = v.offsetMax = Vector2.zero;
            velo = v.gameObject.AddComponent<Image>(); velo.sprite = Vineta(); velo.color = new Color(0.02f, 0.05f, 0.06f, 0f); velo.raycastTarget = false;

            punto = CrearPunto(64);
            aro = CrearAro(128);
            var guia = Rect("Guia", lienzo.transform);
            guia.anchorMin = guia.anchorMax = new Vector2(0.5f, 0f); guia.pivot = new Vector2(0.5f, 0.5f);
            guia.anchoredPosition = new Vector2(0f, 320f); guia.sizeDelta = new Vector2(150f, 150f);   // por encima de los subtítulos de Mateo
            nucleo = Img("Nucleo", guia, punto, 150f);
            progreso = Img("Progreso", guia, aro, 172f);
            progreso.type = Image.Type.Filled; progreso.fillMethod = Image.FillMethod.Radial360; progreso.fillOrigin = (int)Image.Origin360.Top; progreso.fillClockwise = true;
            anillo = Img("Anillo", guia, aro, 150f);
            fase = Texto("Fase", guia, 30, new Vector2(0f, 112f));                  // la fase y la voz de Pa van arriba del anillo
            fase.color = new Color(0.93f, 0.89f, 0.82f, 0.95f);
            sub = Texto("Pa", guia, 26, new Vector2(0f, 150f));
            sub.supportRichText = true; sub.color = Color.white; sub.text = "";
            lienzo.enabled = false;

            // las marcas de "Revelar" viven en su propio lienzo: siguen visibles aunque la guía ya se haya cerrado
            lienzoMarcas = Lienzo("Revelar_Canvas", 79);
        }

        Canvas Lienzo(string n, int orden)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = orden;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            return c;
        }

        static RectTransform Rect(string n, Transform padre) { var r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(padre, false); return r; }

        static Image Img(string n, RectTransform padre, Sprite s, float tam)
        {
            var r = Rect(n, padre); r.sizeDelta = new Vector2(tam, tam);
            var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.raycastTarget = false; return i;
        }

        static Text Texto(string n, RectTransform padre, int tam, Vector2 pos)
        {
            var r = Rect(n, padre); r.sizeDelta = new Vector2(900f, 40f); r.anchoredPosition = pos;
            var tx = r.gameObject.AddComponent<Text>();
            tx.font = Fuentes.Maquina ? Fuentes.Maquina : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.fontSize = tam; tx.alignment = TextAnchor.MiddleCenter; tx.raycastTarget = false;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sombra = r.gameObject.AddComponent<Shadow>(); sombra.effectColor = new Color(0, 0, 0, 0.8f); sombra.effectDistance = new Vector2(2, -2);
            return tx;
        }

        /// <summary>¿Hay algo entre los ojos y el objeto? Se salta el cuerpo de Mateo (antes el primer choque era su propio
        /// cuerpo y por eso se marcaba todo, hasta lo que estaba detrás de las paredes y en otros salones).</summary>
        bool Tapado(Vector3 c, Transform objeto)
        {
            Vector3 o = cam.transform.position, d = c - o; float dist = d.magnitude;
            var hits = Physics.RaycastAll(o, d / dist, dist - 0.05f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var h in hits)
            {
                if (h.transform.root == transform.root) continue;                                // el propio Mateo
                if (h.transform.IsChildOf(objeto) || objeto.IsChildOf(h.transform)) return false; // llegó al objeto
                return true;
            }
            return false;
        }

        void Velo(float a) { if (velo) velo.color = new Color(velo.color.r, velo.color.g, velo.color.b, a); }

        IEnumerator Revelar()
        {
            foreach (var m in marcas) if (m.img) Destroy(m.img.gameObject);
            marcas.Clear();
            var vistos = new HashSet<GameObject>();
            foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                if (!it.isActiveAndEnabled || !vistos.Add(it.gameObject)) continue;
                if (it is LockedDoor puerta && !puerta.IsLocked) continue;        // puertas normales no: solo las cerradas
                if (it is CombinationLock cl && cl.IsSolved) continue;
                var r = it.GetComponentInChildren<Renderer>();
                Vector3 c = r ? r.bounds.center : it.transform.position;
                if (Vector3.Distance(c, cam.transform.position) > radio) continue;
                if (Mathf.Abs(c.y - cam.transform.position.y) > 2.2f) continue;                  // solo el piso donde está Mateo
                if (Tapado(c, it.transform)) continue;                                           // detrás de una pared: no
                var img = new GameObject("Marca", typeof(RectTransform)).AddComponent<Image>();
                img.transform.SetParent(lienzoMarcas.transform, false);
                img.sprite = punto; img.raycastTarget = false;
                ((RectTransform)img.transform).sizeDelta = new Vector2(70, 70);
                marcas.Add((it.transform, c, img));
            }
            float tt = 0f;
            while (tt < duracion)
            {
                tt += Time.deltaTime;
                float a = tt < 0.4f ? tt / 0.4f : 1f - Mathf.Clamp01((tt - duracion + 1f) / 1f);
                float pulso = 1f + 0.15f * Mathf.Sin(tt * 6f);
                foreach (var m in marcas)
                    if (m.img) { m.img.color = new Color(0.95f, 0.80f, 0.40f, 0.85f * a); m.img.transform.localScale = Vector3.one * pulso; }
                yield return null;
            }
            foreach (var m in marcas) if (m.img) Destroy(m.img.gameObject);
            marcas.Clear();
        }

        void ActualizarMarcas()
        {
            if (cam == null || marcas.Count == 0) return;
            var rt = (RectTransform)lienzoMarcas.transform;
            foreach (var m in marcas)
            {
                if (!m.img) continue;
                Vector3 s = cam.WorldToScreenPoint(m.centro);
                m.img.enabled = s.z > 0f;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, s, null, out var p);
                ((RectTransform)m.img.transform).anchoredPosition = p;
            }
        }

        static Sprite CrearPunto(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float halo = Mathf.Clamp01(1f - d); halo *= halo;
                    float anillo = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) * 14f);
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Max(halo * 0.6f, anillo));
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Un aro fino y suave (el anillo que respira y la barra circular del ciclo).</summary>
        static Sprite CrearAro(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) * 22f));
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Viñeta suave: oscurece los bordes mientras se respira (el mundo se aleja un poco).</summary>
        static Sprite Vineta()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.3f, d))));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }
    }
}
