using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using EstanDentro.Stress;
using EstanDentro.Network;
using EstanDentro.UI;

namespace EstanDentro.Breathing
{
    [DefaultExecutionOrder(-30)]
    public class BreathingMinigame : MonoBehaviour
    {
        public static BreathingMinigame Instance { get; private set; }

        // Eventos para que otros sistemas (SuffocationSystem) reaccionen al desempeno del jugador.
        public event System.Action OnCycleSuccess;
        public event System.Action OnCycleFail;


        [Header("Activacion")]
        [SerializeField, Range(0f, 1f)] private float showAtStressNormalized = 0.4f;
        [SerializeField, Range(0f, 1f)] private float hideAtStressNormalized = 0.25f;
        [SerializeField] private bool alwaysVisibleForDebug = false;

        // Si !=0, el minijuego se mantiene visible por triggers narrativos / cinematicas
        // ignorando el rango de stress. Llamar ForceShow() / ForceHide() desde otros sistemas.
        private bool forcedVisible;

        [Header("Ciclo (segundos)")]
        [SerializeField, Tooltip("Tiempo del INHALA. Real diafragmatico: 3-4s. Default 3.5s.")]
        private float inhaleSeconds = 3.5f;
        [SerializeField, Tooltip("Tiempo del EXHALA. Mas corto se siente mas natural. Default 3s.")]
        private float exhaleSeconds = 3f;
        [SerializeField, Tooltip("Pausa antes del proximo inhale. Default 1s.")]
        private float pauseSeconds = 1f;
        [SerializeField, Tooltip("Tiempo minimo (s) de exhale sostenido para considerar ciclo OK. Default 1.5s.")]
        private float exhaleMinSustainSeconds = 1.5f;
        [SerializeField, Tooltip("Fallback teclado: tiempo minimo de Space presionado en INHALA para que el ciclo cuente. Default 2s.")]
        private float minInhaleHeldSeconds = 2f;

        [Header("Recompensa / castigo (puntos de estres)")]
        [SerializeField] private float stressDownOnSuccess = 9f;
        [SerializeField] private float stressUpOnFail = 1f;
        [SerializeField, Tooltip("Cantidad de ciclos al inicio donde fallar NO suma estres (modo aprendizaje).")]
        private int freeCyclesAtStart = 2;

        [Header("Tutorial primera aparicion (texto modal — descontinuado)")]
        [SerializeField, Tooltip("Default FALSE — el tutorial textual molesta porque el jugador puede saltarlo. El espacio mental ya ensena por si solo con los prompts y el orbe.")]
        private bool showTutorialFirstTime = false;
        [SerializeField, TextArea(3, 8)] private string tutorialMessage =
            "El miedo subio tu estres.\n\n" +
            "Sigue al orbe. Cuando aparezca el boton, pulsalo para avanzar.";

        [Header("Visual - Vignette diegetico")]
        [SerializeField, Range(0f, 1f), Tooltip("Alpha del vignette al inhalar pleno (bordes mas abiertos / claridad central). Default 0.15.")]
        private float minVignetteAlpha = 0.15f;
        [SerializeField, Range(0f, 1f), Tooltip("Alpha del vignette al exhalar pleno (bordes envuelven la vista). Default 0.7 - menos opresivo.")]
        private float maxVignetteAlpha = 0.7f;
        [SerializeField] private float fadeSeconds = 1f;
        [SerializeField, Tooltip("Color/tinte del vignette durante INHALA. Default casi negro con tinte muy sutil azul.")]
        private Color inhaleColor = new Color(0.03f, 0.04f, 0.06f, 1f);
        [SerializeField, Tooltip("Color/tinte del vignette durante EXHALA. Default mismo dark que inhala - el cambio de hue distrae, mejor solo cambia alpha.")]
        private Color exhaleColor = new Color(0.03f, 0.04f, 0.06f, 1f);
        [SerializeField, Tooltip("Color/tinte del vignette durante PAUSA. Default mismo dark - sin saltos de color.")]
        private Color pauseColor = new Color(0.03f, 0.04f, 0.06f, 1f);
        [SerializeField] private Color labelColor = new Color(0.95f, 0.93f, 0.85f, 0.95f);
        [SerializeField] private Color freeCycleBadgeColor = new Color(0.4f, 0.7f, 0.45f, 0.95f);

        [Header("Espacio Mental — eternal/ethereal sin caja")]
        [SerializeField, Tooltip("Si true, abre el 'espacio mental' como viñeta radial sin panel rectangular.")]
        private bool useMentalSpace = true;
        [SerializeField, Tooltip("Color del vignette etereo (alpha alto en bordes, alpha 0 en centro). Default negro profundo.")]
        private Color mentalSpaceBgColor = new Color(0f, 0f, 0f, 0.92f);
        [SerializeField, Tooltip("Color del panel central (alpha 0 = invisible, solo layout). NO se pinta.")]
        private Color mentalSpacePanelColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField, Tooltip("Color del borde del panel. Alpha 0 por default — sin borde, todo flota.")]
        private Color mentalSpacePanelBorderColor = new Color(0f, 0f, 0f, 0f);
        [Header("Acentos cálidos del espacio mental")]
        [SerializeField] private Color amberAccent = new Color(0.93f, 0.78f, 0.4f, 1f);
        [SerializeField] private Color amberSoft = new Color(0.85f, 0.7f, 0.45f, 0.85f);

        [Header("Audio del Espacio Mental")]
        [SerializeField, Tooltip("Loop ambiental que suena MIENTRAS el espacio mental esta abierto (ej. drone meditativo, latido lento, breathing pad). Se reproduce en bucle, se detiene al cerrar.")]
        private AudioClip mentalSpaceLoopClip;
        [SerializeField, Range(0f, 1f), Tooltip("Volumen del loop ambiental.")]
        private float mentalSpaceLoopVolume = 0.6f;
        [SerializeField, Tooltip("Fade in/out del loop ambiental al abrir/cerrar (segundos).")]
        private float mentalSpaceAudioFade = 1.2f;
        [SerializeField, Tooltip("Audio one-shot al avanzar un nodo (opcional).")]
        private AudioClip nodeAdvanceClip;
        [SerializeField, Range(0f, 1f)] private float nodeAdvanceVolume = 0.7f;
        [SerializeField, Tooltip("Audio one-shot al completar todo el recorrido (opcional).")]
        private AudioClip completedClip;
        [SerializeField, Range(0f, 1f)] private float completedVolume = 0.85f;

