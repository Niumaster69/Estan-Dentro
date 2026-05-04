using UnityEngine;
using EstanDentro.Network;

namespace EstanDentro.Intrusions.Gaze
{
    /// <summary>
    /// Se adjunta a la pizarra. Cuando el jugador la mira durante holdDurationSec,
    /// pide a IntrusionManager que dispare la intrusion del Observador.
    /// Se puede re-disparar tras cooldownSec (el evento es repetible segun diseño).
    /// </summary>
    public class ObserverTrigger : GazeTargetBase
    {
        [Header("Observador")]
        [Tooltip("Segundos de espera minima entre disparos consecutivos.")]
        public float cooldownSec = 6f;

        float nextAllowedTime;

        protected override void OnHoldReached()
        {
            ResetGaze();
            if (Time.time < nextAllowedTime) return;

            var mgr = IntrusionManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[ObserverTrigger] No hay IntrusionManager en la escena. Agrega un GameObject con el componente IntrusionManager.", this);
                return;
            }

            mgr.Trigger(IntrusionType.Observer);
            GameSession.ObserverTriggeredAtLeastOnce = true; // flag para evaluar 'sigilo_observer' al fin de capitulo
            nextAllowedTime = Time.time + cooldownSec;
        }
    }
}
