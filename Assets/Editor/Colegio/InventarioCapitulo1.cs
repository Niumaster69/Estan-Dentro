using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorColegio
{
    /// <summary>Lista cómo está armado el capítulo 1 en las escenas viejas (objetos con lógica y a qué apuntan).</summary>
    public static class InventarioCapitulo1
    {
        static readonly string[] Escenas =
        {
            "Assets/Scenes/Level-1/EscenaUno-salonPrincipal.unity",
            "Assets/Scenes/Level-1/EsecenaUno-DuctosDeVentilacion.unity",
        };

        public static void Volcar()
        {
            var sb = new StringBuilder();
            foreach (var ruta in Escenas)
            {
                var esc = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                sb.AppendLine($"=== {ruta}");
                foreach (var root in esc.GetRootGameObjects())
                    sb.AppendLine($"ROOT {root.name}  hijos={root.transform.childCount}  pos={root.transform.position}");
                foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (mb == null) continue;
                    var ns = mb.GetType().Namespace ?? "";
                    if (!ns.StartsWith("EstanDentro")) continue;
                    var t = mb.transform;
                    string ruta2 = t.name; var p = t.parent;
                    while (p != null) { ruta2 = p.name + "/" + ruta2; p = p.parent; }
                    sb.AppendLine($"[{mb.GetType().Name}] {ruta2}  pos={t.position:F2} activo={mb.gameObject.activeInHierarchy}");
                    var so = new SerializedObject(mb);
                    var it = so.GetIterator();
                    bool entrar = true;
                    while (it.NextVisible(entrar))
                    {
                        entrar = false;
                        if (it.name == "m_Script") continue;
                        string v = it.propertyType switch
                        {
                            SerializedPropertyType.ObjectReference => it.objectReferenceValue ? it.objectReferenceValue.name + " (" + it.objectReferenceValue.GetType().Name + ")" : "null",
                            SerializedPropertyType.String => "\"" + it.stringValue.Replace("\n", " ").Substring(0, Math.Min(90, it.stringValue.Length)) + "\"",
                            SerializedPropertyType.Integer => it.intValue.ToString(),
                            SerializedPropertyType.Boolean => it.boolValue.ToString(),
                            SerializedPropertyType.Float => it.floatValue.ToString("0.##"),
                            SerializedPropertyType.Vector3 => it.vector3Value.ToString(),
                            SerializedPropertyType.Enum => it.enumNames.Length > it.enumValueIndex && it.enumValueIndex >= 0 ? it.enumNames[it.enumValueIndex] : "?",
                            SerializedPropertyType.Generic when it.isArray => $"array[{it.arraySize}]",
                            _ => null
                        };
                        if (v != null) sb.AppendLine($"      {it.name} = {v}");
                    }
                }
            }
            string salida = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../01_Documentacion/capitulo1_inventario.txt"));
            File.WriteAllText(salida, sb.ToString());
            Debug.Log("[Inventario] " + salida);
        }

        public static void VolcarBatch()
        {
            try { Volcar(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[Inventario] " + e); EditorApplication.Exit(1); }
        }
    }
}
