using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EstanDentro.EditorTools
{
    public class SeatAnchorTools : EditorWindow
    {
        const string SeatAnchorsName = "SeatAnchors";
        const string MesaSillaPrefix = "mesaSilla";
        static readonly Regex MesaSillaRegex = new Regex(@"^mesaSilla(\d+)$", RegexOptions.Compiled);

        float yOffset = 0.8f;
        bool matchMesaSillaRotation = true;
        bool overwriteExisting = false;
        Transform seatAnchorsParent;

        [MenuItem("Tools/Estan Dentro/Seat Anchors/Generator...")]
        static void Open()
        {
            var w = GetWindow<SeatAnchorTools>(true, "Seat Anchor Generator", true);
            w.minSize = new Vector2(360, 220);
            w.TryAutoAssignParent();
        }

        void OnEnable() => TryAutoAssignParent();

        void TryAutoAssignParent()
        {
            if (seatAnchorsParent != null) return;
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var t = FindChildRecursive(root.transform, SeatAnchorsName);
                if (t != null) { seatAnchorsParent = t; return; }
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Genera Seat_XX desde mesaSillaN", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Escanea la escena activa, encuentra los mesaSillaN y crea un Seat_XX por cada uno bajo SeatAnchors. " +
                "Usa la posicion mundial de la mesaSilla y le suma Y Offset para la altura del torso de la silueta.",
                MessageType.Info);

            seatAnchorsParent = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("SeatAnchors parent", "Si lo dejas vacio, se creara uno en la raiz de la escena."),
                seatAnchorsParent, typeof(Transform), true);

            yOffset = EditorGUILayout.FloatField(
                new GUIContent("Y Offset", "Altura extra sobre el pivote del mesaSilla. 0.8 coincide con Seat_02."),
                yOffset);

            matchMesaSillaRotation = EditorGUILayout.Toggle(
                new GUIContent("Match rotacion mesaSilla", "Los anchors heredan la rotacion mundial del pupitre."),
                matchMesaSillaRotation);

            overwriteExisting = EditorGUILayout.Toggle(
                new GUIContent("Sobrescribir existentes", "Si existe un Seat_XX con ese indice, se reposiciona. Si no esta activo, se salta."),
                overwriteExisting);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(SceneManager.GetActiveScene().rootCount == 0))
            {
                if (GUILayout.Button("Generar desde mesaSillaN", GUILayout.Height(30)))
                    Generate();

                if (GUILayout.Button("Alinear Y de Seat_XX existentes", GUILayout.Height(22)))
                    AlignExistingHeights();
            }
        }

        void Generate()
        {
            var scene = SceneManager.GetActiveScene();
            var mesaSillas = CollectMesaSillas(scene);
            if (mesaSillas.Count == 0)
            {
                EditorUtility.DisplayDialog("Seat Anchors", "No se encontro ningun mesaSillaN en la escena activa.", "OK");
                return;
            }

            Transform parent = EnsureParent();
            int created = 0, updated = 0, skipped = 0;

            foreach (var kv in mesaSillas)
            {
                int idx = kv.Key;
                Transform mesa = kv.Value;
                string seatName = $"Seat_{idx:00}";
                Transform existing = FindChildDirect(parent, seatName);

                ComputeVisiblePose(mesa, out Vector3 basePos, out Quaternion baseRot);
                Vector3 worldPos = basePos + new Vector3(0f, yOffset, 0f);
                Quaternion worldRot = matchMesaSillaRotation ? baseRot : Quaternion.identity;

                if (existing != null)
                {
                    if (!overwriteExisting) { skipped++; continue; }
                    Undo.RecordObject(existing, "Update Seat Anchor");
                    existing.SetPositionAndRotation(worldPos, worldRot);
                    updated++;
                }
                else
                {
                    var go = new GameObject(seatName);
                    Undo.RegisterCreatedObjectUndo(go, "Create Seat Anchor");
                    Undo.SetTransformParent(go.transform, parent, "Parent Seat Anchor");
                    go.transform.SetPositionAndRotation(worldPos, worldRot);
                    created++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[SeatAnchorTools] creados {created}, actualizados {updated}, saltados {skipped}. Total mesaSillas: {mesaSillas.Count}.");
        }

        void AlignExistingHeights()
        {
            Transform parent = seatAnchorsParent;
            if (parent == null) { EditorUtility.DisplayDialog("Seat Anchors", "No hay SeatAnchors en la escena.", "OK"); return; }

            int count = 0;
            foreach (Transform child in parent)
            {
                if (!child.name.StartsWith("Seat_")) continue;
                Undo.RecordObject(child, "Align Seat Anchor Height");
                Vector3 p = child.position;
                p.y = yOffset;
                child.position = p;
                count++;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[SeatAnchorTools] alineados {count} anchors a Y={yOffset}.");
        }

        Transform EnsureParent()
        {
            if (seatAnchorsParent != null) return seatAnchorsParent;
            var go = new GameObject(SeatAnchorsName);
            Undo.RegisterCreatedObjectUndo(go, "Create SeatAnchors parent");
            seatAnchorsParent = go.transform;
            return seatAnchorsParent;
        }

        static void ComputeVisiblePose(Transform mesa, out Vector3 pos, out Quaternion rot)
        {
            var renderers = mesa.GetComponentsInChildren<Renderer>(includeInactive: false);
            if (renderers.Length == 0)
            {
                pos = mesa.position;
                rot = mesa.rotation;
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            pos = new Vector3(b.center.x, b.min.y, b.center.z);

            Transform rotSource = mesa;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].transform.rotation != Quaternion.identity)
                {
                    rotSource = renderers[i].transform;
                    break;
                }
            }
            rot = rotSource.rotation;
        }

        static SortedDictionary<int, Transform> CollectMesaSillas(Scene scene)
        {
            var result = new SortedDictionary<int, Transform>();
            foreach (var root in scene.GetRootGameObjects())
                CollectRecursive(root.transform, result);
            return result;
        }

        static void CollectRecursive(Transform t, SortedDictionary<int, Transform> acc)
        {
            var m = MesaSillaRegex.Match(t.name);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int idx) && !acc.ContainsKey(idx))
                acc[idx] = t;
            foreach (Transform c in t) CollectRecursive(c, acc);
        }

        static Transform FindChildRecursive(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindChildRecursive(c, name);
                if (r != null) return r;
            }
            return null;
        }

        static Transform FindChildDirect(Transform parent, string name)
        {
            foreach (Transform c in parent) if (c.name == name) return c;
            return null;
        }
    }
}
