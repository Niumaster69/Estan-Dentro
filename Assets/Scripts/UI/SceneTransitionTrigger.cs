using UnityEngine;

namespace EstanDentro.UI
{
    /// <summary>
    /// Trigger box que carga otra escena cuando el Player entra.
    /// Util para portales entre escenarios (Salon -> Ductos -> Oficina).
    /// Requiere un Collider con Is Trigger marcado.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [SerializeField, Tooltip("Nombre exacto de la escena destino (debe estar en Build Settings).")]
        private string targetSceneName = "Dev_Duvan";
        [SerializeField, Tooltip("Tip que aparece en la pantalla de carga.")]
        private string loadingTip = "";
        [SerializeField, Tooltip("Tiempo minimo de pantalla de carga en segundos.")]
        private float minLoadDisplay = 1.5f;
        [SerializeField, Tooltip("Si true, el trigger solo se activa una vez por ejecucion (no se repite).")]
        private bool oneShot = true;

        private bool triggered;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneShot && triggered) return;
            var pc = other.GetComponentInParent<EstanDentro.Player.PlayerController>();
            if (pc == null) return;
            triggered = true;
            SceneTransition.LoadScene(targetSceneName, minLoadDisplay, loadingTip);
        }
    }
}
