using System;

namespace EstanDentro.Network
{
    // Mapean 1:1 a los modelos del API EstanDentro.Api.
    // Compatibles con UnityEngine.JsonUtility (sin propiedades, solo campos publicos).

    [Serializable]
    public class JugadorDto
    {
        public int idJugador;
        public string usuario;
        public string nombres;
        public string apellido;
        public string fechaCreacion;
    }

    [Serializable]
    public class PartidaDto
    {
        public int idPartida;
        public int idJugador;
        public string nombrePartida;
        public string fechaInicio;
        public string fechaFin;
        public byte estado;
        public int capituloAlcanzado;
        public int tiempoSegundos;
    }

    [Serializable]
    public class LogroDto
    {
        public int idLogro;
        public string nombreLogro;
        public int idTemas;
        public string codigo;
        public string descripcion;
        public int puntos;
    }

    [Serializable]
    public class LogroXPartidaDto
    {
        public int idPartida;
        public int idLogro;
        public string fechaDesbloqueo;
    }

    // JsonUtility no soporta arrays JSON como root. Wrappers para deserializar listas.
    [Serializable] public class JugadorListWrapper { public JugadorDto[] items; }
    [Serializable] public class PartidaListWrapper { public PartidaDto[] items; }
    [Serializable] public class LogroListWrapper { public LogroDto[] items; }
    [Serializable] public class LogroXPartidaListWrapper { public LogroXPartidaDto[] items; }

    // ----- Create DTOs (solo los campos que el cliente envia en POST) -----
    // Razon: JsonUtility serializa TODOS los campos del DTO. Si envio "fechaCreacion": "" el API responde
    // 400 porque no puede parsear "" como DateTime. Estos DTOs omiten los campos auto-generados por la BD
    // (IdX por identity, FechaCreacion / FechaDesbloqueo por GETUTCDATE() default).

    [Serializable]
    public class JugadorCreateDto
    {
        public string usuario;
        public string nombres;
        public string apellido;
    }

    [Serializable]
    public class PartidaCreateDto
    {
        public int idJugador;
        public string nombrePartida;
        public string fechaInicio;   // ISO 8601 UTC
        public byte estado;
        public int capituloAlcanzado;
        public int tiempoSegundos;
    }

    [Serializable]
    public class LogroXPartidaCreateDto
    {
        public int idPartida;
        public int idLogro;
    }
}
