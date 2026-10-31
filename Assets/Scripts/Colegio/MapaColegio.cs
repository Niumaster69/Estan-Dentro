using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EstanDentro.Colegio
{
    /// <summary>
    /// El plano del colegio (lo genera Editor/Mejoras/MapaColegio en Resources/Mapa/mapa_colegio.json) y las salas que
    /// Mateo ya pisó. El cuaderno lo dibuja en la pestaña MAPA, estilo Resident Evil: lo visitado coloreado, lo demás a
    /// lápiz, puertas cerradas en rojo y abiertas en azul, y dónde está Mateo. En el modo exploración se ve todo.
    /// Las salas visitadas duran toda la partida (también al ir y volver por los ductos); se borran al volver al menú.
    /// </summary>
    public static class MapaColegio
    {
        [Serializable] public class Piso { public string nombre; public float y; public string celdas; [NonSerialized] public byte[] c; }
        [Serializable] public class Sala { public int id; public string nombre; public int piso; public float cx, cz; public float px, py, pz; public int area; }
        [Serializable] class Datos { public float celda, x0, z0; public int w, h; public List<Piso> pisos; public List<Sala> salas; }

        static Datos datos;
        static readonly HashSet<int> visitadas = new HashSet<int>();
        public static event Action<Sala> OnSalaNueva;

        public static bool Hay => Cargar() != null;
        public static int W => datos.w;
        public static int H => datos.h;
        public static float Celda => datos.celda;
        public static IReadOnlyList<Piso> Pisos => datos.pisos;
        public static IReadOnlyList<Sala> Salas => datos.salas;
        public static bool Visitada(int id) => visitadas.Contains(id) || Exploracion.Activo;

        static Datos Cargar()
        {
            if (datos != null) return datos;
            var ta = Resources.Load<TextAsset>("Mapa/mapa_colegio");
            if (!ta) return null;
            datos = JsonUtility.FromJson<Datos>(ta.text);
            foreach (var p in datos.pisos) p.c = Convert.FromBase64String(p.celdas);
            return datos;
        }

        /// <summary>Planta más cercana a esa altura (los pies de Mateo).</summary>
        public static int PisoDe(float y)
        {
            if (Cargar() == null) return 0;
            int mejor = 0; float d = float.MaxValue;
            for (int i = 0; i < datos.pisos.Count; i++) { float dd = Mathf.Abs(y - 0.3f - datos.pisos[i].y); if (dd < d) { d = dd; mejor = i; } }
            return mejor;
        }

        /// <summary>Celda del plano (i, j) de un punto del mundo.</summary>
        public static Vector2 ACelda(Vector3 p) => new Vector2((p.x - datos.x0) / datos.celda, (p.z - datos.z0) / datos.celda);

        public static byte Valor(int piso, int i, int j) => i < 0 || j < 0 || i >= datos.w || j >= datos.h ? (byte)0 : datos.pisos[piso].c[j * datos.w + i];

        /// <summary>La sala donde está ese punto (o la más cercana a 60 cm si está en una puerta); -1 si ninguna.</summary>
        public static int SalaEn(Vector3 p)
        {
            if (Cargar() == null) return -1;
            int piso = PisoDe(p.y); var c = ACelda(p); int ci = Mathf.FloorToInt(c.x), cj = Mathf.FloorToInt(c.y);
            for (int r = 0; r <= 3; r++)
                for (int dj = -r; dj <= r; dj++) for (int di = -r; di <= r; di++)
                {
                    byte v = Valor(piso, ci + di, cj + dj);
                    if (v >= 3) return v - 3;
                }
            return -1;
        }

        public static Sala SalaPorId(int id) => Cargar() != null && id >= 0 ? datos.salas.Find(s => s.id == id) : null;

        public static void Visitar(int id)
        {
            if (id < 0 || !visitadas.Add(id)) return;
            var s = SalaPorId(id); if (s != null) OnSalaNueva?.Invoke(s);
        }

        public static void Olvidar() => visitadas.Clear();

        /// <summary>
        /// La planta pintada a lápiz sobre el papel del cuaderno: salas visitadas con un lavado de color (la actual más
        /// cálida), las no visitadas apenas sombreadas, paredes en tinta (más claras donde no se ha estado).
        /// </summary>
        public static Texture2D Textura(int piso, int salaActual, int salaElegida, out RectInt recorte)
        {
            var c = datos.pisos[piso].c; int w = datos.w, h = datos.h;
            // recorte: lo que tiene paredes o salas en esa planta, con 1 m de margen
            int i0 = w, j0 = h, i1 = 0, j1 = 0;
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                if (c[j * w + i] != 0) { if (i < i0) i0 = i; if (i > i1) i1 = i; if (j < j0) j0 = j; if (j > j1) j1 = j; }
            int m = Mathf.RoundToInt(1f / datos.celda);
            i0 = Mathf.Max(0, i0 - m); j0 = Mathf.Max(0, j0 - m); i1 = Mathf.Min(w - 1, i1 + m); j1 = Mathf.Min(h - 1, j1 + m);
            recorte = new RectInt(i0, j0, i1 - i0 + 1, j1 - j0 + 1);

            var tex = new Texture2D(recorte.width, recorte.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[recorte.width * recorte.height];
            var tinta = new Color32(40, 34, 28, 235); var tintaClara = new Color32(70, 62, 52, 90);
            var lavado = new Color32(110, 150, 190, 120); var actual = new Color32(214, 120, 70, 150); var elegida = new Color32(200, 60, 50, 170);
            var noVista = new Color32(90, 80, 70, 28); var puerta = new Color32(0, 0, 0, 0);
            for (int j = 0; j < recorte.height; j++)
                for (int i = 0; i < recorte.width; i++)
                {
                    int gi = i + i0, gj = j + j0; byte v = c[gj * w + gi];
                    Color32 col = new Color32(0, 0, 0, 0);
                    if (v == 1) col = CercaDeVisitada(piso, gi, gj) ? tinta : tintaClara;
                    else if (v == 2) col = puerta;
                    else if (v >= 3)
                    {
                        int id = v - 3;
                        col = id == salaElegida ? elegida : id == salaActual ? actual : Visitada(id) ? lavado : noVista;
                        // trama de lápiz suave sobre lo visitado
                        if (Visitada(id) && ((gi + gj) % 6 == 0)) col.a = (byte)Mathf.Min(255, col.a + 40);
                    }
                    px[j * recorte.width + i] = col;
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static bool CercaDeVisitada(int piso, int i, int j)
        {
            for (int dj = -2; dj <= 2; dj++) for (int di = -2; di <= 2; di++)
            { byte v = Valor(piso, i + di, j + dj); if (v >= 3 && Visitada(v - 3)) return true; }
            return false;
        }

        // ------------------------------------------------------------------ rastreo de lo visitado
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Instalar()
        {
            SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
            AlCargar(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void AlCargar(Scene s, LoadSceneMode modo)
        {
            if (s.name == "MainMenu") { Olvidar(); return; }
            if (s.name != "Colegio" || !Hay) return;
            if (!UnityEngine.Object.FindAnyObjectByType<Rastreador>()) new GameObject("__MapaRastreador").AddComponent<Rastreador>();
        }

        class Rastreador : MonoBehaviour
        {
            Player.PlayerController j; float prox;
            void Update()
            {
                if (Time.unscaledTime < prox) return;
                prox = Time.unscaledTime + 0.25f;
                if (!j) j = FindAnyObjectByType<Player.PlayerController>();
                if (j) Visitar(SalaEn(j.transform.position));
                if (visitadas.Count == 2 && !Exploracion.Activo) UI.GuiaTeclas.PrimeraVez("M", "Mapa del colegio");   // al salir del primer salón
            }
        }
    }
}
