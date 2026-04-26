using UnityEngine;

namespace EstanDentro.UI
{
    public static class Settings
    {
        private const string K_VOLUME = "settings_volume";
        private const string K_MOUSE_SENS = "settings_mouseSens";
        private const string K_GAMEPAD_SENS = "settings_gamepadSens";
        private const string K_INVERT_Y = "settings_invertY";

        public const float DEFAULT_VOLUME = 1.0f;
        public const float DEFAULT_MOUSE_SENS = 0.25f;
        public const float DEFAULT_GAMEPAD_SENS = 240f;
        public const bool DEFAULT_INVERT_Y = false;

        public const float MOUSE_SENS_MIN = 0.05f;
        public const float MOUSE_SENS_MAX = 0.6f;
        public const float GAMEPAD_SENS_MIN = 60f;
        public const float GAMEPAD_SENS_MAX = 450f;

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

        /// <summary>Aplica todos los settings activos. Llamar al iniciar cada escena.</summary>
        public static void ApplyAll()
        {
            AudioListener.volume = MasterVolume;
        }
    }
}
