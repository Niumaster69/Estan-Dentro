using UnityEngine;
using UnityEngine.Events;

namespace EstanDentro.Player
{
    /// <summary>
    /// Marca una posicion + rotacion (yaw) donde el player puede aparecer al cargar la escena.
    /// PlayerSpawner busca un PlayerSpawnPoint cuyo Id coincida con GameSession.NextSpawnPointId,
    /// o usa el marcado como Default si no hay match.
    ///
    /// Setup: empty GameObject ubicado donde quieres que aparezca el player. Rotacion en Y determina
    /// hacia donde mira (X y Z se ignoran, las rotaciones X/Z las maneja el CameraPivot).
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("Identificador unico de este spawn dentro de la escena. Ej: 'inicio_salon', 'salas_finales'.")]
        private string id = "default";

        [SerializeField, Tooltip("Si true, este es el spawn por defecto cuando GameSession.NextSpawnPointId esta vacio o no matchea ningun id.")]
        private bool isDefault = false;

        [SerializeField, Tooltip("Si true, ademas de teletransportar, deshabilita el PlayerWakeUp en este Player (para que NO corra la cinematica de despertar al spawnear aqui). Recomendado para spawns no-iniciales.")]
        private bool skipWakeUp = false;

        [Header("Eventos al usar este spawn")]
        [SerializeField, Tooltip("Eventos disparados cuando el PlayerSpawner usa este SpawnPoint. Util para invocar un VideoCinematicPlayer.Play() al llegar a este spawn.")]
        public UnityEvent onSpawnUsed;

        public string Id => id;
        public bool IsDefault => isDefault;
        public bool SkipWakeUp => skipWakeUp;
        public void NotifySpawnUsed() => onSpawnUsed?.Invoke();

        private void OnDrawGizmos()
        {
            Gizmos.color = isDefault ? new Color(0.4f, 1f, 0.5f, 0.9f) : new Color(0.4f, 0.7f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.2f);
        }
    }
}
