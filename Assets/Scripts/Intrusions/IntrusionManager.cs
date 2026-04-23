using System;
using UnityEngine;
using UnityEngine.Events;

namespace EstanDentro.Intrusions
{
    public enum IntrusionType { None, Observer, Iracundo, Infante }

    /// <summary>
    /// Hub central de intrusiones. Skeleton sin efectos: solo Debug.Log + UnityEvents
    /// a los que se van a suscribir post-procesado (Henry ID 10), audios (Carlos) y SilhouetteManager.
    /// Singleton por escena.
    /// </summary>
    public class IntrusionManager : MonoBehaviour
    {
        public static IntrusionManager Instance { get; private set; }

        [Serializable] public class IntrusionTypedEvent : UnityEvent<IntrusionType> {}

        [Header("Evento generico (todas las intrusiones)")]
        public IntrusionTypedEvent onIntrusionTriggered;

        [Header("Eventos por tipo (para wiring en Inspector)")]
        public UnityEvent onObserver;
        public UnityEvent onIracundo;
        public UnityEvent onInfante;

        [Header("Debug")]
        public bool logToConsole = true;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Trigger(IntrusionType type)
        {
            if (type == IntrusionType.None) return;

            if (logToConsole) Debug.Log($"[Intrusion] Triggered: {type} @ {Time.time:F2}s", this);

            onIntrusionTriggered?.Invoke(type);
            switch (type)
            {
                case IntrusionType.Observer: onObserver?.Invoke(); break;
                case IntrusionType.Iracundo: onIracundo?.Invoke(); break;
                case IntrusionType.Infante:  onInfante?.Invoke();  break;
            }
        }
    }
}
