# Brief para Carlos — Audio y Cinemáticas

**Proyecto:** Están Dentro – Lo que no puedo ver (Capítulo 1)
**Fecha:** 2026-04-26
**Entrega:** miércoles 29 de abril
**Para:** Carlos (Narrativa, Audio, Cinemáticas)

---

## Cómo usar este documento

Este doc tiene 3 secciones:
1. **Lista de audios** — todo lo que necesitamos sonoramente. Cada item con descripción de cuándo se dispara, mood, duración estimada y referencia.
2. **Cinemáticas** — qué cinemáticas necesitamos (intro, tutorial respiración, posibles flashbacks, outro), con guion sugerido.
3. **Mood y referencias** — estilo sonoro y visual del juego.

Si encontrás algo bueno en freesound.org / pixabay / zapsplat / OpenGameArt, traélo. Si vas a crear algo desde cero, mejor.

**Formato preferido:** `.wav` o `.ogg`, mono o stereo según indique cada item, 44.1 kHz.

---

## Sección 1 — AUDIO (lista completa)

### A. AMBIENT (capas continuas, MUY bajo volumen)

| ID | Nombre | Descripción | Tipo | Duración | Mood / referencia |
|---|---|---|---|---|---|
| A01 | `ambient_aula_base` | Rumor de aula vacía. Edificio antiguo crujiendo lejano. Apenas perceptible. Reemplaza al SubliminalLoopController actual. | Loop | 60-90s loopeable | Outlast cuando estás en pasillos vacíos (silencio NO total, presencia sutil). Volumen final: -28 dB. |
| A02 | `ambient_blackout` | Zumbido grave + electricidad muerta. Cuando se va la luz tras la cerradura. | Loop | 30-60s loopeable | Visage en el sótano. Resident Evil 7 generador apagado. Volumen: -18 dB. |

### B. STINGERS (eventos puntuales aleatorios, durante exploración)

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| B01 | `stinger_crujido_madera` | Crujido sutil de madera (silla, pupitre, piso). | One-shot | 0.5-1.5s | Random cada 30-90s mientras el jugador camina. |
| B02 | `stinger_gota_agua` | Una gota cayendo en algún lugar. | One-shot | 0.4-0.8s | Random cada 60-120s. |
| B03 | `stinger_tic_reloj` | Un solo tic-tac de reloj viejo, fuera de ritmo. | One-shot | 0.2s | Random cada 20-40s, más frecuente cuando estrés > 40. |
| B04 | `stinger_papel` | Un papel moviéndose lejos (roce). | One-shot | 1-2s | Random cada 60-90s. |

### C. INTRUSIONES Y SOBRESALTOS

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| C01 | `intrusion_observer_susurro` | Susurro indistinguible (palabras enredadas, voz femenina o masculina muy procesada). | One-shot | 2-3s | Cuando estrés > 60. Volumen medio. Sutil. |
| C02 | `intrusion_observer_aparicion` | Sonido frío al disparar la intrusión del Observador. Tipo "vidrio rajándose lento" + susurro. | One-shot | 1.5-2.5s | Al disparar `IntrusionManager.OnObserver` (mirada sostenida en pizarra). **CRÍTICO.** |
| C03 | `intrusion_observer_pasos` | Pasos lentos detrás del jugador. Stereo paneado (alterna izq/der según se mueve la cámara). | One-shot o loop corto | 4-6s | Mientras el Observador está activo (cooldown). Sutil pero presente. |
| C04 | `intrusion_iracundo_metal` | (Para futuro) Marco metálico raspando. Para la intrusión del Iracundo, si se implementa. | One-shot | 1-2s | Pendiente Sprint posterior. |
| C05 | `intrusion_infante_cancion` | (Para futuro) Canción infantil distorsionada (cuna o "estrellita"). Para el Infante. | Loop corto | 8-15s | Pendiente Sprint posterior. |

