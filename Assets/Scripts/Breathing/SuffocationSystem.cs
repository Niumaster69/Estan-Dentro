using UnityEngine;
using UnityEngine.UI;
using EstanDentro.Stress;
using EstanDentro.Player;

namespace EstanDentro.Breathing
{
    /// <summary>
    /// Sistema fisiologico de SOFOCO. Cuando el jugador tiene estres alto y falla ciclos
    /// de respiracion, sube un nivel 0..1 de "asfixia" que se manifiesta como:
    ///   - Audio de jadeo (volumen + pitch escalan con nivel)
    ///   - Vignette diegetico que cierra la vision desde los bordes
    ///   - Camera shake sutil sobre cameraPivot
    ///   - Si llega a 1.0 -> blackout + colapso (StressSystem.Add(maxStress) -> GameOver)
    ///
    /// Como se sale: completar ciclos exitosos baja el nivel; tambien decae solo cuando
    /// el estres baja del umbral.
    ///
    /// Setup minimo:
    ///   - Pegar este script en un GameObject vacio en la escena (recomendado: el mismo que tiene StressSystem o BreathingMinigame).
    ///   - Asignar AudioClip de jadeo (Assets/Audio/Ambient/breath_man.flac o breath_woman.wav).
    ///   - Listo. El sistema se autoengancha a StressSystem y BreathingMinigame.
    /// </summary>
    [DefaultExecutionOrder(-25)]
    public class SuffocationSystem : MonoBehaviour
    {
        public static SuffocationSystem Instance { get; private set; }

        [Header("Disparadores")]
        [SerializeField, Range(0f, 1f), Tooltip("Estres normalizado a partir del cual EMPIEZAN a contar los fallos para subir sofoco.")]
        private float stressThreshold = 0.6f;
        [SerializeField, Tooltip("Cuantos fallos consecutivos en cualquier nivel de estres tambien empujan sofoco (independiente del estres).")]
        private int failsToTrigger = 2;

        [Header("Niveles")]
        [SerializeField, Range(0f, 1f), Tooltip("Cuanto sube el nivel por cada fallo cuando el disparador esta activo.")]
        private float levelUpPerFail = 0.25f;
        [SerializeField, Range(0f, 1f), Tooltip("Cuanto baja el nivel por cada ciclo exitoso.")]
        private float levelDownPerSuccess = 0.35f;
        [SerializeField, Tooltip("Decay pasivo por segundo cuando el estres esta por debajo del umbral.")]
        private float passiveDecayPerSecond = 0.06f;
        [SerializeField, Tooltip("Subida pasiva por segundo cuando estres alto + sin atender la respiracion. 0 = no subir solo.")]
        private float passiveBuildPerSecond = 0.02f;
        [SerializeField, Tooltip("Si el nivel llega o supera este valor, dispara el colapso (GameOver).")]
        private float collapseThreshold = 1f;
        [SerializeField, Tooltip("Tiempo (s) que el nivel debe permanecer >= collapseThreshold antes de disparar el colapso. Margen para que el jugador reaccione con un exhale ultimo.")]
        private float collapseGraceSeconds = 1.2f;

        [Header("Audio jadeo")]
        [SerializeField, Tooltip("Clip largo de jadeo en loop (ej. breath_man.flac).")]
        private AudioClip jadeoClip;
        [SerializeField, Range(0f, 1f)] private float jadeoMaxVolume = 0.85f;
        [SerializeField, Range(0.5f, 1.5f)] private float jadeoMinPitch = 0.95f;
        [SerializeField, Range(0.5f, 2f)] private float jadeoMaxPitch = 1.35f;

        [Header("Vignette")]
        [SerializeField, Range(0f, 1f), Tooltip("Alpha del vignette al nivel maximo (1 = vision casi cerrada).")]
        private float vignetteMaxAlpha = 0.92f;
        [SerializeField] private Color vignetteColor = new Color(0.05f, 0.02f, 0.02f, 1f);
        [SerializeField, Tooltip("Suavizado del fade del vignette para que no se sienta mecanico.")]
        private float vignetteSmoothing = 4f;

        [Header("Camera shake")]
        [SerializeField, Tooltip("Camara que se sacude. Si es null, busca PlayerController.CameraPivot.")]
        private Transform shakeTarget;
        [SerializeField] private float shakeMaxAngle = 1.2f;
        [SerializeField] private float shakeFrequency = 8f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;
        [SerializeField, Tooltip("Si true, fuerza nivel = forcedDebugLevel (para tunear visuales).")]
        private bool useForcedLevel = false;
        [SerializeField, Range(0f, 1f)] private float forcedDebugLevel = 0.5f;

