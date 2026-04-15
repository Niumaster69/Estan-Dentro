# 002 - FPS Controller

**Fecha:** 2026-04-15
**Sprint:** 1 - Fundamentos y Setup
**Responsable:** Duvan Lozano
**Rama:** `feature/fps-controller`

## Qué se hizo

- Script `PlayerController.cs` en `Assets/Scripts/Player/` con:
  - Movimiento WASD usando `CharacterController`.
  - Cámara con mouse (yaw en el cuerpo, pitch en pivote de cámara, clamp -85° / 85°).
  - Sprint (LeftShift) y Crouch (Ctrl) — la altura del controller se interpola entre 1.8 m y 1.0 m.
  - Gravedad simple (-20) y snap al suelo cuando está grounded.
  - Cursor bloqueado al centro de la pantalla al iniciar.
- Se usa el asset `InputSystem_Actions.inputactions` existente (Move, Look, Sprint, Crouch) vía `PlayerInput` en modo **Send Messages**.
- Valores por defecto razonables en Inspector (walk 4, sprint 7, crouch 2, sensibilidad 0.15). Se ajustarán más adelante desde menú de opciones.

## Archivos creados o modificados

- `Assets/Scripts/Player/PlayerController.cs` (nuevo)
- `Assets/Scenes/Dev/Dev_Duvan.unity` (modificado — Duvan arma el prefab Player)
- `Document/Changelog/002_FPS_Controller.md` (nuevo)

## Notas para el equipo

- **Henry:** cuando tengas el blockout del Salón 4-B, usa este Player como referencia de escala (1.8 m de altura). El Player ya soporta subir desniveles pequeños via CharacterController.
- **Carlos:** los sonidos de footsteps entran en Sprint 2, no tocar este script aún.
- **Todos:** no editar `PlayerController.cs` sin avisar. Si hay que añadir algo (linterna, interacción), se hace en scripts aparte.
