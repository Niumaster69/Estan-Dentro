using UnityEditor;
using UnityEngine;
using EstanDentro.Colegio;

namespace EstanDentro.EditorColegio
{
    /// <summary>
    /// Configura la importación de los FBX que vienen de Blender (Assets/Art/Edificio) y guarda en cada objeto
    /// sus propiedades personalizadas (funcion, interactuable, prop_tipo, bisagra_sentido, ducto_salida...).
    /// </summary>
    public class ColegioPostprocesador : AssetPostprocessor
    {
        const string CARPETA = "Assets/Art/Edificio/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(CARPETA)) return;
            var mi = (ModelImporter)assetImporter;
            mi.bakeAxisConversion = true;           // Blender Z-arriba -> Unity Y-arriba sin rotaciones raras
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.importNormals = ModelImporterNormals.Import;
            mi.isReadable = false;
            mi.generateSecondaryUV = assetPath.Contains("Edificio_");   // UV2 para luz horneada en muros y pisos
        }

        void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] nombres, object[] valores)
        {
            if (!assetPath.StartsWith(CARPETA)) return;
            var d = go.GetComponent<DatosBlender>() ?? go.AddComponent<DatosBlender>();
            for (int i = 0; i < nombres.Length; i++)
                d.Set(nombres[i], valores[i] == null ? "" : valores[i].ToString());
        }
    }
}
