# Flujo API ↔ Estan Dentro

Este documento responde 3 preguntas:

1. **Que servicios** (endpoints) tiene la API.
2. **Cuando** se dispara cada llamada en el juego.
3. **Por que** la llamada existe (que persiste, que valor agrega).

Si vas a tocar codigo de integracion Unity ↔ API, esta es la fuente de verdad. Para decisiones de diseno historicas ver `Changelog/008`. Para entender los sistemas del juego que disparan estos eventos ver `Arquitectura_Sistemas.md`.

**Ultima actualizacion:** 2026-05-02

---

## Proposito de la API

- **Persistir progreso real** del jugador entre sesiones del juego.
- **Demostrar BD relacional poblada** con datos del juego en vivo (entregable academico para el profesor).
- **(Futuro)** Leaderboards, perfiles publicos, estadisticas comparativas.

---

## Configuracion tecnica

| Item | Valor |
|---|---|
| Base URL produccion | `http://estandentro.somee.com` |
| Base URL dev local | `https://localhost:7177` (cuando levantas la API en VS) |
| Protocolo | **HTTP** (no HTTPS) — limitacion de somee free |
| Autenticacion | Ninguna (Flow B, ver Changelog/008) |
| Formato | JSON (Content-Type: application/json) |
| Timeout sugerido | 5 segundos |
| Encoding | UTF-8 |

⚠️ **Implicacion HTTP en Unity**: Unity 2022+ bloquea HTTP en builds por defecto.
**Solucion**: en `Edit > Project Settings > Player > Other Settings > Configuration`, poner `Allow downloads over HTTP` en `Always allowed`. Sin esto las llamadas fallan en build pero funcionan en editor.

---

## Endpoints disponibles (resumen)

Generados desde Swagger en somee. Detalle clickeable en `http://estandentro.somee.com/swagger`.

| Recurso | Verbos | Notas |
|---|---|---|
| `/api/Temas` | GET, GET{id}, POST, PUT{id}, DELETE{id} | Categorias de logros |
| `/api/Logros` | GET, GET{id}, POST, PUT{id}, DELETE{id} | Catalogo. Codigo unico requerido |
| `/api/Jugadores` | GET, GET{id}, POST, PUT{id}, DELETE{id} | Usuario unico |
| `/api/Partidas` | GET, GET{id}, POST, PUT{id}, DELETE{id} | FK a Jugador |
| `/api/LogroXPartida` | GET, GET{idP}/{idL}, POST, PUT{idP}/{idL}, DELETE{idP}/{idL} | PK compuesta. FechaDesbloqueo auto |

---

## Flujo cronologico — cuando se dispara cada cosa

Sigue el orden temporal de una sesion de juego completa.

### 0. ARRANQUE — precarga del catalogo de logros

**WHEN:** al iniciar la app, antes del primer frame del MainMenu.
**WHY:** necesitamos mapear `codigo` -> `IdLogro` para poder hacer `POST /api/LogroXPartida` con IDs correctos cuando el juego dispare un evento.

**Llamada:**
```
GET http://estandentro.somee.com/api/Logros
```

**Resultado:** array de logros. Guardar en memoria (no PlayerPrefs, queremos siempre la version mas fresca):

```csharp
public static class GameSession {
    public static Dictionary<string, int> LogroIdByCodigo = new();
    // se llena al arrancar: { "cerradura_primera": 1, "respiracion_zen": 2, ... }
}
```

**Politica de fallo:** si la API no responde, `GameSession.LogroIdByCodigo` queda vacio. El juego **funciona igual**, solo no podra registrar logros en BD durante esta sesion (warning en consola).

---

### 1. AUTO-PERFIL — primer arranque silencioso

