using UnityEngine;
using UnityEngine.InputSystem;

namespace EstanDentro.Breathing
{
    [DefaultExecutionOrder(-90)]
    public class BreathingInputProvider : MonoBehaviour
    {
        public static BreathingInputProvider Instance { get; private set; }

        [Header("Microfono")]
        [SerializeField, Tooltip("Substring del nombre de device. Vacio = primer device. Ej: 'Wireless Controller' matchea el mic del DualSense.")]
        private string preferredDeviceName = "Wireless Controller";
        [SerializeField] private int desiredSampleRate = 44100;
        [SerializeField, Tooltip("Tamano de ventana RMS en samples. Mayor = mas suave, menor = mas reactivo.")]
        private int rmsWindow = 1024;
        [SerializeField, Tooltip("Diferencia minima exhale-noise para aceptar mic. Si menor, fallback teclado.")]
        private float minThresholdGap = 0.005f;
        [SerializeField] private bool logDevicesOnStart = true;
        [SerializeField] private bool logRmsPeriodically = true;
        [SerializeField] private float logRmsEverySeconds = 0.5f;
        private float lastRmsLogTime;

        public enum InputMode { Uncalibrated, Microphone, KeyboardFallback }
        public InputMode Mode { get; private set; } = InputMode.Uncalibrated;
        public bool IsCalibrated => Mode != InputMode.Uncalibrated;
        public float NoiseRms { get; private set; }
        public float ExhaleRms { get; private set; }
        public float Threshold { get; private set; }
        public float CurrentRms { get; private set; }

        private AudioClip micClip;
        private string micDevice;
        private float[] readBuffer;

        public bool MicAvailable => Microphone.devices != null && Microphone.devices.Length > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            readBuffer = new float[rmsWindow];
            if (logDevicesOnStart) LogDevices();
        }

        private void LogDevices()
        {
            var devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[Breathing] Unity NO ve ningun microfono. Revisa Windows -> Privacidad -> Microfono y que no este muteado.");
                return;
            }
            Debug.Log($"[Breathing] {devices.Length} microfono(s) detectado(s):");
            for (int i = 0; i < devices.Length; i++)
            {
                int min, max;
                Microphone.GetDeviceCaps(devices[i], out min, out max);
                string freqDesc = (min == 0 && max == 0) ? "cualquier freq" : $"{min}-{max} Hz";
                Debug.Log($"   [{i}] \"{devices[i]}\"  ({freqDesc})");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            StopMic();
        }

