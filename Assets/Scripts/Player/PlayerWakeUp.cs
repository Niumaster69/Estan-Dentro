using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using EstanDentro.UI;
using EstanDentro.Breathing;

namespace EstanDentro.Player
{
    /// <summary>
    /// Secuencia simple de "despertar". Overlay negro fullscreen con pestañeos lentos
    /// y look around natural. Sin post-process HDRP (para evitar bugs de fisica/render).
    ///
    /// Audio: el latido (heartbeatLoopClip) suena fuerte durante los pestaneos y decae
    /// durante el look around. El jadeo seco (gaspClip) se dispara una vez al primer pestaneo.
    ///
    /// Al terminar la rutina, fuerza el minijuego de respiracion (BreathingMinigame.ForceShow)
    /// para que el primer momento del juego sea respirar. Se libera tras forcedBreathingSeconds.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerWakeUp : MonoBehaviour
    {
        [Header("Activacion")]
        [SerializeField] private bool playOnStart = true;

        [Header("Audio")]
        [SerializeField, Tooltip("Latido (heartbeat) en loop. Vol fuerte durante pestaneos, decae durante look around.")]
        private AudioClip heartbeatLoopClip;
        [SerializeField, Range(0f, 1f), Tooltip("Volumen del latido al pico (durante pestaneos).")]
        private float heartbeatPeakVolume = 0.8f;
        [SerializeField, Range(0.5f, 1.5f)] private float heartbeatPeakPitch = 1.1f;
        [SerializeField, Range(0.5f, 1.5f)] private float heartbeatRestPitch = 0.85f;
        [SerializeField, Tooltip("Jadeo seco que se dispara una vez al primer pestaneo (gasp de respirar de golpe al despertar).")]
        private AudioClip gaspClip;
        [SerializeField, Range(0f, 1f)] private float gaspVolume = 0.9f;

        [Header("Forzar respiracion al despertar")]
        [SerializeField, Tooltip("Al terminar la rutina de despertar, fuerza el minijuego de respiracion. 0 = no forzar (deja al stress decidir). Por default 0 porque el trigger esta delegado a LockedDoor.")]
        private float forcedBreathingSeconds = 0f;

        [Header("Vista borrosa post-pestaneo (overlay legacy)")]
        [SerializeField, Range(0f, 0.5f), Tooltip("Alpha del overlay residual sobre la pantalla. Deja en 0 si usas Volume HDRP (mas natural).")]
        private float blurRemainAlpha = 0f;
        [SerializeField] private Color blurTint = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private float blurFadeOutSeconds = 0.5f;

        [Header("Vision borrosa REAL via Volume HDRP")]
        [SerializeField, Tooltip("Si true, crea un Volume con DOF + Vignette + LensDistortion + ChromaticAberration que simula vision borrosa REAL sobre el mundo renderizado.")]
        private bool useHdrpBlurVolume = true;
        [SerializeField, Range(0f, 1f), Tooltip("Vignette maximo al despertar.")]
        private float vignetteMax = 0.55f;
        [SerializeField, Range(-1f, 1f), Tooltip("LensDistortion al despertar. Negativo = pinch (apretado), positivo = barrel (expandido). -0.3 sugiere ojos no enfocados.")]
        private float lensDistortionMax = -0.35f;
        [SerializeField, Range(0f, 1f), Tooltip("ChromaticAberration maximo. Da sensacion de vision desincronizada.")]
        private float chromaticAberrationMax = 0.55f;
        [SerializeField, Tooltip("Peso residual del Volume DURANTE el look around (0=vista clara, 1=full borroso). Bajo = sutil. Por default 0.4.")]
        private float blurWeightDuringLook = 0.4f;

        [Header("Fase 1 - Pestaneos")]
        [SerializeField] private float blackHoldSeconds = 1.5f;
        [SerializeField, Range(1, 5)] private int blinks = 3;
        [SerializeField] private float blinkOpenSeconds = 0.9f;
        [SerializeField] private float blinkCloseSeconds = 0.35f;
        [SerializeField] private float blinkPauseOpen = 0.4f;

        [Header("Fase 2 - Look around natural")]
        [SerializeField] private float lookStartDelay = 0.6f;
        [SerializeField] private float lookYawAmplitude = 22f;
        [SerializeField] private float lookLeftSeconds = 2.0f;
        [SerializeField] private float lookLeftPause = 0.5f;
        [SerializeField] private float lookRightSeconds = 2.8f;
        [SerializeField] private float lookRightPause = 0.5f;
        [SerializeField] private float lookCenterSeconds = 1.5f;

        [Header("Pestaneos lentos DURANTE look around")]
        [SerializeField, Tooltip("Si true, durante el look around se hacen pestaneos suaves random. Por default false (se sentian antinaturales).")]
        private bool slowBlinksDuringLook = false;
        [SerializeField, Range(0f, 1f), Tooltip("Alpha del overlay en el pico del pestaneo lento. 1.0 = full negro como los pestaneos iniciales.")]
        private float slowBlinkPeakAlpha = 1f;
        [SerializeField, Tooltip("Tiempo de cerrar los ojos en el pestaneo lento.")]
        private float slowBlinkCloseSeconds = 0.3f;
        [SerializeField, Tooltip("Tiempo de pausa con ojos cerrados.")]
        private float slowBlinkHoldClosedSeconds = 0.15f;
        [SerializeField, Tooltip("Tiempo de abrir los ojos en el pestaneo lento (mas lento = mas pesado).")]
        private float slowBlinkOpenSeconds = 0.6f;
        [SerializeField, Tooltip("Pausa entre pestaneos lentos (min/max). Random entre estos dos valores.")]
        private Vector2 slowBlinkIntervalRange = new Vector2(2.5f, 5.5f);

        [Header("Camara - pitch")]
        [SerializeField] private float startPitch = 55f;
        [SerializeField] private float endPitch = 0f;

        [Header("Objetivo HUD")]
        [SerializeField, Tooltip("Texto que aparece en el ObjectiveHUD al terminar la animacion de despertar. Vacio = no se muestra.")]
        private string firstObjective = "Sal del salón";

        [Header("Eventos al terminar la rutina")]
        [SerializeField, Tooltip("Eventos disparados al final del despertar (despues del look around). Util para arrancar el ambiente del salon (AmbientLoopWithRandomEvents.StartAmbience).")]
        public UnityEvent onWakeUpComplete;

        private PlayerController playerController;
        private Transform cameraPivot;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Image blackImage;
        private AudioSource heartbeatSrc;
        private AudioSource oneShotSrc;

        // Volume HDRP (vision borrosa real)
        private Volume blurVolume;
        private VolumeProfile blurProfile;
        private DepthOfField dofEffect;
        private Vignette vignetteEffect;
        private LensDistortion lensEffect;
        private ChromaticAberration chromaticEffect;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            cameraPivot = playerController.CameraPivot;
            BuildOverlay();
            BuildAudioSources();
            if (useHdrpBlurVolume) BuildBlurVolume();
        }