**WHEN:** primer Awake del MainMenu, **solo si** `PlayerPrefs.GetInt("CurrentJugadorId", 0) == 0` (primera vez que corre el juego en esta instalacion).
**WHY:** necesitamos un `IdJugador` asociado a esta instalacion para registrar partidas y logros, pero **sin pedir nada al jugador** (cero friccion, cero rotura de inmersion del genero horror). Esto **NO es un login** en el sentido tradicional — es un perfil generado automaticamente que el jugador puede personalizar despues en Ajustes -> Perfil (ver seccion 5).

**Generacion del usuario** (cliente):

Por convencion: `{nombreWindows}_{guidCorto}` para tener legibilidad + uniqueness garantizada.

```csharp
string nombreWindows = Environment.UserName ?? "Player";
string guidCorto = Guid.NewGuid().ToString("N").Substring(0, 6);
string usuarioGenerado = $"{nombreWindows}_{guidCorto}";  // ej: "duvan_a3f8b9"
```

**Llamada — crear el jugador silenciosamente:**

```
POST http://estandentro.somee.com/api/Jugadores
Content-Type: application/json

{
  "usuario": "duvan_a3f8b9",
  "nombres": "duvan",
  "apellido": ""
}
```

- `usuario`: el unico tecnicamente importante (UNIQUE en BD).
- `nombres`: el nombre de Windows (display amigable que se muestra al jugador en pantalla "Perfil").
- `apellido`: vacio. El jugador puede llenarlo despues en Perfil si quiere.

**Tras la respuesta `201`:**

```csharp
PlayerPrefs.SetInt("CurrentJugadorId", response.idJugador);
PlayerPrefs.SetString("CurrentUsuario", response.usuario);
PlayerPrefs.SetString("CurrentNombres", response.nombres);
PlayerPrefs.Save();

GameSession.CurrentJugadorId = response.idJugador;
GameSession.CurrentUsuario = response.usuario;
GameSession.CurrentNombres = response.nombres;
```

**Si NO es primer arranque** (segundas/n veces):
- Leer `PlayerPrefs.GetInt("CurrentJugadorId", 0)` y poblar `GameSession`.
- **Sin llamadas a la API.**
- Sin pantallas. El jugador ve directamente el MainMenu.

**Politica de fallo:** si la API falla en el primer arranque, `CurrentJugadorId` queda en 0 (modo offline implicito). El juego es totalmente jugable; la persistencia queda apagada hasta el siguiente arranque con conexion. **No mostrar dialogo de error**, solo log warning. La proxima vez que arranque con red, se reintenta el auto-perfil.

---

### 2. INICIO DE PARTIDA — el jugador hace click en "Jugar"

**WHEN:** click en "Jugar" del MainMenu (post-login), antes de la calibracion del mic.
**WHY:** registrar que esta sesion de juego empezo, obtener un `IdPartida` para asociar logros que se desbloquearan durante la sesion.

**Llamada:**
```
POST http://estandentro.somee.com/api/Partidas
Content-Type: application/json

{
  "idJugador": 1,
  "nombrePartida": "Partida del 2026-05-02 21:30",
  "fechaInicio": "2026-05-02T21:30:00Z",
  "estado": 0,
  "capituloAlcanzado": 1,
  "tiempoSegundos": 0
}
```

- `nombrePartida`: opcional. Sugerencia: timestamp legible.
- `fechaInicio`: `DateTime.UtcNow` en formato ISO 8601.
- `estado`: 0 = EnCurso.

**Respuesta `201`:** el `Partida` con `idPartida` asignado.

**Guardar en runtime:**
```csharp
GameSession.CurrentPartidaId = response.idPartida;
GameSession.PartidaStartTime = DateTime.UtcNow;  // para calcular TiempoSegundos al final
```

**Politica de fallo:** si falla, `CurrentPartidaId = 0`. El juego sigue, pero los logros que se desbloqueen durante esta partida no se podran registrar. Log warning.

---

### 3. DESBLOQUEO DE LOGRO — durante el juego

