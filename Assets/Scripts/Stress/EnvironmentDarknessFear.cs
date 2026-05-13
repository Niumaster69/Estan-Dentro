using UnityEngine;
using UnityEngine.Events;

namespace EstanDentro.Stress
{
    /// <summary>
    /// Dispara "miedo a la oscuridad ambiental" — sube el estres cuando las luces del cuarto se apagan.
    ///
    /// Pensado para momentos guionados (puzzles): las luces del salon se apagan, debe empezar
    /// a subir el estres gradualmente. Cuando vuelvan las luces, se detiene.
    ///
    /// Uso 1 (automatico): asigna los Lights del cuarto. Cada Update el componente verifica si
    /// la suma de intensidades esta debajo del threshold → empieza a sumar estres.
    ///
    /// Uso 2 (manual via eventos): si el puzzle no tiene Lights identificables, llama
    /// BeginDarkness() / EndDarkness() desde un UnityEvent (ej. CombinationLock.onSolved).
    /// </summary>
    public class EnvironmentDarknessFear : MonoBehaviour
    {
        [Header("Modo Automatico — monitorear Lights del cuarto")]
        [SerializeField, Tooltip("Lights del entorno (NO la linterna del player). Si todos estan apagados o intensidad < threshold, empieza miedo. Vacio = modo manual.")]
        private Light[] roomLights;
        [SerializeField, Tooltip("Suma minima de intensidades de roomLights para considerar 'oscuridad'. Default 0.05.")]
        private float darknessIntensityThreshold = 0.05f;

        [Header("Comportamiento del miedo")]
        [SerializeField, Tooltip("Segundos de oscuridad continua antes de empezar a sumar estres (margen para apagones cortos).")]
        private float startAfterSeconds = 1.5f;
        [SerializeField, Tooltip("Estres por segundo de oscuridad continua.")]
        private float stressRatePerSecond = 4f;
        [SerializeField, Tooltip("Cap del estres acumulado por un solo apagon. 0 = sin cap.")]
        private float maxStressPerEvent = 60f;

        [Header("Efectos colaterales")]
        [SerializeField, Tooltip("Si true, al iniciar la oscuridad fuerza el minijuego de respiracion abierto. UTIL para el momento del susto.")]
        private bool forceBreathingOnStart;
        [SerializeField, Tooltip("Si true, dispara un audio one-shot cuando se inicia la oscuridad.")]
        private AudioClip darknessStingerClip;
        [SerializeField, Range(0f, 1f)] private float darknessStingerVolume = 0.85f;
        [SerializeField, Tooltip("Evento al iniciar el momento de miedo. Util para post-process, shake, etc.")]
        public UnityEvent onDarknessBegin;
        [SerializeField, Tooltip("Evento al volver las luces.")]
        public UnityEvent onDarknessEnd;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private float darknessTimer;
        private float stressAddedThisEvent;
        private bool inDarkness;
        private AudioSource audioSrc;

        private void Awake()
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null && darknessStingerClip != null)
            {
                audioSrc = gameObject.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0f;
            }
        }

        private void Update()
        {
            // Modo automatico: detectar oscuridad via Lights del cuarto
            if (roomLights != null && roomLights.Length > 0)
            {
                bool dark = ComputeRoomDarkness();
                if (dark && !inDarkness) BeginDarkness();
                else if (!dark && inDarkness) EndDarkness();
            }

            // Tick del estres mientras estemos en oscuridad
            if (inDarkness) TickFear();
        }

        private bool ComputeRoomDarkness()
        {
            float sum = 0f;
            for (int i = 0; i < roomLights.Length; i++)
            {
                if (roomLights[i] == null) continue;
                if (!roomLights[i].enabled || !roomLights[i].gameObject.activeInHierarchy) continue;
                sum += roomLights[i].intensity;
            }
            return sum < darknessIntensityThreshold;
        }

        private void TickFear()
        {
            darknessTimer += Time.deltaTime;
            if (darknessTimer < startAfterSeconds) return;
            if (StressSystem.Instance == null) return;
            if (maxStressPerEvent > 0f && stressAddedThisEvent >= maxStressPerEvent) return;

            float delta = stressRatePerSecond * Time.deltaTime;
            if (maxStressPerEvent > 0f)
                delta = Mathf.Min(delta, maxStressPerEvent - stressAddedThisEvent);
            StressSystem.Instance.Add(delta);
            stressAddedThisEvent += delta;
        }

        /// <summary>Inicia el momento de miedo manualmente. Wirear desde UnityEvent del puzzle.</summary>
        public void BeginDarkness()
        {
            if (inDarkness) return;
            inDarkness = true;
            darknessTimer = 0f;
            stressAddedThisEvent = 0f;
            if (debugLog) Debug.Log("[EnvironmentDarknessFear] BeginDarkness — empieza el miedo a la oscuridad.");

            if (darknessStingerClip != null && audioSrc != null)
                audioSrc.PlayOneShot(darknessStingerClip, darknessStingerVolume);
            if (forceBreathingOnStart && EstanDentro.Breathing.BreathingMinigame.Instance != null)
                EstanDentro.Breathing.BreathingMinigame.Instance.ForceShow();
            onDarknessBegin?.Invoke();
        }

        /// <summary>Termina el momento de miedo manualmente. Wirear cuando vuelvan las luces.</summary>
        public void EndDarkness()
        {
            if (!inDarkness) return;
            inDarkness = false;
            if (debugLog) Debug.Log($"[EnvironmentDarknessFear] EndDarkness — estres acumulado: {stressAddedThisEvent:F1}.");

            if (forceBreathingOnStart && EstanDentro.Breathing.BreathingMinigame.Instance != null)
                EstanDentro.Breathing.BreathingMinigame.Instance.ForceHide();
            onDarknessEnd?.Invoke();
        }
    }
}
