using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EstanDentro.UI;

namespace EstanDentro.Player
{
    /// <summary>
    /// Secuencia simple de "despertar". Overlay negro fullscreen con pestañeos lentos
    /// y look around natural. Sin post-process HDRP (para evitar bugs de fisica/render).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerWakeUp : MonoBehaviour
    {
        [Header("Activacion")]
        [SerializeField] private bool playOnStart = true;

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

        [Header("Camara - pitch")]
        [SerializeField] private float startPitch = 55f;
        [SerializeField] private float endPitch = 0f;

        [Header("Objetivo HUD")]
        [SerializeField, Tooltip("Texto que aparece en el ObjectiveHUD al terminar la animacion de despertar. Vacio = no se muestra.")]
        private string firstObjective = "Sal del salón";

        private PlayerController playerController;
        private Transform cameraPivot;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Image blackImage;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            cameraPivot = playerController.CameraPivot;
            BuildOverlay();
        }

        private void Start()
        {
            if (!playOnStart) return;
            StartCoroutine(WakeUpRoutine());
        }

        private IEnumerator WakeUpRoutine()
        {
            playerController.InputEnabled = false;

            // Estado inicial: ojos cerrados (negro full), camara mirando al escritorio
            playerController.SetPitch(startPitch);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(startPitch, 0f, 0f);
            blackImage.color = Color.black;
            canvasGroup.alpha = 1f;
            canvas.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(blackHoldSeconds);

            // ----- Fase 1: pestaneos -----
            for (int i = 0; i < blinks; i++)
            {
                float p = (float)(i + 1) / blinks;
                float openAlpha = Mathf.Lerp(0.7f, 0f, p);

                yield return FadeAlpha(canvasGroup.alpha, openAlpha, blinkOpenSeconds);
                yield return new WaitForSecondsRealtime(blinkPauseOpen);

                if (i < blinks - 1)
                {
                    yield return FadeAlpha(openAlpha, 1f, blinkCloseSeconds);
                    yield return new WaitForSecondsRealtime(0.12f);
                }
            }

            // El overlay ya tiene alpha bajo o 0, podemos ocultar el canvas
            canvas.gameObject.SetActive(false);

            // ----- Fase 2: look around natural -----
            yield return new WaitForSecondsRealtime(lookStartDelay);

            float baseYaw = transform.eulerAngles.y;

            // Izquierda
            yield return RotateAndPitch(baseYaw, baseYaw - lookYawAmplitude, startPitch, Mathf.Lerp(startPitch, endPitch, 0.4f), lookLeftSeconds);
            yield return new WaitForSecondsRealtime(lookLeftPause);

            // Derecha (recorrido completo)
            yield return RotateAndPitch(baseYaw - lookYawAmplitude, baseYaw + lookYawAmplitude, Mathf.Lerp(startPitch, endPitch, 0.4f), Mathf.Lerp(startPitch, endPitch, 0.75f), lookRightSeconds);
            yield return new WaitForSecondsRealtime(lookRightPause);

            // Volver al centro y enfocar
            yield return RotateAndPitch(baseYaw + lookYawAmplitude, baseYaw, Mathf.Lerp(startPitch, endPitch, 0.75f), endPitch, lookCenterSeconds);

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
