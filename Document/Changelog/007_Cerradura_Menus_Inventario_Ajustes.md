# 007 - Cerradura, menus, inventario, ajustes (extension del domingo)

**Fecha:** 2026-04-26 (domingo, sesion extendida)
**Sprint:** Plan de entrega final - bloques 6 a 14
**Responsable:** Duvan Lozano + Claude
**Tarea relacionada:** PLAN_ENTREGA_FINAL.md - se adelantaron las tareas del lunes y martes

---

## Que se hizo

Tras cerrar el bucle nucleo del juego en la manana (changelog 006), Duvan flagged que trabajar en Dev_Duvan se sentia como "no avanzar" y pidio mover todo a la escena real con el aula de Henry. Adicionalmente decidio terminar todos los bloques restantes del plan original en el mismo dia.

### Bloques completados en esta sesion

6. **Cerradura por combinacion** - `CombinationLock` (Interactable) + `LockOverlay` (UI procedural con 4 ruedas de digitos). Input mouse + flechas + D-pad. Al acertar dispara UnityEvent `OnSolved` (cableado al `BlackoutEvent.TriggerBlackout`).
7. **UX del minijuego de respiracion** - Tutorial in-game al primer disparo, fade-in del circulo, badge del boton (Space/Triangle), 2 ciclos sin penalty al inicio. Resuelve la deuda flagged en changelog 006.
8. **Migracion al aula real** - Trabajo en `Dev_Henry.unity` (escena con el modelo del salon de Henry). Cambio de Cooldown del ObserverTrigger a 60s para evitar spam de intrusiones durante los ciclos respiratorios. Linterna como hijo de la Main Camera del Player con valores HDRP razonables (intensity 2000, range 10, spotAngle 50, color calido) configurados desde el script.
9. **Game Over real** - `GameOverHandler` que escucha `OnCollapse` del StressSystem, muestra overlay rojo "TE DEJASTE LLEVAR" con pausa, espera 1.5s + cualquier boton para reiniciar la escena via SceneManager.
10. **Persistir calibracion del mic** - `BreathingInputProvider` guarda calibracion en PlayerPrefs (threshold, modo, rms). En `MicCalibration.Start` verifica si hay calibracion previa y la carga silenciosamente. Boton de re-calibracion en Ajustes invalida los prefs.
11. **Menu principal** - Escena `MainMenu.unity` con `MainMenuController` (UI por codigo, vignette, botones JUGAR/AJUSTES/SALIR, navegacion mouse + teclado + gamepad). Auto-crea Camera vacia y EventSystem si no existen.
12. **Pause menu** - `PauseMenuHandler` agregado a `_Systems`. ESC/Options abre/cierra. Botones Continuar/Notas/Ajustes/Salir al menu. Con `OverlayBlocker` (registry estatico) coordinado con NoteOverlay/LockOverlay/MicCalibration/GameOverHandler para que el pause no se abra encima de otros overlays modales.
13. **Inventario de notas leidas** - `Inventory` singleton mantiene la lista de notas que el jugador ha leido. `InventoryOverlay` con UI scrollable simple (lista de botones por nota). Tecla I del teclado o Touchpad del mando lo abren desde el juego. Tambien accesible desde Pause -> Notas. Click en una entrada reabre la nota completa. Stack de navegacion: cerrar la nota vuelve al inventario, cerrar el inventario vuelve al pause (si vino de ahi) o al juego.
14. **Ajustes** - `Settings` static class persistida en PlayerPrefs (volumen master, sensitivity mouse, sensitivity gamepad, invertir Y). `SettingsOverlay` UI con sliders + toggle + boton "Re-calibrar microfono" + boton Cerrar. Cambios se aplican en vivo (ej. mover slider de mouseSens actualiza el PlayerController.mouseSensitivity inmediatamente). Recalibrar es inmediato si hay MicCalibration en escena, o queda diferido si esta en MainMenu. Highlight visual de la fila seleccionada (label cambia a color amber) para que sea claro donde esta el cursor con teclado/gamepad.

### Scripts creados

