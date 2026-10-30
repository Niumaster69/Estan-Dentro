using UnityEngine;
using UnityEngine.UI;

namespace EstanDentro.UI
{
    /// <summary>
    /// Lo que Mateo tiene en las manos, siempre a la vista abajo a la derecha (Henry: "tener C de manos en el HUD").
    /// Dos cuadros con el dibujo y el número (pilas, monedas…); si una mano tiene la linterna, debajo va su barrita de pila
    /// (con poca pila parpadea en rojo).
    /// </summary>
    public class ManosHUD : MonoBehaviour
    {
        static ManosHUD inst;
        CanvasGroup grupo;
        readonly Image[] dibujo = new Image[2], marco = new Image[2], pila = new Image[2], pilaFondo = new Image[2];
        readonly Text[] cuenta = new Text[2];
        Interaction.Flashlight linterna; float buscar;
        Player.PlayerController jugador; float buscarJugador;
        string firma = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (inst) return;
            var go = new GameObject("ManosHUD"); DontDestroyOnLoad(go);
            inst = go.AddComponent<ManosHUD>(); inst.Construir();
        }

        void Update()
        {
            var inv = Inventory.Inventory.Instance;
            // solo jugando: en el menú (o en una escena sin Mateo) no se ven las manos
            if (!jugador && (buscarJugador -= Time.unscaledDeltaTime) <= 0f) { buscarJugador = 0.5f; jugador = FindAnyObjectByType<Player.PlayerController>(); }
            bool ver = jugador && inv != null && !OverlayBlocker.IsBlocking && Time.timeScale > 0f && inv.CarriedItems.Count > 0;
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, ver ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (inv == null) return;
            if (!linterna && (buscar -= Time.unscaledDeltaTime) <= 0f) { buscar = 1f; linterna = FindAnyObjectByType<Interaction.Flashlight>(); }
            var enMano = inv.CarriedItems.FindAll(x => x.enMano && !Inventory.Inventory.VaAlBolsillo(x.id));
            // solo se rehace lo que cambia
            string f = string.Join("|", enMano.ConvertAll(x => x.id + x.cantidad));
            if (f != firma)
            {
                firma = f;
                for (int i = 0; i < 2; i++)
                {
                    var it = i < enMano.Count ? enMano[i] : (i == 1 && enMano.Count == 1 && Inventory.Inventory.DosManos(enMano[0].id) ? enMano[0] : null);
                    dibujo[i].sprite = it != null ? Inventory.DibujosObjetos.De(it.id) : null;
                    dibujo[i].color = dibujo[i].sprite ? Color.white : Color.clear;
                    cuenta[i].text = it != null && it.cantidad > 1 ? "×" + it.cantidad : "";
                    marco[i].color = it != null ? new Color(0.93f, 0.90f, 0.82f, 0.92f) : new Color(0.93f, 0.90f, 0.82f, 0.18f);
                }
            }
            // la pila, debajo de la mano que tiene la linterna
            for (int i = 0; i < 2; i++)
            {
                bool esLinterna = i < enMano.Count && enMano[i].id == "linterna" && linterna;
                pilaFondo[i].enabled = pila[i].enabled = esLinterna;
                if (!esLinterna) continue;
                float c = Mathf.Clamp01(linterna.Carga);
                bool baja = c < 0.25f, parpadeo = baja && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f;
                var rp = (RectTransform)pila[i].transform; rp.sizeDelta = new Vector2(124f * c, rp.sizeDelta.y);
                pila[i].color = baja ? new Color(0.85f, 0.2f, 0.15f, parpadeo ? 0.35f : 1f) : new Color(0.93f, 0.88f, 0.62f, 1f);
            }
        }

        void Construir()
        {
            var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 120;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            grupo = gameObject.AddComponent<CanvasGroup>(); grupo.alpha = 0f; grupo.blocksRaycasts = false;
            for (int i = 0; i < 2; i++)
            {
                // mano izquierda y derecha, abajo a la derecha de la pantalla
                var celda = new GameObject("Mano" + i, typeof(RectTransform)); celda.transform.SetParent(transform, false);
                var r = (RectTransform)celda.transform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
                r.anchoredPosition = new Vector2(-40f - (1 - i) * 156f, 46f); r.sizeDelta = new Vector2(140f, 140f);
                marco[i] = celda.AddComponent<Image>(); marco[i].raycastTarget = false;
                dibujo[i] = Img(celda.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
                dibujo[i].preserveAspect = true;
                cuenta[i] = new GameObject("N", typeof(RectTransform)).AddComponent<Text>(); cuenta[i].transform.SetParent(celda.transform, false);
                cuenta[i].font = Fuentes.Maquina; cuenta[i].fontSize = 26; cuenta[i].color = new Color(0.55f, 0.12f, 0.08f); cuenta[i].alignment = TextAnchor.UpperRight; cuenta[i].raycastTarget = false;
                var rc = (RectTransform)cuenta[i].transform; rc.anchorMin = rc.anchorMax = rc.pivot = Vector2.one; rc.anchoredPosition = new Vector2(-6f, -4f); rc.sizeDelta = new Vector2(80f, 34f);
                pilaFondo[i] = Img(celda.transform, Vector2.zero, Vector2.zero); pilaFondo[i].color = new Color(0f, 0f, 0f, 0.6f);
                var rf = (RectTransform)pilaFondo[i].transform; rf.anchorMin = rf.anchorMax = rf.pivot = new Vector2(0.5f, 0f); rf.anchoredPosition = new Vector2(0f, -22f); rf.sizeDelta = new Vector2(132f, 14f);
                pila[i] = Img(pilaFondo[i].transform, Vector2.zero, Vector2.zero);
                var rp = (RectTransform)pila[i].transform; rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0f, 0.5f); rp.anchoredPosition = new Vector2(4f, 0f); rp.sizeDelta = new Vector2(124f, 8f);
            }
        }

        static Image Img(Transform padre, Vector2 min, Vector2 max)
        {
            var i = new GameObject("I", typeof(RectTransform)).AddComponent<Image>(); i.transform.SetParent(padre, false); i.raycastTarget = false;
            var r = (RectTransform)i.transform; r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            return i;
        }
    }
}
