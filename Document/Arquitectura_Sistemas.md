# Arquitectura de sistemas — Estan Dentro

Inventario tecnico de todos los sistemas del juego. Para cada sistema: que hace, donde vive el codigo, que eventos dispara, de que depende. Al final, una seccion de **hooks para la API** que conecta cada sistema con los endpoints de persistencia.

**Ultima actualizacion:** 2026-05-02 (Sprint 3, post deploy API)

---

## Mapa rapido

```
Assets/Scripts/
├─ Player/         FPS + secuencia de despertar
├─ Audio/          Loops ambientales
├─ Stress/         Medidor de estres + Game Over
├─ Breathing/      Minijuego de respiracion + calibracion mic
├─ Interaction/    Raycast, notas, cerradura, linterna, apagon
├─ Intrusions/     Observador, gaze, siluetas perifericas
├─ Inventory/      Lista de notas leidas + UI
├─ UI/             Menus, pausa, settings, HUD, transiciones, perfil, resumen capitulo
├─ Network/        ⭐ NUEVO — cliente API, GameSession, auto-perfil, DTOs
└─ Editor/         Editor tools (no runtime)
```

---

## Sistemas por carpeta

### Player

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `PlayerController.cs` | FPS controller. WASD + look + sprint + crouch via `PlayerInput` (Send Messages). Input dual teclado/mando. Aplica `Settings` en Start (mouseSens, gamepadSens, invertY). | Metodos: `OnInteract`, `OnFlashlight`, `OnBreathe`. Property `InputEnabled`. Metodo `ApplySettings()` para refrescar desde Ajustes. |
| `PlayerWakeUp.cs` | Secuencia inicial al cargar la escena: overlay negro -> pestaneos -> look around natural -> devuelve control. | UnityEvent `WakeUpRoutine` (privado). Mientras corre, `PlayerController.InputEnabled = false`. |

**Dependencias entre Player:** `PlayerWakeUp` requiere `PlayerController` (RequireComponent).

---

### Audio

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `SubliminalLoopController.cs` | Loops sutiles de horror persistentes. Conecta al `MainMixer.mixer`. | Reemplazado por `ambient_aula_base` cuando lleguen los assets de Carlos (ver brief). |

---

### Stress (sistema central, conecta todo)

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `StressSystem.cs` | Medidor 0-100. Sube por eventos del juego (intrusiones, susto, mira sostenida del Observer). Baja por respiracion exitosa. Si llega a 100 -> colapso. | **`OnCollapse`** (UnityEvent, KEY para Game Over y logros). Metodos: `AddStress(float)`, `RemoveStress(float)`, `GetCurrent()`. |
| `GameOverHandler.cs` | Escucha `StressSystem.OnCollapse`. Pausa el juego, muestra overlay rojo "TE DEJASTE LLEVAR", espera 1.5s + boton -> reinicia escena via `SceneManager`. | Sin eventos publicos. Reinicia la escena al confirmar. |

**Hook critico para API**: `OnCollapse` = fin de partida con estado=Abandonada (ver `Flujo_API_Estan_Dentro.md`).

---

### Breathing (mecanica nuclear pedagogica)

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `BreathingInputProvider.cs` | Abstrae la fuente de "respiracion": mic real o tecla Espacio sostenida. Persiste calibracion en `PlayerPrefs`. | `HasStoredCalibration()`, `TryLoadStoredCalibration()`, `SaveCalibration()`, `ClearStoredCalibration()`. |
| `MicCalibration.cs` | UI de calibracion inicial al primer arranque. En `Start` verifica si hay calibracion previa y la carga silenciosamente. Boton de re-calibracion. | `RestartCalibration()`. |
| `BreathingMinigame.cs` | Minijuego rítmico 4-4-2. Tutorial in-game al primer disparo. Fade-in del circulo, badge del boton, modo "freeCycles" sin penalty al inicio. | UnityEvents `OnCycleSuccess`, `OnCycleFail`. Metodo `StartMinigame()`. |

**Hook para logro `respiracion_zen`**: contar los `OnCycleFail` durante todo el capitulo. Si al final son 0 -> desbloquear.

---