        private AudioSource mentalSpaceAudioSrc;
        [Header("Desenfoque del mundo (HDRP) — efecto inventario")]
        [SerializeField, Tooltip("Si true, crea un Volume HDRP con DOF + Vignette que desenfoca el mundo detras del panel.")]
        private bool useWorldBlur = true;
        [SerializeField, Range(0f, 1f), Tooltip("Intensidad del vignette del Volume HDRP cuando el minijuego esta abierto.")]
        private float worldBlurVignette = 0.5f;
        [SerializeField, Tooltip("Velocidad del fade in/out del blur.")]
        private float blurFadeSpeed = 3f;
        [SerializeField, Tooltip("Tamano del panel flotante (ancho x alto).")]
        private Vector2 mentalSpacePanelSize = new Vector2(960f, 680f);
        [SerializeField, Tooltip("Cantidad de nodos del recorrido. Cada uno requiere presionar X para avanzar. 3 = corto/educativo, 5 = mas inmersivo.")]
        private int totalNodes = 3;
        [SerializeField, Tooltip("Tamano del orbe central en pixeles.")]
        private float orbSize = 70f;
        [SerializeField, Tooltip("Tamano de cada nodo del recorrido en pixeles.")]
        private float nodeSize = 28f;
        [SerializeField, Tooltip("Amplitud horizontal de la curva (zigzag) del recorrido.")]
        private float pathAmplitudeX = 150f;
        [SerializeField, Tooltip("Altura vertical total del recorrido.")]
        private float pathHeightY = 380f;
        [SerializeField, Tooltip("Cooldown (segundos) entre clicks X. Fuerza al jugador a respirar antes de poder avanzar.")]
        private float advanceCooldown = 3f;
        [SerializeField, Tooltip("Cooldown (segundos) DESPUES de cerrar manualmente el minigame, antes de poder re-abrirlo. Evita que se dispare de nuevo justo despues de terminar una sesion meditativa.")]
        private float reopenCooldownSeconds = 6f;
        [SerializeField, Tooltip("Pausa inicial (segundos) cuando abre el minigame, antes de empezar el ciclo respiratorio. Da transicion natural en vez de arranque abrupto.")]
        private float introPauseSeconds = 1.5f;
        [SerializeField] private Color nodeInactiveColor = new Color(0.55f, 0.5f, 0.42f, 0.55f);
        [SerializeField] private Color nodeCompletedColor = new Color(0.95f, 0.78f, 0.4f, 1f);
        [SerializeField] private Color orbColor = new Color(1f, 0.95f, 0.82f, 0.9f);
        [SerializeField] private Color breathBarColor = new Color(0.93f, 0.78f, 0.4f, 0.9f);
        [SerializeField, TextArea(2, 5)] private string educationalText =
            "Respira hondo siguiendo el orbe.\nCuando estes listo, pulsa X / Cuadrado para avanzar.";
        [SerializeField] private string completePromptText = "Pulsa X / Cuadrado para avanzar";
        [SerializeField] private string waitPromptText = "Respira... (espera)";
        [SerializeField] private string lastNodePromptText = "Pulsa X / Cuadrado para volver";

        [Header("Minigame UI antiguo (pacer + response al pie) — desactivado por default si Espacio Mental on")]
        [SerializeField, Tooltip("Si true, muestra el pacer abajo y la barra de response. Solo se aplica si Espacio Mental esta off.")]
        private bool showMinigameUI = true;
        [SerializeField, Tooltip("Ancho maximo del pacer bar al lleno (en pixels @1920 ref).")]
        private float pacerMaxWidth = 500f;
        [SerializeField, Tooltip("Y position del pacer en la pantalla. -440 = cerca del borde inferior.")]
        private float pacerY = -440f;
        [SerializeField] private Color pacerColorInhale = new Color(0.85f, 0.7f, 0.28f, 1f);
        [SerializeField] private Color pacerColorExhale = new Color(0.5f, 0.75f, 0.92f, 1f);
        [SerializeField] private Color pacerColorPause = new Color(0.55f, 0.5f, 0.45f, 0.6f);
        [SerializeField] private Color responseBarColor = new Color(0.5f, 0.92f, 0.55f, 0.95f);
        [SerializeField] private Color streakTextColor = new Color(0.92f, 0.89f, 0.83f, 0.9f);
        [SerializeField, Tooltip("Color de flash de borde al completar ciclo OK.")]
        private Color successFlashColor = new Color(0.4f, 0.85f, 0.45f, 0.4f);
        [SerializeField, Tooltip("Duracion del flash de exito en segundos.")]
        private float successFlashDuration = 0.6f;

        private enum Phase { Inhale, Exhale, Pause }
        private Phase phase = Phase.Inhale;
        private float phaseElapsed;
        private float exhaleSustained;
        private float inhaleHeldAccum;
        private bool cycleAlreadyScored;
        private int remainingFreeCycles;

        // UI
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform vignetteRT;
        private Image vignetteImg;
        private Text guideText;
        private Text hintText;
        private Text inputBadge;
        private Text freeCycleBadge;

        // Minigame UI antiguo (modo no-mental-space)
        private RectTransform pacerBgRT;
        private RectTransform pacerFillRT;
        private Image pacerFillImg;
        private RectTransform responseBarBgRT;
        private RectTransform responseBarFillRT;
        private Image responseBarFillImg;
        private Text streakText;
        private Image successFlashImg;
        private int currentStreak;
        private float successFlashElapsed = 999f;

        // Espacio Mental UI
        private GameObject mentalSpaceRoot;
        private Image mentalSpaceBgImg;
        private Image mentalSpacePanelImg;
        private Text educationalLabel;
        private Text completePromptLabel;
        private Text micLabel;
        private RectTransform[] nodeRTs;
        private Image[] nodeImgs;
        private RectTransform orbRT;
        private Image orbImg;
        private RectTransform breathBarFillRT;
        private RectTransform buttonHintRT;
        private Image buttonHintBgImg;
        private Text buttonHintText;
        private int currentNode;
        #pragma warning disable CS0414
        private bool readyToClose; // legacy state — flag de cierre listo, ahora gobernado por atLastNode
        #pragma warning restore CS0414
        private float advanceCooldownTimer;

        // Silenciado de audio durante el espacio mental (se restaura al cerrar)
        private System.Collections.Generic.List<MonoBehaviour> silencedBehaviours = new System.Collections.Generic.List<MonoBehaviour>();

        // Lock del player durante el espacio mental
        private EstanDentro.Player.PlayerController lockedPlayer;
        private bool prevPlayerInputEnabled;
        private CursorLockMode prevCursorLockBeforeMs;
        private bool prevCursorVisibleBeforeMs;

        // Blur Volume HDRP (efecto inventario)
        private Volume blurVolume;
        private VolumeProfile blurProfile;
        private DepthOfField blurDof;
        private Vignette blurVignetteEffect;
        private float blurTargetWeight;
        private bool overlayBlockerRegistered;

        // Variedad del path: se randomiza en cada apertura del minijuego
        private float pathRandomPhase;
        private float pathRandomDir = 1f;
        private float pathRandomFreq = 1.5f;
        private int pathRandomShape;

        // Cooldown post-cierre + intro pause
        private float reopenCooldownTimer;
        private float introPauseTimer;

        // Tutorial UI
        private GameObject tutorialPanel;
        private Text tutorialText;

        // State
        private bool visible;
        private bool tutorialPresentedThisSession;
        private bool tutorialActive;
        private float currentAlpha;
        private float targetAlpha;
        private float prevTimeScaleBeforeTutorial;
        private bool consumeTutorialInputThisFrame;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // Persiste entre escenas. La instancia se crea en salonPrincipal y sobrevive al
            // cambio a Ductos, asi el minijuego puede dispararse alli sin necesidad de un GO
            // por-escena con los mismos clips wireados.
            DontDestroyOnLoad(gameObject);

            BuildUI();
            if (canvas != null) canvas.enabled = false;
            visible = false;
            currentAlpha = 0f;
            targetAlpha = 0f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            remainingFreeCycles = freeCyclesAtStart;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            UpdateAlpha();
            UpdateVisibility();

            if (tutorialActive)
            {
                HandleTutorialInput();
                return;
            }

