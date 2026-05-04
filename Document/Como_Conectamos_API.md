# Como conectamos el juego con la API

Documento corto y practico. Pensado para que entiendas el flujo y se lo puedas explicar a tu profe en 5 minutos.

---

## La idea en una frase

El juego dispara llamadas HTTP a la API cuando ocurren **eventos importantes** (iniciar partida, desbloquear logro, fin de capitulo). La API guarda esos datos en SQL Server. Cuando volves al menu, otra llamada lee la BD para mostrar tu lista de partidas guardadas.

---

## Arquitectura por capas

```
                    ┌────────────────────┐
                    │   Tu juego Unity   │  (escenas, sistemas, scripts)
                    └─────────┬──────────┘
                              │ dispara eventos
                              ▼
                    ┌────────────────────┐
                    │   GameSession      │  estado en memoria del jugador
                    │   (static class)   │  + helpers TryUnlockLogro / Resume
                    └─────────┬──────────┘
                              │ usa
                              ▼
                    ┌────────────────────┐
                    │   ApiClient        │  envia HTTP via UnityWebRequest
                    │   (singleton)      │  metodos: Create/Update/Delete
                    └─────────┬──────────┘
                              │ JSON
                              ▼
              http://estandentro.somee.com/api/...
                              │
                              ▼
                    ┌────────────────────┐
                    │   ASP.NET API      │  controllers + EF Core
                    │   en somee         │
                    └─────────┬──────────┘
                              │ EF Core
                              ▼
                    ┌────────────────────┐
                    │   SQL Server       │  juegoNuevo.mssql.somee.com
                    └────────────────────┘
```

---

## 5 archivos que conectan todo

Todos viven en `Assets/Scripts/Network/`:

### 1. `Dtos.cs` — los datos que viajan por JSON

POCOs serializables (`[Serializable]`) que mapean 1:1 con las entidades de la API. Compatibles con `JsonUtility` de Unity.

```csharp
[Serializable]
public class PartidaDto
{
    public int idPartida;
    public int idJugador;
    public string nombrePartida;
    public string fechaInicio;   // ISO 8601
    public byte estado;          // 0=EnCurso, 1=Completada, 2=Abandonada
    public int capituloAlcanzado;
    public int tiempoSegundos;
}
```

### 2. `ApiClient.cs` — la mensajeria

Singleton MonoBehaviour con metodos para cada endpoint. Patron coroutine + callbacks para no bloquear el frame.

```csharp
ApiClient.Instance.CreatePartida(
    data,
    response => Debug.Log($"OK idPartida={response.idPartida}"),
    error => Debug.LogWarning($"Fallo: {error}")
);
```

Internamente arma un `UnityWebRequest` con el JSON, espera la respuesta, parsea, dispara callback. Best-effort: si falla, log warning y el juego sigue.

### 3. `GameSession.cs` — memoria de la sesion

Static class que mantiene el estado del jugador en memoria (con perfil persistido en `PlayerPrefs`).

```csharp
public static int CurrentJugadorId;
public static int CurrentPartidaId;
public static DateTime PartidaStartTime;
public static HashSet<int> UnlockedLogrosThisPartida;
public static Dictionary<string, int> LogroIdByCodigo;  // catalogo precargado
```

Tambien tiene **helpers centralizados** que los sistemas del juego llaman:

```csharp
GameSession.TryUnlockLogro("cerradura_primera");  // idempotente, best-effort
GameSession.ResumePartida(idPartida, fechaInicio); // para "Continuar"
GameSession.ResetForNewPartida();                  // limpia contadores
```

### 4. `ApiBootstrapper.cs` — corre antes del primer frame

`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` lo ejecuta automaticamente. Hace 2 cosas:

1. **Auto-perfil silencioso**: si es primer arranque, crea un Jugador con el nombre de Windows + GUID corto. Sin pantalla de login.
2. **Precarga catalogo de logros**: `GET /api/Logros` y guarda mapa `codigo → idLogro` en `GameSession.LogroIdByCodigo`. Asi cuando un sistema dispara `TryUnlockLogro("notas_completas")` ya sabe a que id mandar al POST.

### 5. `ChapterFlow.cs` — orquesta el cierre de capitulo

Cuando termina un capitulo, evalua los 3 logros globales (sin colapsar de estres, sin fallar respiracion, sin disparar al observador) y manda el PUT para cerrar la partida (`estado=1`).

```csharp
yield return ChapterFlow.EndChapter(completado: true);
EndOfChapterOverlay.Open(...);
```

---

## El flujo completo de una partida

