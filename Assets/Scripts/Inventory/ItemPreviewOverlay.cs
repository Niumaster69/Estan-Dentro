using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using EstanDentro.UI;

namespace EstanDentro.Inventory
{
    /// <summary>
    /// Dos trabajos detrás del CUADERNO DE MATEO (InventoryOverlay):
    ///   1) La fila de "objeto nuevo": la primera vez que se recoge algo, abre el cuaderno en la hoja de ese objeto
    ///      con el sello NUEVO, cuando no haya otra nota o candado abiertos.
    ///   2) El visor 3D: pinta el objeto real girando en una RenderTexture, con su propia luz y exposición,
    ///      lejos de la escena (y = -10000) para que no lo afecte la noche del colegio.
    /// </summary>
    public class ItemPreviewOverlay : MonoBehaviour
    {
        private static ItemPreviewOverlay instance;
        const int CAPA_PREVIEW = 31;
        public static readonly Color FondoPapel = new Color(0.90f, 0.86f, 0.76f);

        private RenderTexture rt;
        private Transform mundo, pivote;
        private readonly Queue<(string id, string nombre)> fila = new Queue<(string, string)>();
        private Inventory suscrito;
        private string modeloActual;

        // ------------------------------------------------------------------ API
        public static RenderTexture Vista { get { Asegurar(); return instance.rt; } }

        /// <summary>Mostrar un objeto ya conocido (desde otros menús).</summary>
        public static void Show(string itemId, string displayName, Action onClose = null) => InventoryOverlay.AbrirObjeto(itemId, false, onClose);

        /// <summary>Objeto recién recogido: espera su turno y abre el cuaderno con el sello NUEVO.</summary>
        public static void Nuevo(string itemId, string displayName) { Asegurar(); instance.fila.Enqueue((itemId, displayName)); }

        public static void Close() => InventoryOverlay.Close();

        /// <summary>Pone el modelo del objeto en el visor (o lo apaga con id vacío). Devuelve false si no hay modelo.</summary>
        public static bool MostrarModelo(string id)
        {
            Asegurar();
            if (string.IsNullOrEmpty(id)) { instance.mundo.gameObject.SetActive(false); instance.modeloActual = null; return false; }
            instance.mundo.gameObject.SetActive(true);
            if (instance.modeloActual == id) return instance.pivote.childCount > 0;
            instance.modeloActual = id;
            return instance.PonerModelo(id);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Asegurar()
        {
            if (instance != null) return;
            instance = new GameObject("__CuadernoVisor").AddComponent<ItemPreviewOverlay>();
        }

        // ------------------------------------------------------------------ ciclo
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            rt = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            rt.Create();
            ConstruirMundo();
            mundo.gameObject.SetActive(false);
        }

        private void Update()
        {
            var inv = Inventory.Instance;
            if (inv != suscrito)
            {
                if (suscrito != null) suscrito.OnObjetoNuevo -= Nuevo;
                suscrito = inv;
                if (suscrito != null) suscrito.OnObjetoNuevo += Nuevo;
            }
            if (fila.Count > 0 && !OverlayBlocker.IsBlocking && !InventoryOverlay.EstaAbierto)
            {
                var (id, _) = fila.Dequeue();
                InventoryOverlay.AbrirObjeto(id, true, null);
            }
            if (pivote && mundo.gameObject.activeSelf) pivote.Rotate(Vector3.up, 35f * Time.unscaledDeltaTime, Space.World);
        }

        // ------------------------------------------------------------------ modelo
        bool PonerModelo(string id)
        {
            foreach (Transform h in pivote) Destroy(h.gameObject);
            pivote.localRotation = Quaternion.Euler(-16f, 0f, 0f);
            GameObject modelo = null;
            foreach (var d in FindObjectsByType<Colegio.DatosBlender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (d.Get("prop_tipo") != id) continue;
                var mf = d.GetComponent<MeshFilter>(); var mr = d.GetComponent<MeshRenderer>();
                if (!mf || !mr || !mf.sharedMesh) continue;
                modelo = new GameObject("Modelo");
                modelo.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                modelo.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                modelo.transform.rotation = d.transform.rotation * Quaternion.Inverse(Quaternion.Euler(0f, d.transform.eulerAngles.y, 0f));
                break;
            }
            if (modelo == null && id == "destornillador") modelo = Destornillador();
            if (modelo == null) return false;

            foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CAPA_PREVIEW;
            modelo.transform.SetParent(pivote, false);
            var rs = modelo.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float tam = Mathf.Max(b.size.x, b.size.y, b.size.z);
            modelo.transform.localScale *= tam > 1e-4f ? 1.0f / tam : 1f;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            modelo.transform.position += pivote.position - b.center;
            return true;
        }

