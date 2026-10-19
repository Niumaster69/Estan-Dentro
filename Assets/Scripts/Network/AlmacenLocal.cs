using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EstanDentro.Network
{
    /// <summary>
    /// Guardado LOCAL con las mismas tablas que tenía la API de somee (Jugadores, Partidas, Logros, LogroXPartida).
    /// Vive en un archivo JSON en la carpeta de datos del juego (Application.persistentDataPath/estan_dentro_datos.json).
    /// ApiClient lo usa mientras no haya una URL de nube configurada (ver StreamingAssets/nube.json).
    /// </summary>
    public static class AlmacenLocal
    {
        [Serializable]
        class Datos
        {
            public int sigJugador = 1, sigPartida = 1;
            public List<JugadorDto> jugadores = new List<JugadorDto>();
            public List<PartidaDto> partidas = new List<PartidaDto>();
            public List<LogroXPartidaDto> logrosXPartida = new List<LogroXPartidaDto>();
        }

        // mismo catálogo que estaba sembrado en la base de datos (Document/Flujo_API_Estan_Dentro.md)
        static readonly LogroDto[] Catalogo =
        {
            new LogroDto { idLogro = 2, codigo = "cerradura_primera", nombreLogro = "Maestro de cerraduras", descripcion = "Resuelve una cerradura sin equivocarte", idTemas = 2, puntos = 30 },
            new LogroDto { idLogro = 3, codigo = "notas_completas", nombreLogro = "Lector compulsivo", descripcion = "Lee todas las notas del aula", idTemas = 2, puntos = 20 },
            new LogroDto { idLogro = 4, codigo = "respiracion_zen", nombreLogro = "Aire en orden", descripcion = "Completa el capítulo sin fallar ningún ciclo de respiración", idTemas = 3, puntos = 50 },
            new LogroDto { idLogro = 5, codigo = "superviviente", nombreLogro = "Mente fría", descripcion = "Termina el capítulo sin colapsar de estrés", idTemas = 3, puntos = 50 },
            new LogroDto { idLogro = 6, codigo = "sigilo_observer", nombreLogro = "Sin que te vean", descripcion = "Termina el capítulo sin disparar al Observador", idTemas = 4, puntos = 40 },
        };

        static Datos datos;
        public static string Ruta => Path.Combine(Application.persistentDataPath, "estan_dentro_datos.json");

        static Datos D
        {
            get
            {
                if (datos != null) return datos;
                try { datos = File.Exists(Ruta) ? JsonUtility.FromJson<Datos>(File.ReadAllText(Ruta)) : null; }
                catch (Exception e) { Debug.LogWarning("[AlmacenLocal] no se pudo leer, empiezo vacío: " + e.Message); }
                return datos ??= new Datos();
            }
        }

        static void Guardar()
        {
            try
            {
                string tmp = Ruta + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(D, true));
                if (File.Exists(Ruta)) File.Delete(Ruta);
                File.Move(tmp, Ruta);                                  // escribir y renombrar: nunca queda a medias
            }
            catch (Exception e) { Debug.LogWarning("[AlmacenLocal] no se pudo guardar: " + e.Message); }
        }

        static string Ahora() => DateTime.UtcNow.ToString("o");
        static T Copia<T>(T x) => JsonUtility.FromJson<T>(JsonUtility.ToJson(x));

        // ---------- Jugador ----------
        public static JugadorDto CrearJugador(JugadorCreateDto c)
        {
            var j = new JugadorDto { idJugador = D.sigJugador++, usuario = c.usuario, nombres = c.nombres, apellido = c.apellido, fechaCreacion = Ahora() };
            D.jugadores.Add(j); Guardar();
            return Copia(j);
        }
        public static JugadorDto Jugador(int id) { var j = D.jugadores.FirstOrDefault(x => x.idJugador == id); return j == null ? null : Copia(j); }
        public static bool ActualizarJugador(JugadorDto j)
        {
            int i = D.jugadores.FindIndex(x => x.idJugador == j.idJugador);
            if (i < 0) return false;
            D.jugadores[i] = Copia(j); Guardar(); return true;
        }
        public static JugadorDto[] Jugadores() => D.jugadores.Select(Copia).ToArray();

        // ---------- Partida ----------
        public static PartidaDto CrearPartida(PartidaCreateDto c)
        {
            var p = new PartidaDto { idPartida = D.sigPartida++, idJugador = c.idJugador, nombrePartida = c.nombrePartida,
                                     fechaInicio = string.IsNullOrEmpty(c.fechaInicio) ? Ahora() : c.fechaInicio,
                                     estado = c.estado, capituloAlcanzado = c.capituloAlcanzado, tiempoSegundos = c.tiempoSegundos };
            D.partidas.Add(p); Guardar();
            return Copia(p);
        }
        public static bool ActualizarPartida(PartidaDto p)
        {
            int i = D.partidas.FindIndex(x => x.idPartida == p.idPartida);
            if (i < 0) return false;
            D.partidas[i] = Copia(p); Guardar(); return true;
        }
        public static PartidaDto[] Partidas() => D.partidas.Select(Copia).ToArray();
        public static PartidaDto Partida(int id) { var p = D.partidas.FirstOrDefault(x => x.idPartida == id); return p == null ? null : Copia(p); }
        public static bool BorrarPartida(int id)
        {
            int n = D.partidas.RemoveAll(x => x.idPartida == id);
            D.logrosXPartida.RemoveAll(x => x.idPartida == id);
            if (n > 0) Guardar();
            return n > 0;
        }

        // ---------- Logros ----------
        public static LogroDto[] Logros() => Catalogo.Select(Copia).ToArray();
        public static void DesbloquearLogro(int idPartida, int idLogro)
        {
            if (D.logrosXPartida.Any(x => x.idPartida == idPartida && x.idLogro == idLogro)) return;   // como la llave primaria
            D.logrosXPartida.Add(new LogroXPartidaDto { idPartida = idPartida, idLogro = idLogro, fechaDesbloqueo = Ahora() });
            Guardar();
        }
        public static LogroXPartidaDto[] LogrosXPartida() => D.logrosXPartida.Select(Copia).ToArray();
    }
}
