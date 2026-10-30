using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EstanDentro.UI
{
    /// <summary>
    /// Guía de controles (Henry: "cada cosa nueva que puede hacer el usuario es algo nuevo que debe aprender").
    ///  - Junto al punto del centro: la tecla dibujada y lo que hace con lo que se está mirando ([E] Abrir el casillero).
    ///  - PrimeraVez(...): la primera vez que se puede usar una tecla nueva, un aviso fijo abajo con la tecla dibujada;
    ///    se quita en cuanto el jugador la usa (o a los 12 s). No vuelve a salir en esa partida.
    /// </summary>
    public class GuiaTeclas : MonoBehaviour
    {
        static GuiaTeclas inst;
        static readonly HashSet<string> vistas = new HashSet<string>();
        GameObject junto, abajo; Text juntoLetra, juntoTexto, abajoLetra, abajoTexto;
        string teclaAbajo; float hastaAbajo;
        readonly Queue<(string tecla, string texto)> fila = new Queue<(string, string)>();

        static GuiaTeclas Inst
        {
            get
            {
                if (inst) return inst;
                var go = new GameObject("GuiaTeclas"); DontDestroyOnLoad(go);
                inst = go.AddComponent<GuiaTeclas>(); inst.Construir(); return inst;
            }
        }

        /// <summary>Tecla junto al punto (null = nada).</summary>
        public static void Junto(string tecla, string accion)
        {
            if (tecla == null && !inst) return;
            var g = Inst; bool ver = tecla != null;
            if (g.junto.activeSelf != ver) g.junto.SetActive(ver);
            if (ver) { g.juntoLetra.text = tecla; g.juntoTexto.text = accion; Ajustar(g.junto, g.juntoLetra, g.juntoTexto); }
        }

        /// <summary>La primera vez que se puede usar una tecla: aviso fijo abajo hasta que la usa.</summary>
        public static void PrimeraVez(string tecla, string texto)
        {
            if (!vistas.Add(tecla + "|" + texto)) return;
            Inst.fila.Enqueue((tecla, texto));
        }

        /// <summary>Avisar que el jugador ya usó esa tecla (quita el aviso si es el que está).</summary>
        public static void Usada(string tecla)
        {
            if (inst && inst.teclaAbajo == tecla) inst.hastaAbajo = 0f;
        }

        void Update()
        {
            bool bloqueado = OverlayBlocker.IsBlocking || Time.timeScale == 0f;
            if (teclaAbajo != null && (Time.unscaledTime > hastaAbajo))
            { teclaAbajo = null; abajo.SetActive(false); }
            if (teclaAbajo == null && fila.Count > 0 && !bloqueado)
            {
                var (t, x) = fila.Dequeue(); teclaAbajo = t; hastaAbajo = Time.unscaledTime + 12f;
                abajoLetra.text = t; abajoTexto.text = x; Ajustar(abajo, abajoLetra, abajoTexto); abajo.SetActive(true);
            }
            if (abajo.activeSelf == bloqueado && teclaAbajo != null) abajo.SetActive(!bloqueado);
            if (bloqueado && junto.activeSelf) junto.SetActive(false);
        }

        /// <summary>La cajita de la tecla se ensancha para teclas largas ("Tab", "Esc") y el texto se corre detrás.</summary>
        static void Ajustar(GameObject go, Text letra, Text texto)
        {
            var caja = (RectTransform)go.transform.Find("Caja");
            float alto = caja.sizeDelta.y;
            float ancho = Mathf.Max(alto, letra.text.Length * letra.fontSize * 0.62f + 16f);
            caja.sizeDelta = new Vector2(ancho, alto); caja.anchoredPosition = new Vector2(ancho / 2f, 0f);
            var rl = (RectTransform)letra.transform; rl.sizeDelta = new Vector2(ancho, rl.sizeDelta.y); rl.anchoredPosition = new Vector2(ancho / 2f, 0f);
            var rt = (RectTransform)texto.transform; rt.anchoredPosition = new Vector2(ancho + 14f + rt.sizeDelta.x / 2f, 0f);
        }

        void Construir()
        {
            var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 91;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            (junto, juntoLetra, juntoTexto) = Tecla(new Vector2(30f, -2f), 26);
            (abajo, abajoLetra, abajoTexto) = Tecla(new Vector2(-170f, -175f), 30);     // encima de los avisos de abajo (no se pisan)
            junto.SetActive(false); abajo.SetActive(false);
        }

        (GameObject, Text, Text) Tecla(Vector2 pos, int tam)
        {
            var go = new GameObject("Tecla", typeof(RectTransform)); go.transform.SetParent(transform, false);
            ((RectTransform)go.transform).anchoredPosition = pos;
            var caja = new GameObject("Caja", typeof(RectTransform)).AddComponent<Image>(); caja.transform.SetParent(go.transform, false);
            float lado = tam * 1.5f;
            var rc = (RectTransform)caja.transform; rc.sizeDelta = new Vector2(lado, lado); rc.anchoredPosition = new Vector2(lado / 2f, 0f);
            caja.color = new Color(0.95f, 0.93f, 0.86f, 0.92f); caja.raycastTarget = false;
            var letra = Texto(go.transform, tam - 2, new Vector2(lado / 2f, 0f), lado, TextAnchor.MiddleCenter); letra.color = new Color(0.08f, 0.08f, 0.1f);
            var texto = Texto(go.transform, tam, new Vector2(lado + 14f + 400f, 0f), 800f, TextAnchor.MiddleLeft);
            texto.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            return (go, letra, texto);
        }

        static Text Texto(Transform padre, int tam, Vector2 pos, float ancho, TextAnchor al)
        {
            var t = new GameObject("T", typeof(RectTransform)).AddComponent<Text>(); t.transform.SetParent(padre, false);
            t.font = Fuentes.Maquina;
            t.fontSize = tam; t.alignment = al; t.color = new Color(0.97f, 0.95f, 0.9f); t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var r = (RectTransform)t.transform; r.sizeDelta = new Vector2(ancho, tam * 1.6f); r.anchoredPosition = pos;
            return t;
        }
    }
}
