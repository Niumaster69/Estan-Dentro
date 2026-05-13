using UnityEngine;
using UnityEngine.Events;
using EstanDentro.Network;

namespace EstanDentro.Interaction
{
    public class CombinationLock : Interactable
    {
        [Header("Combinacion")]
        [SerializeField, Tooltip("Codigo correcto. Cantidad de digitos = cantidad de ruedas.")]
        private int[] correctCode = new int[] { 1, 7, 0, 5 };

        [Header("Feedback")]
        [SerializeField] private bool autoCloseOnSolved = true;
        [SerializeField] private bool oneShot = true;

        [Header("Mision contextual (opcional)")]
        [SerializeField, Tooltip("Si se setea, al primer intento de abrir el lock aparece una mision secundaria en el inventario. Al resolver, se marca completada.")]
        private string contextualMissionId = "";
        [SerializeField, Tooltip("Texto de la mision contextual. Ej: 'Descifra el codigo del armario'.")]
        private string contextualMissionText = "";

        [Header("Logro")]
        [SerializeField, Tooltip("Si true, al resolver SIN equivocarse desbloquea el logro 'cerradura_primera' (Maestro de cerraduras).")]
        private bool grantsCerraduraPrimera = true;

        [Header("Audio")]
        [SerializeField, Tooltip("Click cuando se cambia un digito de la rueda. Llamado desde LockOverlay via PlayDigitClick().")]
        private AudioClip digitClickClip;
        [SerializeField, Range(0f, 1f)] private float digitClickVolume = 0.7f;
        [SerializeField, Tooltip("Sonido al resolver la combinacion correctamente.")]
        private AudioClip solvedClip;
        [SerializeField, Range(0f, 1f)] private float solvedVolume = 0.9f;

        [Header("Eventos")]
        public UnityEvent onSolved;
        public UnityEvent onFailed;

        public bool IsSolved { get; private set; }
        public int FailsCount { get; private set; }
        public int[] CorrectCode => correctCode;

        public override void Interact()
        {
            if (oneShot && IsSolved) return;

            // Agregar mision contextual al primer intento (si esta configurada y aun no se agrego).
            if (!string.IsNullOrEmpty(contextualMissionId)
                && Inventory.Inventory.Instance != null
                && !Inventory.Inventory.Instance.HasMission(contextualMissionId))
            {
                Inventory.Inventory.Instance.AddMission(contextualMissionId,
                    contextualMissionText,
                    Inventory.Inventory.MissionCategory.Secundaria);
                EstanDentro.UI.ObjectiveHUD.PulseCircle();
                EstanDentro.UI.ObjectiveHUD.Notify("Nueva pista: " + contextualMissionText, 4f);
            }

            LockOverlay.Open(this);
        }

        public void NotifyAttempt(int[] attempt)
        {
            if (CodeMatches(attempt))
            {
                IsSolved = true;
                Debug.Log($"[Lock] Resuelto en '{name}' con {FailsCount} fallos.");

                // Completar mision contextual al resolver
                if (!string.IsNullOrEmpty(contextualMissionId)
                    && Inventory.Inventory.Instance != null)
                {
                    Inventory.Inventory.Instance.CompleteMission(contextualMissionId);
                }

                // Logro 'cerradura_primera': resolver sin equivocarse
                if (grantsCerraduraPrimera && FailsCount == 0)
                    GameSession.TryUnlockLogro("cerradura_primera");

                if (solvedClip != null)
                {
                    var am = EstanDentro.Audio.AudioManager.Instance;
                    if (am != null) am.PlaySFX(solvedClip, solvedVolume);
                    else AudioSource.PlayClipAtPoint(solvedClip, transform.position, solvedVolume);
                }

                onSolved?.Invoke();
                if (autoCloseOnSolved) LockOverlay.Close();
            }
            else
            {
                FailsCount++;
                Debug.Log($"[Lock] Fallo en '{name}'. Intento: {string.Join("", attempt)} (totalFallos={FailsCount})");
                onFailed?.Invoke();
            }
        }

        /// <summary>Llamado por LockOverlay cada vez que el usuario rota un digito.</summary>
        public void PlayDigitClick()
        {
            if (digitClickClip == null) return;
            var am = EstanDentro.Audio.AudioManager.Instance;
            if (am != null) am.PlayUI(digitClickClip, digitClickVolume);
            else AudioSource.PlayClipAtPoint(digitClickClip, transform.position, digitClickVolume);
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
