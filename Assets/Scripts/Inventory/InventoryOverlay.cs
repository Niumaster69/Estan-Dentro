using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// EL CUADERNO DE MATEO (Henry eligió el estilo "cuaderno", 19-oct). Es a la vez el inventario y la tarjeta de
    /// objeto nuevo. Dos hojas: a la izquierda la lista, a la derecha el detalle. Pestañas al borde: Objetos, Pistas,
    /// Objetivos.
    ///   Objetos: EN LAS MANOS (2) / EN LA MOCHILA (4) / EN EL BOLSILLO. Espacio pasa de la mano a la mochila y al revés;
    ///            G suelta. Lo demás es automático: lo que hace falta, Mateo lo saca solo (Inventory.Usar).
    /// Controles: W/S o flechas eligen · Espacio mano/mochila · G soltar · Tab o 1-3 pestañas · I, E o Esc cerrar.
    /// </summary>
    public class InventoryOverlay : MonoBehaviour
    {
        public enum Tab { Items, Notas, Misiones }

        private static InventoryOverlay instance;
        static readonly Color papel = new Color(0.90f, 0.86f, 0.76f);
        static readonly Color tinta = new Color(0.16f, 0.14f, 0.12f);
        static readonly Color tintaSuave = new Color(0.36f, 0.32f, 0.27f);
        static readonly Color rojo = new Color(0.62f, 0.12f, 0.12f);
        static readonly Color renglon = new Color(0.27f, 0.43f, 0.63f, 0.28f);
        static readonly Color margen = new Color(0.72f, 0.16f, 0.12f, 0.5f);

        private Canvas canvas;
        private RectTransform hojaIzq, hojaDer, contIzq, contDer;
        private readonly Dictionary<Tab, Image> pestanas = new Dictionary<Tab, Image>();
        private Text pie;
        private Tab tab = Tab.Items;
        private int sel;
        private bool modoNuevo;
        private string idNuevo;
        private Action alCerrar;
        private bool consumir;
        private CursorLockMode cursorPrevio; private bool cursorVisible;
        private readonly List<(RectTransform rt, Action alElegir)> filas = new List<(RectTransform, Action)>();
        private Inventory suscrito;

        public static bool EstaAbierto => instance != null && instance.canvas.gameObject.activeSelf;

        // ------------------------------------------------------------------ API
        public static void Open(Action onClose = null) => OpenTab(Tab.Items, onClose);

        public static void OpenTab(Tab t, Action onClose = null)
        {
            Asegurar();
            if (EstaAbierto) { instance.tab = t; instance.sel = 0; instance.Refrescar(); return; }
            instance.Abrir(t, null, false, onClose);
        }

        /// <summary>Abre el cuaderno en la hoja de un objeto. esNuevo = recién recogido (sello NUEVO, "[E] seguir").</summary>
        public static void AbrirObjeto(string id, bool esNuevo, Action onClose)
        {
            Asegurar();
            if (EstaAbierto) { instance.tab = Tab.Items; instance.SeleccionarObjeto(id); instance.Refrescar(); return; }
            instance.Abrir(Tab.Items, id, esNuevo, onClose);
        }

        public static void Close()
        {
            if (!EstaAbierto) return;
            instance.canvas.gameObject.SetActive(false);
            EstanDentro.Audio.SonidoCorto.Juego("objetos/cuaderno_cerrar", Vector3.zero, 0.6f, 0f);
            Pausa.Soltar(instance);
            Cursor.lockState = instance.cursorPrevio; Cursor.visible = instance.cursorVisible;
            OverlayBlocker.Unregister();
            var cb = instance.alCerrar; instance.alCerrar = null; cb?.Invoke();
        }

        static void Asegurar()
        {
            if (instance != null) return;
            var go = new GameObject("__CuadernoDeMateo");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<InventoryOverlay>();
            instance.Construir();
            instance.canvas.gameObject.SetActive(false);
        }

        void Abrir(Tab t, string id, bool esNuevo, Action onClose)
        {
            tab = t; sel = 0; modoNuevo = esNuevo; idNuevo = esNuevo ? id : null; alCerrar = onClose;
            if (!string.IsNullOrEmpty(id)) SeleccionarObjeto(id);
            Pausa.Pedir(this);
            EstanDentro.Audio.SonidoCorto.Juego("objetos/cuaderno_abrir", Vector3.zero, 0.6f, 0f);
            cursorPrevio = Cursor.lockState; cursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            canvas.gameObject.SetActive(true);
            consumir = true;
            OverlayBlocker.Register();
            Refrescar();
        }

        void SeleccionarObjeto(string id)
        {
            var lista = Ordenados();
            int i = lista.FindIndex(x => x.id == id);
            sel = Mathf.Max(0, i);
        }

        // ------------------------------------------------------------------ entrada
        private void Update()
        {
            var inv = Inventory.Instance;
            if (inv != suscrito)
            {
                if (suscrito != null) suscrito.OnInventoryChanged -= AlCambiar;
                suscrito = inv;
                if (suscrito != null) suscrito.OnInventoryChanged += AlCambiar;
            }
            if (!EstaAbierto) return;
            if (consumir) { consumir = false; return; }
            var kb = Keyboard.current; var gp = Gamepad.current; var ms = Mouse.current;

            bool cerrar = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                       || (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame));
            if (cerrar) { Close(); return; }

            int n = filas.Count;
            if (n > 0)
            {
                if ((kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) || (gp != null && gp.dpad.down.wasPressedThisFrame)) { sel = (sel + 1) % n; modoNuevo = false; Refrescar(); }
                if ((kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) || (gp != null && gp.dpad.up.wasPressedThisFrame)) { sel = (sel - 1 + n) % n; modoNuevo = false; Refrescar(); }
            }
            if ((kb != null && kb.tabKey.wasPressedThisFrame) || (gp != null && gp.rightShoulder.wasPressedThisFrame)) CambiarTab((Tab)(((int)tab + 1) % 3));
            if (kb != null && kb.digit1Key.wasPressedThisFrame) CambiarTab(Tab.Items);
            if (kb != null && kb.digit2Key.wasPressedThisFrame) CambiarTab(Tab.Notas);
            if (kb != null && kb.digit3Key.wasPressedThisFrame) CambiarTab(Tab.Misiones);

            if (tab == Tab.Items && Inventory.Instance != null)
            {
                var lista = Ordenados();
                var it = sel < lista.Count ? lista[sel] : null;
                if (it != null && ((kb != null && kb.spaceKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame)))
                {
                    string msg = Inventory.Instance.Mover(it.id);
                    Refrescar();
                    Aviso(msg);
                }
                if (it != null && ((kb != null && kb.gKey.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame)))
                {
                    Inventory.Instance.Soltar(it.id, Inventory.Instance.PuntoParaSoltar());
                    sel = Mathf.Max(0, sel - 1);
                    Refrescar();
                }
            }

            if (ms != null && ms.leftButton.wasPressedThisFrame)
            {
                Vector2 p = ms.position.ReadValue();
                foreach (var kv in pestanas)
                    if (RectTransformUtility.RectangleContainsScreenPoint(kv.Value.rectTransform, p, null)) { CambiarTab(kv.Key); return; }
                for (int i = 0; i < filas.Count; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(filas[i].rt, p, null)) { sel = i; modoNuevo = false; Refrescar(); return; }
            }
        }

        void CambiarTab(Tab t) { if (t != tab) EstanDentro.Audio.SonidoCorto.Juego("objetos/cuaderno_pagina", Vector3.zero, 0.5f, 0f); tab = t; sel = 0; modoNuevo = false; Refrescar(); }
        void AlCambiar() { if (EstaAbierto) Refrescar(); }

        // ------------------------------------------------------------------ contenido
        List<Inventory.ItemEntry> Ordenados()
        {
            var r = new List<Inventory.ItemEntry>();
            var inv = Inventory.Instance; if (inv == null) return r;
            foreach (var x in inv.CarriedItems) if (!Inventory.VaAlBolsillo(x.id) && x.enMano) r.Add(x);
            foreach (var x in inv.CarriedItems) if (!Inventory.VaAlBolsillo(x.id) && !x.enMano) r.Add(x);
            foreach (var x in inv.CarriedItems) if (Inventory.VaAlBolsillo(x.id)) r.Add(x);
            return r;
        }

        void Refrescar()
        {
            Limpiar(contIzq); Limpiar(contDer); filas.Clear();
            foreach (var kv in pestanas)
            {
                bool act = kv.Key == tab;
                kv.Value.rectTransform.sizeDelta = new Vector2(116f, act ? 82f : 66f);          // la sección activa sobresale más
                kv.Value.color = new Color(kv.Value.color.r, kv.Value.color.g, kv.Value.color.b, act ? 1f : 0.7f);
            }
            switch (tab)
            {
                case Tab.Items: PaginaObjetos(); break;
                case Tab.Notas: PaginaPistas(); break;
                case Tab.Misiones: PaginaObjetivos(); break;
            }
            pie.text = modoNuevo ? "[E] seguir          [I] mochila"
                     : tab == Tab.Items ? "W/S elegir     Espacio mano ⇄ mochila     G soltar     Tab pestaña     I cerrar"
                     : "W/S elegir     Tab pestaña     I cerrar";
        }

        void PaginaObjetos()
        {
            var inv = Inventory.Instance;
            float y = 345f;
            Texto(contIzq, "Lo que llevo", 40, tinta, new Vector2(0, y), 640, TextAnchor.MiddleLeft); y -= 70f;
            var lista = Ordenados();
            if (inv == null || lista.Count == 0)
            {
                Texto(contIzq, "Tengo las manos vacías.", 28, tintaSuave, new Vector2(0, y), 640, TextAnchor.MiddleLeft);
                Texto(contDer, "Lo que recojas aparece aquí:\nlo que tengo en las manos,\nlo que guardé en la mochila\ny lo del bolsillo.", 26, tintaSuave, new Vector2(0, 120), 600, TextAnchor.MiddleLeft);
                return;
            }
            int idx = 0;
            void Seccion(string titulo, Func<Inventory.ItemEntry, bool> filtro, string vacio)
            {
                Texto(contIzq, titulo, 22, rojo, new Vector2(0, y), 640, TextAnchor.MiddleLeft); y -= 46f;
                bool alguno = false;
                foreach (var it in lista)
                {
                    if (!filtro(it)) continue;
                    alguno = true;
                    bool es = idx == sel;
                    var t = Texto(contIzq, (es ? "›  " : "    ") + it.displayName + (it.cantidad > 1 ? $"  ×{it.cantidad}" : ""), 30, es ? tinta : tintaSuave, new Vector2(0, y), 640, TextAnchor.MiddleLeft);
                    if (es) Subrayar(t.rectTransform, 340f);
                    filas.Add((t.rectTransform, null));
                    idx++; y -= 44f;
                }
                if (!alguno) { Texto(contIzq, "    " + vacio, 24, new Color(tintaSuave.r, tintaSuave.g, tintaSuave.b, 0.6f), new Vector2(0, y), 640, TextAnchor.MiddleLeft); y -= 40f; }
                y -= 14f;
            }
            Seccion($"EN LAS MANOS  {inv.ManosOcupadas}/{Inventory.Manos}", x => !Inventory.VaAlBolsillo(x.id) && x.enMano, "(libres)");
            if (inv.TieneMochila) Seccion($"EN LA MOCHILA  {inv.MochilaOcupada}/{Inventory.EspaciosMochila}", x => !Inventory.VaAlBolsillo(x.id) && !x.enMano, "(vacía)");
            else { Texto(contIzq, "SIN MOCHILA", 22, rojo, new Vector2(0, y), 640, TextAnchor.MiddleLeft); y -= 40f;
                   Texto(contIzq, "    (la mía debe estar en el salón)", 24, tintaSuave, new Vector2(0, y), 640, TextAnchor.MiddleLeft); y -= 54f; }
            if (lista.Exists(x => Inventory.VaAlBolsillo(x.id))) Seccion("EN EL BOLSILLO", x => Inventory.VaAlBolsillo(x.id), "");

            // hoja derecha: el objeto elegido
            sel = Mathf.Clamp(sel, 0, lista.Count - 1);
            var obj = lista[sel];
            var ficha = CatalogoObjetos.De(obj.id);
            // el objeto DIBUJADO en la hoja (a lápiz, hecho en Blender: scripts_blender/dibujos_objetos.py)
            var dibujo = Resources.Load<Texture2D>("Dibujos/" + obj.id);
            float yd = 300f;
            if (dibujo != null)
            {
                var v = Rect(contDer, "Dibujo", new Vector2(-10f, 165f), new Vector2(430f, 430f));
                v.localRotation = Quaternion.Euler(0, 0, 3f);
                var ri = v.gameObject.AddComponent<RawImage>(); ri.texture = dibujo; ri.raycastTarget = false;
                ri.color = new Color(1f, 1f, 1f, 0.95f);                // crayola + lápiz sobre el papel
                yd = -90f;
            }
            if (modoNuevo && obj.id == idNuevo)
            {
                var s = Texto(contDer, "NUEVO", 30, rojo, new Vector2(230f, 360f), 200, TextAnchor.MiddleCenter);
                var marco = s.gameObject.AddComponent<Outline>(); marco.effectColor = rojo; marco.effectDistance = new Vector2(1.5f, -1.5f);
                s.rectTransform.localRotation = Quaternion.Euler(0, 0, 9f);
            }
            Texto(contDer, obj.displayName.ToUpper(), 38, tinta, new Vector2(0, yd), 620, TextAnchor.MiddleLeft); yd -= 62f;
            var q = Texto(contDer, ficha.queEs, 25, tintaSuave, new Vector2(0, yd - 18f), 620, TextAnchor.UpperLeft); yd -= 92f;
            var u = Texto(contDer, ficha.paraQue, 26, rojo, new Vector2(0, yd - 18f), 620, TextAnchor.UpperLeft); yd -= 118f;
            if (!string.IsNullOrEmpty(ficha.tecla)) { Texto(contDer, ficha.tecla, 24, tinta, new Vector2(0, yd), 620, TextAnchor.MiddleLeft); yd -= 44f; }
            string donde = Inventory.VaAlBolsillo(obj.id) ? "En el bolsillo." : obj.enMano ? "Lo tengo en la mano." : "Está guardado en la mochila.";
            if (!Inventory.VaAlBolsillo(obj.id))
                donde += obj.enMano ? (inv.TieneMochila ? "   [Espacio] guardar en la mochila" : "") : "   [Espacio] sacarlo a la mano";
            Texto(contDer, donde, 22, tintaSuave, new Vector2(0, yd), 620, TextAnchor.MiddleLeft);
        }

        void PaginaPistas()
        {
            var inv = Inventory.Instance;
            Texto(contIzq, "Pistas", 40, tinta, new Vector2(0, 345f), 640, TextAnchor.MiddleLeft);
            if (inv == null || inv.ReadNotes.Count == 0)
            {
                Texto(contIzq, "Todavía no he leído nada.", 28, tintaSuave, new Vector2(0, 270f), 640, TextAnchor.MiddleLeft);
                return;
            }
            float y = 270f;
            for (int i = 0; i < inv.ReadNotes.Count; i++)
            {
                bool es = i == sel;
                var t = Texto(contIzq, (es ? "›  " : "    ") + inv.ReadNotes[i].title, 28, es ? tinta : tintaSuave, new Vector2(0, y), 640, TextAnchor.MiddleLeft);
                if (es) Subrayar(t.rectTransform, 420f);
                filas.Add((t.rectTransform, null)); y -= 46f;
            }
            sel = Mathf.Clamp(sel, 0, inv.ReadNotes.Count - 1);
            var nota = inv.ReadNotes[sel];
            Texto(contDer, nota.title, 32, rojo, new Vector2(0, 345f), 620, TextAnchor.MiddleLeft);
            Texto(contDer, nota.body, 26, tinta, new Vector2(0, 300f), 620, TextAnchor.UpperLeft, 560f);
        }

        void PaginaObjetivos()
        {
            var inv = Inventory.Instance;
            Texto(contIzq, "Lo que tengo que hacer", 36, tinta, new Vector2(0, 345f), 640, TextAnchor.MiddleLeft);
            if (inv == null || inv.Missions.Count == 0) { Texto(contIzq, "Nada todavía.", 28, tintaSuave, new Vector2(0, 270f), 640, TextAnchor.MiddleLeft); return; }
            float y = 270f;
            Inventory.MissionEntry ahora = null;
            for (int i = 0; i < inv.Missions.Count; i++)
            {
                var m = inv.Missions[i];
                if (!m.completed && ahora == null && m.category == Inventory.MissionCategory.Principal) ahora = m;
                bool es = i == sel;
                var t = Texto(contIzq, (m.completed ? "✓  " : es ? "›  " : "•  ") + m.text, 26, m.completed ? new Color(tintaSuave.r, tintaSuave.g, tintaSuave.b, 0.55f) : (es ? tinta : tintaSuave),
                              new Vector2(0, y), 640, TextAnchor.MiddleLeft);
                if (m.completed) Tachar(t.rectTransform);
                filas.Add((t.rectTransform, null)); y -= 44f;
            }
            if (ahora == null) foreach (var m in inv.Missions) if (!m.completed) { ahora = m; break; }
            Texto(contDer, "AHORA", 24, rojo, new Vector2(0, 345f), 620, TextAnchor.MiddleLeft);
            Texto(contDer, ahora != null ? ahora.text : "Seguir adelante.", 34, tinta, new Vector2(0, 300f), 620, TextAnchor.UpperLeft, 300f);
        }

        void Aviso(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            var t = Texto(contDer, msg, 22, rojo, new Vector2(0, -385f), 620, TextAnchor.MiddleLeft);
        }

        // ------------------------------------------------------------------ construcción
        void Construir()
        {
            var cgo = new GameObject("Cuaderno_Canvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 210;
            var sc = cgo.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f); sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            var bg = Rect(canvas.transform, "Fondo", Vector2.zero, Vector2.zero);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.015f, 0.015f, 0.9f);

            var libro = Rect(canvas.transform, "Cuaderno", new Vector2(-30f, 10f), new Vector2(1600f, 900f));
            libro.localRotation = Quaternion.Euler(0, 0, -0.6f);
            var sombra = Rect(libro, "Sombra", new Vector2(14f, -18f), new Vector2(1600f, 900f));
            sombra.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);

            hojaIzq = Hoja(libro, new Vector2(-400f, 0f), true);
            hojaDer = Hoja(libro, new Vector2(400f, 0f), false);
            var lomo = Rect(libro, "Lomo", Vector2.zero, new Vector2(26f, 900f));
            lomo.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.2f, 0.15f, 0.35f);

            contIzq = Rect(hojaIzq, "Contenido", new Vector2(40f, 0f), new Vector2(640f, 860f));
            contDer = Rect(hojaDer, "Contenido", new Vector2(-10f, 0f), new Vector2(640f, 860f));

            // secciones arriba del cuaderno, como separadores (estilo The Forest): mochila, lupa, diana
            Pestana(libro, Tab.Items, "OBJETOS", new Vector2(-590f, 446f), new Color(0.55f, 0.12f, 0.10f));
            Pestana(libro, Tab.Notas, "PISTAS", new Vector2(-460f, 446f), new Color(0.23f, 0.35f, 0.23f));
            Pestana(libro, Tab.Misiones, "OBJETIVOS", new Vector2(-330f, 446f), new Color(0.20f, 0.29f, 0.45f));

            pie = Texto(canvas.transform, "", 20, new Color(0.95f, 0.93f, 0.85f, 0.6f), new Vector2(0f, -495f), 1500, TextAnchor.MiddleCenter);
        }

        RectTransform Hoja(Transform padre, Vector2 pos, bool conMargen)
        {
            var h = Rect(padre, "Hoja", pos, new Vector2(800f, 900f));
            h.gameObject.AddComponent<Image>().color = papel;
            for (int r = 0; r < 21; r++)
            {
                var ln = Rect(h, "Renglon", new Vector2(0f, 380f - r * 40f), new Vector2(800f, 2f));
                ln.gameObject.AddComponent<Image>().color = renglon;
            }
            if (conMargen)
            {
                var m = Rect(h, "Margen", new Vector2(-330f, 0f), new Vector2(2f, 900f));
                m.gameObject.AddComponent<Image>().color = margen;
            }
            return h;
        }

        void Pestana(Transform padre, Tab t, string texto, Vector2 pos, Color c)
        {
            var r = Rect(padre, "Pestana_" + texto, pos, new Vector2(116f, 66f));
            r.pivot = new Vector2(0.5f, 0f);
            var img = r.gameObject.AddComponent<Image>(); img.color = c;
            // ícono en vez de palabra: mochila = objetos, lupa = pistas, diana = objetivos
            var ic = Rect(r, "Icono", Vector2.zero, new Vector2(46f, 46f));
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            var ii = ic.gameObject.AddComponent<Image>(); ii.raycastTarget = false; ii.preserveAspect = true;
            ii.sprite = IconosCuaderno.Crear(t == Tab.Items ? IconosCuaderno.Icono.Mochila : t == Tab.Notas ? IconosCuaderno.Icono.Lupa : IconosCuaderno.Icono.Diana);
            pestanas[t] = img;
        }

        // ------------------------------------------------------------------ utilidades UI
        static void Limpiar(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }

        static RectTransform Rect(Transform padre, string n, Vector2 pos, Vector2 tam)
        {
            var go = new GameObject(n, typeof(RectTransform)); go.transform.SetParent(padre, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = tam;
            return r;
        }

        static Text Texto(Transform padre, string s, int tam, Color c, Vector2 pos, float ancho, TextAnchor al, float alto = 0f)
        {
            var r = Rect(padre, "Txt", pos, new Vector2(ancho, alto > 0 ? alto : tam * 1.6f));
            if (al == TextAnchor.UpperLeft) r.pivot = new Vector2(0.5f, 1f);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Fuentes.Maquina; t.text = s; t.fontSize = tam; t.color = c; t.alignment = al;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            return t;
        }

        static void Subrayar(RectTransform fila, float largo)
        {
            var l = Rect(fila, "Subrayado", new Vector2(-fila.sizeDelta.x * 0.5f + largo * 0.5f + 34f, -fila.sizeDelta.y * 0.42f), new Vector2(largo, 3f));
            l.gameObject.AddComponent<Image>().color = new Color(rojo.r, rojo.g, rojo.b, 0.75f);
        }

        static void Tachar(RectTransform fila)
        {
            var l = Rect(fila, "Tachado", new Vector2(-fila.sizeDelta.x * 0.1f, 0f), new Vector2(fila.sizeDelta.x * 0.75f, 2f));
            l.gameObject.AddComponent<Image>().color = new Color(rojo.r, rojo.g, rojo.b, 0.7f);
        }

        private void OnDestroy()
        {
            if (instance == this && canvas != null && canvas.gameObject.activeSelf) { OverlayBlocker.Unregister(); Pausa.Soltar(this); }
        }
    }
}
