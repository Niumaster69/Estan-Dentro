using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using EstanDentro.UI;
using EstanDentro.Breathing;
using EstanDentro.Stress;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Puerta bloqueable: NO usa fisicas (no se abre por contacto). Solo responde a Interact (E).
    /// Si esta locked: suena audio de "puerta cerrada", sacude la puerta y muestra HUD.
    /// Si esta unlocked: toggle del Animator Bool 'Open'.
    ///
    /// Para desbloquear con password:
    ///   1. Pon un CombinationLock cerca de la puerta.
    ///   2. En el Inspector del CombinationLock -> evento onSolved -> arrastra esta puerta -> LockedDoor.Unlock().
    /// </summary>
    public class LockedDoor : Interactable
    {
        [Header("Estado")]
        [SerializeField, Tooltip("Si true, la puerta no se abre. Llama a Unlock() para desbloquear.")]
        private bool isLocked = true;

        [Header("Animator")]
        [SerializeField, Tooltip("Animator de la puerta. Si es null, se busca en este GO o en hijos.")]
        private Animator animator;
        [SerializeField, Tooltip("Nombre del parametro Bool. Por convencion: 'Open'.")]
        private string animatorBoolParam = "Open";
        [SerializeField, Tooltip("Si true, fuerza Open=false en Awake (asegura puerta cerrada al iniciar).")]
        private bool forceClosedOnAwake = true;
        [SerializeField, Tooltip("Si true al desbloquear, abre la puerta automaticamente.")]
        private bool autoOpenOnUnlock = false;

        [Header("Feedback locked")]
        [SerializeField, TextArea(1, 3), Tooltip("Mensaje HUD al intentar abrir mientras esta locked. Vacio = no muestra.")]
        private string lockedHudMessage = "Esta cerrada.";
        [SerializeField] private float hudMessageSeconds = 1.5f;
        [SerializeField, Tooltip("Audio al intentar abrir mientras esta locked (jaloneo / chapa).")]
        private AudioClip lockedAudioClip;
        [SerializeField, Range(0f, 1f)] private float lockedAudioVolume = 0.85f;

        [Header("Sacudida visual al intentar abrir locked")]
        [SerializeField, Tooltip("Transform que se sacude. Si null, sacude este mismo. Util si la puerta gira sobre un pivot hijo.")]
        private Transform shakeTarget;
        [SerializeField, Tooltip("Eje local de la sacudida (Y para puertas con bisagra vertical).")]
        private Vector3 shakeAxis = Vector3.up;
        [SerializeField, Tooltip("Grados de sacudida pico.")]
        private float shakeAngle = 4f;
        [SerializeField, Tooltip("Duracion total de la sacudida.")]
        private float shakeDuration = 0.45f;
        [SerializeField, Tooltip("Numero de oscilaciones.")]
        private float shakeFrequency = 8f;

        [Header("Disparador de respiracion al fallar")]
        [SerializeField, Tooltip("Cuanto estres sumar cuando el player intenta abrir y esta locked. 0 = no sumar.")]
        private float stressOnLockedTry = 25f;
        [SerializeField, Tooltip("Segundos que se fuerza el minijuego de respiracion al fallar. 0 = no forzar (deja al stress decidir si aparece).")]
        private float forceBreathingSeconds = 15f;
        [SerializeField, Tooltip("Cooldown (s) entre disparos del minijuego. Evita spamear al hacer click repetido en la puerta.")]
        private float breathingTriggerCooldown = 8f;

        [Header("Bloqueo fisico (anti-empuje)")]
        [SerializeField, Tooltip("Si true, en Awake congela el Rigidbody y desactiva el HingeJoint para que la puerta no se mueva al chocar el player. Recomendado.")]
        private bool freezePhysicsOnAwake = true;
        [SerializeField, Tooltip("Tambien busca Rigidbody/HingeJoint en hijos (ej. cuando la puerta esta dentro de un FBX).")]
        private bool freezeIncludeChildren = true;

        [Header("Eventos")]
        public UnityEvent onTriedWhileLocked;
        public UnityEvent onUnlocked;
        public UnityEvent onOpened;

        // Runtime
        private AudioSource audioSrc;
        private bool isShaking;
        private bool currentOpen;
        private Quaternion shakeBaseLocalRotation;
        private bool shakeBaseCaptured;
        private float lastBreathingTriggerTime = -999f;

        public bool IsLocked => isLocked;
        public bool IsOpen => currentOpen;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (shakeTarget == null) shakeTarget = transform;
            shakeBaseLocalRotation = shakeTarget.localRotation;
            shakeBaseCaptured = true;

            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null)
            {
                audioSrc = gameObject.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0.7f;
            }

            if (forceClosedOnAwake && animator != null && HasBoolParam(animator, animatorBoolParam))
            {
                animator.SetBool(animatorBoolParam, false);
                currentOpen = false;
            }

            if (freezePhysicsOnAwake) FreezePhysics();
        }

        private void FreezePhysics()
        {
            // Rigidbodies
            var rbs = freezeIncludeChildren
                ? GetComponentsInChildren<Rigidbody>(true)
                : GetComponents<Rigidbody>();
            foreach (var rb in rbs)
            {
                if (rb == null) continue;
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // HingeJoints: HingeJoint no es Behaviour (no tiene .enabled). Lo bloqueamos con limits a 0
            // y desactivamos motor/spring. Con el Rigidbody kinematic encima, el joint queda inerte.
            var hinges = freezeIncludeChildren
                ? GetComponentsInChildren<HingeJoint>(true)
                : GetComponents<HingeJoint>();
            foreach (var hj in hinges)
            {
                if (hj == null) continue;
                hj.useMotor = false;
                hj.useSpring = false;
                hj.useLimits = true;
                var limits = hj.limits;
                limits.min = 0f;
                limits.max = 0f;
                hj.limits = limits;
            }
        }

        public override void Interact()
        {
            // Defensa: si hay un overlay modal activo (minijuego de respiracion, ajustes, notas...)
            // ignorar la interaccion para que no se traslape con la UI activa.
            if (EstanDentro.UI.OverlayBlocker.IsBlocking) return;

            if (isLocked)
            {
                TryFeedbackLocked();
                onTriedWhileLocked?.Invoke();
                return;
            }
            ToggleOpen();
        }

        private void TryFeedbackLocked()
        {
            if (lockedAudioClip != null && audioSrc != null)
                audioSrc.PlayOneShot(lockedAudioClip, lockedAudioVolume);
            if (!string.IsNullOrEmpty(lockedHudMessage))
                ObjectiveHUD.Show(lockedHudMessage, hudMessageSeconds);
            if (!isShaking) StartCoroutine(ShakeRoutine());

            // Disparador del minijuego de respiracion: con cooldown para no spamear
            if (Time.time - lastBreathingTriggerTime >= breathingTriggerCooldown)
            {
                lastBreathingTriggerTime = Time.time;
                if (stressOnLockedTry > 0f && StressSystem.Instance != null)
                    StressSystem.Instance.Add(stressOnLockedTry);
                if (forceBreathingSeconds > 0f && BreathingMinigame.Instance != null)
                {
                    BreathingMinigame.Instance.ForceShow();
                    StartCoroutine(ReleaseForcedBreathingAfter(forceBreathingSeconds));
                }
            }
        }

        private IEnumerator ReleaseForcedBreathingAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (BreathingMinigame.Instance != null) BreathingMinigame.Instance.ForceHide();
        }

        private IEnumerator ShakeRoutine()
        {
            if (!shakeBaseCaptured || shakeTarget == null) yield break;
            isShaking = true;
            Quaternion baseRot = shakeTarget.localRotation;
            float t = 0f;
            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / shakeDuration);
                float decay = 1f - p; // amortiguacion lineal
                float angle = Mathf.Sin(p * shakeFrequency * Mathf.PI * 2f) * shakeAngle * decay;
                shakeTarget.localRotation = baseRot * Quaternion.AngleAxis(angle, shakeAxis.normalized);
                yield return null;
            }
            shakeTarget.localRotation = baseRot;
            isShaking = false;
        }

        private void ToggleOpen()
        {
            if (animator == null || !HasBoolParam(animator, animatorBoolParam)) return;
            currentOpen = !currentOpen;
            animator.SetBool(animatorBoolParam, currentOpen);
            if (currentOpen) onOpened?.Invoke();
        }

        public void Unlock()
        {
            if (!isLocked) return;
            isLocked = false;
            onUnlocked?.Invoke();
            Debug.Log($"[LockedDoor] '{name}' desbloqueada.");
            if (autoOpenOnUnlock) ToggleOpen();
        }

        public void Lock()
        {
            isLocked = true;
        }

        private static bool HasBoolParam(Animator a, string paramName)
        {
            foreach (var p in a.parameters)
                if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool) return true;
            return false;
        }
    }
}
