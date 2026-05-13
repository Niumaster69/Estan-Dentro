using System;
using System.Collections.Generic;
using UnityEngine;

namespace EstanDentro.Network
{
    // Estado de la sesion. Vive en memoria. Solo el perfil (IdJugador, Usuario, Nombres) se persiste en PlayerPrefs.
    // Detalle en Document/Flujo_API_Estan_Dentro.md.
    public static class GameSession
    {
        private const string PrefKeyJugadorId = "ED_JugadorId";
        private const string PrefKeyUsuario = "ED_Usuario";
        private const string PrefKeyNombres = "ED_Nombres";

        // Perfil persistente
        public static int CurrentJugadorId;
        public static string CurrentUsuario = "";
        public static string CurrentNombres = "";

        // Por partida (memoria)
        public static int CurrentPartidaId;
        public static DateTime PartidaStartTime;
        public static readonly HashSet<int> UnlockedLogrosThisPartida = new();

        // Catalogo precargado al arrancar
        public static readonly Dictionary<string, int> LogroIdByCodigo = new();
        public static readonly Dictionary<int, LogroDto> LogroByIdCache = new();

        // Contadores que evaluan logros al fin de capitulo
        public static int BreathingFailedCycles;
        public static bool ObserverTriggeredAtLeastOnce;
        public static bool StressCollapsed;

        // Id del PlayerSpawnPoint donde debe aparecer el player al cargar la siguiente escena.
        // Lo setea quien dispara un cambio de escena (ej. ExitDuctsTrigger) y lo consume PlayerSpawner.Start().
        // Vacio = usar el SpawnPoint marcado como default. Se limpia tras consumirse.
        public static string NextSpawnPointId = "";

        // Modo offline = no se pudo crear/recuperar el jugador en BD
        public static bool IsOnline => CurrentJugadorId != 0;
        public static bool HasActivePartida => CurrentPartidaId != 0;
        public static bool HasCatalog => LogroIdByCodigo.Count > 0;

        public static void LoadFromPlayerPrefs()
        {
            CurrentJugadorId = PlayerPrefs.GetInt(PrefKeyJugadorId, 0);
            CurrentUsuario = PlayerPrefs.GetString(PrefKeyUsuario, "");
            CurrentNombres = PlayerPrefs.GetString(PrefKeyNombres, "");
        }

        public static void SaveProfileToPlayerPrefs(int idJugador, string usuario, string nombres)
        {
            CurrentJugadorId = idJugador;
            CurrentUsuario = usuario;
            CurrentNombres = nombres;
            PlayerPrefs.SetInt(PrefKeyJugadorId, idJugador);
            PlayerPrefs.SetString(PrefKeyUsuario, usuario);
            PlayerPrefs.SetString(PrefKeyNombres, nombres);
            PlayerPrefs.Save();
        }

        public static void UpdateNombres(string nuevoNombres)
        {
            CurrentNombres = nuevoNombres;
            PlayerPrefs.SetString(PrefKeyNombres, nuevoNombres);
            PlayerPrefs.Save();
        }

        public static void ResetForNewPartida()
        {
            CurrentPartidaId = 0;
            PartidaStartTime = DateTime.UtcNow;
            UnlockedLogrosThisPartida.Clear();
            BreathingFailedCycles = 0;
            ObserverTriggeredAtLeastOnce = false;
            StressCollapsed = false;
        }

        // Para retomar una partida vieja: setea el idPartida y resetea contadores,
        // pero NO crea partida nueva. Los logros que se desbloqueen quedan asociados al idPartida viejo.
        // Nota academica: el juego no guarda checkpoints; al retomar se rejuega el capituloAlcanzado
        // desde el principio. La utilidad de "Retomar" es preservar el historico de la partida
        // (su idPartida, fechaInicio, tiempoSegundos previos) en BD.
        public static void ResumePartida(int idPartida, DateTime fechaInicioOriginal)
        {
            CurrentPartidaId = idPartida;
            PartidaStartTime = fechaInicioOriginal;
            UnlockedLogrosThisPartida.Clear();
            BreathingFailedCycles = 0;
            ObserverTriggeredAtLeastOnce = false;
            StressCollapsed = false;
        }

        public static bool TryGetLogroIdByCodigo(string codigo, out int idLogro)
            => LogroIdByCodigo.TryGetValue(codigo, out idLogro);

        // Helper unico para desbloquear un logro desde sistemas del juego.
        // Centraliza: verificar conexion + partida, idempotencia local, llamada API best-effort.
        // Si el jugador esta offline o el logro ya se desbloqueo en esta partida, no hace nada.
        public static void TryUnlockLogro(string codigo)
        {
            if (!IsOnline)
            {
                Debug.Log($"[Logro] Skip '{codigo}': sin IdJugador (offline).");
                return;
            }
            if (!HasActivePartida)
            {
                Debug.LogWarning($"[Logro] Skip '{codigo}': no hay partida activa (CurrentPartidaId=0).");
                return;
            }
            if (!LogroIdByCodigo.TryGetValue(codigo, out int idLogro))
            {
                Debug.LogWarning($"[Logro] Skip '{codigo}': no esta en el catalogo precargado.");
                return;
            }
            if (UnlockedLogrosThisPartida.Contains(idLogro)) return; // idempotente

            UnlockedLogrosThisPartida.Add(idLogro);
            int idPartida = CurrentPartidaId;
            EstanDentro.Network.ApiClient.Instance.UnlockLogro(
                idPartida, idLogro,
                () => Debug.Log($"[Logro] Desbloqueado: '{codigo}' (idLogro={idLogro})"),
                err => Debug.LogWarning($"[Logro] Fallo desbloquear '{codigo}': {err}")
            );
        }
    }
}
