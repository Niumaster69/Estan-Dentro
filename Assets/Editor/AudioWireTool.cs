using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EstanDentro.EditorTools
{
    /// <summary>
    /// Recorre todos los componentes en la(s) escena(s) abierta(s) y asigna automaticamente
    /// AudioClips de Assets/Audio/Ambient/ a slots de tipo AudioClip vacios, segun un mapping
    /// explicito field-name -> nombre de archivo (sin extension, case-insensitive).
    ///
    /// Acceso: barra de menus -> Tools -> Estan Dentro -> Wire Audio Clips
    ///
    /// El tool NUNCA sobreescribe slots que ya tienen un AudioClip asignado. Solo llena los vacios.
    /// </summary>
    public static class AudioWireTool
    {
        private const string AudioFolder = "Assets/Audio/Ambient";

        // Mapping field-name (case-insensitive) -> nombre del archivo (sin extension, case-insensitive).
        // Si quieres mapear varios fields al mismo archivo, repetir entrada.
        private static readonly Dictionary<string, string> FieldToFile = new(System.StringComparer.OrdinalIgnoreCase)
        {
            // PlayerWakeUp
            { "heartbeatLoopClip", "Heartbeat (despertar)" },
            { "gaspClip", "breath_man" }, // reuso: gasp del despertar usa el breath corto
            // DuctoTechoInteractable
            { "mesaRodandoClip", "HeavyProp raspe" }, // reuso: mesa rodando == raspe de mueble
            { "destornilladorClip", "CombinationLock click digito" }, // reuso: clicks metalicos pasan como destornillador
            // BreathingMinigame / SuffocationSystem
            { "jadeoClip", "breath_man" },
            // SubliminalLoopController (clip layers — se llenan por nombre del clip)
            // DuctoTechoInteractable
            { "ductoCayendoClip", "ductoCayendoClip" },
            // ExitDuctsTrigger / EndGameTrigger
            { "enterStinger", "enterStinger" },
            { "ambientStingerOnEnter", "enterStinger" },
            // ObserverApparitionTrigger
            { "stingerClip", "enterStinger" },
            // LockedDoor
            { "lockedAudioClip", "LockedDoor jaloneo" },
            // CombinationLock
            { "digitClickClip", "CombinationLock click digito" },
            { "solvedClip", "CombinationLock solved" },
            // Flashlight
            { "toggleClip", "Flashlight onoff" },
            // Note
            { "pickupClip", "Pickup item" },
            // HeavyProp
            { "scrapeClip", "HeavyProp raspe" },
            // SimpleOpenable (casilleros sin combinacion) — reusan el jaloneo de LockedDoor
            { "openClip", "LockedDoor jaloneo" },
            { "closeClip", "LockedDoor jaloneo" },
            // MainMenuController
            { "uiClickClip", "UI Click" },
            { "uiHoverClip", "UI Hover" },
            { "uiBackClip", "UI Click" }, // mismo audio que el click (decision del usuario)
            // AudioManager (autoPlayMusicOnAwake intencionalmente NO mapeado aqui — se asigna
            //  condicionalmente solo en MainMenu via logica especial en el bucle de wiring).
        };

        // Mapping ESPECIFICO por escena: solo se aplica si la escena activa coincide.
        // Asi evitamos que la musica del menu termine tocando en todas las escenas.
        private static readonly Dictionary<string, Dictionary<string, string>> PerSceneFieldToFile = new()
        {
            ["MainMenu"] = new(System.StringComparer.OrdinalIgnoreCase)
            {
                { "autoPlayMusicOnAwake", "Musica menu loop" },
            },
        };

        [MenuItem("Tools/Estan Dentro/Wire Audio Clips (escena activa)")]
        public static void WireActiveScene()
        {
            var clipsByName = LoadClipsByName();
            if (clipsByName.Count == 0)
            {
                EditorUtility.DisplayDialog("AudioWireTool",
                    $"No se encontraron AudioClips en '{AudioFolder}'.", "OK");
                return;
            }

            // PASO 0: asegurar que existe un AudioManager en la escena activa. Sin el, MainMenuController.PlayUI
            // no suena (Instance es null) y los SFX que pasan por AudioManager.PlaySFX tampoco.
            bool createdAudioManager = EnsureAudioManagerInActiveScene();

            int filled = 0, alreadyFilled = 0, unresolved = 0;
            var unresolvedFields = new HashSet<string>();
            var changedObjects = new HashSet<Object>();

            // Mapping a usar: el global + los especificos de esta escena (si los hay)
            var activeMapping = new Dictionary<string, string>(FieldToFile, System.StringComparer.OrdinalIgnoreCase);
            string activeSceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            if (PerSceneFieldToFile.TryGetValue(activeSceneName, out var perScene))
            {
                foreach (var kv in perScene) activeMapping[kv.Key] = kv.Value;
                Debug.Log($"[AudioWire] Aplicando mapping especifico para escena '{activeSceneName}': {perScene.Count} fields adicionales.");
            }

            foreach (var go in GetAllRootGameObjectsInOpenScenes())
            {
                foreach (var comp in go.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var prop = so.GetIterator();
                    bool hasNext = prop.NextVisible(true);
                    bool dirty = false;
                    while (hasNext)
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference
                            && prop.type == "PPtr<$AudioClip>")
                        {
                            if (prop.objectReferenceValue != null)
                            {
                                alreadyFilled++;
                            }
                            else if (activeMapping.TryGetValue(prop.name, out string fileName))
                            {
                                if (clipsByName.TryGetValue(fileName.ToLowerInvariant(), out AudioClip clip))
                                {
                                    prop.objectReferenceValue = clip;
                                    filled++;
                                    dirty = true;
                                    Debug.Log($"[AudioWire] {comp.GetType().Name}.{prop.name} <- {clip.name} (en GO '{comp.gameObject.name}')");
                                }
                                else
                                {
                                    unresolved++;
                                    unresolvedFields.Add($"{prop.name} -> {fileName} (archivo no encontrado)");
                                }
                            }
                            else
                            {
                                unresolved++;
                                unresolvedFields.Add($"{prop.name} (sin mapping en AudioWireTool.FieldToFile)");
                            }
                        }
                        hasNext = prop.NextVisible(false);
                    }
                    if (dirty)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(comp);
                        changedObjects.Add(comp);
                    }
                }
            }

            // Marcar escenas como modificadas
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var s = EditorSceneManager.GetSceneAt(i);
                if (s.isLoaded) EditorSceneManager.MarkSceneDirty(s);
            }

            string report = $"AudioManager creado: {(createdAudioManager ? "SI" : "ya existia")}\n" +
                            $"Asignados: {filled}\nYa asignados: {alreadyFilled}\nSin resolver: {unresolved}\n";
            if (unresolvedFields.Count > 0)
            {
                report += "\nSin resolver (revisar):\n";
                foreach (var u in unresolvedFields) report += "  - " + u + "\n";
            }
            Debug.Log("[AudioWire] Reporte:\n" + report);
            EditorUtility.DisplayDialog("AudioWireTool — Reporte", report, "OK");
        }

        private static bool EnsureAudioManagerInActiveScene()
        {
            var amType = System.Type.GetType("EstanDentro.Audio.AudioManager, Assembly-CSharp");
            if (amType == null)
            {
                Debug.LogWarning("[AudioWire] No encontre el tipo EstanDentro.Audio.AudioManager. Saltando creacion.");
                return false;
            }
            var existing = Object.FindFirstObjectByType(amType);
            if (existing != null) return false;
            var go = new GameObject("_AudioManager");
            go.AddComponent(amType);
            UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(
                go, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[AudioWire] _AudioManager creado en la escena activa.");
            return true;
        }

        [MenuItem("Tools/Estan Dentro/List Audio Clips disponibles")]
        public static void ListClips()
        {
            var clips = LoadClipsByName();
            var lines = new List<string> { $"Encontrados {clips.Count} clips en {AudioFolder}:" };
            foreach (var kv in clips) lines.Add("  - " + kv.Value.name);
            Debug.Log(string.Join("\n", lines));
        }

        private static Dictionary<string, AudioClip> LoadClipsByName()
        {
            var result = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(AudioFolder)) return result;

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;
                string nameNoExt = Path.GetFileNameWithoutExtension(path);
                // El dict matchea por nombre exacto (sin ext) en lowercase
                result[nameNoExt.ToLowerInvariant()] = clip;
            }
            return result;
        }

        private static IEnumerable<GameObject> GetAllRootGameObjectsInOpenScenes()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var s = EditorSceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                foreach (var go in s.GetRootGameObjects())
                    yield return go;
            }
        }
    }
}
