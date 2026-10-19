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
        public Vector3 adelante = Vector3.forward;
        public float velocidad = 11f;          // ~40 km/h con lluvia
        public Transform[] limpiabrisas;       // pivotes de las plumillas
        public Light[] lucesCamion;
        public Transform camion;
        public Transform carroTrafico;         // un carro que pasa en sentido contrario (se clona)
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
        Canvas lienzo; Text subtitulo, ayuda, opciones; Image negro;
        readonly Queue<(string quien, string que, float seg)> fila = new Queue<(string, string, float)>();
        bool hablando;

        public bool Termino => terminando;
        public IReadOnlyCollection<string> Tocados => tocados;

        void Awake()
        {
            Instancia = this;
            ConstruirUI();
            rot0 = ojos.localRotation; ojos0 = ojos.localPosition;
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
        }

        // ------------------------------------------------------------------ guion
        IEnumerator Guion()
        {
            negro.color = Color.black;
            yield return Fundido(1f, 0f, 2.5f);                                        // entrada: abrir los ojos en el carro
            Decir("Pa", "Mire cómo está lloviendo… Bogotá entera salió en carro hoy.", 4f);
            yield return new WaitForSeconds(4.5f);
            ayuda.text = "Mueve el mouse para mirar.   [E] tocar";
            yield return new WaitForSeconds(6f);

            // el diálogo llega después de tocar 2 cosas (o a los 40 s)
            float t0 = Time.time;
            yield return new WaitUntil(() => tocados.Count >= 2 || Time.time - t0 > 40f);
            yield return new WaitUntil(() => !hablando);
            yield return new WaitForSeconds(1f);
            Decir("Pa", "El bus es más cómodo, ya sé. Pero hoy cumples dieciséis y te traigo yo.", 4.5f);
            yield return new WaitForSeconds(4f);
            eligiendo = true;
            opciones.text = "[1]  Gracias, Pa.          [2]  Igual iba a llegar tarde.";
            float te = Time.time;
            yield return new WaitUntil(() => eligio || Time.time - te > 12f);
            eligiendo = false; opciones.text = "";
            if (eleccion == 2) { Decir("Mateo", "Igual iba a llegar tarde.", 2f); Decir("Pa", "Ja. Pues llegas tarde, pero llegas con tu papá.", 3.5f); }
            else { Decir("Mateo", "Gracias, Pa.", 2f); Decir("Pa", "Dieciséis… ¿En qué momento creciste, mijo?", 3.5f); }

            // tiempo para tocar lo que falte (hasta 4 cosas o 60 s más)
            float t1 = Time.time;
            yield return new WaitUntil(() => tocados.Count >= 4 || Time.time - t1 > 60f);
            yield return new WaitUntil(() => !hablando);
            yield return new WaitForSeconds(1.5f);
            yield return Presagio();
        }

        IEnumerator Presagio()
        {
            terminando = true; ayuda.text = ""; mirando = null;
            StartCoroutine(MirarAlFrente(1.4f));                    // Mateo levanta la vista: el jugador ve venir lo que viene
            Decir("Mateo", radioFuente ? "Pa, cambie esa canción, ¿sí?" : "Pa, ponga algo de música, ¿sí?", 2.5f);
            yield return new WaitForSeconds(2.2f);
            Decir("Pa", "Ya va, ya va… a ver…", 2.2f);
            SonidoCorto.Juego("prologo/radio_sintonia", radio.transform.position, 0.6f, 0.4f);
            if (radioFuente) radioFuente.Stop();
            yield return new WaitForSeconds(1.6f);
            // durante: luces de frente. Un camión invade el carril.
            if (camion)
            {
                camion.gameObject.SetActive(true);
                foreach (var l in lucesCamion) if (l) l.enabled = true;
                // viene en su carril como cualquier carro (85 km/h de acercamiento); a ~38 m se le va el carril
                Vector3 lado = Vector3.Cross(Vector3.up, adelante);
                float dist = 120f, desvio = -3.4f, vCarro = velocidad;
                bool frenando = false;
                while (dist > 4.5f)
                {
                    if (dist < 38f) desvio = Mathf.MoveTowards(desvio, 0.2f, 3.2f * Time.deltaTime);
                    if (dist < 22f && !frenazo)
                    {
                        frenazo = true; frenando = true;
                        Decir("Mateo", "¡PA!", 1.2f);
                        SonidoCorto.Juego("prologo/choque", ojos.position, 1f, 0f);
                    }
                    if (frenando) vCarro = Mathf.MoveTowards(vCarro, 2f, 9f * Time.deltaTime);
                    velocidad = vCarro;                                  // la calle frena con el carro
                    dist -= (vCarro + 12.5f) * Time.deltaTime;
                    camion.position = ojos.position + adelante * dist + lado * desvio + Vector3.down * 1.2f;
                    bool cambiando = desvio > -3.3f && desvio < 0.1f;
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
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.loop) a.Stop();                                                 // silencio de golpe

            // salida: negro, monitor de hospital, una voz lejana y el monitor se vuelve campana
            yield return new WaitForSeconds(2.5f);
            var monitor = SonidoCorto.Play(SonidoCorto.Clip("prologo/monitor"), ojos.position, 0.45f, 12f, 0f, true);
            yield return new WaitForSeconds(3f);
            Decir("", "…respira conmigo, mijo. Cuatro… sostén…", 4.5f, lejano: true);
            yield return new WaitForSeconds(6f);
            if (monitor) monitor.volume = 0.15f;
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
            if (g == cuaderno)
            {
                SonidoCorto.Juego("objetos/cuaderno_pagina", g.transform.position, 0.6f, 0.3f);
                Decir("Mateo", "(Ejercicios pág. 142. Examen el viernes… y no he hecho ni uno.)", 4f);
            }
            else if (g == lonchera)
            {
                SonidoCorto.Juego("objetos/objeto_tomar", g.transform.position, 0.6f, 0.3f);
                Decir("Mateo", "¿Y este candado?", 1.8f);
                Decir("Pa", "Te puse candado para que no te la roben. La clave es tu ruta, la 46. Cero, cuatro, seis.", 5.5f);
            }
            else if (g == radio)
            {
                if (radioFuente == null)
                {
                    SonidoCorto.Juego("prologo/radio_sintonia", g.transform.position, 0.5f, 0.4f, 1.4f);
                    var clip = Resources.Load<AudioClip>("Prologo/radio_cancion");
                    radioFuente = SonidoCorto.Play(clip, g.transform.position, 0.35f, 9999f, 0.5f, true);
                    Decir("Pa", "♪ Tara-rará… lalalá la lluviaaa… ♪ (desafina a propósito)", 3.5f);
                    Decir("Mateo", "(Se ríe.) Pa, usted canta horrible.", 2.5f);
                    Decir("Pa", "Horrible pero con sentimiento.", 2.5f);
                }
                else { Decir("Mateo", "(Mejor la dejo. A Pa le gusta.)", 2.5f); }
            }
            else if (g == celular)
            {
                SonidoCorto.Juego("sustos/celular_vibra", g.transform.position, 0.35f, 0.3f, 1.2f);
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
            pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -45f, 70f);
            ojos.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
        }
        public GameObject Mirando => mirando;
        public string LoQueVe() => Physics.Raycast(ojos.position, ojos.forward, out var h, 3f) ? $"{h.collider.name} a {h.distance:F2} m" : "nada";

        public void Elegir(int n) { if (eligiendo) { eleccion = n; eligio = true; } }

        // ------------------------------------------------------------------ cada cuadro
        void Update()
        {
            // la calle pasa: el carro está quieto y el mundo se mueve hacia atrás (dos tramos que se turnan)
            foreach (var c in calles)
                {
                    c.position -= adelante * velocidad * Time.deltaTime;
                    if (Vector3.Dot(c.position - ojos.position, adelante) < -largoCalle - 40f) c.position += adelante * largoCalle * calles.Length;
                }
            // tráfico normal en sentido contrario: así el camión del final no sale de la nada
            if (carroTrafico && Time.timeSinceLevelLoad > proximoCarro && !terminando)
            {
                proximoCarro = Time.timeSinceLevelLoad + Random.Range(7f, 14f);
                var t = Instantiate(carroTrafico, carroTrafico.parent); t.gameObject.SetActive(true);
                t.position = ojos.position + adelante * 160f - Vector3.Cross(Vector3.up, adelante) * 3.4f + Vector3.down * 1.2f;
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
            if (!terminando) ojos.localPosition = ojos0 + new Vector3(0f, Mathf.PerlinNoise(Time.time * 2.2f, 0f) - 0.5f, 0f) * 0.008f;

            Mirar();
            Apuntar();
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
            if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * 90f * Time.deltaTime;
            yaw = Mathf.Clamp(yaw + d.x, -110f, 95f);           // no se puede girar del todo: va sentado con cinturón
            pitch = Mathf.Clamp(pitch - d.y, -45f, 70f);           // se puede mirar las piernas (sin ver el cuello por dentro)
            ojos.localRotation = rot0 * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
        }

        void Apuntar()
        {
            mirando = null;
            if (terminando) { ayuda.text = ""; return; }
            var cam = ojos.GetComponent<Camera>();
            Ray r = new Ray(ojos.position, ojos.forward);
            foreach (var h in Physics.RaycastAll(r, 1.4f).OrderBy(h => h.distance))
            {
                var g = h.collider.gameObject;
                while (g && g != cuaderno && g != lonchera && g != radio && g != celular) g = g.transform.parent ? g.transform.parent.gameObject : null;
                if (g) { mirando = g; break; }
            }
            if (Time.timeSinceLevelLoad < 9f) return;
            ayuda.text = mirando == null ? "" :
                "[E]  " + (mirando == cuaderno ? "Cuaderno" : mirando == lonchera ? "Lonchera" : mirando == radio ? "Radio" : "Celular");
        }

        IEnumerator Limpiabrisas()
        {
            if (limpiabrisas == null || limpiabrisas.Length == 0) yield break;
            var ini = limpiabrisas.Select(l => l.localRotation).ToArray();
            while (true)
            {
                SonidoCorto.Juego("prologo/limpiabrisas", limpiabrisas[0].position, 0.35f, 0.4f);
                for (float k = 0f; k < 1.6f; k += Time.deltaTime)
                {
                    float a = Mathf.Sin(Mathf.PI * k / 1.6f) * 95f;                       // va y vuelve
                    for (int i = 0; i < limpiabrisas.Length; i++) limpiabrisas[i].localRotation = ini[i] * Quaternion.Euler(0f, 0f, -a);
                    yield return null;
                }
                yield return new WaitForSeconds(terminando ? 99f : 1.2f);
            }
        }

        // ------------------------------------------------------------------ subtítulos
        void Decir(string quien, string que, float seg, bool lejano = false)
        {
            fila.Enqueue((quien, lejano ? "<i>" + que + "</i>" : que, seg));
            if (!hablando) StartCoroutine(Hablar());
        }

        IEnumerator Hablar()
        {
            hablando = true;
            while (fila.Count > 0)
            {
                var (quien, que, seg) = fila.Dequeue();
                subtitulo.text = string.IsNullOrEmpty(quien) ? que : $"<color=#E8B04A>{quien}:</color>  {que}";
                for (float a = 0f; a < 1f; a += Time.deltaTime * 5f) { subtitulo.color = new Color(1, 1, 1, a); yield return null; }
                subtitulo.color = Color.white;
                yield return new WaitForSeconds(seg);
                for (float a = 1f; a > 0f; a -= Time.deltaTime * 5f) { subtitulo.color = new Color(1, 1, 1, a); yield return null; }
                subtitulo.text = "";
                yield return new WaitForSeconds(0.25f);
            }
            hablando = false;
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
            ayuda = Texto(go, 22, new Vector2(0, -470), 1200); ayuda.color = new Color(0.95f, 0.93f, 0.85f, 0.6f);
            // punto de mira pequeño
            var p = new GameObject("Punto", typeof(RectTransform)).AddComponent<Image>();
            p.transform.SetParent(go.transform, false); ((RectTransform)p.transform).sizeDelta = new Vector2(6, 6);
            p.color = new Color(1, 1, 1, 0.5f); p.raycastTarget = false;
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
