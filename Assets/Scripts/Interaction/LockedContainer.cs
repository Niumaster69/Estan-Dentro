using UnityEngine;
using EstanDentro.UI;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Wrapper de "container abrible" que se abre cuando algo (un CombinationLock,
    /// un evento, o llamada manual) dispara OpenContainer().
    ///
    /// Al abrirse:
    ///   - Dispara Animator.SetBool(boolParam, true) (la animacion del prefab del casillero/lonchera)
    ///   - Entrega un item al Inventory (linterna, destornillador, etc)
    ///   - Muestra una nota interior (NoteOverlay) y la registra en Inventory
    ///   - Muestra un mensaje en el ObjectiveHUD
    ///
    /// Wire en Inspector:
    ///   - El CombinationLock del mismo GameObject (o adyacente) tiene un evento onSolved.
    ///   - En Inspector, arrastra este componente y elegi LockedContainer.OpenContainer.
    /// </summary>
    public class LockedContainer : MonoBehaviour
    {
        [Header("Animator (animacion de apertura)")]
        [SerializeField] private Animator animator;
        [SerializeField, Tooltip("Nombre del parametro Bool que dispara la animacion. Para Casillero/Lonchera es 'Open' con mayuscula.")]
        private string animatorBoolParam = "Open";

        [Header("Item entregado al abrir (opcional)")]
        [SerializeField, Tooltip("Id del item que se agrega al Inventory. Ej: 'linterna', 'destornillador'. Vacio = no entrega item.")]
        private string itemToGiveId;
        [SerializeField] private string itemToGiveDisplayName;

        [Header("Nota interior (opcional)")]
        [SerializeField] private string interiorNoteTitle;
        [SerializeField, TextArea(3, 8)] private string interiorNoteBody;
        [SerializeField, Tooltip("Si true, abre el NoteOverlay automaticamente al abrir el container.")]
        private bool autoShowNote = true;

        [Header("HUD (opcional)")]
        [SerializeField, Tooltip("Mensaje que aparece en ObjectiveHUD al abrir. Vacio = no muestra HUD.")]
        private string hudMessage;
        [SerializeField] private float hudDisplayTime = 4f;

        [Header("Eventos especiales")]
        [SerializeField, Tooltip("Si true, al abrir dispara el BlackoutEvent.TriggerBlackout() (apaga las luces). Util para la lonchera de la linterna.")]
        private bool triggerBlackoutOnOpen = false;

        [Header("Comportamiento")]
        [SerializeField, Tooltip("Si true, solo se puede abrir una vez (idempotente).")]
        private bool oneShot = true;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            // Auto-find si no se asigno en Inspector
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Llamar desde un CombinationLock.onSolved o desde otro evento para abrir el container.
        /// </summary>
        public void OpenContainer()
        {
            if (oneShot && IsOpen) return;
            IsOpen = true;
            Debug.Log($"[LockedContainer] Abriendo '{name}'. " +
                      $"itemId='{itemToGiveId}' | " +
                      $"hudMessage='{hudMessage}' | " +
                      $"triggerBlackoutOnOpen={triggerBlackoutOnOpen}");

            if (animator != null)
            {
                bool hasParam = false;
                foreach (var p in animator.parameters)
                    if (p.name == animatorBoolParam) { hasParam = true; break; }
                if (hasParam) animator.SetBool(animatorBoolParam, true);
                else Debug.LogError($"[LockedContainer] '{name}': Animator '{animator.name}' no tiene Bool '{animatorBoolParam}'.");
            }
            else
            {
                Debug.LogError($"[LockedContainer] '{name}': no hay Animator. Asigna uno en Inspector.");
            }

            if (!string.IsNullOrEmpty(itemToGiveId))
            {
                Inventory.Inventory.Instance?.RegisterItem(itemToGiveId, itemToGiveDisplayName);
            }

            if (!string.IsNullOrEmpty(interiorNoteTitle))
            {
                Inventory.Inventory.Instance?.RegisterNote(interiorNoteTitle, interiorNoteBody);
                if (autoShowNote)
                    NoteOverlay.Show(interiorNoteTitle, interiorNoteBody);
            }

            if (!string.IsNullOrEmpty(hudMessage))
                ObjectiveHUD.Show(hudMessage, hudDisplayTime);

            // Dispara el BlackoutEvent (apaga las luces) si la flag esta activa.
            // La linterna de la escena se desbloquea automaticamente porque al recoger el
            // item "linterna" en RegisterItem(...) la Flashlight chequea Inventory.HasItem
            // en su Update y se auto-unlock.
            if (triggerBlackoutOnOpen)
            {
                if (BlackoutEvent.Instance != null) BlackoutEvent.Instance.TriggerBlackout();
                else Debug.LogWarning($"[LockedContainer] '{name}': triggerBlackoutOnOpen=true pero no hay BlackoutEvent en la escena.");
            }
        }
    }
}
