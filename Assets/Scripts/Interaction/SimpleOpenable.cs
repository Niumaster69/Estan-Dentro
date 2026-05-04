using UnityEngine;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Casillero "falso" que se abre con un click (E / Cross) y NO requiere combinacion.
    /// Util para los casilleros vacios que distraen del real.
    ///
    /// Setup:
    ///   - Pegar este script al GameObject del casillero.
    ///   - Asignar el Animator del prefab.
    ///   - Asegurarse de que tenga un Collider (para que el InteractionSystem lo detecte).
    /// </summary>
    public class SimpleOpenable : Interactable
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField, Tooltip("Nombre del parametro Bool en el Animator. Para los prefabs Casillero/Lonchera es 'Open' con mayuscula.")]
        private string animatorBoolParam = "Open";

        [Header("Comportamiento")]
        [SerializeField, Tooltip("Si true, abre una sola vez y se queda abierto. Si false, cada E lo abre/cierra (toggle).")]
        private bool oneShot = false;
        [SerializeField, Tooltip("Mensaje opcional al abrir (ej. 'Casillero vacio').")]
        private string hudMessageOnOpen;

        private bool opened;

        private void Awake()
        {
            // Auto-find si no se asigno en Inspector (busca en este GO o en hijos).
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) Debug.LogWarning($"[SimpleOpenable] '{name}' no tiene Animator asignado ni en hijos.");
        }

        public override void Interact()
        {
            // Si oneShot y ya esta abierto, ignora (no permite cerrar).
            if (oneShot && opened) return;

            // Toggle: si esta cerrado abre, si esta abierto cierra.
            bool newState = !opened;
            opened = newState;

            if (animator != null)
            {
                if (HasParam(animator, animatorBoolParam))
                {
                    animator.SetBool(animatorBoolParam, newState);
                    Debug.Log($"[SimpleOpenable] '{name}' set bool '{animatorBoolParam}' = {newState}.");
                }
                else
                {
                    Debug.LogError($"[SimpleOpenable] '{name}': el Animator '{animator.name}' no tiene un parametro Bool llamado '{animatorBoolParam}'. Revisa el Animator Controller (tab Parameters).");
                }
            }
            else
            {
                Debug.LogError($"[SimpleOpenable] '{name}': no encontre Animator en este GO ni en hijos. Asigna uno en Inspector.");
            }

            // El HUD solo se muestra al ABRIR, no al cerrar.
            if (newState && !string.IsNullOrEmpty(hudMessageOnOpen))
                EstanDentro.UI.ObjectiveHUD.Show(hudMessageOnOpen, 2f);
        }

        private static bool HasParam(Animator a, string paramName)
        {
            foreach (var p in a.parameters)
                if (p.name == paramName) return true;
            return false;
        }
    }
}
