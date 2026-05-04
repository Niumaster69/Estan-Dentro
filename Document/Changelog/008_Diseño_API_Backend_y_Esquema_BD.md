# 008 - Diseno API Backend y esquema de Base de Datos

**Fecha:** 2026-05-02 (sabado)
**Sprint:** Sprint 3 - integracion API + persistencia de jugadores y logros
**Responsable:** Duvan Lozano + Claude
**Proyecto API:** `C:\Users\duvan\OneDrive\Escritorio\Universidad\ApiJuego\` (proyecto hermano, fuera del repo de Unity)
**Servidor BD:** somee.com (catalog `juegoNuevo`, host `juegoNuevo.mssql.somee.com`)
**Tarea relacionada:** integracion futura del juego con backend para guardar partidas y desbloqueo de logros

---

## Que se hizo

Sesion de **diseno**, no de implementacion. Se reviso el estado actual del proyecto API (creado dias antes) y se tomaron decisiones de arquitectura y modelado de datos antes de escribir codigo nuevo.

### Estado de partida (lo que existia antes de esta sesion)

- ASP.NET Core 8 + Entity Framework Core 9 + SQL Server (somee).
- Solo 2 tablas creadas: `Temas` y `Logros` (1 migration aplicada: `20260428023053_initial`).
- Solo `TemasController` con CRUD basico estilo scaffold.
- Swagger habilitado como UI de pruebas.
- Connection string a somee con credenciales en plano dentro de `appsettings.json` (asumido OK porque el repo de la API no se va a subir publicamente).
- Esquema de ejemplo dado por el profesor:
  - JUGADOR (Id, Nombres, Apellido, Correo, Contrasena)
  - PARTIDAS (Id, Partida, fechahora, idjugador FK)
  - LOGROS (Id, logro, idtema FK)
  - LOGROXPARTIDA (idpartida + idlogro PK compuesta)
  - TEMAS (Id, tema)

### Decisiones de diseno tomadas

#### 1. Flow de autenticacion: B (Solo `Usuario`, sin contrasena, sin correo)

El juego es un ejecutable de escritorio sin servicio de email asociado. Pedirle al jugador correo + contrasena es friccion innecesaria para un proyecto academico local.

**Flujo elegido:**
1. Jugador abre `EstanDentro.exe`.
2. Menu principal pide solo `Usuario` (mas opcionalmente Nombres, Apellido la primera vez).
3. Si el usuario existe en BD -> entra como ese jugador.
4. Si no existe -> se crea el `Jugador` y entra.
5. Las partidas y logros desbloqueados quedan asociados a ese `IdJugador`.

**Justificacion para el profesor:**
> Decidimos eliminar `Correo` y `Contrasena` del modelo `Jugador` porque la aplicacion es un ejecutable de escritorio sin servicio de email (no hay flujo de recuperacion de password) y porque agregar contrasena obligaria a manejar hashing/seguridad sin beneficio pedagogico real. `Usuario` con indice unico cumple el rol de identificador del jugador. Esto reduce friccion de onboarding y mantiene el modelo relacional intacto (FKs hacia Partidas y LogroXPartida funcionan igual). Si en el futuro se requiere autenticacion real, agregar `PasswordHash` es una migration adicional sin romper datos existentes.

#### 2. Cliente Unity: descartado por ahora

Swagger sirve como cliente para testing. Cuando llegue el momento de consumir la API desde Unity (post entrega-base), se hara con `UnityWebRequest` directo en `Assets/Scripts/Network/`. No se disena cliente separado ni libreria compartida.

#### 3. Estilo de codigo: replicar al 100% al profesor

Todo codigo nuevo en el proyecto API debe parecer escrito por la misma persona que escribio `TemasController.cs`, `AppDbContext.cs`, `Temas.cs` y `Logro.cs`. Patrones a respetar:

- Modelos POCO simples sin data annotations (`[Key]`, `[Required]`, etc.)
- Configuracion en `OnModelCreating` con Fluent API (`HasKey`, `Property.IsRequired().HasMaxLength`, `HasOne/WithMany/HasForeignKey`)
- `[JsonIgnore]` en navegaciones tipo coleccion para evitar ciclos JSON
- `OnDelete(DeleteBehavior.Restrict)` en FKs
- Migrations con `dotnet ef migrations add` (no escritas a mano)
- Controllers tipo "scaffold" estilo `TemasController`
- Namespace `WebApplication1.Controllers.Models` para modelos (raro pero es lo del profe; cambiara a `EstanDentro.Api.Controllers.Models` tras el rebrand pendiente)

**No introducir** sin permiso explicito: `IEntityTypeConfiguration<T>` separado, repositorios, Unit of Work, AutoMapper, DTOs para CRUD basico, mover archivos a carpetas mas convencionales como `Data/` o `Models/` raiz.

#### 4. Esquema final de la BD

```
JUGADOR
  IdJugador       int identity, PK
  Usuario         nvarchar(30) UNIQUE NOT NULL    <- identificador login
  Nombres         nvarchar(50) NOT NULL
  Apellido        nvarchar(50) NOT NULL
  FechaCreacion   datetime2 default GETUTCDATE()

