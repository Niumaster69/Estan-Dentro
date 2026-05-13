using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using EstanDentro.Breathing;
using EstanDentro.Network;
using EstanDentro.Audio;

namespace EstanDentro.UI
{
    public class MainMenuController : MonoBehaviour
    {
        // Fonts compartidas con otras pantallas (LoadingScreenController, etc).
        // Se setean en Awake() y persisten via static aunque MainMenu se descargue.
        public static Font SharedTitleFont { get; private set; }
        public static Font SharedBodyFont { get; private set; }

        [Header("Audio UI (se tocan via AudioManager.PlayUI)")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip uiHoverClip;
        [SerializeField] private AudioClip uiBackClip;
        [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float uiHoverVolume = 0.55f;

        [Header("Texto")]
        [SerializeField] private string gameTitle = "ESTAN DENTRO";
        [SerializeField] private string gameSubtitle = "Lo que no puedo ver";
        [SerializeField] private string versionLabel = "Capitulo 1 - prototipo";

        [Header("Carga")]
        [SerializeField] private string playSceneName = "Cinematic_Intro";

        [Header("Tipografia (opcional - asignar .ttf desde Inspector)")]
        [SerializeField, Tooltip("Fuente del titulo grande. Recomendado: Creepster de Google Fonts. Vacio = LegacyRuntime.")]
        private Font titleFont;
        [SerializeField, Tooltip("Fuente del subtitulo y cuerpo. Recomendado: Special Elite o IM Fell English. Vacio = LegacyRuntime.")]
        private Font bodyFont;

        [Header("Imagen de fondo (Outlast / Visage / Madison style)")]
        [SerializeField, Tooltip("Sprite de la imagen de fondo. Idealmente screenshot del aula con iluminacion dramatica. Vacio = gradiente solo.")]
        private Sprite backgroundImage;
        [SerializeField, Tooltip("Tinte sobre la imagen. Color * imagen. Blanco (1,1,1,1) = sin tinte, imagen tal cual.")]
        private Color backgroundImageTint = new Color(1f, 1f, 1f, 1f);
        [SerializeField, Tooltip("Activa paneo lento Ken Burns sobre la imagen.")]
        private bool kenBurnsPan = true;
        [SerializeField, Tooltip("Velocidad del paneo Ken Burns (mas lento = mas atmosferico).")]
        private float kenBurnsSpeed = 0.04f;
        [SerializeField, Tooltip("Magnitud del zoom Ken Burns (0.05 = 5% extra de zoom).")]
        private float kenBurnsZoom = 0.05f;
        [SerializeField, Tooltip("Capa de color encima de la imagen. Alpha bajo = imagen mas brillante.")]
        private Color backgroundOverlayColor = new Color(0f, 0f, 0f, 0.10f);
        [SerializeField, Range(0f, 1f), Tooltip("Slider directo del alpha del overlay. 0 = sin oscurecer, 1 = totalmente negro.")]
        private float backgroundOverlayAlpha = 0.10f;
        [SerializeField, Range(0f, 1f), Tooltip("Slider directo del alpha de la vignette. 0 = sin vignette, 1 = bordes negros totales.")]
        private float vignetteAlpha = 0.6f;

        [Header("Efectos sutiles")]
        [SerializeField, Tooltip("Activa el grano de pelicula sobre todo. POR DEFECTO APAGADO.")]
        private bool noiseEnabled = false;
        [SerializeField, Range(0f, 0.3f), Tooltip("Intensidad del grano. Solo aplica si noiseEnabled=true.")]
        private float noiseAlpha = 0.02f;

        [Header("Glitch en bandas (estilo señal corrupta)")]
        [SerializeField, Tooltip("Activa el sistema de bandas horizontales que aparecen brevemente.")]
        private bool intenseGlitchEnabled = true;
        [SerializeField] private float intenseGlitchMinInterval = 2.5f;
        [SerializeField] private float intenseGlitchMaxInterval = 6f;
        [SerializeField, Tooltip("Duracion de cada burst de bandas.")]
        private float intenseGlitchDuration = 0.08f;
        [SerializeField, Tooltip("Cantidad MAXIMA de bandas pre-creadas (entre 2 y este numero aparecen aleatorias en cada glitch).")]
        private int glitchBandCount = 5;
        [SerializeField, Tooltip("Altura minima de una banda en pixeles.")]
        private float glitchBandMinHeight = 12f;
        [SerializeField, Tooltip("Altura maxima de una banda en pixeles.")]
        private float glitchBandMaxHeight = 70f;
        [SerializeField, Tooltip("Colores random que toman las bandas. Solo negro para look sobrio.")]
        private Color[] intenseGlitchColors = new Color[] {
            new Color(0.0f, 0.0f, 0.0f, 0.92f) // negro / corte de señal
        };
        [SerializeField, Range(0f, 30f), Tooltip("Pixeles de desplazamiento sutil del fondo cuando ocurre un glitch (como el del titulo).")]
        private float backgroundGlitchShift = 6f;

        [Header("Paleta")]
        [SerializeField] private Color backgroundTopColor = new Color(0.025f, 0.02f, 0.025f, 1f);
        [SerializeField] private Color backgroundBottomColor = new Color(0.06f, 0.025f, 0.03f, 1f);
        [SerializeField] private Color titleColor = new Color(0.92f, 0.89f, 0.83f, 1f);
        [SerializeField] private Color subtitleColor = new Color(0.78f, 0.62f, 0.22f, 0.85f);
        [SerializeField] private Color bodyColor = new Color(0.92f, 0.89f, 0.83f, 0.95f);
        [SerializeField] private Color buttonNormalColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Color buttonHoverColor = new Color(0.85f, 0.7f, 0.28f, 0.78f);
        [SerializeField] private Color buttonPressedColor = new Color(0.62f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color buttonTextColor = new Color(0.95f, 0.93f, 0.85f, 1f);
        [SerializeField] private Color vignetteColor = new Color(0.08f, 0.0f, 0.0f, 0.6f);
        [SerializeField] private Color glitchColor = new Color(0.95f, 0.25f, 0.25f, 1f);

        [Header("Layout")]
        [SerializeField] private float buttonWidth = 380f;
        [SerializeField] private float buttonHeight = 64f;
        [SerializeField] private float buttonSpacing = 18f;

        [Header("Animacion")]
        [SerializeField, Tooltip("Segundos del fade-in inicial del menu.")] private float fadeInSeconds = 1.8f;
        [SerializeField, Tooltip("Escala del boton seleccionado. 1.0 = sin pulso.")] private float selectedButtonScale = 1.06f;
        [SerializeField, Tooltip("Velocidad del lerp de la animacion del boton seleccionado.")] private float selectedLerpSpeed = 12f;
        [SerializeField, Tooltip("Velocidad del pulso lento del titulo.")] private float titlePulseSpeed = 1.2f;
        [SerializeField, Tooltip("Amplitud del pulso del titulo (alpha).")] private float titlePulseAmplitude = 0.06f;
        [SerializeField, Tooltip("Min segundos entre glitches del titulo.")] private float glitchMinInterval = 1.5f;
        [SerializeField, Tooltip("Max segundos entre glitches del titulo.")] private float glitchMaxInterval = 4f;
        [SerializeField, Tooltip("Duracion de un glitch.")] private float glitchDuration = 0.08f;
        [SerializeField, Tooltip("Maximo desplazamiento en pixeles del glitch.")] private float glitchMaxShift = 8f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Button playButton;
        private Button continueButton;
        private Text continueHintText;
        private Button settingsButton;
        private Button quitButton;
        private RectTransform[] buttonRTs;
        private Text titleText;
        private RectTransform titleRT;
        private Vector2 titleAnchoredOriginalPos;
        private float nextGlitchTime;
        private float glitchEndsAt;
        private RectTransform backgroundImageRT;
        private float nextIntenseGlitchTime;
        private float intenseGlitchEndsAt;
        private GameObject[] glitchBandGOs;
        private Image[] glitchBandImgs;
        private RectTransform[] glitchBandRTs;
        private Vector2 backgroundGlitchOffset;
        private CanvasGroup menuPanelGroup;
        private Transform menuPanelTransform;

        private void Awake()
        {
            // Registrar fuentes en el static para que el LoadingScreenController las use
            // (se mantiene aunque MainMenu se descargue — Font es un asset, sobrevive).
            SharedTitleFont = titleFont;
            SharedBodyFont = bodyFont;

            EnsureCamera();
            EnsureEventSystem();
            BuildUI();
            Settings.ApplyAll();
        }

        private void Start()
        {
            // Selecciona el primer boton para navegacion con teclado/gamepad
            EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
            // Asegura tiempo normal y mouse visible en el menu
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // El boton "Continuar" empieza oculto y solo se muestra si el jugador tiene partidas guardadas.
            RefreshContinueButton();

            // Fade-in
            StartCoroutine(FadeInRoutine());
        }

        // Habilita/deshabilita el boton "Continuar" segun si el jugador tiene partidas en BD.
        // Si no hay (offline o lista vacia) el boton queda visible pero gris + texto "(sin partidas guardadas)".
        private void RefreshContinueButton()
        {
            if (continueButton == null) return;
            // Estado por default: deshabilitado con mensaje (mientras llega el GET)
            SetContinueState(enabled: false, hint: "(verificando...)");

            if (!GameSession.IsOnline)
            {
                SetContinueState(enabled: false, hint: "(sin conexion)");
                return;
            }

            ApiClient.Instance.GetAllPartidas(
                partidas =>
                {
                    int jugadorId = GameSession.CurrentJugadorId;
                    int countMias = 0;
                    foreach (var p in partidas)
                    {
                        if (p.idJugador == jugadorId) countMias++;
                    }
                    Debug.Log($"[MainMenu] GET partidas OK. Total={partidas.Length}, mias (idJugador={jugadorId})={countMias}.");
                    if (countMias > 0) SetContinueState(enabled: true, hint: "");
                    else SetContinueState(enabled: false, hint: "(sin partidas guardadas)");
                },
                err =>
                {
                    Debug.LogWarning($"[MainMenu] No se pudo cargar partidas para decidir 'Continuar': {err}");
                    SetContinueState(enabled: false, hint: "(sin conexion)");
                }
            );
        }

        private void SetContinueState(bool enabled, string hint)
        {
            if (continueButton != null) continueButton.interactable = enabled;
            if (continueHintText != null)
            {
                continueHintText.text = hint;
                continueHintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            }
        }

        // Recalcula las posiciones Y de los botones del menu, saltando los inactivos
        // para que no quede un hueco visible cuando "Continuar" esta oculto.
        private void RelayoutButtons()
        {
            const float startY = -40f;
            Button[] order = { playButton, continueButton, settingsButton, quitButton };
            int visibleIdx = 0;
            string debugStr = "[MainMenu] RelayoutButtons: ";
            foreach (var btn in order)
            {
                if (btn == null) { debugStr += $"{(btn == playButton ? "play" : btn == continueButton ? "continue" : btn == settingsButton ? "settings" : "quit")}=NULL, "; continue; }
                if (!btn.gameObject.activeSelf) { debugStr += $"{btn.name}=hidden, "; continue; }
                var rt = btn.GetComponent<RectTransform>();
                float y = startY - visibleIdx * (buttonHeight + buttonSpacing);
                rt.anchoredPosition = new Vector2(0f, y);
                debugStr += $"{btn.name}=slot{visibleIdx}(y={y:F0}), ";
                visibleIdx++;
            }
            Debug.Log(debugStr);
        }

        private IEnumerator FadeInRoutine()
        {
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeInSeconds)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(t / fadeInSeconds);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            UpdateSelectedButtonAnimation();
            UpdateTitlePulse();
            UpdateTitleGlitch();
            UpdateIntenseGlitch();
            UpdateKenBurns();
        }

        private void UpdateIntenseGlitch()
        {
            if (!intenseGlitchEnabled || glitchBandGOs == null || glitchBandGOs.Length == 0) return;

            if (nextIntenseGlitchTime <= 0f)
                nextIntenseGlitchTime = Time.unscaledTime + Random.Range(intenseGlitchMinInterval, intenseGlitchMaxInterval);

            if (Time.unscaledTime > nextIntenseGlitchTime && Time.unscaledTime > intenseGlitchEndsAt)
            {
                intenseGlitchEndsAt = Time.unscaledTime + intenseGlitchDuration;
                nextIntenseGlitchTime = Time.unscaledTime + Random.Range(intenseGlitchMinInterval, intenseGlitchMaxInterval);
                ActivateGlitchBands();
            }
            else if (Time.unscaledTime > intenseGlitchEndsAt && glitchBandGOs[0].activeSelf)
            {
                DeactivateGlitchBands();
            }
        }

        private void ActivateGlitchBands()
        {
            if (intenseGlitchColors == null || intenseGlitchColors.Length == 0) return;
            int active = Random.Range(2, glitchBandGOs.Length + 1);
            for (int i = 0; i < glitchBandGOs.Length; i++)
            {
                if (i < active)
                {
                    var rt = glitchBandRTs[i];
                    rt.sizeDelta = new Vector2(0f, Random.Range(glitchBandMinHeight, glitchBandMaxHeight));
                    rt.anchoredPosition = new Vector2(0f, Random.Range(-440f, 440f));
                    glitchBandImgs[i].color = intenseGlitchColors[Random.Range(0, intenseGlitchColors.Length)];
                    glitchBandGOs[i].SetActive(true);
                }
                else
                {
                    glitchBandGOs[i].SetActive(false);
                }
            }
            // Shift sutil de la imagen de fondo (similar al titulo)
            backgroundGlitchOffset = new Vector2(
                Random.Range(-backgroundGlitchShift, backgroundGlitchShift),
                Random.Range(-backgroundGlitchShift * 0.4f, backgroundGlitchShift * 0.4f));
        }

        private void DeactivateGlitchBands()
        {
            for (int i = 0; i < glitchBandGOs.Length; i++)
                glitchBandGOs[i].SetActive(false);
            backgroundGlitchOffset = Vector2.zero;
        }

        private void UpdateKenBurns()
        {
            if (backgroundImageRT == null) return;
            float t = Time.unscaledTime * kenBurnsSpeed;
            float scale = 1f;
            float panX = 0f, panY = 0f;
            if (kenBurnsPan)
            {
                float zoomCycle = (Mathf.Sin(t) + 1f) * 0.5f;
                scale = 1f + kenBurnsZoom * zoomCycle;
                panX = Mathf.Sin(t * 0.7f) * 18f;
                panY = Mathf.Cos(t * 0.5f) * 12f;
            }
            backgroundImageRT.localScale = new Vector3(scale, scale, 1f);
            // Suma el shift sutil del Intense Glitch al pan de Ken Burns
            backgroundImageRT.anchoredPosition = new Vector2(panX, panY) + backgroundGlitchOffset;
        }

        private void UpdateTitlePulse()
        {
            if (titleText == null) return;
            float pulse = Mathf.Sin(Time.unscaledTime * titlePulseSpeed) * titlePulseAmplitude;
            float a = Mathf.Clamp01(titleColor.a - titlePulseAmplitude * 0.5f + pulse + titlePulseAmplitude * 0.5f);
            // Solo aplicar pulso si NO hay glitch activo (el glitch maneja su propio color)
            if (Time.unscaledTime > glitchEndsAt)
                titleText.color = new Color(titleColor.r, titleColor.g, titleColor.b, a);
        }

        private void UpdateTitleGlitch()
        {
            if (titleRT == null) return;
            if (nextGlitchTime <= 0f)
                nextGlitchTime = Time.unscaledTime + Random.Range(glitchMinInterval, glitchMaxInterval);

            if (Time.unscaledTime > nextGlitchTime && Time.unscaledTime > glitchEndsAt)
            {
                glitchEndsAt = Time.unscaledTime + glitchDuration;
                nextGlitchTime = Time.unscaledTime + Random.Range(glitchMinInterval, glitchMaxInterval);
                titleRT.anchoredPosition = titleAnchoredOriginalPos +
                    new Vector2(Random.Range(-glitchMaxShift, glitchMaxShift),
                                Random.Range(-glitchMaxShift * 0.4f, glitchMaxShift * 0.4f));
                titleText.color = glitchColor;
            }
            else if (Time.unscaledTime > glitchEndsAt && titleRT.anchoredPosition != titleAnchoredOriginalPos)
            {
                titleRT.anchoredPosition = titleAnchoredOriginalPos;
                // El color lo restaura UpdateTitlePulse en el siguiente frame
            }
        }

        private void UpdateSelectedButtonAnimation()
        {
            if (buttonRTs == null) return;
            var selected = EventSystem.current?.currentSelectedGameObject;
            for (int i = 0; i < buttonRTs.Length; i++)
            {
                if (buttonRTs[i] == null) continue;
                bool isSel = selected != null && buttonRTs[i].gameObject == selected;
                Vector3 target = isSel ? Vector3.one * selectedButtonScale : Vector3.one;
                buttonRTs[i].localScale = Vector3.Lerp(buttonRTs[i].localScale, target, Time.unscaledDeltaTime * selectedLerpSpeed);
            }
        }

        private void EnsureCamera()
        {
            var existing = FindFirstObjectByType<Camera>();
            if (existing != null) return;
            var go = new GameObject("MainMenu_Camera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundTopColor;
            cam.cullingMask = 0;
            cam.orthographic = true;
            go.tag = "MainCamera";
            if (FindFirstObjectByType<AudioListener>() == null)
                go.AddComponent<AudioListener>();
        }

        // ---------- handlers ----------

        public void OnPlayClicked()
        {
            // Flow: JUGAR -> CALIBRACION (en este mismo MainMenu) -> CINEMATICA -> LOADING -> juego.
            // La calibracion no debe invadir el gameplay: corre aqui antes de cargar la cinematica.
            HideMenuPanel();
            StartCoroutine(RunCalibrationThenLoad());
        }

        public void OnContinueClicked()
        {
            PartidasOverlay.Open(
                onResume: (idPartida, fechaInicio, capituloAlcanzado) =>
                {
                    HideMenuPanel();
                    StartCoroutine(RunCalibrationThenResume(idPartida, fechaInicio, capituloAlcanzado));
                },
                onClose: () => { /* el overlay se cerro, no hay que hacer nada extra */ }
            );
        }

        // Igual que RunCalibrationThenLoad pero NO crea partida nueva — reutiliza el idPartida
        // que vino de la lista (partida vieja, posiblemente Completada o Abandonada).
        private IEnumerator RunCalibrationThenResume(int idPartida, System.DateTime fechaInicioOriginal, int capituloAlcanzado)
        {
            // 1. BreathingInputProvider
            if (BreathingInputProvider.Instance == null)
            {
                var providerGo = new GameObject("__BreathingInputProvider");
                providerGo.AddComponent<BreathingInputProvider>();
            }

            // 2. MicCalibration (skip si ya esta calibrado)
            var calibGo = new GameObject("__MicCalibration");
            var calib = calibGo.AddComponent<MicCalibration>();
            bool done = false;
            calib.OnCalibrationDone += _ => done = true;
            while (!done) yield return null;

            // 3. Reusar idPartida viejo (no POST nueva)
            GameSession.ResumePartida(idPartida, fechaInicioOriginal);
            Debug.Log($"[MainMenu] Retomando idPartida={idPartida} (capituloAlcanzado={capituloAlcanzado}).");

            // 4. Cargar la escena del capitulo. Por ahora solo Cap 1 esta implementado;
            //    cuando haya Cap 2/3 mapear capituloAlcanzado a la escena correspondiente.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(playSceneName);
        }

        private IEnumerator RunCalibrationThenLoad()
        {
            // 1. Asegurar que existe BreathingInputProvider (singleton). Si no esta, lo creamos.
            if (BreathingInputProvider.Instance == null)
            {
                var providerGo = new GameObject("__BreathingInputProvider");
                providerGo.AddComponent<BreathingInputProvider>();
            }

            // 2. Spawnear MicCalibration. Su Start() decide:
            //    - Si ya hay calibracion guardada -> skip inmediato (canvas oculto, dispara OnCalibrationDone).
            //    - Si no -> muestra UI y arranca el flujo.
            var calibGo = new GameObject("__MicCalibration");
            var calib = calibGo.AddComponent<MicCalibration>();

            bool done = false;
            calib.OnCalibrationDone += _ => done = true;

            // Esperar a que la calibracion termine (auto-skip o usuario completo el flujo).
            while (!done) yield return null;

            // 3. Crear la partida en la API (best-effort).
            //    Si no hay jugador (modo offline), saltar. Si la API no responde, continuar igual.
            yield return CreatePartidaBestEffort();

            // 4. Continuar al cinematica DIRECTAMENTE (sin loading screen).
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f; // por si vinimos de un Time.timeScale=0 de la calibracion
            SceneManager.LoadScene(playSceneName);
        }

        private IEnumerator CreatePartidaBestEffort()
        {
            // Reset contadores antes de cualquier cosa (la partida arranca limpia)
            GameSession.ResetForNewPartida();

            // Sin jugador (la API fallo al crear el auto-perfil) => skip POST silenciosamente.
            if (!GameSession.IsOnline)
            {
                Debug.LogWarning("[MainMenu] Sin IdJugador. Partida se juega offline (no se persiste).");
                yield break;
            }

            System.DateTime now = System.DateTime.UtcNow;
            var data = new PartidaCreateDto
            {
                idJugador = GameSession.CurrentJugadorId,
                nombrePartida = "Partida " + now.ToString("yyyy-MM-dd HH:mm"),
                fechaInicio = now.ToString("o"), // ISO 8601 UTC
                estado = 0,                       // EnCurso
                capituloAlcanzado = 1,
                tiempoSegundos = 0
            };

            bool finished = false;
            ApiClient.Instance.CreatePartida(
                data,
                response =>
                {
                    GameSession.CurrentPartidaId = response.idPartida;
                    GameSession.PartidaStartTime = now;
                    Debug.Log($"[MainMenu] Partida creada idPartida={response.idPartida}");
                    finished = true;
                },
                error =>
                {
                    Debug.LogWarning($"[MainMenu] No se pudo crear partida en API. Modo offline para esta sesion. {error}");
                    finished = true;
                }
            );

            // Esperar respuesta (max ~6s — el ApiClient ya tiene timeout interno de 5s)
            float waited = 0f;
            while (!finished && waited < 6f) { waited += Time.unscaledDeltaTime; yield return null; }
        }

        /// <summary>Util publica para tocar SFX UI desde botones de back/cancel.</summary>
        public void PlayBackSound() => PlayUiClip(uiBackClip, uiClickVolume);

        private void PlayUiClip(AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(clip, volume);
        }

        public void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] OnSettingsClicked - hide menu, open settings");
            HideMenuPanel();
            // transparentBg: false → fondo 100% negro, no se ve la imagen del menu detras.
            SettingsOverlay.Open(onClose: ShowMenuPanel, transparentBg: false);
        }

        private void HideMenuPanel()
        {
            Debug.Log($"[MainMenu] HideMenuPanel. group={(menuPanelGroup != null ? "ok" : "NULL")}");
            if (menuPanelGroup == null) return;
            menuPanelGroup.alpha = 0f;
            menuPanelGroup.interactable = false;
            menuPanelGroup.blocksRaycasts = false;
        }

        private void ShowMenuPanel()
        {
            Debug.Log($"[MainMenu] ShowMenuPanel. group={(menuPanelGroup != null ? "ok" : "NULL")} canvas.enabled={(canvas != null ? canvas.enabled.ToString() : "NULL")} canvasGroup.alpha={(canvasGroup != null ? canvasGroup.alpha.ToString() : "NULL")}");
            if (menuPanelGroup == null) return;
            menuPanelGroup.alpha = 1f;
            menuPanelGroup.interactable = true;
            menuPanelGroup.blocksRaycasts = true;
            EventSystem.current?.SetSelectedGameObject(settingsButton.gameObject);
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- build ----------

        private void EnsureEventSystem()
        {
            var existing = FindFirstObjectByType<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("MainMenu_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Fondo base: gradiente vertical SIEMPRE (por si la imagen no cubre o tiene transparencia)
            var bgGo = new GameObject("BG_Gradient");
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = MakeVerticalGradientSprite(256, backgroundTopColor, backgroundBottomColor);
            bgImg.raycastTarget = false;

            // Imagen de fondo (si esta asignada): se renderiza encima del gradiente con paneo Ken Burns
            if (backgroundImage != null)
            {
                // Crear con RectTransform desde el principio para evitar el bug de
                // Transform reemplazado que rompe la jerarquia.
                var bgImgGo = new GameObject("BG_Image", typeof(RectTransform));
                backgroundImageRT = bgImgGo.GetComponent<RectTransform>();
                backgroundImageRT.SetParent(canvas.transform, false);
                backgroundImageRT.anchorMin = Vector2.zero;
                backgroundImageRT.anchorMax = Vector2.one;
                backgroundImageRT.offsetMin = Vector2.zero;
                backgroundImageRT.offsetMax = Vector2.zero;
                var bImg = bgImgGo.AddComponent<Image>();
                bImg.sprite = backgroundImage;
                bImg.color = backgroundImageTint;
                bImg.preserveAspect = false;
                bImg.raycastTarget = false;
                backgroundImageRT.SetSiblingIndex(1);
            }

            // Capa de color encima de la imagen (oscurece para que el texto se lea)
            // Usa el slider 'backgroundOverlayAlpha' como override del alpha del color.
            if (backgroundOverlayAlpha > 0.001f)
            {
                var ovGo = new GameObject("BG_Overlay");
                ovGo.transform.SetParent(canvas.transform, false);
                var ovRT = ovGo.AddComponent<RectTransform>();
                ovRT.anchorMin = Vector2.zero;
                ovRT.anchorMax = Vector2.one;
                ovRT.offsetMin = Vector2.zero;
                ovRT.offsetMax = Vector2.zero;
                var ovImg = ovGo.AddComponent<Image>();
                var ovColor = backgroundOverlayColor;
                ovColor.a = backgroundOverlayAlpha;
                ovImg.color = ovColor;
                ovImg.raycastTarget = false;
            }

            // Vignette con tinte rojizo (sangre seca)
            BuildVignette(canvas.transform);

            // Noise grain sutil sobre todo
            BuildNoiseLayer(canvas.transform);

            // Intense Glitch: pre-crear N bandas horizontales, ocultas por defecto
            if (intenseGlitchEnabled && glitchBandCount > 0)
            {
                glitchBandGOs = new GameObject[glitchBandCount];
                glitchBandImgs = new Image[glitchBandCount];
                glitchBandRTs = new RectTransform[glitchBandCount];
                for (int i = 0; i < glitchBandCount; i++)
                {
                    var go = new GameObject($"GlitchBand_{i}", typeof(RectTransform));
                    var rt = go.GetComponent<RectTransform>();
                    rt.SetParent(canvas.transform, false);
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(0f, 30f);
                    rt.anchoredPosition = Vector2.zero;
                    var img = go.AddComponent<Image>();
                    img.color = Color.clear;
                    img.raycastTarget = false;
                    go.SetActive(false);
                    glitchBandGOs[i] = go;
                    glitchBandImgs[i] = img;
                    glitchBandRTs[i] = rt;
                }
            }

            // === LAYOUT CINEMATOGRAFICO CENTRADO ===
            // MenuPanel agrupa el UI del menu (titulo, botones, etc) bajo un CanvasGroup
            // para poder ocultarlos al abrir Ajustes sin perder el fondo.
            var menuPanelGo = new GameObject("MenuPanel", typeof(RectTransform));
            var menuPanelRT = menuPanelGo.GetComponent<RectTransform>();
            menuPanelRT.SetParent(canvas.transform, false);
            menuPanelRT.anchorMin = Vector2.zero;
            menuPanelRT.anchorMax = Vector2.one;
            menuPanelRT.offsetMin = Vector2.zero;
            menuPanelRT.offsetMax = Vector2.zero;
            menuPanelGroup = menuPanelGo.AddComponent<CanvasGroup>();
            menuPanelTransform = menuPanelGo.transform;

            // Origen: centro del canvas. Todos los elementos se anclan en (0.5, 0.5).
            // Title arriba del centro, subtitle pegado abajo, botones en columna, footer al pie.
            titleText = MakeMenuText("Title", gameTitle, 110, FontStyle.Bold, titleColor,
                new Vector2(0f, 220f), new Vector2(1400, 160), useTitleFont: true);
            titleRT = titleText.GetComponent<RectTransform>();
            titleAnchoredOriginalPos = titleRT.anchoredPosition;

            // Subtitle - pegado debajo del titulo
            MakeMenuText("Subtitle", "— " + gameSubtitle + " —", 28, FontStyle.Italic, subtitleColor,
                new Vector2(0f, 120f), new Vector2(900, 44), useTitleFont: false);

            // Botones en columna centrada, debajo del subtitle.
            playButton = MakeMenuButton("Btn_Play", "NUEVA PARTIDA", Vector2.zero, OnPlayClicked);
            continueButton = MakeMenuButton("Btn_Continue", "CONTINUAR", Vector2.zero, OnContinueClicked);
            settingsButton = MakeMenuButton("Btn_Settings", "AJUSTES", Vector2.zero, OnSettingsClicked);
            quitButton = MakeMenuButton("Btn_Quit", "SALIR", Vector2.zero, OnQuitClicked);

            buttonRTs = new RectTransform[] {
                playButton.GetComponent<RectTransform>(),
                continueButton.GetComponent<RectTransform>(),
                settingsButton.GetComponent<RectTransform>(),
                quitButton.GetComponent<RectTransform>()
            };

            SetupNavigation(playButton, continueButton, settingsButton, quitButton);
            RelayoutButtons();

            // Hint pequeño debajo del boton Continuar para indicar estado (sin partidas / sin conexion).
            continueHintText = MakeMenuText("ContinueHint", "", 12, FontStyle.Italic,
                new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.6f),
                Vector2.zero, new Vector2(buttonWidth, 14f), useTitleFont: false);
            var continueRT = continueButton.GetComponent<RectTransform>();
            var hintRT = continueHintText.GetComponent<RectTransform>();
            hintRT.anchoredPosition = new Vector2(continueRT.anchoredPosition.x,
                continueRT.anchoredPosition.y - (buttonHeight * 0.5f) - 7f);
            continueHintText.gameObject.SetActive(false);

            // Footer / version - centrado al pie del canvas
            MakeMenuText("Footer", versionLabel, 14, FontStyle.Normal,
                new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.45f),
                new Vector2(0f, -460f), new Vector2(800, 28), useTitleFont: false);
        }

        private void BuildVignette(Transform parent)
        {
            if (vignetteAlpha <= 0.001f) return;
            var vGo = new GameObject("Vignette");
            vGo.transform.SetParent(parent, false);
            var rt = vGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = vGo.AddComponent<Image>();
            var vColor = vignetteColor;
            vColor.a = vignetteAlpha;
            img.sprite = MakeRadialVignetteSprite(256, vColor);
            img.raycastTarget = false;
        }

        private Sprite MakeVerticalGradientSprite(int height, Color top, Color bottom)
        {
            var tex = new Texture2D(2, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[2 * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color c = Color.Lerp(bottom, top, t);
                px[y * 2] = c;
                px[y * 2 + 1] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, height), new Vector2(0.5f, 0.5f));
        }

        private void BuildNoiseLayer(Transform parent)
        {
            if (!noiseEnabled) return;
            var go = new GameObject("Noise", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = MakeNoiseSprite(512);
            img.color = new Color(1f, 1f, 1f, noiseAlpha);
            img.raycastTarget = false;
        }

        private Sprite MakeNoiseSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                float v = Random.value;
                px[i] = new Color(v, v, v, v);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite MakeRadialVignetteSprite(int size, Color edge)
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
                // Curva mas pronunciada: 0 hasta 0.3, despues sube fuerte
                float t = Mathf.Clamp01((d - 0.3f) / 0.7f);
                t = t * t; // ease in cuadratico
                px[y * size + x] = new Color(edge.r, edge.g, edge.b, edge.a * t);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Button MakeMenuButton(string name, string label, Vector2 anchoredPos, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(menuPanelTransform != null ? menuPanelTransform : canvas.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = buttonNormalColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = buttonHoverColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            colors.fadeDuration = 0.12f;
            btn.colors = colors;
            btn.onClick.AddListener(() => { PlayUiClip(uiClickClip, uiClickVolume); onClick?.Invoke(); });

            // Hover via EventTrigger
            var trigger = go.AddComponent<EventTrigger>();
            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener(_ => PlayUiClip(uiHoverClip, uiHoverVolume));
            trigger.triggers.Add(hoverEntry);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(buttonHoverColor.r, buttonHoverColor.g, buttonHoverColor.b, 0.4f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Label
            var lblGo = new GameObject("Label", typeof(RectTransform));
            var lblRT = lblGo.GetComponent<RectTransform>();
            lblRT.SetParent(go.transform, false);
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(20f, 0f);
            lblRT.offsetMax = new Vector2(-20f, 0f);
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.font = GetBodyFont();
            lblTxt.text = label;
            lblTxt.alignment = TextAnchor.MiddleCenter;
            lblTxt.fontSize = 28;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.color = buttonTextColor;
            lblTxt.raycastTarget = false;

            return btn;
        }

        private Text MakeMenuText(string name, string content, int size, FontStyle style, Color color,
            Vector2 anchoredPos, Vector2 sizeDelta, bool useTitleFont)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(menuPanelTransform != null ? menuPanelTransform : canvas.transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = useTitleFont ? GetTitleFont() : GetBodyFont();
            t.text = content;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.raycastTarget = false;

            if (useTitleFont)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
                shadow.effectDistance = new Vector2(3f, -4f);
            }

            return t;
        }

        private void SetupNavigation(params Button[] btns)
        {
            int n = btns.Length;
            for (int i = 0; i < n; i++)
            {
                SetVerticalNav(btns[i],
                    prev: btns[(i - 1 + n) % n],
                    next: btns[(i + 1) % n]);
            }
        }

        private void SetVerticalNav(Button btn, Button prev, Button next)
        {
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = prev;
            nav.selectOnDown = next;
            btn.navigation = nav;
        }

        private Font GetTitleFont()
        {
            if (titleFont != null) return titleFont;
            return GetDefaultFont();
        }

        private Font GetBodyFont()
        {
            if (bodyFont != null) return bodyFont;
            return GetDefaultFont();
        }

        private static Font GetDefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