### D. INTERACCIÓN

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| D01 | `cerradura_click_digito` | Click mecánico ligero al cambiar un dígito de la cerradura. | One-shot | 0.1s | Al mover D-pad / flechas en LockOverlay. |
| D02 | `cerradura_acierto` | Click metálico fuerte + "se traba positivamente". | One-shot | 0.4-0.8s | Al confirmar código correcto. |
| D03 | `cerradura_fallo` | Buzzer corto / sonido seco negativo. | One-shot | 0.3s | Al confirmar código incorrecto. |
| D04 | `nota_papel_tomar` | Crujido de papel al tomar/leer una nota. | One-shot | 0.5s | Al abrir NoteOverlay. |
| D05 | `nota_papel_cerrar` | Sonido más sutil de papel doblándose. | One-shot | 0.3s | Al cerrar NoteOverlay. |
| D06 | `linterna_click` | Click de linterna al encender/apagar. | One-shot | 0.15s | Al togglear Flashlight. |

### E. APAGÓN (CRÍTICO)

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| E01 | `apagon_golpe` | Golpe seco + interruptor + parpadeo eléctrico breve. | One-shot | 0.8-1.2s | Al disparar BlackoutEvent (resolver cerradura). **CRÍTICO.** |
| E02 | `apagon_zumbido` | Empieza el zumbido de A02 después del golpe. | (Ver A02) | - | Disparado junto con E01. |

### F. JUGADOR Y CUERPO

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| F01 | `pasos_jugador_normal` | Pasos sobre piso de aula (madera o cerámica). 4-6 variaciones para no repetir. | One-shot x N | 0.3s c/u | Cada step del Player al caminar (no agachado). |
| F02 | `pasos_jugador_crouch` | Pasos más sutiles agachado. 4-6 variaciones. | One-shot x N | 0.3s c/u | Caminar agachado. |
| F03 | `pasos_jugador_sprint` | Pasos más fuertes corriendo. | One-shot x N | 0.25s c/u | Sprint. |
| F04 | `respiracion_normal` | Respiración suave del jugador (loop sutil cuando está calmo, casi silencio). | Loop | 4-6s | Continuo a volumen muy bajo. |
| F05 | `respiracion_agitada` | Respiración acelerada cuando estrés > 70. Cross-fade con F04. | Loop | 3-4s | Estrés alto. |
| F06 | `latido_cardiaco` | Latido cardíaco. Volumen y velocidad reactivos al estrés. | Loop con pitch variable | 1.2s loopeable | Aparece a estrés 50, acelera con más estrés. **CRÍTICO.** |

### F-bis. WAKE-UP DEL JUGADOR (NUEVO - secuencia cinematográfica al spawn)

El sistema `PlayerWakeUp` ejecuta una secuencia automática al entrar al aula: pantalla negra, pestañeos lentos, look around izq/der/centro, devuelve control. ~10 segundos. Necesita audio para acompañar:

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| F07 | `wakeup_respiracion_dormido` | Respiración pesada de alguien que recién se está despertando, lenta. | Loop o one-shot | 4-6s | Durante los primeros segundos del wake-up (pantalla negra). |
| F08 | `wakeup_inhalada_profunda` | Inhalada profunda + suspiro al "abrir los ojos". | One-shot | 1.5-2s | Al primer pestañeo del wake-up (cuando alpha del overlay baja por primera vez). |
| F09 | `wakeup_tinnitus` | Zumbido agudo sutil en los oídos (típico de aturdimiento al despertar). Va bajando volumen durante el look around. | One-shot con fade | 5-7s | Disparado al iniciar el wake-up, fade out al terminar. |
| F10 | `wakeup_pestaneo` | Sonido sutil de párpado/ojo (casi imperceptible). 2-3 variaciones. | One-shot | 0.1-0.2s c/u | Cada pestañeo del wake-up. |

### G. MINIJUEGO RESPIRACIÓN (CRÍTICO PEDAGÓGICAMENTE)

