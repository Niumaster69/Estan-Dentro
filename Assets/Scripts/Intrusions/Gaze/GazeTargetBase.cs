using UnityEngine;
using UnityEngine.Events;

namespace EstanDentro.Intrusions.Gaze
{
    /// <summary>
    /// Base de cualquier objeto que reaccione a la mirada del jugador.
    /// Requiere un Collider en el GameObject para que el GazeDetector pueda raycastear.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class GazeTargetBase : MonoBehaviour
    {
        [Header("Mirada sostenida")]
        [Tooltip("Segundos de mirada continua necesarios para disparar onGazeHeld.")]
        public float holdDurationSec = 3.5f;
        [Tooltip("Si true, el tiempo se resetea cuando el jugador aparta la vista.")]
        public bool resetOnLookAway = true;

        [Header("Eventos")]
        public UnityEvent onGazeEnter;
        public UnityEvent onGazeHeld;
        public UnityEvent onGazeExit;

        public bool IsBeingGazed { get; private set; }
        public float CurrentGazeTime { get; private set; }
        public bool HasFiredHold { get; private set; }

        public virtual void GazeEnter()
        {
            IsBeingGazed = true;
            onGazeEnter?.Invoke();
        }

        public virtual void GazeStay(float deltaTime)
        {
            if (!IsBeingGazed) return;
            CurrentGazeTime += deltaTime;
            if (!HasFiredHold && CurrentGazeTime >= holdDurationSec)
            {
                HasFiredHold = true;
                onGazeHeld?.Invoke();
                OnHoldReached();
            }
        }

        public virtual void GazeExit()
        {
            IsBeingGazed = false;
            onGazeExit?.Invoke();
            if (resetOnLookAway) ResetGaze();
        }

        public virtual void ResetGaze()
        {
            CurrentGazeTime = 0f;
            HasFiredHold = false;
        }

        protected virtual void OnHoldReached() {}
    }
}
