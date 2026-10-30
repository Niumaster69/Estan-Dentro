using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// El aula de música: un recuerdo bonito con Pa y el final del capítulo 1 (Henry 20-oct).
    /// Al entrar, la sala se entibia, suena la cumbia de la radio del prólogo y Pa canta (mal) como el Día de la familia;
    /// Mateo lo recuerda. Luego todo se pone blanco y sale el cierre del capítulo; vuelve al menú.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RecuerdoAula : MonoBehaviour
    {
        public Vector3 puntoPa;                 // donde "estaba" Pa (frente al tablero del aula)
        [Tooltip("Final de NARRATIVA_UNIFICADA §2.3: el aula se vuelve el cuarto del hospital y Mateo respira con Pa. Apagado = cierre anterior.")]
        public bool finalHospital = true;
        bool hecho;
        Image blanco; Text cierre;

        void Awake() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider c)
        {
            if (hecho || !c.GetComponentInParent<Player.PlayerController>()) return;
            hecho = true;
            StartCoroutine(Recordar(c.GetComponentInParent<Player.PlayerController>()));
        }

        IEnumerator Recordar(Player.PlayerController j)
        {
            // si llega huyendo de la Pesadilla, el aula la deshace: el recuerdo bonito empieza cuando todo vuelve
            if (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal)
            {
                Mente.ControlMente.Resolver("aula_musica", "…Aquí no entra. Aquí no.");
                Mente.ControlMente.Soltar();
                if (Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal && !Mente.EstadoMente.EnTransicion) Mente.EstadoMente.Ir(Mente.EstadoMente.Estado.Normal);
                float t0 = 0f;
                while ((Mente.EstadoMente.Actual != Mente.EstadoMente.Estado.Normal || Mente.EstadoMente.EnTransicion) && t0 < 8f) { t0 += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(1f);
            }
            Stress.StressSystem.Instance?.ResetTo(0f);
            ObjectiveHUD.Notify("…Esta es el aula de música.", 3f);
            // la sala se entibia: una luz cálida donde estaba Pa
            var luzGo = new GameObject("Luz_Recuerdo"); luzGo.transform.position = puntoPa + Vector3.up * 1.6f;
            var luz = luzGo.AddComponent<Light>(); luz.type = LightType.Point; luz.color = new Color(1f, 0.78f, 0.5f); luz.range = 9f;
            luz.lightUnit = UnityEngine.Rendering.LightUnit.Lumen; luz.intensity = 0f;
            var musica = Sonar(Resources.Load<AudioClip>("Prologo/radio_cancion"), puntoPa, 0f, true);
            if (musica) musica.time = 97.4f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                luz.intensity = Mathf.Lerp(0f, 2600f, t / 4f);
                if (musica) musica.volume = Mathf.Lerp(0f, 0.18f, t / 4f);
                yield return null;
            }
            ObjectiveHUD.Notify("El Día de la familia. Pa se subió aquí, con una guitarra prestada…", 4.5f);
            yield return new WaitForSeconds(2.5f);
            var canto = Resources.Load<AudioClip>("Voces/Prologo/pa_canta_trompeta");
            if (canto) EstanDentro.Audio.Voz.Sonar(canto, puntoPa, EstanDentro.Audio.Voz.Lugar.Recuerdo, 0.9f, agachaMusica: false);
            yield return new WaitForSeconds(canto ? canto.length + 0.5f : 4f);
            ObjectiveHUD.Notify("…y cantó delante de todo el curso. Horrible.", 3.5f);
            yield return new WaitForSeconds(3.2f);
            var frase = Resources.Load<AudioClip>("Voces/Prologo/horrible_pero_con_sentimiento");
            if (frase) EstanDentro.Audio.Voz.Sonar(frase, puntoPa, EstanDentro.Audio.Voz.Lugar.Recuerdo);
            ObjectiveHUD.Notify("«Horrible, pero con sentimiento».", 3f);
            yield return new WaitForSeconds(frase ? frase.length + 1f : 3f);
            ObjectiveHUD.Notify("Yo me quería esconder debajo del pupitre… y al final canté con él.", 4.5f);
            yield return new WaitForSeconds(4.8f);
            ObjectiveHUD.Notify("…Pa. Quiero volver.", 3.5f);
            yield return new WaitForSeconds(2.5f);
            if (finalHospital)
            {
                yield return Hospital(j, musica);
                yield break;
            }
            // todo se pone blanco: fin del capítulo
            if (j) j.InputEnabled = false;
            Construir();
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                blanco.color = new Color(1f, 0.97f, 0.9f, t / 3f);
                if (musica) musica.volume = Mathf.Lerp(0.18f, 0.3f, t / 3f);
                yield return null;
            }
            cierre.text = "ESTÁN DENTRO\n\n<size=40>Fin del capítulo 1</size>\n\n<size=30>Respira. Alguien te está esperando.</size>";
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { cierre.color = new Color(0.15f, 0.12f, 0.1f, t / 1.5f); yield return null; }
            yield return new WaitForSeconds(6f);
            for (float t = 0f; t < 2f; t += Time.deltaTime) { if (musica) musica.volume = Mathf.Lerp(0.3f, 0f, t / 2f); yield return null; }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            EstanDentro.UI.Pausa.Reiniciar();
            SceneManager.LoadScene("MainMenu");
        }

        // ---------------- Final del hospital (NARRATIVA_UNIFICADA §2.3) ----------------
        // El aula se apaga en un cuarto de hospital de noche: solo el pitido del monitor y la voz de Pa desde "afuera".
        // Los cinco textos del giro y la última respiración con Pa (la misma mecánica de Q que se practicó en todo el
        // capítulo; el monitor se calma con cada ciclo bien hecho). Después: título, gancho del capítulo 2 y el resumen
        // "Lo que aprendiste" con el rango de calma.

        Image fondo, banda; Text linea, aviso;
        float intervaloMonitor = 0.8f;
        bool monitorVivo;

        /// <summary>El cuarto 3D del hospital (lo arma Editor/Mejoras/MejorasEscena.CuartoHospital, apagado). Si no está, el final va sobre negro.</summary>
        static GameObject BuscarCuarto()
        {
            foreach (var g in SceneManager.GetActiveScene().GetRootGameObjects()) if (g.name == "Mejoras_CuartoHospital") return g;
            return null;
        }

        IEnumerator Hospital(Player.PlayerController j, AudioSource musica)
        {
            if (j) j.InputEnabled = false;
            ConstruirFinal();
            monitorVivo = true;
            StartCoroutine(Monitor());
            var cuarto = BuscarCuarto();
            if (cuarto)
            {
                // un destello blanco y el aula de música ya es el cuarto del hospital
                for (float t = 0f; t < 1.2f; t += Time.deltaTime)
                {
                    fondo.color = new Color(1f, 0.98f, 0.94f, t / 1.2f);
                    if (musica) musica.volume = Mathf.Lerp(0.18f, 0f, t / 1.2f);
                    yield return null;
                }
                if (musica) Destroy(musica.gameObject);
                var luzRecuerdo = GameObject.Find("Luz_Recuerdo"); if (luzRecuerdo) Destroy(luzRecuerdo);
                // la escena se ve limpia: sin la barra de estrés, sin las manos del HUD (capas 100 y 120) ni lo que Mateo tiene en la mano
                foreach (var mv in FindObjectsByType<Player.ManosVisibles>(FindObjectsSortMode.None)) mv.enabled = false;
                var manos = GameObject.Find("Manos_Objetos"); if (manos) manos.SetActive(false);
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (c.sortingOrder == 100 || c.sortingOrder == 120) c.enabled = false;
                ApagarAula(cuarto.transform, j);
                cuarto.SetActive(true);
                var vista = cuarto.transform.Find("Vista_Final");
                if (j && vista) { j.Teletransportar(vista.position); j.transform.rotation = vista.rotation; j.SetPitch(9f); }
                yield return new WaitForSeconds(0.6f);
                for (float t = 0f; t < 3f; t += Time.deltaTime) { fondo.color = new Color(1f, 0.98f, 0.94f, 1f - t / 3f); yield return null; }
                fondo.color = Color.clear;
                banda.color = Color.white;                                   // los textos van abajo, sobre la escena
                linea.rectTransform.anchoredPosition = new Vector2(0f, -330f);
                aviso.rectTransform.anchoredPosition = new Vector2(0f, -410f);
                yield return new WaitForSeconds(2.5f);                       // dejar mirar el cuarto antes del primer texto
            }
            else
            {
                for (float t = 0f; t < 3f; t += Time.deltaTime)
                {
                    fondo.color = new Color(0.05f, 0.04f, 0.035f, t / 3f);
                    if (musica) musica.volume = Mathf.Lerp(0.18f, 0f, t / 3f);
                    yield return null;
                }
                if (musica) Destroy(musica.gameObject);
                yield return new WaitForSeconds(1.2f);
            }

            string[] giro = {
                "Diecisiete de mayo. Tu cumpleaños.",
                "Ese día no murió nadie.",
                "Pa está vivo. Te visita todos los días.",
                "El que no ha vuelto eres tú.",
                "Tres años dormido."
            };
            foreach (var s in giro) yield return Linea(s, 2.8f);

            // la última respiración con Pa
            var cam = Camera.main;
            var voz = Resources.Load<AudioClip>("Voces/Respirar/pa_respira_conmigo");
            if (voz) EstanDentro.Audio.Voz.Sonar(voz, cam ? cam.transform.position : transform.position, EstanDentro.Audio.Voz.Lugar.Lejos, 1f);
            Concentrarse.YaInvito();
            linea.text = "<color=#d9b45a>Pa:</color> Respira conmigo, mijo.";
            yield return Fundir(linea, 0f, 1f, 1f);
            aviso.text = "Mantén Q y respira con él";
            int base0 = Concentrarse.CiclosBuenos;
            float espera = 0f;
            while (Concentrarse.CiclosBuenos - base0 < 2 && espera < 60f)
            {
                espera += Time.deltaTime;
                aviso.color = new Color(0.93f, 0.89f, 0.82f, Concentrarse.Respirando ? Mathf.Max(0f, aviso.color.a - Time.deltaTime) : 0.6f + 0.3f * Mathf.Sin(Time.time * 2.5f));
                intervaloMonitor = Mathf.Lerp(0.8f, 1.25f, (Concentrarse.CiclosBuenos - base0) / 2f);   // el corazón se calma
                yield return null;
            }
            aviso.color = Color.clear;
            yield return Fundir(linea, 1f, 0f, 1.2f);
            yield return new WaitForSeconds(1.5f);

            // el monitor se acelera y la imagen se rompe
            for (float t = 0f; t < 2.5f; t += Time.deltaTime) { intervaloMonitor = Mathf.Lerp(1.2f, 0.18f, t / 2.5f); yield return null; }
            monitorVivo = false;
            var golpe = Resources.Load<AudioClip>("Sonidos/golpe");
            if (golpe && cam) AudioSource.PlayClipAtPoint(golpe, cam.transform.position, 1f);
            fondo.color = new Color(0.02f, 0f, 0f, 1f);
            banda.color = Color.clear;
            linea.rectTransform.anchoredPosition = Vector2.zero;
            aviso.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            linea.font = Fuentes.Titulo ? Fuentes.Titulo : linea.font; linea.fontSize = 120;
            linea.text = "<color=#c23a2e>ESTÁN DENTRO</color>";
            linea.color = Color.white;
            yield return new WaitForSeconds(2.2f);
            aviso.text = "Algo no quiere que despiertes.\n\n<size=26>Capítulo 2: Los pasillos</size>";
            yield return Fundir(aviso, 0f, 1f, 1.2f);
            yield return new WaitForSeconds(3.5f);
            yield return Fundir(linea, 1f, 0f, 1f);
            aviso.color = Color.clear;

            yield return LoQueAprendiste();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            EstanDentro.UI.Pausa.Reiniciar();
            SceneManager.LoadScene("MainMenu");
        }

        IEnumerator Monitor()
        {
            var pitido = Resources.Load<AudioClip>("Sonidos/monitor_pitido");
            var g = new GameObject("Monitor_Hospital"); DontDestroyOnLoad(g);
            var a = g.AddComponent<AudioSource>(); a.clip = pitido; a.spatialBlend = 0f; a.volume = 0.35f;
            g.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Room;
            while (monitorVivo)
            {
                MonitorECG.Intervalo = intervaloMonitor;                       // la línea del pulso va con el pitido
                if (pitido) a.Play();
                yield return new WaitForSeconds(intervaloMonitor);
            }
            Destroy(g, 1f);
        }

        /// <summary>Esconde los muebles del aula de música (no las paredes ni el piso) para que quede solo el cuarto del hospital.</summary>
        static void ApagarAula(Transform cuarto, Player.PlayerController j)
        {
            var caja = new Bounds(new Vector3(3.6f, cuarto.position.y + 1.3f, 23.5f), new Vector3(6.4f, 2.6f, 10f));
            int n = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r.transform.IsChildOf(cuarto) || (j && r.transform.IsChildOf(j.transform))) continue;
                var b = r.bounds;
                if (!caja.Contains(b.center) || b.size.x > 3.5f || b.size.z > 3.5f || b.size.y > 2.4f) continue;   // paredes, piso, techo
                r.enabled = false; n++;
            }
            Debug.Log($"[Final] aula de música → hospital: {n} muebles escondidos");
        }

        IEnumerator Linea(string s, float dura)
        {
            linea.text = s;
            yield return Fundir(linea, 0f, 1f, 1f);
            yield return new WaitForSeconds(dura);
            yield return Fundir(linea, 1f, 0f, 0.8f);
            yield return new WaitForSeconds(0.4f);
        }

        static IEnumerator Fundir(Text tx, float de, float a, float dura)
        {
            var c = tx.color;
            for (float t = 0f; t < dura; t += Time.deltaTime) { tx.color = new Color(c.r, c.g, c.b, Mathf.Lerp(de, a, t / dura)); yield return null; }
            tx.color = new Color(c.r, c.g, c.b, a);
        }

        /// <summary>Resumen del capítulo: rango de calma, lo que se practicó y a quién llamar. Sigue con una tecla.</summary>
        IEnumerator LoQueAprendiste()
        {
            int buenos = Concentrarse.CiclosBuenos, total = Mathf.Max(Concentrarse.CiclosTotales, buenos);
            float precision = total > 0 ? (float)buenos / total : 0f;
            bool colapso = Network.GameSession.StressCollapsed;
            string rango = total == 0 ? "C" : precision >= 0.8f && total >= 4 && !colapso ? "S" : precision >= 0.6f && !colapso ? "A" : precision >= 0.4f || !colapso ? "B" : "C";
            fondo.color = new Color(0.05f, 0.045f, 0.04f, 1f);
            linea.font = Fuentes.Maquina ? Fuentes.Maquina : linea.font; linea.fontSize = 34;
            linea.alignment = TextAnchor.MiddleCenter;
            linea.text =
                "<size=26><color=#d9b45a>LO QUE APRENDISTE</color></size>\n\n" +
                $"<size=110><color=#a9cfd3>{rango}</color></size>\n<size=24>rango de calma</size>\n\n" +
                (total == 0 ? "Esta vez no respiraste con Pa. La próxima, mantén Q al ritmo de su voz.\n\n"
                            : $"Respiraste con Pa {total} {(total == 1 ? "vez" : "veces")}; {buenos} a tiempo.\n\n") +
                "<size=28>Inhala 4 · sostén 4 · exhala 6.\nSoltar el aire más despacio de lo que entra ayuda a calmar el cuerpo.\nFunciona también fuera del juego.</size>\n\n" +
                "<size=28>Mateo creyó algo que no era verdad. La culpa no siempre dice la verdad.</size>\n\n" +
                "<size=26><color=#d9b45a>Si sientes que no puedes con algo, habla con alguien.\nLínea 192, opción 4 · Bogotá: 106</color></size>";
            yield return Fundir(linea, 0f, 1f, 1.5f);
            aviso.rectTransform.anchoredPosition = new Vector2(0f, -470f);
            aviso.fontSize = 22; aviso.text = "Pulsa una tecla para volver al menú";
            yield return Fundir(aviso, 0f, 0.7f, 1f);
            float t = 0f;
            yield return new WaitForSeconds(1.5f);
            while (t < 40f)
            {
                t += Time.deltaTime;
                var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
                if ((kb != null && kb.anyKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame)) break;
                yield return null;
            }
        }

        void ConstruirFinal()
        {
            var go = new GameObject("Final_Hospital");
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 170;   // tapa el HUD (100) y las manos (120)
            Concentrarse.GuiaEncima(true);                                                                              // la guía de respirar queda encima (172)
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            fondo = new GameObject("Fondo", typeof(RectTransform)).AddComponent<Image>(); fondo.transform.SetParent(go.transform, false);
            var r = (RectTransform)fondo.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            fondo.color = Color.clear; fondo.raycastTarget = false;
            // banda oscura abajo: los textos del giro se leen sobre el cuarto del hospital
            banda = new GameObject("Banda", typeof(RectTransform)).AddComponent<Image>(); banda.transform.SetParent(go.transform, false);
            var rb = (RectTransform)banda.transform; rb.anchorMin = Vector2.zero; rb.anchorMax = new Vector2(1f, 0f); rb.pivot = new Vector2(0.5f, 0f);
            rb.sizeDelta = new Vector2(0f, 420f); rb.anchoredPosition = Vector2.zero;
            var gt = new Texture2D(1, 64) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) gt.SetPixel(0, y, new Color(0.02f, 0.015f, 0.01f, Mathf.SmoothStep(0.85f, 0f, y / 63f)));
            gt.Apply();
            banda.sprite = Sprite.Create(gt, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
            banda.color = Color.clear; banda.raycastTarget = false;
            linea = TextoFinal(go.transform, 46, Vector2.zero, new Vector2(1600f, 900f));
            aviso = TextoFinal(go.transform, 30, new Vector2(0f, -170f), new Vector2(1400f, 200f));
        }

        static Text TextoFinal(Transform padre, int tam, Vector2 pos, Vector2 tamano)
        {
            var tx = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>(); tx.transform.SetParent(padre, false);
            tx.font = Fuentes.Maquina ? Fuentes.Maquina : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.fontSize = tam; tx.alignment = TextAnchor.MiddleCenter; tx.supportRichText = true; tx.raycastTarget = false;
            tx.color = new Color(0.93f, 0.89f, 0.82f, 0f);
            tx.horizontalOverflow = HorizontalWrapMode.Wrap; tx.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = (RectTransform)tx.transform; rt.sizeDelta = tamano; rt.anchoredPosition = pos;
            return tx;
        }

        static AudioSource Sonar(AudioClip clip, Vector3 pos, float vol, bool bucle)
        {
            if (!clip) return null;
            var g = new GameObject("Musica_Recuerdo"); g.transform.position = pos;
            var a = g.AddComponent<AudioSource>(); a.clip = clip; a.volume = vol; a.loop = bucle; a.spatialBlend = 0.6f; a.maxDistance = 14f;
            var lp = g.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = 3200f;                       // como si sonara lejos, en la memoria
            g.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Room;
            a.Play();
            return a;
        }

        void Construir()
        {
            var go = new GameObject("Cierre_Capitulo1");
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 400;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            blanco = new GameObject("Blanco", typeof(RectTransform)).AddComponent<Image>(); blanco.transform.SetParent(go.transform, false);
            var r = (RectTransform)blanco.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            blanco.color = new Color(1, 1, 1, 0);
            cierre = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>(); cierre.transform.SetParent(go.transform, false);
            cierre.font = Fuentes.Titulo ? Fuentes.Titulo : Fuentes.Maquina; cierre.fontSize = 96; cierre.alignment = TextAnchor.MiddleCenter;
            cierre.supportRichText = true; cierre.color = new Color(0, 0, 0, 0);
            var rt = (RectTransform)cierre.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
