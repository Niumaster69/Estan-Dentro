using UnityEngine;
using EstanDentro.Interaction;

namespace EstanDentro.Stress
{
    /// <summary>
    /// Sube el estres gradualmente mientras la linterna esta apagada.
    /// Incluye los apagones del FlashlightFlicker (que ya suman +5 puntual) Y los apagones
    /// voluntarios del jugador.
    ///
    /// Setup:
    ///   - Pegar este script en el mismo GameObject que tiene Flashlight (o en cualquier GO
    ///     y referenciar la Flashlight target).
    ///   - Tunear startAfterSeconds y stressRatePerSecond a gusto.
    ///
    /// Comportamiento:
    ///   - Si la linterna esta ON: el timer se resetea, no se suma estres.
    ///   - Si la linterna esta OFF: tras startAfterSeconds segundos, empieza a sumar
    ///     stressRatePerSecond por segundo de oscuridad continua.
    /// </summary>
    public class DarknessStress : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("Linterna a monitorear. Si vacio, se busca en el mismo GO.")]
        private Flashlight target;

        [Header("Comportamiento")]
        [SerializeField, Tooltip("Segundos de oscuridad continua antes de empezar a sumar estres. Da margen para apagados breves sin penalizar.")]
        private float startAfterSeconds = 2f;
        [SerializeField, Tooltip("Estres por segundo de oscuridad continua una vez pasado el startAfter.")]
        private float stressRatePerSecond = 2f;
        [SerializeField, Tooltip("Cap del estres acumulado por darkness en un solo apagon. Evita que el jugador se ahogue por dejar apagada la linterna mucho tiempo. 0 = sin cap.")]
        private float maxStressPerOff = 35f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private float darknessTimer;
        private float stressAddedThisOff;
        private bool wasOnLastFrame = true;

        private void Awake()
        {
            if (target == null) target = GetComponent<Flashlight>();
            if (target == null)
            {
                Debug.LogWarning("[DarknessStress] No encontre Flashlight en el GO. El componente se queda inactivo.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (target == null || StressSystem.Instance == null) return;

            bool isOn = target.IsOn;

            if (isOn)
            {
                // Reset al encender
                if (!wasOnLastFrame && debugLog)
                    Debug.Log($"[DarknessStress] Luz encendida — reset (stress acumulado: {stressAddedThisOff:F1}).");
                darknessTimer = 0f;
                stressAddedThisOff = 0f;
                wasOnLastFrame = true;
                return;
            }

            // Luz apagada
            if (wasOnLastFrame && debugLog)
                Debug.Log("[DarknessStress] Luz apagada — empieza a contar darkness.");
            wasOnLastFrame = false;

            darknessTimer += Time.deltaTime;
            if (darknessTimer < startAfterSeconds) return;

            // Capeado por apagon
            if (maxStressPerOff > 0f && stressAddedThisOff >= maxStressPerOff) return;

            float delta = stressRatePerSecond * Time.deltaTime;
            if (maxStressPerOff > 0f)
                delta = Mathf.Min(delta, maxStressPerOff - stressAddedThisOff);
            StressSystem.Instance.Add(delta);
            stressAddedThisOff += delta;
        }
    }
}
