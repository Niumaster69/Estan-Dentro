using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EstanDentro.UI;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// T (o Y en el mando): con tiza, Mateo marca con una X la pared o el piso que mira, para saber por dónde ya pasó
    /// (perderse con sentido en pasillos que se repiten). Cada tiza da 6 marcas; quedan para siempre en esa partida.
    /// </summary>
    public class MarcaTiza : MonoBehaviour
    {
        public const int MarcasPorTiza = 6;
        public int maximoMarcas = 80;
        int restantes;
        Camera cam;
        Material material;
        readonly Queue<GameObject> marcas = new Queue<GameObject>();

        void Update()
        {
            if (OverlayBlocker.IsBlocking) return;
            var kb = Keyboard.current; var gp = Gamepad.current;
            if (!((kb != null && kb.tKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame))) return;
            GuiaTeclas.Usada("T");
            IntentarMarcar();
        }

        public int MarcasHechas => marcas.Count;

        /// <summary>Marca lo que se está mirando (también lo usa la prueba automática).</summary>
        public void IntentarMarcar()
        {
            if (cam == null) cam = Camera.main;
            var inv = Inventory.Inventory.Instance;
            if (inv == null || cam == null) return;
            if (restantes <= 0)
            {
                if (!inv.HasItem("tiza")) { ObjectiveHUD.Notify("No tengo tiza.", 1.6f); return; }
                inv.RemoveItem("tiza"); restantes = MarcasPorTiza;
            }
            if (!Golpe(out var h)) { ObjectiveHUD.Notify("Muy lejos para marcar.", 1.4f); return; }
            Marcar(h.point, h.normal);
            EstanDentro.Audio.SonidoCorto.Juego("objetos/tiza", h.point, 0.7f, 0.6f);
            restantes--;
            if (restantes == 0 && !inv.HasItem("tiza")) ObjectiveHUD.Notify("Se acabó la tiza.", 2f);
        }

        bool Golpe(out RaycastHit mejor)
        {
            mejor = default; float d = float.MaxValue;
            foreach (var h in Physics.RaycastAll(cam.transform.position, cam.transform.forward, 2.6f, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.transform.root != cam.transform.root && h.distance < d) { d = h.distance; mejor = h; }
            return d < float.MaxValue;
        }

        void Marcar(Vector3 punto, Vector3 normal)
        {
            if (material == null) material = CrearMaterial();
            var raiz = new GameObject("MarcaTiza");
            // de frente a la superficie, con el "arriba" de la marca hacia arriba (o hacia adelante si es el piso)
            Vector3 arriba = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.8f ? Vector3.ProjectOnPlane(cam.transform.forward, normal) : Vector3.up;
            raiz.transform.SetPositionAndRotation(punto + normal * 0.004f, Quaternion.LookRotation(-normal, arriba));
            float tam = Random.Range(0.26f, 0.34f);
            Trazo(raiz.transform, 45f + Random.Range(-8f, 8f), tam);
            Trazo(raiz.transform, -45f + Random.Range(-8f, 8f), tam * Random.Range(0.85f, 1.05f));
            marcas.Enqueue(raiz);
            while (marcas.Count > maximoMarcas) Destroy(marcas.Dequeue());
        }

        void Trazo(Transform padre, float angulo, float largo)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(g.GetComponent<Collider>());
            g.name = "Trazo";
            g.transform.SetParent(padre, false);
            g.transform.localRotation = Quaternion.Euler(0f, 0f, angulo);
            g.transform.localPosition = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f), 0f);
            g.transform.localScale = new Vector3(largo, Random.Range(0.022f, 0.03f), 1f);
            g.GetComponent<Renderer>().sharedMaterial = material;
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material CrearMaterial()
        {
            Material baseM = null;
            foreach (var mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (mr.sharedMaterial != null && mr.sharedMaterial.shader != null && mr.sharedMaterial.shader.name == "HDRP/Lit") { baseM = mr.sharedMaterial; break; }
            var m = baseM != null ? new Material(baseM) : new Material(Shader.Find("HDRP/Lit"));
            foreach (var mapa in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_EmissiveColorMap" })
                if (m.HasProperty(mapa)) m.SetTexture(mapa, null);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.86f, 0.85f, 0.81f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_EmissiveColor")) m.SetColor("_EmissiveColor", Color.black);
            m.doubleSidedGI = true;
            return m;
        }
    }
}
