using UnityEngine;

namespace EstanDentro.UI
{
    public static class Settings
    {
        private const string K_VOLUME = "settings_volume";
        private const string K_MUSIC_VOLUME = "settings_musicVolume";
        private const string K_SFX_VOLUME = "settings_sfxVolume";
        private const string K_CINEMATIC_VOLUME = "settings_cinematicVolume";
        private const string K_VOICE_VOLUME = "settings_voiceVolume";
        private const string K_MOUSE_SENS = "settings_mouseSens";
        private const string K_GAMEPAD_SENS = "settings_gamepadSens";
        private const string K_INVERT_Y = "settings_invertY";
        private const string K_BRIGHTNESS = "settings_brightness";
        private const string K_CALIDAD = "settings_calidad";

        public const float DEFAULT_VOLUME = 1.0f;
        public const float DEFAULT_MUSIC_VOLUME = 0.85f;
        public const float DEFAULT_SFX_VOLUME = 1.0f;
        public const float DEFAULT_CINEMATIC_VOLUME = 1.0f;
        public const float DEFAULT_VOICE_VOLUME = 1.0f;
        public const float DEFAULT_MOUSE_SENS = 0.25f;
        public const float DEFAULT_GAMEPAD_SENS = 240f;
        public const bool DEFAULT_INVERT_Y = false;
        public const float DEFAULT_BRIGHTNESS = 1.0f;

        public const float MOUSE_SENS_MIN = 0.05f;
        public const float MOUSE_SENS_MAX = 0.6f;
        public const float GAMEPAD_SENS_MIN = 60f;
        public const float GAMEPAD_SENS_MAX = 450f;
        public const float BRIGHTNESS_MIN = 0.5f;
        public const float BRIGHTNESS_MAX = 1.6f;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(K_VOLUME, DEFAULT_VOLUME);
            set
            {
                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(K_VOLUME, value);
                PlayerPrefs.Save();
                AudioListener.volume = value;
            }
        }

        // Volumenes por categoria. La integracion con AudioMixer se hace cuando Carlos arme el pipeline de audio.
        // Hoy solo persisten en PlayerPrefs.
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(K_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
            set { value = Mathf.Clamp01(value); PlayerPrefs.SetFloat(K_MUSIC_VOLUME, value); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(K_SFX_VOLUME, DEFAULT_SFX_VOLUME);
            set { value = Mathf.Clamp01(value); PlayerPrefs.SetFloat(K_SFX_VOLUME, value); PlayerPrefs.Save(); }
        }

        public static float CinematicVolume
        {
            get => PlayerPrefs.GetFloat(K_CINEMATIC_VOLUME, DEFAULT_CINEMATIC_VOLUME);
            set { value = Mathf.Clamp01(value); PlayerPrefs.SetFloat(K_CINEMATIC_VOLUME, value); PlayerPrefs.Save(); }
        }

        public static float VoiceVolume
        {
            get => PlayerPrefs.GetFloat(K_VOICE_VOLUME, DEFAULT_VOICE_VOLUME);
            set { value = Mathf.Clamp01(value); PlayerPrefs.SetFloat(K_VOICE_VOLUME, value); PlayerPrefs.Save(); }
        }

        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(K_MOUSE_SENS, DEFAULT_MOUSE_SENS);
            set
            {
                value = Mathf.Clamp(value, MOUSE_SENS_MIN, MOUSE_SENS_MAX);
                PlayerPrefs.SetFloat(K_MOUSE_SENS, value);
                PlayerPrefs.Save();
            }
        }

        public static float GamepadSensitivity
        {
            get => PlayerPrefs.GetFloat(K_GAMEPAD_SENS, DEFAULT_GAMEPAD_SENS);
            set
            {
                value = Mathf.Clamp(value, GAMEPAD_SENS_MIN, GAMEPAD_SENS_MAX);
                PlayerPrefs.SetFloat(K_GAMEPAD_SENS, value);
                PlayerPrefs.Save();
            }
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(K_INVERT_Y, DEFAULT_INVERT_Y ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(K_INVERT_Y, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static float Brightness
        {
            get => PlayerPrefs.GetFloat(K_BRIGHTNESS, DEFAULT_BRIGHTNESS);
            set
            {
                value = Mathf.Clamp(value, BRIGHTNESS_MIN, BRIGHTNESS_MAX);
                PlayerPrefs.SetFloat(K_BRIGHTNESS, value);
                PlayerPrefs.Save();
                ApplyBrightness();
            }
        }

        /// <summary>
        /// Calidad gráfica = nivel de QualitySettings (cada uno con su asset de HDRP): 0 Alta (High Fidelity),
        /// 1 Media (Balanced), 2 Baja (Performant). Con tarjetas de 4 GB o menos (GTX 1650, RTX 3050 4 GB) empieza en Media.
        /// </summary>
        public static int CalidadPorDefecto => SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize <= 4600 ? 1 : 0;
        public static int CalidadMax => Mathf.Max(0, QualitySettings.names.Length - 1);

        public static int Calidad
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(K_CALIDAD, CalidadPorDefecto), 0, CalidadMax);
            set
            {
                PlayerPrefs.SetInt(K_CALIDAD, Mathf.Clamp(value, 0, CalidadMax));
                PlayerPrefs.Save();
                ApplyCalidad();
            }
        }

        public static string NombreCalidad(int i) => i <= 0 ? "Alta" : i == 1 ? "Media" : "Baja";

        private static void ApplyCalidad()
        {
            if (QualitySettings.names.Length == 0) return;
            int q = Calidad;
            if (QualitySettings.GetQualityLevel() != q) QualitySettings.SetQualityLevel(q, true);
        }

        /// <summary>Aplica todos los settings activos. Llamar al iniciar cada escena.</summary>
        public static void ApplyAll()
        {
            AudioListener.volume = MasterVolume;
            ApplyBrightness();
            ApplyCalidad();
        }

        /// <summary>Restaura todos los settings a sus valores por defecto.</summary>
        public static void ResetToDefaults()
        {
            MasterVolume = DEFAULT_VOLUME;
            MusicVolume = DEFAULT_MUSIC_VOLUME;
            SfxVolume = DEFAULT_SFX_VOLUME;
            CinematicVolume = DEFAULT_CINEMATIC_VOLUME;
            VoiceVolume = DEFAULT_VOICE_VOLUME;
            MouseSensitivity = DEFAULT_MOUSE_SENS;
            GamepadSensitivity = DEFAULT_GAMEPAD_SENS;
            InvertY = DEFAULT_INVERT_Y;
            Brightness = DEFAULT_BRIGHTNESS;
            Calidad = CalidadPorDefecto;
        }

        /// <summary>Aplica el brillo via RenderSettings.ambientIntensity (afecta luz ambiental).
        /// En HDRP esto es limitado pero algo cambia. Para post-process completo se requiere acceso al Volume Profile.</summary>
        private static void ApplyBrightness()
        {
            // Multiplicador del ambiente. 1 = default, 0.5 = mas oscuro, 1.6 = mas claro.
            RenderSettings.ambientIntensity = Brightness;
        }
    }
}
