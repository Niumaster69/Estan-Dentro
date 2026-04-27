using UnityEngine;

namespace EstanDentro.Intrusions.Gaze
{
    /// <summary>
    /// Se adjunta a la camara del jugador. Cada frame lanza un raycast desde el forward
    /// de la camara y notifica al GazeTargetBase apuntado (Enter / Stay / Exit).
    /// Respeta oclusion: si un muro esta entre camara y objetivo, el raycast impacta el muro primero
    /// y la mirada no cuenta.
    /// </summary>
    public class GazeDetector : MonoBehaviour
    {
        [Header("Raycast")]
        [Tooltip("Distancia maxima para detectar objetos mirados.")]
        public float maxDistance = 50f;
        [Tooltip("Capas que el raycast considera (incluye las que ocluyen).")]
        public LayerMask hitLayers = ~0;
        [Tooltip("Como tratar los colliders marcados como Trigger.")]
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Camara")]
        [Tooltip("Si queda vacia, usa GetComponent<Camera>() o Camera.main.")]
        public Camera sourceCamera;

        [Header("Debug")]
        public bool drawGizmo = true;
        public bool logTargetChanges = false;

        GazeTargetBase current;

        void Awake()
        {
            if (sourceCamera == null) sourceCamera = GetComponent<Camera>();
            if (sourceCamera == null) sourceCamera = Camera.main;
        }

        void Update()
        {
            if (sourceCamera == null) return;

            GazeTargetBase hit = null;
            Ray ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit info, maxDistance, hitLayers, triggerInteraction))
                hit = info.collider.GetComponentInParent<GazeTargetBase>();

            if (hit != current)
            {
                if (current != null) current.GazeExit();
                current = hit;
                if (current != null)
                {
                    current.GazeEnter();
                    if (logTargetChanges) Debug.Log($"[Gaze] Enter: {current.name}", current);
                }
            }

            if (current != null) current.GazeStay(Time.deltaTime);
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            Camera cam = sourceCamera != null ? sourceCamera : GetComponent<Camera>() ?? Camera.main;
            if (cam == null) return;
            Gizmos.color = current != null ? Color.red : Color.yellow;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * maxDistance);
        }
    }
}
