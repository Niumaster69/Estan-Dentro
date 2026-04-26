using UnityEngine;
using UnityEngine.Events;

namespace EstanDentro.Interaction
{
    public class CombinationLock : Interactable
    {
        [Header("Combinacion")]
        [SerializeField, Tooltip("Codigo correcto. Cantidad de digitos = cantidad de ruedas.")]
        private int[] correctCode = new int[] { 7, 4, 2 };

        [Header("Feedback")]
        [SerializeField] private bool autoCloseOnSolved = true;
        [SerializeField] private bool oneShot = true;

        [Header("Eventos")]
        public UnityEvent onSolved;
        public UnityEvent onFailed;

        public bool IsSolved { get; private set; }
        public int[] CorrectCode => correctCode;

        public override void Interact()
        {
            if (oneShot && IsSolved) return;
            LockOverlay.Open(this);
        }

        public void NotifyAttempt(int[] attempt)
        {
            if (CodeMatches(attempt))
            {
                IsSolved = true;
                Debug.Log($"[Lock] Resuelto en '{name}'.");
                onSolved?.Invoke();
                if (autoCloseOnSolved) LockOverlay.Close();
            }
            else
            {
                Debug.Log($"[Lock] Fallo en '{name}'. Intento: {string.Join("", attempt)}");
                onFailed?.Invoke();
            }
        }

        private bool CodeMatches(int[] attempt)
        {
            if (attempt == null || attempt.Length != correctCode.Length) return false;
            for (int i = 0; i < correctCode.Length; i++)
                if (attempt[i] != correctCode[i]) return false;
            return true;
        }
    }
}
