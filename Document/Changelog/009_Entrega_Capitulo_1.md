# 009 — Entrega Capitulo 1

**Fecha:** 2026-05-13
**Estado:** Entregado a las ~15:30 (deadline 15:00, 30 min de overshoot por polish final)
**Branch:** main
**Commit final:** `a2bdb60`

---

## Resumen

Sesion completa de cierre del Capitulo 1 desde "flujo end-to-end roto" hasta entrega
funcional con polish exhaustivo. Se hizo refactor de mecanicas core (respiracion),
creacion de sistemas de tension (oscuridad, golpes, gateo) y armado del final
narrativo (puzzle de puertas + creditos).

## Commits del dia (orden cronologico)

1. `a233d2f` — Safety commit base: flujo end-to-end funcional del Capitulo 1 + .gitignore para 4.6 GB de .mov
2. `f852275` — Espacio mental + UI menus + mecanicas de tension (gran refactor visual)
3. `007a7d8` — Quitar EscenaUno-salonSecundario fantasma del EditorBuildSettings + warnings silenciados
4. `87724ea` — Build settings: quitar referencia a escena eliminada
5. `e3f4a13` — CombinationLock.disableOnSolved[] (quick wire de puertas)
6. `e509fde` — Patron loquer: LockedDoor integra CombinationLock en mismo GO
7. `ec983e7` — Animacion por codigo como fallback cuando no hay Animator
8. `1b874fc` — Rotacion por codigo configurable en cualquier eje (local o mundo)
9. `a2bdb60` — Wireado final del puzzle de puertas (entrega)

## Sistemas nuevos / refactorizados

### Espacio Mental (BreathingMinigame redesign completo)
- Ventana etera (viñeta radial, sin caja rectangular) reemplaza overlay azul
- Recorrido de 3 nodos en curva variada (4 formas randomizadas por apertura: sinusoide, zigzag, arco, espiral S)
- Orbe pulsante sigue la respiracion (inhala 3.5s / exhala 3s / pausa 1s)
- Modo MEDITACION GUIADA: no requiere input por ciclo, solo presionar X en cada nodo
- Lock total del player durante minijuego (movement + camara + mouse + control)
- HDRP DepthOfField + Vignette para desenfocar mundo detras (efecto inventario)
- Audio: loop ambiental con fade in/out 1.2s + clip de avance + clip de completado
- Cooldown post-cierre de 6s evita re-trigger inmediato
- Pausa inicial de 1.5s al abrir (transicion natural)
- Stress reset a 0 al completar (alivio total post-meditacion)
- DontDestroyOnLoad: persiste entre escenas (funciona en salonPrincipal y Ductos)
- OverlayBlocker bloquea interacciones de mundo durante minigame
- Boton hint dorado pulsante al lado del orbe cuando avanzable
- Path se randomiza cada apertura para variedad

### Warning Screen (pre-intro)
- Pantalla con fade in/hold/fade out antes del Cinematic_Intro
- Texto educativo de salud mental + linea 192 opc. 4 / 106 Bogota
- Flash subliminal configurable en posicion superior ("ya estan dentro")
- Auto-crea camara dummy para evitar "Display 1 No cameras rendering"

### Credits Screen
- Mensaje reflexivo previo sobre respirar y pedir ayuda
- Linea 192 opc. 4 / 106 Bogota explicita
- Creditos del equipo: Duvan / Henry / Carlos
- Fade in/hold/fade out por fase, skippeable
- Auto-disparado por EndGameTrigger despues del video del twist

### Mecanicas de tension
- **FlashlightFlicker**: linterna parpadea/falla en Ductos, +5 stress por fallo
- **DarknessStress**: estres sube +2/s mientras la linterna esta apagada (cap 35)
- **EnvironmentDarknessFear**: trigger manual o auto (via Lights[]) para puzzles donde se apagan luces
- **DuctThumps**: golpes externos random al ducto cada 15-40s con camera shake + 6 stress
- **CrawlFootsteps**: pasos al gatear, volumen y ritmo escalan con velocidad del CharacterController
- **StressSystem.passiveRisePerSecond**: tension constante en escenas como Ductos (+0.5/s = +5 cada 10s)
- **StressSystem.logStressChanges**: logging para diagnostico