**WHEN:** cuando ocurre un evento de gameplay que califica como logro. Cada uno de los 5 logros tiene su trigger especifico (ver tabla mas abajo).
**WHY:** registrar que este jugador desbloqueo este logro en esta partida.

**Llamada generica:**
```
POST http://estandentro.somee.com/api/LogroXPartida
Content-Type: application/json

{
  "idPartida": 1,
  "idLogro": 3
}
```

`fechaDesbloqueo` se autocompleta en BD con `GETUTCDATE()`.

**Respuesta `201`:** el registro creado.

**Idempotencia:** si el logro ya estaba desbloqueado en esta partida, la BD rechaza el insert (PK compuesta duplicada). Hay que manejar el 500 con mensaje "duplicate key" como **exito silencioso** (no tiene sentido bloquear UI por re-disparar un logro).

**Cliente debe:**
- Mantener un `HashSet<int>` en memoria con los `idLogro` ya desbloqueados en la partida actual.
- Antes de POST, chequear el set. Si ya esta, no llamar.
- Mostrar toast UI "🏆 Logro desbloqueado: [Nombre]" cuando el POST devuelve 201.

**Politica de fallo:** si falla, NO bloquear el juego. Log warning. Considerar (futuro) cola de retry.

---

### 4. FIN DE PARTIDA — Game Over o capitulo completado

**WHEN:** se dispara `StressSystem.OnCollapse` (Game Over) **O** el jugador llega al final del capitulo.
**WHY:** marcar el estado final de la partida y registrar tiempo total. Tambien es el momento de evaluar logros "globales del capitulo" (sigilo_observer, respiracion_zen, superviviente).

#### 4a. Si llego al final (capitulo completado)

**Antes de `PUT /api/Partidas`:** evaluar 3 logros que requieren llegar al final SIN haber fallado:

```csharp
// Evaluar logros de capitulo
if (!GameSession.ObserverTriggeredAtLeastOnce)
    UnlockLogro("sigilo_observer");
if (GameSession.BreathingFailedCycles == 0)
    UnlockLogro("respiracion_zen");
if (!GameSession.StressCollapsed)
    UnlockLogro("superviviente");
```

**Despues, cerrar la partida:**
```
PUT http://estandentro.somee.com/api/Partidas/1
Content-Type: application/json

{
  "idPartida": 1,
  "idJugador": 1,
  "nombrePartida": "Partida del 2026-05-02 21:30",
  "fechaInicio": "2026-05-02T21:30:00Z",
  "fechaFin": "2026-05-02T22:15:00Z",
  "estado": 1,
  "capituloAlcanzado": 1,
  "tiempoSegundos": 2700
}
```

- `estado: 1` = Completada.
- `fechaFin`: `DateTime.UtcNow`.
- `tiempoSegundos`: `(DateTime.UtcNow - GameSession.PartidaStartTime).TotalSeconds`.

**Respuesta `204 No Content`** si fue exitoso.

#### 4b. Si fue Game Over (StressSystem.OnCollapse)

```
PUT http://estandentro.somee.com/api/Partidas/1
Content-Type: application/json

{ ...mismos campos pero estado: 2 }
```

- `estado: 2` = Abandonada.
- No se evaluan los 3 logros de capitulo (no terminó).

**Politica de fallo:** si el PUT falla, log warning. La partida queda en estado `EnCurso (0)` en BD permanentemente. Aceptable por ahora (no bloquea siguiente sesion, solo es ruido en historico).

#### 4c. Pantalla de Resumen — `EndOfChapterOverlay`

Tras el PUT (haya sido exitoso o no), mostrar la pantalla de resumen al jugador. Esta es la **pantalla mas valiosa para la entrega academica** — aqui se ve toda la persistencia funcionando sin romper la inmersion del juego.

**UI esperada:**

