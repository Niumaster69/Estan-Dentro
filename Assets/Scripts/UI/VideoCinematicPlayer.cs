using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace EstanDentro.UI
{
    /// <summary>
    /// Reproduce un VideoClip fullscreen como cinematica. Skippeable con cualquier tecla.
    /// Al terminar (o al hacer skip), dispara onComplete y se autodestruye o desactiva.
    ///
    /// Setup minimo:
    ///   - Crear empty GameObject en la escena (no tiene que tener nada).
    ///   - Add Component -> VideoCinematicPlayer.
    ///   - Asignar 'clip' (arrastrar un .mov de Assets/Art/cinematics/Esenas/).
    ///   - (Opcional) Configurar onComplete via Inspector (ej. SceneTransition.LoadScene).
    ///   - Si 'playOnStart' = true, se reproduce solo al cargar la escena.
    ///   - Sino, llamar Play() desde codigo o desde otro evento (ej. trigger collider).
    ///
    /// Implementacion: usa VideoPlayer + RenderTexture + RawImage en un Canvas overlay.
    /// </summary>
    public class VideoCinematicPlayer : MonoBehaviour
    {
        [Header("Video")]
        [SerializeField, Tooltip("Clip de video a reproducir (arrastrar un .mov importado de Assets/Art/cinematics/).")]
        private VideoClip clip;
        [SerializeField, Tooltip("Si true, reproduce automaticamente al cargar la escena.")]
        private bool playOnStart = true;

        [Header("Audio")]
        [SerializeField, Range(0f, 1f), Tooltip("Volumen base del video (multiplicado por Settings.CinematicVolume cada frame).")]
        private float audioVolume = 1f;
        [SerializeField, Tooltip("Si true, silencia los AudioSources y desactiva controllers (AmbientLoop, FlashlightFlicker) de la escena durante el video. Evita que efectos de gameplay suenen encima de la cinematica.")]
        private bool silenceSceneAudio = true;

        private AudioSource videoAudioSrc;

        [Header("UI")]
        [SerializeField, Tooltip("Color de fondo durante el video (negro recomendado).")]
        private Color bgColor = Color.black;
        [SerializeField, Tooltip("Texto de hint para skip. Vacio = no se muestra.")]
        private string skipHint = "[Cualquier tecla para saltar]";
        [SerializeField, Tooltip("Si true, el usuario puede saltar la cinematica con cualquier tecla.")]
        private bool allowSkip = true;

        [Header("Carga de siguiente escena al terminar (opcional)")]
        [SerializeField, Tooltip("Nombre de escena a cargar al terminar el video. Vacio = no cargar nada. Usa SceneTransition.LoadScene (con loading screen).")]
        private string nextSceneName = "";
        [SerializeField, TextArea(1, 3), Tooltip("Tip a mostrar en la pantalla de carga. Vacio = tip rotado del default.")]
        private string loadingTip = "";

        [Header("Comportamiento al terminar")]
        [SerializeField, Tooltip("Eventos disparados al terminar el video (o al hacer skip). Util si quieres logica custom adicional a nextSceneName.")]
        public UnityEvent onComplete;
        [SerializeField, Tooltip("Si true, destruye este GameObject al terminar (despues de disparar onComplete).")]
        private bool destroyOnComplete = false;
        [SerializeField, Tooltip("Si true, desactiva este GameObject al terminar.")]
        private bool deactivateOnComplete = true;

        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RawImage videoImage;
        private bool playing;
        private bool completed;

        private void Start()
        {
            if (playOnStart) Play();
        }

        /// <summary>Asignar clip + (opcional) volumen desde codigo antes de Play().</summary>
        public void SetClip(VideoClip newClip, float volume = -1f)
        {
            clip = newClip;
            if (volume >= 0f) audioVolume = Mathf.Clamp01(volume);
        }

        public void Play()
        {
            if (playing) return;
            if (clip == null) { Debug.LogWarning($"[VideoCinematic] '{name}': clip null, no se puede reproducir."); return; }
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            playing = true;
            completed = false;

            EnsureCameraExists();
            BuildOverlay();
            BuildVideoPlayer();

            // Silenciar efectos de gameplay para que no suenen encima del video
            SilenceSceneAudio();

            // Esperar a que VideoPlayer este preparado (ready to play)
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared) yield return null;

            videoPlayer.Play();

            // Loop hasta que termine o el usuario skip
            while (videoPlayer != null && videoPlayer.isPlaying)
            {
                if (allowSkip && AnyKeyPressed())
                {
                    Debug.Log("[VideoCinematic] Skip por input.");
                    break;
                }
                yield return null;
            }

            Complete();
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
            playing = false;

            if (videoPlayer != null) videoPlayer.Stop();
            onComplete?.Invoke();

            // Limpiar render texture
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            // Cargar siguiente escena si esta configurada
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Time.timeScale = 1f;
                SceneTransition.LoadScene(nextSceneName, tip: string.IsNullOrEmpty(loadingTip) ? null : loadingTip);
                return; // no apagar canvas ni destruir aun: la carga ya esta en curso
            }

            // Apagar canvas
            if (canvas != null) canvas.gameObject.SetActive(false);

            if (destroyOnComplete) Destroy(gameObject);
            else if (deactivateOnComplete) gameObject.SetActive(false);
        }

        private bool AnyKeyPressed()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.anyKey.wasPressedThisFrame)
                || (gp != null && (gp.buttonSouth.wasPressedThisFrame
                                  || gp.buttonNorth.wasPressedThisFrame
                                  || gp.buttonEast.wasPressedThisFrame
                                  || gp.buttonWest.wasPressedThisFrame
                                  || gp.startButton.wasPressedThisFrame));
        }

        // Garantiza que haya una Camera en la escena. Sin Camera, Unity muestra "Display 1 No cameras rendering"
        // aunque el Canvas sea ScreenSpaceOverlay. En las escenas de cinematica que solo tienen este componente,
        // ninguna Camera existe, asi que la creamos dummy aqui.
        private void EnsureCameraExists()
        {
            var existing = Camera.main;
            if (existing != null) return;
            // Fallback: buscar cualquier camara activa
            var any = FindFirstObjectByType<Camera>();
            if (any != null && any.isActiveAndEnabled) return;

            var camGo = new GameObject("Cinematic_Camera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
            cam.cullingMask = 0; // no renderiza nada del mundo, solo el Canvas overlay
            cam.orthographic = true;
            camGo.tag = "MainCamera";
            // AudioListener para que el audio del video se escuche (sin esto el VideoPlayer.audioOutputMode=Direct no suena)
            if (FindFirstObjectByType<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
            Debug.Log("[VideoCinematic] No habia Camera en la escena. Cree una dummy para evitar 'Display 1 No cameras rendering'.");
        }

        private void BuildOverlay()
        {
            if (canvas != null) { canvas.gameObject.SetActive(true); return; }

            var canvasGo = new GameObject("VideoCinematic_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // por encima de todo (mas que EndGame=260, ObserverSlide=220, GameOver=250)

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Fondo negro
            var bgGo = new GameObject("BG", typeof(RectTransform));
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = bgColor;

            // RawImage para el video (fullscreen, fit con aspect ratio mantenido)
            var videoGo = new GameObject("VideoImage", typeof(RectTransform));
            videoGo.transform.SetParent(canvas.transform, false);
            var vRT = videoGo.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one;
            vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;
            videoImage = videoGo.AddComponent<RawImage>();
            videoImage.color = Color.white;
            videoImage.raycastTarget = false;

            // Skip hint abajo
            if (!string.IsNullOrEmpty(skipHint))
            {
                var hintGo = new GameObject("SkipHint", typeof(RectTransform));
                hintGo.transform.SetParent(canvas.transform, false);
                var hRT = hintGo.GetComponent<RectTransform>();
                hRT.anchorMin = new Vector2(0.5f, 0f);
                hRT.anchorMax = new Vector2(0.5f, 0f);
                hRT.pivot = new Vector2(0.5f, 0f);
                hRT.sizeDelta = new Vector2(800f, 30f);
                hRT.anchoredPosition = new Vector2(0f, 30f);
                var hintTxt = hintGo.AddComponent<Text>();
                Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                hintTxt.font = f;
                hintTxt.text = skipHint;
                hintTxt.alignment = TextAnchor.MiddleCenter;
                hintTxt.fontSize = 16;
                hintTxt.color = new Color(0.92f, 0.89f, 0.83f, 0.5f);
                hintTxt.raycastTarget = false;
            }
        }

        private void BuildVideoPlayer()
        {
            // RenderTexture a la resolucion del clip
            int w = (int)clip.width;
            int h = (int)clip.height;
            if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
            renderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            videoImage.texture = renderTexture;

            videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;

            // Enrutamos el audio a un AudioSource propio para que el slider Cinematic de Ajustes lo controle.
            if (videoAudioSrc == null)
            {
                videoAudioSrc = gameObject.AddComponent<AudioSource>();
                videoAudioSrc.playOnAwake = false;
                videoAudioSrc.spatialBlend = 0f;
                videoAudioSrc.volume = audioVolume;
            }
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, videoAudioSrc);
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.clip = clip;
            videoPlayer.skipOnDrop = true;
        }

        private void Update()
        {
            // Aplicar volumen del slider Cinematic de Ajustes en vivo.
            // Master ya se aplica via AudioListener.volume (Settings.MasterVolume setter).
            if (videoAudioSrc != null)
                videoAudioSrc.volume = audioVolume * Mathf.Clamp01(Settings.CinematicVolume);
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        // Detiene AudioSources y desactiva controladores de audio (AmbientLoop, FlashlightFlicker)
        // para que no suenen encima del video. Usa string type names para evitar dependencias duras.
        // No restaura nada porque despues del video la escena cambia.
        private void SilenceSceneAudio()
        {
            if (!silenceSceneAudio) return;

            // Desactivar controllers que fuerzan audio cada Update (re-encenderian la fuente si la paramos)
            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var b in allBehaviours)
            {
                if (b == this) continue;
                var typeName = b.GetType().FullName;
                if (typeName == "EstanDentro.Audio.AmbientLoopWithRandomEvents"
                    || typeName == "EstanDentro.Interaction.FlashlightFlicker")
                {
                    b.enabled = false;
                }
            }

            // Detener todas las fuentes de audio excepto la del video
            var sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var src in sources)
            {
                if (src == videoAudioSrc) continue;
                if (src.isPlaying) src.Stop();
            }
        }
    }
}
