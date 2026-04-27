<<<<<<< HEAD
# 003 - Loop Subliminal del Accidente

**Fecha:** 2026-04-15
**Sprint:** 1 - Fundamentos y Setup
**Responsable:** Duvan Lozano
**Rama:** `feature/subliminal-audio`

## Qué se hizo

Implementación de la **segunda capa sonora** definida en el GDD: el eco subliminal del accidente a −20 dB (solo audible con auriculares).

- `MainMixer.mixer` creado en `Assets/Audio/` con grupos: `Master → Ambient, Subliminal, SFX, Music`. El grupo **Subliminal está atenuado a -20 dB** (valor fijo del GDD).
- Script `SubliminalLoopController.cs` en `Assets/Scripts/Audio/` que gestiona múltiples capas de audio en paralelo:
  - Cada capa es un `AudioSource` independiente enrutado al grupo `Subliminal` del mixer.
  - Modulación de volumen suave (continuamente varía entre `volumenBase ± variacionVolumen`) para evitar que suene mecánico.
  - Variación de pitch aleatoria pequeña (±2–4%) al iniciar cada loop.
  - Soporta capas en loop continuo (respiración, loops subliminales) y capas con pausas aleatorias entre repeticiones (goteo irregular).
  - Fade-in de 3 segundos al iniciar.
- Prefab configurado en `Dev_Duvan.unity` con 4 capas:
  1. `loop subliminal.wav` (loop continuo, base)
  2. `loop_subliminal1.wav` (loop continuo, variante para romper repetición)
  3. `breath_man.flac` (respiración, loop continuo)
  4. `Irregular_drip.flac` (goteo, pausas aleatorias 6–15s)

## Archivos creados o modificados

- `Assets/Audio/MainMixer.mixer` (nuevo)
- `Assets/Scripts/Audio/SubliminalLoopController.cs` (nuevo)
- `Assets/Scenes/Dev/Dev_Duvan.unity` (modificado — añadido GameObject `SubliminalLoop`)
- `Document/Changelog/003_Loop_Subliminal_Accidente.md` (nuevo)

## Notas para el equipo

- **Carlos:** cuando hagas mezclas nuevas de loop subliminal, pueden sustituir los clips en el Inspector del prefab sin tocar código. Si necesitas tic-tac del intermitente o crujido metálico en archivos separados, se agregan como nueva capa (`Size` del array Capas → +1).
- **Henry:** el mixer ya tiene grupos para todo (Ambient, Subliminal, SFX, Music). Cuando coloques audio ambiental del Salón 4-B, rutéalos al grupo `Ambient` desde el AudioSource (campo **Output**).
- **Todos:** el loop subliminal está pensado para sonar **continuamente mientras el jugador esté en el Salón 4-B**. Cuando tengamos la escena definitiva se instancia el prefab ahí. Para probar en tu escena Dev, usa el GameObject de `Dev_Duvan` como referencia.
- Se recomienda **probar siempre con auriculares**, por parlantes no se percibe.
=======
# 003 - Loop Subliminal del Accidente

**Fecha:** 2026-04-15
**Sprint:** 1 - Fundamentos y Setup
**Responsable:** Duvan Lozano
**Rama:** `feature/subliminal-audio`

## Qué se hizo

Implementación de la **segunda capa sonora** definida en el GDD: el eco subliminal del accidente a −20 dB (solo audible con auriculares).

- `MainMixer.mixer` creado en `Assets/Audio/` con grupos: `Master → Ambient, Subliminal, SFX, Music`. El grupo **Subliminal está atenuado a -20 dB** (valor fijo del GDD).
- Script `SubliminalLoopController.cs` en `Assets/Scripts/Audio/` que gestiona múltiples capas de audio en paralelo:
  - Cada capa es un `AudioSource` independiente enrutado al grupo `Subliminal` del mixer.
  - Modulación de volumen suave (continuamente varía entre `volumenBase ± variacionVolumen`) para evitar que suene mecánico.
  - Variación de pitch aleatoria pequeña (±2–4%) al iniciar cada loop.
  - Soporta capas en loop continuo (respiración, loops subliminales) y capas con pausas aleatorias entre repeticiones (goteo irregular).
  - Fade-in de 3 segundos al iniciar.
- Prefab configurado en `Dev_Duvan.unity` con 4 capas:
  1. `loop subliminal.wav` (loop continuo, base)
  2. `loop_subliminal1.wav` (loop continuo, variante para romper repetición)
  3. `breath_man.flac` (respiración, loop continuo)
  4. `Irregular_drip.flac` (goteo, pausas aleatorias 6–15s)

## Archivos creados o modificados

- `Assets/Audio/MainMixer.mixer` (nuevo)
- `Assets/Scripts/Audio/SubliminalLoopController.cs` (nuevo)
- `Assets/Scenes/Dev/Dev_Duvan.unity` (modificado — añadido GameObject `SubliminalLoop`)
- `Document/Changelog/003_Loop_Subliminal_Accidente.md` (nuevo)

## Notas para el equipo

- **Carlos:** cuando hagas mezclas nuevas de loop subliminal, pueden sustituir los clips en el Inspector del prefab sin tocar código. Si necesitas tic-tac del intermitente o crujido metálico en archivos separados, se agregan como nueva capa (`Size` del array Capas → +1).
- **Henry:** el mixer ya tiene grupos para todo (Ambient, Subliminal, SFX, Music). Cuando coloques audio ambiental del Salón 4-B, rutéalos al grupo `Ambient` desde el AudioSource (campo **Output**).
- **Todos:** el loop subliminal está pensado para sonar **continuamente mientras el jugador esté en el Salón 4-B**. Cuando tengamos la escena definitiva se instancia el prefab ahí. Para probar en tu escena Dev, usa el GameObject de `Dev_Duvan` como referencia.
- Se recomienda **probar siempre con auriculares**, por parlantes no se percibe.
>>>>>>> 0a110bca3c7d383d84ab6cc6c8aec8abc0cf9a75