```
┌─────────────────────────────────────┐
│   Capitulo 1 — COMPLETADO           │  (o "ABANDONADO" si fue Game Over)
│                                     │
│   Tiempo: 47:32                     │
│                                     │
│   LOGROS DESBLOQUEADOS              │
│   🔓 Maestro de cerraduras   +30    │
│   🔓 Lector compulsivo       +20    │
│   🔓 Sin que te vean         +40    │
│                                     │
│   LOGROS PENDIENTES                 │
│   🔒 Aire en orden                  │
│      "Sin fallar ciclos respiracion"│
│   🔒 Mente fria                     │
│      "Sin colapsar de estres"       │
│                                     │
│   PUNTOS: 90 / 190                  │
│                                     │
│   [ Volver al Menu ]  [ Reintentar ]│
└─────────────────────────────────────┘
```

**Datos que necesita la pantalla** (cero llamadas API extras, todo se calcula con `GameSession` + catalogo precargado):

- `tiempo`: `(DateTime.UtcNow - GameSession.PartidaStartTime).ToString("mm':'ss")`.
- `logros desbloqueados`: filtrar el catalogo (`GameSession.LogroIdByCodigo`) por los que esten en `GameSession.UnlockedLogrosThisPartida`. Mostrar nombre + puntos.
- `logros pendientes`: el complemento (los del catalogo que no estan en UnlockedLogrosThisPartida). Mostrar nombre + descripcion para motivar a reintentar.
- `puntos`: suma de puntos de los desbloqueados / suma total del catalogo (`190` si esta el catalogo completo).

**Botones:**
- `Volver al Menu`: `SceneManager.LoadScene("MainMenu")`.
- `Reintentar`: ejecuta el flujo del paso 2 (POST nueva partida) y carga la escena de juego.

---

### 5. PERFIL EN AJUSTES — pantalla opcional para personalizar y ver historial

**WHEN:** jugador hace `Ajustes -> Perfil` (boton nuevo en `SettingsOverlay`).
**WHY:** que el jugador pueda:
- Ver y editar su nombre visible (`nombres`).
- Ver sus stats acumuladas (partidas jugadas, completadas, logros X/5, puntos totales).
- Ver historial de partidas con su resultado.

**Para la entrega academica:** esta es la pantalla donde el profe ve la BD relacional poblada — con datos reales del jugador, partidas y logros. Sin tener que abrir SQL Server Object Explorer.

**UI esperada:**

```
┌───────────────────────────────────────────────────┐
│   PERFIL                                          │
│                                                   │
│   Nombre: [duvan          ]  [Editar]             │
│                                                   │
│   ESTADISTICAS                                    │
│   Partidas jugadas:     7                         │
│   Partidas completadas: 3                         │
│   Logros desbloqueados: 4 / 5                     │
│   Puntos totales:       150                       │
│                                                   │
│   HISTORIAL DE PARTIDAS                           │
│   #07  2026-05-02 22:15  Completada  47:32  90pts │
│   #06  2026-05-02 21:45  Abandonada  03:15   0pts │
│   #05  2026-05-02 20:30  Completada  52:18  120pts│
│   ...                                             │
│                                                   │
│   [ Cerrar ]                                      │
└───────────────────────────────────────────────────┘
```

**Llamadas API al abrir Perfil:**

1. `GET /api/Jugadores/{CurrentJugadorId}` — datos del jugador (nombre actual).
2. `GET /api/Partidas` — todas las partidas. Cliente filtra por `idJugador == CurrentJugadorId` y ordena por FechaInicio descendente.
3. `GET /api/LogroXPartida` — todos los desbloqueos. Cliente filtra por las partidas del jugador y agrega para mostrar conteo de logros unicos desbloqueados (con `DISTINCT IdLogro`).

> **Nota de escalabilidad:** los endpoints actuales devuelven todos los registros y el cliente filtra. Para alcance academico funciona. En produccion convendria endpoints dedicados (`GET /api/Jugadores/{id}/Partidas`, `GET /api/Jugadores/{id}/Logros`).

**Si el jugador edita su nombre y guarda:**