        GameObject Destornillador()
        {
            var raiz = new GameObject("Destornillador");
            GameObject Parte(PrimitiveType t, Vector3 pos, Vector3 esc, Color c, bool metal)
            {
                var g = GameObject.CreatePrimitive(t);
                Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(raiz.transform, false);
                g.transform.localPosition = pos; g.transform.localScale = esc;
                g.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                // el material por defecto de las primitivas no existe en el ejecutable (sale fucsia): se copia uno HDRP/Lit del colegio
                var r = g.GetComponent<Renderer>(); var m = new Material(MaterialBase() ?? r.sharedMaterial);
                foreach (var mapa in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap" })
                    if (m.HasProperty(mapa)) m.SetTexture(mapa, null);
                if (m.HasProperty("_EmissiveColor")) m.SetColor("_EmissiveColor", Color.black);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", metal ? 0.75f : 0.45f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal ? 1f : 0f);
                r.sharedMaterial = m;
                return g;
            }
            Parte(PrimitiveType.Cylinder, new Vector3(-0.32f, 0, 0), new Vector3(0.13f, 0.2f, 0.13f), new Color(0.55f, 0.07f, 0.05f), false);
            Parte(PrimitiveType.Cylinder, new Vector3(0.13f, 0, 0), new Vector3(0.035f, 0.26f, 0.035f), new Color(0.72f, 0.72f, 0.74f), true);
            var punta = Parte(PrimitiveType.Cube, new Vector3(0.41f, 0, 0), new Vector3(0.03f, 0.06f, 0.012f), new Color(0.72f, 0.72f, 0.74f), true);
            punta.transform.localRotation = Quaternion.identity;
            raiz.transform.rotation = Quaternion.Euler(0f, 0f, 25f);
            return raiz;
        }

        static Material materialBase;
        static Material MaterialBase()
        {
            if (materialBase) return materialBase;
            foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var m = mr.sharedMaterial;
                if (m != null && m.shader != null && m.shader.name == "HDRP/Lit") { materialBase = m; break; }
            }
            return materialBase;
        }

        // ------------------------------------------------------------------ mundo del visor
        void ConstruirMundo()
        {
            mundo = new GameObject("CuadernoVisor_Mundo").transform;
            mundo.position = new Vector3(0f, -10000f, 0f);
            DontDestroyOnLoad(mundo.gameObject);
            pivote = new GameObject("Pivote").transform;
            pivote.SetParent(mundo, false);

            var cg = new GameObject("Camara"); cg.layer = CAPA_PREVIEW;
            cg.transform.SetParent(mundo, false);
            cg.transform.localPosition = new Vector3(0f, 0.12f, -2.3f);
            cg.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);
            var cam = cg.AddComponent<Camera>();
            cam.targetTexture = rt; cam.fieldOfView = 32f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 20f;
            cam.cullingMask = 1 << CAPA_PREVIEW;
            var hd = cg.AddComponent<HDAdditionalCameraData>();
            hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            // fondo del color del papel: el objeto queda "apoyado" en la hoja del cuaderno
            hd.backgroundColorHDR = FondoPapel.linear * 3.2f;
            hd.volumeLayerMask = 1 << CAPA_PREVIEW;

            var vg = new GameObject("Volumen"); vg.layer = CAPA_PREVIEW; vg.transform.SetParent(mundo, false);
            var vol = vg.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 100f;
            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            var exp = perfil.Add<Exposure>(true); exp.mode.Override(ExposureMode.Fixed); exp.fixedExposure.Override(5.5f);
            var fog = perfil.Add<Fog>(true); fog.enabled.Override(false);
            var tm = perfil.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.None);
            vol.sharedProfile = perfil;

            Luz(new Vector3(1.2f, 1.6f, -1.4f), 700f, new Color(1f, 0.93f, 0.82f));
            Luz(new Vector3(-1.5f, 0.3f, -1.0f), 260f, new Color(0.85f, 0.85f, 0.9f));
            Luz(new Vector3(0f, 1.2f, 1.5f), 220f, new Color(1f, 0.9f, 0.75f));
        }

        void Luz(Vector3 pos, float lumen, Color c)
        {
            var g = new GameObject("Luz"); g.layer = CAPA_PREVIEW;
            g.transform.SetParent(mundo, false); g.transform.localPosition = pos;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = 5f;
            l.lightUnit = LightUnit.Lumen; l.intensity = lumen; l.shadows = LightShadows.None;
        }

        private void OnDestroy() { if (rt != null) rt.Release(); }
    }
}
