using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EstanDentro.UI;
using EstanDentro.Player;

namespace EstanDentro.Interaction
{
    /// <summary>
    /// Activacion por PROXIMIDAD (distancia) al ducto del techo.
    ///
    /// Setup minimo:
    ///   - Crear empty GameObject en la posicion del piso DEBAJO del ducto.
    ///   - Pegar este script. Listo. No necesita Collider ni nada mas.
    ///   - Cuando el player se acerca a `activationRadius` metros, si tiene el destornillador,
    ///     aparece un prompt "Mira arriba [E] / [Cross]" abajo en pantalla.
    ///
    /// Al presionar E: bloquea input, inclina la camara hacia arriba, suena audio
    /// (mesa+destornillador+caida), fade a negro, slides, loading → Acto 2.
    /// </summary>
    public class DuctoTechoInteractable : MonoBehaviour
    {
        [Header("Activacion (mirar hacia arriba)")]
        [SerializeField, Tooltip("Pitch en grados para considerar 'mirando arriba'. Negativo = mirar arriba. -30 es '30 grados sobre el horizonte'.")]
        private float lookUpThresholdPitch = -30f;
        [SerializeField, Tooltip("Si true, ademas exige proximidad. Si false, basta con mirar arriba (mas permisivo).")]
        private bool alsoRequireProximity = false;
        [SerializeField, Tooltip("Solo si alsoRequireProximity = true. Radio en metros.")]
        private float activationRadius = 8f;
        [SerializeField, Tooltip("Si true, dibuja una esfera en Scene view.")]
        private bool drawGizmo = true;
        [SerializeField, Tooltip("Si true, loguea estado cada 1 segundo (pitch + distancia).")]
        private bool debugLogState = false;

        [Header("Requisito")]
        [SerializeField] private string requiredItemId = "destornillador";
        [SerializeField] private string promptText = "Mira arriba [E] / [Cross]";

        [Header("Rejilla — caida por fisica (recomendado por Henry)")]
        [SerializeField, Tooltip("Opcion A: ya tiene Rigidbody+Collider configurado. Asignalo aqui.")]
        private Rigidbody rejillaRigidbody;
        [SerializeField, Tooltip("Opcion B (mas facil): solo asigna el GameObject de la rejilla. Si no tiene Rigidbody, el script le anade uno al activarse.")]
        private Transform rejillaTransform;
        [SerializeField, Tooltip("Masa que se le pone al Rigidbody si se crea automaticamente.")]
        private float rejillaAutoMass = 2f;
        [SerializeField, Tooltip("Si la rejilla esta parented a algo (ej. el ducto), despegarla al activar para que la fisica funcione bien.")]
        private bool detachRejillaFromParent = true;
        [SerializeField, Tooltip("Empuje vertical (m/s) que se le aplica al destrabarla, simulando el aflojado. 0 = solo cae.")]
        private float rejillaDropImpulse = 0.5f;
        [SerializeField, Tooltip("Pequeno torque aleatorio al caer para que no caiga perfectamente recta.")]
        private float rejillaRandomTorque = 1.5f;

        [Header("Animator (fallback, opcional)")]
        [SerializeField, Tooltip("Solo si NO usas Rigidbody. Si esta vacio, el script intenta encontrar Animator en el padre.")]
        private Animator animator;
        [SerializeField] private string animatorBoolParam = "Open";

        [Header("Escritorio que se arrastra")]
        [SerializeField, Tooltip("El GameObject del escritorio (ej. 'Escritorio v2 (10)'). Se arrastra hasta quedar bajo el ducto.")]
        private Transform escritorio;
        [SerializeField, Tooltip("Empty con la posicion final del escritorio (debajo del ducto). Si lo dejas null, usa escritorioMoveOffset.")]
        private Transform escritorioTargetPos;
        [SerializeField, Tooltip("Solo si escritorioTargetPos es null. Desplazamiento relativo a la pos actual del escritorio.")]
        private Vector3 escritorioMoveOffset = new Vector3(0f, 0f, 1.2f);
        [SerializeField] private float escritorioMoveDuration = 1.6f;
        [SerializeField, Tooltip("Wobble de rotacion del escritorio mientras se desliza (grados). 0 = sin rotacion, 3 = sutil.")]
        private float deskRotationWobble = 3f;
        [SerializeField, Tooltip("Inclinacion hacia adelante del escritorio al arrastrarlo (grados). Simula que el peso se inclina un poco al moverse.")]
        private float deskForwardLean = 4f;

