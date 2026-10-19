using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// CANDADO EN PRIMERA PERSONA (Henry, 19-oct: "acercamiento de cámara, a nuestro modo, no otra pantalla").
    /// La cámara se acerca al candado 3D real; el mundo NO se pausa (sigue la oscuridad y el sonido).
    /// Las ruedas giran de verdad (36° por número); sobre cada rueda se ve su número, y tenues el anterior y el siguiente.
    /// Acierto: clic, el candado cae frente a ti, la cámara vuelve y se abre lo de adentro. Fallo: se sacude, "No abre".
    /// Controles: A/D rueda · W/S o rueda del mouse girar · números del teclado · E/Enter probar · Esc salir.
    /// </summary>
    public class InspeccionCandado : MonoBehaviour
    {
        static InspeccionCandado instancia;
        public static bool Abierta => instancia != null && instancia.activa;

        CombinationLock cerradura; CandadoVisual visual;
        Transform cam; Transform camPadre; Vector3 camPosLocal; Quaternion camRotLocal; float fovPrevio, cercaPrevio;
        Player.PlayerController jugador;
        readonly List<Transform> ruedas = new List<Transform>();
        int[] valores; int sel; bool activa, ocupada;
        Canvas lienzo; Text titulo, ayuda, estado;
        // los números van PEGADOS a cada rueda (un lienzo en el mundo por rueda), no flotando en la pantalla
        readonly List<(GameObject lienzo, Text actual, Text ant, Text sig, Color tinta)> numeros = new List<(GameObject, Text, Text, Text, Color)>();
        Light luz;

        public static void Abrir(CombinationLock cl, CandadoVisual cv)
        {
            if (instancia == null) { instancia = new GameObject("__InspeccionCandado").AddComponent<InspeccionCandado>(); instancia.ConstruirUI(); }
            if (instancia.activa || instancia.ocupada) return;
            instancia.StartCoroutine(instancia.Entrar(cl, cv));
        }

        // ------------------------------------------------------------------ entrar / salir
        IEnumerator Entrar(CombinationLock cl, CandadoVisual cv)
        {
            ocupada = true; cerradura = cl; visual = cv;
            var c = Camera.main; if (c == null) { ocupada = false; yield break; }
            cam = c.transform; camPadre = cam.parent; camPosLocal = cam.localPosition; camRotLocal = cam.localRotation; fovPrevio = c.fieldOfView;
            cercaPrevio = c.nearClipPlane;
            c.nearClipPlane = 0.02f;            // la lonchera se mira a 17 cm: con el plano normal (≈0,3 m) el candado se recortaba
            jugador = FindAnyObjectByType<Player.PlayerController>();
            if (jugador) jugador.InputEnabled = false;
            OverlayBlocker.Register();

            ruedas.Clear();
            ruedas.AddRange(cv.GetComponentsInChildren<DatosBlender>()
                .Where(d => d.Get("prop_tipo") == "rueda_candado")
                .OrderBy(d => int.TryParse(d.Get("indice"), out var i) ? i : 0).Select(d => d.transform));
            int n = Mathf.Max(1, cl.CorrectCode.Length);
            valores = new int[n];
            for (int i = 0; i < n; i++) valores[i] = Random.Range(0, 10);
            sel = 0;

            // pose: frente al candado (del lado del jugador), cerca según su tamaño
            var b = Limites(cv.gameObject);
            // el frente del candado es su cara delgada (no hacia donde esté parado el jugador), del lado del jugador
            Vector3 frente = cam.position - b.center;
            var mf = cv.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var tam = mf.sharedMesh.bounds.size;
                // de los 3 ejes locales, descartar el que apunta al cielo y quedarse con el más delgado de los otros dos
                var ejes = new[] { (cv.transform.right, tam.x * cv.transform.lossyScale.x), (cv.transform.up, tam.y * cv.transform.lossyScale.y), (cv.transform.forward, tam.z * cv.transform.lossyScale.z) };
                var vertical = ejes.OrderByDescending(e => Mathf.Abs(Vector3.Dot(e.Item1, Vector3.up))).First();
                Vector3 nrm = ejes.Where(e => e.Item1 != vertical.Item1).OrderBy(e => Mathf.Abs(e.Item2)).First().Item1;
                nrm.y = 0f;
                // del lado CONTRARIO a lo que el candado cierra (la puerta / el casillero / la lonchera quedan detrás)
                var host = cl.GetComponent<Renderer>();
                Vector3 desdeHost = host ? b.center - host.bounds.center : frente;
                if (nrm.sqrMagnitude > 1e-4f) frente = Vector3.Dot(nrm, desdeHost) >= 0f ? nrm : -nrm;
            }
            frente.y = 0f; frente.Normalize();
            float dist = Mathf.Clamp(Mathf.Max(b.size.x, b.size.y) * 3.2f, 0.17f, 0.45f);
            Vector3 destino = b.center + frente * dist + Vector3.up * dist * 0.18f;
            // que nada (la misma lonchera, la puerta) quede entre la cámara y el candado: si algo tapa, probar el otro lado y acercarse
            bool Tapado(Vector3 desde)
            {
                foreach (var h in Physics.RaycastAll(desde, (b.center - desde).normalized, Vector3.Distance(desde, b.center) - 0.01f, ~0, QueryTriggerInteraction.Ignore))
                    if (!h.transform.IsChildOf(cv.transform) && h.transform.root != cam.root) return true;
                return false;
            }
            if (Tapado(destino))
            {
                foreach (var prueba in new[] { b.center - frente * dist + Vector3.up * dist * 0.18f, b.center + frente * dist * 0.6f + Vector3.up * dist * 0.4f, b.center + Vector3.up * dist })
                    if (!Tapado(prueba)) { destino = prueba; break; }
            }
            Quaternion mira = Quaternion.LookRotation(b.center - destino, Vector3.up);
            Debug.Log($"[Inspeccion] {cl.name}: candado {b.center:F3} tam {b.size:F3} | dueño {(cl.GetComponent<Renderer>() ? cl.GetComponent<Renderer>().bounds.center.ToString("F3") : "-")} | frente {frente:F2} | cámara {destino:F3}");

            // una luz pequeña y cálida: Mateo acerca la cara (y la linterna) al candado
            luz = new GameObject("LuzInspeccion").AddComponent<Light>();
            luz.type = LightType.Point; luz.lightUnit = UnityEngine.Rendering.LightUnit.Lumen; luz.intensity = 40f; luz.range = 1.2f;
            luz.color = new Color(1f, 0.86f, 0.66f); luz.shadows = LightShadows.None;
            luz.transform.position = destino + Vector3.up * 0.12f + Vector3.Cross(Vector3.up, frente) * 0.1f;

            cam.SetParent(null, true);
            Vector3 p0 = cam.position; Quaternion r0 = cam.rotation;
            for (float t = 0f; t < 0.65f; t += Time.unscaledDeltaTime)
            {
                float k = Suave(t / 0.65f);
                cam.SetPositionAndRotation(Vector3.Lerp(p0, destino, k), Quaternion.Slerp(r0, mira, k));
                c.fieldOfView = Mathf.Lerp(fovPrevio, 42f, k);
                yield return null;
            }
            cam.SetPositionAndRotation(destino, mira);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            titulo.text = cl.NombreVisible + "   ·   " + cl.CorrectCode.Length + " ruedas";
            ayuda.text = "A / D  rueda      W / S  o rueda del mouse  girar      E  probar      Esc  dejarlo"
                       + (TienePata() ? "      R  forzar con la pata de cabra (hace ruido)" : "");
            lienzo.enabled = true; activa = true; ocupada = false;
            for (int i = 0; i < ruedas.Count && i < n; i++) ruedas[i].localRotation *= Quaternion.Euler(valores[i] * 36f, 0f, 0f);
            CrearNumeros(frente);
            Refrescar();
        }

        IEnumerator Salir(bool resuelto)
        {
            activa = false; ocupada = true; lienzo.enabled = false;
            foreach (var nu in numeros) if (nu.lienzo) Destroy(nu.lienzo);
            numeros.Clear();
            var c = cam.GetComponent<Camera>();
            if (resuelto)
            {
                if (visual) visual.Soltar();
                yield return new WaitForSecondsRealtime(0.9f);  // ver caer el candado (reloj real: no se congela si algo pausa)
            }
            Vector3 p0 = cam.position; Quaternion r0 = cam.rotation;
            cam.SetParent(camPadre, true);
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = Suave(t / 0.5f);
                cam.localPosition = Vector3.Lerp(cam.parent.InverseTransformPoint(p0), camPosLocal, k);
                cam.localRotation = Quaternion.Slerp(Quaternion.Inverse(cam.parent.rotation) * r0, camRotLocal, k);
                c.fieldOfView = Mathf.Lerp(42f, fovPrevio, k);
                yield return null;
            }
            cam.localPosition = camPosLocal; cam.localRotation = camRotLocal; c.fieldOfView = fovPrevio; c.nearClipPlane = cercaPrevio;
            if (luz) Destroy(luz.gameObject);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            if (jugador) jugador.InputEnabled = true;
            OverlayBlocker.Unregister();
            ocupada = false;
            if (resuelto && cerradura)
            {
                if (forzado) { cerradura.Forzar(); ObjectiveHUD.Notify("¡Crac! Eso se oyó en todo el piso…", 3f); }
                else cerradura.NotifyAttempt((int[])valores.Clone());   // abre lo de adentro / la puerta
            }
            forzado = false;
        }

        // ------------------------------------------------------------------ entrada
        void Update()
        {
            if (!activa) return;
            if (cerradura == null || cerradura.IsSolved) { StartCoroutine(Salir(false)); return; }   // se resolvió por otro lado
            var kb = Keyboard.current; var gp = Gamepad.current; var ms = Mouse.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame)) { StartCoroutine(Salir(false)); return; }
            if ((kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)) || (gp != null && gp.dpad.left.wasPressedThisFrame)) { sel = (sel - 1 + valores.Length) % valores.Length; Clic(); Refrescar(); }
            if ((kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)) || (gp != null && gp.dpad.right.wasPressedThisFrame)) { sel = (sel + 1) % valores.Length; Clic(); Refrescar(); }
            if ((kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) || (gp != null && gp.dpad.up.wasPressedThisFrame)) Girar(+1);
            if ((kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) || (gp != null && gp.dpad.down.wasPressedThisFrame)) Girar(-1);
            if (ms != null) { float w = ms.scroll.ReadValue().y; if (w > 0.1f) Girar(+1); else if (w < -0.1f) Girar(-1); }
            if (kb != null)
                for (int d = 0; d <= 9; d++)
                    if (kb[Digitos[d]].wasPressedThisFrame || kb[Numpad[d]].wasPressedThisFrame)
                    { Girar(((d - valores[sel]) % 10 + 10) % 10, sinRefresco: true); sel = (sel + 1) % valores.Length; Refrescar(); }
            if ((kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame)) Probar();
            if (((kb != null && kb.rKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame)) && TienePata()) Forzar();
        }

        static readonly Key[] Digitos = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] Numpad = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        void Girar(int pasos, bool sinRefresco = false)
        {
            valores[sel] = (valores[sel] + pasos + 100) % 10;
            if (sel < ruedas.Count) StartCoroutine(GirarRueda(ruedas[sel], pasos * 36f));
            Clic();
            if (!sinRefresco) Refrescar();
        }

        IEnumerator GirarRueda(Transform r, float grados)
        {
            Quaternion a = r.localRotation, b = a * Quaternion.Euler(grados, 0f, 0f);
            for (float t = 0f; t < 0.12f; t += Time.unscaledDeltaTime) { r.localRotation = Quaternion.Slerp(a, b, t / 0.12f); yield return null; }
            r.localRotation = b;
        }

        void Clic() { if (cerradura) cerradura.PlayDigitClick(); }

        /// <summary>Para la prueba automática: forzar el candado que se está mirando.</summary>
        public static void ForzarActual() { if (instancia != null) instancia.Forzar(); }

        static bool TienePata() { var inv = Inventory.Inventory.Instance; return inv != null && inv.HasItem("pata_de_cabra"); }

        /// <summary>Con la pata de cabra: el candado salta sin la clave, pero hace MUCHO ruido y asusta.</summary>
        public void Forzar()
        {
            if (!activa || cerradura == null) return;
            var inv = Inventory.Inventory.Instance;
            if (inv != null && !inv.Usar("pata_de_cabra")) return;
            EstanDentro.Audio.SonidoCorto.Juego("candado/forzar", visual ? visual.transform.position : cam.position, 1f, 0.6f);
            Ruido.Emitir(visual ? visual.transform.position : cam.position, 22f, "candado forzado con la pata de cabra");
            Stress.StressSystem.Instance?.Add(15f);
            forzado = true;
            StartCoroutine(Salir(true));
        }
        bool forzado;

        void Probar()
        {
            bool bien = cerradura.CorrectCode.Length == valores.Length;
            for (int i = 0; bien && i < valores.Length; i++) bien = cerradura.CorrectCode[i] == valores[i];
            if (bien) { StartCoroutine(Salir(true)); return; }
            cerradura.NotifyAttempt((int[])valores.Clone());          // cuenta el fallo
            EstanDentro.Audio.SonidoCorto.Juego("candado/no_abre", visual ? visual.transform.position : cam.position, 0.8f, 0.5f);
            estado.text = "No abre."; estado.color = new Color(0.85f, 0.25f, 0.2f);
            if (visual) StartCoroutine(Sacudir(visual.transform));
            CancelInvoke(nameof(LimpiarEstado)); Invoke(nameof(LimpiarEstado), 1.2f);
        }

        void LimpiarEstado() { if (estado) estado.text = ""; }

        IEnumerator Sacudir(Transform t)
        {
            Vector3 p0 = t.position;
            for (float k = 0f; k < 0.3f; k += Time.deltaTime) { t.position = p0 + t.right * Mathf.Sin(k * 80f) * 0.003f * (1f - k / 0.3f); yield return null; }
            t.position = p0;
        }

        // ------------------------------------------------------------------ números pegados a las ruedas
        void CrearNumeros(Vector3 frente)
        {
            foreach (var nu in numeros) if (nu.lienzo) Destroy(nu.lienzo);
            numeros.Clear();
            for (int i = 0; i < ruedas.Count && i < valores.Length; i++)
            {
                var r = ruedas[i];
                var wb = Limites(r.gameObject);
                float diametro = wb.size.y;
                // justo delante de la cara de la rueda que mira a la cámara
                float salto = Mathf.Abs(Vector3.Dot(wb.extents, new Vector3(Mathf.Abs(frente.x), Mathf.Abs(frente.y), Mathf.Abs(frente.z)))) + 0.0012f;
                var go = new GameObject("NumerosRueda" + i);
                go.transform.SetPositionAndRotation(wb.center + frente * salto, Quaternion.LookRotation(-frente, Vector3.up));
                var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
                var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(100f, 100f);
                go.transform.localScale = Vector3.one * (diametro / 100f);
                // número claro en ruedas oscuras (latón, maleta) y oscuro en ruedas claras (bronce de la cadena)
                var rr = r.GetComponent<Renderer>();
                Color col = rr && rr.sharedMaterial && rr.sharedMaterial.HasProperty("_BaseColor") ? rr.sharedMaterial.GetColor("_BaseColor") : Color.black;
                float lum = 0.3f * col.r + 0.59f * col.g + 0.11f * col.b;
                Color tinta = lum > 0.35f ? new Color(0.08f, 0.06f, 0.03f) : new Color(0.95f, 0.93f, 0.85f);
                Text Num(float y, int tam, float alfa)
                {
                    var t = new GameObject("n", typeof(RectTransform)).AddComponent<Text>();
                    t.transform.SetParent(go.transform, false);
                    var tr = (RectTransform)t.transform; tr.sizeDelta = new Vector2(100f, tam * 1.3f); tr.anchoredPosition = new Vector2(0f, y);
                    t.font = Fuentes.Maquina; t.fontSize = tam; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
                    t.horizontalOverflow = HorizontalWrapMode.Overflow; t.color = new Color(tinta.r, tinta.g, tinta.b, alfa);
                    return t;
                }
                // solo el número de enfrente, del tamaño del canto de la rueda (antes el de arriba y el de abajo se salían)
                Vector3 eje = r.transform.right;
                float ancho = Mathf.Abs(Vector3.Dot(wb.size, new Vector3(Mathf.Abs(eje.x), Mathf.Abs(eje.y), Mathf.Abs(eje.z))));
                int tam = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(40f, 0.8f * ancho * 100f / (0.62f * diametro))), 8, 40);
                numeros.Add((go, Num(0f, tam, 1f), Num(0f, 1, 0f), Num(0f, 1, 0f), tinta));
            }
        }

        void Refrescar()
        {
            for (int i = 0; i < numeros.Count && i < valores.Length; i++)
            {
                var nu = numeros[i];
                nu.actual.text = valores[i].ToString();
                nu.ant.text = ((valores[i] + 9) % 10).ToString();
                nu.sig.text = ((valores[i] + 1) % 10).ToString();
                nu.actual.color = i == sel ? new Color(1f, 0.78f, 0.25f) : nu.tinta;     // la rueda elegida, en ámbar
            }
        }

        void ConstruirUI()
        {
            DontDestroyOnLoad(gameObject);
            var go = new GameObject("Candado_Canvas"); go.transform.SetParent(transform, false);
            lienzo = go.AddComponent<Canvas>(); lienzo.renderMode = RenderMode.ScreenSpaceOverlay; lienzo.sortingOrder = 185;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            titulo = Texto("", 26, new Color(0.95f, 0.93f, 0.85f, 0.85f), new Vector2(0, 470), 1200);
            ayuda = Texto("A / D  rueda      W / S  o rueda del mouse  girar      E  probar      Esc  dejarlo", 20, new Color(0.95f, 0.93f, 0.85f, 0.6f), new Vector2(0, -490), 1500);
            estado = Texto("", 34, Color.white, new Vector2(0, -420), 800);
            lienzo.enabled = false;
        }

        Text Texto(string s, int tam, Color c, Vector2 pos, float ancho)
        {
            var r = new GameObject("Txt", typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(lienzo.transform, false); r.anchoredPosition = pos; r.sizeDelta = new Vector2(ancho, tam * 1.5f);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Fuentes.Maquina; t.text = s; t.fontSize = tam; t.color = c; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        static Bounds Limites(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.one * 0.05f);
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static float Suave(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
    }
}