```
PUT /api/Jugadores/{CurrentJugadorId}
Content-Type: application/json

{
  "idJugador": <id>,
  "usuario": "<usuario_no_cambia>",
  "nombres": "<nuevo_nombre>",
  "apellido": ""
}
```

- Solo cambia `nombres`. `usuario` permanece igual (es el identificador estable, no se reasigna).
- Tras el PUT exitoso (`204 No Content`): actualizar `PlayerPrefs.SetString("CurrentNombres", nuevo)` y `GameSession.CurrentNombres`.

**Politica de fallo:** si la API no responde, mostrar la pantalla con mensaje "Sin conexion. Conectate a internet para ver tus stats." Sin crash. El boton "Editar" queda deshabilitado hasta tener red.

---

## Catalogo de logros (5 logros, 3 temas)

Estado actual de la BD tras el sembrado (2026-05-02). Los IDs reflejan el identity actual de SQL Server.

### Temas

| IdTemas | NombreTemas | Notas |
|---|---|---|
| 1 | Sigilo (prueba inicial) | **Preservado como evidencia** del primer flujo end-to-end. NO usar. |
| 2 | Exploracion | ✅ Catalogo oficial |
| 3 | Pedagogia | ✅ Catalogo oficial |
| 4 | Sigilo | ✅ Catalogo oficial |

### Logros

| IdLogro | Codigo | NombreLogro | Descripcion | IdTemas | Puntos | Notas |
|---|---|---|---|---|---|---|
| 1 | `sigilo_perfecto` | Sin ser detectado | Logro de prueba inicial | 1 | 50 | **Preservado, no mapeado a evento del juego** — queda inerte en BD |
| 2 | `cerradura_primera` | Maestro de cerraduras | Resuelve la cerradura del aula sin equivocarte | 2 (Exploracion) | 30 | ✅ Catalogo |
| 3 | `notas_completas` | Lector compulsivo | Lee todas las notas del aula | 2 (Exploracion) | 20 | ✅ Catalogo |
| 4 | `respiracion_zen` | Aire en orden | Completa el capitulo sin fallar ningun ciclo de respiracion | 3 (Pedagogia) | 50 | ✅ Catalogo |
| 5 | `superviviente` | Mente fria | Termina el capitulo sin colapsar de estres | 3 (Pedagogia) | 50 | ✅ Catalogo |
| 6 | `sigilo_observer` | Sin que te vean | Termina el capitulo sin disparar al Observador | 4 (Sigilo) | 40 | ✅ Catalogo |

**Total puntos posibles en una partida perfecta:** 190 (solo se cuentan los del catalogo, el de prueba no).

> **Importante:** el cliente Unity **no debe hardcodear IDs** — los IDs cambian si la BD se re-siembra. Usar siempre `GameSession.LogroIdByCodigo[codigo]` para resolver. Por eso el campo `Codigo` de `Logro` es UNIQUE: es el identificador estable.

---

## Mapeo evento de juego → logro (tabla maestra)

| Evento de juego | Trigger en codigo | Logro a desbloquear |
|---|---|---|
| Jugador resuelve `CombinationLock` con `failsCount == 0` | suscribirse a `CombinationLock.OnSolved`, leer la propiedad `FailsCount` | `cerradura_primera` |
| Jugador lee la 3a nota | suscribirse a `Note.OnRead`, evaluar `Inventory.Count == 3` | `notas_completas` |
| Capitulo termina con `BreathingMinigame.OnCycleFail` count == 0 | contador `GameSession.BreathingFailedCycles`, suma en `OnCycleFail` | `respiracion_zen` (al fin de capitulo) |
| Capitulo termina sin `OnCollapse` disparado | flag `GameSession.StressCollapsed`, se setea en `OnCollapse` | `superviviente` (al fin de capitulo) |
| Capitulo termina sin `ObserverTrigger.OnTriggered` | flag `GameSession.ObserverTriggeredAtLeastOnce` | `sigilo_observer` (al fin de capitulo) |