> ⚠️ **CONTEXTO IMPORTANTE**: el mic NO se está captando bien en la PC de prueba. Por eso el modo principal del minijuego es FALLBACK TECLADO (sostener Space/Triangle en INHALA, soltar en EXHALA). Los audios guía G01-G05 son lo único que mantiene el valor PEDAGÓGICO del minijuego — el jugador respira siguiendo el audio aunque sea con teclas. **SIN ESTOS AUDIOS, el mensaje educativo se diluye totalmente.**

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| G01 | `respiracion_inhala_guia` | Tono ascendente suave durante la fase INHALA del minijuego (4 segundos). Idealmente con voz susurrada "inhalaaaa..." o solo tono puro. | One-shot | 4s | Sincronizado con fase Inhale del BreathingMinigame. **CRÍTICO.** |
| G02 | `respiracion_exhala_guia` | Tono descendente suave durante la fase EXHALA (4 segundos). Idealmente con voz susurrada "exhalaaaa..." o solo tono puro. | One-shot | 4s | Sincronizado con fase Exhale. **CRÍTICO.** |
| G03 | `respiracion_pausa_guia` | Silencio o tono apenas perceptible durante la fase PAUSA (2 segundos). | (silencio) o tono | 2s | Fase Pause. |
| G04 | `respiracion_ciclo_ok` | Sonido sutil positivo al completar un ciclo correcto (tipo "campana lejana suave" o "respiro de alivio"). | One-shot | 0.6s | Al ScoreSuccess del ciclo. |
| G05 | `respiracion_ciclo_fail` | Sonido neutro o ligeramente negativo al fallar un ciclo. | One-shot | 0.4s | Al ScoreFail del ciclo. |

### H. UI Y MENÚ

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| H01 | `menu_hover` | Tick suave al cambiar de botón con teclado/gamepad. | One-shot | 0.1s | Botones del menú principal y settings. |
| H02 | `menu_select` | Click al confirmar opción. | One-shot | 0.15s | Confirmación de botón. |
| H03 | `menu_back` | Click al volver atrás (Esc / Circle). | One-shot | 0.15s | Cerrar overlays. |
| H04 | `menu_glitch` | Sonido de "señal corrupta" sutil cuando aparece el glitch del menú. | One-shot | 0.2s | Sincronizado con bandas glitch del MainMenu. |
| H05 | `loading_whoosh` | Whoosh cinematográfico al cambiar de escena. | One-shot | 1-1.5s | Al iniciar transición SceneTransition. |
| H06 | `pause_open` | Sonido grave + reverberación al pausar. | One-shot | 0.4s | Al abrir Pause Menu. |
| H07 | `pause_close` | Inverso del H06 al despausar. | One-shot | 0.4s | Al cerrar Pause Menu. |

### I. CINEMATIC + GAME OVER

| ID | Nombre | Descripción | Tipo | Duración | Cuándo |
|---|---|---|---|---|---|
| I01 | `cinematic_intro_drone` | Drone grave atmosférico que acompaña la cinemática de intro (texto narrativo). | Loop | 60s+ | Durante Cinematic_Intro. |
| I02 | `cinematic_intro_voz_off` | (Opcional) Voz en off masculina o femenina suave que dice los textos del intro. | One-shot | 5-8s c/u | Sincronizado con cada slide de la cinemática. |
| I03 | `gameover_collapse` | Sonido de colapso (respiración corta + golpe sordo + ringing alto). | One-shot | 2-3s | Al disparar OnCollapse del StressSystem. **CRÍTICO.** |
| I04 | `gameover_drone` | Drone más oscuro durante la pantalla "Te dejaste llevar". | Loop | 30s | Durante GameOverHandler activo. |
| I05 | `outro_calma` | Tono de cierre tranquilo, esperanzador. | One-shot | 4-6s | Para el outro al terminar el slice (futuro). |

---

## Sección 2 — CINEMÁTICAS

### Sistemas técnicos ya construidos (para tu referencia, Carlos)

Antes de los detalles de cada cinemática, tené presente que los siguientes sistemas YA están implementados y solo necesitan tu contenido:

