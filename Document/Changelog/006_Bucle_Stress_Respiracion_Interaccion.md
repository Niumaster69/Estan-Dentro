# 006 - Bucle Stress + Respiracion + Interaccion + Linterna

**Fecha:** 2026-04-26 (domingo)
**Sprint:** Plan de entrega final (capitulo 1 jugable, miercoles 29)
**Responsable:** Duvan Lozano + Claude
**Rama sugerida:** `feature/breathing-and-interaction`
**Tarea relacionada:** PLAN_ENTREGA_FINAL.md - bloques 1 a 5 del domingo

---

## Que se hizo

Dia critico del plan. Se construyo el bucle nucleo del juego de punta a punta dentro de una escena de pruebas (`Dev_Duvan.unity`), porque `Chapter1_Salon4B` tiene el modelo del aula sin colliders y el Player se caia al vacio. Migracion al aula real queda para el lunes 27.

### Bloques completados

1. **Input dual** - Action Maps de Input System extendidos con Flashlight (F / Square), Pause (Esc / Options), BreatheFallback (Space). Look separado en `Look` (mouse) + `LookGamepad` (stick derecho) con sensitivities propias. Sprint y Crouch se manejan via polling porque Send Messages no propaga el release de buttons. Velocidades de horror (walk 2.8, sprint 4.5, crouch 1.4).
2. **StressSystem + HUD** - Medidor 0-100 con barra horizontal en esquina inferior izquierda (Canvas + UI por codigo, sin TMP). Tecla K +10 / J -10 / R reset (debug). Color verde-amarillo-rojo segun nivel. Pulso vertical cuando sobre 70%. Singleton via `Instance`. Eventos C# `OnStressChanged` y `OnCollapse`.
3. **Calibracion mic + Respiracion** - Pantalla de calibracion al iniciar (3s ruido base + 3s exhale). Si `gap exhale - noise < 0.005` cae a fallback teclado. Minijuego: circulo pulsante 4-4-2 (inhalar / exhalar / pausa). En modo mic baja estres por exhale RMS sostenido 2s. En fallback baja estres si Triangle/Space sostenido en INHALA + soltado en EXHALA. Falla suma +1, exito resta -9.
4. **Interaccion + Linterna** - Reticle dinamico que cambia al apuntar a un `Interactable`. Cubo de prueba con `Note` que abre overlay UI con titulo + cuerpo + hint. Spot Light hijo de la camara con `Flashlight.cs` (toggle F/Square, parametro `unlocked` para narrative gating). `BlackoutEvent.cs` apaga todas las luces de la escena excepto las que tengan `Flashlight` y desbloquea las linternas (tecla B para test).
5. **Bucle completo** - `IntrusionManager` + `ObserverTrigger` (cubo Pizarra_Test) + cableado UnityEvent `onObserver` -> `StressSystem.Add(40)` desde Inspector. Validado: mirar pizarra 3.5s -> intrusion -> estres 40 -> circulo aparece -> respirar baja a 0.

### Scripts creados

- `Assets/Scripts/Stress/StressSystem.cs` - Singleton del medidor de estres. Soporta passive decay opcional, debug keys K/J/R, eventos C#.
- `Assets/Scripts/UI/HUDBootstrapper.cs` - Construye el Canvas + barra de estres por codigo en Awake. Suscribe al OnStressChanged.
- `Assets/Scripts/Breathing/BreathingInputProvider.cs` - Abstrae mic vs teclado. Calibra threshold por amplitud RMS. Resolucion de device por substring (ej `Wireless Controller`). Fallback automatico si gap insuficiente.
- `Assets/Scripts/Breathing/MicCalibration.cs` - Pantalla overlay que pausa el juego (Time.timeScale = 0). 3 fases: WaitStart, CaptureNoise, CaptureExhale. Esc -> fallback inmediato.
- `Assets/Scripts/Breathing/BreathingMinigame.cs` - Circulo pulsante con sprite generado por codigo. Aparece cuando estres >= 40, oculto cuando <= 25 (histeresis). En fallback exige inhaleHeldAccum >= 3s para validar el ciclo.
- `Assets/Scripts/Interaction/InteractionSystem.cs` - Raycast desde camara con offset adelante para evitar auto-colision. Reticle UI por codigo. Polling de E/Cross para Interact.
- `Assets/Scripts/Interaction/Note.cs` - Contiene `Interactable` (abstract base), `Note` (concrete) y `NoteOverlay` (singleton UI que pausa el juego con Time.timeScale=0).
- `Assets/Scripts/Interaction/Flashlight.cs` - Toggle del Spot Light. Parametros `unlocked` y `startEnabled` para narrative gating.
- `Assets/Scripts/Interaction/BlackoutEvent.cs` - Singleton. `TriggerBlackout()` apaga luces excepto Flashlight. Tecla B para test. Evento C# OnBlackout.

