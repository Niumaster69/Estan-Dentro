using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EstanDentro.Audio;
using EstanDentro.UI;

namespace EstanDentro.Prologo
{
    /// <summary>
    /// PRÓLOGO "17 de mayo" (NARRATIVA_UNIFICADA §2.1). Carro de Pa, 6:50 a. m., lluvia. Mateo de copiloto: se mira, no se camina.
    /// 4 cosas para tocar (cuaderno, lonchera, radio, celular), una respuesta para elegir, el presagio, el choque,
    /// el monitor del hospital y la campana del colegio → escena del colegio.
    /// Cada momento tiene entrada, durante y salida (fundidos, sonido y subtítulo).
    /// </summary>
    public class ControlPrologo : MonoBehaviour
    {
        public static ControlPrologo Instancia { get; private set; }

        [Header("Escena")]
        public Transform ojos;                 // cámara de Mateo
        public Transform[] calles;             // 2 tramos de calle que se van turnando
        public float largoCalle = 600f;
        public float finCalle = 50f;           // cuánto se extiende un tramo hacia adelante de su origen
        public Vector3 adelante = Vector3.forward;
        public float velocidad = 11f;          // ~40 km/h con lluvia
        public Transform[] limpiabrisas;       // pivotes de las plumillas
        public Light[] lucesCamion;
        public Transform camion;
        public Transform carroTrafico;         // un carro que pasa en sentido contrario (se clona)
        public Transform[] carrosTrafico;      // modelos reales (Logan, Spark, Aveo, taxi, bus SITP): se elige uno al azar
        public float sueloY;                   // altura de la calle (los vehículos van apoyados en ella)
        readonly List<Transform> trafico = new List<Transform>();
        float proximoCarro = 6f;
        public string escenaSiguiente = "Colegio";

        [Header("Cosas para tocar (por nombre de objeto)")]
        public GameObject cuaderno, lonchera, radio, celular;

        // estado
        readonly HashSet<string> tocados = new HashSet<string>();
        bool eligiendo, eligio, terminando, frenazo;
        int eleccion;
        float yaw, pitch;
        Quaternion rot0;
        Vector3 ojos0;
        AudioSource radioFuente;
        GameObject mirando;

        // interfaz
        Canvas lienzo; Text subtitulo, ayuda, opciones, titulo; Image negro, barra, punto;
        readonly Queue<(string quien, string que, float seg)> fila = new Queue<(string, string, float)>();
        bool hablando;

        public bool Termino => terminando;
        public IReadOnlyCollection<string> Tocados => tocados;

        void Awake()
        {
            Instancia = this;
            ConstruirUI();
            rot0 = ojos.localRotation; ojos0 = ojos.localPosition;
            // el celular es delgado y queda casi de canto sobre las piernas: la mira se le escapaba. Una caja de 14 cm
            // alrededor (como la mano que lo busca) basta para mirarlo sin tapar la lonchera ni el cuaderno
            AgrandarParaMirar(celular, 0.14f);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            foreach (var l in lucesCamion) if (l) l.enabled = false;
            if (camion) camion.gameObject.SetActive(false);
        }

        void Start()
        {
            // fondo sonoro: motor, lluvia en el techo (bucles) y el limpiabrisas al ritmo de su animación
            SonidoCorto.Play(SonidoCorto.Clip("prologo/carro_dentro"), ojos.position, 0.55f, 9999f, 0f, true);
            SonidoCorto.Play(SonidoCorto.Clip("prologo/lluvia_techo"), ojos.position + Vector3.up * 0.4f, 0.5f, 9999f, 0f, true);
            StartCoroutine(Guion());
            StartCoroutine(Limpiabrisas());
            StartCoroutine(Charla());
            CabinaCerrada();
        }

        // eco de cabina cerrada para todo lo que suena dentro: rebote muy corto, los agudos se los comen el tapiz y los asientos
        void CabinaCerrada()
        {
            var z = new GameObject("Eco_Cabina").AddComponent<AudioReverbZone>();
            z.transform.position = ojos.position - Vector3.Cross(Vector3.up, adelante) * 0.37f;
            z.minDistance = 2.2f; z.maxDistance = 3.2f;
            z.reverbPreset = AudioReverbPreset.User;
            z.room = -900; z.roomHF = -1600; z.roomLF = 0;
            z.decayTime = 0.28f; z.decayHFRatio = 0.35f;
            z.reflections = -300; z.reflectionsDelay = 0.002f;
            z.reverb = -700; z.reverbDelay = 0.004f;
            z.diffusion = 70f; z.density = 90f; z.HFReference = 5000f; z.LFReference = 250f;
        }

        // si Mateo se queda callado mirando por la ventana, Pa saca conversación (no quedan silencios largos)
        float ultimaFrase;
        static readonly (string quien, string que, float seg)[][] charla =
        {
            new[] { ("Pa", "¿Desayunó? Le dejé arepa en el microondas… medio quemada, eso sí.", 3.8f), ("Mateo", "Me la comí. Quemada y todo.", 2.2f) },
            new[] { ("Pa", "El domingo vamos al estadio, ¿sí o qué? Ya compré las boletas.", 3.6f), ("Mateo", "¿En serio, Pa?", 1.6f), ("Pa", "En serio. Usted y yo, como antes.", 2.8f) },
            new[] { ("Pa", "A su edad yo iba al colegio en cicla. Con este aguacero y todo.", 3.6f) },
            new[] { ("Pa", "Coma algo en el descanso, mijo, que usted no come nada.", 3.2f), ("Mateo", "Sí, Pa…", 1.4f) },
        };
        IEnumerator Charla()
        {
            ultimaFrase = Time.time;
            int i = 0;
            while (!terminando && i < charla.Length)
            {
                yield return null;
                if (hablando || cantando || eligiendo || radioLista || Time.time - ultimaFrase < 9f || Time.timeSinceLevelLoad < 22f) continue;
                foreach (var (quien, que, seg) in charla[i]) Decir(quien, que, seg);
                i++;
            }
        }