### Interaction

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `InteractionSystem.cs` | Raycast desde camara + reticle dinamico + tecla E. Expone qué objeto está mirando el jugador. | Metodo `TryInteract()` (llamado por `OnInteract` del Player). |
| `Note.cs` | Nota leíble del entorno. Al ser inspeccionada abre `NoteOverlay` con texto. Registra automaticamente en `Inventory`. Hereda de `Interactable`. | Al primer Read se registra en `Inventory.Add(NoteEntry)`. |
| `CombinationLock.cs` | Cerradura de 4 ruedas de digitos. Configurable: combinacion, oneShot opcional. Hereda de `Interactable`. | UnityEvents `OnSolved`, `OnFailed`. Cableado al `BlackoutEvent.TriggerBlackout` en escena. |
| `LockOverlay.cs` | UI singleton procedural con 4 ruedas. Input dual mouse + flechas + D-pad. Coordina con `OverlayBlocker`. | Lo llama `CombinationLock` al `OnInteract`. |
| `Flashlight.cs` | Linterna como hijo de Main Camera del Player. Aplica defaults visuales del Light en `Awake` (intensity 2000 lumens, range 10, spotAngle 50, color calido, soft shadows). | Property `IsOn`. Metodo `Toggle()`. Llamado por `OnFlashlight` del Player. |
| `BlackoutEvent.cs` | Evento global de apagón. Se dispara al resolver la cerradura. | Metodo estatico `TriggerBlackout()`. Apaga luces de escena, fuerza activar linterna. |

**Hook para logro `cerradura_primera`**: en `CombinationLock.OnSolved`, verificar si `failsCount == 0`. Si es 0 -> desbloquear logro.
**Hook para logro `notas_completas`**: en `Note.OnRead`, verificar si `Inventory.Count >= 3` -> desbloquear.

---

### Intrusions (atmosfera + susto)

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `IntrusionManager.cs` | Coordina apariciones del Observador y otros eventos de intrusion. Cooldown configurable (60s en aula real para no spamear durante respiracion). Cada intrusion suma stress. | Metodo `TriggerIntrusion(IntrusionType)`. |
| `GazeDetector.cs` | Detecta cuando el jugador esta mirando un `GazeTargetBase` y por cuanto tiempo. | UnityEvents `OnGazeStart`, `OnGazeProgress`, `OnGazeComplete`. |
| `GazeTargetBase.cs` | Clase base para targets de gaze. Configurable umbral de tiempo. | Heredan de aqui los targets concretos. |
| `ObserverTrigger.cs` | Hereda de `GazeTargetBase`. Configurado en la pizarra (Cubo.002 en Dev_Henry). Al gaze sostenido -> dispara `IntrusionManager.TriggerIntrusion(Observer)`. | UnityEvent `OnTriggered` (heredado). |
| `SilhouetteManager.cs` | Spawnea siluetas perifericas (shader) en el campo de vision lateral. | API privada, solo usado internamente. Stand-by si el shader visual no se ve bien. |
| `SilhouetteController.cs` | Controla una silueta individual: aparicion, fade, despawn. | Subordinado al manager. |

**Hook para logro `sigilo_observer`**: en `ObserverTrigger.OnTriggered`, marcar flag `observerTriggeredAtLeastOnce = true`. Si al terminar el capitulo es `false` -> desbloquear logro.

---

### Inventory

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `Inventory.cs` | Singleton. Mantiene la lista de `NoteEntry` leidas. Polling de tecla I (teclado) o Touchpad (mando). | `Add(NoteEntry)`, `GetAll()`, property `Count`. |
| `InventoryOverlay.cs` | UI scrollable con lista de botones por nota. Click reabre la nota. Stack de navegacion: cerrar nota -> vuelve al inventario. | Coordina con `OverlayBlocker`. |

---