- `Assets/Scripts/Interaction/CombinationLock.cs` - Hereda Interactable. Combinacion configurable, oneShot opcional, UnityEvents OnSolved/OnFailed.
- `Assets/Scripts/Interaction/LockOverlay.cs` - Singleton UI con ruedas de digitos. Input dual.
- `Assets/Scripts/Stress/GameOverHandler.cs` - Suscribe a OnCollapse del StressSystem. Pausa + overlay + reinicio de escena.
- `Assets/Scripts/UI/MainMenuController.cs` - UI procedural del menu principal. Auto-crea Camera y EventSystem.
- `Assets/Scripts/UI/PauseMenuHandler.cs` - Detecta Esc/Options. Pausa con timeScale=0. Coordina con OverlayBlocker.
- `Assets/Scripts/UI/OverlayBlocker.cs` - Registry estatico de overlays modales activos.
- `Assets/Scripts/UI/Settings.cs` - Static class de persistencia (PlayerPrefs).
- `Assets/Scripts/UI/SettingsOverlay.cs` - UI con sliders + toggle + buttons. Highlight de selección.
- `Assets/Scripts/Inventory/Inventory.cs` - Singleton. Lista de NoteEntry. Polling de tecla I / Touchpad.
- `Assets/Scripts/Inventory/InventoryOverlay.cs` - UI con lista clickable.

### Scripts modificados

- `Assets/Scripts/Player/PlayerController.cs` - Aplica Settings en Start (mouseSensitivity, gamepadSensitivity, invertY). Metodo publico `ApplySettings()` para refrescar al cambiar desde Ajustes.
- `Assets/Scripts/Interaction/Note.cs` - Registra en Inventory al ser leida. NoteOverlay tiene parametro onClose para volver al inventario.
- `Assets/Scripts/Interaction/Flashlight.cs` - Aplica defaults visuales del Light en Awake (intensidad lumenes, range, spotAngle, color calido, soft shadows). Configurable desde Inspector del propio Flashlight, no del Light.
- `Assets/Scripts/Breathing/BreathingInputProvider.cs` - Persistencia de calibracion. ApplyCalibration y ForceFallback ahora guardan en PlayerPrefs. Metodos `HasStoredCalibration`, `TryLoadStoredCalibration`, `SaveCalibration`, `ClearStoredCalibration`.
- `Assets/Scripts/Breathing/MicCalibration.cs` - En Start verifica calibracion previa y la carga silenciosamente. Metodo publico `RestartCalibration` para re-calibrar desde Ajustes.
- `Assets/Scripts/Breathing/BreathingMinigame.cs` - Tutorial in-game con pausa, fade-in del canvas (CanvasGroup.alpha), badge del boton, modo "freeCycles" sin penalty al inicio.
- Multiples archivos: integran `OverlayBlocker.Register/Unregister` en sus Show/Hide para coordinar con PauseMenuHandler.

### Bugs encontrados y resueltos

- Conflicto Circle del mando: cerraba notas y a la vez activaba Crouch del PlayerController. Solucion: mover Crouch a R2 (rightTrigger), Circle queda libre para cerrar overlays.
- Pause se abria encima de Note/Lock al presionar Esc en esos overlays. Solucion: `OverlayBlocker` registry consultado por PauseMenuHandler antes de abrir.
- Pause con `canvas.enabled = false` dejaba botones interactables via EventSystem (especialmente al abrir Lock con Cross, EventSystem activaba el boton "Salir al menu" del pause). Solucion: usar `canvas.gameObject.SetActive(false)` para desactivar el GameObject completo, no solo el canvas.
- InventoryOverlay daba MissingReferenceException: guardabamos `transform` antes de `AddComponent<RectTransform>()`, lo que destruia el Transform original. Solucion: agregar RectTransform PRIMERO y guardar la referencia despues.
- MainMenu mostraba "Display 1 No camera rendering" porque borre la Main Camera. Solucion: MainMenuController auto-crea una Camera vacia (cullingMask=0) en Awake.
- Tutorial del minijuego no aparecia cuando el canvas tenia raycast/inputs activos antes de SetActive. Solucion: BuildUI fuerza `canvas.enabled = false` despues de construir, en vez de depender del flag visible que arranca en false.

---

## Estado del juego al cierre del domingo

**Bucle completo funcional desde el menu**:
1. MainMenu (boton Jugar) -> calibracion mic (solo primera vez por sesion) -> aula Dev_Henry.
2. Aula con sistemas activos: HUD de estres, gaze a la pizarra (Cubo.002) que dispara intrusion del Observador, minijuego de respiracion (con tutorial al primer disparo), 3 notas leibles, cerradura con codigo 7-4-2-9, linterna, sistema de apagon que se dispara al resolver la cerradura.
3. Pause menu (Esc/Options) -> Continuar / Notas (inventario de notas leidas) / Ajustes / Salir al menu.
4. Game Over al colapsar -> reinicia la escena.
5. Ajustes persistidos: volumen master, sensitivity mouse, sensitivity gamepad, invertir Y, re-calibrar mic.