        [Header("Reaccion al caer la rejilla (flinch)")]
        [SerializeField, Tooltip("Distancia hacia atras que retrocede la cabeza al caer la rejilla (m).")]
        private float flinchBackDistance = 0.15f;
        [SerializeField, Tooltip("Pitch adicional durante el flinch. Positivo = mira un poco hacia abajo (sigue la rejilla cayendo). 0 = solo retrocede.")]
        private float flinchPitchDelta = 6f;
        [SerializeField, Tooltip("Duracion total del flinch (retroceso + vuelta a su posicion).")]
        private float flinchDuration = 0.7f;

        [Header("Player — subir al escritorio y al ducto")]
        [SerializeField, Tooltip("Empty con la posicion del player parado SOBRE el escritorio (recomendado: hijo del escritorio para que se mueva con el).")]
        private Transform playerOnDeskPos;
        [SerializeField, Tooltip("Empty con la posicion del player ENTRANDO al ducto. Si es null, no entra (queda mirando arriba).")]
        private Transform playerInDuctPos;
        [SerializeField] private float climbToDeskDuration = 1.6f;
        [SerializeField] private float climbIntoDuctDuration = 1.6f;

        [Header("Player — feel del trepar (que no se sienta robotico)")]
        [SerializeField, Tooltip("Amplitud del bob vertical durante el trepar. ~0.06 = 6cm. 0 = sin bob.")]
        private float climbBobAmount = 0.07f;
        [SerializeField, Tooltip("Frecuencia del bob en Hz (cuantos pasos/sacudidas por segundo).")]
        private float climbBobFrequency = 2.6f;
        [SerializeField, Tooltip("Curvatura del trayecto al trepar (arco vertical extra a mitad de subida).")]
        private float climbArcHeight = 0.18f;
        [SerializeField, Tooltip("Pitch dip al subir al escritorio: cuanto mira hacia abajo a mitad del trepar (mira el escritorio para 'apoyar los pies'). 0 = nada.")]
        private float climbDeskPitchDip = 12f;
        [SerializeField, Tooltip("Wobble de yaw durante el trepar (grados). Pequeno movimiento lateral organico.")]
        private float climbYawWobble = 2.5f;
        [SerializeField, Tooltip("Wobble de roll (cabeza ladeada) durante el trepar. Muy sutil.")]
        private float climbRollWobble = 1.5f;

        [Header("Camara — coreografia")]
        [SerializeField, Tooltip("Pitch al que se endereza la camara apenas empieza a moverse la mesa. 0 = mirando al frente (recomendado), +10 = un poco al piso.")]
        private float lookAtDeskPitch = 0f;
        [SerializeField, Tooltip("Cuanto tarda la cabeza en girar+enderezarse al frente. Idealmente igual o menor que la duracion del movimiento de la mesa.")]
        private float lookDownDuration = 0.7f;
        [SerializeField, Tooltip("Mientras la mesa rueda, gira lentamente la cabeza para seguirla con la mirada. 0 = quieto.")]
        private float trackDeskYawSpeed = 3f;
        [SerializeField, Tooltip("Tiempo extra observando despues de que la mesa termina de rodar (con respiracion organica).")]
        private float deskLingerSeconds = 0.6f;

