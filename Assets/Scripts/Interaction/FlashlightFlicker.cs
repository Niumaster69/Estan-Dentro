using System.Collections;
using UnityEngine;
using EstanDentro.Stress;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Hace que la Flashlight parpadee y falle aleatoriamente, sumando estres en cada fallo.
    /// Pensado para zonas de tension (ductos, salas finales).
    ///
    /// Setup:
    ///   - Pegar este script en el mismo GameObject que tiene el componente Flashlight.
    ///   - La Flashlight debe estar unlocked = true (o setear requireInventoryItem = "" para ignorar inventario).
    ///   - Tunear intervalos y duracion de fallo en Inspector.
    ///
    /// Solo parpadea si la linterna esta encendida en el momento del fallo programado.
    /// Si el jugador apaga la linterna manualmente con F, el flicker no la prende a la fuerza.
    /// </summary>
    [RequireComponent(typeof(Flashlight))]
    public class FlashlightFlicker : MonoBehaviour
    {
        [Header("Intervalo entre fallos (segundos)")]
        [SerializeField, Tooltip("Tiempo minimo entre fallos.")]
        private float intervalMin = 15f;
        [SerializeField, Tooltip("Tiempo maximo entre fallos.")]
        private float intervalMax = 30f;

        [Header("Duracion del fallo (segundos)")]
        [SerializeField, Tooltip("Tiempo minimo que la linterna queda apagada.")]
        private float failDurationMin = 1.2f;
        [SerializeField, Tooltip("Tiempo maximo que la linterna queda apagada.")]
        private float failDurationMax = 2.8f;

        [Header("Pre-flicker (parpadeo rapido antes de apagarse)")]
        [SerializeField, Tooltip("Cuantos ciclos de parpadeo antes del apagado total. 0 = corte limpio sin warning.")]
        private int preFlickerCycles = 3;
        [SerializeField, Tooltip("Duracion total del pre-flicker en segundos.")]
        private float preFlickerDuration = 0.35f;

        [Header("Estres")]
        [SerializeField, Tooltip("Puntos de estres que se suman cada fallo. 0 = no suma. Default 5.")]
        private float stressPerFailure = 5f;

        [Header("Activacion")]
        [SerializeField, Tooltip("Si false, no parpadea. Util para deshabilitar via codigo desde otro trigger.")]
        private bool autoStart = true;
        [SerializeField, Tooltip("Tiempo de espera al iniciar la escena antes del primer fallo (segundos). Permite ambientar primero.")]
        private float initialDelay = 8f;
        [SerializeField, Tooltip("Si true, loguea cada fallo a consola para debug.")]
        private bool debugLog = false;

        private Flashlight flashlight;
        private Coroutine flickerCo;
        private bool running;

        private void Awake()
        {
            flashlight = GetComponent<Flashlight>();
        }

        private void OnEnable()
        {
            if (autoStart) StartFlickering();
        }

        private void OnDisable()
        {
            StopFlickering();
        }

        public void StartFlickering()
        {
            if (running) return;
            running = true;
            flickerCo = StartCoroutine(FlickerLoop());
        }

        public void StopFlickering()
        {
            running = false;
            if (flickerCo != null) { StopCoroutine(flickerCo); flickerCo = null; }
        }

        private IEnumerator FlickerLoop()
        {
            if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

            while (running)
            {
                float wait = Random.Range(intervalMin, intervalMax);
                yield return new WaitForSeconds(wait);

                if (!running) yield break;

                // Solo flickear si la linterna esta encendida (no forzar al jugador a tenerla on)
                if (!flashlight.IsOn) continue;

                yield return DoFlickerAndFail();
            }
        }

        private IEnumerator DoFlickerAndFail()
        {
            if (debugLog) Debug.Log("[FlashlightFlicker] Iniciando fallo.");

            // Pre-flicker: parpadeo rapido como warning
            if (preFlickerCycles > 0 && preFlickerDuration > 0f)
            {
                float step = preFlickerDuration / (preFlickerCycles * 2f);
                for (int i = 0; i < preFlickerCycles; i++)
                {
                    if (!running) yield break;
                    flashlight.SetEnabled(false);
                    yield return new WaitForSeconds(step);
                    if (!running) yield break;
                    flashlight.SetEnabled(true);
                    yield return new WaitForSeconds(step);
                }
            }

            if (!running) yield break;

            // Apagado total
            flashlight.SetEnabled(false);

            // Sumar estres
            if (stressPerFailure > 0f && StressSystem.Instance != null)
                StressSystem.Instance.Add(stressPerFailure);

            // Esperar duracion del fallo
            float failDuration = Random.Range(failDurationMin, failDurationMax);
            yield return new WaitForSeconds(failDuration);

            // Reactivar (solo si el flicker sigue activo y el jugador no apago manualmente)
            if (running) flashlight.SetEnabled(true);

            if (debugLog) Debug.Log("[FlashlightFlicker] Fin del fallo.");
        }
    }
}