        // Runtime
        private float level;
        private int recentFails;
        private float collapseTimer;
        private bool collapseTriggered;

        // Audio
        private AudioSource jadeoSrc;

        // UI
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Image vignetteImg;
        private float displayedAlpha;

        // Shake
        private Vector3 shakeBaseEuler;
        private float shakeSeed;
        private bool shakeBaseCaptured;

        public float CurrentLevel => level;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildAudio();
            BuildUI();
            shakeSeed = Random.Range(0f, 100f);
        }

        private void OnEnable()
        {
            if (StressSystem.Instance != null) StressSystem.Instance.OnCollapse += HandleStressCollapse;
            if (BreathingMinigame.Instance != null)
            {
                BreathingMinigame.Instance.OnCycleFail += HandleCycleFail;
                BreathingMinigame.Instance.OnCycleSuccess += HandleCycleSuccess;
            }
        }

        private void OnDisable()
        {
            if (StressSystem.Instance != null) StressSystem.Instance.OnCollapse -= HandleStressCollapse;
            if (BreathingMinigame.Instance != null)
            {
                BreathingMinigame.Instance.OnCycleFail -= HandleCycleFail;
                BreathingMinigame.Instance.OnCycleSuccess -= HandleCycleSuccess;
            }
        }

        private void Start()
        {
            // Reintento de subscripcion: BreathingMinigame puede inicializar despues de este Awake.
            if (BreathingMinigame.Instance != null)
            {
                BreathingMinigame.Instance.OnCycleFail -= HandleCycleFail;
                BreathingMinigame.Instance.OnCycleSuccess -= HandleCycleSuccess;
                BreathingMinigame.Instance.OnCycleFail += HandleCycleFail;
                BreathingMinigame.Instance.OnCycleSuccess += HandleCycleSuccess;
            }
            CaptureShakeBase();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- eventos ----------

        private void HandleCycleFail()
        {
            recentFails++;
            bool stressOk = StressSystem.Instance != null && StressSystem.Instance.Normalized >= stressThreshold;
            bool failTrigger = recentFails >= failsToTrigger;
            if (stressOk || failTrigger)
            {
                AddLevel(levelUpPerFail);
                if (debugLog) Debug.Log($"[Suffocation] FAIL +{levelUpPerFail:F2} (stressOk={stressOk}, failTrigger={failTrigger}). lvl={level:F2}");
            }
            else if (debugLog)
            {
                Debug.Log($"[Suffocation] FAIL ignorado (estres bajo y solo {recentFails} fallos).");
            }
        }

        private void HandleCycleSuccess()
        {
            recentFails = 0;
            AddLevel(-levelDownPerSuccess);
            if (debugLog) Debug.Log($"[Suffocation] OK -{levelDownPerSuccess:F2}. lvl={level:F2}");
        }

        private void HandleStressCollapse()
        {
            // El estres ya colapso por su cuenta — apagar el sistema para evitar doble disparo.
            level = 0f;
            collapseTimer = 0f;
            collapseTriggered = true;
        }

        // ---------- update ----------

        private void Update()
        {
            if (useForcedLevel) level = forcedDebugLevel;

            // Decay/build pasivo
            if (!collapseTriggered && !useForcedLevel)
            {
                bool stressOk = StressSystem.Instance != null && StressSystem.Instance.Normalized >= stressThreshold;
                if (stressOk && passiveBuildPerSecond > 0f)
                    AddLevel(passiveBuildPerSecond * Time.deltaTime);
                else if (!stressOk && passiveDecayPerSecond > 0f)
                    AddLevel(-passiveDecayPerSecond * Time.deltaTime);
            }

            ApplyAudio();
            ApplyVignette();
            CheckCollapse();
        }

        private void LateUpdate()
        {
            // En LateUpdate para que el shake quede ENCIMA del HandleLook del PlayerController.
            ApplyShake();
        }

        private void AddLevel(float delta)
        {
            level = Mathf.Clamp01(level + delta);
        }

        private void CheckCollapse()
        {
            if (collapseTriggered) return;
            if (level >= collapseThreshold)
            {
                collapseTimer += Time.deltaTime;
                if (collapseTimer >= collapseGraceSeconds)
                {
                    collapseTriggered = true;
                    if (StressSystem.Instance != null)
                    {
                        StressSystem.Instance.Add(StressSystem.Instance.MaxStress);
                        Debug.Log("[Suffocation] Colapso por sofoco: estres a maximo.");
                    }
                }
            }
            else
            {
                collapseTimer = 0f;
            }
        }

        // ---------- audio ----------

        private void BuildAudio()
        {
            jadeoSrc = gameObject.AddComponent<AudioSource>();
            jadeoSrc.clip = jadeoClip;
            jadeoSrc.loop = true;
            jadeoSrc.playOnAwake = false;
            jadeoSrc.spatialBlend = 0f;
            jadeoSrc.volume = 0f;
            jadeoSrc.pitch = jadeoMinPitch;
            if (jadeoClip != null) jadeoSrc.Play();
        }

        private void ApplyAudio()
        {
            if (jadeoSrc == null || jadeoClip == null) return;
            float target = level * jadeoMaxVolume;
            jadeoSrc.volume = Mathf.MoveTowards(jadeoSrc.volume, target, Time.deltaTime * 1.5f);
            jadeoSrc.pitch = Mathf.Lerp(jadeoMinPitch, jadeoMaxPitch, level);
            if (!jadeoSrc.isPlaying && level > 0.05f) jadeoSrc.Play();
        }

        // ---------- vignette ----------

        private void BuildUI()
        {
            var go = new GameObject("Suffocation_Canvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160; // entre breathing (150) y prompts (165)

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var vignetteGo = new GameObject("Suffocation_Vignette", typeof(RectTransform));
            var rt = vignetteGo.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            vignetteImg = vignetteGo.AddComponent<Image>();
            vignetteImg.sprite = CreateVignetteSprite(256);
            vignetteImg.color = vignetteColor;
            vignetteImg.raycastTarget = false;
        }

        private void ApplyVignette()
        {
            if (vignetteImg == null) return;
            float target = level * vignetteMaxAlpha;
            displayedAlpha = Mathf.Lerp(displayedAlpha, target, Time.deltaTime * vignetteSmoothing);
            var c = vignetteColor;
            c.a = displayedAlpha;
            vignetteImg.color = c;
            if (canvasGroup != null) canvasGroup.alpha = displayedAlpha > 0.01f ? 1f : 0f;
        }

        private Sprite CreateVignetteSprite(int size)
        {
            // Radial alpha gradient: claro en centro, oscuro en bordes (mas pronunciado que el del minijuego).
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
                float t = Mathf.Clamp01((d - 0.10f) / 0.90f);
                t = Mathf.Pow(t, 1.6f); // curva mas agresiva
                px[y * size + x] = new Color(1f, 1f, 1f, t);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // ---------- camera shake ----------

        private void CaptureShakeBase()
        {
            if (shakeTarget == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) shakeTarget = pc.CameraPivot;
            }
            if (shakeTarget != null)
            {
                shakeBaseEuler = shakeTarget.localEulerAngles;
                shakeBaseCaptured = true;
            }
        }

        private void ApplyShake()
        {
            if (shakeTarget == null || !shakeBaseCaptured || level <= 0.05f) return;
            float t = Time.time * shakeFrequency;
            float intensity = level * shakeMaxAngle;
            float dx = (Mathf.PerlinNoise(shakeSeed, t) - 0.5f) * 2f * intensity;
            float dy = (Mathf.PerlinNoise(shakeSeed + 50f, t) - 0.5f) * 2f * intensity;
            float dz = (Mathf.PerlinNoise(shakeSeed + 100f, t) - 0.5f) * 2f * intensity * 0.5f;
            // Aditivo encima de lo que el PlayerController ya escribio este frame (HandleLook corre con executionOrder default 0; este sistema -25 corre antes).
            // Por eso aplicamos en LateUpdate via offset acumulativo:
            // Implementacion simplificada: agregamos una transform child intermedia en runtime no — mantenemos sumando localEulerAngles.
            var e = shakeTarget.localEulerAngles;
            shakeTarget.localEulerAngles = new Vector3(e.x + dx, e.y + dy, dz);
        }

        // API publica para forzar nivel (para cinematicas o scripted moments).
        public void SetLevel(float newLevel) { level = Mathf.Clamp01(newLevel); }
        public void Reset() { level = 0f; recentFails = 0; collapseTimer = 0f; collapseTriggered = false; }
    }
}