- **`CinematicController.cs`**: reproduce slides de texto secuencial con typewriter, fade, skip con cualquier tecla. La escena `Cinematic_Intro` ya existe y arranca al darle JUGAR. Solo hace falta agregar audio (drone + voz off).
- **`LoadingScreenController.cs`**: pantalla negra con tip de respiración rotativo + fade in/out. Aparece automáticamente entre escenas. Necesita el sonido whoosh (H05).
- **`PlayerWakeUp.cs`**: secuencia de pestañeos + look around al spawnear en el aula. ~10 segundos. Necesita audios F07-F10 (sección F-bis).
- **`BreathingMinigame.cs`**: minijuego de respiración con fases Inhale/Exhale/Pause y tutorial in-game al primer disparo. Necesita G01-G05 (CRÍTICOS).

### Cinemática 1 — INTRO (la que ya está en `Cinematic_Intro` con texto placeholder)

**Estado actual:** `CinematicController` con texto secuencial typewriter (4 slides). Funciona pero necesita upgrade.

**Lo que necesita:**
1. **Voz en off** masculina o femenina suave (tipo Visage, Madison, monólogo interior). Lee los siguientes 4 slides:

```
Slide 1: "No recuerdo cómo llegué aquí."
Slide 2: "El aula está vacía. Solo yo. Y lo que sea que esté dentro de mí."
Slide 3: "Algo no está bien."
Slide 4: "Antes de entrar... respira."
```

2. **Drone atmosférico** de fondo (I01).
3. **Visuales** (futuro): hoy es solo texto. Para el futuro Carlos puede grabar un video corto:
   - Plano negro al inicio.
   - Fade a primer plano de un pupitre vacío.
   - Cámara se acerca lento, sin movimiento brusco.
   - Texto aparece intercalado con el video.
   - Al final, fade a negro → entra calibración mic → entra wake-up del Player.

**Duración total:** ~30-40 segundos.

---

### Cinemática 2 — TUTORIAL DE RESPIRACIÓN (NUEVA — propuesta del 26)

**Concepto:** en lugar del tutorial in-game actual que es solo texto, una pequeña animación cinematográfica que ENSEÑE al jugador la técnica respiratoria 4-4-2.

**Cuándo se dispara:** la primera vez que el estrés cruza 40 (igual que ahora), en lugar del overlay de texto.

**Estructura:**

```
[0:00-0:03] El gameplay PAUSA. Pantalla se desatura levemente. 
            Aparece un círculo grande pulsante en el centro 
            (similar al actual pero más grande y limpio).

[0:03-0:08] Voz en off o texto cinematográfico:
            "Cuando el miedo sube, tu cuerpo se acelera."
            Acompañado de heartbeat sound (F06) audible.

[0:08-0:13] Voz/texto: "Pero podés hacer que baje."
            Heartbeat baja gradualmente.

[0:13-0:21] Voz/texto: "Inhala. Cuatro segundos."
            Círculo CRECE durante 4s mientras se escucha 
            el tono inhala (G01) y la barra del control 
            (Triangle/Space) se ilumina.

[0:21-0:25] Voz/texto: "Sostén. Dos segundos."
            Círculo se queda quieto. Silencio.

[0:25-0:33] Voz/texto: "Exhala. Cuatro segundos."
            Círculo SE CONTRAE. Tono exhala (G02).
            Indicación visual: "ahora soltá el botón."

[0:33-0:37] Voz/texto: "Hacelo conmigo."
            El minijuego empieza realmente. El jugador 
            tiene que hacer 1 ciclo guiado.

[0:37+] Cinemática termina. Vuelve el control normal.
```

**Audios necesarios para esta cinemática:**
- I02 (voz en off para los textos).
- F06 (heartbeat reactivo).
- G01, G02, G03 (tonos guía).

**Visuales:** se puede hacer todo en código UI (sin video real), pero si Carlos puede crear una animación corta tipo "ondas concéntricas + glow" sería ideal.

---

### Cinemática 3 — RECUERDO / FLASHBACK (OPCIONAL — para discusión)

**Idea:** mostrar un recuerdo del jugador en algún momento del slice. Da contexto narrativo y profundidad.

**Posibles temas (elegir UNO o combinar):**

