using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Compila un ejecutable de Windows solo con la escena del colegio (para probar sin abrir Unity).</summary>
    public static class CompilarColegio
    {
        const string SALIDA = "../../06_Builds/Colegio/EstanDentro_Colegio.exe";

        [MenuItem("Estan Dentro/6. Compilar ejecutable del colegio")]
        public static void Compilar()
        {
            string exe = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SALIDA));
            Directory.CreateDirectory(Path.GetDirectoryName(exe));
            var opciones = new BuildPlayerOptions
            {
                scenes = new[] { ConstruirPrologo.ESCENA, "Assets/Scenes/Colegio/Colegio.unity", PortarCapitulo1.ESCENA_DUCTOS },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport r = BuildPipeline.BuildPlayer(opciones);
            Debug.Log($"[Compilar] {r.summary.result} | {r.summary.totalSize / (1024 * 1024)} MB | {r.summary.totalTime} | {exe}");
            if (r.summary.result != BuildResult.Succeeded) throw new Exception("La compilación falló: " + r.summary.totalErrors + " errores");
        }

        public static void CompilarBatch()
        {
            try { Compilar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Compilar] " + e); EditorApplication.Exit(1); }
        }
    }
}
