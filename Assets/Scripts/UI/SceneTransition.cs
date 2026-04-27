using UnityEngine;
using UnityEngine.SceneManagement;

namespace EstanDentro.UI
{
    /// <summary>
    /// Helper estatico para cambiar de escena con pantalla de carga.
    /// Auto-crea LoadingScreenController si no existe.
    /// </summary>
    public static class SceneTransition
    {
        public const float DEFAULT_MIN_DISPLAY = 1.5f;

        /// <summary>Carga una escena con loading screen + tip rotado del default. minDisplay es el tiempo minimo que la pantalla esta visible.</summary>
        public static void LoadScene(string sceneName, float minDisplay = DEFAULT_MIN_DISPLAY, string tip = null)
        {
            // Asegurar que el tiempo este en 1 (por si veniamos de un overlay pausado)
            Time.timeScale = 1f;

            var loader = LoadingScreenController.GetOrCreate();
            loader.StartCoroutine(loader.LoadSceneRoutine(sceneName, minDisplay, tip));
        }

        /// <summary>Recarga la escena actual (util para Game Over).</summary>
        public static void Reload(float minDisplay = DEFAULT_MIN_DISPLAY, string tip = null)
        {
            string current = SceneManager.GetActiveScene().name;
            LoadScene(current, minDisplay, tip);
        }
    }
}