        // ------------------------------------------------------------------ guion
        IEnumerator Guion()
        {
            negro.color = Color.black;
            // entrada: en negro se oyen la lluvia, el motor y la radio del carro; la fecha y se abren los ojos
            PrenderRadio(0.045f);
            yield return new WaitForSeconds(1.2f);
            yield return Titulo("Bogotá. 17 de mayo. 6:42 a. m.", Fuentes.Maquina, 40, new Color(0.9f, 0.88f, 0.82f), 3.8f);
            yield return Fundido(1f, 0f, 2.5f);
            Decir("Pa", "Mire cómo está lloviendo… Bogotá entera salió en carro hoy.", 4f);
            yield return new WaitForSeconds(4.2f);
            StartCoroutine(GuiaMirar());
            yield return new WaitForSeconds(2f);

            // el diálogo llega después de tocar 2 cosas (o a los 40 s)
            float t0 = Time.time;
            yield return new WaitUntil(() => tocados.Contains(lonchera.name));   // la lonchera desencadena la conversación (sin apuro: el juego espera)
            yield return new WaitUntil(() => !hablando && !cantando);
            yield return new WaitForSeconds(1f);
            Decir("Pa", "El bus es más cómodo, ya sé. Pero hoy cumple dieciséis y lo traigo yo.", 4.5f);
            yield return new WaitForSeconds(4f);
            eligiendo = true;
            // como en Detroit: una barra que se va acabando; si no elige, queda la primera (Mateo no dice nada raro)
            const float plazo = 8f;
            float te = Time.time;
            barra.gameObject.SetActive(true);
            while (!eligio && Time.time - te < plazo)
            {
                float u = 1f - (Time.time - te) / plazo;
                ((RectTransform)barra.transform).sizeDelta = new Vector2(700f * u, 4f);
                barra.color = new Color(0.95f, 0.85f, 0.6f, 0.35f + 0.5f * u);
                opciones.text = "<color=#FFE7A8>[1]  Gracias, Pa.</color>          <color=#B8B0A0>[2]  Igual iba a llegar tarde.</color>";
                ayuda.text = "Elige con las teclas  1  o  2";
                yield return null;
            }
            if (!eligio) eleccion = 1;
            opciones.text = eleccion == 2 ? "<color=#5A564E>[1]  Gracias, Pa.</color>          <color=#FFE7A8>[2]  Igual iba a llegar tarde.</color>"
                                          : "<color=#FFE7A8>[1]  Gracias, Pa.</color>          <color=#5A564E>[2]  Igual iba a llegar tarde.</color>";
            barra.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            eligiendo = false; opciones.text = ""; ayuda.text = "";
            if (eleccion == 2) { Decir("Mateo", "Igual iba a llegar tarde.", 2f); Decir("Pa", "Ja. Pues llega tarde, pero llega con su papá.", 3.5f); }
            else { Decir("Mateo", "Gracias, Pa.", 2f); Decir("Pa", "Dieciséis… ¿En qué momento creció, mijo?", 3.5f); }

            // primero todo lo demás (cuaderno, lonchera y celular), sin apuro; el carro sigue andando
            float t1 = Time.time;
            yield return new WaitUntil(() => tocados.Contains(cuaderno.name) && tocados.Contains(celular.name));   // el juego espera: el carro sigue andando, la lluvia y la radio en bucle
            yield return new WaitUntil(() => !hablando && !cantando);
            // ahora sí la radio: el jugador le sube; si no, Pa la sube solo porque suena la que le gusta
            radioLista = true;
            float t2 = Time.time;
            // la radio la sube el jugador (nada avanza solo); si se demora, Pa lo anima una vez
            bool animo = false;
            while (!subioRadio)
            {
                if (!animo && Time.time - t2 > 18f && !hablando) { animo = true; Decir("Pa", "Súbale a la radio, mijo, que esa me gusta.", 3f); }
                yield return null;
            }
            // el canto, el "usted canta horrible", y después el presagio y el choque
            yield return null;
            while (cantoEnCurso || hablando || cantando) yield return null;
            yield return new WaitForSeconds(1.2f);
            yield return Presagio();
        }