        private void BuildBlurVolume()
        {
            var volGo = new GameObject("WakeUp_BlurVolume");
            volGo.transform.SetParent(transform, false);
            blurVolume = volGo.AddComponent<Volume>();
            blurVolume.isGlobal = true;
            blurVolume.priority = 9999f; // alto para ganarle al global volume del proyecto
            blurVolume.weight = 0f; // arranca apagado

            blurProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            blurVolume.profile = blurProfile;

            dofEffect = blurProfile.Add<DepthOfField>();
            dofEffect.focusMode.value = DepthOfFieldMode.Manual;
            dofEffect.focusMode.overrideState = true;
            dofEffect.nearFocusStart.value = 0f;
            dofEffect.nearFocusStart.overrideState = true;
            // nearFocusEnd alto = todo el frame queda en el rango de blur cercano
            dofEffect.nearFocusEnd.value = 50f;
            dofEffect.nearFocusEnd.overrideState = true;

            vignetteEffect = blurProfile.Add<Vignette>();
            vignetteEffect.intensity.value = vignetteMax;
            vignetteEffect.intensity.overrideState = true;
            vignetteEffect.color.value = new Color(0.02f, 0.02f, 0.03f, 1f);
            vignetteEffect.color.overrideState = true;
            vignetteEffect.smoothness.value = 0.45f;
            vignetteEffect.smoothness.overrideState = true;

            lensEffect = blurProfile.Add<LensDistortion>();
            lensEffect.intensity.value = lensDistortionMax;
            lensEffect.intensity.overrideState = true;

            chromaticEffect = blurProfile.Add<ChromaticAberration>();
            chromaticEffect.intensity.value = chromaticAberrationMax;
            chromaticEffect.intensity.overrideState = true;
        }