#### Opción A — El recuerdo del accidente
- El SubliminalLoop ya menciona "accidente" (changelog 003).
- Flashback corto al accidente que llevó al jugador al aula.
- Visuales sugeridos:
  - Cámara dentro de un bus/auto, vista del jugador.
  - Sonido de tráfico, voces de compañeros.
  - Frenazo brusco. Pantalla blanca. Silencio.
  - Vuelta al aula.

**Cuándo:** podría dispararse al leer una nota específica que es "boleto del bus".

**Audios:** sonido de bus, frenazo, gritos, blanco silencio.

#### Opción B — El recuerdo de los compañeros
- Las frases del Observador ("No estaban dormidos", "Falta alguien", "Tú no deberías haber salido") sugieren que algo pasó con los compañeros.
- Flashback sutil de los compañeros sentados, alguien hablándole al jugador.

**Cuándo:** al disparar la intrusión del Observador por primera vez.

**Audios:** voces lejanas de niños riendo, deformándose, silencio brusco.

#### Opción C — El recuerdo de quien le enseñó a respirar
- Vinculado al tutorial de respiración.
- Flashback breve: una madre, abuela, profe enseñándole a respirar al jugador cuando era niño.
- Conecta la mecánica respiratoria con un origen emocional.

**Cuándo:** justo antes o durante el tutorial de respiración (Cinemática 2).

**Audios:** voz cálida femenina o masculina, ambient hogareño, transición.

**Recomendación:** Opción C es la más alineada con el mensaje pedagógico del juego. Las otras dos pueden venir en sprints futuros.

---

### Cinemática 4 — OUTRO (al terminar el slice)

**Cuándo:** después del pasillo final (cuando se construya), al llegar el jugador a la sala outro.

**Estructura:**

```
[0:00-0:03] Pantalla negra.
[0:03-0:10] Texto/voz: "No estaba en el aula."
            Pausa.
[0:10-0:18] Texto/voz: "Estaba dentro de mí."
            Pausa más larga.
[0:18-0:25] Texto/voz: "Y ahora sé respirar."
            Música/drone se transforma de oscuro a calmo.
[0:25-0:30] Texto: "Fin del Capítulo 1"
[0:30+] Botón "Volver al menú principal".
```

**Audios:**
- I05 (tono de calma).
- I02 (voz en off para los textos).

---

## Sección 3 — MOOD Y REFERENCIAS

### Estética sonora general

**Sí queremos:**
- Silencios prolongados.
- Sonidos sutiles, pequeños, presentes.
- Frecuencias graves para tensión.
- Ningún jumpscare gratuito.
- Audio reactivo al estado emocional (estrés).
- Stereo paneado para crear sensación de "presencia detrás".

**No queremos:**
- Loops continuos sin descanso.
- Música épica.
- Sonidos genéricos de horror/Halloween.
- Sobre-procesamiento (reverberación al máximo).

### Referencias específicas

| Juego | Por qué |
|---|---|
| **Visage** | Ambient atmosférico, silencio dominante, latido reactivo. |
| **Outlast** | Susurros distantes, ambient bajo continuo + stingers fuertes en intrusiones. |
| **Madison** | Polaroids/cámara analógica, voz interior procesada, sonidos físicos del entorno. |
| **Resident Evil 7** | Apartado del horror narrativo: respiración del personaje audible cuando está estresado. |
| **Silent Hill 2** | Drones graves continuos sutiles + sonidos texturales de hierro/metal. |
| **PT (Hideo Kojima)** | Susurros, radio cortándose, sensación de "algo está mal pero no sé qué". |

### Volúmenes recomendados (en relación al master)

- Ambient base: -28 dB.
- Stingers: -10 a -6 dB (notables pero no agresivos).
- Latido cardíaco: -18 dB cuando está activo.
- SFX intrusión Observador (C02): -3 dB (debe asustar).
- SFX apagón (E01): -3 dB.
- Voz en off: -8 dB (claro pero no gritando).
- Audio guía respiración: -12 dB (suave, didáctico).

---

## Resumen ejecutivo

**Lo más crítico para entrega del miércoles 29:**