        IEnumerator Presagio()
        {
            terminando = true; ayuda.text = ""; mirando = null; Tecla(false, null, null);
            // todo en orden, esperando a que cada uno termine: nada de relojes que se adelanten
            while (cantoEnCurso || hablando || cantando || eligiendo) yield return null;    // si Pa está cantando, primero termina
            fila.Clear();
            StartCoroutine(MirarAlFrente(1.4f));                    // Mateo levanta la vista: el jugador ve venir lo que viene
            Decir("Mateo", "Pa… ¿le cambia de emisora? Porfa.", 2.5f);
            yield return null; while (hablando) yield return null;   // Mateo lo pide (y termina de decirlo)
            yield return new WaitForSeconds(0.25f);
            Decir("Pa", "Ya va, ya va… a ver…", 2.2f);
            yield return new WaitForSeconds(0.6f);
            // Pa baja la mirada a la radio y busca emisora: la pantalla dice "Buscando…"
            SonidoCorto.Juego("prologo/radio_sintonia", radio.transform.position, 0.6f, 0.4f);
            if (radioFuente) radioFuente.Stop();
            var pr = radio.GetComponentInChildren<PantallaRadio>(true) ?? FindAnyObjectByType<PantallaRadio>(); if (pr) pr.Buscando();
            while (hablando) yield return null;
            yield return new WaitForSeconds(0.4f);
            // durante: luces de frente. Un camión invade el carril.
            if (camion)
            {
                camion.gameObject.SetActive(true);
                foreach (var l in lucesCamion) if (l) l.enabled = true;
                // viene en su carril como cualquier carro (85 km/h de acercamiento); a ~38 m se le va el carril
                Vector3 lado = Vector3.Cross(Vector3.up, adelante);
                float dist = 120f, desvio = -3.87f, vCarro = velocidad;
                bool frenando = false;
                while (dist > 4.5f)
                {
                    if (dist < 38f) desvio = Mathf.MoveTowards(desvio, -0.6f, 3.2f * Time.deltaTime);
                    if (dist < 22f && !frenazo)
                    {
                        frenazo = true; frenando = true;
                        Decir("Mateo", "¡PA!", 1.2f);
                        SonidoCorto.Juego("prologo/choque", ojos.position, 1f, 0f);
                    }
                    if (frenando) vCarro = Mathf.MoveTowards(vCarro, 2f, 9f * Time.deltaTime);
                    velocidad = vCarro;                                  // la calle frena con el carro
                    dist -= (vCarro + 12.5f) * Time.deltaTime;
                    var pc = ojos.position + adelante * dist + lado * desvio; pc.y = sueloY;
                    camion.position = pc;
                    bool cambiando = desvio > -3.8f && desvio < -0.7f;
                    camion.rotation = Quaternion.LookRotation(-adelante + (cambiando ? lado * 0.25f : Vector3.zero));
                    foreach (var l in lucesCamion) if (l) l.intensity = Mathf.Lerp(40000f, 3000f, dist / 120f);
                    if (dist < 30f)
                    {
                        // Mateo levanta la vista al frente; el carro se inclina al frenar
                        yaw = Mathf.MoveTowards(yaw, 0f, 120f * Time.deltaTime);
                        pitch = Mathf.MoveTowards(pitch, -2f, 90f * Time.deltaTime);
                        ojos.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch + (frenando ? 4f : 0f), 0f, 0f);
                    }
                    yield return null;
                }
            }
            if (!frenazo) SonidoCorto.Juego("prologo/choque", ojos.position, 1f, 0f);
            for (float k = 0f; k < 0.35f; k += Time.deltaTime)
            {
                ojos.localPosition = ojos0 + Random.insideUnitSphere * 0.08f * (1f - k / 0.35f);
                if (k > 0.15f) negro.color = Color.black;
                yield return null;
            }
            negro.color = Color.black;
            AudioListener.volume = 1f;
            choco = true;
            CallarTodo();                                              // ninguna frase sigue sonando encima de los créditos
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.loop) a.Stop();                                                 // silencio de golpe