---

## Estado en runtime — `GameSession` static class

Esta clase mantiene el estado de la sesion actual. Vive en memoria mientras corre el juego, no se persiste (excepto IdJugador y Usuario que viven en PlayerPrefs).

```csharp
public static class GameSession
{
    // Persistente entre sesiones (PlayerPrefs)
    public static int CurrentJugadorId = 0;
    public static string CurrentUsuario = "";   // identificador estable (ej. "duvan_a3f8b9")
    public static string CurrentNombres = "";   // nombre visible del jugador (editable en Perfil)

    // Por partida (memoria)
    public static int CurrentPartidaId = 0;
    public static DateTime PartidaStartTime;
    public static HashSet<int> UnlockedLogrosThisPartida = new();

    // Catalogo precargado al arrancar la app
    public static Dictionary<string, int> LogroIdByCodigo = new();

    // Contadores para logros de capitulo (se resetean al iniciar partida)
    public static int BreathingFailedCycles = 0;
    public static bool ObserverTriggeredAtLeastOnce = false;
    public static bool StressCollapsed = false;
    // CombinationLock failsCount no necesita estar aqui, lo lee del componente al OnSolved
    // notasRead se infiere de Inventory.Count

    public static void ResetForNewPartida() {
        CurrentPartidaId = 0;
        UnlockedLogrosThisPartida.Clear();
        BreathingFailedCycles = 0;
        ObserverTriggeredAtLeastOnce = false;
        StressCollapsed = false;
    }
}
```

---

## Politica de errores (general)

| Tipo de error | Comportamiento |
|---|---|
| API no responde / timeout | Log warning, continuar el juego sin crash. La partida actual no se persiste, pero el juego es jugable. |
| 400 Bad Request | Bug de cliente. Log error con el body que se envio. NO retry automatico. |
| 500 Internal Server Error con "duplicate key" en LogroXPartida | Tratar como exito (el logro ya estaba registrado). NO mostrar error al usuario. |
| 500 con FK violation | Bug de cliente (mando un IdJugador o IdPartida que no existe). Log error. |
| Sin red detectada antes de llamar | Skip silenciosamente. |

**Bottom line:** la API es **best-effort**. Si funciona, perfecto, datos persistidos. Si no, el juego sigue jugable. Nunca bloquear UI por una llamada API fallida.

---

## Decisiones de diseno justificadas

### Por que precargar el catalogo de logros en lugar de hardcodear IDs
Si hardcodeo `idLogro = 3` en el cliente y alguien re-siembra la BD en otro orden, el cliente desbloquea el logro equivocado. Usar `codigo` (estable) y resolverlo a `idLogro` al runtime es robusto.

### Por que evaluar 3 logros (sigilo, respiracion, superviviente) recien al fin de capitulo
Son logros de "no fallaste en todo el cap". No se puede saber si los lograste hasta que el cap termine. Hacerlo al toque desperdiciaria llamadas y daria positivos falsos.

### Por que no usamos contraseña ni correo
Ver `Changelog/008_Diseño_API_Backend_y_Esquema_BD.md` seccion "Flow B".

### Por que coroutines y no async/await
El resto del proyecto usa coroutines (BreathingMinigame, PlayerWakeUp, etc.). Mantener consistencia. ApiClient sera coroutine + callback `Action<T> onSuccess, Action<string> onError`.

### Por que `static class GameSession` en vez de Singleton MonoBehaviour
No necesita componente en escena ni lifecycle de Unity. Es estado puro de sesion. Static es lo mas simple.

### Por que auto-perfil silencioso en vez de pantalla de login
Ningun juego de horror te pide nombre y apellido para jugar — eso rompe la inmersion del frame 1. El perfil se genera automaticamente en background usando `Environment.UserName + GUID corto`. Si el jugador quiere personalizar su nombre, lo hace en `Ajustes -> Perfil` (opcional). Esto mantiene la persistencia y la entrega academica (BD poblada, jugador identificado) sin sacrificar UX.