| Prioridad | Audios | Cinemáticas |
|---|---|---|
| **CRÍTICO** | C02 (intrusión), E01 (apagón), **G01-G02 (respiración guía — sin esto el juego pierde su valor pedagógico)**, F06 (latido), I03 (gameover) | Voz en off para Cinemática 1 (intro, escena `Cinematic_Intro` ya existe) |
| **IMPORTANTE** | A01 (ambient base, reemplaza al SubliminalLoop actual), F07-F09 (wake-up del jugador), B01-B04 (stingers), D01-D03 (cerradura), F01 (pasos), H05 (whoosh) | Cinemática 2 (tutorial respiración animado — opción para reemplazar el tutorial de texto actual) |
| **NICE TO HAVE** | A02 (apagón ambient), C01-C03 (susurros + pasos), F04-F05 (respiración personaje), F10 (pestañeos), I02 (voz off completa), H01-H07 (UI) | Cinemática 3 (flashback opción C) |
| **POST-ENTREGA** | C04-C05 (otras intrusiones), I05 (outro), I04 (gameover drone) | Cinemática 4 (outro completo) — vinculado al pasillo final + sala outro pendiente |

**Si solo tenés tiempo para 6 audios, dame estos:**
1. **G01 + G02** (guía respiración — críticos pedagógicamente porque el mic no funciona).
2. C02 (intrusión Observer).
3. E01 (apagón).
4. F06 (latido reactivo).
5. F08 (inhalada profunda al despertar — wake-up).

Eso eleva el juego enormemente.

## Cambios recientes (decisiones del 26-04-2026 que afectan tu trabajo)

1. **Mic NO se capta** en máquina de prueba → fallback teclado activo. Los audios G01-G05 son VITALES para el valor pedagógico.
2. **PlayerWakeUp** implementado → necesita audios F07-F10 (sección F-bis nueva).
3. **CinematicController** y escena `Cinematic_Intro` ya existen → tu trabajo es agregar voz off + drone (I01, I02).
4. **LoadingScreen** entre escenas ya implementado → necesita whoosh (H05).
5. **PauseMenu, GameOver, Settings, Inventory** todos implementados → necesitan UI sounds (H01-H07, I03, I04).
6. **Multi-escena planeada** (Salón / Ductos / Oficina) → para el futuro. Por ahora con 1 escena. Cada transición usaría H05 (whoosh) + posible audio único por destino.
7. **Frases de pizarra de Carlos** (`Assets/Art/text/frases_pizzarra.txt`) → vamos a integrarlas dinámicamente sobre `Cubo.002`. Si querés agregar un sutil "sound de tiza escribiendo" cuando aparece una frase nueva, sería un buen detalle (ID propuesto: B05 `tiza_escribir`).

---

## Cómo entregarlo

- **Carpeta**: `Assets/Art/Sounds/` (ya existe en el proyecto). Subcarpetas por categoría: `Ambient/`, `Stingers/`, `Intrusions/`, `Interaction/`, `Player/`, `Breathing/`, `UI/`, `Cinematic/`.
- **Nomenclatura**: usar el ID de este doc (ej. `C02_intrusion_observer_aparicion.wav`).
- **Formato**: `.wav` o `.ogg`, 44.1 kHz, mono o stereo según indique.
- **Subir al repo**: en una rama `feature/audio-cap1` para PR a main.

Si tenés dudas sobre cuándo se dispara cada audio, mirá el script asociado:
- Stress: `Assets/Scripts/Stress/StressSystem.cs` (eventos OnStressChanged, OnCollapse).
- Intrusion: `Assets/Scripts/Intrusions/IntrusionManager.cs` (eventos OnObserver, OnIracundo, OnInfante).
- Blackout: `Assets/Scripts/Interaction/BlackoutEvent.cs` (TriggerBlackout).
- Breathing: `Assets/Scripts/Breathing/BreathingMinigame.cs` (fases Inhale/Exhale/Pause).
- Cerradura: `Assets/Scripts/Interaction/CombinationLock.cs` y `LockOverlay.cs`.

Cualquier duda, hablá conmigo (Duvan) por el grupo del equipo.

¡Gracias Carlos!
