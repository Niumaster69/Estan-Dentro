using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Pantalla del candado. Dibuja el candado según su estilo (latón, maleta, cadena) para que cada uno
    /// se reconozca, con ruedas que muestran el número anterior y el siguiente como un candado real.
    /// Teclado: A/D o flechas cambian de rueda, W/S o rueda del mouse giran, Enter prueba, Esc sale.
    /// Mouse: clic en una rueda la selecciona; rueda del mouse la gira.
    /// </summary>
    public class LockOverlay : MonoBehaviour
    {
        private static LockOverlay instance;

        static readonly Color fondo = new Color(0.02f, 0.015f, 0.015f, 0.88f);
        static readonly Color crema = new Color(0.95f, 0.93f, 0.85f, 1f);
        static readonly Color ambar = new Color(0.85f, 0.70f, 0.28f, 1f);
        static readonly Color sangre = new Color(0.62f, 0.12f, 0.12f, 1f);

        struct Aspecto
        {
            public Color cuerpo, gancho, rueda, digito, ventana;
            public Vector2 tamCuerpo; public float grosorGancho, altoGancho, anchoGancho;
            public bool cadena; public string tipo;
        }

        static Aspecto AspectoDe(EstiloCandado e, int ruedas)
        {
            switch (e)
            {
                case EstiloCandado.Laton3:
                    return new Aspecto { tipo = "Candado de latón", cuerpo = new Color(0.72f, 0.55f, 0.22f), gancho = new Color(0.72f, 0.72f, 0.70f),
                        rueda = new Color(0.08f, 0.08f, 0.08f), digito = crema, ventana = new Color(0.25f, 0.18f, 0.06f),
                        tamCuerpo = new Vector2(420, 300), grosorGancho = 30, altoGancho = 190, anchoGancho = 260 };
                case EstiloCandado.Maleta3:
                    return new Aspecto { tipo = "Candadito de maleta", cuerpo = new Color(0.62f, 0.10f, 0.09f), gancho = new Color(0.12f, 0.12f, 0.12f),
                        rueda = new Color(0.10f, 0.10f, 0.10f), digito = crema, ventana = new Color(0.30f, 0.04f, 0.04f),
                        tamCuerpo = new Vector2(500, 200), grosorGancho = 16, altoGancho = 90, anchoGancho = 300 };
                case EstiloCandado.Cadena4:
                    return new Aspecto { tipo = "Candado de cadena", cuerpo = new Color(0.33f, 0.35f, 0.38f), gancho = new Color(0.62f, 0.63f, 0.65f),
                        rueda = new Color(0.66f, 0.50f, 0.20f), digito = new Color(0.08f, 0.06f, 0.03f), ventana = new Color(0.12f, 0.13f, 0.14f),
                        tamCuerpo = new Vector2(560, 340), grosorGancho = 44, altoGancho = 230, anchoGancho = 330, cadena = true };
                default:
                    return new Aspecto { tipo = "Candado", cuerpo = new Color(0.25f, 0.24f, 0.22f), gancho = new Color(0.6f, 0.6f, 0.6f),
                        rueda = new Color(0.1f, 0.1f, 0.1f), digito = crema, ventana = new Color(0.1f, 0.1f, 0.1f),
                        tamCuerpo = new Vector2(120 * ruedas + 140, 280), grosorGancho = 28, altoGancho = 170, anchoGancho = 240 };
            }
        }

        private Canvas canvas;
        private RectTransform raiz;          // todo lo que cambia según el candado
        private Text titulo, tipoTxt, estado;
        private Text[] actual, anterior, siguiente;
        private RectTransform[] ruedasRT;
        private Image[] marcos;
        private int[] valores;
        private int sel;
        private CombinationLock target;
        private float failHasta;
        private bool consumirEsteFrame;
        private CursorLockMode cursorPrevio;
        private bool cursorVisiblePrevio;

        public static bool Abierta => instance != null && instance.canvas.enabled;

        public static void Open(CombinationLock lockComp)
        {
            EnsureInstance();
            if (instance.canvas.enabled) return;
            instance.target = lockComp;
            instance.Construir(lockComp);
            EstanDentro.UI.Pausa.Pedir(instance);
            instance.canvas.enabled = true;
            instance.consumirEsteFrame = true;
            instance.cursorPrevio = Cursor.lockState; instance.cursorVisiblePrevio = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            instance.Refrescar();
            OverlayBlocker.Register();
        }

        public static void Close()
        {
            if (instance == null || !instance.canvas.enabled) return;
            instance.canvas.enabled = false;
            EstanDentro.UI.Pausa.Soltar(instance);
            Cursor.lockState = instance.cursorPrevio; Cursor.visible = instance.cursorVisiblePrevio;
            instance.target = null;
            OverlayBlocker.Unregister();
        }

        private void OnDestroy()
        {
            if (instance == this && canvas != null && canvas.enabled)
            {
                OverlayBlocker.Unregister();
                EstanDentro.UI.Pausa.Soltar(this);
            }
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("__LockOverlay");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LockOverlay>();
            instance.ConstruirLienzo();
        }

        // ------------------------------------------------------------------ entrada
        private void Update()
        {
            if (canvas == null || !canvas.enabled) return;
            if (consumirEsteFrame) { consumirEsteFrame = false; return; }
            var kb = Keyboard.current; var gp = Gamepad.current; var ms = Mouse.current;

            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame)) { Close(); return; }

            if ((kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) || (gp != null && gp.dpad.left.wasPressedThisFrame)) Mover(-1);
            if ((kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) || (gp != null && gp.dpad.right.wasPressedThisFrame)) Mover(+1);
            if ((kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) || (gp != null && gp.dpad.up.wasPressedThisFrame)) Girar(+1);
            if ((kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) || (gp != null && gp.dpad.down.wasPressedThisFrame)) Girar(-1);
            if (kb != null)
                for (int d = 0; d <= 9; d++)
                    if (kb[Digitos[d]].wasPressedThisFrame || kb[Numpad[d]].wasPressedThisFrame)
                    { valores[sel] = d; Clic(); Mover(+1); }

            if (ms != null)
            {
                float rueda = ms.scroll.ReadValue().y;
                if (rueda > 0.1f) Girar(+1); else if (rueda < -0.1f) Girar(-1);
                if (ms.leftButton.wasPressedThisFrame)
                    for (int i = 0; i < ruedasRT.Length; i++)
                        if (RectTransformUtility.RectangleContainsScreenPoint(ruedasRT[i], ms.position.ReadValue(), null)) { sel = i; Clic(); Refrescar(); }
            }

            if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame)) Probar();

            if (failHasta > 0f && Time.unscaledTime >= failHasta) { failHasta = 0f; Refrescar(); }
        }

        static readonly Key[] Digitos = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] Numpad = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        void Mover(int d) { sel = (sel + d + valores.Length) % valores.Length; Clic(); Refrescar(); }
        void Girar(int d) { valores[sel] = (valores[sel] + d + 10) % 10; Clic(); Refrescar(); }
        void Clic() { if (target != null) target.PlayDigitClick(); }

        void Probar()
        {
            if (target == null) return;
            var t = target;
            t.NotifyAttempt((int[])valores.Clone());
            if (t != null && !t.IsSolved)
            {
                estado.text = "No abre.";
                estado.color = sangre;
                failHasta = Time.unscaledTime + 0.9f;
                if (raiz) StartCoroutine(Sacudir());
            }
        }

        System.Collections.IEnumerator Sacudir()
        {
            float t = 0f; Vector2 p0 = raiz.anchoredPosition;
            while (t < 0.35f) { t += Time.unscaledDeltaTime; raiz.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 70f) * 10f * (1f - t / 0.35f), 0f); yield return null; }
            raiz.anchoredPosition = p0;
        }

        void Refrescar()
        {
            for (int i = 0; i < valores.Length; i++)
            {
                actual[i].text = valores[i].ToString();
                anterior[i].text = ((valores[i] + 9) % 10).ToString();
                siguiente[i].text = ((valores[i] + 1) % 10).ToString();
                marcos[i].color = i == sel ? ambar : new Color(0, 0, 0, 0);
            }
            if (failHasta <= 0f) { estado.color = crema; estado.text = "A / D  cambiar rueda      W / S  o rueda del mouse  girar      Enter  probar      Esc  salir"; }
        }

        // ------------------------------------------------------------------ dibujo
        void ConstruirLienzo()
        {
            var co = new GameObject("Lock_Canvas");
            co.transform.SetParent(transform, false);
            canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190;
            var scaler = co.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            co.AddComponent<GraphicRaycaster>();
            canvas.enabled = false;
            var bg = Rect(canvas.transform, "Fondo", Vector2.zero, Vector2.zero);
            bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.gameObject.AddComponent<Image>().color = fondo;
        }

        void Construir(CombinationLock cl)
        {
            if (raiz) Destroy(raiz.gameObject);
            int n = Mathf.Max(1, cl.CorrectCode.Length);
            var a = AspectoDe(cl.Estilo, n);
            raiz = Rect(canvas.transform, "Candado", Vector2.zero, new Vector2(1200, 900));

            titulo = Texto(raiz, cl.NombreVisible, Fuentes.Maquina, 40, crema, new Vector2(0, 390), new Vector2(1100, 60));
            tipoTxt = Texto(raiz, $"{a.tipo} · {n} ruedas", Fuentes.Maquina, 22, ambar, new Vector2(0, 345), new Vector2(1100, 34));

            float yCuerpo = -40f;
            // gancho (U invertida) detrás del cuerpo
            var g = Rect(raiz, "Gancho", new Vector2(0, yCuerpo + a.tamCuerpo.y * 0.5f + a.altoGancho * 0.5f - a.grosorGancho * 0.6f), new Vector2(a.anchoGancho, a.altoGancho));
            var gi = g.gameObject.AddComponent<Image>(); gi.sprite = SpriteGancho(a.anchoGancho, a.altoGancho, a.grosorGancho); gi.color = a.gancho;
            if (a.cadena)
                for (int k = -1; k <= 1; k += 2)
                    for (int e = 0; e < 4; e++)
                    {
                        var es = Rect(raiz, "Eslabon", new Vector2(k * (a.anchoGancho * 0.5f + 70 + e * 95), yCuerpo + a.tamCuerpo.y * 0.5f + a.altoGancho - 40 - e * 18), new Vector2(110, 62));
                        es.localRotation = Quaternion.Euler(0, 0, k * (8 + e * 4));
                        var ei = es.gameObject.AddComponent<Image>(); ei.sprite = SpriteAnillo(110, 62, 12); ei.color = a.gancho * 0.85f;
                    }
            // cuerpo
            var c = Rect(raiz, "Cuerpo", new Vector2(0, yCuerpo), a.tamCuerpo);
            var ci = c.gameObject.AddComponent<Image>(); ci.sprite = SpriteRedondeado((int)a.tamCuerpo.x, (int)a.tamCuerpo.y, a.cadena ? 22 : 46); ci.color = a.cuerpo;
            var brillo = Rect(c, "Brillo", new Vector2(0, a.tamCuerpo.y * 0.32f), new Vector2(a.tamCuerpo.x * 0.86f, a.tamCuerpo.y * 0.12f));
            var bi = brillo.gameObject.AddComponent<Image>(); bi.sprite = SpriteRedondeado(200, 30, 14); bi.color = new Color(1, 1, 1, 0.10f);

            // ventana con las ruedas
            float ancho = 92f, sep = 16f, alto = a.tamCuerpo.y * 0.62f;
            float total = n * ancho + (n - 1) * sep;
            var ven = Rect(c, "Ventana", new Vector2(0, -a.tamCuerpo.y * 0.04f), new Vector2(total + 30, alto + 20));
            var vi = ven.gameObject.AddComponent<Image>(); vi.sprite = SpriteRedondeado(200, 120, 14); vi.color = a.ventana;

            valores = new int[n]; actual = new Text[n]; anterior = new Text[n]; siguiente = new Text[n]; ruedasRT = new RectTransform[n]; marcos = new Image[n];
            for (int i = 0; i < n; i++)
            {
                valores[i] = Random.Range(0, 10);
                float x = -total * 0.5f + ancho * 0.5f + i * (ancho + sep);
                var m = Rect(ven, "Marco" + i, new Vector2(x, 0), new Vector2(ancho + 10, alto + 10));
                marcos[i] = m.gameObject.AddComponent<Image>(); marcos[i].sprite = SpriteRedondeado(100, 200, 12);
                var r = Rect(ven, "Rueda" + i, new Vector2(x, 0), new Vector2(ancho, alto));
                var ri = r.gameObject.AddComponent<Image>(); ri.sprite = SpriteRueda(96, 256); ri.color = a.rueda;
                ruedasRT[i] = r;
                anterior[i] = Texto(r, "", Fuentes.Maquina, 34, a.digito * new Color(1, 1, 1, 0.35f), new Vector2(0, alto * 0.32f), new Vector2(ancho, 50));
                actual[i] = Texto(r, "", Fuentes.Maquina, 66, a.digito, Vector2.zero, new Vector2(ancho, 80));
                siguiente[i] = Texto(r, "", Fuentes.Maquina, 34, a.digito * new Color(1, 1, 1, 0.35f), new Vector2(0, -alto * 0.32f), new Vector2(ancho, 50));
            }
            sel = 0;
            estado = Texto(raiz, "", Fuentes.Maquina, 20, crema, new Vector2(0, -400), new Vector2(1150, 40));
        }

        static RectTransform Rect(Transform padre, string nombre, Vector2 pos, Vector2 tam)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
            return rt;
        }

        static Text Texto(Transform padre, string s, Font f, int tam, Color col, Vector2 pos, Vector2 caja)
        {
            var rt = Rect(padre, "Txt", pos, caja);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = f; t.text = s; t.fontSize = tam; t.color = col; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        // sprites generados (sin texturas en el proyecto)
        static Sprite Hacer(int w, int h, System.Func<float, float, float> alfa)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = new Color(1, 1, 1, alfa(x + 0.5f, y + 0.5f));
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        static Sprite SpriteRedondeado(int w, int h, float r) => Hacer(w, h, (x, y) =>
        {
            float dx = Mathf.Max(Mathf.Max(r - x, x - (w - r)), 0f), dy = Mathf.Max(Mathf.Max(r - y, y - (h - r)), 0f);
            return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
        });

        static Sprite SpriteGancho(float w, float h, float g) => Hacer((int)w, (int)h, (x, y) =>
        {
            float rx = w * 0.5f - g * 0.5f, cy = h - w * 0.5f;
            float d;
            if (y > cy) { float dx = x - w * 0.5f, dy = y - cy; d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - rx); }
            else d = Mathf.Min(Mathf.Abs(x - g * 0.5f), Mathf.Abs(x - (w - g * 0.5f)));
            return Mathf.Clamp01(g * 0.5f - d + 0.5f);
        });

        static Sprite SpriteAnillo(float w, float h, float g) => Hacer((int)w, (int)h, (x, y) =>
        {
            float nx = (x - w * 0.5f) / (w * 0.5f - g * 0.5f), ny = (y - h * 0.5f) / (h * 0.5f - g * 0.5f);
            float d = Mathf.Abs(Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(w, h) * 0.5f;
            return Mathf.Clamp01(g * 0.5f - d + 0.5f);
        });

        // rueda: cilindro visto de frente (más oscuro arriba y abajo) con estrías
        static Sprite SpriteRueda(int w, int h) => Hacer(w, h, (x, y) =>
        {
            float v = (y / h) * 2f - 1f;
            float sombra = 1f - 0.55f * v * v;
            float estria = (Mathf.FloorToInt(y / 6f) % 2 == 0) ? 1f : 0.93f;
            return Mathf.Clamp01(sombra * estria);
        });
    }
}
