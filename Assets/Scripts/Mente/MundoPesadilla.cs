using System.Collections.Generic;
using UnityEngine;

namespace EstanDentro.Mente
{
    /// <summary>
    /// El colegio en la Pesadilla: cada material del edificio cambia por su versión dañada (texturas F2 de materiales.json:
    /// pintura que se cae, humedad, óxido) y las luces bajan y se enfrían. Se dispara dentro del destello de la transición
    /// (EstadoMente.OnCambioMundo). Las parejas normal → pesadilla las arma ArreglosColegio.MaterialesPesadilla.
    /// </summary>
    public class MundoPesadilla : MonoBehaviour
    {
        public Material[] normales;
        public Material[] pesadillas;
        public Color luzPesadilla = new Color(0.45f, 0.75f, 0.78f);
        public float luzFactor = 0.4f;

        readonly List<(Renderer r, Material[] antes)> cambiados = new List<(Renderer, Material[])>();
        readonly List<(Light l, Color c, float i)> luces = new List<(Light, Color, float)>();
        Dictionary<Material, Material> mapa;
        bool enPesadilla;

        readonly List<SoloEnPesadilla> solo = new List<SoloEnPesadilla>();

        void Awake() { EstadoMente.Asegurar(); }

        void Start()
        {
            solo.AddRange(FindObjectsByType<SoloEnPesadilla>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var o in solo) o.gameObject.SetActive(o.alReves);
        }
        void OnEnable() { EstadoMente.OnCambioMundo += Cambiar; }
        void OnDisable() { EstadoMente.OnCambioMundo -= Cambiar; }

        void Cambiar(bool pesadilla)
        {
            if (pesadilla == enPesadilla) return;
            enPesadilla = pesadilla;
            foreach (var o in solo) if (o) o.gameObject.SetActive(pesadilla != o.alReves);
            if (mapa == null) { mapa = new Dictionary<Material, Material>(); for (int i = 0; i < normales.Length && i < pesadillas.Length; i++) if (normales[i] && pesadillas[i]) mapa[normales[i]] = pesadillas[i]; }
            if (pesadilla)
            {
                cambiados.Clear(); luces.Clear();
                foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var ms = r.sharedMaterials; bool alguno = false;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] && mapa.TryGetValue(ms[i], out var p)) { ms[i] = p; alguno = true; }
                    if (!alguno) continue;
                    cambiados.Add((r, r.sharedMaterials)); r.sharedMaterials = ms;
                }
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.GetComponentInParent<Interaction.Flashlight>() || l.type == LightType.Directional) continue;
                    luces.Add((l, l.color, l.intensity));
                    l.color = Color.Lerp(l.color, luzPesadilla, 0.75f); l.intensity *= luzFactor;
                }
            }
            else
            {
                foreach (var (r, antes) in cambiados) if (r) r.sharedMaterials = antes;
                foreach (var (l, c, i) in luces) if (l) { l.color = c; l.intensity = i; }
                cambiados.Clear(); luces.Clear();
            }
        }
    }
}
