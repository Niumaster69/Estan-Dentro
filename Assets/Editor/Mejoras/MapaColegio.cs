using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorMejoras
{
    /// <summary>
    /// El plano del colegio para el mapa del cuaderno (estilo Resident Evil) y el viaje del modo exploración.
    /// Recorre cada piso en una cuadrícula de 20 cm con la física de la escena: dónde hay piso, dónde hay pared (de 1 m a
    /// 2,4 m de alto: no cuenta muebles ni el dintel de las puertas) y dónde hay puerta. Las zonas de piso que separan las
    /// paredes y las puertas son las salas; el nombre sale de la rejilla que tiene cada sala (red_ductos.json).
    /// Queda en Resources/Mapa/mapa_colegio.json y lo lee Colegio/MapaColegio.cs.
    ///   Unity.exe -batchmode -projectPath . -executeMethod EstanDentro.EditorMejoras.MapaColegio.GenerarBatch
    /// </summary>
    public static class MapaColegio
    {
        const string ESCENA = "Assets/Scenes/Colegio/Colegio.unity";
        const string SALIDA = "Assets/Resources/Mapa/mapa_colegio.json";
        const float CELDA = 0.2f;
        // el colegio con su patio y la reja de afuera (más allá es la calle: no va en el mapa)
        static readonly Rect TERRENO = Rect.MinMaxRect(-8f, -9f, 56f, 56f);

        // nivel del piso de cada planta (P3 = 7,2 como en ArreglosColegio)
        static readonly (string nombre, float y)[] PISOS = { ("Sótano", -3.6f), ("Piso 1", 0f), ("Piso 2", 3.6f), ("Piso 3", 7.2f) };

        [MenuItem("Estan Dentro/Mejoras/Generar el mapa del colegio")]
        public static void GenerarYGuardar()
        {
            EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
            Generar();
        }

        public static void GenerarBatch()
        {
            try { GenerarYGuardar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Mapa] " + e); EditorApplication.Exit(1); }
        }

        /// <summary>Alturas donde hay piso (histograma de golpes hacia arriba): para revisar PISOS.</summary>
        public static void AlturasBatch()
        {
            EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var b = Limites();
            var hist = new Dictionary<float, int>();
            for (float x = b.xMin; x < b.xMax; x += 1f)
                for (float z = b.yMin; z < b.yMax; z += 1f)
                    foreach (var h in Physics.RaycastAll(new Vector3(x, 30f, z), Vector3.down, 50f, ~0, QueryTriggerInteraction.Ignore))
                        if (h.normal.y > 0.9f) { float k = Mathf.Round(h.point.y * 10f) / 10f; hist[k] = hist.TryGetValue(k, out var n) ? n + 1 : 1; }
            Debug.Log($"[Mapa] límites {b}");
            foreach (var kv in hist.OrderByDescending(k => k.Value).Take(14)) Debug.Log($"[Mapa] piso en y {kv.Key:F1}: {kv.Value}");
            EditorApplication.Exit(0);
        }

        [Serializable] public class Piso { public string nombre; public float y; public string celdas; }
        [Serializable] public class Sala { public int id; public string nombre; public int piso; public float cx, cz; public float px, py, pz; public int area; }
        [Serializable] public class Datos { public float celda, x0, z0; public int w, h; public List<Piso> pisos = new List<Piso>(); public List<Sala> salas = new List<Sala>(); }

        /// <summary>El rectángulo (x, z) del edificio con el patio: la caja de los colliders del edificio, con margen.</summary>
        static Rect Limites()
        {
            var cajas = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => !c.isTrigger && c.enabled && c.gameObject.activeInHierarchy)
                .Select(c => c.bounds).Where(bb => bb.size.x < 120f && bb.size.z < 120f && bb.center.y > -8f && bb.center.y < 14f).ToList();
            float x0 = cajas.Min(c => c.min.x), x1 = cajas.Max(c => c.max.x), z0 = cajas.Min(c => c.min.z), z1 = cajas.Max(c => c.max.z);
            return Rect.MinMaxRect(Mathf.Max(x0, -40f) - 1f, Mathf.Max(z0, -40f) - 1f, Mathf.Min(x1, 100f) + 1f, Mathf.Min(z1, 100f) + 1f);
        }

        public static void Generar()
        {
            Physics.SyncTransforms();
            var lim = TERRENO;
            int w = Mathf.CeilToInt(lim.width / CELDA), h = Mathf.CeilToInt(lim.height / CELDA);
            var datos = new Datos { celda = CELDA, x0 = lim.xMin, z0 = lim.yMin, w = w, h = h };

            // las hojas de las puertas no son pared (ni a 1 m ni arriba): en su lugar va el marcador de puerta
            var puertas = UnityEngine.Object.FindObjectsByType<Interaction.LockedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var deHoja = new HashSet<Collider>(puertas.SelectMany(p => p.GetComponentsInChildren<Collider>(true)));
            // lo que se mueve o se recoge tampoco es pared
            foreach (var i in UnityEngine.Object.FindObjectsByType<Interaction.Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!(i is Interaction.LockedDoor)) foreach (var c in i.GetComponentsInChildren<Collider>(true)) deHoja.Add(c);

            // la huella de cada puerta (cerrada) en su planta: separa las salas aunque la hoja no tenga collider
            var huellas = puertas.Select(pu =>
            {
                var rs = pu.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(pu.transform.position, Vector3.zero);
                var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                if (bb.size.x < bb.size.z) bb.size = new Vector3(Mathf.Max(bb.size.x, 0.3f), bb.size.y, bb.size.z); else bb.size = new Vector3(bb.size.x, bb.size.y, Mathf.Max(bb.size.z, 0.3f));
                return bb;
            }).Where(bb => bb.size.y > 1.5f && bb.size.y < 3.2f).ToList();
            bool EnPuerta(Vector3 p, float yp)
            {
                foreach (var bb in huellas)
                    if (Mathf.Abs(bb.min.y - yp) < 0.6f && p.x > bb.min.x && p.x < bb.max.x && p.z > bb.min.z && p.z < bb.max.z) return true;
                return false;
            }
            Debug.Log($"[Mapa] {huellas.Count} de {puertas.Length} puertas con huella");

            var nombres = Rejillas();
            var buf = new Collider[16];
            int idSala = 0;
            for (int ip = 0; ip < PISOS.Length; ip++)
            {
                float yp = PISOS[ip].y;
                var cel = new byte[w * h];      // 0 nada · 1 pared · 2 puerta · 3+ sala
                var piso = new bool[w * h];
                var alturaPiso = new float[w * h];
                int nPiso = 0, nPared = 0;
                for (int j = 0; j < h; j++)
                    for (int i = 0; i < w; i++)
                    {
                        var p = new Vector3(lim.xMin + (i + 0.5f) * CELDA, yp, lim.yMin + (j + 0.5f) * CELDA);
                        // pared: un collider alto (más de 2,3 m: muros, columnas) a la altura del zócalo, del pecho o de la cabeza;
                        // así las ventanas no la cortan y los muebles no cuentan
                        bool Choca(float alto, bool puerta)
                        {
                            int n = Physics.OverlapBoxNonAlloc(p + Vector3.up * alto, new Vector3(CELDA * 0.35f, 0.08f, CELDA * 0.35f), buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                            for (int k = 0; k < n; k++)
                            {
                                bool esHoja = deHoja.Contains(buf[k]) && buf[k].GetComponentInParent<Interaction.LockedDoor>(true);
                                if (puerta ? esHoja : !deHoja.Contains(buf[k]) && !buf[k].GetComponentInParent<Rigidbody>() && buf[k].bounds.size.y > 2.3f) return true;
                            }
                            return false;
                        }
                        int idx = j * w + i;
                        if (EnPuerta(p, yp)) { cel[idx] = 2; continue; }
                        if (Choca(1.0f, true)) { cel[idx] = 2; continue; }
                        if (Choca(0.3f, false) || Choca(1.2f, false) || Choca(2.0f, false)) { cel[idx] = 1; nPared++; continue; }
                        // piso: el primer golpe hacia abajo desde 1,6 m está a menos de 45 cm del nivel de la planta
                        // (debajo de pupitres y mesas también: se mira todo lo que hay en la vertical)
                        foreach (var hit in Physics.RaycastAll(p + Vector3.up * 1.6f, Vector3.down, 2.1f, ~0, QueryTriggerInteraction.Ignore))
                            if (Mathf.Abs(hit.point.y - yp) < 0.45f && hit.normal.y > 0.7f) { piso[idx] = true; alturaPiso[idx] = hit.point.y; nPiso++; break; }
                    }
                // salas: zonas de piso conectadas (las paredes y las puertas las separan)
                var sala = new int[w * h];
                for (int k = 0; k < sala.Length; k++) sala[k] = -1;
                var salasPiso = new List<(int id, List<int> celdas)>();
                for (int k = 0; k < sala.Length; k++)
                {
                    if (!piso[k] || sala[k] >= 0) continue;
                    var lista = new List<int>(); var cola = new Queue<int>(); cola.Enqueue(k); sala[k] = -2;
                    while (cola.Count > 0)
                    {
                        int c = cola.Dequeue(); lista.Add(c);
                        int ci = c % w, cj = c / w;
                        foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            int ni = ci + di, nj = cj + dj; if (ni < 0 || nj < 0 || ni >= w || nj >= h) continue;
                            int nk = nj * w + ni; if (!piso[nk] || sala[nk] != -1) continue;
                            sala[nk] = -2; cola.Enqueue(nk);
                        }
                    }
                    if (lista.Count < 40) { foreach (var c in lista) sala[c] = -3; continue; }   // menos de 1,6 m²: rincones entre muebles
                    foreach (var c in lista) sala[c] = idSala;
                    salasPiso.Add((idSala, lista)); idSala++;
                }
                foreach (var (id, celdas) in salasPiso)
                {
                    float cx = 0, cz = 0;
                    foreach (var c in celdas) { cx += c % w; cz += c / w; }
                    cx = lim.xMin + (cx / celdas.Count + 0.5f) * CELDA; cz = lim.yMin + (cz / celdas.Count + 0.5f) * CELDA;
                    // dónde aparecer: la celda más cercana al centro con 60 cm de sala alrededor y sin ningún mueble donde
                    // va el cuerpo de Mateo (cápsula de 35 cm de radio, de las rodillas a la cabeza)
                    int mejor = celdas[0]; float dMejor = float.MaxValue;
                    foreach (var c in celdas.OrderBy(c => { float x = c % w - (cx - lim.xMin) / CELDA, z = c / w - (cz - lim.yMin) / CELDA; return x * x + z * z; }))
                    {
                        int ci = c % w, cj = c / w; bool libre = true;
                        for (int dj = -3; dj <= 3 && libre; dj++) for (int di = -3; di <= 3 && libre; di++)
                        { int ni = ci + di, nj = cj + dj; libre = ni >= 0 && nj >= 0 && ni < w && nj < h && sala[nj * w + ni] == id; }
                        if (!libre) continue;
                        float x = lim.xMin + (ci + 0.5f) * CELDA, z = lim.yMin + (cj + 0.5f) * CELDA;
                        float yf = alturaPiso[c];
                        if (Physics.CheckCapsule(new Vector3(x, yf + 0.45f, z), new Vector3(x, yf + 1.7f, z), 0.35f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        // y el piso de verdad bajo los pies (no la tabla de un pupitre)
                        if (!Physics.Raycast(new Vector3(x, yf + 0.4f, z), Vector3.down, out var suelo, 0.6f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(suelo.point.y - yp) > 0.45f) continue;
                        mejor = c; dMejor = 0f; break;
                    }
                    if (dMejor > 0f) Debug.LogWarning($"[Mapa] sala {id} (piso {ip}) sin lugar libre para aparecer");
                    var s = new Sala
                    {
                        id = id, piso = ip, cx = cx, cz = cz, area = celdas.Count,
                        px = lim.xMin + (mejor % w + 0.5f) * CELDA, py = alturaPiso[mejor] + 0.05f, pz = lim.yMin + (mejor / w + 0.5f) * CELDA,
                    };
                    s.nombre = Nombrar(s, celdas, w, lim, nombres.Where(n => n.piso == ip).ToList(), sala);
                    datos.salas.Add(s);
                }
                for (int k = 0; k < cel.Length; k++) if (sala[k] >= 0) cel[k] = (byte)Mathf.Min(255, 3 + sala[k]);
                datos.pisos.Add(new Piso { nombre = PISOS[ip].nombre, y = yp, celdas = Convert.ToBase64String(cel) });
                Revision(cel, w, h, PISOS[ip].nombre);
                Debug.Log($"[Mapa] {PISOS[ip].nombre}: {nPiso} celdas de piso, {nPared} de pared, {salasPiso.Count} salas");
            }
            if (idSala > 252) Debug.LogWarning($"[Mapa] {idSala} salas: las de más de 252 se confunden en el mapa");
            Directory.CreateDirectory(Path.GetDirectoryName(SALIDA));
            File.WriteAllText(SALIDA, JsonUtility.ToJson(datos));
            AssetDatabase.ImportAsset(SALIDA);
            foreach (var s in datos.salas.OrderBy(s => s.piso).ThenByDescending(s => s.area))
                Debug.Log($"[Mapa] sala {s.id} piso {s.piso} «{s.nombre}» {s.area * CELDA * CELDA:F0} m² centro ({s.cx:F1}, {s.cz:F1})");
            Debug.Log($"[Mapa] listo: {w}×{h} celdas, {datos.salas.Count} salas → {SALIDA}");
        }

        /// <summary>Imagen de revisión de cada planta (fuera del proyecto, en Dev/mapa_revision): pared negra, puerta roja, salas de colores.</summary>
        static void Revision(byte[] cel, int w, int h, string nombre)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../mapa_revision"));
            Directory.CreateDirectory(dir);
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int k = 0; k < px.Length; k++)
            {
                byte v = cel[k];
                px[k] = v == 0 ? new Color32(235, 228, 210, 255) : v == 1 ? new Color32(30, 25, 20, 255) : v == 2 ? new Color32(200, 30, 30, 255)
                      : (Color32)Color.HSVToRGB((v * 0.137f) % 1f, 0.35f, 0.9f);
            }
            t.SetPixels32(px); t.Apply();
            File.WriteAllBytes(Path.Combine(dir, nombre.Replace(" ", "_") + ".png"), t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }

        /// <summary>Las rejillas de la escena con el nombre de su sala (red_ductos.json) y la planta.</summary>
        static List<(string nombre, int piso, Vector3 pos)> Rejillas()
        {
            var r = new List<(string, int, Vector3)>();
            string ruta = "Assets/Art/Edificio/red_ductos.json";
            if (!File.Exists(ruta)) return r;
            string txt = File.ReadAllText(ruta);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(txt, "\"(REJILLA_P(\\d)_\\d+)\": \\[\"([^\"]+)\""))
            {
                var go = GameObject.Find(m.Groups[1].Value);
                if (!go) continue;
                var rend = go.GetComponentInChildren<Renderer>();
                Vector3 p = rend ? rend.bounds.center : go.transform.position;
                r.Add((m.Groups[3].Value, int.Parse(m.Groups[2].Value), p));   // P1 = índice 1 en PISOS (0 es el sótano)
            }
            Debug.Log($"[Mapa] {r.Count} rejillas con nombre de sala");
            return r;
        }

        static string Nombrar(Sala s, List<int> celdas, int w, Rect lim, List<(string nombre, int piso, Vector3 pos)> rejillas, int[] sala)
        {
            // la rejilla que cae dentro de la sala (o a menos de 1 m de una de sus celdas)
            string mejor = null; float dMejor = 1.0f;
            foreach (var r in rejillas)
            {
                if (r.nombre == "Pasillo") continue;
                int ri = Mathf.FloorToInt((r.pos.x - lim.xMin) / CELDA), rj = Mathf.FloorToInt((r.pos.z - lim.yMin) / CELDA);
                for (int dj = -5; dj <= 5; dj++) for (int di = -5; di <= 5; di++)
                {
                    int ni = ri + di, nj = rj + dj; if (ni < 0 || nj < 0 || ni >= w || nj * w + ni >= sala.Length) continue;
                    if (sala[nj * w + ni] != s.id) continue;
                    float d = Mathf.Sqrt(di * di + dj * dj) * CELDA;
                    if (d < dMejor) { dMejor = d; mejor = r.nombre; }
                }
            }
            float m2 = celdas.Count * CELDA * CELDA;
            // el primer piso alrededor del patio central está abierto: corredor y patio son una sola zona
            if (s.piso == 1 && m2 > 1500f) return "Patio";
            if (s.piso == 1 && m2 > 400f) return "Patio central";
            if (mejor != null) return mejor;
            if (s.piso == 0) return "Sótano";
            // las escaleras van en las esquinas del edificio (48 × 36 m)
            bool esquina = (s.cx < 9f || s.cx > 39f) && (s.cz < 6.5f || s.cz > 29.5f);
            if (esquina && m2 < 50f) return "Escalera";
            if (s.cz > 38f || s.cz < -2f || s.cx < -2f || s.cx > 50f) return "Caseta del patio";
            // sin rejilla: largo y angosto es pasillo
            int i0 = celdas.Min(c => c % w), i1 = celdas.Max(c => c % w), j0 = celdas.Min(c => c / w), j1 = celdas.Max(c => c / w);
            float largo = Mathf.Max(i1 - i0, j1 - j0) * CELDA, ancho = Mathf.Min(i1 - i0, j1 - j0) * CELDA;
            if (largo > 3.5f * Mathf.Max(ancho, 0.5f) || celdas.Count * CELDA * CELDA > 120f) return "Pasillo";
            if (ancho < 3.2f && largo < 5f) return "Escalera";
            return "Sala";
        }
    }
}
