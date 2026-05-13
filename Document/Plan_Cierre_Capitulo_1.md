# Plan de Cierre - Capitulo 1

**Fecha del snapshot:** 2026-05-12
**Objetivo:** terminar el flujo completo end-to-end del Capitulo 1 para entrega.

---

## Estado actual (LO QUE ESTA HECHO)

### Audio (sistema robusto)
- `AudioManager` con auto-create de `AudioListener` (no falla aunque la escena no tenga uno)
- `Settings` (Master/Music/SFX/Cinematic) funcionales y conectados a sliders del menu
- `VideoCinematicPlayer` enrutado por `Settings.CinematicVolume`
- `AmbientLoopWithRandomEvents` con:
  - Brisa de fondo subliminal (loopVolume = 0.004)
  - Crujidos esporadicos (vol 0.85, intervalo 18-45s)
  - Auto-restart si Unity hace voice stealing
  - Forzado de volumen cada frame
- `AudioWireTool` (Tools > Estan Dentro > Wire Audio Clips) auto-asigna clips por convencion
- `SubliminalLoopController` en salonPrincipal **vaciado** (no era lo que queriamos)

### Audio wireado en componentes
- `MainMenuController`: UI Click, Hover, BackCancel (todos al mismo archivo de click)
- `Flashlight.toggleClip` - click on/off
- `CombinationLock`: digitClick + solvedClip + `PlayDigitClick()` invocado por `LockOverlay`
- `LockedDoor.lockedAudioClip` - jaloneo de chapa
  - **NUEVO COMPORTAMIENTO**: al intentar abrir locked dispara `BreathingMinigame.ForceShow()` por 15s + suma 25 stress (cooldown 8s)
- `HeavyProp.scrapeClip` - raspe al chocar
- `Note.pickupClip` - recoger nota
- `SimpleOpenable.openClip/closeClip` - reusa `LockedDoor jaloneo` (decision temporal)
- `EndGameTrigger.overrideVideoClip` - slot para reemplazar slides con video

### PlayerWakeUp (cinematica de despertar)
- Espera a que LoadingScreen termine
- Pestañeos pesados: blackHold 3s, 3 pestaneos con close 0.75s + open 1.6s + pause 1.0s entre ellos
- Volume HDRP en runtime con DOF + Vignette + LensDistortion + ChromaticAberration (priority 9999)
- Fade gradual del borde negro durante TODO el look around (no jump abrupto)
- Heartbeat (despertar) DESCARTADO del slot porque persistia indefinidamente
- `forcedBreathingSeconds = 0` (el minijuego de respiracion se dispara en LockedDoor ahora, no al despertar)

### Cinematicas (escenas dedicadas)
- `VideoCinematicPlayer.cs` componente reutilizable. Auto-crea Camera + AudioListener si la escena no los tiene.
- `Cinematic_Intro.unity` ya existe — wireado con `Completo musica saundtrak corto.mov`, carga `EscenaUno-salonPrincipal` al terminar.
- `DuctoTechoInteractable.nextSceneName = "Cinematic_Ductos"` (configurado)
- `ExitDuctsTrigger.nextSceneName = "Cinematic_SalasFinales"` (configurado, preserva NextSpawnPointId)
- `EndGameTrigger.nextSceneName = "Cinematic_Final"` (configurado)

### Sistema de Spawn
- `PlayerSpawnPoint` + `PlayerSpawner` + `GameSession.NextSpawnPointId`
- Spawn points soportan `onSpawnUsed` UnityEvent
- Skip wakeup configurable por spawn point

### Logica de gameplay
- `EndGameTrigger`: trigger del final con slides del twist + cierre de partida API
- `ExitDuctsTrigger`: trigger del final de los ductos
- `ObserverApparitionTrigger`: primera aparicion del Observador (audio + shake + ForceShow respiracion)
- `BreathingMinigame.ForceShow()` / `ForceHide()` publicos
- `SuffocationSystem` (jadeo + vignette + shake + colapso)

---