        [Header("Camara — respiracion organica (vida)")]
        [SerializeField, Tooltip("Amplitud de wobble en pitch durante fases 'estaticas' (grados).")]
        private float idleBreathPitch = 1.2f;
        [SerializeField, Tooltip("Amplitud de wobble en yaw (grados).")]
        private float idleBreathYaw = 1.5f;
        [SerializeField, Tooltip("Amplitud de wobble en roll (cabeza ladeada, grados).")]
        private float idleBreathRoll = 0.8f;
        [SerializeField, Tooltip("Pitch final mirando hacia arriba al ducto. Negativo = mirando arriba. -70 a -85 es 'casi vertical'.")]
        private float cameraTargetPitch = -75f;
        [SerializeField] private float cameraTiltDuration = 1.4f;
        [SerializeField, Tooltip("Si true, gira al player (yaw) automaticamente para que mire al escritorio antes de la cinematica.")]
        private bool autoOrientToDesk = true;
        [SerializeField] private float orientToDeskDuration = 0.5f;

        [Header("Audio (Carlos los asigna despues)")]
        [SerializeField] private AudioClip mesaRodandoClip;
        [SerializeField] private AudioClip destornilladorClip;
        [SerializeField] private AudioClip ductoCayendoClip;
        [SerializeField] private float volumeScale = 0.85f;

        [Header("Tiempos")]
        [SerializeField] private float waitBeforeMesa = 0.3f;
        [SerializeField] private float waitBeforeDestornillador = 0.6f;
        [SerializeField] private float fadeToBlackDuration = 1.0f;

        [Header("Slides en negro")]
        [SerializeField] private CinematicSlide[] slides = new CinematicSlide[] {
            new CinematicSlide { text = "Subi.", durationSeconds = 2.2f },
            new CinematicSlide { text = "Mas arriba.", durationSeconds = 2.0f },
            new CinematicSlide { text = "Solo un poco mas.", durationSeconds = 2.5f },
        };
        [SerializeField] private float slideFadeIn = 0.6f;
        [SerializeField] private float slideFadeOut = 0.6f;
        [SerializeField] private int slideFontSize = 38;
        [SerializeField] private Color slideTextColor = new Color(0.92f, 0.89f, 0.83f, 1f);

        [Header("Transicion")]
        [SerializeField] private string nextSceneName = "EsecenaUno-DuctosDeVentilacion";
        [SerializeField] private string loadingTip = "Inhala 4. Sostén 4. Sigue arriba.";

        [Header("Mision")]
        [SerializeField] private string completedMissionId = "salir_salon";

        [System.Serializable]
        public class CinematicSlide
        {
            [TextArea(1, 4)] public string text;
            public float durationSeconds = 2.5f;
            public AudioClip audio;
        }

        // Runtime
        private bool isActive; // mirando arriba + (opcional) en proximidad + tiene item
        private bool triggered;
        private AudioSource audioSource;
        private PlayerController playerController;
        private Transform cameraPivot;
        private Transform playerTransform;
        private float lastLogTime;
        private bool warnedNoPlayer;
        private bool warnedNoInventory;

        // Prompt overlay
        private Canvas promptCanvas;
        private Text promptTextUI;