### Scripts modificados

- `Assets/Scripts/Player/PlayerController.cs` - Velocidades de horror, sensitivity dual mouse/stick, OnInteract/OnFlashlight/OnPause/OnBreatheFallback, polling para Sprint/Crouch (Send Messages no propaga release).

### Recursos modificados

- `Assets/InputSystem_Actions.inputactions` - Acciones nuevas Flashlight, Pause, BreatheFallback, LookGamepad. Action Jump removida (libera Space). Interact gamepad pasa de Triangle a Cross.

---

## Pivotes del dia (cambios respecto al plan original)

1. **Escena de pruebas Dev_Duvan en lugar de Chapter1_Salon4B**: el modelo del aula vino de Blender sin Mesh Collider. Player se caia al vacio. El plan ya preveia construir todo en Dev_Duvan y migrar al aula el lunes, asi que solo se adelanto. Decision: arreglar colliders del aula el lunes 27.
2. **Fallback teclado como modo principal** (registrado en `PLAN_ENTREGA_FINAL.md` seccion "Pivote 2026-04-26 (tarde)"). Casco no funciono (Windows mezcla con Realtek), DualSense en Unity da RMS 0.00001 (probable captura exclusiva por otra app). Sistema sigue intentando mic automaticamente al iniciar - si funciona en otra maquina, se usa. Si no, fallback con timing estricto.
3. **Triangle como input de respiracion en mando** (no Cross). Cross ya estaba para Interact y habria conflicto al apuntar a notas durante el ciclo respiratorio.
4. **Cooldown de Observer Trigger subido a 60s** para tests. En el juego real (lunes) hay que decidir entre one-shot, cooldown largo, o intrusion narrativa que cambia con cada disparo.

---

## Guia de instalacion (estado al cierre del domingo)

Escena: `Assets/Scenes/Dev_Duvan.unity`. Hierarchy esperada:

```
Piso (Plane con MeshCollider)
Player
  └ Main Camera (con AudioListener + GazeDetector)
    └ Flashlight (Spot Light + Flashlight.cs)
_Systems (StressSystem + HUDBootstrapper + InteractionSystem)
_Breathing (BreathingInputProvider + MicCalibration + BreathingMinigame)
_Events (BlackoutEvent)
IntrusionManager (IntrusionManager.cs)
Pizarra_Test (Cube grande + ObserverTrigger, onObserver -> StressSystem.Add(40))
Nota_Test (Cube pequeno + Note.cs)
Directional Light
Sky and Fog Volume (HDRP - desactivado para que el blackout funcione bien)
```

Configuracion clave:
- `BreathingInputProvider.preferredDeviceName` = `Wireless Controller` (substring match).
- `BreathingInputProvider.minThresholdGap` = `0.005`.
- `BreathingMinigame.exhaleMinSustainSeconds` = `2`.
- `BreathingMinigame.minInhaleHeldSeconds` = `3`.
- `BreathingMinigame.stressUpOnFail` = `1`.
- `ObserverTrigger.cooldownSec` = `60` (test) - el lunes decidir politica final.

---

## Controles validados

| Accion | Teclado | Mando |
|---|---|---|
| Caminar | WASD | Stick izquierdo |
| Mirar | Mouse | Stick derecho |
| Sprint (hold) | LeftShift | L3 |
| Crouch (hold) | C | Circle |
| Interact | E | Cross |
| Linterna | F | Square |
| Pause | Esc | Options |
| Respirar - mantener en INHALA | Space | Triangle |
| Respirar - soltar en EXHALA | (soltar Space) | (soltar Triangle) |
| Debug subir estres | K | - |
| Debug bajar estres | J | - |
| Debug reset estres | R | - |
| Debug blackout | B | - |