```
1. ARRANQUE de la app
   └─> ApiBootstrapper:
       ├─> POST /api/Jugadores  (auto-perfil si primer arranque)
       └─> GET /api/Logros      (precarga catalogo)

2. CLICK en "Nueva Partida"
   └─> MainMenuController.RunCalibrationThenLoad():
       ├─> calibracion mic
       └─> POST /api/Partidas   (crea partida, estado=0 EnCurso)
           └─> guarda CurrentPartidaId

3. JUGANDO
   └─> Eventos disparan POST /api/LogroXPartida via TryUnlockLogro:
       - Resolver casillero sin fallar  → "cerradura_primera"
       - Leer 3 notas distintas          → "notas_completas"
   └─> Otros eventos solo setean flags en GameSession para evaluar al final:
       - BreathingMinigame.OnCycleFail   → BreathingFailedCycles++
       - ObserverTrigger.OnTriggered     → ObserverTriggeredAtLeastOnce = true
       - StressSystem.OnCollapse         → StressCollapsed = true

4. FIN DE CAPITULO (Acto 3, futuro)
   └─> ChapterFlow.EndChapter(true):
       ├─> Evalua flags y POST 3 logros mas (sigilo_observer, respiracion_zen, superviviente)
       └─> PUT /api/Partidas    (estado=1 Completada, fechaFin, tiempoSegundos)
   └─> EndOfChapterOverlay muestra resumen con logros + puntos + boton "Volver al menu"

5. VUELVE AL MENU
   └─> MainMenuController.RefreshContinueButton():
       └─> GET /api/Partidas + filtra por idJugador
           ├─> Si vacio: boton "Continuar" gris con "(sin partidas guardadas)"
           └─> Si tiene partidas: boton "Continuar" habilitado

6. CLICK "CONTINUAR"
   └─> PartidasOverlay.LoadAndPopulate():
       └─> GET /api/Partidas + filtra por idJugador
           └─> Lista de filas con [Retomar] [Borrar]
               ├─> [Retomar] → reusa idPartida viejo (sin POST nuevo)
               └─> [Borrar]  → DELETE /api/Partidas/{id}
```

---

## Decisiones de diseno

### Cero friccion para el jugador
No hay pantalla de login. El perfil se crea automaticamente con el nombre de Windows. Si te interesa cambiar tu nombre visible, lo haces desde Ajustes → Perfil.

### Best-effort en todo
Si la API esta caida, el juego sigue jugable. Solo no se persisten los datos. Cero crashes. Cero dialogos de error que rompan inmersion. El usuario probablemente ni se entera.

### Catalogo de logros precargado
No hardcodeo IDs en el cliente. Uso el campo `codigo` (string estable). Asi si la BD se re-siembra y los IDs cambian, el cliente sigue funcionando.

### Idempotencia local
`GameSession.UnlockedLogrosThisPartida` es un `HashSet<int>` que registra que logros ya se mandaron al POST en esta sesion. Asi no spammeo la API ni dependo de duplicate-key errors.

### Save games con DELETE real
La pantalla de Continuar permite borrar partidas via `DELETE /api/Partidas/{id}`. Demuestra los 4 verbos REST (POST, GET, PUT, DELETE) en el modelo academico.

---

## Endpoints en uso

| Verbo | URL | Cuando se llama |
|---|---|---|
| POST | `/api/Jugadores` | Primer arranque (auto-perfil) |
| GET | `/api/Logros` | Cada arranque (precarga catalogo) |
| GET | `/api/Partidas` | Cargar menu (decidir si mostrar Continuar) + abrir PartidasOverlay |
| POST | `/api/Partidas` | Click "Nueva partida" (post-calibracion) |
| PUT | `/api/Partidas/{id}` | Fin de capitulo (estado=1 Completada) |
| DELETE | `/api/Partidas/{id}` | Boton "Borrar" en PartidasOverlay |
| POST | `/api/LogroXPartida` | Cada vez que se desbloquea un logro |

---

## Resumen para tu profe

> "El juego se conecta a la API por HTTP usando `UnityWebRequest`. Toda la logica de red esta encapsulada en una capa `Network` con 5 archivos que separan responsabilidades: DTOs (datos), ApiClient (mensajeria), GameSession (estado), ApiBootstrapper (init), ChapterFlow (logica de cierre).
>
> Los eventos del juego (resolver puzzle, leer nota, terminar capitulo) disparan llamadas a la API a traves de helpers centralizados como `TryUnlockLogro(codigo)`. Los logros se identifican por un campo `codigo` UNIQUE en BD, no por ID, para que el cliente no rompa si la BD se re-siembra.
>
> El flujo demuestra los 4 verbos REST: POST al crear partida y desbloquear logros, GET al listar partidas y precargar catalogo, PUT al cerrar partida con su tiempo total, DELETE al borrar partidas desde el menu de save games."

---

## Documentos relacionados

- `Changelog/008_Diseño_API_Backend_y_Esquema_BD.md` — historia del diseno BD
- `Flujo_API_Estan_Dentro.md` — flujo detallado de cada llamada
- `Estado_Actual_2026-05-04.md` — estado del proyecto al cierre del Sprint 3
