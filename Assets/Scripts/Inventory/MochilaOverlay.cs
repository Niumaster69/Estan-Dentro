using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using EstanDentro.UI;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// La mochila (I): Mateo se agacha, mira abajo y la abre. Las cosas son dibujos que se ARRASTRAN entre las manos y la
    /// mochila, o afuera para dejarlas en el piso. El cuaderno (pistas, misiones) queda en Tab.
    /// (Henry: "que si se le da I se agache el personaje para ver la mochila… el inventario se gestionaría en la mochila")
    /// </summary>
    public class MochilaOverlay : MonoBehaviour
    {
        static MochilaOverlay inst;
        public static bool EstaAbierta => inst && inst.abierta;

        Canvas lienzo; CanvasGroup grupo;
        RectTransform zonaManos, zonaMochila, zonaBolsillo, raiz;
        Text titulo, info, pie;
        readonly List<GameObject> piezas = new List<GameObject>();
        bool abierta, animando, agachadoAntes;
        float pitchAntes;
        Player.PlayerController jugador;
        static readonly Color tela = new Color(0.20f, 0.23f, 0.19f, 0.97f), telaOscura = new Color(0.12f, 0.14f, 0.11f, 1f),
                              costura = new Color(0.80f, 0.72f, 0.50f, 0.8f), papel = new Color(0.93f, 0.90f, 0.82f, 1f);

        public static void Abrir()
        {
            if (!inst) { var go = new GameObject("Mochila_UI"); DontDestroyOnLoad(go); inst = go.AddComponent<MochilaOverlay>(); inst.Construir(); }
            if (OverlayBlocker.IsBlocking || InventoryOverlay.EstaAbierto) return;            // nunca debajo de otra ventana
            if (!inst.abierta && !inst.animando) inst.StartCoroutine(inst.AbrirAnimado());
        }

        public static void Cerrar() { if (inst && inst.abierta && !inst.animando) inst.StartCoroutine(inst.CerrarAnimado()); }

        IEnumerator AbrirAnimado()
        {
            animando = true;
            jugador = FindAnyObjectByType<Player.PlayerController>();
            if (jugador)
            {
                agachadoAntes = jugador.ForzarAgachado; pitchAntes = jugador.Pitch;
                jugador.ForzarAgachado = true; jugador.InputEnabled = false;
                for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)               // se agacha y mira la mochila
                { jugador.SetPitch(Mathf.Lerp(pitchAntes, 62f, t / 0.35f)); yield return null; }
            }
            EstanDentro.Audio.SonidoCorto.Juego("objetos/cuaderno_abrir", Camera.main ? Camera.main.transform.position : Vector3.zero, 0.5f, 0f);
            abierta = true;
            Pausa.Pedir(this); OverlayBlocker.Register();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            lienzo.gameObject.SetActive(true);
            Rehacer();
            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime) { grupo.alpha = t / 0.18f; yield return null; }
            grupo.alpha = 1f; animando = false;
        }

        IEnumerator CerrarAnimado()
        {
            animando = true;
            for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime) { grupo.alpha = 1f - t / 0.12f; yield return null; }
            lienzo.gameObject.SetActive(false);
            abierta = false;
            Pausa.Soltar(this); OverlayBlocker.Unregister();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            if (jugador)
            {
                float desde = jugador.Pitch;
                for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime) { jugador.SetPitch(Mathf.Lerp(desde, pitchAntes, t / 0.25f)); yield return null; }
                jugador.ForzarAgachado = agachadoAntes; jugador.InputEnabled = true;
            }
            animando = false;
        }

        void Update()
        {
            if (!abierta || animando) return;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && (kb.iKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) || (gp != null && gp.buttonEast.wasPressedThisFrame))
                Cerrar();
            else if (kb != null && kb.tabKey.wasPressedThisFrame)
            { StartCoroutine(CerrarYCuaderno()); }
        }

        IEnumerator CerrarYCuaderno() { yield return CerrarAnimado(); InventoryOverlay.Open(); }

        // ------------------------------------------------------------------ contenido (estilo B: el cuaderno abierto)
        static readonly Color tinta = new Color(0.16f, 0.15f, 0.13f), tinta2 = new Color(0.35f, 0.32f, 0.26f, 1f), cinta = new Color(0.90f, 0.84f, 0.62f, 0.8f);
        Text sinMochila;

        static Font mano;
        static Font Mano()
        {
            if (mano) return mano;
            mano = Font.CreateDynamicFontFromOSFont(new[] { "Ink Free", "Segoe Print", "Comic Sans MS" }, 32);   // letra a mano de Mateo
            return mano ? mano : (mano = Fuentes.Maquina);
        }

        public void Rehacer()
        {
            foreach (var p in piezas) if (p) Destroy(p);
            piezas.Clear();
            var inv = Inventory.Instance;
            if (inv == null) return;
            zonaMochila.gameObject.SetActive(inv.TieneMochila);
            sinMochila.gameObject.SetActive(!inv.TieneMochila);
            var enMano = inv.CarriedItems.FindAll(x => x.enMano && !Inventory.VaAlBolsillo(x.id));
            int k = 0;
            foreach (var it in enMano) { Ficha(it, zonaManos, k, 2, Inventory.DosManos(it.id)); k += Inventory.DosManos(it.id) ? 2 : 1; }
            var guardado = inv.CarriedItems.FindAll(x => !x.enMano && !Inventory.VaAlBolsillo(x.id));
            for (int i = 0; i < guardado.Count; i++) Ficha(guardado[i], zonaMochila, i, 3, false);
            info.text = "Pasa el mouse por algo para ver qué es.";
        }

        void Ficha(Inventory.ItemEntry it, RectTransform zona, int i, int columnas, bool doble)
        {
            var go = new GameObject("Ficha_" + it.id, typeof(RectTransform)); go.transform.SetParent(zona, false);
            piezas.Add(go);
            var r = (RectTransform)go.transform;
            const float lado = 190f, sep = 214f;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(10f + (i % columnas) * sep, -10f - (i / columnas) * sep);
            r.sizeDelta = new Vector2(doble ? lado * 2f + 24f : lado, lado);
            r.localRotation = Quaternion.Euler(0f, 0f, ((it.id.GetHashCode() & 7) - 3.5f) * 0.6f);          // pegadas a mano, un poco torcidas
            var fondo = go.AddComponent<Image>(); fondo.sprite = Redondeado(); fondo.type = Image.Type.Sliced;
            fondo.color = new Color(1f, 0.99f, 0.95f, 0.55f);
            go.AddComponent<Outline>().effectColor = new Color(tinta.r, tinta.g, tinta.b, 0.25f);
            var tape = Caja(go.transform, Vector2.zero, new Vector2(78f, 22f), cinta);                          // la cinta que la pega
            var rt = (RectTransform)tape.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, 2f); rt.localRotation = Quaternion.Euler(0, 0, -4f);
            var dib = new GameObject("Dibujo", typeof(RectTransform)).AddComponent<Image>(); dib.transform.SetParent(go.transform, false);
            var rd = (RectTransform)dib.transform; rd.anchorMin = new Vector2(0.06f, 0.24f); rd.anchorMax = new Vector2(0.94f, 0.92f); rd.offsetMin = rd.offsetMax = Vector2.zero;
            dib.sprite = DibujosObjetos.De(it.id); dib.preserveAspect = true; dib.raycastTarget = false;
            if (!dib.sprite) dib.color = Color.clear;
            var nombre = Texto(go.transform, 26, it.displayName, tinta); nombre.font = Mano();
            var rn = (RectTransform)nombre.transform; rn.anchorMin = new Vector2(0f, 0f); rn.anchorMax = new Vector2(1f, 0.25f); rn.offsetMin = rn.offsetMax = Vector2.zero;
            if (it.cantidad > 1)
            {
                var n = Texto(go.transform, 30, "×" + it.cantidad, new Color(0.55f, 0.12f, 0.08f)); n.font = Mano(); n.alignment = TextAnchor.UpperRight;
                var rc = (RectTransform)n.transform; rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(1f, 1f); rc.anchoredPosition = new Vector2(-8f, -6f); rc.sizeDelta = new Vector2(90f, 40f);
            }
            var arr = go.AddComponent<FichaArrastrable>(); arr.id = it.id; arr.mochila = this;
        }

        /// <summary>Soltar una ficha: según dónde cayó, a la mano, a la mochila o al piso.</summary>
        public void Solto(string id, Vector2 pantalla)
        {
            var inv = Inventory.Instance; if (inv == null) return;
            var it = inv.Buscar(id); if (it == null) return;
            bool enManos = RectTransformUtility.RectangleContainsScreenPoint(zonaManos, pantalla);
            bool enMochila = zonaMochila.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(zonaMochila, pantalla);
            bool dentro = RectTransformUtility.RectangleContainsScreenPoint(raiz, pantalla);
            string msg = "";
            if (enManos && !it.enMano) msg = inv.Mover(id);
            else if (enMochila && it.enMano) msg = inv.Mover(id);
            else if (!dentro) inv.Soltar(id, inv.PuntoParaSoltar());
            Rehacer();
            if (!string.IsNullOrEmpty(msg)) info.text = msg;
        }

        public void Mostrar(string id)
        {
            var inv = Inventory.Instance; var it = inv?.Buscar(id); if (it == null) return;
            var f = CatalogoObjetos.De(id);
            info.text = $"<b>{it.displayName}</b>{(it.cantidad > 1 ? $"  (tengo {it.cantidad})" : "")}\n{f.queEs}\n<color=#8E2A1E>{f.paraQue}</color>" + (string.IsNullOrEmpty(f.tecla) ? "" : $"\n<color=#6B624F>{f.tecla}</color>");
        }

        // ------------------------------------------------------------------ armado: un cuaderno abierto, dos páginas
        void Construir()
        {
            if (!FindAnyObjectByType<EventSystem>()) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>(); DontDestroyOnLoad(es); }
            var c = new GameObject("Lienzo", typeof(RectTransform)); c.transform.SetParent(transform, false);
            lienzo = c.AddComponent<Canvas>(); lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 160;
            var sc = c.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            c.AddComponent<GraphicRaycaster>();
            grupo = c.AddComponent<CanvasGroup>();
            var velo = Caja(c.transform, Vector2.zero, new Vector2(4000, 3000), new Color(0f, 0f, 0f, 0.6f)); velo.raycastTarget = true;
            // el libro
            var sombra = Caja(c.transform, new Vector2(10f, -28f), new Vector2(1560f, 860f), new Color(0f, 0f, 0f, 0.5f));
            var libro = Caja(c.transform, new Vector2(0f, -10f), new Vector2(1540f, 840f), papel);
            raiz = (RectTransform)libro.transform;
            for (int y = 0; y < 22; y++)                                                                   // renglones
                Caja(raiz, new Vector2(0f, 360f - y * 36f), new Vector2(1480f, 2f), new Color(0.31f, 0.43f, 0.63f, 0.22f));
            Caja(raiz, new Vector2(-690f, 0f), new Vector2(2f, 840f), new Color(0.75f, 0.25f, 0.2f, 0.35f));   // margen rojo
            Caja(raiz, Vector2.zero, new Vector2(22f, 840f), new Color(0f, 0f, 0f, 0.22f));                   // el lomo
            Caja(raiz, Vector2.zero, new Vector2(6f, 840f), new Color(0f, 0f, 0f, 0.35f));
            // página izquierda: en mis manos + la ficha del objeto
            titulo = Texto(raiz, 56, "En mis manos", tinta); titulo.font = Mano(); titulo.alignment = TextAnchor.MiddleLeft;
            Pos(titulo, new Vector2(-400f, 340f), new Vector2(600f, 80f));
            zonaManos = Zona(raiz, new Vector2(-445f, 85f), new Vector2(470f, 230f));
            info = Texto(raiz, 30, "", tinta); info.font = Mano(); info.alignment = TextAnchor.UpperLeft;
            Pos(info, new Vector2(-390f, -230f), new Vector2(620f, 300f));
            // página derecha: en la mochila (6 lugares)
            var t2 = Texto(raiz, 56, "En la mochila", tinta); t2.font = Mano(); t2.alignment = TextAnchor.MiddleLeft;
            Pos(t2, new Vector2(370f, 340f), new Vector2(600f, 80f));
            zonaMochila = Zona(raiz, new Vector2(380f, -40f), new Vector2(660f, 450f));
            sinMochila = Texto(raiz, 40, "Mi mochila se quedó en mi puesto…\nTengo que encontrarla.\n\nSin ella solo cargo lo que\nme quepa en las manos.", tinta2); sinMochila.font = Mano();
            Pos(sinMochila, new Vector2(380f, 20f), new Vector2(620f, 400f));
            zonaBolsillo = zonaManos;                                                                       // (ya no hay bolsillo)
            pie = Texto(c.transform, 22, "Arrastra: a las MANOS para usar, a la MOCHILA para guardar, afuera del cuaderno para dejarlo en el piso.\n[I] cerrar        [Tab] pistas y misiones", new Color(0.95f, 0.93f, 0.85f));
            Pos(pie, new Vector2(0f, -480f), new Vector2(1700f, 70f));
            lienzo.gameObject.SetActive(false);
        }

        RectTransform Zona(Transform padre, Vector2 pos, Vector2 tam)
        {
            var z = Caja(padre, pos, tam, new Color(0f, 0f, 0f, 0.0f)); z.raycastTarget = true;
            return (RectTransform)z.transform;
        }

        static Image Caja(Transform padre, Vector2 pos, Vector2 tam, Color c)
        {
            var g = new GameObject("Caja", typeof(RectTransform)).AddComponent<Image>(); g.transform.SetParent(padre, false);
            var r = (RectTransform)g.transform; r.anchoredPosition = pos; r.sizeDelta = tam; g.color = c; g.raycastTarget = false;
            g.sprite = Redondeado(); g.type = Image.Type.Sliced;
            return g;
        }

        static Text Texto(Transform padre, int tam, string s, Color c)
        {
            var t = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>(); t.transform.SetParent(padre, false);
            t.font = Fuentes.Maquina; t.fontSize = tam; t.text = s; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.supportRichText = true; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void Pos(Graphic g, Vector2 p, Vector2 tam) { var r = (RectTransform)g.transform; r.anchoredPosition = p; r.sizeDelta = tam; }

        static Sprite redondeado;
        static Sprite Redondeado()
        {
            if (redondeado) return redondeado;
            const int N = 48, R = 14;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(R - x, x - (N - 1 - R))), dy = Mathf.Max(0, Mathf.Max(R - y, y - (N - 1 - R)));
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(R - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
            }
            t.Apply();
            return redondeado = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        }
    }

    /// <summary>Una cosa de la mochila que se arrastra con el mouse.</summary>
    public class FichaArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler
    {
        public string id; public MochilaOverlay mochila;
        Vector2 inicio; Transform padre; int orden;

        public void OnPointerEnter(PointerEventData e) => mochila.Mostrar(id);

        public void OnBeginDrag(PointerEventData e)
        {
            var r = (RectTransform)transform; inicio = r.anchoredPosition; padre = transform.parent; orden = transform.GetSiblingIndex();
            transform.SetParent(GetComponentInParent<Canvas>().transform, true); transform.SetAsLastSibling();
            r.localScale = Vector3.one * 1.08f;
        }

        public void OnDrag(PointerEventData e) { transform.position = e.position; }

        public void OnEndDrag(PointerEventData e)
        {
            transform.SetParent(padre, true); transform.SetSiblingIndex(orden);
            var r = (RectTransform)transform; r.anchoredPosition = inicio; r.localScale = Vector3.one;
            mochila.Solto(id, e.position);
        }
    }
}
