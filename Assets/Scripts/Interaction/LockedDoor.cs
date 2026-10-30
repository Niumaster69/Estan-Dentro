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

        [Header("Cerradura integrada (estilo loquer/taquilla)")]
        [SerializeField, Tooltip("Si se asigna, al intentar abrir la puerta locked se abre DIRECTAMENTE el UI de esta cerradura. Mas natural que tener el candado como GO separado. Si null, se autobusca en el mismo GO o hijos.")]
        private CombinationLock integratedLock;
        [SerializeField, Tooltip("Si true, en Awake se suscribe automaticamente al onSolved del integratedLock para llamar Unlock(). Asi no hay que wirear UnityEvent.")]
        private bool autoBindIntegratedLock = true;

        [Header("Animacion por codigo (fallback si Animator no funciona)")]
        [SerializeField, Tooltip("Si true Y no hay Animator, la puerta gira con un coroutine. Util cuando el modelo no tiene Animator configurado.")]
        private bool useCodeAnimationFallback = true;
        [SerializeField, Tooltip("Transform que rota al abrir. Si null, se usa este transform. Si la puerta tiene un pivot hijo, asignalo aqui.")]
        private Transform doorPivotTransform;
        [SerializeField, Tooltip("Rotacion delta al abrir (grados en X, Y, Z LOCAL). Pon 90 en el eje correcto. Ej: (0,90,0) gira sobre Y, (90,0,0) sobre X, (0,0,90) sobre Z. Signo controla direccion.")]
        private Vector3 codeOpenEulerDelta = new Vector3(0f, 90f, 0f);
        [SerializeField, Tooltip("Si true, la rotacion se aplica en espacio MUNDO (no local). Util si el pivot tiene rotacion rara y quieres girar sobre el eje Y del mundo (vertical).")]
        private bool codeRotateInWorldSpace = false;
        [SerializeField, Tooltip("Duracion de la animacion por codigo (segundos).")]
        private float codeAnimDuration = 0.9f;

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
        [Tooltip("Llave (id del inventario) que abre esta puerta, p. ej. llave_4b. Vacío = no se abre con llave.")]
        public string llaveId = "";
        public string llaveNombre = "la llave";
        bool TieneLlave => !string.IsNullOrEmpty(llaveId) && Inventory.Inventory.Instance != null && Inventory.Inventory.Instance.HasItem(llaveId);
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

            // Autobuscar cerradura integrada en mismo GO o hijos
            if (integratedLock == null) integratedLock = GetComponent<CombinationLock>();
            if (integratedLock == null) integratedLock = GetComponentInChildren<CombinationLock>();
            // Suscribir automaticamente para que al resolver el codigo se desbloquee la puerta
            if (autoBindIntegratedLock && integratedLock != null)
            {
                integratedLock.onSolved.AddListener(Unlock);
                Debug.Log($"[LockedDoor] '{name}' auto-bind a CombinationLock '{integratedLock.name}'. Al resolver el codigo se desbloquea esta puerta.");
            }

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

        public override string Accion => isLocked && TieneLlave ? "Abrir con " + llaveNombre : isLocked && integratedLock != null && !integratedLock.IsSolved && TieneCandadoVisible() ? "Empujar la puerta" : "Abrir la puerta";

        bool? candadoVisible;
        bool TieneCandadoVisible()
        {
            if (candadoVisible == null)
            {
                candadoVisible = false;
                foreach (var cv in FindObjectsByType<EstanDentro.Colegio.CandadoVisual>(FindObjectsSortMode.None))
                    if (cv.cerradura == integratedLock) { candadoVisible = true; break; }
            }
            return candadoVisible.Value;
        }


        public override void Interact()
        {
            // Defensa: si hay un overlay modal activo (minijuego de respiracion, ajustes, notas...)
            // ignorar la interaccion para que no se traslape con la UI activa.
            if (EstanDentro.UI.OverlayBlocker.IsBlocking) return;

            if (isLocked && TieneLlave)
            {
                EstanDentro.Audio.SonidoCorto.Juego("casillero/pestillo", transform.position + Vector3.up, 0.8f, 0.8f, 0.8f);
                ObjectiveHUD.Show($"La llave gira. Se abrió.", 2f);
                Unlock();
                if (!IsOpen) ToggleOpen();
                return;
            }
            if (isLocked)
            {
                // Patron loquer/taquilla: si hay una cerradura integrada NO resuelta, abrir directamente
                // su UI sin necesidad de objeto separado para el candado.
                // si la cerradura tiene un candado de verdad colgando (CandadoVisual), la clave se pone EN EL CANDADO:
                // la puerta no abre otra pantalla de clave (había dos sistemas para lo mismo)
                if (integratedLock != null && !integratedLock.IsSolved && TieneCandadoVisible())
                {
                    TryFeedbackLocked();
                    return;
                }
                if (integratedLock != null && !integratedLock.IsSolved)
                {
                    LockOverlay.Open(integratedLock);
                    return;
                }
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

        private Coroutine codeAnimCo;

        private void ToggleOpen()
        {
            bool animatorOK = animator != null && HasBoolParam(animator, animatorBoolParam);

            if (animatorOK)
            {
                currentOpen = !currentOpen;
                animator.SetBool(animatorBoolParam, currentOpen);
                Debug.Log($"[LockedDoor] '{name}' ToggleOpen: animator.SetBool('{animatorBoolParam}', {currentOpen}) OK.");
                if (currentOpen) onOpened?.Invoke();
                return;
            }

            // Fallback por codigo: rotar el transform
            if (useCodeAnimationFallback)
            {
                currentOpen = !currentOpen;
                Transform pivot = doorPivotTransform != null ? doorPivotTransform : transform;
                if (codeAnimCo != null) StopCoroutine(codeAnimCo);
                codeAnimCo = StartCoroutine(CodeAnimateOpen(pivot, currentOpen));
                Debug.Log($"[LockedDoor] '{name}' ToggleOpen: animacion por codigo (open={currentOpen}).");
                if (currentOpen) onOpened?.Invoke();
                return;
            }

            if (animator == null)
                Debug.LogWarning($"[LockedDoor] '{name}' ToggleOpen: NO HAY Animator asignado ni en hijos, y useCodeAnimationFallback=false. La puerta no se abrira visualmente.");
            else
                Debug.LogWarning($"[LockedDoor] '{name}' ToggleOpen: Animator no tiene Bool param '{animatorBoolParam}', y useCodeAnimationFallback=false.");
        }

        [SerializeField, Tooltip("Si true, al abrir gira hacia el lado contrario a quien la empuja (nunca contra el jugador).")]
        private bool abrirLejosDelJugador = true;
        private Vector3 deltaUsado;

        /// <summary>Elige el sentido de giro que aleja la hoja de la puerta del jugador (cámara principal).</summary>
        private Vector3 DeltaLejosDelJugador(Transform pivot)
        {
            var cam = Camera.main;
            var r = GetComponentInChildren<Renderer>();
            if (cam == null || r == null) return codeOpenEulerDelta;
            Vector3 centro = r.bounds.center, eje = pivot.position;
            Vector3 jugador = cam.transform.position;
            Vector3 a = Quaternion.Euler(codeOpenEulerDelta) * (centro - eje) + eje;
            Vector3 b = Quaternion.Euler(-codeOpenEulerDelta) * (centro - eje) + eje;
            float da = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(jugador.x, jugador.z));
            float db = Vector2.Distance(new Vector2(b.x, b.z), new Vector2(jugador.x, jugador.z));
            return da >= db ? codeOpenEulerDelta : -codeOpenEulerDelta;
        }

        private System.Collections.IEnumerator CodeAnimateOpen(Transform pivot, bool opening)
        {
            Vector3 delta = opening ? codeOpenEulerDelta : -codeOpenEulerDelta;
            if (codeRotateInWorldSpace && abrirLejosDelJugador)
            {
                if (opening) deltaUsado = DeltaLejosDelJugador(pivot);
                delta = opening ? deltaUsado : -deltaUsado;
            }

            if (codeRotateInWorldSpace)
            {
                Quaternion fromRot = pivot.rotation;
                Quaternion toRot = Quaternion.Euler(delta) * fromRot;
                float t = 0f;
                while (t < codeAnimDuration)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / codeAnimDuration);
                    float eased = 1f - Mathf.Pow(1f - p, 3f);
                    pivot.rotation = Quaternion.Slerp(fromRot, toRot, eased);
                    yield return null;
                }
                pivot.rotation = toRot;
            }
            else
            {
                Quaternion fromRot = pivot.localRotation;
                Quaternion toRot = fromRot * Quaternion.Euler(delta);
                float t = 0f;
                while (t < codeAnimDuration)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / codeAnimDuration);
                    float eased = 1f - Mathf.Pow(1f - p, 3f);
                    pivot.localRotation = Quaternion.Slerp(fromRot, toRot, eased);
                    yield return null;
                }
                pivot.localRotation = toRot;
            }
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