TEMAS  (ya existe)
  IdTemas         int identity, PK
  NombreTemas     nvarchar(50) NOT NULL

LOGROS  (ya existe, se agregaran campos)
  IdLogro         int identity, PK
  NombreLogro     nvarchar(100) NOT NULL
  IdTemas         int FK -> TEMAS, RESTRICT
  Codigo          nvarchar(50) UNIQUE NOT NULL    <- NUEVO. Codigo estable que el cliente envia ("sigilo_perfecto")
  Descripcion     nvarchar(200) NULL              <- NUEVO. Para mostrar como desbloquearlo
  Puntos          int default 10                  <- NUEVO. Gamificacion

PARTIDAS
  IdPartida          int identity, PK
  IdJugador          int FK -> JUGADOR, RESTRICT
  NombrePartida      nvarchar(100) NULL
  FechaInicio        datetime2 NOT NULL           <- renombrado de "fechahora"
  FechaFin           datetime2 NULL               <- NUEVO. NULL = en curso
  Estado             tinyint default 0            <- NUEVO. 0=EnCurso, 1=Completada, 2=Abandonada
  CapituloAlcanzado  int default 1                <- NUEVO. Para "continuar"
  TiempoSegundos     int default 0                <- NUEVO. Acumulado de juego

LOGROXPARTIDA  (mantenemos nombre del profe)
  IdPartida        int FK -> PARTIDAS    \  PK compuesta
  IdLogro          int FK -> LOGROS      /
  FechaDesbloqueo  datetime2 default GETUTCDATE()  <- NUEVO. Momento exacto del desbloqueo
```

**Trade-off conocido de `LogroXPartida`:** si un jugador se pasa el juego varias veces, el mismo logro aparece varias veces (uno por partida en que lo desbloqueo). Para mostrar "logros unicos del jugador" se requiere `GROUP BY IdLogro`. Aceptable porque el profe pidio esta tabla por nombre, y el `FechaDesbloqueo` permite mostrar el primer desbloqueo si se necesita.

### Pendientes de esta sesion (para sesiones siguientes)

1. **Rebrand del proyecto API**: `WebApplication1` -> `EstanDentro.Api`. Renombrar archivos, namespaces, .sln, .csproj.
2. **Fase 1 - Modelos + migration**: crear `Jugador.cs`, `Partida.cs`, `LogroXPartida.cs`, refinar `Logro.cs`, actualizar `AppDbContext`, generar migration `AddJugadoresPartidas`.
3. **Fase 2 - Aplicar migration en SQL Server local** con guia paso a paso para Duvan (SSMS).
4. **Fase 3 - Controllers**: `JugadoresController`, `PartidasController`, `LogroXPartidaController` (estilo `TemasController`).
5. **Fase 4 - Replicar en somee**: aplicar la migration al servidor remoto.
6. **Fase 5 (post entrega base)** - Cliente Unity: `Assets/Scripts/Network/ApiClient.cs` con `UnityWebRequest`, UI de "ingresar usuario" en menu principal, integracion con sistemas existentes para registrar partidas y logros.

---

## Implementacion completada (2026-05-02)

Despues de la sesion de diseno, se implemento todo el plan en la misma fecha:

### Fases ejecutadas
- Rebrand: `WebApplication1` -> `EstanDentro.Api` (carpetas, .sln, .csproj, namespaces, eliminados templates `WeatherForecast*`)
- Modelos creados siguiendo estilo profe (POCOs, sin annotations): `Jugador`, `Partida`, `LogroXPartida`
- `Logro` extendido: `Codigo` (unico), `Descripcion`, `Puntos`
- `AppDbContext` actualizado: 3 DbSets nuevos + Fluent API completa (HasKey compuesto en LogroXPartida, indices unicos en Usuario y Codigo, defaults `GETUTCDATE()` en FechaCreacion y FechaDesbloqueo)
- Migration `20260502205627_AddJugadoresPartidas` generada con `dotnet ef migrations add` y aplicada con `dotnet ef database update` desde Package Manager Console de Visual Studio
- 4 controllers nuevos estilo `TemasController` scaffold: `JugadoresController`, `PartidasController`, `LogrosController`, `LogroXPartidaController` (este ultimo con rutas `{idPartida}/{idLogro}` por la PK compuesta)
- Bug fix: navegaciones "uno" marcadas como nullable (`Temas?`, `Jugador?`, `Partida?`, `Logro?`) para evitar 400 de auto-validacion de `[ApiController]` con `<Nullable>enable</Nullable>`. Justificacion: consistente con el patron del profe que ya usa `?` en navegaciones colecciones (`ICollection<Logro>? Logros` en Temas.cs).
- Deploy en somee: API publica en `http://estandentro.somee.com/swagger`