        // Cinematic overlay
        private Canvas cinematicCanvas;
        private CanvasGroup cinematicCanvasGroup;
        private Text slideTextUI;
        private CanvasGroup slideTextGroup;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInParent<Animator>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.7f;
            }
        }

        private void Update()
        {
            // Cachear referencia al player la primera vez
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    cameraPivot = playerController.CameraPivot;
                    playerTransform = playerController.transform;
                    Debug.Log($"[Ducto] PlayerController encontrado. Mi posicion: {transform.position}, Player: {playerTransform.position}, Radio: {activationRadius}m");
                }
                else
                {
                    if (!warnedNoPlayer)
                    {
                        Debug.LogError("[Ducto] No encontre PlayerController en la escena. Verifica que existe el GameObject del Player.");
                        warnedNoPlayer = true;
                    }
                    return;
                }
            }

            if (Inventory.Inventory.Instance == null && !warnedNoInventory)
            {
                Debug.LogError("[Ducto] No hay Inventory.Instance en la escena. Crea un GameObject con componente Inventory.");
                warnedNoInventory = true;
            }

            if (triggered) return;

            // Pitch de la camara: negativo = mirando hacia arriba
            float pitch = NormalizePitch(cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f);
            bool lookingUp = pitch <= lookUpThresholdPitch;

            // Distancia (opcional)
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inProximity = !alsoRequireProximity || dist <= activationRadius;

            bool hasItem = HasRequiredItem();
            bool nowActive = lookingUp && inProximity && hasItem;

            // Log periodico para diagnostico
            if (debugLogState && Time.unscaledTime - lastLogTime > 1f)
            {
                lastLogTime = Time.unscaledTime;
                Debug.Log($"[Ducto] pitch={pitch:F1} (umbral {lookUpThresholdPitch}) | mirando arriba: {lookingUp} | dist: {dist:F2}m | item: {hasItem} | activo: {nowActive}");
            }

            if (nowActive != isActive)
            {
                isActive = nowActive;
                if (nowActive) Debug.Log($"[Ducto] >>> Activo: mirando arriba + tiene destornillador. Listo para activar (E).");
                else Debug.Log($"[Ducto] <<< Inactivo. Pitch={pitch:F1}, mirandoArriba={lookingUp}, item={hasItem}.");
            }

            ShowPrompt(isActive);

            if (isActive)
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                bool pressed = (kb != null && kb.eKey.wasPressedThisFrame)
                            || (gp != null && gp.buttonSouth.wasPressedThisFrame);
                if (pressed) Activate();
            }
        }

        private bool HasRequiredItem()
        {
            return Inventory.Inventory.Instance != null
                && Inventory.Inventory.Instance.HasItem(requiredItemId);
        }

        private void Activate()
        {
            triggered = true;
            ShowPrompt(false);
            // Diagnostico: imprimir el estado de los campos importantes en este momento
            Debug.Log($"[Ducto] Activate en GameObject '{gameObject.name}' (instanceID={GetInstanceID()})." +
                      $" rejillaRigidbody={(rejillaRigidbody != null ? rejillaRigidbody.name : "NULL")}" +
                      $" | rejillaTransform={(rejillaTransform != null ? rejillaTransform.name : "NULL")}" +
                      $" | escritorio={(escritorio != null ? escritorio.name : "NULL")}" +
                      $" | playerOnDeskPos={(playerOnDeskPos != null ? playerOnDeskPos.name : "NULL")}" +
                      $" | playerInDuctPos={(playerInDuctPos != null ? playerInDuctPos.name : "NULL")}");
            StartCoroutine(SubidaSequence());
        }

        private void DropRejilla()
        {
            Rigidbody rb = rejillaRigidbody;

            // Si no hay Rigidbody asignado pero hay Transform, conseguir o crear el Rigidbody
            if (rb == null && rejillaTransform != null)
            {
                rb = rejillaTransform.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = rejillaTransform.gameObject.AddComponent<Rigidbody>();
                    rb.mass = rejillaAutoMass;
                    Debug.Log($"[Ducto] Rigidbody anadido automaticamente a '{rejillaTransform.name}' (mass={rejillaAutoMass}).");
                }
                // Asegurar que tenga collider para chocar contra el piso
                if (rejillaTransform.GetComponent<Collider>() == null)
                {
                    var col = rejillaTransform.gameObject.AddComponent<BoxCollider>();
                    Debug.Log($"[Ducto] BoxCollider anadido automaticamente a '{rejillaTransform.name}' (size: {col.size}).");
                }
            }

            if (rb == null)
            {
                Debug.LogWarning("[Ducto] DropRejilla: ni 'rejillaRigidbody' ni 'rejillaTransform' asignados en el Inspector. La rejilla NO va a caer.");
                if (animator != null && HasBoolParam(animator, animatorBoolParam))
                {
                    animator.SetBool(animatorBoolParam, true);
                    Debug.Log("[Ducto] Fallback: Animator activado.");
                }
                return;
            }

            // Despegar de su padre para que la fisica funcione (si el padre es kinematico, lo arrastra)
            if (detachRejillaFromParent && rb.transform.parent != null)
            {
                Debug.Log($"[Ducto] Despegando rejilla de su padre '{rb.transform.parent.name}' para fisica limpia.");
                rb.transform.SetParent(null, true);
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();

            if (rejillaDropImpulse != 0f)
                rb.AddForce(Vector3.down * rejillaDropImpulse, ForceMode.VelocityChange);
            if (rejillaRandomTorque > 0f)
                rb.AddTorque(new Vector3(
                    Random.Range(-rejillaRandomTorque, rejillaRandomTorque),
                    Random.Range(-rejillaRandomTorque, rejillaRandomTorque),
                    Random.Range(-rejillaRandomTorque, rejillaRandomTorque)),
                    ForceMode.VelocityChange);

            Debug.Log($"[Ducto] Rejilla soltada: useGravity={rb.useGravity}, isKinematic={rb.isKinematic}, mass={rb.mass}, pos={rb.transform.position}, parent={(rb.transform.parent != null ? rb.transform.parent.name : "null")}");
        }

        // ---------- SECUENCIA CINEMATICA ----------

        private IEnumerator SubidaSequence()
        {
            Debug.Log("[Ducto] Iniciando cinematica de subida.");

            // 1. Bloquear input + desactivar el componente PlayerController y el CharacterController
            //    (si solo bajamos InputEnabled, HandleMove() sigue llamando controller.Move sobre un CC apagado y spammea errores)
            CharacterController cc = playerController != null ? playerController.GetComponent<CharacterController>() : null;
            if (playerController != null)
            {
                playerController.InputEnabled = false;
                playerController.enabled = false;
            }
            if (cc != null) cc.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Capturar el pitch inicial (el player esta mirando arriba al ducto)
            float initialPitch = cameraPivot != null ? NormalizePitch(cameraPivot.localEulerAngles.x) : 0f;

            // 2. Pausa breve mirando arriba (con respiracion organica, no estatico)
            yield return BreathWait(0.4f, initialPitch);

            // 3. Audio destornillador + respiracion (player aflojando la rejilla)
            PlayClip(destornilladorClip);
            yield return BreathWait(waitBeforeDestornillador, initialPitch);

            // 4. CAE LA REJILLA + flinch hacia atras (camara retrocede, mira la caida, vuelve)
            PlayClip(ductoCayendoClip);
            DropRejilla();
            yield return FlinchAtRejillaFall(initialPitch);

            // 5. AHORA suena la mesa rodando + la cabeza se endereza + yaw hacia escritorio
            yield return new WaitForSeconds(waitBeforeMesa);
            PlayClip(mesaRodandoClip);
            Coroutine moveDeskCo = StartCoroutine(MoveEscritorio());
            if (autoOrientToDesk && escritorio != null)
                StartCoroutine(OrientPlayerYawTowards(escritorio.position, orientToDeskDuration));
            yield return TiltCameraTo(lookAtDeskPitch, lookDownDuration);
            // mientras la mesa termina de rodar: respiracion organica + tracking del escritorio con la mirada
            yield return WatchDeskAlive();
            yield return moveDeskCo;

            // 6. El player trepa al escritorio (con bob, arco y dip de pitch para no sentirse robotico)
            if (playerOnDeskPos != null && playerTransform != null)
                yield return ClimbPlayerTo(playerOnDeskPos.position, climbToDeskDuration, climbDeskPitchDip);

            // 7. La camara ahora mira hacia arriba al ducto
            yield return TiltCameraTo(cameraTargetPitch, cameraTiltDuration);

            // 8. Player se mete dentro del ducto (sin dip de pitch porque ya esta mirando arriba; solo bob + wobble)
            if (playerInDuctPos != null && playerTransform != null)
                yield return ClimbPlayerTo(playerInDuctPos.position, climbIntoDuctDuration, 0f);

            // 9. Fade a negro
            BuildCinematicOverlay();
            yield return FadeCanvasGroup(cinematicCanvasGroup, 0f, 1f, fadeToBlackDuration);

            // 9. Slides
            slideTextGroup.alpha = 0f;
            foreach (var slide in slides)
            {
                slideTextUI.text = slide.text;
                if (slide.audio != null) PlayClip(slide.audio);
                yield return FadeCanvasGroup(slideTextGroup, 0f, 1f, slideFadeIn);
                yield return new WaitForSecondsRealtime(slide.durationSeconds);
                yield return FadeCanvasGroup(slideTextGroup, 1f, 0f, slideFadeOut);
            }

            // 10. Completar mision principal del Acto 1
            if (!string.IsNullOrEmpty(completedMissionId) && Inventory.Inventory.Instance != null)
                Inventory.Inventory.Instance.CompleteMission(completedMissionId);

            // 11. Loading screen → Acto 2 (transicion limpia, sin overlay).
            //     El cierre de partida + EndOfChapterOverlay se dispara recien al final del Acto 3.
            Time.timeScale = 1f;
            SceneTransition.LoadScene(nextSceneName, tip: loadingTip);
        }

        private IEnumerator MoveEscritorio()
        {
            if (escritorio == null) yield break;
            Vector3 from = escritorio.position;
            Vector3 to = escritorioTargetPos != null ? escritorioTargetPos.position : from + escritorioMoveOffset;
            Quaternion fromRot = escritorio.rotation;
            float rotSeed = Random.Range(0f, 100f);
            float t = 0f;
            while (t < escritorioMoveDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / escritorioMoveDuration);
                float eased = 1f - Mathf.Pow(1f - p, 2f);
                escritorio.position = Vector3.Lerp(from, to, eased);

                // Rotacion mientras se mueve (se ve como si lo arrastraran de verdad)
                float intensity = Mathf.Sin(p * Mathf.PI); // 0 → 1 → 0 (bell)
                float wobblePitch = (Mathf.PerlinNoise(rotSeed, t * 4f) - 0.5f) * 2f * deskRotationWobble * intensity;
                float wobbleRoll = (Mathf.PerlinNoise(t * 3.5f, rotSeed + 50f) - 0.5f) * 2f * deskRotationWobble * intensity;
                float leanForward = deskForwardLean * intensity;
                escritorio.rotation = fromRot * Quaternion.Euler(wobblePitch + leanForward, 0f, wobbleRoll);

                yield return null;
            }
            escritorio.position = to;
            escritorio.rotation = fromRot;
        }

        // Pausa con respiracion organica (sustituye WaitForSeconds para que la camara nunca quede estatica)
        private IEnumerator BreathWait(float seconds, float basePitch)
        {
            float seed = Random.Range(0f, 100f);
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                ApplyBreathing(seed, t, basePitch);
                yield return null;
            }
        }

        // Cuando cae la rejilla: la cabeza retrocede + mira un poco hacia abajo (siguiendo la caida)
        // y vuelve a su pitch inicial. Curva de campana, sin sacudon.
        private IEnumerator FlinchAtRejillaFall(float basePitch)
        {
            if (cameraPivot == null) yield break;
            Vector3 startLocalPos = cameraPivot.localPosition;
            Vector3 backOffset = new Vector3(0f, 0f, -flinchBackDistance);
            float seed = Random.Range(0f, 100f);
            float t = 0f;
            while (t < flinchDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / flinchDuration);
                float bell = Mathf.Sin(p * Mathf.PI); // 0 → 1 → 0
                cameraPivot.localPosition = startLocalPos + backOffset * bell;
                // pitch + respiracion sutil
                float bp = (Mathf.PerlinNoise(seed, t * 0.6f) - 0.5f) * 2f * idleBreathPitch;
                float by = (Mathf.PerlinNoise(seed + 30f, t * 0.5f) - 0.5f) * 2f * idleBreathYaw;
                float br = (Mathf.PerlinNoise(seed + 60f, t * 0.4f) - 0.5f) * 2f * idleBreathRoll;
                cameraPivot.localRotation = Quaternion.Euler(basePitch + flinchPitchDelta * bell + bp, by, br);
                yield return null;
            }
            cameraPivot.localPosition = startLocalPos;
        }

        // Mientras la mesa termina de rodar, la cabeza la sigue con la mirada (yaw lerp lento)
        // y aplica respiracion organica (Perlin noise) sobre el cameraPivot.
        // Esto evita la sensacion 'estatica' de que la camara queda mirando un solo punto fijo.
        private IEnumerator WatchDeskAlive()
        {
            if (escritorio == null || cameraPivot == null) yield break;
            float seed = Random.Range(0f, 100f);
            float t = 0f;
            float waitForLanding = escritorioMoveDuration * 1.05f;
            float total = waitForLanding + deskLingerSeconds;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                // tracking yaw del player hacia el escritorio (lerp suave)
                if (playerTransform != null && trackDeskYawSpeed > 0f)
                {
                    Vector3 toDesk = escritorio.position - playerTransform.position;
                    toDesk.y = 0f;
                    if (toDesk.sqrMagnitude > 0.0001f)
                    {
                        Quaternion target = Quaternion.LookRotation(toDesk.normalized);
                        playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, target,
                            Time.unscaledDeltaTime * trackDeskYawSpeed);
                    }
                }
                // respiracion organica sobre el pitch base
                ApplyBreathing(seed, t, lookAtDeskPitch);
                yield return null;
            }
        }

        // Aplica wobble organico (Perlin) al cameraPivot sobre un pitch base dado.
        private void ApplyBreathing(float seed, float t, float basePitch)
        {
            if (cameraPivot == null) return;
            float bp = (Mathf.PerlinNoise(seed, t * 0.6f) - 0.5f) * 2f * idleBreathPitch;
            float by = (Mathf.PerlinNoise(seed + 30f, t * 0.5f) - 0.5f) * 2f * idleBreathYaw;
            float br = (Mathf.PerlinNoise(seed + 60f, t * 0.4f) - 0.5f) * 2f * idleBreathRoll;
            cameraPivot.localRotation = Quaternion.Euler(basePitch + bp, by, br);
        }

        private IEnumerator ClimbPlayerTo(Vector3 target, float duration, float pitchDipAmount)
        {
            if (playerTransform == null) yield break;
            Vector3 from = playerTransform.position;
            float startPitch = cameraPivot != null ? NormalizePitch(cameraPivot.localEulerAngles.x) : 0f;
            float noiseSeed = Random.Range(0f, 100f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);

                // ventana de intensidad: 0 al inicio, 1 al medio, 0 al final (suaviza arranque/aterrizaje)
                float intensity = Mathf.Sin(p * Mathf.PI);

                // posicion: lerp + arco vertical + bob ritmico
                Vector3 pos = Vector3.Lerp(from, target, eased);
                float arc = intensity * climbArcHeight;
                float bob = Mathf.Sin(t * climbBobFrequency * Mathf.PI * 2f) * climbBobAmount * intensity;
                pos.y += arc + bob;
                playerTransform.position = pos;

                // pitch: dip hacia el escritorio a mitad del trepar y vuelta
                if (cameraPivot != null)
                {
                    float pitch = startPitch + intensity * pitchDipAmount;
                    // wobble organico con perlin
                    float yawW = (Mathf.PerlinNoise(noiseSeed, t * 1.4f) - 0.5f) * 2f * climbYawWobble * intensity;
                    float rollW = (Mathf.PerlinNoise(t * 1.1f, noiseSeed + 50f) - 0.5f) * 2f * climbRollWobble * intensity;
                    cameraPivot.localRotation = Quaternion.Euler(pitch, yawW, rollW);
                    if (playerController != null) playerController.SetPitch(pitch);
                }
                yield return null;
            }
            playerTransform.position = target;
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(startPitch, 0f, 0f);
                if (playerController != null) playerController.SetPitch(startPitch);
            }
        }

        private IEnumerator TiltCameraTo(float targetPitch, float duration)
        {
            if (playerController == null || cameraPivot == null) yield break;
            float startPitch = NormalizePitch(cameraPivot.localEulerAngles.x);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                float pitch = Mathf.Lerp(startPitch, targetPitch, eased);
                playerController.SetPitch(pitch);
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                yield return null;
            }
            playerController.SetPitch(targetPitch);
            cameraPivot.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
        }

        private IEnumerator OrientPlayerYawTowards(Vector3 worldTarget, float duration)
        {
            if (playerTransform == null) yield break;
            Vector3 dir = worldTarget - playerTransform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) yield break;
            Quaternion fromRot = playerTransform.rotation;
            Quaternion toRot = Quaternion.LookRotation(dir.normalized);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                playerTransform.rotation = Quaternion.Slerp(fromRot, toRot, eased);
                yield return null;
            }
            playerTransform.rotation = toRot;
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip, volumeScale);
        }

        private static bool HasBoolParam(Animator a, string paramName)
        {
            foreach (var p in a.parameters) if (p.name == paramName) return true;
            return false;
        }

        // ---------- helpers ----------

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            cg.alpha = to;
        }

        // ---------- prompt UI ----------

        private void ShowPrompt(bool show)
        {
            if (promptCanvas == null) BuildPromptOverlay();
            if (promptCanvas == null) return;
            if (promptCanvas.gameObject.activeSelf != show)
                promptCanvas.gameObject.SetActive(show);
        }

        private void BuildPromptOverlay()
        {
            var go = new GameObject("DuctoPrompt_Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            promptCanvas = go.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            promptCanvas.sortingOrder = 165;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            var panelRT = panelGo.GetComponent<RectTransform>();
            panelRT.SetParent(promptCanvas.transform, false);
            panelRT.anchorMin = new Vector2(0.5f, 0f);
            panelRT.anchorMax = new Vector2(0.5f, 0f);
            panelRT.pivot = new Vector2(0.5f, 0f);
            panelRT.sizeDelta = new Vector2(420f, 60f);
            panelRT.anchoredPosition = new Vector2(0f, 220f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.7f);
            panelImg.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.SetParent(panelRT, false);
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
            promptTextUI = textGo.AddComponent<Text>();
            promptTextUI.font = GetBodyFont();
            promptTextUI.text = promptText;
            promptTextUI.alignment = TextAnchor.MiddleCenter;
            promptTextUI.fontSize = 22;
            promptTextUI.fontStyle = FontStyle.Bold;
            promptTextUI.color = new Color(0.96f, 0.85f, 0.42f, 1f);
            promptTextUI.raycastTarget = false;

            promptCanvas.gameObject.SetActive(false);
        }

        // ---------- cinematic overlay ----------

        private void BuildCinematicOverlay()
        {
            if (cinematicCanvas != null) return;

            var go = new GameObject("DuctoCinematic_Canvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            cinematicCanvas = go.AddComponent<Canvas>();
            cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cinematicCanvas.sortingOrder = 240;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            cinematicCanvasGroup = go.AddComponent<CanvasGroup>();
            cinematicCanvasGroup.alpha = 0f;
            cinematicCanvasGroup.blocksRaycasts = true;

            var bgGo = new GameObject("Black", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(cinematicCanvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = Color.black;

            var textGo = new GameObject("Slide", typeof(RectTransform));
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.SetParent(cinematicCanvas.transform, false);
            textRT.anchorMin = textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.sizeDelta = new Vector2(1400f, 240f);
            textRT.anchoredPosition = Vector2.zero;
            slideTextGroup = textGo.AddComponent<CanvasGroup>();
            slideTextGroup.alpha = 0f;

            slideTextUI = textGo.AddComponent<Text>();
            slideTextUI.font = GetBodyFont();
            slideTextUI.text = "";
            slideTextUI.alignment = TextAnchor.MiddleCenter;
            slideTextUI.fontSize = slideFontSize;
            slideTextUI.fontStyle = FontStyle.Italic;
            slideTextUI.color = slideTextColor;
            slideTextUI.raycastTarget = false;
        }

        private static Font GetBodyFont()
        {
            if (MainMenuController.SharedBodyFont != null) return MainMenuController.SharedBodyFont;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        // Visualizar el radio en Scene view
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            Gizmos.color = new Color(0.96f, 0.85f, 0.42f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