        private void BuildAudioSources()
        {
            heartbeatSrc = gameObject.AddComponent<AudioSource>();
            heartbeatSrc.clip = heartbeatLoopClip;
            heartbeatSrc.loop = true;
            heartbeatSrc.playOnAwake = false;
            heartbeatSrc.spatialBlend = 0f;
            heartbeatSrc.volume = 0f;
            heartbeatSrc.pitch = heartbeatPeakPitch;

            oneShotSrc = gameObject.AddComponent<AudioSource>();
            oneShotSrc.playOnAwake = false;
            oneShotSrc.spatialBlend = 0f;
        }

        private void Start()
        {
            Debug.Log($"[WakeUp] Start. playOnStart={playOnStart} enabled={enabled} gameObject.activeInHierarchy={gameObject.activeInHierarchy}");
            if (!playOnStart) return;
            StartCoroutine(WakeUpRoutine());
        }

        /// <summary>
        /// Limpia el overlay y blur volume que Awake() creo. Llamado por PlayerSpawner cuando
        /// el SpawnPoint destino tiene SkipWakeUp = true. Si solo se hace wakeUp.enabled = false,
        /// el overlay negro creado en Awake permanece visible bloqueando la vista del player.
        /// </summary>
        public void AbortAndCleanup()
        {
            StopAllCoroutines();
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
            if (blurVolume != null)
            {
                Destroy(blurVolume.gameObject);
                blurVolume = null;
            }
            if (heartbeatSrc != null && heartbeatSrc.isPlaying) heartbeatSrc.Stop();
            enabled = false;
        }

