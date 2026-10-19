using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.Interaction;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// Mantener Q (o LB): Mateo respira hondo y lo que se puede usar cerca brilla unos segundos (nuestro "Revelio").
    /// Con el estrés alto no funciona: el miedo nubla. Atado al sistema de respiración y estrés.
    /// </summary>
    public class Concentrarse : MonoBehaviour
    {
        public float mantener = 0.7f;          // segundos de respiración antes de revelar
        public float radio = 7f;
        public float duracion = 2.8f;
        public float espera = 6f;              // entre usos
        public float estresMaximo = 0.6f;      // normalizado; por encima no puedes concentrarte

        Camera cam;
        Canvas lienzo;
        Image velo;
        Sprite punto;
        float cargando, listoEn;
        readonly List<(Transform t, Vector3 centro, Image img)> marcas = new List<(Transform, Vector3, Image)>();

        void Start()
        {
            cam = GetComponentInChildren<Camera>() ?? Camera.main;
            var go = new GameObject("Concentrarse_Canvas");
            go.transform.SetParent(transform, false);
            lienzo = go.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = 80;
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            var v = new GameObject("Velo", typeof(RectTransform)).GetComponent<RectTransform>();
            v.SetParent(lienzo.transform, false); v.anchorMin = Vector2.zero; v.anchorMax = Vector2.one; v.offsetMin = v.offsetMax = Vector2.zero;
            velo = v.gameObject.AddComponent<Image>(); velo.color = new Color(0.02f, 0.01f, 0.01f, 0f); velo.raycastTarget = false;
            punto = CrearPunto(64);
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            ActualizarMarcas();
            if (OverlayBlocker.IsBlocking || Time.time < listoEn) { cargando = 0f; Velo(0f); return; }
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool q = (kb != null && kb.qKey.isPressed) || (gp != null && gp.leftShoulder.isPressed);
            if (!q) { cargando = 0f; Velo(0f); return; }
            cargando += Time.deltaTime;
            Velo(Mathf.Clamp01(cargando / mantener) * 0.35f);
            if (cargando < mantener) return;
            cargando = 0f; listoEn = Time.time + espera;
            var estres = Stress.StressSystem.Instance;
            if (estres != null && estres.Normalized > estresMaximo)
            {
                ObjectiveHUD.Notify("No puedo concentrarme… primero tengo que calmarme.", 3f);
                return;
            }
            StartCoroutine(Revelar());
        }

        void Velo(float a) { if (velo) velo.color = new Color(velo.color.r, velo.color.g, velo.color.b, a); }

        IEnumerator Revelar()
        {
            foreach (var m in marcas) if (m.img) Destroy(m.img.gameObject);
            marcas.Clear();
            var vistos = new HashSet<GameObject>();
            foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                if (!it.isActiveAndEnabled || !vistos.Add(it.gameObject)) continue;
                if (it is LockedDoor puerta && !puerta.IsLocked) continue;        // puertas normales no: solo las cerradas
                if (it is CombinationLock cl && cl.IsSolved) continue;
                var r = it.GetComponentInChildren<Renderer>();
                Vector3 c = r ? r.bounds.center : it.transform.position;
                if (Vector3.Distance(c, cam.transform.position) > radio) continue;
                if (Physics.Linecast(cam.transform.position, c, out var h, ~0, QueryTriggerInteraction.Ignore)
                    && !h.transform.IsChildOf(it.transform) && h.transform.root != transform.root) continue;   // tapado
                var img = new GameObject("Marca", typeof(RectTransform)).AddComponent<Image>();
                img.transform.SetParent(lienzo.transform, false);
                img.sprite = punto; img.raycastTarget = false;
                ((RectTransform)img.transform).sizeDelta = new Vector2(70, 70);
                marcas.Add((it.transform, c, img));
            }
            float t = 0f;
            while (t < duracion)
            {
                t += Time.deltaTime;
                float a = t < 0.4f ? t / 0.4f : 1f - Mathf.Clamp01((t - duracion + 1f) / 1f);
                float pulso = 1f + 0.15f * Mathf.Sin(t * 6f);
                foreach (var m in marcas)
                    if (m.img) { m.img.color = new Color(0.95f, 0.80f, 0.40f, 0.85f * a); m.img.transform.localScale = Vector3.one * pulso; }
                yield return null;
            }
            foreach (var m in marcas) if (m.img) Destroy(m.img.gameObject);
            marcas.Clear();
        }

        void ActualizarMarcas()
        {
            if (cam == null || marcas.Count == 0) return;
            var rt = (RectTransform)lienzo.transform;
            foreach (var m in marcas)
            {
                if (!m.img) continue;
                Vector3 s = cam.WorldToScreenPoint(m.centro);
                m.img.enabled = s.z > 0f;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, s, null, out var p);
                ((RectTransform)m.img.transform).anchoredPosition = p;
            }
        }

        static Sprite CrearPunto(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float halo = Mathf.Clamp01(1f - d); halo *= halo;
                    float anillo = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) * 14f);
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Max(halo * 0.6f, anillo));
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
