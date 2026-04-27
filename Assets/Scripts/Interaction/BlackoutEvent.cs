using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EstanDentro.Interaction
{
    public class BlackoutEvent : MonoBehaviour
    {
        public static BlackoutEvent Instance { get; private set; }

        [Header("Configuracion")]
        [SerializeField, Tooltip("Luces especificas a excluir del apagon. La linterna se excluye automaticamente.")]
        private List<Light> excludedLights = new List<Light>();

        [Header("Debug")]
        [SerializeField] private bool debugKeyEnabled = true;

        public event Action OnBlackout;
        public bool IsBlackedOut { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!debugKeyEnabled) return;
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) TriggerBlackout();
        }

        public void TriggerBlackout()
        {
            if (IsBlackedOut) return;
            int count = 0;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var lt in lights)
            {
                if (excludedLights.Contains(lt)) continue;
                if (lt.GetComponent<Flashlight>() != null) continue;
                if (!lt.enabled) continue;
                lt.enabled = false;
                count++;
            }
            IsBlackedOut = true;
            Debug.Log($"[Blackout] Apagaron {count} luces.");

            // Desbloquea todas las linternas de la escena (overlay narrativo).
            var flashlights = FindObjectsByType<Flashlight>(FindObjectsSortMode.None);
            foreach (var f in flashlights) f.Unlock();

            OnBlackout?.Invoke();
        }
    }
}
