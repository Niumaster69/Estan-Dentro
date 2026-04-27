using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EstanDentro.Stress
{
    [DefaultExecutionOrder(-100)]
    public class StressSystem : MonoBehaviour
    {
        public static StressSystem Instance { get; private set; }

        [Header("Estres")]
        [SerializeField, Range(1f, 200f)] private float maxStress = 100f;
        [SerializeField, Range(0f, 200f)] private float startStress = 0f;
        [SerializeField, Tooltip("Puntos de estres que bajan por segundo de forma natural. 0 = no decay.")]
        private float passiveDecayPerSecond = 0f;

        [Header("Debug (quitar antes de entrega)")]
        [SerializeField] private bool debugKeysEnabled = true;
        [SerializeField] private float debugStep = 10f;

        public float CurrentStress { get; private set; }
        public float MaxStress => maxStress;
        public float Normalized => CurrentStress / maxStress;
        public bool IsCollapsed { get; private set; }

        public event Action<float, float> OnStressChanged;
        public event Action OnCollapse;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            CurrentStress = Mathf.Clamp(startStress, 0f, maxStress);
        }

        private void Start()
        {
            OnStressChanged?.Invoke(CurrentStress, maxStress);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsCollapsed && passiveDecayPerSecond > 0f && CurrentStress > 0f)
                Add(-passiveDecayPerSecond * Time.deltaTime, silent: false);

            if (debugKeysEnabled) PollDebugKeys();
        }

        private void PollDebugKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.kKey.wasPressedThisFrame) Add(debugStep);
            if (kb.jKey.wasPressedThisFrame) Add(-debugStep);
            if (kb.rKey.wasPressedThisFrame) ResetTo(0f);
        }

        public void Add(float delta) => Add(delta, silent: false);

        private void Add(float delta, bool silent)
        {
            if (IsCollapsed && delta >= 0f) return;
            if (IsCollapsed && delta < 0f) IsCollapsed = false; // un alivio te saca del colapso
            float prev = CurrentStress;
            CurrentStress = Mathf.Clamp(CurrentStress + delta, 0f, maxStress);
            if (!Mathf.Approximately(prev, CurrentStress))
                OnStressChanged?.Invoke(CurrentStress, maxStress);
            if (CurrentStress >= maxStress && !IsCollapsed) Collapse();
        }

        public void ResetTo(float value)
        {
            IsCollapsed = false;
            CurrentStress = Mathf.Clamp(value, 0f, maxStress);
            OnStressChanged?.Invoke(CurrentStress, maxStress);
        }

        private void Collapse()
        {
            IsCollapsed = true;
            Debug.Log("[Stress] Collapse — estres maximo alcanzado");
            OnCollapse?.Invoke();
        }
    }
}