### UI (capa de presentacion)

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `MainMenuController.cs` | Menu principal procedural. Botones JUGAR / AJUSTES / SALIR. Navegable por mouse + teclado + gamepad. Auto-crea Camera vacia y EventSystem si no existen. | **Aqui se enchufa el flujo de login a la API** (input de Usuario antes de "Jugar"). Ver `Flujo_API_Estan_Dentro.md`. |
| `PauseMenuHandler.cs` | Detecta Esc/Options. Pausa con `timeScale=0`. Botones Continuar / Notas / Ajustes / Salir al menu. Coordina con `OverlayBlocker` para no abrirse encima de Note/Lock/Calibration. | Metodos `Open()`, `Close()`. |
| `OverlayBlocker.cs` | Registry estatico de overlays modales activos. Cualquier UI modal se registra al abrir, des-registra al cerrar. PauseMenuHandler consulta antes de abrir. | `Register(string id)`, `Unregister(string id)`, `IsAnyOpen()`. |
| `Settings.cs` | Static class persistida en PlayerPrefs. Props: VolumenMaster, MouseSensitivity, GamepadSensitivity, InvertY. | `Load()`, `Save()`, `Reset()`. |
| `SettingsOverlay.cs` | UI sliders + toggle + boton "Re-calibrar microfono" + Cerrar. Cambios se aplican en vivo. Highlight de fila seleccionada. | Coordina con `OverlayBlocker`. |
| `HUDBootstrapper.cs` | Arma el HUD del juego (medidor de stres, indicador de aire, reticle del InteractionSystem) en runtime. | Construccion procedural. |
| `LoadingScreenController.cs` | Pantalla de carga con un tip de regulacion emocional (técnica 4-7-8, anclaje sensorial, etc.). | Mostrada entre escenas. |
| `CinematicController.cs` | Reproduce cinematicas (intro narrativo, etc.). | Metodo `PlayClip(name)`. |
| `SceneTransition.cs` | Singleton para transiciones entre escenas con fade. | `LoadScene(string name)`. |
| `SceneTransitionTrigger.cs` | Trigger en mundo que dispara `SceneTransition.LoadScene` al colisionar. | Configurable en Inspector. |
| `ProfileOverlay.cs` ⭐ | **Nuevo (Fase 5)**. Pantalla de Perfil accesible desde `SettingsOverlay`. Muestra nombre editable + stats acumuladas (partidas jugadas/completadas, logros desbloqueados X/5, puntos totales) + historial de partidas. Aqui es donde el profe ve la BD relacional poblada. | Llamadas a `ApiClient.GetJugador`, `GetPartidasByJugador`, `GetLogroXPartidaByJugador`, `UpdateJugador`. Coordina con `OverlayBlocker`. |
| `EndOfChapterOverlay.cs` ⭐ | **Nuevo (Fase 5)**. Pantalla de resumen al fin de capitulo o Game Over. Muestra titulo (COMPLETADO/ABANDONADO), tiempo total, logros desbloqueados con puntos, logros pendientes con su descripcion (motivacion), puntos totales (X/190). Botones: Volver al Menu / Reintentar. | Cero llamadas API (todo desde `GameSession`). Botones disparan `SceneManager.LoadScene` o reinicio de partida. |

---

### Network ⭐ NUEVO (Fase 5)

Sistema de comunicacion con la API de persistencia (`http://estandentro.somee.com`). Detalle completo en `Flujo_API_Estan_Dentro.md`.

| Script | Que hace | Eventos / API publica |
|---|---|---|
| `ApiClient.cs` ⭐ | Singleton MonoBehaviour. Envuelve `UnityWebRequest` con coroutines + callbacks. Patron `Coroutine Method(args, Action<T> onSuccess, Action<string> onError)`. Centraliza la BaseUrl. | Metodos publicos: `CreateJugador`, `GetJugador`, `UpdateJugador`, `CreatePartida`, `UpdatePartida`, `UnlockLogro`, `GetAllLogros`, `GetAllPartidas`, `GetAllLogroXPartida`. |
| `GameSession.cs` ⭐ | Static class. Mantiene estado de sesion: `CurrentJugadorId`, `CurrentUsuario`, `CurrentNombres`, `CurrentPartidaId`, `PartidaStartTime`, `UnlockedLogrosThisPartida`, `LogroIdByCodigo` (catalogo precargado), contadores para logros de capitulo (`BreathingFailedCycles`, `ObserverTriggeredAtLeastOnce`, `StressCollapsed`). | `ResetForNewPartida()`. Acceso directo a campos publicos. |
| `ApiBootstrapper.cs` ⭐ | MonoBehaviour persistente (`DontDestroyOnLoad`). En `Awake` ejecuta: 1) auto-perfil (crea Jugador en BD si es primer arranque, persiste en PlayerPrefs); 2) precarga catalogo de Logros y llena `GameSession.LogroIdByCodigo`. Modo offline si la API no responde. | Ningun evento publico — corre solo y llena `GameSession`. |
| `Dtos.cs` ⭐ | `[Serializable]` DTOs compatibles con `JsonUtility`: `JugadorDto`, `PartidaDto`, `LogroDto`, `LogroXPartidaDto`. Incluye wrappers `JugadorListWrapper`, `PartidaListWrapper`, etc. para deserializar arrays JSON (`JsonUtility` no soporta arrays raiz). | Solo modelos. |

---