### Flujo end-to-end probado en Swagger

1. `POST /api/Temas` -> tema "Sigilo" (idTemas=1)
2. `POST /api/Logros` -> "Sin ser detectado" (idTemas=1, codigo `sigilo_perfecto`, puntos 50)
3. `POST /api/Jugadores` -> "Duvancho" / "Duvan Lozano" (FechaCreacion auto via GETUTCDATE)
4. `POST /api/Partidas` -> idJugador=1, FechaInicio explicita
5. `POST /api/LogroXPartida` -> idPartida=1, idLogro=1 (FechaDesbloqueo auto)
6. GETs de cada endpoint devuelven los datos correctamente
7. URL publica `http://estandentro.somee.com/api/Jugadores` devuelve los mismos datos -> confirma BD compartida (siempre fue somee)

### URLs y rutas

- **Swagger publico**: `http://estandentro.somee.com/swagger/index.html`
- **Endpoints**: `http://estandentro.somee.com/api/{Temas|Logros|Jugadores|Partidas|LogroXPartida}`
- **BD**: `juegoNuevo.mssql.somee.com` catalog `juegoNuevo` (sin cambios desde el diseno inicial)

### Caveat con Swagger UI

El example body que genera Swagger incluye TODOS los campos del modelo (incluidas navegaciones y campos auto-generados). Antes de Execute hay que **borrar**:
- Los `id*` (los genera la BD via Identity)
- Los objetos anidados (`temas`, `jugador`, `partida`, `logro`, etc.) — son nav properties, no van en el body
- Los campos con default SQL: `fechaCreacion`, `fechaDesbloqueo`

Ejemplo limpio para `POST /api/Logros`:
```json
{ "nombreLogro": "...", "idTemas": 1, "codigo": "...", "descripcion": "...", "puntos": 50 }
```

### Pendientes para sesiones futuras

- **Fase 5**: integrar API con Unity (ApiClient + UI "ingresar usuario" en menu principal del juego + enganchar sistemas existentes con `POST /api/Partidas` al inicio y `POST /api/LogroXPartida` al desbloquear)
- **HTTPS en somee**: la URL actual es HTTP. Unity 2022+ bloquea HTTP en builds por defecto, hay que evaluar si somee gratis soporta SSL o ajustar Player Settings
- **Cleanup warnings Unity**: 3 warnings pendientes desde antes de esta sesion (CS0414 en PlayerWakeUp, material EscritorioMadera duplicado, Pared self-intersecting en Estructura v2.fbx)

---

## Para la guia de entrega final

Esta seccion sirve como insumo cuando se redacte el documento de entrega del proyecto API. Puntos clave a incluir:

- **Justificacion de eliminar `Correo` y `Contrasena`** (ver seccion "Flow B" arriba). Defendible academicamente.
- **Justificacion de campos nuevos en `LOGROS` y `PARTIDAS`** (Codigo, Descripcion, Puntos, Estado, CapituloAlcanzado, TiempoSegundos): permiten estadisticas de juego reales y desacoplan el cliente de los IDs de BD.
- **Justificacion de mantener `LogroXPartida`** (en vez de `LogroXJugador`): respeta el esquema del profesor y permite trazar en que partida se desbloqueo cada logro via `FechaDesbloqueo`.
- **Razon de no usar autenticacion**: alcance academico, ejecutable local, sin servicio de email, sin necesidad pedagogica de demostrar JWT.
