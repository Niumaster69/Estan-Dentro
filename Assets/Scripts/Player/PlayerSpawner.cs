using UnityEngine;
using EstanDentro.Network;

namespace EstanDentro.Player
{
    /// <summary>
    /// Al cargar la escena, busca el PlayerSpawnPoint que matchea con GameSession.NextSpawnPointId
    /// (o el marcado como Default si no hay match) y teletransporta al player ahi.
    ///
    /// Setup: pegar este componente en el mismo GameObject del Player (el que tiene PlayerController
    /// + CharacterController). En cada escena, agregar 1+ PlayerSpawnPoints (empty GO) marcando uno
    /// como Default.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Si true, loguea decisiones de spawn en consola.")]
        private bool debugLog = true;

        private void Start()
        {
            var spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
            if (spawns == null || spawns.Length == 0)
            {
                if (debugLog) Debug.Log("[Spawner] No hay PlayerSpawnPoints en la escena. Player se queda en su posicion inicial.");
                return;
            }

            PlayerSpawnPoint target = null;
            string wantId = GameSession.NextSpawnPointId;

            if (!string.IsNullOrEmpty(wantId))
            {
                foreach (var sp in spawns)
                {
                    if (sp.Id == wantId) { target = sp; break; }
                }
                if (target == null && debugLog)
                    Debug.LogWarning($"[Spawner] No encontre SpawnPoint con id '{wantId}'. Fallback a default.");
            }

            if (target == null)
            {
                foreach (var sp in spawns)
                {
                    if (sp.IsDefault) { target = sp; break; }
                }
            }

            // Consumir el id (uso de un solo viaje)
            GameSession.NextSpawnPointId = "";

            if (target == null)
            {
                if (debugLog) Debug.LogWarning("[Spawner] No hay ningun SpawnPoint marcado como Default. Player se queda donde el editor lo posiciono.");
                return;
            }

            Teleport(target);
            if (target.SkipWakeUp) DisableWakeUp();
            target.NotifySpawnUsed();

            if (debugLog) Debug.Log($"[Spawner] Player teletransportado a SpawnPoint '{target.Id}' (default={target.IsDefault}, skipWakeUp={target.SkipWakeUp}).");
        }

        private void Teleport(PlayerSpawnPoint target)
        {
            var cc = GetComponent<CharacterController>();
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;

            transform.position = target.transform.position;
            transform.rotation = Quaternion.Euler(0f, target.transform.eulerAngles.y, 0f);

            if (cc != null && wasEnabled) cc.enabled = true;
        }

        private void DisableWakeUp()
        {
            var wakeUp = GetComponent<PlayerWakeUp>();
            if (wakeUp != null) wakeUp.AbortAndCleanup();
        }
    }
}