## TODO PARA MAÑANA

### Critico (necesario para flujo completo)

1. **Crear las 3 escenas de cinematica faltantes** (duplicando `Cinematic_Intro.unity`):
   - `Cinematic_Ductos.unity` -> clip `2.mov`, nextScene `EsecenaUno-DuctosDeVentilacion`
   - `Cinematic_SalasFinales.unity` -> clip `4.mov`, nextScene `EscenaUno-salonPrincipal`
   - `Cinematic_Final.unity` -> clip `5 v1.mov`, nextScene `MainMenu`

2. **Agregar las 3 escenas a Build Settings** (File > Build Settings)

3. **Crear 2 SpawnPoints en `EscenaUno-salonPrincipal.unity`**:
   - Empty GO `SpawnPoint_InicioSalon` en la pos inicial del Player -> `PlayerSpawnPoint` con id="inicio_salon", isDefault=true
   - Empty GO `SpawnPoint_SalasFinales` en la entrada de las salas finales (mirando hacia donde se entra) -> `PlayerSpawnPoint` con id="salas_finales", isDefault=false, skipWakeUp=true

4. **En el GO del Player**: Add Component `PlayerSpawner` si no esta.

### Tests end-to-end (probar flujo completo)

5. **Flujo full**: MainMenu -> Nueva Partida -> Cinematic_Intro -> salonPrincipal -> sube a ducto -> Cinematic_Ductos -> ductos -> sale -> Cinematic_SalasFinales -> salonPrincipal (spawn salas finales) -> trigger final -> Cinematic_Final -> MainMenu

6. **Probar mecanicas dentro de cada zona**:
   - Salon: tocar puertas cerradas dispara respiracion (verificar)
   - Casilleros simples abren con sonido
   - CombinationLock funciona
   - Linterna se prende/apaga con sonido
   - Notas se recogen
   - Brisa siempre presente, crujidos espaciados

### Pulido

7. **Si la brisa AUN se escucha fuerte con loopVolume=0.004**: el archivo `Brisa loop de fondo.mp3` tiene mucho nivel inherente. Opciones:
   - Normalizar el archivo en Unity (Inspector del .mp3, ajustar)
   - Reemplazar con uno mas suave de Freesound
   - O bajar `AudioListener.volume` via Master del menu

8. **Limpiar Debug.Log de diagnostico** en:
   - `PlayerWakeUp` ([WakeUp] logs)
   - `AmbientLoopWithRandomEvents` ([Ambient] logs)
   - `AudioManager` ([AudioManager] AudioListener.volume logs)

9. **Commit** del bloque completo (NO incluir `Assets/Art/cinematics/Esenas/` que pesa 4.6 GB)

### Optimizacion / nice-to-have (si hay tiempo)

10. **Locker open/close**: ahora reusa `LockedDoor jaloneo` para casilleros simples. Si Carlos consigue un audio dedicado de "metal locker creak", reemplazar.

11. **1.mov queda sin uso**. Decidir si se usa en algun momento o se descarta.

12. **mesaRodandoClip y destornilladorClip** del `DuctoTechoInteractable`: actualmente reusan `HeavyProp raspe` y `CombinationLock click digito` — funcionan pero se podria mejorar con audios dedicados.

---

## Audios disponibles en `Assets/Audio/Ambient/`