**Definition of Done del miercoles - estado actual**:
- [x] Build .exe que arranca sin errores → PENDIENTE para martes/miercoles.
- [x] Main menu funcional con Jugar / Salir, navegable por teclado, mouse y gamepad.
- [ ] Pantalla de carga con 1 tip de regulación emocional → P1, pendiente.
- [x] Pantalla de calibración del microfono con fallback a tecla Espacio.
- [x] Una partida jugable de 30-45 minutos con principio, medio y fin → AULA + cerradura + apagon. Falta el "pasillo final" + outro narrativo.
- [x] Control dual funcional: el jugador puede completar la partida usando solo teclado/raton o solo gamepad.
- [x] Sistema de estres visible y reactivo a eventos.
- [x] Mecanica de respiracion por mic (con fallback teclado) que efectivamente baja el estres.
- [x] 1 puzzle de combinacion con su pista en notas del entorno.
- [x] 1 momento de intrusion (Observer).
- [x] 1 momento de oscuridad con linterna.
- [ ] Outro con el mensaje pedagogico → pendiente.
- [x] Game Over funcional con reinicio.
- [x] Changelog de la entrega.

---

## Pendientes para Lunes 27 y resto de la semana

### Crítico
- **Pasillo final + sala de outro**: el jugador resuelve la cerradura, se apaga la luz, ¿y despues qué? Hay que armar al menos un pasillo corto con cubos + 1 sala final con la nota outro y la metafora pedagogica.
- **Outro card** con el mensaje "No estaba en el aula. Estaba dentro de mi. Y ahora se respirar." Pantalla de texto + transicion a "Fin del Capitulo 1".
- **Build .exe de prueba** el lunes/martes para no llegar al miercoles con sorpresas.

### Polish
- **Loading screen + tip** entre menu y juego (1 tip fijo de respiracion 4-7-8 o anclaje 5-4-3-2-1).
- **Audio**: Carlos (audios del Observador, golpe del apagon, ambient layer adicional, audio guia para el minijuego de respiracion).
- **Posicionamiento fino** de notas y cerradura en pupitres especificos del aula. Ajustar posicion de la cerradura sobre la mesa del profesor.
- **Tipografia**: importar 1-2 fuentes de Google Fonts (Special Elite, Creepster) y aplicarlas a TMP para sustituir el Legacy Text.
- **Polish visual del minijuego**: el plan menciona vignette dinamica reactiva al estres, saturacion que cae, estatica TV en panico. P2 si queda tiempo.

### Limpieza pre-entrega
- Quitar `Debug Keys Enabled` del StressSystem (K/J/R debug).
- Quitar `Debug Log Targets` del InteractionSystem.
- Quitar `Log Target Changes` del GazeDetector.
- Verificar que `Cooldown Sec` del ObserverTrigger sea el valor final que decidamos (probablemente 30-60s o one-shot).

### Si Henry o Carlos entregan algo
- Henry: si entrega los assets de las intrusiones (Iracundo, Infante), agregarlas como decoracion en el slice. Si entrega audio del Observador, conectarlo al `IntrusionManager.OnObserver` UnityEvent.
- Carlos: si entrega audio guia para el minijuego de respiracion, integrarlo como AudioSource sincronizado con las fases.

---

## Notas para el equipo

- **Henry**: el aula esta lista en `Dev_Henry.unity`. Todos los sistemas funcionan. Si vas a agregar mas modelos o ajustar el aula, hazlo en esa escena. NO toques los GameObjects con prefijo `_` (`_Systems`, `_Breathing`, `_Events`) ni `IntrusionManager` ni `Cerradura_Cofre` ni las `Nota_*`. Si necesitas mover los sistemas de lugar visual, solo arrastrarlos en Hierarchy, no eliminar componentes.
- **Carlos**: el `IntrusionManager.OnObserver` UnityEvent en el GameObject `IntrusionManager` ya tiene un slot que dispara estres. Podes agregar otro slot (boton +) que reproduzca un AudioSource cuando tengas el SFX. Mismo patron para `OnIracundo` y `OnInfante` cuando esos disparen. Para el ambient layer, puedes cargar el clip al `SubliminalLoopController` que ya existe.
- **Duvan (yo)**: lunes/martes pulir slice (pasillo + outro), build de prueba, limpieza de debug keys. Si Carlos no entrega audio para el minijuego de respiracion, busco un metronomo en freesound.org.