        private IEnumerator WakeUpRoutine()
        {
            Debug.Log("[WakeUp] WakeUpRoutine arranca. Canvas seteado a negro full.");
            playerController.InputEnabled = false;

            // Estado inicial: ojos cerrados (negro full), camara mirando al escritorio
            playerController.SetPitch(startPitch);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(startPitch, 0f, 0f);
            blackImage.color = Color.black;
            canvasGroup.alpha = 1f;
            canvas.gameObject.SetActive(true);

            // Esperar a que el LoadingScreen termine su fade-out (sino tapa los pestaneos)
            float waitStart = Time.realtimeSinceStartup;
            while (LoadingScreenController.Instance != null && LoadingScreenController.Instance.IsVisible)
            {
                yield return null;
                if (Time.realtimeSinceStartup - waitStart > 10f)
                {
                    Debug.LogWarning("[WakeUp] Timeout esperando LoadingScreen (10s). Procediendo anyway.");
                    break;
                }
            }
            Debug.Log($"[WakeUp] LoadingScreen ya no esta visible. Empezando pestaneos. Canvas activo: {canvas.gameObject.activeSelf}, alpha: {canvasGroup.alpha}");

            // Activar blur HDRP a maximo (vista totalmente borrosa)
            if (blurVolume != null) blurVolume.weight = 1f;

            // Latido al pico desde antes del primer pestaneo
            if (heartbeatLoopClip != null)
            {
                heartbeatSrc.volume = heartbeatPeakVolume;
                heartbeatSrc.pitch = heartbeatPeakPitch;
                heartbeatSrc.Play();
            }

            yield return new WaitForSecondsRealtime(blackHoldSeconds);

            // ----- Fase 1: pestaneos -----
            for (int i = 0; i < blinks; i++)
            {
                float p = (float)(i + 1) / blinks;
                float openAlpha = Mathf.Lerp(0.7f, 0f, p);

                // Gasp seco solo en el primer pestaneo (respira de golpe al abrir los ojos)
                if (i == 0 && gaspClip != null) oneShotSrc.PlayOneShot(gaspClip, gaspVolume);

                yield return FadeAlpha(canvasGroup.alpha, openAlpha, blinkOpenSeconds);
                yield return new WaitForSecondsRealtime(blinkPauseOpen);

                if (i < blinks - 1)
                {
                    yield return FadeAlpha(openAlpha, 1f, blinkCloseSeconds);
                    yield return new WaitForSecondsRealtime(0.12f);
                }
            }

            // Vista borrosa: mantener el overlay con tinte sutil
            blackImage.color = blurTint;

            // ARRANQUE: el ultimo pestaneo termina con alpha 0. Subimos a alpha "residual inicial" antes del look around.
            // Esto da el negro residual que se va a desvanecer LENTAMENTE durante todo el look around.
            float initialResidualAlpha = Mathf.Max(blurRemainAlpha, 0.55f);
            canvasGroup.alpha = initialResidualAlpha;

            // Calculo la duracion total del look around para hacer que el fade tarde toda esa fase
            float totalLookDuration = lookStartDelay + lookLeftSeconds + lookLeftPause + lookRightSeconds + lookRightPause + lookCenterSeconds;

            // Fade GRADUAL del overlay durante TODO el look around (no jump abrupto)
            StartCoroutine(FadeAlpha(initialResidualAlpha, 0f, totalLookDuration));

            // Fade GRADUAL del blur HDRP durante TODO el look around (acompaña el aclarar de la vision)
            if (blurVolume != null) StartCoroutine(FadeBlurVolume(blurVolume.weight, 0f, totalLookDuration));

            // Latido decae durante el look around (de peak -> rest pitch/vol)
            if (heartbeatSrc.isPlaying) StartCoroutine(FadeHeartbeatToRest(2.5f));

            // ----- Fase 2: look around natural -----
            yield return new WaitForSecondsRealtime(lookStartDelay);

            // Pestaneos lentos en paralelo durante el look around
            Coroutine slowBlinkCo = null;
            if (slowBlinksDuringLook) slowBlinkCo = StartCoroutine(SlowBlinksRoutine());

            float baseYaw = transform.eulerAngles.y;

            // Izquierda
            yield return RotateAndPitch(baseYaw, baseYaw - lookYawAmplitude, startPitch, Mathf.Lerp(startPitch, endPitch, 0.4f), lookLeftSeconds);
            yield return new WaitForSecondsRealtime(lookLeftPause);

            // Derecha (recorrido completo)
            yield return RotateAndPitch(baseYaw - lookYawAmplitude, baseYaw + lookYawAmplitude, Mathf.Lerp(startPitch, endPitch, 0.4f), Mathf.Lerp(startPitch, endPitch, 0.75f), lookRightSeconds);
            yield return new WaitForSecondsRealtime(lookRightPause);

            // Volver al centro y enfocar
            yield return RotateAndPitch(baseYaw + lookYawAmplitude, baseYaw, Mathf.Lerp(startPitch, endPitch, 0.75f), endPitch, lookCenterSeconds);

            // Detener pestaneos lentos si estaban
            if (slowBlinkCo != null) StopCoroutine(slowBlinkCo);

            // En este punto el fade ya termino durante el look around. Por seguridad: asegurar alpha=0 y weight=0
            canvasGroup.alpha = 0f;
            if (blurVolume != null) blurVolume.weight = 0f;
            canvas.gameObject.SetActive(false);

            // Devolver control
            playerController.InputEnabled = true;

            // Tutorial primer toast: explica el inventario + bindings
            ObjectiveHUD.Notify("Apretá [I] (teclado) o [Touchpad] (mando) para abrir tu inventario y misiones.", 7f);

            // Esperar un momento para que se vea el tutorial antes del objetivo
            yield return new WaitForSecondsRealtime(2.5f);

            // Agregar primer objetivo principal al sistema de misiones
            if (!string.IsNullOrEmpty(firstObjective))
            {
                if (Inventory.Inventory.Instance != null)
                {
                    Inventory.Inventory.Instance.AddMission(
                        "salir_salon",
                        firstObjective,
                        Inventory.Inventory.MissionCategory.Principal);
                }
                ObjectiveHUD.PulseCircle();
                ObjectiveHUD.Notify("Nueva misión: " + firstObjective, 4f);
            }

            // Forzar el primer minijuego de respiracion (independiente del stress)
            if (forcedBreathingSeconds > 0f && BreathingMinigame.Instance != null)
            {
                BreathingMinigame.Instance.ForceShow();
                StartCoroutine(ReleaseForcedBreathingAfter(forcedBreathingSeconds));
            }
            else
            {
                // Sin respiracion forzada: apagar el heartbeat directamente al terminar el despertar
                // (sin esto se quedaba en loop indefinido a volumen residual generando un drone molesto).
                StartCoroutine(FadeHeartbeatOutAndStop(1.5f));
            }

            // Disparar eventos al terminar (ej. arrancar ambient sound del salon)
            onWakeUpComplete?.Invoke();
        }

