using System.Collections.Generic;
using UnityEngine;

namespace EstanDentro.Intrusions
{
    /// <summary>
    /// Gestiona las siluetas perifericas del Salon 4-B.
    /// - Instancia siluetas en un conjunto de "asientos" (Transforms vacios colocados en la escena).
    /// - En muerte/reinicio reposiciona aleatoriamente para cumplir la regla del GDD
    ///   "el salon recuerda": siluetas cambian de posicion/densidad.
    /// - Expone una silueta "adherida" (El Que Se Sienta Delante) que sigue al jugador en la periferia.
    /// </summary>
    public class SilhouetteManager : MonoBehaviour
    {
        [Header("Referencias")]
        public Camera playerCamera;
        public GameObject silhouettePrefab;

        [Header("Siluetas de compañeros (pupitres)")]
        [Tooltip("Puntos donde pueden aparecer siluetas sentadas. Colocar Transforms vacios en cada pupitre.")]
        public List<Transform> seatAnchors = new List<Transform>();
        [Tooltip("Cuantas de esas plazas ocupan siluetas al iniciar el capitulo.")]
        [Range(0, 40)] public int initialOccupancy = 8;
        [Tooltip("Si es true, cada reinicio reordena aleatoriamente las ocupadas.")]
        public bool reshuffleOnReset = true;

        [Header("Silueta adherida (El Que Se Sienta Delante)")]
        public bool spawnStalker = true;
        public float stalkerLockDistance = 2.5f;
        [Range(30f, 120f)] public float stalkerLockAngle = 60f;

        readonly List<SilhouetteController> spawned = new List<SilhouetteController>();
        SilhouetteController stalker;

        void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            SpawnInitial();
            if (spawnStalker) SpawnStalker();
        }

        void SpawnInitial()
        {
            if (silhouettePrefab == null || seatAnchors.Count == 0) return;

            var indices = new List<int>();
            for (int i = 0; i < seatAnchors.Count; i++) indices.Add(i);
            Shuffle(indices);

            int n = Mathf.Min(initialOccupancy, seatAnchors.Count);
            for (int i = 0; i < n; i++)
            {
                var anchor = seatAnchors[indices[i]];
                var go = Instantiate(silhouettePrefab, anchor.position, anchor.rotation, transform);
                go.name = $"Silhouette_Seat_{indices[i]:00}";
                var ctrl = go.GetComponent<SilhouetteController>();
                if (ctrl == null) ctrl = go.AddComponent<SilhouetteController>();
                ctrl.targetCamera = playerCamera;
                ctrl.lockToView = false;
                spawned.Add(ctrl);
            }
        }

        void SpawnStalker()
        {
            if (silhouettePrefab == null || playerCamera == null) return;
            var go = Instantiate(silhouettePrefab, playerCamera.transform.position, Quaternion.identity, transform);
            go.name = "Silhouette_Stalker";
            var ctrl = go.GetComponent<SilhouetteController>();
            if (ctrl == null) ctrl = go.AddComponent<SilhouetteController>();
            ctrl.targetCamera = playerCamera;
            ctrl.lockToView = true;
            ctrl.lockDistance = stalkerLockDistance;
            ctrl.lockAngleDeg = stalkerLockAngle;
            ctrl.orbit = false;
            ctrl.fadeSpeed = 8f;
            stalker = ctrl;
        }

        /// <summary>
        /// Reposiciona las siluetas (excepto stalker) en nuevos asientos aleatorios.
        /// Llamar desde el sistema de muerte/reinicio del Sprint 3.
        /// </summary>
        public void OnPlayerReset()
        {
            if (!reshuffleOnReset || seatAnchors.Count == 0) return;

            var indices = new List<int>();
            for (int i = 0; i < seatAnchors.Count; i++) indices.Add(i);
            Shuffle(indices);

            for (int i = 0; i < spawned.Count && i < seatAnchors.Count; i++)
            {
                var anchor = seatAnchors[indices[i]];
                spawned[i].transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            }
        }

        /// <summary>
        /// Encoge las siluetas durante la intrusion del Iracundo (GDD: "se encogen como si tuvieran miedo").
        /// Sprint 3 conecta esto desde IntrusionIracundo.
        /// </summary>
        public void SetCoweringScale(float scale)
        {
            foreach (var s in spawned)
                s.transform.localScale = Vector3.one * scale;
        }

        static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
