using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Saca los dibujos a lápiz de todos los objetos (DibujosObjetos) a una carpeta, para revisarlos sin jugar.</summary>
    public static class ProbarDibujos
    {
        public static void Batch()
        {
            var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-carpeta");
            string carpeta = i >= 0 ? a[i + 1] : "Dibujos"; Directory.CreateDirectory(carpeta);
            EditorSceneManager.OpenScene("Assets/Scenes/Colegio/Colegio.unity");
            var ids = new[] { "linterna", "destornillador", "baterias", "tiza", "moneda", "pata_de_cabra", "llaves", "papel", "radio", "brick_game", "galon" };
            var hoja = new Texture2D(256 * 4, 256 * 3, TextureFormat.RGBA32, false);
            var papel = new Color(0.93f, 0.90f, 0.82f); var px = new Color[hoja.width * hoja.height]; for (int k = 0; k < px.Length; k++) px[k] = papel; hoja.SetPixels(px);
            for (int n = 0; n < ids.Length; n++)
            {
                var s = EstanDentro.Inventory.DibujosObjetos.De(ids[n]);
                if (!s) { Debug.Log("[Dibujos] sin dibujo " + ids[n]); continue; }
                File.WriteAllBytes(Path.Combine(carpeta, ids[n] + ".png"), s.texture.EncodeToPNG());
                var t = s.texture; int ox = (n % 4) * 256, oy = (2 - n / 4) * 256;
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                { var c = t.GetPixel(x, y); hoja.SetPixel(ox + x, oy + y, Color.Lerp(papel, new Color(c.r, c.g, c.b), c.a)); }
                Debug.Log("[Dibujos] " + ids[n]);
            }
            hoja.Apply(); File.WriteAllBytes(Path.Combine(carpeta, "hoja.png"), hoja.EncodeToPNG());
            EditorApplication.Exit(0);
        }
    }
}
