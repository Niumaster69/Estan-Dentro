using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EstanDentro.Colegio;

namespace EstanDentro.EditorColegio
{
    /// <summary>Por qué no se alcanza un objeto: cuenta qué descarta cada lugar alrededor (mismo criterio que AutoPrueba).</summary>
    public static class DiagnosticoAlcance
    {
        public static void Batch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity", OpenSceneMode.Single);
            Physics.SyncTransforms();
            var objetivos = Object.FindObjectsByType<DatosBlender>(FindObjectsSortMode.None)
                .Where(d => d.Get("interactuable").StartsWith("carta") || d.Get("interactuable").StartsWith("lonchera")).ToList();
            foreach (var d in objetivos)
            {
                var r = d.GetComponent<Renderer>(); var b = r.bounds; var c = b.center;
                int sinSuelo = 0, otroPiso = 0, capsula = 0, lejos = 0, tapado = 0, ok = 0;
                string tapa = "", choca = "";
                for (float dx = -2.2f; dx <= 2.2f; dx += 0.1f)
                    for (float dz = -2.2f; dz <= 2.2f; dz += 0.1f)
                    {
                        float dd = Mathf.Sqrt(dx * dx + dz * dz); if (dd < 0.5f || dd > 2.2f) continue;
                        var pie = new Vector3(c.x + dx, 0, c.z + dz);
                        if (!Physics.Raycast(new Vector3(pie.x, c.y, pie.z), Vector3.down, out var s, 4f, ~0, QueryTriggerInteraction.Ignore) || s.normal.y < 0.9f) { sinSuelo++; continue; }
                        pie.y = s.point.y;
                        if (pie.y > b.min.y + 0.2f || pie.y < b.min.y - 3.6f) { otroPiso++; continue; }
                        var cols = Physics.OverlapCapsule(pie + Vector3.up * 0.4f, pie + Vector3.up * 1.65f, 0.35f, ~0, QueryTriggerInteraction.Ignore);
                        if (cols.Length > 0) { capsula++; if (choca.Length < 200) choca += cols[0].name + "(" + cols[0].GetType().Name + ") "; continue; }
                        var ojo = pie + Vector3.up * 1.65f;
                        if (Vector3.Distance(ojo, c) > 2.9f) { lejos++; continue; }
                        if (Physics.Linecast(ojo, c, out var h, ~0, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(d.transform) && h.transform != d.transform)
                        { tapado++; if (tapa.Length < 200) tapa += h.collider.name + " "; continue; }
                        ok++;
                    }
                var pup = Physics.OverlapBox(new Vector3(c.x, b.min.y - 0.05f, c.z), new Vector3(0.05f, 0.05f, 0.05f));
                Debug.Log($"[Alcance] {d.name} c={c:F2} minY={b.min.y:F2} | ok={ok} sinSuelo={sinSuelo} otroPiso={otroPiso} capsula={capsula} lejos={lejos} tapado={tapado}" +
                          $"\n   capsula choca con: {choca}\n   tapado por: {tapa}\n   debajo: {string.Join(",", pup.Select(x => x.name + "(" + x.GetType().Name + " " + x.bounds.max.y.ToString("F2") + ")"))}");
            }
            EditorApplication.Exit(0);
        }
    }
}