            // salida (como The Last of Us): silencio, y en negro entran la canción triste y el título del juego
            yield return new WaitForSeconds(2.2f);
            var musica = SonidoCorto.Play(Resources.Load<AudioClip>("Prologo/musica_creditos"), ojos.position, 0f, 9999f, 0f, false);
            if (musica) { musica.pitch = 1f; musica.gameObject.AddComponent<Agachar>().dbAbajo = -6f; StartCoroutine(Volumen(musica, 0.32f, 3f)); }
            yield return new WaitForSeconds(3f);
            // créditos de inicio (como The Last of Us: después de la tragedia, en negro, con la canción triste)
            var crema = new Color(0.9f, 0.88f, 0.82f);
            yield return Titulo("ESTÁN DENTRO", Fuentes.Titulo, 110, new Color(0.62f, 0.08f, 0.06f), 6f);
            yield return Titulo("Un juego de\n\nHenry Alejandro Ortega Rodríguez\nDuvan Lozano Romero\nCarlos Mario del Valle", Fuentes.Maquina, 34, crema, 6f);
            yield return Titulo("Música\n\n«Lost Dream Loop» — SergeQuadrado\n«La cumbia pasando» — Chogo El Bandeño", Fuentes.Maquina, 30, crema, 5f);
            yield return new WaitForSeconds(1f);
            // debajo de la música aparece el monitor del hospital y la voz de Pa, lejos; la música se va
            var monitor = SonidoCorto.Play(SonidoCorto.Clip("prologo/monitor"), ojos.position, 0f, 30f, 0f, true);
            if (monitor) StartCoroutine(Volumen(monitor, 0.4f, 4f));
            if (musica) StartCoroutine(Volumen(musica, 0.16f, 5f));
            yield return new WaitForSeconds(4f);
            Decir("", "…respira conmigo, mijo. Cuatro… sostén…", 4.5f, lejano: true);
            yield return new WaitForSeconds(5.5f);
            if (musica) StartCoroutine(Volumen(musica, 0f, 4f));
            // el pitido del monitor se vuelve la campana del colegio
            if (monitor) StartCoroutine(Volumen(monitor, 0f, 2.5f));
            SonidoCorto.Juego("ambiente/timbre", ojos.position, 0.8f, 0f);
            yield return new WaitForSeconds(3.6f);
            SceneManager.LoadScene(escenaSiguiente);
        }

        IEnumerator MirarAlFrente(float seg)
        {
            float y0 = yaw, p0 = pitch;
            for (float k = 0f; k < seg; k += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, k / seg);
                yaw = Mathf.Lerp(y0, -12f, u); pitch = Mathf.Lerp(p0, 2f, u);         // al frente, un poco hacia Pa
                ojos.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ tocar cosas
        public void Tocar(GameObject g)
        {
            if (g == null || terminando) return;                                 // el toque siempre cuenta; lo que se dice va en fila
            if (tocados.Contains(g.name)) return;                                // cada cosa una sola vez (no se repite lo que dicen)
            if (g == radio && !radioLista) return;
            if (!PuedeTocar) return;                                             // mientras alguien habla no se mete otra conversación
            if (g == cuaderno)
            {
                SonidoCorto.Juego("objetos/cuaderno_pagina", g.transform.position, 0.6f, 0.3f);
                Decir("Mateo", "(Ejercicios pág. 142. Examen el viernes… y no he hecho ni uno.)", 4f);
            }
            else if (g == lonchera)
            {
                SonidoCorto.Juego("objetos/objeto_tomar", g.transform.position, 0.6f, 0.3f);
                if (!revisandoLonchera) StartCoroutine(MirarLonchera(g.transform));
                Decir("Mateo", "¿Y este candado?", 1.8f);
                Decir("Pa", "Le puse candado para que no se la roben. La clave es su ruta, la 46. Cero, cuatro, seis.", 5.5f);
            }
            else if (g == radio)
            {
                if (!subioRadio)
                {
                    // Mateo le sube a la radio y Pa se pone a cantar encima de la cumbia (mal, a propósito)
                    subioRadio = true;
                    StartCoroutine(PaCanta());
                }
                else { Decir("Mateo", "(Mejor la dejo. A Pa le gusta.)", 2.5f); }
            }
            else if (g == celular)
            {
                SonidoCorto.Juego("sustos/celular_vibra", g.transform.position, 0.35f, 0.3f, 1.2f);
                var pan = g.GetComponentInChildren<PantallaCelular>(); if (pan) pan.Mensaje();   // se ilumina: llega el mensaje del grupo
                Decir("", "Grupo 4°B:  «¡Feliz cumple, Mendoza! 🎂🎉»", 3.5f);
                Decir("Mateo", "(De fondo de pantalla, Pa y yo en el estadio. Tenía nueve.)", 3.5f);
            }
            else return;
            tocados.Add(g.name);
        }

        // para la prueba automática: girar la cabeza (dentro de los límites del cuello) hacia una dirección del mundo
        public void Apuntar(Vector3 dirMundo)
        {
            var local = Quaternion.Inverse(ojos.parent ? ojos.parent.rotation * rot0 : rot0) * dirMundo;
            yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -110f, 95f);
            pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -45f, 84f);
            ojos.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
        }
        static void AgrandarParaMirar(GameObject g, float lado)
        {
            if (!g) return;
            var bc = g.GetComponent<BoxCollider>(); if (!bc) return;
            var s = g.transform.lossyScale;
            Vector3 minimo = new Vector3(lado / Mathf.Max(1e-4f, Mathf.Abs(s.x)), lado / Mathf.Max(1e-4f, Mathf.Abs(s.y)), lado / Mathf.Max(1e-4f, Mathf.Abs(s.z)));
            bc.size = Vector3.Max(bc.size, minimo);
        }

        public GameObject Mirando => mirando;
        public bool RadioLista => radioLista;
        /// <summary>Se puede tocar algo solo en silencio: nadie hablando, cantando ni eligiendo (así no se cruzan las conversaciones).</summary>
        public bool PuedeTocar => !terminando && !hablando && !cantando && !eligiendo && !revisandoLonchera;
        public string LoQueVe() => Physics.Raycast(ojos.position, ojos.forward, out var h, 3f) ? $"{h.collider.name} a {h.distance:F2} m" : "nada";

        public void Elegir(int n) { if (eligiendo) { eleccion = n; eligio = true; } }

        // ------------------------------------------------------------------ cada cuadro
        void Update()
        {
            // la calle pasa: el carro está quieto y el mundo se mueve hacia atrás (dos tramos que se turnan)
            foreach (var c in calles)
                {
                    c.position -= adelante * velocidad * Time.deltaTime;
                    // cada tramo va de su origen hacia ATRÁS (origen-600 … origen+50): se pasa adelante apenas queda todo detrás
                    if (Vector3.Dot(c.position - ojos.position, adelante) < -(finCalle + 40f)) c.position += adelante * largoCalle * calles.Length;
                }
            // tráfico normal en sentido contrario: así el camión del final no sale de la nada
            if ((carroTrafico || (carrosTrafico != null && carrosTrafico.Length > 0)) && Time.timeSinceLevelLoad > proximoCarro && !terminando)
            {
                proximoCarro = Time.timeSinceLevelLoad + Random.Range(7f, 14f);
                var molde = carrosTrafico != null && carrosTrafico.Length > 0 ? carrosTrafico[Random.Range(0, carrosTrafico.Length)] : carroTrafico;
                var t = Instantiate(molde, molde.parent); t.gameObject.SetActive(true);
                var pt = ojos.position + adelante * 160f - Vector3.Cross(Vector3.up, adelante) * 3.87f; pt.y = sueloY;
                t.position = pt;
                t.rotation = Quaternion.LookRotation(-adelante);
                trafico.Add(t);
            }
            for (int i = trafico.Count - 1; i >= 0; i--)
            {
                var t = trafico[i];
                t.position -= adelante * (velocidad + 12f) * Time.deltaTime;
                if (Vector3.Dot(t.position - ojos.position, adelante) < -30f) { Destroy(t.gameObject); trafico.RemoveAt(i); }
            }
            // vibración suave del carro

            SeguirRadio();
            // el punto del centro solo cuando se está jugando (se puede mover la cabeza): no en negro, títulos ni presagio
            Mirar();
            Apuntar();
            // el punto del centro solo cuando se está jugando (se puede mover la cabeza): no en negro, títulos ni presagio;
            // crece y se aclara al mirar algo que se puede tocar
            if (punto)
            {
                punto.enabled = !terminando && negro.color.a < 0.3f;
                float meta = mirando ? 16f : 6f;
                var rp = (RectTransform)punto.transform;
                float tam = Mathf.Lerp(rp.sizeDelta.x, meta, Time.deltaTime * 12f);
                rp.sizeDelta = new Vector2(tam, tam);
                punto.color = new Color(1f, mirando ? 0.92f : 1f, mirando ? 0.7f : 1f, mirando ? 0.95f : 0.5f);
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.wasPressedThisFrame && mirando) Tocar(mirando);
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Elegir(1);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Elegir(2);
            }
        }

        void Mirar()
        {
            if (terminando) return;
            Vector2 d = Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.08f : Vector2.zero;
            movidoMouse += d.magnitude / 0.08f + (Gamepad.current != null ? Gamepad.current.rightStick.ReadValue().magnitude * 200f * Time.deltaTime : 0f);
            if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * 90f * Time.deltaTime;
            yaw = Mathf.Clamp(yaw + d.x, -110f, 95f);           // no se puede girar del todo: va sentado con cinturón
            // hacia abajo se alcanza el celular y la lonchera; mirando a los lados, menos (el hombro está ahí)
            float maxAbajo = Mathf.Lerp(84f, 38f, Mathf.InverseLerp(35f, 105f, Mathf.Abs(yaw)));   // de frente se alcanza a mirar el celular en las piernas
            pitch = Mathf.Clamp(pitch - d.y, -45f, maxAbajo);
            ojos.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
            // la cabeza gira sobre el cuello (detrás y debajo de los ojos), no sobre los ojos
            Vector3 cuello = new Vector3(0f, 0.10f, 0.08f);
            ojos.localPosition = ojos0 - rot0 * cuello + ojos.localRotation * cuello
                               + new Vector3(0f, Mathf.PerlinNoise(Time.time * 2.2f, 0f) - 0.5f, 0f) * 0.008f;   // vibración suave del carro
        }

        void Apuntar()
        {
            mirando = null;
            if (terminando || !PuedeTocar) { if (!eligiendo) ayuda.text = ""; Tecla(false, null, null); return; }
            var cam = ojos.GetComponent<Camera>();
            Ray r = new Ray(ojos.position, ojos.forward);
            foreach (var h in Physics.RaycastAll(r, 1.4f).OrderBy(h => h.distance))
            {
                var g = h.collider.gameObject;
                while (g && g != cuaderno && g != lonchera && g != radio && g != celular) g = g.transform.parent ? g.transform.parent.gameObject : null;
                if (g) { mirando = tocados.Contains(g.name) || (g == radio && !radioLista) ? null : g; break; }   // lo ya tocado no vuelve a ofrecer [E]; la radio, al final
            }
            ayuda.text = "";
            // guía junto al punto: la tecla dibujada y lo que hace (cada cosa nueva que se puede hacer se enseña)
            string accion = mirando == null ? null : mirando == cuaderno ? "Mirar el cuaderno" : mirando == lonchera ? "Levantar la lonchera"
                          : mirando == radio ? "Subirle a la radio" : "Mirar el celular";
            Tecla(accion != null, "E", accion);
        }

        IEnumerator Limpiabrisas()
        {
            if (limpiabrisas == null || limpiabrisas.Length == 0) yield break;
            var ini = limpiabrisas.Select(l => l.localRotation).ToArray();
            // hacia dónde gira cada plumilla para subir por el vidrio (y no meterse en el capó)
            var signo = new float[limpiabrisas.Length];
            for (int i = 0; i < limpiabrisas.Length; i++)
            {
                var l = limpiabrisas[i];
                float y0 = l.TransformPoint(Vector3.right * 0.5f).y;
                l.localRotation = ini[i] * Quaternion.Euler(0f, 0f, 10f);
                signo[i] = l.TransformPoint(Vector3.right * 0.5f).y > y0 ? 1f : -1f;
                l.localRotation = ini[i];
            }
            while (true)
            {
                SonidoCorto.Juego("prologo/limpiabrisas", limpiabrisas[0].position, 0.35f, 0.4f);
                for (float k = 0f; k < 1.6f; k += Time.deltaTime)
                {
                    float a = Mathf.Sin(Mathf.PI * k / 1.6f) * 95f;                       // va y vuelve
                    for (int i = 0; i < limpiabrisas.Length; i++) limpiabrisas[i].localRotation = ini[i] * Quaternion.Euler(0f, 0f, signo[i] * a);
                    yield return null;
                }
                yield return new WaitForSeconds(1.1f);                       // siguen hasta el choque: Pa necesita ver
            }
        }

        // ------------------------------------------------------------------ subtítulos
        void Decir(string quien, string que, float seg, bool lejano = false)
        {
            fila.Enqueue((quien, lejano ? "<i>" + que + "</i>" : que, seg));
            if (!hablando) StartCoroutine(nameof(Hablar));
        }

        IEnumerator Hablar()
        {
            hablando = true;
            while (fila.Count > 0)
            {
                while (cantando) yield return null;
                var (quien, que, seg) = fila.Dequeue();
                subtitulo.text = string.IsNullOrEmpty(quien) ? que : $"<color=#E8B04A>{quien}:</color>  {que}";
                for (float a = 0f; a < 1f; a += Time.deltaTime * 5f) { subtitulo.color = new Color(1, 1, 1, a); yield return null; }
                subtitulo.color = Color.white;
                // la voz grabada (si existe): Pa desde su puesto, Mateo en voz alta o pensando, la voz lejana del hospital
                bool lejos = que.StartsWith("<i>"), piensa = quien == "Mateo" && que.StartsWith("(");
                var clip = Voz.Clip("Prologo", que);
                if (clip)
                {
                    Vector3 pos = quien == "Pa" ? ojos.position - Vector3.Cross(Vector3.up, adelante) * 0.75f : ojos.position;
                    Voz.Sonar(clip, pos, lejos ? Voz.Lugar.Lejos : piensa ? Voz.Lugar.Pensamiento : quien == "Pa" ? Voz.Lugar.CarroPa : quien == "Mateo" ? Voz.Lugar.CarroMateo : Voz.Lugar.Carro);
                    seg = Mathf.Max(clip.length + 0.15f, 0.8f);
                }
                yield return new WaitForSeconds(seg);
                for (float a = 1f; a > 0f; a -= Time.deltaTime * 5f) { subtitulo.color = new Color(1, 1, 1, a); yield return null; }
                subtitulo.text = "";
                ultimaFrase = Time.time;
                yield return new WaitForSeconds(0.1f);
            }
            hablando = false;
        }

        bool subioRadio, revisandoLonchera, radioLista;    // la radio solo se ofrece al final (es lo que lleva al choque)

        // Mateo levanta la lonchera, la gira para ver el candadito (el jugador ve las 3 ruedas) y la vuelve a dejar
        IEnumerator MirarLonchera(Transform lon)
        {
            revisandoLonchera = true;
            var cand = lon.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith("Candadito"));
            Vector3 p0 = lon.position; Quaternion r0 = lon.rotation;
            Vector3 centro = lon.GetComponentInChildren<Renderer>() ? lon.GetComponentInChildren<Renderer>().bounds.center : p0;
            Vector3 frente = cand ? (cand.GetComponentInChildren<Renderer>().bounds.center - centro) : lon.forward;
            frente.y = 0f; if (frente.sqrMagnitude < 1e-6f) frente = lon.forward;
            // frente al pecho, por encima de las rodillas (en el aire libre del carro), con el candado mirando a la cámara.
            // El camino sube primero en vertical (por fuera del brazo) y luego se acerca: no atraviesa a Mateo.
            Vector3 al = Vector3.ProjectOnPlane(ojos.forward, Vector3.up).normalized; if (al.sqrMagnitude < 0.01f) al = adelante;
            Vector3 destino = ojos.position + al * 0.42f + Vector3.down * 0.22f;
            Quaternion giro = Quaternion.FromToRotation(frente.normalized, (ojos.position - destino).normalized) * r0;
            Vector3 desfase = p0 - centro;
            Vector3 c0 = centro, c2 = destino, c1 = new Vector3(c0.x, Mathf.Max(c0.y, c2.y) + 0.12f, c0.z);   // control: arriba del punto de salida
            for (float k = 0f; k < 1.1f; k += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, k / 1.1f);
                lon.rotation = Quaternion.Slerp(r0, giro, u);
                Vector3 c = (1 - u) * (1 - u) * c0 + 2 * (1 - u) * u * c1 + u * u * c2;
                lon.position = c + lon.rotation * Quaternion.Inverse(r0) * desfase;
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            yield return new WaitUntil(() => !hablando);                         // mientras Pa explica la clave
            yield return new WaitForSeconds(0.6f);
            Quaternion r1 = lon.rotation;
            for (float k = 0f; k < 1.1f; k += Time.deltaTime)                     // de vuelta por el mismo camino
            {
                float u = 1f - Mathf.SmoothStep(0f, 1f, k / 1.1f);
                lon.rotation = Quaternion.Slerp(r0, r1, u);
                Vector3 c = (1 - u) * (1 - u) * c0 + 2 * (1 - u) * u * c1 + u * u * c2;
                lon.position = c + lon.rotation * Quaternion.Inverse(r0) * desfase;
                yield return null;
            }
            lon.SetPositionAndRotation(p0, r0);
            SonidoCorto.Juego("objetos/objeto_tomar", p0, 0.4f, 0.3f);
            revisandoLonchera = false;
        }

        // "La cumbia pasando" (Chogo El Bandeño): la radio salta al pedazo de la trompeta y Pa lo canta encima con su voz
        // (la voz del cantante pasada a la de Pa en ElevenLabs: 04_Audio/voces/prologo/pa_canta_trompeta.mp3, empieza en 97,4 s)
        const float CantoDesde = 97.4f;
        bool cantando;                                                  // mientras Pa canta no entra ninguna otra frase
        bool cantoEnCurso, choco;                                              // el canto y las dos respuestas (el presagio espera a que termine todo)
        IEnumerator PaCanta()
        {
            cantoEnCurso = true;
            yield return new WaitUntil(() => !hablando);                // espera a que nadie esté hablando
            if (choco) { cantoEnCurso = false; yield break; }
            cantando = true;
            if (!radioFuente) PrenderRadio(0.09f);
            if (!radioFuente) yield break;
            radioFuente.time = CantoDesde;
            foreach (var p in parlantes) if (p) p.time = CantoDesde;
            StartCoroutine(Volumen(radioFuente, 0.09f, 0.6f));
            var canto = Voz.Sonar(Voz.Clip("Prologo", "pa_canta_trompeta"), ojos.position - Vector3.Cross(Vector3.up, adelante) * 0.75f, Voz.Lugar.CarroPa, 0.85f, agachaMusica: false);   // canta CON la radio: la radio no baja
            // subtítulos del canto directo (no por la fila: la fila queda en pausa mientras canta)
            float dur = canto ? canto.clip.length : 13f;
            subtitulo.color = Color.white;
            subtitulo.text = "<color=#E8B04A>Pa:</color>  ♪ También me traje las maracas, el güiro y las tumbadoras… ♪";
            yield return new WaitForSeconds(dur * 0.5f);
            subtitulo.text = "<color=#E8B04A>Pa:</color>  ♪ El cantar de esta trompeta… que cuando no ríe, llora. ♪";
            yield return new WaitForSeconds(dur * 0.5f + 0.3f);
            subtitulo.text = "";
            cantando = false;
            if (!choco)
            {
                Decir("Mateo", "Pa, usted canta horrible.", 2.2f);
                Decir("Pa", "Horrible pero con sentimiento.", 2.5f);
                yield return null; while (hablando) yield return null;
            }
            cantoEnCurso = false;
        }

        // la radio del carro: cumbia (Fósforo, "Cumbia de Obama", CC BY-NC-SA) por un parlante pequeño de tablero
        // 4 parlantes como en un carro: puertas de adelante (abajo, cerca de los pies) y de atrás. radioFuente es el de la
        // puerta de Pa y manda: volumen y tiempo; los otros 3 lo siguen (Update → SeguirRadio).
        readonly List<AudioSource> parlantes = new List<AudioSource>();
        void PrenderRadio(float vol)
        {
            if (radioFuente) return;
            var clip = Resources.Load<AudioClip>("Prologo/radio_cancion");
            if (!clip) return;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante), centro = ojos.position - lado * 0.37f;
            Vector3[] puestos =
            {
                centro - lado * 0.82f + adelante * 0.35f + Vector3.down * 0.75f,   // puerta de Pa
                centro + lado * 0.82f + adelante * 0.35f + Vector3.down * 0.75f,   // puerta de Mateo
                centro - lado * 0.80f - adelante * 0.95f + Vector3.down * 0.55f,   // atrás
                centro + lado * 0.80f - adelante * 0.95f + Vector3.down * 0.55f,
            };
            double inicio = AudioSettings.dspTime + 0.1;
            foreach (var p in puestos)
            {
                var go = new GameObject("Parlante_Radio"); go.transform.position = p;
                var a = go.AddComponent<AudioSource>();
                a.clip = clip; a.loop = true; a.volume = vol; a.pitch = 1f; a.spatialBlend = 1f;
                a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = 0.4f; a.maxDistance = 4f; a.dopplerLevel = 0f;
                // parlante de puerta: sin graves profundos ni agudos finos, un poco de distorsión de cono barato
                go.AddComponent<EqCabina>().Iniciar(EqCabina.Perfil.Parlante);
                go.AddComponent<AudioDistortionFilter>().distortionLevel = 0.05f;
                go.AddComponent<Agachar>();                                      // baja cuando alguien habla
                a.PlayScheduled(inicio);
                parlantes.Add(a);
            }
            radioFuente = parlantes[0];
        }

        void SeguirRadio()
        {
            if (parlantes.Count == 0) return;
            for (int i = 1; i < parlantes.Count; i++)
            {
                var p = parlantes[i]; if (!p) continue;
                if (!radioFuente || !radioFuente.isPlaying) { if (p.isPlaying && radioFuente && radioFuente.time > 0.2f) p.Stop(); continue; }
                p.volume = radioFuente.volume * (i < 2 ? 1f : 0.7f);                       // los de atrás, más lejos y tapados por los asientos
                if (Mathf.Abs(p.time - radioFuente.time) > 0.04f) p.time = radioFuente.time;
            }
        }

        void CallarTodo()
        {
            fila.Clear(); StopCoroutine(nameof(Hablar)); hablando = false; subtitulo.text = "";
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a && a.name.StartsWith("Voz_")) Destroy(a.gameObject);
        }

        // ------------------------------------------------------------------ guía de controles (se enseña lo nuevo)
        GameObject teclaGO; Text teclaLetra, teclaTexto; Text guiaGrande;
        float movidoMouse;

        /// <summary>Al empezar: "mueve el mouse para mirar", hasta que el jugador de verdad mueve la cabeza.</summary>
        IEnumerator GuiaMirar()
        {
            // fijo, sin parpadear; se quita apenas el jugador mueve la cámara
            movidoMouse = 0f;
            guiaGrande.text = "Mueve el  <b>mouse</b>  para mirar alrededor";
            guiaGrande.color = new Color(1f, 0.95f, 0.85f, 1f);
            while (movidoMouse < 15f) yield return null;
            guiaGrande.text = ""; guiaGrande.color = Color.clear;
        }

        /// <summary>La tecla dibujada junto al punto del centro, con lo que hace.</summary>
        void Tecla(bool ver, string letra, string que)
        {
            if (!teclaGO) return;
            teclaGO.SetActive(ver);
            if (!ver) return;
            teclaLetra.text = letra; teclaTexto.text = que;
        }

        void CrearGuia(Transform padre)
        {
            guiaGrande = Texto(padre.gameObject, 36, new Vector2(0, -330), 1500); guiaGrande.color = Color.clear;
            teclaGO = new GameObject("Tecla", typeof(RectTransform)); teclaGO.transform.SetParent(padre, false);
            ((RectTransform)teclaGO.transform).anchoredPosition = new Vector2(34f, -2f);
            var caja = new GameObject("Caja", typeof(RectTransform)).AddComponent<Image>(); caja.transform.SetParent(teclaGO.transform, false);
            ((RectTransform)caja.transform).sizeDelta = new Vector2(38f, 38f); ((RectTransform)caja.transform).anchoredPosition = new Vector2(19f, 0f);
            caja.color = new Color(0.95f, 0.93f, 0.86f, 0.92f); caja.raycastTarget = false;
            teclaLetra = Texto(teclaGO, 24, new Vector2(19f, 0f), 38); teclaLetra.color = new Color(0.08f, 0.08f, 0.1f);
            teclaLetra.GetComponent<Outline>().enabled = false;
            teclaTexto = Texto(teclaGO, 26, new Vector2(52f + 180f, 0f), 360); teclaTexto.alignment = TextAnchor.MiddleLeft;
            teclaGO.SetActive(false);
        }

        IEnumerator Volumen(AudioSource s, float a, float seg)
        {
            float de = s.volume;
            for (float k = 0f; k < seg && s; k += Time.deltaTime) { s.volume = Mathf.Lerp(de, a, k / seg); yield return null; }
            if (s) { s.volume = a; if (a <= 0f) s.Stop(); }
        }

        IEnumerator Titulo(string texto, Font fuente, int tam, Color color, float seg)
        {
            titulo.text = texto; titulo.font = fuente; titulo.fontSize = tam;
            for (float k = 0f; k < 1.2f; k += Time.deltaTime) { titulo.color = new Color(color.r, color.g, color.b, k / 1.2f); yield return null; }
            yield return new WaitForSeconds(seg - 2.4f);
            for (float k = 0f; k < 1.2f; k += Time.deltaTime) { titulo.color = new Color(color.r, color.g, color.b, 1f - k / 1.2f); yield return null; }
            titulo.text = "";
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator Fundido(float de, float a, float seg)
        {
            for (float k = 0f; k < seg; k += Time.deltaTime) { negro.color = new Color(0, 0, 0, Mathf.Lerp(de, a, k / seg)); yield return null; }
            negro.color = new Color(0, 0, 0, a);
        }

        void ConstruirUI()
        {
            var go = new GameObject("Prologo_UI");
            go.transform.SetParent(transform, false);
            lienzo = go.AddComponent<Canvas>(); lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 100;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            negro = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>();
            negro.transform.SetParent(go.transform, false);
            var rn = (RectTransform)negro.transform; rn.anchorMin = Vector2.zero; rn.anchorMax = Vector2.one; rn.offsetMin = rn.offsetMax = Vector2.zero;
            negro.color = Color.black; negro.raycastTarget = false;
            subtitulo = Texto(go, 34, new Vector2(0, -400), 1500);
            opciones = Texto(go, 30, new Vector2(0, -300), 1500); opciones.color = new Color(0.95f, 0.85f, 0.6f);
            ayuda = Texto(go, 26, new Vector2(0, -470), 1200); ayuda.color = new Color(0.95f, 0.93f, 0.85f, 0.88f);
            titulo = Texto(go, 96, new Vector2(0, 40), 1700); titulo.color = Color.clear; ((RectTransform)titulo.transform).sizeDelta = new Vector2(1700, 600); titulo.GetComponent<Outline>().enabled = false;
            barra = new GameObject("Barra_Eleccion", typeof(RectTransform)).AddComponent<Image>();
            barra.transform.SetParent(go.transform, false); ((RectTransform)barra.transform).anchoredPosition = new Vector2(0, -255);
            barra.raycastTarget = false; barra.gameObject.SetActive(false);
            // punto de mira pequeño
            var p = punto = new GameObject("Punto", typeof(RectTransform)).AddComponent<Image>();
            {   // un círculo de bordes suaves (sin imagen era un cuadrado)
                const int N = 32; var tx = new Texture2D(N, N, TextureFormat.RGBA32, false);
                for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                { float d = new Vector2(x - N / 2f + 0.5f, y - N / 2f + 0.5f).magnitude; tx.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(N / 2f - 1f - d))); }
                tx.Apply(); p.sprite = Sprite.Create(tx, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            }
            p.transform.SetParent(go.transform, false); ((RectTransform)p.transform).sizeDelta = new Vector2(6, 6);
            p.color = new Color(1, 1, 1, 0.5f); p.raycastTarget = false;
            CrearGuia(go.transform);
            negro.transform.SetAsFirstSibling();                     // los subtítulos quedan encima del negro
        }

        Text Texto(GameObject padre, int tam, Vector2 pos, float ancho)
        {
            var t = new GameObject("Txt", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(padre.transform, false);
            var r = (RectTransform)t.transform; r.sizeDelta = new Vector2(ancho, tam * 3f); r.anchoredPosition = pos;
            t.font = Fuentes.Maquina; t.fontSize = tam; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            t.supportRichText = true; t.raycastTarget = false;
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.8f); o.effectDistance = new Vector2(2, -2);
            return t;
        }
    }
}
