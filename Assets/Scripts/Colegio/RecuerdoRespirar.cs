using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// El primer recuerdo, guiado: enseña a respirar con Pa antes de necesitarlo (POR_HACER: "primer recuerdo guiado en el
    /// 4°B que enseñe a respirar, antes de la rectoría"). Llega con el apagón del 4°B: en lo oscuro Mateo se acuerda de la
    /// noche de la tormenta en el carro, cuando Pa le enseñó a contar el aire. Mateo se queda quieto, Pa cuenta y la guía
    /// de Concentrarse (Q) aparece; con un ciclo bien hecho el recuerdo se cierra, el estrés baja y la técnica queda en
    /// PISTAS. Si no le sale, Pa se lo explica de nuevo; al tercer intento sigue igual (nunca se queda trabado).
    /// </summary>
    public class RecuerdoRespirar : MonoBehaviour
    {
        public static bool Hecho { get; private set; }
        public static bool Activo { get; private set; }
        /// <summary>Pa ya explicó y espera que Mateo respire (la prueba automática mantiene Q desde aquí).</summary>
        public static bool EsperandoQ { get; private set; }

        Canvas lienzo; Image calido; Text linea, aviso;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Instalar()
        {
            SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
            AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void AlCargar(Scene s, LoadSceneMode modo)
        {
            if (s.name == "MainMenu") { Hecho = false; Activo = false; EsperandoQ = false; return; }
            if (s.name != "Colegio" || Hecho || Exploracion.Activo) return;
            new GameObject("__RecuerdoRespirar").AddComponent<RecuerdoRespirar>();
        }

        IEnumerator Start()
        {
            // espera el apagón (lo dispara la lonchera)
            Interaction.BlackoutEvent b = null;
            while (!(b = Interaction.BlackoutEvent.Instance) || !b.IsBlackedOut) yield return null;
            // lo oscuro primero: unos segundos y que no haya ninguna ventana abierta (la tarjeta de la linterna, la mochila…)
            yield return new WaitForSeconds(3f);
            float libre = 0f;
            while (libre < 1.5f) { libre = Libre() ? libre + Time.unscaledDeltaTime : 0f; yield return null; }
            yield return Recordar();
        }

        static bool Libre() => !OverlayBlocker.IsBlocking && Time.timeScale > 0f && !TableroInteractivo.Mirando
                               && !Inventory.InventoryOverlay.EstaAbierto && !Inventory.MochilaOverlay.EstaAbierta && Escondite.Actual == null;

        IEnumerator Recordar()
        {
            Activo = true;
            var j = FindAnyObjectByType<Player.PlayerController>();
            if (j) j.InputEnabled = false;
            Construir();
            var st = Stress.StressSystem.Instance;
            if (st && st.Normalized < 0.45f) st.AddSilencioso((0.45f - st.Normalized) * 100f);   // el miedo del apagón, para sentir cómo baja

            yield return Fundir(0f, 1f, 1.5f);
            yield return Decir("Así de oscuro estaba la noche de la tormenta. En el carro, con Pa.", 3.5f);
            yield return Decir("Yo tenía siete años y no podía respirar del susto.", 3.2f);
            var voz = Resources.Load<AudioClip>("Voces/Respirar/pa_respira_conmigo");
            var cam = Camera.main;
            if (voz) Audio.Voz.Sonar(voz, cam ? cam.transform.position : transform.position, Audio.Voz.Lugar.Recuerdo, 0.9f);
            Concentrarse.YaInvito();
            yield return Decir("<color=#d9b45a>Pa:</color> Respira conmigo, mijo. Cuatro para adentro, cuatro lo guardas… y seis para afuera, despacio.", 5.5f, mantener: true);

            GuiaTeclas.PrimeraVez("Q", "Respirar con Pa");
            int intentos = 0, buenos0 = Concentrarse.CiclosBuenos, totales0 = Concentrarse.CiclosTotales;
            aviso.text = "Mantén <b>Q</b> en INHALA y SOSTÉN · suéltala en EXHALA";
            float espera = 0f; EsperandoQ = true;
            while (Concentrarse.CiclosBuenos == buenos0 && intentos < 3 && espera < 90f)
            {
                espera += Time.deltaTime;
                // la guía de Concentrarse tapa el centro: el aviso late abajo solo mientras no se respira
                aviso.color = new Color(0.93f, 0.89f, 0.82f, Concentrarse.Respirando ? 0.35f : 0.6f + 0.3f * Mathf.Sin(Time.time * 2.5f));
                if (Concentrarse.CiclosTotales > totales0)
                {
                    totales0 = Concentrarse.CiclosTotales; intentos++;
                    if (Concentrarse.CiclosBuenos == buenos0 && intentos < 3)
                        linea.text = "<color=#d9b45a>Pa:</color> Otra vez, sin afán. Aprieta mientras cuento hasta ocho… y suelta el aire en seis.";
                }
                yield return null;
            }
            EsperandoQ = false;
            bool aprendio = Concentrarse.CiclosBuenos > buenos0;
            aviso.text = "";
            yield return Decir(aprendio ? "<color=#d9b45a>Pa:</color> Eso. ¿Sí ves? El aire que sale despacio le dice al cuerpo que ya pasó."
                                        : "<color=#d9b45a>Pa:</color> No importa si sale torcido. Lo importante es parar y contar.", 4.5f);
            yield return Decir("Cuando todo se rompa… paro y respiro con él.", 3f);
            // espera a que termine la respiración que esté en curso (si sigue con Q)
            float tq = 0f; while (Concentrarse.Respirando && tq < 16f) { tq += Time.deltaTime; yield return null; }
            yield return Fundir(1f, 0f, 1.2f);
            if (st) st.AddSilencioso(-25f);
            Inventory.Inventory.Instance?.RegisterNote("Respirar con Pa", "Pa me lo enseñó la noche de la tormenta:\n\nINHALA 4 — mantén Q\nSOSTÉN 4 — sigue con Q\nEXHALA 6 — suelta, despacio\n\nSoltar el aire más despacio de lo que entra calma el cuerpo. Cuando todo se rompa, paro y respiro.");
            if (j) j.InputEnabled = true;
            Destroy(lienzo.gameObject);
            Hecho = true; Activo = false;
            ObjectiveHUD.Notify("Lo anoté en el cuaderno (Pistas).", 2.5f);
            Destroy(gameObject);
        }

        IEnumerator Decir(string s, float dura, bool mantener = false)
        {
            linea.text = s;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { linea.color = new Color(1f, 1f, 1f, t / 0.6f); yield return null; }
            linea.color = Color.white;
            yield return new WaitForSeconds(dura);
            if (mantener) yield break;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { linea.color = new Color(1f, 1f, 1f, 1f - t / 0.5f); yield return null; }
            linea.color = Color.clear;
        }

        IEnumerator Fundir(float de, float a, float dura)
        {
            for (float t = 0f; t < dura; t += Time.deltaTime) { calido.color = new Color(0.32f, 0.2f, 0.08f, Mathf.Lerp(de, a, t / dura) * 0.45f); yield return null; }
            calido.color = new Color(0.32f, 0.2f, 0.08f, a * 0.45f);
        }

        void Construir()
        {
            var go = new GameObject("Recuerdo_Respirar", typeof(RectTransform)); go.transform.SetParent(transform, false);
            lienzo = go.AddComponent<Canvas>(); lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 75;   // debajo de la guía de respirar (80)
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            // el recuerdo se ve cálido, como la luz del carro
            calido = new GameObject("Calido", typeof(RectTransform)).AddComponent<Image>(); calido.transform.SetParent(go.transform, false);
            var r = (RectTransform)calido.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            calido.color = Color.clear; calido.raycastTarget = false;
            linea = Texto(go.transform, 34, new Vector2(0f, 250f), new Vector2(1500f, 160f));
            aviso = Texto(go.transform, 28, new Vector2(0f, -470f), new Vector2(1400f, 60f));
        }

        static Text Texto(Transform padre, int tam, Vector2 pos, Vector2 tamano)
        {
            var tx = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>(); tx.transform.SetParent(padre, false);
            tx.font = Fuentes.Maquina ? Fuentes.Maquina : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tx.fontSize = tam; tx.alignment = TextAnchor.MiddleCenter; tx.supportRichText = true; tx.raycastTarget = false;
            tx.color = Color.clear; tx.horizontalOverflow = HorizontalWrapMode.Wrap; tx.verticalOverflow = VerticalWrapMode.Overflow;
            var s = tx.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.85f); s.effectDistance = new Vector2(2, -2);
            var rt = (RectTransform)tx.transform; rt.sizeDelta = tamano; rt.anchoredPosition = pos;
            return tx;
        }
    }
}
