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
            // -salida <ruta.exe>: compilar en otra carpeta (para probar mientras el juego de Henry está abierto)
            var args = Environment.GetCommandLineArgs(); int ia = Array.IndexOf(args, "-salida");
            string exe = ia >= 0 && ia + 1 < args.Length ? Path.GetFullPath(args[ia + 1]) : Path.GetFullPath(Path.Combine(Application.dataPath, "..", SALIDA));
            Directory.CreateDirectory(Path.GetDirectoryName(exe));
            var opciones = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu/MainMenu.unity", ConstruirPrologo.ESCENA, "Assets/Scenes/Colegio/Colegio.unity", PortarCapitulo1.ESCENA_DUCTOS },
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