---

## Bugs encontrados y resueltos

- Mesh Collider faltante en aula -> migracion a Dev_Duvan.
- 2 AudioListeners en escena -> identificado y eliminado el residual.
- Input System Send Messages no propaga release de Button -> Sprint/Crouch a polling.
- BreathingMinigame canvas visible al inicio -> Awake fuerza canvas.enabled=false explicitamente.
- InteractionSystem raycast chocaba con el Player -> originForwardOffset = 0.2.
- Calibracion mic con DualSense daba gap negativo -> threshold bajado a 0.005 + fallback automatico.
- Fallback teclado trivial (no apretar nada = ✓) -> exige inhaleHeldAccum >= 3s en INHALA antes de validar EXHALA.
- ScoreFail rapido + intrusion frecuente colapsaba el estres en 14s -> cooldown del Observer subido a 60s + stressUpOnFail bajado a 1.
- StressSystem.IsCollapsed bloqueaba toda interaccion -> Add con delta negativo des-colapsa.

---

## Pendientes para Lunes 27

Bloque 5 / Lunes (cerradura + escena real) segun plan, mas deuda acumulada del domingo:

### Migracion a escena real
- Arreglar Mesh Colliders del modelo del aula en `Chapter1_Salon4B` (Mesh Collider en raiz o submeshes).
- Mover `_Systems`, `_Breathing`, `_Events`, `IntrusionManager`, `Flashlight` (en camara), 2-3 notas, 1 cerradura al aula.
- Usar la pizarra real del aula (Opcion B del changelog 005) en vez del cubo Pizarra_Test.

### Cerradura por combinacion (planeado para lunes)
- `CombinationLock.cs` con UI procedural de 3-4 ruedas de digitos.
- Mouse / Enter para teclado, D-pad + Cross para mando.
- Al acertar dispara `BlackoutEvent.Instance.TriggerBlackout()`.

### UX del minijuego (deuda critica)
- Tutorial in-game al primer disparo de la intrusion (overlay pausa con instruccion del control).
- Animacion de fade-in del circulo (no aparicion de golpe).
- Iconos del boton (Triangle / Space) visibles junto al circulo en vez de solo texto.
- 2-3 ciclos guiados sin penalty al inicio.
- Audio guia (martes).

### Polish y limpieza
- Quitar `Debug Keys Enabled` del StressSystem antes de la entrega.
- Quitar `Log Target Changes` del GazeDetector y `Debug Log Targets` del InteractionSystem.
- Decidir politica del Observer Trigger en el aula: one-shot, cooldown largo, o intrusion evolutiva.

### Pantalla de Game Over real
- "Te dejaste llevar. Respira." + boton de reiniciar escena.
- En el plan estaba en P0, queda para Lunes/Martes.

---

## Notas para el equipo

- **Henry**: el lunes 27 vamos a tu escena `Chapter1_Salon4B`. Necesitamos los Mesh Colliders del aula (en la raiz o en los submeshes de piso/paredes). Si la pizarra esta separada como submesh, conviene aplicarle el ObserverTrigger directamente (Opcion B del changelog 005).
- **Carlos**: pendiente para martes - audio guia del minijuego de respiracion. Sonido sutil que marque inhale/exhale (estilo metronomo o respiracion). Tambien quedan los SFX del Observador y del apagon (golpe seco + zumbido).
- **Duvan (yo)**: lunes arrancar con migracion al aula + cerradura. Antes del fin del lunes hacer un build de prueba para no llegar al miercoles con sorpresas.

---

## Referencias

- `Document/PLAN_ENTREGA_FINAL.md` - plan maestro con los 2 pivotes del 26.
- Changelog 005 - skeleton del IntrusionManager + GazeDetector + ObserverTrigger.
- Memoria del proyecto: `~/.claude/.../memory/project_estan_dentro.md`.
