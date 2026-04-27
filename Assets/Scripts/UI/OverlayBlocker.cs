namespace EstanDentro.UI
{
    /// <summary>
    /// Registry estatico de overlays modales activos.
    /// Cada overlay (NoteOverlay, LockOverlay, MicCalibration, GameOverHandler...) llama
    /// Register cuando se abre y Unregister cuando se cierra. PauseMenuHandler consulta
    /// IsBlocking antes de abrirse para no superponerse.
    /// </summary>
    public static class OverlayBlocker
    {
        private static int count;
        public static bool IsBlocking => count > 0;
        public static int Count => count;

        public static void Register() { count++; }

        public static void Unregister()
        {
            count = System.Math.Max(0, count - 1);
        }

        public static void Reset() { count = 0; }
    }
}