| Archivo | Uso actual |
|---|---|
| Brisa loop de fondo.mp3 | `AmbientLoop.loopClip` |
| Crujido 1.flac | `AmbientLoop.randomEventClips[0]` |
| Crujido 2.wav | `AmbientLoop.randomEventClips[1]` |
| Crujido 3.mp3 | `AmbientLoop.randomEventClips[2]` |
| Crujido (4 elemento) | `AmbientLoop.randomEventClips[3]` |
| Heartbeat (despertar).ogg | DESCARTADO del PlayerWakeUp |
| Musica menu loop.mp3 | `AudioManager.autoPlayMusicOnAwake` en MainMenu |
| UI Click.wav | UI Click + UI BackCancel (mismo archivo) |
| UI Hover.wav | UI Hover |
| LockedDoor jaloneo.ogg | LockedDoor + SimpleOpenable (reuso) |
| CombinationLock click digito.wav | Lock digit + DuctoTechoInteractable.destornillador (reuso) |
| CombinationLock solved.wav | Lock solved |
| Flashlight onoff.wav | Flashlight toggle |
| Pickup item.wav | Note pickup |
| HeavyProp raspe.wav | HeavyProp scrape + DuctoTechoInteractable.mesaRodando (reuso) |
| Gasp dramatico.mp3 | PlayerWakeUp.gaspClip |
| ductoCayendoClip.ogg | DuctoTechoInteractable.ductoCayendo |
| enterStinger.flac | ExitDuctsTrigger.enterStinger + ObserverApparition.stinger |
| breath_man.flac, breath_woman.wav | SuffocationSystem (jadeo) |
| seat_belt.wav | (no wireado todavia) |

---

## Cinematicas .mov en `Assets/Art/cinematics/Esenas/` (4.6 GB total - NO COMMITEAR)

| Archivo | Uso |
|---|---|
| Completo musica saundtrak corto.mov (1.6 GB) | **Intro** - cuenta toda la historia previa al juego |
| 2.mov (256 MB) | Cinematic_Ductos (entrada a ductos) |
| 4.mov (82 MB) | Cinematic_SalasFinales (salida a salas finales) |
| 5 v1.mov (137 MB) | Cinematic_Final (twist del final) |
| 1.mov, 5 v2o.mov, Completo soundtrack largo | Sin uso confirmado |

---

## Flujo final esperado del Capitulo 1

```
MainMenu (musica menu + UI clicks)
  -> Nueva Partida
  -> [LoadingScreen 4.5s]
  -> Cinematic_Intro (Completo soundtrack corto.mov - cuenta el porque del personaje)
  -> [LoadingScreen]
  -> EscenaUno-salonPrincipal (spawn "inicio_salon")
       PlayerWakeUp: black hold + pestaneos pesados + look around con borde negro fade
       Ambient: brisa subliminal + crujidos cada 18-45s
       Player explora, intenta puertas cerradas -> respiracion se dispara
       Player toca casilleros, recoge notas, resuelve combinationLock
       Player encuentra destornillador
       Player sube al ducto del techo
  -> [LoadingScreen]
  -> Cinematic_Ductos (2.mov)
  -> [LoadingScreen]
  -> EsecenaUno-DuctosDeVentilacion
       Player gatea por ductos
       (sistema de Observador puede aparecer aqui en Sprint 3)
       Player llega al ExitDuctsTrigger al final
  -> [LoadingScreen]
  -> Cinematic_SalasFinales (4.mov)
  -> [LoadingScreen]
  -> EscenaUno-salonPrincipal (spawn "salas_finales", skipWakeUp=true)
       Player explora las salas finales
       Player llega al EndGameTrigger
  -> [LoadingScreen]
  -> Cinematic_Final (5 v1.mov - twist: padre vivo, tu moriste)
  -> [LoadingScreen]
  -> MainMenu
```

---

## Riesgos / atajos tecnicos pendientes

- **Time.timeScale = 0**: muchos componentes lo activan (Inventory, Note, Pause, Settings, etc.). El `AmbientLoopWithRandomEvents.RandomEventsRoutine` usa `WaitForSeconds` que respeta timeScale. Si el jugador abre un overlay, los crujidos se pausan (intencional, no es bug).
- **VolumeProfile HDRP runtime**: el blur del despertar puede competir con el global Volume del proyecto si este tiene priority mayor. Por eso usamos priority=9999.
- **VideoCinematicPlayer.SetTargetAudioSource** enruta el audio del video por un AudioSource controlado por Settings.CinematicVolume, no por Direct.
- **Master Volume**: el usuario tenia 0.34 en PlayerPrefs (debe ajustar en Ajustes al maximo).