### Por que mostrar el resumen al fin de capitulo en pantalla propia
Es la pantalla mas valiosa para la entrega: muestra logros desbloqueados, pendientes y stats sin tener que abrir la BD. Tambien es satisfactorio para el jugador (recompensa) y pedagogico (motiva a reintentar para conseguir los pendientes). Inspirado en patrones de Hades / Celeste.

---

## Proximos pasos para integrar

1. **Sembrar BD** — ✅ ya hecho (3 temas + 5 logros del catalogo + datos de prueba preservados).
2. **Player Settings** — `Allow downloads over HTTP` = Always allowed (Unity 2022+).
3. **Crear `Assets/Scripts/Network/ApiClient.cs`** — Singleton MonoBehaviour. UnityWebRequest + coroutines + callbacks (`onSuccess`, `onError`). Metodos: `CreateJugador`, `GetJugador`, `UpdateJugador`, `CreatePartida`, `UpdatePartida`, `UnlockLogro`, `GetAllLogros`, `GetPartidasByJugador` (filtrado cliente).
4. **Crear `Assets/Scripts/Network/Dtos.cs`** — `JugadorDto`, `PartidaDto`, `LogroDto`, `LogroXPartidaDto` con `[Serializable]` (compatibles con `JsonUtility`).
5. **Crear `Assets/Scripts/Network/GameSession.cs`** — static class con el estado de sesion (ver bloque mas arriba).
6. **Crear `Assets/Scripts/Network/ApiBootstrapper.cs`** — MonoBehaviour que en `Awake` (GameObject persistente con DontDestroyOnLoad):
   - Auto-perfil al primer arranque (POST jugador, persistir en PlayerPrefs).
   - Precarga catalogo con GET /api/Logros, llena `GameSession.LogroIdByCodigo`.
7. **Crear `Assets/Scripts/UI/ProfileOverlay.cs`** — pantalla de Perfil que se abre desde `SettingsOverlay` (boton nuevo "Perfil"). Muestra nombre editable + stats + historial. Llamadas API descriptas en seccion 5.
8. **Crear `Assets/Scripts/UI/EndOfChapterOverlay.cs`** — pantalla de resumen tras `OnCollapse` o fin de capitulo. Datos en memoria, cero llamadas API. Botones Volver al Menu / Reintentar.
9. **Modificar `SettingsOverlay`** — agregar boton "Perfil" que abre `ProfileOverlay`.
10. **Hooks en sistemas existentes** — uno por uno: `CombinationLock.OnSolved`, `Note.OnRead`, `BreathingMinigame.OnCycleFail`, `ObserverTrigger.OnTriggered`, `StressSystem.OnCollapse`. Ver "Mapeo evento → logro".
11. **POST partida al iniciar** — en el flujo `MainMenu -> calibracion mic -> escena de juego`, ejecutar `ApiClient.CreatePartida` despues de la calibracion (cuando empieza la partida real).
12. **PUT al fin de capitulo** — definir donde esta el "fin de capitulo" (escena de outro o trigger especifico) y cerrar la partida ahi + abrir `EndOfChapterOverlay`. Si fue Game Over via `StressSystem.OnCollapse` -> mismo PUT con `estado=2` + abrir overlay.

---

## Que NO esta documentado aqui (links)

- Decisiones de diseno BD (Flow B, sin contraseña, sin correo, justificacion del LogroXPartida): `Changelog/008_Diseño_API_Backend_y_Esquema_BD.md`.
- Inventario de los sistemas de juego que disparan estos eventos: `Arquitectura_Sistemas.md`.
- Codigo fuente de la API: proyecto separado en `C:\Users\duvan\OneDrive\Escritorio\Universidad\ApiJuego\EstanDentro.Api\`.