        public bool TryStartMic()
        {
            if (!MicAvailable) { Debug.LogWarning("[Breathing] No hay devices de microfono."); return false; }
            try
            {
                micDevice = ResolveDevice();
                int min, max;
                Microphone.GetDeviceCaps(micDevice, out min, out max);
                int freq;
                if (min == 0 && max == 0) freq = desiredSampleRate;
                else freq = Mathf.Clamp(desiredSampleRate, min, max);
                micClip = Microphone.Start(micDevice, true, 1, freq);
                Debug.Log($"[Breathing] Mic abierto: \"{micDevice}\" @ {freq} Hz. Recording={Microphone.IsRecording(micDevice)}");
                return micClip != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Breathing] No pude abrir mic: {e.Message}");
                micClip = null;
                return false;
            }
        }

        private string ResolveDevice()
        {
            var devices = Microphone.devices;
            if (!string.IsNullOrEmpty(preferredDeviceName))
            {
                for (int i = 0; i < devices.Length; i++)
                    if (devices[i] != null && devices[i].IndexOf(preferredDeviceName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return devices[i];
                Debug.LogWarning($"[Breathing] Ningun device contiene '{preferredDeviceName}'. Uso el primero.");
            }
            return devices[0];
        }

        public void StopMic()
        {
            if (!string.IsNullOrEmpty(micDevice) && Microphone.IsRecording(micDevice))
                Microphone.End(micDevice);
            micDevice = null;
            micClip = null;
        }

        private void Update()
        {
            if (micClip != null && Microphone.IsRecording(micDevice))
            {
                CurrentRms = SampleRms();

                if (logRmsPeriodically && Time.unscaledTime - lastRmsLogTime > logRmsEverySeconds)
                {
                    lastRmsLogTime = Time.unscaledTime;
                    Debug.Log($"[Breathing] RMS={CurrentRms:F5}  pos={Microphone.GetPosition(micDevice)}  device='{micDevice}'");
                }
            }
        }

        private float SampleRms()
        {
            int micPos = Microphone.GetPosition(micDevice);
            if (micPos < rmsWindow) return 0f; // mic todavia llenando buffer
            int pos = micPos - rmsWindow;
            if (!micClip.GetData(readBuffer, pos)) return 0f;
            double sum = 0;
            for (int i = 0; i < readBuffer.Length; i++)
                sum += readBuffer[i] * readBuffer[i];
            return Mathf.Sqrt((float)(sum / readBuffer.Length));
        }

        public void ApplyCalibration(float noiseRms, float exhaleRms)
        {
            NoiseRms = noiseRms;
            ExhaleRms = exhaleRms;
            float gap = exhaleRms - noiseRms;
            if (gap < minThresholdGap)
            {
                ForceFallback("calibracion mic insuficiente (gap " + gap.ToString("F4") + ")");
                return;
            }
            Threshold = noiseRms + gap * 0.5f;
            Mode = InputMode.Microphone;
            Debug.Log($"[Breathing] Mic calibrado. noise={noiseRms:F4} exhale={exhaleRms:F4} threshold={Threshold:F4}");
            SaveCalibration();
        }

        public void ForceFallback(string reason)
        {
            StopMic();
            Mode = InputMode.KeyboardFallback;
            Debug.Log($"[Breathing] Fallback teclado activo. Motivo: {reason}");
            SaveCalibration();
        }

        // ----- Persistencia (PlayerPrefs) -----

        private const string PREF_KEY_CALIBRATED = "breathing_calibrated";
        private const string PREF_KEY_THRESHOLD = "breathing_threshold";
        private const string PREF_KEY_FALLBACK = "breathing_use_fallback";
        private const string PREF_KEY_NOISE = "breathing_noise";
        private const string PREF_KEY_EXHALE = "breathing_exhale";

        public bool HasStoredCalibration() => PlayerPrefs.GetInt(PREF_KEY_CALIBRATED, 0) == 1;

        public bool TryLoadStoredCalibration()
        {
            if (!HasStoredCalibration()) return false;
            bool useFallback = PlayerPrefs.GetInt(PREF_KEY_FALLBACK, 0) == 1;
            NoiseRms = PlayerPrefs.GetFloat(PREF_KEY_NOISE, 0f);
            ExhaleRms = PlayerPrefs.GetFloat(PREF_KEY_EXHALE, 0f);
            if (useFallback)
            {
                Mode = InputMode.KeyboardFallback;
                Debug.Log("[Breathing] Calibracion previa: fallback teclado.");
                return true;
            }
            Threshold = PlayerPrefs.GetFloat(PREF_KEY_THRESHOLD, 0f);
            if (TryStartMic())
            {
                Mode = InputMode.Microphone;
                Debug.Log($"[Breathing] Calibracion previa cargada. Threshold={Threshold:F4}");
                return true;
            }
            // No pudo abrir mic con la calibracion guardada: caer a fallback sin perder pref
            Mode = InputMode.KeyboardFallback;
            Debug.LogWarning("[Breathing] Calibracion previa pero mic no abrio. Uso fallback temporal.");
            return true;
        }

        public void SaveCalibration()
        {
            PlayerPrefs.SetInt(PREF_KEY_CALIBRATED, 1);
            PlayerPrefs.SetInt(PREF_KEY_FALLBACK, Mode == InputMode.KeyboardFallback ? 1 : 0);
            PlayerPrefs.SetFloat(PREF_KEY_NOISE, NoiseRms);
            PlayerPrefs.SetFloat(PREF_KEY_EXHALE, ExhaleRms);
            if (Mode == InputMode.Microphone)
                PlayerPrefs.SetFloat(PREF_KEY_THRESHOLD, Threshold);
            PlayerPrefs.Save();
        }

        public static void ClearStoredCalibration()
        {
            PlayerPrefs.DeleteKey(PREF_KEY_CALIBRATED);
            PlayerPrefs.DeleteKey(PREF_KEY_THRESHOLD);
            PlayerPrefs.DeleteKey(PREF_KEY_FALLBACK);
            PlayerPrefs.DeleteKey(PREF_KEY_NOISE);
            PlayerPrefs.DeleteKey(PREF_KEY_EXHALE);
            PlayerPrefs.Save();
            Debug.Log("[Breathing] Calibracion guardada borrada.");
        }

        public bool IsExhalingNow()
        {
            switch (Mode)
            {
                case InputMode.Microphone:
                    return CurrentRms > Threshold;
                case InputMode.KeyboardFallback:
                    return !FallbackInhaleHeld();
                default:
                    return false;
            }
        }

        public static bool FallbackInhaleHeld()
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            return (kb != null && kb.spaceKey.isPressed)
                || (gp != null && gp.buttonNorth.isPressed);
        }

        public float NormalizedExhaleLevel()
        {
            if (Mode == InputMode.KeyboardFallback)
                return IsExhalingNow() ? 1f : 0f;
            if (Mode != InputMode.Microphone || Threshold <= 0f) return 0f;
            return Mathf.Clamp01(CurrentRms / (Threshold * 1.5f));
        }
    }
}