### Patron Loquer (LockedDoor + CombinationLock integrados)
- LockedDoor detecta CombinationLock en mismo GO o hijos (campo integratedLock)
- autoBindIntegratedLock: en Awake suscribe onSolved → Unlock automaticamente
- Al interactuar con puerta locked, se abre directamente el UI del candado (sin GO separado)
- Animacion por codigo como fallback cuando no hay Animator: rotacion configurable (X/Y/Z, local o mundo)

### UI menus y gamepad
- MainMenuController: re-seleccion via coroutine de 2 frames (fix focus gamepad post-overlay)
- Auto-restore EventSystem selection cuando mouse deselect + gamepad input
- Audio en tabs de SettingsOverlay + back sound en X/ESC

### OverlayBlocker / Interactions
- InteractionSystem ignora input cuando OverlayBlocker.IsBlocking
- LockedDoor.Interact() y CombinationLock.Interact() bailout si overlay activo

### ObjectiveOnSceneLoad
- Componente para mostrar mensaje en ObjectiveHUD al cargar escena (Ductos, salas finales)
- Una vez por sesion, con delay configurable

## Bugfixes importantes

- AmbientLoopWithRandomEvents.fadeInSeconds ahora se respeta (no arranca de golpe)
- VideoCinematicPlayer.SilenceSceneAudio: silencia ambient + flashlight flicker + AudioSources durante el video
- PlayerWakeUp.AbortAndCleanup: destruye overlay cuando SkipWakeUp=true (evitaba pantalla negra al regresar a salas finales)
- DntDestroyOnLoad de BreathingMinigame permite que funcione cross-scene
- Build Settings: removida referencia a salonSecundario.unity (escena eliminada)
- Warnings CS0414 silenciados con pragma

## Estado final del flujo del Capitulo 1

```
MainMenu (UI con audio, gamepad funcional)
  └→ Nueva Partida
  └→ Cinematic_Intro (warning screen + subliminal → video soundtrack)
  └→ salonPrincipal Acto 1 (despertar + exploracion + puzzle + ducto)
       └→ video 2.mov overlay (flashback accidente)
  └→ Ductos (gateo + thumps + flicker + ambient + minigame respiracion si stress)
       └→ video 4.mov overlay (segundo flashback)
  └→ salonPrincipal Acto 3 / salas finales (spawn salas_finales, skipWakeUp)
       └→ puzzle: puertas con codigo 1705 (patron loquer)
       └→ EndGameTrigger
       └→ video 5 v1.mov (twist final)
       └→ CreditsScreen (mensaje reflexivo + equipo)
  └→ MainMenu
```

## Codigo final del puzzle

**Combinacion: `1705`** (fecha del accidente, dia/mes).

## Pendientes documentados (post-entrega)

- Animator Controller real con bool "Open" en las puertas (ahora usa fallback de rotacion por codigo)
- Refactor del Observer Apparition (Sprint 3)
- Calibracion real del microfono (actualmente fallback teclado funciona pero mic no calibra por gap insuficiente)
- Polish narrativo de pizarron y notas distribuidas para que el codigo 1705 sea genuinamente deducible del entorno (hoy se da hint directo en la nota final)
- Verificar wireado del Audio de la respiracion para que entre suave
- 4.6 GB de cinematicas .mov se distribuyen por separado (Drive/Onedrive), NO estan en git

## Resultado

Entrega funcional con polish significativo en mecanicas de tension y minijuego de
respiracion. Buffer de tiempo se gasto en redisenos profundos pero estables. Push
final en `a2bdb60`. Build paso sin errores tras quitar referencia fantasma del
EditorBuildSettings.
