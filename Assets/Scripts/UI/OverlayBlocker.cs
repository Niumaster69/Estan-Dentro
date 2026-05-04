namespace EstanDentro.UI
{
    /// <summary>
    /// Registry estatico de overlays modales activos.
    /// Cada overlay (NoteOverlay, LockOverlay, MicCalibration, GameOverHandler...) llama
    /// Register cuando se abre y Unregister cuando se cierra. PauseMenuHandler consulta
    /// IsBlocking antes de abrirse para no superponerse.
    ///
    /// Tambien expone WasJustDismissed: true si en este frame se acaba de cerrar un overlay,
    /// para que overlays inferiores no procesen el mismo Esc/Cross y se cierren en cadena.
    /// </summary>
    public static class OverlayBlocker
    {
        private static int count;
        private static int lastDismissFrame = -1;

        public static bool IsBlocking => count > 0;
        public static int Count => count;
        public static bool WasJustDismissed => lastDismissFrame == UnityEngine.Time.frameCount;

        public static void Register() { count++; }

        public static void Unregister()
        {
            count = System.Math.Max(0, count - 1);
            lastDismissFrame = UnityEngine.Time.frameCount;
        }

        public static void Reset() { count = 0; lastDismissFrame = -1; }
    }
}
