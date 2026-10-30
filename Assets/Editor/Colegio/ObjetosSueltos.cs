using System.IO;
using UnityEditor;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Prefabs de los objetos que se recogen (Assets/Art/Objetos/Objetos.fbx, de exportar_objetos.py) en
    /// Resources/Objetos/&lt;id&gt;: los usa BotinVisible para que lo de adentro de casilleros, lonchera y cajones SE VEA y se coja.
    /// </summary>
    public static class ObjetosSueltos
    {
        const string FBX = "Assets/Art/Objetos/Objetos.fbx";
        const string MATS = "Assets/Art/Edificio/Materials";
        const string SALIDA = "Assets/Resources/Objetos";

        public static void PrepararBatch() { Preparar(); EditorApplication.Exit(0); }

        public static void Preparar()
        {
            var mi = AssetImporter.GetAtPath(FBX) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[Objetos] no está " + FBX); return; }
            // materiales: los mismos del colegio (por nombre)
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(FBX))
            {
                if (!(m is Material emb)) continue;
                var hdrp = AssetDatabase.LoadAssetAtPath<Material>($"{MATS}/{emb.name}.mat") ?? AssetDatabase.LoadAssetAtPath<Material>($"{MATS}/ED_{emb.name}.mat");
                if (hdrp) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), emb.name), hdrp);
                else Debug.LogWarning("[Objetos] sin material para " + emb.name);
            }
            mi.SaveAndReimport();
            Directory.CreateDirectory(SALIDA);
            var raiz = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
            int n = 0;
            foreach (Transform hijo in raiz.transform)
            {
                var mf = hijo.GetComponent<MeshFilter>(); var mr = hijo.GetComponent<MeshRenderer>();
                if (!mf || !mr) { Debug.Log($"[Objetos] {hijo.name}: sin malla directa (hijos {hijo.childCount})"); if (hijo.childCount > 0) { mf = hijo.GetComponentInChildren<MeshFilter>(); mr = hijo.GetComponentInChildren<MeshRenderer>(); } if (!mf || !mr) continue; }
                var go = new GameObject(hijo.name);
                var h = new GameObject("Malla", typeof(MeshFilter), typeof(MeshRenderer));
                h.transform.SetParent(go.transform, false);
                h.transform.localRotation = hijo.localRotation; h.transform.localScale = hijo.localScale;
                h.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                h.GetComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                PrefabUtility.SaveAsPrefabAsset(go, $"{SALIDA}/{hijo.name}.prefab");
                Object.DestroyImmediate(go); n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Objetos] {n} prefabs en {SALIDA}");
        }
    }
}