            if (!visible) return;
            TickCycle();
            ApplyVisuals();
        }

        // ---------- visibility / fade ----------

        private void UpdateAlpha()
        {
            if (canvasGroup == null) return;
            if (!Mathf.Approximately(currentAlpha, targetAlpha))
            {
                float speed = fadeSeconds <= 0f ? 99f : (1f / fadeSeconds);
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.unscaledDeltaTime);
                canvasGroup.alpha = currentAlpha;

                if (currentAlpha <= 0.001f && targetAlpha == 0f && canvas != null && canvas.enabled)
                    canvas.enabled = false;
            }
            // Blur HDRP fade
            if (blurVolume != null)
            {
                blurVolume.weight = Mathf.MoveTowards(blurVolume.weight, blurTargetWeight, blurFadeSpeed * Time.unscaledDeltaTime);
            }
        }

        [SerializeField, Tooltip("Si true, loguea cada segundo el estado del BreathingMinigame (stress, visible, cooldowns). Diagnostico.")]
        private bool logVisibilityState = false;
        private float lastVisLog;

        private void UpdateVisibility()
        {
            if (logVisibilityState && Time.unscaledTime - lastVisLog > 1f)
            {
                lastVisLog = Time.unscaledTime;
                float st = StressSystem.Instance != null ? StressSystem.Instance.Normalized : -1f;
                Debug.Log($"[Breathing][Vis] stress={st:P0} threshold={showAtStressNormalized:P0} visible={visible} forcedVisible={forcedVisible} reopenCD={reopenCooldownTimer:F1}s stressInstance={(StressSystem.Instance != null ? StressSystem.Instance.gameObject.scene.name : "NULL")}");
            }

            // Cooldown post-cierre: no permitir re-abrir por algunos segundos despues de completar
            if (reopenCooldownTimer > 0f)
            {
                reopenCooldownTimer -= Time.unscaledDeltaTime;
                if (visible) TryHide();
                return;
            }

            if (alwaysVisibleForDebug || forcedVisible) { TryShow(); return; }
            if (StressSystem.Instance == null) { TryHide(); return; }
            float t = StressSystem.Instance.Normalized;
            if (!visible && t >= showAtStressNormalized) TryShow();
            else if (visible && t <= hideAtStressNormalized) TryHide();
        }

        /// <summary>
        /// Fuerza el minijuego visible ignorando el stress. Util para cinematicas (despertar,
        /// aparicion del Observador) o triggers narrativos. Llamar ForceHide() para liberar.
        /// </summary>
        public void ForceShow() { forcedVisible = true; }

        /// <summary>
        /// Libera el forzado. Si el stress sigue alto el minijuego se mantiene; si bajo, se oculta.
        /// </summary>
        public void ForceHide() { forcedVisible = false; }

        /// <summary>True si esta visible (por stress o por forzado).</summary>
        public bool IsVisible => visible;

        private void TryShow()
        {
            if (visible) return;
            visible = true;
            if (canvas != null) canvas.enabled = true;

            bool needsTutorial = showTutorialFirstTime && !tutorialPresentedThisSession;
            if (needsTutorial) StartTutorial();
            else BeginCycleNow();
        }

        private void TryHide()
        {
            if (!visible) return;
            visible = false;
            targetAlpha = 0f;
            // Restaurar audio + movement del player + desregistrar overlay + fade out blur + stop loop
            if (useMentalSpace)
            {
                RestoreSceneAudio();
                RestorePlayer();
                if (overlayBlockerRegistered) { OverlayBlocker.Unregister(); overlayBlockerRegistered = false; }
                blurTargetWeight = 0f;
                StopMentalSpaceAudio();
            }
        }

        private void BeginCycleNow()
        {
            ResetCycle();
            // Reset progresion del espacio mental al abrir
            currentNode = 0;
            readyToClose = false;
            advanceCooldownTimer = advanceCooldown; // cooldown inicial: respira al menos una vez antes de poder avanzar
            introPauseTimer = introPauseSeconds; // pausa inicial antes de empezar el ciclo respiratorio
            targetAlpha = 1f;
            // Variar el path para cada apertura — randomizar shape + phase + direction
            pathRandomShape = Random.Range(0, 4);
            pathRandomPhase = Random.Range(0f, Mathf.PI * 2f);
            pathRandomDir = (Random.value < 0.5f) ? -1f : 1f;
            pathRandomFreq = Random.Range(1.2f, 2.2f);
            RepositionNodes();
            // Reset orb a la nueva posicion del nodo 0
            if (orbRT != null && nodeRTs != null && nodeRTs.Length > 0)
                orbRT.anchoredPosition = nodeRTs[0].anchoredPosition;
            // Silenciar audio + lock del player + bloquear interacciones de mundo + blur
            if (useMentalSpace)
            {
                SilenceSceneAudio();
                LockPlayer();
                if (!overlayBlockerRegistered) { OverlayBlocker.Register(); overlayBlockerRegistered = true; }
                blurTargetWeight = 1f;
                StartMentalSpaceAudio();
            }
            // sin pausar
        }

        private void StartMentalSpaceAudio()
        {
            if (mentalSpaceLoopClip == null) return;
            if (mentalSpaceAudioSrc == null)
            {
                mentalSpaceAudioSrc = gameObject.AddComponent<AudioSource>();
                mentalSpaceAudioSrc.playOnAwake = false;
                mentalSpaceAudioSrc.spatialBlend = 0f;
                mentalSpaceAudioSrc.loop = true;
                mentalSpaceAudioSrc.ignoreListenerPause = true;
                mentalSpaceAudioSrc.priority = 0; // alta prioridad
            }
            mentalSpaceAudioSrc.clip = mentalSpaceLoopClip;
            mentalSpaceAudioSrc.volume = 0f;
            mentalSpaceAudioSrc.Play();
            StartCoroutine(FadeAudioSource(mentalSpaceAudioSrc, 0f, mentalSpaceLoopVolume, mentalSpaceAudioFade, false));
        }

        private void StopMentalSpaceAudio()
        {
            if (mentalSpaceAudioSrc == null || !mentalSpaceAudioSrc.isPlaying) return;
            StartCoroutine(FadeAudioSource(mentalSpaceAudioSrc, mentalSpaceAudioSrc.volume, 0f, mentalSpaceAudioFade, true));
        }

        private System.Collections.IEnumerator FadeAudioSource(AudioSource src, float from, float to, float duration, bool stopAtEnd)
        {
            if (src == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                if (src == null) yield break;
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            if (src != null)
            {
                src.volume = to;
                if (stopAtEnd) src.Stop();
            }
        }

        private CharacterController lockedCC;
        private bool prevCCEnabled;
        private bool prevPlayerEnabled;

        private void LockPlayer()
        {
            if (lockedPlayer != null) return; // ya bloqueado
            lockedPlayer = FindFirstObjectByType<EstanDentro.Player.PlayerController>();
            if (lockedPlayer != null)
            {
                prevPlayerInputEnabled = lockedPlayer.InputEnabled;
                prevPlayerEnabled = lockedPlayer.enabled;
                lockedCC = lockedPlayer.GetComponent<CharacterController>();
                if (lockedCC != null) { prevCCEnabled = lockedCC.enabled; lockedCC.enabled = false; }
                // Desactivar el script entero detiene Update -> ni movimiento ni cam look
                lockedPlayer.InputEnabled = false;
                lockedPlayer.enabled = false;
            }
            prevCursorLockBeforeMs = Cursor.lockState;
            prevCursorVisibleBeforeMs = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RestorePlayer()
        {
            // Safety: si el player fue destruido (cambio de escena), no acceder a la ref.
            if (lockedPlayer != null)
            {
                try
                {
                    lockedPlayer.enabled = prevPlayerEnabled;
                    lockedPlayer.InputEnabled = prevPlayerInputEnabled;
                    if (lockedCC != null) lockedCC.enabled = prevCCEnabled;
                }
                catch (System.Exception) { /* player destruido por scene change */ }
            }
            lockedPlayer = null;
            lockedCC = null;
            Cursor.lockState = prevCursorLockBeforeMs;
            Cursor.visible = prevCursorVisibleBeforeMs;
        }

        // Silencia AmbientLoop + FlashlightFlicker + todos los AudioSources de la escena
        // mientras el espacio mental esta activo. Se restaura al cerrar.
        private void SilenceSceneAudio()
        {
            silencedBehaviours.Clear();
            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var b in allBehaviours)
            {
                if (b == this) continue;
                var typeName = b.GetType().FullName;
                if ((typeName == "EstanDentro.Audio.AmbientLoopWithRandomEvents"
                    || typeName == "EstanDentro.Interaction.FlashlightFlicker") && b.enabled)
                {
                    b.enabled = false;
                    silencedBehaviours.Add(b);
                }
            }
            var sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var src in sources)
            {
                if (src.isPlaying) src.Stop();
            }
        }

        private void RestoreSceneAudio()
        {
            foreach (var b in silencedBehaviours)
            {
                if (b != null) b.enabled = true;
            }
            silencedBehaviours.Clear();
        }

        private void ResetCycle()
        {
            phase = Phase.Inhale;
            phaseElapsed = 0f;
            exhaleSustained = 0f;
            inhaleHeldAccum = 0f;
            cycleAlreadyScored = false;
        }

        // ---------- tutorial ----------

        private void StartTutorial()
        {
            tutorialActive = true;
            consumeTutorialInputThisFrame = true;
            prevTimeScaleBeforeTutorial = Time.timeScale;
            Time.timeScale = 0f;

            tutorialPanel.SetActive(true);
            tutorialText.text = tutorialMessage + "\n\n[Pulsa cualquier tecla / boton para empezar]";

            // Fade-in inmediato del canvas (alpha 1)
            currentAlpha = 1f;
            targetAlpha = 1f;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void HandleTutorialInput()
        {
            if (consumeTutorialInputThisFrame) { consumeTutorialInputThisFrame = false; return; }

            var kb = Keyboard.current;
            var gp = Gamepad.current;
            bool any = (kb != null && kb.anyKey.wasPressedThisFrame)
                    || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                      || gp.buttonNorth.wasPressedThisFrame
                                      || gp.buttonEast.wasPressedThisFrame
                                      || gp.buttonWest.wasPressedThisFrame
                                      || gp.startButton.wasPressedThisFrame));
            if (!any) return;

            tutorialActive = false;
            tutorialPresentedThisSession = true;
            tutorialPanel.SetActive(false);
            Time.timeScale = prevTimeScaleBeforeTutorial > 0f ? prevTimeScaleBeforeTutorial : 1f;
            BeginCycleNow();
        }

        // ---------- cycle ----------

        private void TickCycle()
        {
            // Pausa inicial: orbe quieto en escala base hasta que termine
            if (introPauseTimer > 0f)
            {
                introPauseTimer -= Time.deltaTime;
                return;
            }
            phaseElapsed += Time.deltaTime;
            var provider = BreathingInputProvider.Instance;
            bool isFallback = provider != null && provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback;

            // En modo meditacion guiada (mental space) no necesitamos input — solo avanza por tiempo
            if (useMentalSpace)
            {
                switch (phase)
                {
                    case Phase.Inhale:
                        if (phaseElapsed >= inhaleSeconds) Advance(Phase.Exhale);
                        break;
                    case Phase.Exhale:
                        if (phaseElapsed >= exhaleSeconds) Advance(Phase.Pause);
                        break;
                    case Phase.Pause:
                        if (phaseElapsed >= pauseSeconds) Advance(Phase.Inhale);
                        break;
                }
                return;
            }

            // Modo legacy: con scoring por input
            switch (phase)
            {
                case Phase.Inhale:
                    if (isFallback && BreathingInputProvider.FallbackInhaleHeld())
                        inhaleHeldAccum += Time.deltaTime;
                    if (phaseElapsed >= inhaleSeconds) Advance(Phase.Exhale);
                    break;
                case Phase.Exhale:
                    bool exhaleAllowed = !isFallback || inhaleHeldAccum >= minInhaleHeldSeconds;
                    if (exhaleAllowed && provider != null && provider.IsExhalingNow())
                        exhaleSustained += Time.deltaTime;
                    if (!cycleAlreadyScored && exhaleSustained >= exhaleMinSustainSeconds)
                    {
                        ScoreSuccess();
                        cycleAlreadyScored = true;
                    }
                    if (phaseElapsed >= exhaleSeconds)
                    {
                        if (!cycleAlreadyScored) ScoreFail();
                        Advance(Phase.Pause);
                    }
                    break;
                case Phase.Pause:
                    if (phaseElapsed >= pauseSeconds) Advance(Phase.Inhale);
                    break;
            }
        }

        private void Advance(Phase next)
        {
            // En modo meditacion: cada vez que volvemos a Inhale (despues de Pause), se completo un ciclo.
            // Avanzamos el orbe un nodo y damos baja de estres.
            bool cycleCompleted = useMentalSpace && phase == Phase.Pause && next == Phase.Inhale;

            phase = next;
            phaseElapsed = 0f;
            if (next == Phase.Inhale)
            {
                exhaleSustained = 0f;
                inhaleHeldAccum = 0f;
                cycleAlreadyScored = false;
            }

            if (cycleCompleted)
            {
                OnMeditationCycleCompleted();
            }
        }

        private void OnMeditationCycleCompleted()
        {
            // Modo click-to-advance: NO auto-avanzamos nodos al completar ciclo respiratorio.
            // El avance ocurre solo cuando el jugador pulsa X (UpdateMentalSpaceUI).
            // Dejamos el ciclo respiratorio corriendo en loop como guia visual.
        }

        private void ScoreSuccess()
        {
            if (StressSystem.Instance == null) return;
            StressSystem.Instance.Add(-stressDownOnSuccess);
            currentStreak++;
            if (currentNode < totalNodes) currentNode++;
            TriggerSuccessFlash();
            Debug.Log($"[Breathing] Ciclo OK -{stressDownOnSuccess}. Streak={currentStreak}. Nodo {currentNode}/{totalNodes}. Estres={StressSystem.Instance.CurrentStress:F0}");
            OnCycleSuccess?.Invoke();
        }

        private void ScoreFail()
        {
            if (StressSystem.Instance == null) return;

            // Reset streak (visible feedback de que el ciclo no se completo)
            currentStreak = 0;

            // Contar TODOS los fallos para el logro 'respiracion_zen' (incluso los ciclos
            // de aprendizaje "free"). El logro requiere terminar el capitulo sin fallar
            // ningun ciclo, por lo que cualquier fallo cuenta.
            GameSession.BreathingFailedCycles++;
            OnCycleFail?.Invoke();

            if (remainingFreeCycles > 0)
            {
                remainingFreeCycles--;
                Debug.Log($"[Breathing] Ciclo FAIL (sin penalty - aprendizaje). Quedan {remainingFreeCycles} libres.");
                return;
            }
            StressSystem.Instance.Add(stressUpOnFail);
            Debug.Log($"[Breathing] Ciclo FAIL +{stressUpOnFail}. Estres={StressSystem.Instance.CurrentStress:F0}");
        }

        // ---------- visuals ----------

        private void ApplyVisuals()
        {
            // Vignette diegetico:
            //   INHALA: alpha decrece de max -> min (bordes se abren, claridad central crece)
            //   EXHALA: alpha crece de min -> max (bordes cierran, oscuridad envuelve)
            //   PAUSA:  alpha sostenido en max (envuelto, antes del proximo inhale)
            float vignetteAlpha;
            Color tint;
            string guide;
            switch (phase)
            {
                case Phase.Inhale:
                    vignetteAlpha = Mathf.Lerp(maxVignetteAlpha, minVignetteAlpha, phaseElapsed / inhaleSeconds);
                    tint = inhaleColor;
                    guide = "INHALA";
                    break;
                case Phase.Exhale:
                    vignetteAlpha = Mathf.Lerp(minVignetteAlpha, maxVignetteAlpha, phaseElapsed / exhaleSeconds);
                    tint = exhaleColor;
                    guide = cycleAlreadyScored ? "EXHALA  ✓" : "EXHALA";
                    break;
                default:
                    vignetteAlpha = maxVignetteAlpha;
                    tint = pauseColor;
                    guide = "PAUSA";
                    break;
            }
            if (vignetteImg != null)
                vignetteImg.color = new Color(tint.r, tint.g, tint.b, vignetteAlpha);
            guideText.text = guide;

            UpdateInputBadge();
            UpdateHintAndFreeCycleBadge();
            if (useMentalSpace) UpdateMentalSpaceUI();
            else UpdateMinigameUI();
        }

        private void UpdateInputBadge()
        {
            var provider = BreathingInputProvider.Instance;
            bool isFallback = provider == null || provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback;
            if (!isFallback)
            {
                inputBadge.text = phase == Phase.Inhale ? "[ Inhala por la nariz ]"
                                : phase == Phase.Exhale ? "[ Exhala al MANDO ]"
                                : "";
                return;
            }
            // Fallback: mostrar tecla
            switch (phase)
            {
                case Phase.Inhale: inputBadge.text = "[ Mantener ESPACIO / TRIANGLE ]"; break;
                case Phase.Exhale: inputBadge.text = "[ Soltar ESPACIO / TRIANGLE ]"; break;
                default: inputBadge.text = ""; break;
            }
        }

        private void UpdateHintAndFreeCycleBadge()
        {
            var provider = BreathingInputProvider.Instance;
            hintText.text = provider != null && provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback
                ? "El miedo es real, pero no manda. Respira con el ritmo de los bordes."
                : "Sosten el mando cerca de tu boca. Inhala por la nariz, exhala al mando.";

            if (remainingFreeCycles > 0)
            {
                freeCycleBadge.gameObject.SetActive(true);
                freeCycleBadge.text = $"Aprendiendo - {remainingFreeCycles} ciclo(s) sin penalty";
                freeCycleBadge.color = freeCycleBadgeColor;
            }
            else
            {
                freeCycleBadge.gameObject.SetActive(false);
            }
        }

        // ---------- UI build ----------

        private void BuildUI()
        {
            var go = new GameObject("Breathing_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Vignette fullscreen — bordes que pulsan con la respiracion (no tapa el centro)
            var vignetteGo = new GameObject("Breathing_Vignette", typeof(RectTransform));
            vignetteRT = vignetteGo.GetComponent<RectTransform>();
            vignetteRT.SetParent(canvas.transform, false);
            vignetteRT.anchorMin = Vector2.zero;
            vignetteRT.anchorMax = Vector2.one;
            vignetteRT.offsetMin = Vector2.zero;
            vignetteRT.offsetMax = Vector2.zero;
            vignetteImg = vignetteGo.AddComponent<Image>();
            vignetteImg.sprite = CreateVignetteSprite(256);
            vignetteImg.color = new Color(inhaleColor.r, inhaleColor.g, inhaleColor.b, minVignetteAlpha);
            vignetteImg.raycastTarget = false;

            // Texto guia abajo (diegetico, no tapa el centro)
            guideText = MakeText(canvas.transform, "Guide", "INHALA",
                28, FontStyle.Bold,
                new Vector2(400f, 44f), new Vector2(0f, -340f));

            inputBadge = MakeText(canvas.transform, "InputBadge", "",
                18, FontStyle.Bold,
                new Vector2(700f, 32f), new Vector2(0f, -390f));

            hintText = MakeText(canvas.transform, "Hint", "",
                15, FontStyle.Italic,
                new Vector2(900f, 24f), new Vector2(0f, -430f));

            freeCycleBadge = MakeText(canvas.transform, "FreeCycleBadge", "",
                14, FontStyle.Normal,
                new Vector2(500f, 22f), new Vector2(0f, -470f));
            freeCycleBadge.color = freeCycleBadgeColor;
            freeCycleBadge.gameObject.SetActive(false);

            if (useMentalSpace) BuildMentalSpaceUI();
            else if (showMinigameUI) BuildMinigameUI();
            BuildTutorialPanel();
            if (useMentalSpace && useWorldBlur) BuildBlurVolume();
        }

        private void BuildBlurVolume()
        {
            var volGo = new GameObject("MentalSpace_BlurVolume");
            volGo.transform.SetParent(transform, false);
            blurVolume = volGo.AddComponent<Volume>();
            blurVolume.isGlobal = true;
            blurVolume.priority = 9998f; // alto, justo bajo WakeUp (9999)
            blurVolume.weight = 0f;

            blurProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            blurVolume.profile = blurProfile;

            blurDof = blurProfile.Add<DepthOfField>();
            blurDof.focusMode.value = DepthOfFieldMode.Manual;
            blurDof.focusMode.overrideState = true;
            blurDof.nearFocusStart.value = 0f;
            blurDof.nearFocusStart.overrideState = true;
            blurDof.nearFocusEnd.value = 50f;
            blurDof.nearFocusEnd.overrideState = true;

            blurVignetteEffect = blurProfile.Add<Vignette>();
            blurVignetteEffect.intensity.value = worldBlurVignette;
            blurVignetteEffect.intensity.overrideState = true;
            blurVignetteEffect.color.value = new Color(0.02f, 0.02f, 0.03f, 1f);
            blurVignetteEffect.color.overrideState = true;
            blurVignetteEffect.smoothness.value = 0.5f;
            blurVignetteEffect.smoothness.overrideState = true;
        }

        // ---------- Mental Space UI ----------

        private void BuildMentalSpaceUI()
        {
            mentalSpaceRoot = new GameObject("MentalSpace_Root", typeof(RectTransform));
            mentalSpaceRoot.transform.SetParent(canvas.transform, false);
            var rt = mentalSpaceRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // ----- BG: viñeta radial fullscreen (etereo, no caja) -----
            //   alpha=0 en el centro (claridad) → alpha=bgColor.a en los bordes (oscuridad envolvente)
            var bgGo = new GameObject("MentalSpace_VignetteBG", typeof(RectTransform));
            bgGo.transform.SetParent(mentalSpaceRoot.transform, false);
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            mentalSpaceBgImg = bgGo.AddComponent<Image>();
            mentalSpaceBgImg.sprite = CreateVignetteSprite(256);
            mentalSpaceBgImg.color = mentalSpaceBgColor;
            mentalSpaceBgImg.raycastTarget = true; // bloquea clicks de mundo

            // ----- Panel central: invisible (solo layout), sin caja ni borde -----
            var panelGo = new GameObject("MentalSpace_Panel_Invisible", typeof(RectTransform));
            panelGo.transform.SetParent(mentalSpaceRoot.transform, false);
            var panelRT = panelGo.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = mentalSpacePanelSize;
            panelRT.anchoredPosition = Vector2.zero;
            mentalSpacePanelImg = panelGo.AddComponent<Image>();
            mentalSpacePanelImg.color = mentalSpacePanelColor; // alpha 0 por default
            mentalSpacePanelImg.raycastTarget = false;

            // ----- Texto educativo arriba dentro del panel (cremoso, no frio) -----
            educationalLabel = MakeText(panelGo.transform, "EduText", educationalText,
                22, FontStyle.Italic,
                new Vector2(mentalSpacePanelSize.x - 80f, 90f),
                new Vector2(0f, mentalSpacePanelSize.y * 0.5f - 70f));
            educationalLabel.color = new Color(0.96f, 0.89f, 0.78f, 0.9f);

            // ----- Nodos en curva (zigzag/wave) -----
            nodeRTs = new RectTransform[totalNodes];
            nodeImgs = new Image[totalNodes];
            for (int i = 0; i < totalNodes; i++)
            {
                Vector2 pos = ComputeNodePosition(i);
                var nGo = new GameObject($"Node_{i}", typeof(RectTransform));
                nGo.transform.SetParent(panelGo.transform, false);
                var nRT = nGo.GetComponent<RectTransform>();
                nRT.anchorMin = new Vector2(0.5f, 0.5f);
                nRT.anchorMax = new Vector2(0.5f, 0.5f);
                nRT.pivot = new Vector2(0.5f, 0.5f);
                nRT.sizeDelta = new Vector2(nodeSize, nodeSize);
                nRT.anchoredPosition = pos;
                var nImg = nGo.AddComponent<Image>();
                nImg.sprite = CreateCircleSprite(64);
                nImg.color = nodeInactiveColor;
                nImg.raycastTarget = false;
                nodeRTs[i] = nRT;
                nodeImgs[i] = nImg;
            }

            // ----- Orbe (el jugador) -----
            var orbGo = new GameObject("Orb", typeof(RectTransform));
            orbGo.transform.SetParent(panelGo.transform, false);
            orbRT = orbGo.GetComponent<RectTransform>();
            orbRT.anchorMin = new Vector2(0.5f, 0.5f);
            orbRT.anchorMax = new Vector2(0.5f, 0.5f);
            orbRT.pivot = new Vector2(0.5f, 0.5f);
            orbRT.sizeDelta = new Vector2(orbSize, orbSize);
            orbRT.anchoredPosition = ComputeNodePosition(0);
            orbImg = orbGo.AddComponent<Image>();
            orbImg.sprite = CreateCircleSprite(128);
            orbImg.color = orbColor;
            orbImg.raycastTarget = false;

            // ----- Breath progress bar (lateral, dentro del panel) -----
            float panelHalfW = mentalSpacePanelSize.x * 0.5f;
            var breathBgGo = new GameObject("BreathBar_BG", typeof(RectTransform));
            breathBgGo.transform.SetParent(panelGo.transform, false);
            var breathBgRT = breathBgGo.GetComponent<RectTransform>();
            breathBgRT.anchorMin = new Vector2(0.5f, 0.5f);
            breathBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            breathBgRT.pivot = new Vector2(0.5f, 0.5f);
            breathBgRT.sizeDelta = new Vector2(16f, 280f);
            breathBgRT.anchoredPosition = new Vector2(panelHalfW - 50f, 0f);
            var breathBgImg = breathBgGo.AddComponent<Image>();
            breathBgImg.color = new Color(0f, 0f, 0f, 0.55f);
            breathBgImg.raycastTarget = false;

            var breathFillGo = new GameObject("BreathBar_Fill", typeof(RectTransform));
            breathFillGo.transform.SetParent(breathBgGo.transform, false);
            breathBarFillRT = breathFillGo.GetComponent<RectTransform>();
            breathBarFillRT.anchorMin = new Vector2(0.5f, 0f);
            breathBarFillRT.anchorMax = new Vector2(0.5f, 0f);
            breathBarFillRT.pivot = new Vector2(0.5f, 0f);
            breathBarFillRT.sizeDelta = new Vector2(12f, 0f);
            breathBarFillRT.anchoredPosition = new Vector2(0f, 2f);
            var breathFillImg = breathFillGo.AddComponent<Image>();
            breathFillImg.color = breathBarColor;
            breathFillImg.raycastTarget = false;

            micLabel = MakeText(panelGo.transform, "BreathLabel", "INHALA",
                14, FontStyle.Bold,
                new Vector2(120f, 40f),
                new Vector2(panelHalfW - 50f, -160f));
            micLabel.color = amberSoft;
            micLabel.alignment = TextAnchor.UpperCenter;

            // ----- Prompt de avance/cerrar abajo del panel -----
            completePromptLabel = MakeText(panelGo.transform, "AdvancePrompt", completePromptText,
                20, FontStyle.Bold,
                new Vector2(mentalSpacePanelSize.x - 80f, 36f),
                new Vector2(0f, -mentalSpacePanelSize.y * 0.5f + 50f));
            completePromptLabel.color = new Color(0.96f, 0.85f, 0.42f, 0f);

            // ----- Hint del boton al lado del orbe (badge circular con texto X / □) -----
            var hintGo = new GameObject("ButtonHint", typeof(RectTransform));
            hintGo.transform.SetParent(panelGo.transform, false);
            buttonHintRT = hintGo.GetComponent<RectTransform>();
            buttonHintRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonHintRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonHintRT.pivot = new Vector2(0.5f, 0.5f);
            buttonHintRT.sizeDelta = new Vector2(56f, 56f);
            buttonHintRT.anchoredPosition = Vector2.zero;
            buttonHintBgImg = hintGo.AddComponent<Image>();
            buttonHintBgImg.sprite = CreateCircleSprite(64);
            buttonHintBgImg.color = new Color(0.96f, 0.85f, 0.42f, 0f);
            buttonHintBgImg.raycastTarget = false;

            var hintTxtGo = new GameObject("ButtonHintText", typeof(RectTransform));
            hintTxtGo.transform.SetParent(hintGo.transform, false);
            var htRT = hintTxtGo.GetComponent<RectTransform>();
            htRT.anchorMin = Vector2.zero; htRT.anchorMax = Vector2.one;
            htRT.offsetMin = Vector2.zero; htRT.offsetMax = Vector2.zero;
            buttonHintText = hintTxtGo.AddComponent<Text>();
            buttonHintText.font = MainMenuController.SharedBodyFont != null
                ? MainMenuController.SharedBodyFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonHintText.text = "X";
            buttonHintText.fontSize = 28;
            buttonHintText.fontStyle = FontStyle.Bold;
            buttonHintText.alignment = TextAnchor.MiddleCenter;
            buttonHintText.color = new Color(0.05f, 0.04f, 0.02f, 0f);
            buttonHintText.raycastTarget = false;
        }

        // Calcula la posicion del nodo i en un patron curvo variado.
        // Centro del panel = (0,0). Nodo 0 abajo, nodo N-1 arriba.
        // El "shape" se randomiza en cada apertura para que el recorrido se sienta distinto.
        private Vector2 ComputeNodePosition(int i)
        {
            if (totalNodes <= 1) return Vector2.zero;
            float tNorm = (float)i / (totalNodes - 1); // 0 → 1
            float y = Mathf.Lerp(-pathHeightY * 0.5f, pathHeightY * 0.5f, tNorm);
            float x;
            switch (pathRandomShape)
            {
                case 0: // sinusoide simple
                    x = Mathf.Sin(tNorm * Mathf.PI * pathRandomFreq + pathRandomPhase) * pathAmplitudeX * pathRandomDir;
                    break;
                case 1: // zigzag duro (alterna brusco)
                    x = ((i % 2 == 0) ? -1f : 1f) * pathAmplitudeX * pathRandomDir;
                    break;
                case 2: // arco hacia un lado
                    x = Mathf.Sin(tNorm * Mathf.PI) * pathAmplitudeX * pathRandomDir;
                    break;
                case 3: // espiral suave (curva en S doble)
                    x = Mathf.Sin(tNorm * Mathf.PI * 2.5f + pathRandomPhase) * pathAmplitudeX * 0.85f * pathRandomDir;
                    break;
                default:
                    x = Mathf.Sin(tNorm * Mathf.PI * pathRandomFreq) * pathAmplitudeX * pathRandomDir;
                    break;
            }
            return new Vector2(x, y);
        }

        // Re-posiciona los nodos existentes segun el path random actual. Llamar despues de
        // randomizar pathRandomShape/Phase/Dir para que el siguiente recorrido sea distinto.
        private void RepositionNodes()
        {
            if (nodeRTs == null) return;
            for (int i = 0; i < nodeRTs.Length; i++)
            {
                if (nodeRTs[i] != null) nodeRTs[i].anchoredPosition = ComputeNodePosition(i);
            }
        }

        private Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / r;
                float dy = (y - cy) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.Clamp01((d - 0.85f) / 0.15f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // Llamado cada frame mientras el minijuego esta visible. Maneja:
        // - posicion del orbe en el nodo actual
        // - pulsado del orbe con respiracion
        // - barra de respiracion lateral
        // - prompt de "X para avanzar" cuando cooldown termino
        // - input de X / Cuadrado para avanzar al siguiente nodo (o cerrar si esta en el ultimo)
        private void UpdateMentalSpaceUI()
        {
            if (!useMentalSpace || mentalSpaceRoot == null) return;

            // Cooldown del avance
            if (advanceCooldownTimer > 0f) advanceCooldownTimer -= Time.deltaTime;
            bool canAdvance = advanceCooldownTimer <= 0f;
            bool atLastNode = currentNode >= totalNodes - 1;

            // Update color de nodos: pasados ambar, actual cremoso brillante, futuros dim
            for (int i = 0; i < nodeImgs.Length; i++)
            {
                if (i < currentNode) nodeImgs[i].color = nodeCompletedColor;
                else if (i == currentNode) nodeImgs[i].color = new Color(1f, 0.95f, 0.82f, 0.95f);
                else nodeImgs[i].color = nodeInactiveColor;
            }

            // Posicion del orbe en el nodo actual con interpolacion suave
            int idx = Mathf.Clamp(currentNode, 0, totalNodes - 1);
            Vector2 targetPos = nodeRTs[idx].anchoredPosition;
            orbRT.anchoredPosition = Vector2.Lerp(orbRT.anchoredPosition, targetPos, Time.deltaTime * 5f);

            // Pulsacion del orbe con la respiracion
            float breathScale = 1f;
            switch (phase)
            {
                case Phase.Inhale:
                    breathScale = Mathf.Lerp(0.85f, 1.3f, Mathf.Clamp01(phaseElapsed / inhaleSeconds));
                    break;
                case Phase.Exhale:
                    breathScale = Mathf.Lerp(1.3f, 0.85f, Mathf.Clamp01(phaseElapsed / exhaleSeconds));
                    break;
                case Phase.Pause:
                    breathScale = 0.85f;
                    break;
            }
            orbRT.localScale = new Vector3(breathScale, breathScale, 1f);

            // Barra lateral: muestra el progreso de la respiracion actual
            if (breathBarFillRT != null)
            {
                float breathNorm = 0f;
                switch (phase)
                {
                    case Phase.Inhale:
                        breathNorm = Mathf.Clamp01(phaseElapsed / inhaleSeconds);
                        break;
                    case Phase.Exhale:
                        breathNorm = 1f - Mathf.Clamp01(phaseElapsed / exhaleSeconds);
                        break;
                    case Phase.Pause:
                        breathNorm = 0f;
                        break;
                }
                breathBarFillRT.sizeDelta = new Vector2(12f, 276f * breathNorm);

                if (micLabel != null)
                {
                    string phaseLabel = phase == Phase.Inhale ? "INHALA"
                                      : phase == Phase.Exhale ? "EXHALA"
                                      : "...";
                    micLabel.text = phaseLabel;
                }
            }

            // Prompt: que decir y si se ve
            if (completePromptLabel != null)
            {
                string txt;
                float targetAlpha;
                if (!canAdvance)
                {
                    txt = waitPromptText;
                    targetAlpha = 0.4f;
                }
                else if (atLastNode)
                {
                    txt = lastNodePromptText;
                    targetAlpha = 1f;
                }
                else
                {
                    txt = completePromptText;
                    targetAlpha = 1f;
                }
                completePromptLabel.text = txt;
                Color c = completePromptLabel.color;
                c.a = Mathf.Lerp(c.a, targetAlpha, Time.unscaledDeltaTime * 5f);
                completePromptLabel.color = c;
            }

            // Boton hint al lado del orbe - aparece SOLO cuando se puede avanzar, pulsa para llamar la atencion
            if (buttonHintRT != null && buttonHintBgImg != null && buttonHintText != null)
            {
                // Posicionar a la derecha del orbe (con offset)
                Vector2 orbPos = orbRT.anchoredPosition;
                Vector2 hintTargetPos = orbPos + new Vector2(orbSize * 0.55f + 40f, orbSize * 0.4f);
                buttonHintRT.anchoredPosition = Vector2.Lerp(buttonHintRT.anchoredPosition, hintTargetPos, Time.deltaTime * 8f);

                // Alpha: visible cuando canAdvance, invisible cuando cooldown
                float targetA = canAdvance ? 1f : 0f;
                Color bg = buttonHintBgImg.color;
                bg.a = Mathf.Lerp(bg.a, targetA, Time.unscaledDeltaTime * 6f);
                buttonHintBgImg.color = bg;
                Color tx = buttonHintText.color;
                tx.a = Mathf.Lerp(tx.a, targetA, Time.unscaledDeltaTime * 6f);
                buttonHintText.color = tx;

                // Pulsacion sutil cuando visible
                if (canAdvance)
                {
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f;
                    buttonHintRT.localScale = new Vector3(pulse, pulse, 1f);
                }
                else
                {
                    buttonHintRT.localScale = Vector3.one;
                }
            }

            // Detectar tecla/boton para avanzar nodo o cerrar.
            // Aceptamos varias teclas/botones para evitar confusion entre plataformas:
            //   teclado: X, Enter, Space
            //   gamepad PS: Cross (buttonSouth) o Square (buttonWest)
            //   gamepad Xbox: A (buttonSouth) o X (buttonWest)
            if (canAdvance)
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                bool kbPressed = kb != null && (
                    kb.xKey.wasPressedThisFrame ||
                    kb.enterKey.wasPressedThisFrame ||
                    kb.spaceKey.wasPressedThisFrame ||
                    kb.numpadEnterKey.wasPressedThisFrame);
                bool gpPressed = gp != null && (
                    gp.buttonSouth.wasPressedThisFrame ||
                    gp.buttonWest.wasPressedThisFrame);
                bool pressed = kbPressed || gpPressed;
                if (pressed)
                {
                    Debug.Log($"[Breathing] Input detectado para avanzar: kb={kbPressed} gp={gpPressed}");
                    if (atLastNode)
                    {
                        // Recompensa final: completar la meditacion = alivio total.
                        // Reseteamos el stress a 0 para garantizar que el minigame NO se re-dispare
                        // (sino la barra queda sobre el umbral de show y entra en loop).
                        if (StressSystem.Instance != null) StressSystem.Instance.ResetTo(0f);
                        if (completedClip != null && mentalSpaceAudioSrc != null)
                            mentalSpaceAudioSrc.PlayOneShot(completedClip, completedVolume);
                        Debug.Log($"[Breathing] Espacio mental completado. Nodos: {totalNodes}/{totalNodes}. Stress reseteado a 0.");
                        currentNode = 0;
                        advanceCooldownTimer = 0f;
                        // Activar cooldown de reapertura para evitar que se vuelva a disparar de golpe
                        reopenCooldownTimer = reopenCooldownSeconds;
                        forcedVisible = false;
                        TryHide();
                    }
                    else
                    {
                        currentNode++;
                        advanceCooldownTimer = advanceCooldown;
                        TriggerSuccessFlash();
                        currentStreak++;
                        if (StressSystem.Instance != null) StressSystem.Instance.Add(-stressDownOnSuccess);
                        if (nodeAdvanceClip != null && mentalSpaceAudioSrc != null)
                            mentalSpaceAudioSrc.PlayOneShot(nodeAdvanceClip, nodeAdvanceVolume);
                        Debug.Log($"[Breathing] Avance manual: nodo {currentNode}/{totalNodes}. Cooldown {advanceCooldown}s.");
                        OnCycleSuccess?.Invoke();
                    }
                }
            }
        }

        // ---------- Minigame UI ----------

        private void BuildMinigameUI()
        {
            // Pacer bar (BG)
            var pacerBgGo = new GameObject("Pacer_BG", typeof(RectTransform));
            pacerBgGo.transform.SetParent(canvas.transform, false);
            pacerBgRT = pacerBgGo.GetComponent<RectTransform>();
            pacerBgRT.anchorMin = new Vector2(0.5f, 0.5f);
            pacerBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            pacerBgRT.pivot = new Vector2(0.5f, 0.5f);
            pacerBgRT.sizeDelta = new Vector2(pacerMaxWidth, 14f);
            pacerBgRT.anchoredPosition = new Vector2(0f, pacerY);
            var pacerBgImg = pacerBgGo.AddComponent<Image>();
            pacerBgImg.color = new Color(0f, 0f, 0f, 0.5f);
            pacerBgImg.raycastTarget = false;

            // Pacer bar (FILL — crece desde el centro hacia ambos lados)
            var pacerFillGo = new GameObject("Pacer_Fill", typeof(RectTransform));
            pacerFillGo.transform.SetParent(pacerBgGo.transform, false);
            pacerFillRT = pacerFillGo.GetComponent<RectTransform>();
            pacerFillRT.anchorMin = new Vector2(0.5f, 0.5f);
            pacerFillRT.anchorMax = new Vector2(0.5f, 0.5f);
            pacerFillRT.pivot = new Vector2(0.5f, 0.5f);
            pacerFillRT.sizeDelta = new Vector2(0f, 10f);
            pacerFillRT.anchoredPosition = Vector2.zero;
            pacerFillImg = pacerFillGo.AddComponent<Image>();
            pacerFillImg.color = pacerColorInhale;
            pacerFillImg.raycastTarget = false;

            // Response bar (BG)
            var respBgGo = new GameObject("Response_BG", typeof(RectTransform));
            respBgGo.transform.SetParent(canvas.transform, false);
            responseBarBgRT = respBgGo.GetComponent<RectTransform>();
            responseBarBgRT.anchorMin = new Vector2(0.5f, 0.5f);
            responseBarBgRT.anchorMax = new Vector2(0.5f, 0.5f);
            responseBarBgRT.pivot = new Vector2(0.5f, 0.5f);
            responseBarBgRT.sizeDelta = new Vector2(pacerMaxWidth, 7f);
            responseBarBgRT.anchoredPosition = new Vector2(0f, pacerY - 24f);
            var respBgImg = respBgGo.AddComponent<Image>();
            respBgImg.color = new Color(0f, 0f, 0f, 0.4f);
            respBgImg.raycastTarget = false;

            // Response bar (FILL — solo crece a la derecha como bar normal)
            var respFillGo = new GameObject("Response_Fill", typeof(RectTransform));
            respFillGo.transform.SetParent(respBgGo.transform, false);
            responseBarFillRT = respFillGo.GetComponent<RectTransform>();
            responseBarFillRT.anchorMin = new Vector2(0f, 0.5f);
            responseBarFillRT.anchorMax = new Vector2(0f, 0.5f);
            responseBarFillRT.pivot = new Vector2(0f, 0.5f);
            responseBarFillRT.sizeDelta = new Vector2(0f, 5f);
            responseBarFillRT.anchoredPosition = Vector2.zero;
            responseBarFillImg = respFillGo.AddComponent<Image>();
            responseBarFillImg.color = responseBarColor;
            responseBarFillImg.raycastTarget = false;

            // Streak counter (top-right)
            streakText = MakeText(canvas.transform, "StreakText", "",
                22, FontStyle.Bold,
                new Vector2(280f, 32f), new Vector2(-160f, 480f));
            streakText.alignment = TextAnchor.MiddleRight;
            streakText.color = streakTextColor;

            // Success flash (overlay fullscreen, alpha 0 al inicio)
            var flashGo = new GameObject("SuccessFlash", typeof(RectTransform));
            flashGo.transform.SetParent(canvas.transform, false);
            var flashRT = flashGo.GetComponent<RectTransform>();
            flashRT.anchorMin = Vector2.zero;
            flashRT.anchorMax = Vector2.one;
            flashRT.offsetMin = Vector2.zero;
            flashRT.offsetMax = Vector2.zero;
            successFlashImg = flashGo.AddComponent<Image>();
            successFlashImg.sprite = CreateVignetteSprite(128);
            successFlashImg.color = new Color(successFlashColor.r, successFlashColor.g, successFlashColor.b, 0f);
            successFlashImg.raycastTarget = false;
        }

        // Llamar cada frame para actualizar pacer + response + flash
        private void UpdateMinigameUI()
        {
            if (!showMinigameUI || pacerFillRT == null) return;

            // ----- Pacer: crece durante INHALA, decrece durante EXHALA, vacio en PAUSA -----
            float pacerNorm = 0f;
            Color pacerCol = pacerColorPause;
            switch (phase)
            {
                case Phase.Inhale:
                    pacerNorm = Mathf.Clamp01(phaseElapsed / inhaleSeconds);
                    pacerCol = pacerColorInhale;
                    break;
                case Phase.Exhale:
                    pacerNorm = 1f - Mathf.Clamp01(phaseElapsed / exhaleSeconds);
                    pacerCol = pacerColorExhale;
                    break;
                case Phase.Pause:
                    pacerNorm = 0f;
                    pacerCol = pacerColorPause;
                    break;
            }
            pacerFillRT.sizeDelta = new Vector2(pacerMaxWidth * pacerNorm, 10f);
            pacerFillImg.color = pacerCol;

            // ----- Response bar: solo visible en EXHALA, llena con el input real del jugador -----
            float respNorm = 0f;
            if (phase == Phase.Exhale)
            {
                var provider = BreathingInputProvider.Instance;
                if (provider != null)
                {
                    bool isFallback = provider.Mode == BreathingInputProvider.InputMode.KeyboardFallback;
                    if (isFallback)
                    {
                        // teclado: bool 0 o 1
                        respNorm = provider.IsExhalingNow() ? 1f : 0f;
                    }
                    else
                    {
                        // mic: nivel real
                        respNorm = provider.NormalizedExhaleLevel();
                    }
                }
            }
            // smooth lerp para que no se sienta cortante
            float currentRespWidth = responseBarFillRT.sizeDelta.x;
            float targetRespWidth = pacerMaxWidth * respNorm;
            float smoothed = Mathf.Lerp(currentRespWidth, targetRespWidth, Time.deltaTime * 12f);
            responseBarFillRT.sizeDelta = new Vector2(smoothed, 5f);

            // ----- Success flash: fade out -----
            if (successFlashImg != null)
            {
                successFlashElapsed += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(successFlashElapsed / successFlashDuration);
                a *= successFlashColor.a;
                successFlashImg.color = new Color(successFlashColor.r, successFlashColor.g, successFlashColor.b, a);
            }

            // ----- Streak counter -----
            if (streakText != null)
            {
                streakText.text = currentStreak > 0 ? ("Ciclos  " + currentStreak + "  ✓") : "";
            }
        }

        private void TriggerSuccessFlash()
        {
            successFlashElapsed = 0f;
        }

        private void BuildTutorialPanel()
        {
            tutorialPanel = new GameObject("Tutorial_Panel");
            tutorialPanel.transform.SetParent(canvas.transform, false);
            var rt = tutorialPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            tutorialPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

            tutorialText = MakeText(tutorialPanel.transform, "Tutorial_Text", "",
                26, FontStyle.Normal,
                new Vector2(1200f, 600f), Vector2.zero);
            tutorialText.alignment = TextAnchor.MiddleCenter;

            tutorialPanel.SetActive(false);
        }

        private Sprite CreateVignetteSprite(int size)
        {
            // Radial alpha gradient: alpha=0 en el centro (claridad), alpha=1 en los bordes (oscuridad).
            // Curva ease-in cuadratica para que la transicion sea suave y los bordes se sientan envolventes.
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / r;
                float dy = (y - cy) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01((d - 0.25f) / 0.75f);
                t = t * t;
                px[y * size + x] = new Color(1f, 1f, 1f, t);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Text MakeText(Transform parent, string name, string content,
            int size, FontStyle style, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = labelColor;
            return t;
        }
    }
}
