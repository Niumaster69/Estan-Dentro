using System;
using System.Collections;
using UnityEngine;

namespace EstanDentro.Network
{
    // Orquesta el cierre de un capitulo:
    //   1) Evalua y desbloquea los 3 logros que requieren llegar al final sin fallar
    //      (solo si completado=true).
    //   2) PUT /api/Partidas con el estado final (Completada o Abandonada) + tiempo total.
    //
    // El overlay de resumen (EndOfChapterOverlay) lo abre el caller despues de esta coroutine,
    // para que decida que callbacks meter en los botones segun el contexto.
    //
    // Best-effort: si la API no responde, log warning y seguir. Nunca bloquear UI.
    public static class ChapterFlow
    {
        public static IEnumerator EndChapter(bool completado)
        {
            if (completado)
            {
                if (!GameSession.ObserverTriggeredAtLeastOnce)
                    GameSession.TryUnlockLogro("sigilo_observer");
                if (GameSession.BreathingFailedCycles == 0)
                    GameSession.TryUnlockLogro("respiracion_zen");
                if (!GameSession.StressCollapsed)
                    GameSession.TryUnlockLogro("superviviente");
            }

            yield return PutPartidaBestEffort(completado);
        }

        private static IEnumerator PutPartidaBestEffort(bool completado)
        {
            if (!GameSession.IsOnline)
            {
                Debug.Log("[ChapterFlow] Skip PUT partida: sin IdJugador (offline).");
                yield break;
            }
            if (!GameSession.HasActivePartida)
            {
                Debug.LogWarning("[ChapterFlow] Skip PUT partida: no hay partida activa.");
                yield break;
            }

            DateTime now = DateTime.UtcNow;
            int tiempoSegundos = Mathf.Max(0, (int)(now - GameSession.PartidaStartTime).TotalSeconds);
            byte estadoFinal = (byte)(completado ? 1 : 2); // 1=Completada, 2=Abandonada

            var data = new PartidaDto
            {
                idPartida = GameSession.CurrentPartidaId,
                idJugador = GameSession.CurrentJugadorId,
                nombrePartida = "Partida " + GameSession.PartidaStartTime.ToString("yyyy-MM-dd HH:mm"),
                fechaInicio = GameSession.PartidaStartTime.ToString("o"),
                fechaFin = now.ToString("o"),
                estado = estadoFinal,
                capituloAlcanzado = 1,
                tiempoSegundos = tiempoSegundos
            };

            bool finished = false;
            ApiClient.Instance.UpdatePartida(data,
                () => { Debug.Log($"[ChapterFlow] Partida cerrada en API (estado={estadoFinal}, t={tiempoSegundos}s)"); finished = true; },
                err => { Debug.LogWarning($"[ChapterFlow] Fallo PUT partida: {err}"); finished = true; }
            );

            float waited = 0f;
            while (!finished && waited < 6f) { waited += Time.unscaledDeltaTime; yield return null; }
        }
    }
}