        private IEnumerator SlowBlinksRoutine()
        {
            while (true)
            {
                float wait = Random.Range(slowBlinkIntervalRange.x, slowBlinkIntervalRange.y);
                yield return new WaitForSecondsRealtime(wait);

                // Cerrar ojos (rapido, como los pestaneos del inicio)
                yield return FadeAlpha(canvasGroup.alpha, slowBlinkPeakAlpha, slowBlinkCloseSeconds);
                yield return new WaitForSecondsRealtime(slowBlinkHoldClosedSeconds);
                // Abrir ojos (mas lento, sensacion de parpados pesados)
                yield return FadeAlpha(slowBlinkPeakAlpha, blurRemainAlpha, slowBlinkOpenSeconds);
            }
        }

        private IEnumerator FadeBlurVolume(float from, float to, float duration)
        {
            if (blurVolume == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                blurVolume.weight = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            blurVolume.weight = to;
        }

        private void OnDestroy()
        {
            if (blurProfile != null) Destroy(blurProfile);
        }

        private IEnumerator FadeHeartbeatOutAndStop(float duration)
        {
            if (heartbeatSrc == null || !heartbeatSrc.isPlaying) yield break;
            float t = 0f;
            float startVol = heartbeatSrc.volume;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                heartbeatSrc.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            heartbeatSrc.Stop();
        }

        private IEnumerator FadeHeartbeatToRest(float duration)
        {
            if (heartbeatSrc == null) yield break;
            float startVol = heartbeatSrc.volume;
            float startPitch = heartbeatSrc.pitch;
            float endVol = heartbeatPeakVolume * 0.35f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                heartbeatSrc.volume = Mathf.Lerp(startVol, endVol, p);
                heartbeatSrc.pitch = Mathf.Lerp(startPitch, heartbeatRestPitch, p);
                yield return null;
            }
            heartbeatSrc.volume = endVol;
            heartbeatSrc.pitch = heartbeatRestPitch;
        }

        private IEnumerator ReleaseForcedBreathingAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (BreathingMinigame.Instance != null) BreathingMinigame.Instance.ForceHide();
            // Latido fadeout total
            if (heartbeatSrc != null && heartbeatSrc.isPlaying)
            {
                float t = 0f;
                float startVol = heartbeatSrc.volume;
                while (t < 1.5f)
                {
                    t += Time.unscaledDeltaTime;
                    heartbeatSrc.volume = Mathf.Lerp(startVol, 0f, t / 1.5f);
                    yield return null;
                }
                heartbeatSrc.Stop();
            }
        }

        // ---------- helpers ----------

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, EaseInOut(Mathf.Clamp01(t / duration)));
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        // Rota player (yaw) + lerp pitch en simultaneo. NO toca rotacion X o Z para no romper fisica.
        private IEnumerator RotateAndPitch(float fromYaw, float toYaw, float fromPitch, float toPitch, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = EaseInOut(Mathf.Clamp01(t / duration));
                float yaw = Mathf.LerpAngle(fromYaw, toYaw, p);
                float pitchVal = Mathf.Lerp(fromPitch, toPitch, p);
                // Solo modificamos Y. X y Z quedan en 0.
                transform.localEulerAngles = new Vector3(0f, yaw, 0f);
                playerController.SetPitch(pitchVal);
                if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitchVal, 0f, 0f);
                yield return null;
            }
            transform.localEulerAngles = new Vector3(0f, toYaw, 0f);
            playerController.SetPitch(toPitch);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(toPitch, 0f, 0f);
        }

        private static float EaseInOut(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("PlayerWakeUp_Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            var bgGo = new GameObject("Black", typeof(RectTransform));
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.SetParent(canvas.transform, false);
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            blackImage = bgGo.AddComponent<Image>();
            blackImage.color = Color.black;
        }
    }
}