### Editor (no runtime)

| Script | Que hace |
|---|---|
| `SeatAnchorTools.cs` | Editor tool para colocar anchors de asiento del jugador en la escena. |

---

## Diagrama de eventos clave (cadenas de disparo)

```
PlayerController.OnInteract  ─┬─► InteractionSystem.TryInteract  ─┬─► Note.Read       ─► Inventory.Add
                              │                                   ├─► CombinationLock.OnInteract ─► LockOverlay.Show
                              │                                   └─► Flashlight.Toggle
                              │
                              └─ (no aplica al pause/notes)

GazeDetector (gaze sostenido) ─► ObserverTrigger.OnTriggered ─► IntrusionManager.TriggerIntrusion(Observer)
                                                              ─► StressSystem.AddStress(N)

CombinationLock.OnSolved ─► BlackoutEvent.TriggerBlackout ─► (apaga luces) + activa Flashlight

BreathingMinigame.OnCycleSuccess ─► StressSystem.RemoveStress(N)
BreathingMinigame.OnCycleFail    ─► StressSystem.AddStress(N)

StressSystem (ge 100) ─► OnCollapse ─► GameOverHandler ─► overlay rojo + reload scene
```

---

## Hooks para la API (resumen)

Esta seccion conecta los sistemas con los endpoints. Detalle completo en `Flujo_API_Estan_Dentro.md`.

| Trigger | Endpoint | Logro candidato |
|---|---|---|
| `ApiBootstrapper.Awake` (primer arranque, silencioso) | `POST /api/Jugadores` (auto-perfil con `usuario = nombreWindows + GUID`) | — |
| `ApiBootstrapper.Awake` (siempre) | `GET /api/Logros` (precarga catalogo en `GameSession.LogroIdByCodigo`) | — |
| `MainMenuController` click "Jugar" | `POST /api/Partidas` (inicio, obtiene `IdPartida`) | — |
| `CombinationLock.OnSolved` con failsCount==0 | `POST /api/LogroXPartida` | `cerradura_primera` |
| `Note.OnRead` cuando `Inventory.Count == 3` | `POST /api/LogroXPartida` | `notas_completas` |
| Fin de capitulo si `BreathingFailedCycles == 0` | `POST /api/LogroXPartida` | `respiracion_zen` |
| Fin de capitulo si `!StressCollapsed` | `POST /api/LogroXPartida` | `superviviente` |
| Fin de capitulo si `!ObserverTriggeredAtLeastOnce` | `POST /api/LogroXPartida` | `sigilo_observer` |
| `StressSystem.OnCollapse` | `PUT /api/Partidas/{id}` (estado=Abandonada) + abrir `EndOfChapterOverlay` | — |
| Fin natural de capitulo | `PUT /api/Partidas/{id}` (estado=Completada) + abrir `EndOfChapterOverlay` | — |
| `ProfileOverlay.Open` | `GET /api/Jugadores/{id}` + `GET /api/Partidas` (filtra) + `GET /api/LogroXPartida` (filtra) | — |
| `ProfileOverlay` editar nombre | `PUT /api/Jugadores/{id}` (cambia `nombres`, NO `usuario`) | — |

---

## Convenciones tecnicas observadas

- **Singletons**: `Inventory`, `OverlayBlocker`, `SceneTransition`, `MainMenuController`, `LockOverlay`. Acceso via `.Instance`.
- **UI procedural**: casi toda la UI se arma por codigo (Canvas/Image/Text en `Awake` o `BuildUI()`). Decision deliberada del `PLAN_ENTREGA_FINAL.md`.
- **Persistencia local**: `PlayerPrefs` para Settings y calibracion mic. La API persiste el progreso de jugador.
- **Input dual**: todos los sistemas que aceptan input soportan teclado/raton Y gamepad como ciudadanos de primera.
- **OverlayBlocker**: cualquier overlay modal nuevo debe registrarse aqui para no chocar con el pause menu.

---

## Que NO esta documentado aqui (links)

- **Decisiones de diseno** del juego (pivote del puzzle, mensaje pedagogico): ver `PLAN_ENTREGA_FINAL.md` secciones 0-3.
- **Cronologia** de implementacion de cada sistema: ver carpeta `Changelog/`.
- **API y persistencia**: ver `Flujo_API_Estan_Dentro.md` (en esta misma carpeta).
- **Audio assets pendientes**: ver `BRIEF_CARLOS_AUDIO_CINEMATICAS.md`.
