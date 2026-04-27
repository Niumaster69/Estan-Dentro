# PLAN DE ENTREGA FINAL — Capítulo 1 jugable

**Fecha del plan:** 2026-04-25 (sábado)
**Entrega:** miércoles 29 de abril
**Tiempo real disponible:** 4 días, de los cuales solo el **domingo 26** es jornada completa de Duvan.
**Autor del plan:** Duvan + Claude. Documento sujeto a validación antes de implementar.

---

## 0. TL;DR (léelo primero)

> **Nota sobre realismo del cronograma**: este plan asume que tú estás aprendiendo Unity sobre la marcha y necesitas guía paso a paso para todo lo visual. Para que las 4 días sean alcanzables, **toda la UI se construye por código** (mis scripts arman Canvas/Image/Text en runtime). Tú solo tocas la GUI de Unity para lo mínimo: configurar Input Actions, posicionar cosas en mundo 3D, y arrastrar scripts a GameObjects. Detalle en sección 11.


- **Pivote del puzzle**: pasamos de un puzzle narrativo difuso a un **puzzle mecánico de control de respiración / estrés**, alineado con el feedback del profe y con la pedagogía del proyecto (regular ansiedad y miedo en la vida real).
- **Slice jugable**: 1 capítulo único de **30–45 min**, dos espacios (aula + un segundo espacio), tres beats narrativos, un puzzle físico de combinación, dos momentos de tensión.
- **Trabajamos solo con lo que controla Duvan**. Los assets de Henry y Carlos que no lleguen se reemplazan con placeholders (cubos, audio libre, texto en pantalla). Si llegan, se integran sin romper nada.
- **Reutilizamos** todo el Sprint 1 + parte del Sprint 2: PlayerController, escena del aula, mixer y loops, IntrusionManager, GazeDetector, shader de siluetas.
- **Control dual P0**: el jugador elige **teclado/ratón** o **mando (DualSense)** para movimiento, interacción y linterna. Ambos esquemas son ciudadanos de primera, no se prioriza uno sobre otro.
- **Respiración por micrófono del casco/auriculares P0**, con fallback automático a **tecla Espacio** si el mic no calibra. La pedagogía exige que el jugador realmente respire — esto es lo que el profe pidió.
- **Features especiales del DualSense** (haptics, adaptive triggers, mic interno, lightbar) quedan en P2. Solo conexión nativa de gamepad.

---

## 1. Estado del proyecto hoy (qué tenemos para reutilizar)

Construido y funcional:

| Sistema | Archivo | Estado |
|---|---|---|
| FPS Controller | `Assets/Scripts/Player/PlayerController.cs` | ✓ Reutilizable. Tiene WASD + look + sprint + crouch vía `PlayerInput` (Send Messages). Se le agregan métodos `OnInteract`, `OnFlashlight`, `OnBreathe`. |
| Loop subliminal | `Assets/Scripts/Audio/SubliminalLoopController.cs` + `MainMixer.mixer` | ✓ Reutilizable como ambiente de horror persistente. |
| IntrusionManager | `Assets/Scripts/Intrusions/IntrusionManager.cs` | ✓ Reutilizable. Lo conectamos al sistema de estrés (cada intrusión sube el medidor). |
| Gaze (mirada sostenida) | `Assets/Scripts/Intrusions/Gaze/*` | ✓ Reutilizable como **base del sistema de mira/interacción** (extendemos `GazeDetector` para mostrar reticle). |
| Shader siluetas | `Assets/Scripts/Intrusions/SilhouetteManager.cs` + `SilhouetteController.cs` | ✓ Si el shader visual no se ve, lo dejamos en stand-by. No es bloqueante para entregar. |
| Escena aula | `Chapter1_Salon4B/` (lo que haya hecho Henry) | Reutilizamos. **No editamos su escena**, trabajamos sobre prefabs y un duplicado de control si hace falta. |

Lo que hay que construir desde cero (gris = no es bloqueante para entregar):

- Sistema de estrés (medidor 0–100, sube por eventos, baja por respiración).
- Mecánica de respiración (mini-juego rítmico).
- Sistema de interacción (raycast desde cámara + reticle dinámico + tecla E).
- Sistema de inventario simple (lista con slots, UI lateral).
- Sistema de notas leíbles (UI overlay con texto al recoger/inspeccionar).
- Sistema de cerradura por combinación (UI con 3-4 dígitos).
- Linterna + evento de apagón global.
- Game Over + reinicio.
- Main menu, pantalla de carga con tips, menú de pausa.
- Intro y outro narrativos (pantallas de texto + audio, no cinemática 3D).

---

## 2. Pivote del puzzle (cambio de plan validado con el profe)

### Antes
El puzzle era una "perspectiva cruzada" más narrativa que mecánica. El jugador interpreta. Difícil de cerrar y de demostrar.

### Ahora
**El puzzle es la regulación emocional del jugador.** El miedo del entorno sube un medidor de estrés. El jugador aprende a respirar con un patrón rítmico. La respiración es la herramienta para:

1. **Sobrevivir** — si el estrés llega a 100, el jugador colapsa y vuelve al inicio.
2. **Avanzar** — momentos del juego solo se desbloquean en estado de calma (ej. ver una pista escondida que solo aparece cuando estás regulado, abrir una caja sin temblar).

### Mensaje pedagógico
> "El miedo es real, pero no manda. Cuando aprendes a respirar, vuelves a ver lo que estaba ahí todo el tiempo."

Esto se enseña al jugador a través de:
- Pantallas de carga con tips reales sobre regulación emocional (técnica 4-7-8, anclaje sensorial, etc.).
- Tutorial in-game integrado en el primer susto.
- Outro que conecta la mecánica con la metáfora de la disociación.

---

## 3. Visión del Capítulo 1

**Pitch en una frase**: *"Despertaste en el aula vacía. Algo no está bien — y está dentro de ti. Respira con ritmo o el miedo te traga."*

**Loop de gameplay (30–45 min):**

1. **Intro (2 min)** — Pantalla negra con texto y voz en off. "No recuerdo cómo llegué aquí." Fade-in al aula.
2. **Tutorial pasivo (3 min)** — El jugador despierta sentado en un pupitre. Aprende a moverse. Una nota en la mesa: *"Si la sientes subir, respira despacio."*
3. **Primer susto + tutorial activo (5 min)** — El Observador aparece en la pizarra (gaze de 3.5s ya construido). El estrés salta a 60. Aparece UI de respiración. El juego pausa el tiempo y enseña: *inhala con la barra subiendo, exhala bajando, mantén el ritmo*. El jugador la baja a 20 y el momento pasa.
4. **Exploración + puzzle ambiental (15 min)** — El jugador explora el aula. Encuentra:
   - **Nota A** en un pupitre con un acertijo numérico simple (ej. "fechas escritas en la pizarra restadas: 1998 - 1985 = ?").
   - **Pizarra** con números/textos que dan pistas.
   - **Caja/cerradura** (en un casillero o cajón del escritorio) que pide un código de 3-4 dígitos.
   - Mientras explora, hay **2 sustos menores** (silueta en el pasillo, golpe en la puerta) que suben el estrés a 50–70. El jugador debe respirar para seguir.
5. **Apagón + linterna (8 min)** — Al sacar el código y abrir la caja, suena un golpe seco. Las luces se apagan. El jugador saca una linterna. Aparece la salida del aula que no estaba visible antes. Camina por un pasillo corto.
6. **Salida + outro (5 min)** — Llega a un segundo espacio (puede ser el mismo pasillo iluminado distinto, o una habitación pequeña). Una nota final y un pantalla con la metáfora: *"No estaba en el aula. Estaba dentro de mí. Y ahora sé respirar."* Fin del capítulo.

**Resultado**: el jugador termina con una experiencia emocional + 2 técnicas reales que probó (respiración rítmica, anclaje sensorial vía objetos del entorno).

---

## 4. Mecánicas (priorizadas P0 / P1 / P2)

### P0 — sin esto, no se entrega

| Mecánica | Qué hace | Input | Esfuerzo |
|---|---|---|---|
| **Stress System** | Medidor 0–100. Sube por eventos (intrusiones, oscuridad, sustos). Baja por respiración. UI: barra circular en HUD. Si llega a 100 → Game Over. | — | Bajo |
| **Respiración por mic** | UI overlay con un círculo que pulsa (4s inhalar / 4s exhalar / 4s pausa). El jugador exhala suave hacia el mic del casco durante la fase de exhalar. Detección por amplitud RMS con calibración inicial. Acertar el ritmo → estrés baja. Falla → estrés sube poco. **Fallback automático**: tecla **Espacio** sostenida si el mic no detecta señal o no calibra. | Mic + Espacio (fallback) | **Alto** |
| **Calibración de mic** | Pantalla previa al juego: "respira normal 3s" → captura ruido base. "Exhala suave 3s" → captura nivel exhale. Calcula el threshold. Si la diferencia es muy chica → activa fallback teclado. | Mic | Medio |
| **Control dual** | Todas las acciones P0 funcionan tanto en **teclado/ratón** como en **gamepad** (DualSense / Xbox / cualquier compatible con Input System). El jugador no elige modo: ambos están vivos a la vez. | Teclado o gamepad | Medio |
| **Interacción** | Raycast desde cámara. Cuando apunta a un objeto interactuable, el reticle cambia (de punto a ojo/mano según tipo). | E / **Cross (X) gamepad** | Bajo |
| **Notas leíbles** | Recoger una nota → pantalla overlay con el texto. Cualquier tecla cierra. | E / Cross para abrir, cualquier botón para cerrar | Muy bajo |
| **Cerradura por combinación** | UI con 3–4 ruedas de dígitos. Mouse / teclas arriba-abajo / **D-pad gamepad** cambian. Botón confirmar. Si acierta → abre y dispara evento. | Mouse + Enter / **D-pad + Cross** | Bajo |
| **Linterna + apagón** | Tecla F / **Square gamepad** = encender/apagar linterna (Spot Light hijo de cámara). Evento global apaga todas las luces de la escena. | F / **Square** | Muy bajo |
| **Game Over + Restart** | Pantalla negra con "Te dejaste llevar. Respira." y botón para reiniciar la escena. | Cualquier botón | Muy bajo |
| **Main Menu** | "Jugar / Salir". Una sola escena `MainMenu`. Navegable con teclado, mouse y gamepad. | Todos | Muy bajo |
| **Loading screen + tip** | Entre menú y juego, pantalla con tip único de respiración (4-7-8 o anclaje 5-4-3-2-1). | — | Muy bajo |

### P1 — mejora la experiencia, opcional pero deseable

- **Inventario simple** — lista de items recogidos visible con tecla I / Touchpad. (Movido a P1 por tiempo: en el slice actual solo hay 1 item realmente, podemos mostrarlo inline sin inventario.)
- **Pause menu** — ESC pausa con menú "Continuar / Menú principal / Salir". (Movido a P1: ESC simplemente pausa el tiempo y muestra "ESC: continuar / Q: salir al menú", sin UI compleja.)
- **2 intrusiones más** además del Observador (Iracundo, Infante) si los assets llegan.
- **Cinemática intro/outro** con audio + texto sincronizado (no 3D, solo pantalla).
- **Subtítulos** de la voz en off.
- **Reticle especial sobre pistas leíbles** (ojo abierto) vs. interactivos (mano).
- **Audio reactivo al estrés** — el loop subliminal sube de volumen con el estrés.
- **Tips múltiples** en pantalla de carga (P0 entrega solo 1 tip fijo).

### P2 — stretch, solo si todo P0 + P1 está listo

- **Vibración del DualSense** cuando el estrés está alto, rumble bajo cuando está calmado. La conexión básica del mando ya está en P0; esto es solo encender el motor.
- **Adaptive triggers** del DualSense (resistencia variable al respirar mal).
- **IA de "El Que Se Sienta Delante"** como personaje complejo.
- **Variación dinámica de iluminación** según estrés.

---

## 5. Pantallas y flujo

```
[Main Menu] → [Loading screen + tip] → [Capítulo 1 — Intro card]
    ↓
[Aula 4-B: tutorial + exploración + puzzle + sustos]
    ↓
[Apagón + linterna + pasillo]
    ↓
[Habitación final / nota final / outro card]
    ↓
[Pantalla "Fin del Capítulo 1" → volver al menú]
```

**Game Over** se inserta en cualquier punto donde el estrés llegue a 100. Reinicia desde el último checkpoint (al inicio de cada bloque grande: tutorial, exploración, apagón).

---

## 6. Decisión clave: cómo construimos la respiración

**Decisión validada (2026-04-25)**: respiración por **micrófono del casco/auriculares** como input principal, con **fallback automático a tecla Espacio** si la calibración falla. El mando NO se usa para respirar.

### Por qué mic del casco y no del DualSense

| Criterio | Mic auriculares | Mic DualSense |
|---|---|---|
| Distancia a la boca | ~5–10 cm | ~50 cm (en las manos) |
| Señal/ruido para detectar exhale | Alta | Baja, mucho ruido ambiente |
| Universalidad | Todo el mundo tiene | Requiere tener el mando + emparejarlo |
| Riesgo de falsos positivos | Bajo con calibración | Alto |

### Cómo se ve y se siente

- Antes de empezar a jugar, **una pantalla corta de calibración** pide al jugador:
  1. "Respira normal durante 3 segundos" → captura el ruido de fondo (RMS base).
  2. "Exhala suave hacia el micrófono durante 3 segundos" → captura el nivel de exhale.
  3. Calcula el threshold como punto medio. Si la diferencia entre ambos niveles es < 0.02 → muestra "No detectamos tu micrófono. Vas a poder jugar usando la tecla Espacio para respirar." y activa el modo fallback.
- En partida, cuando el estrés sube de 40 aparece el **círculo pulsante** del minijuego de respiración:
  - 4 s **inhalar** (círculo crece, audio guía sube de volumen) — el jugador inhala silencioso por la nariz.
  - 4 s **exhalar** (círculo se contrae, audio guía baja) — el jugador **exhala suave hacia el mic**. Detectamos la amplitud RMS sostenida sobre el threshold por al menos 2.5 s → ciclo válido.
  - 2 s **pausar** — silencio, el círculo queda pequeño.
- Cada ciclo correcto baja el estrés en 8–10 puntos.
- Cada ciclo fallido (no detectamos exhale, o se exhala fuera de fase) sube el estrés en 2.
- En modo fallback (teclado), el ciclo es idéntico pero el jugador **mantiene Espacio** durante "inhalar" y lo **suelta** durante "exhalar".

### Implementación técnica (alto nivel)

- Unity tiene `Microphone.Start(null, true, 1, 44100)` para abrir el mic default de Windows.
- Un `AudioSource` muteado (para no oír retroalimentación) recibe el clip.
- Cada frame leemos los últimos N samples con `clip.GetData()` y calculamos RMS.
- El `BreathingMinigame` consume el RMS y compara con el threshold.
- `BreathingInputProvider` abstrae mic vs. tecla — el resto del sistema no sabe cuál se está usando.

### Por qué esto sí pedagógicamente

El profe pidió que la mecánica enseñe a respirar de verdad. Con el mic, el cuerpo del jugador hace el acto físico de exhalar — no solo presiona un botón. Esa es la diferencia entre "juego sobre respirar" y "juego que te enseña a respirar". El fallback teclado existe solo para no romper la entrega si el hardware del profe falla, pero en la demo principal usamos mic.

---

## 7. Plan de los 4 días — calibrado para principiante en Unity

> Convención: 🟢 = P0, 🟡 = P1. Tiempos estimados ya tienen en cuenta que aprendes Unity sobre la marcha.

### Filosofía del cronograma

Estimaciones basadas en:
- Tú en Unity: clicks lentos, primera vez en muchas pantallas, requiere mi guía paso a paso.
- Yo en código: rápido. Yo te doy el `.cs` listo, tú lo creas y lo arrastras.
- **UI por código** (HUDBootstrapper-style) → ahorra ~5h de Unity GUI a lo largo de la semana.
- Buffer de 30% por errores y depuración.
- Sesiones máximo de 3h sin descanso. Después rinde menos.

### Sábado 25 (HOY) — solo plan ✅
- ✅ Validar este documento contigo.
- ✅ Confirmar decisiones (sección 9).

### Domingo 26 — DÍA CRÍTICO (~9h reales con descansos)

**Meta del día**: bucle estrés ↔ respiración funcionando en `Dev_Duvan.unity`, con teclado y mando, y con interacción + linterna básicas.

#### Bloque 1 — Input dual (2h, 09:00–11:00)
- 🟢 Te paso ruta paso a paso para **agregar 4 acciones nuevas** al `InputSystem_Actions.inputactions` existente: `Interact`, `Flashlight`, `Pause`, `BreatheFallback`. Cada acción con binding teclado y gamepad. *(Move/Look/Sprint/Crouch ya existen y ya tienen ambos bindings por default, así que esos no se tocan.)*
- 🟢 Yo extiendo `PlayerController.cs` para que reciba estos eventos.
- 🟢 Probar en Play: caminas con WASD y con stick izquierdo, miras con mouse y con stick derecho.

> **Lo que tú haces en Unity en este bloque**: abrir el archivo de Input Actions, agregar 4 filas, configurar 8 bindings (4 teclado + 4 gamepad), guardar. ~1.5h con mi guía. Resto es probar.

☕ Descanso 30 min.

#### Bloque 2 — Sistema de estrés + HUD (1.5h, 11:30–13:00)
- 🟢 Yo te paso `StressSystem.cs` y `HUDBootstrapper.cs` (este último construye la UI por código).
- 🟢 Tú creas un GameObject vacío llamado `_Systems`, le arrastras `StressSystem` y `HUDBootstrapper`.
- 🟢 En Play: ya ves la barra de estrés en pantalla. Tecla de prueba (la pongo yo en el script) que sube y baja el estrés para verificar.

> **Lo que tú haces en Unity en este bloque**: crear 1 GameObject, arrastrarle 2 scripts. ~10 min de Unity. El resto es esperar a que yo escriba los scripts y probar.

🍽️ Almuerzo 1h.

#### Bloque 3 — Calibración de mic + Respiración (3h, 14:00–17:00)
- 🟢 Yo te paso `BreathingInputProvider.cs`, `MicCalibration.cs`, `BreathingMinigame.cs`.
- 🟢 Tú: creas un GameObject `_Breathing` con esos 3 scripts. Y cierras los ojos a la magia.
- 🟢 La pantalla de calibración aparece sola al iniciar (es UI por código).
- 🟢 En Play: pasas la calibración con tu casco. Subes el estrés artificialmente. Aparece el círculo. Exhalas → baja. *Si el mic no calibra → fallback a Espacio funciona automático.*
- 🟢 Iteramos balanceo: cuánto baja por exhale, cuánto sube por fallar, duración de fases.

> **Lo que tú haces en Unity**: crear 1 GameObject + arrastrar 3 scripts. ~10 min de Unity. La mayor parte del bloque es **probar y ajustar valores conmigo**, que es lo que más rinde porque ahí ajustamos la sensación.

☕ Descanso 30 min.

#### Bloque 4 — Interacción + linterna (2h, 17:30–19:30)
- 🟢 Yo te paso `InteractionSystem.cs` (extiende `GazeDetector`), `Note.cs`, `Flashlight.cs`, `BlackoutEvent.cs`.
- 🟢 Tú: configuración mínima.
  - Crear hijo `Flashlight` en la cámara → Add Component Spot Light → Add Component `Flashlight.cs` (~10 min con mi guía).
  - Crear un cubo "Nota_Test" en la escena, marcar su layer como `Interactable`, ponerle `Note.cs` con un texto cualquiera (~10 min).
  - Crear `_Events` con `BlackoutEvent.cs` (~5 min).
- 🟢 Probar: tecla F prende/apaga linterna. Apuntas a la nota → reticle cambia → E o Cross la abre. Disparar BlackoutEvent desde la consola → todas las luces se apagan.

> **Lo que tú haces en Unity**: crear 3 GameObjects, configurar layer + scripts. ~30 min de Unity guiado.

#### Bloque 5 — Cierre del día (30 min, 19:30–20:00)
- 🟢 Conectar `IntrusionManager.onObserver` → `StressSystem.AddStress(40)`. (Drag-and-drop en Inspector, te paso instrucciones.)
- 🟢 Probar el bucle completo: caminar, mirar la pizarra 3.5s, intrusión → estrés salta → aparece círculo → respiras → estrés baja.
- 🟢 Yo escribo el changelog 006.

**Si al final del domingo este bucle funciona, el resto de la semana es contenido y empaquetado. La pelea de verdad es el domingo.**

---

### Lunes 27 — Cerradura + escena real (2.5h)

> Asumimos 2.5h reales de Lunes. Si tienes 4h, mejor.

- 🟢 Yo te paso `CombinationLock.cs` + UI procedural.
- 🟢 Tú: creas un cubo "Cerradura" en la escena con `CombinationLock.cs`. ~15 min.
- 🟢 Probar la cerradura con un código de prueba.
- 🟢 **Migración a la escena real**: abrir `Chapter1_Salon4B`, agregar los GameObjects `_Systems`, `_Breathing`, `_Events`, el `Flashlight` en cámara. ~30 min.
- 🟢 Posicionar 2 notas y 1 cerradura en pupitres/escritorio. ~45 min (esto es donde más trabajo manual hay).
- 🟢 Pegar pista en notas con números del puzzle. (Yo escribo los textos, tú los copias en el Inspector.)

### Martes 28 — Menús + flujo (2.5h)

- 🟢 Yo te paso `MainMenuController.cs`, `GameOverController.cs`, `LoadingScreenController.cs`. Todo con UI por código.
- 🟢 Tú: crear 3 escenas vacías (`MainMenu`, `Loading`, `GameOver`), cada una con un GameObject + script. ~30 min.
- 🟢 Configurar Build Settings: agregar las 4 escenas (MainMenu, Loading, Chapter1_Salon4B, GameOver) y orden. ~10 min.
- 🟢 Conectar el flujo: botón Jugar → Loading → Chapter1 → si mueres GameOver → reinicia. ~30 min.
- 🟢 Disparar BlackoutEvent al resolver la cerradura. ~10 min.
- 🟢 Crear el "pasillo" final con 4–6 cubos + 1 luz. ~30 min.
- 🟢 Outro card al llegar al final del pasillo. ~20 min.
- 🟡 Pulido si queda tiempo.

### Miércoles 29 — Build y entrega (1.5h)

- 🟢 `File → Build Settings → Build` para Windows. ~30 min con primer build (a veces tarda).
- 🟢 Probar el `.exe` desde 0, partida completa. ~30 min.
- 🟢 Si hay errores, corregir lo crítico, rebuild.
- 🟢 ZIP + entrega + changelog 008.

### Total realista de tu tiempo en Unity GUI

| Día | Tu tiempo en GUI de Unity | Tu tiempo total |
|---|---|---|
| Sábado | 0 min | 30 min (validar plan) |
| Domingo | ~2h sumadas | ~9h |
| Lunes | ~1.5h | ~2.5h |
| Martes | ~1.5h | ~2.5h |
| Miércoles | ~30 min | ~1.5h |
| **Total** | **~5.5h en GUI** | **~16h** |

Las **5.5h en GUI** distribuidas así son la diferencia entre un plan realista y uno fantasía. Sin la estrategia de UI-por-código serían **15h+** en GUI, lo cual no entra.

---

## 8. Riesgos y plan B

| Riesgo | Probabilidad | Plan B |
|---|---|---|
| Respiración rítmica se siente "robotica" o "aburrida" | Media | Acompañar SIEMPRE con audio guía y vibración visual del círculo. Si aún así no funciona, reducir el ciclo a 3-3-1 para que sea más activo. |
| Henry no entrega assets de la escena | Alta | Trabajamos sobre lo que ya hay en `Chapter1_Salon4B`. Las "habitaciones nuevas" se hacen con cubos. El profe entiende que es prototipo. |
| Carlos no entrega audios | Alta | Reusamos los loops del Sprint 1 + audios libres CC0 (freesound.org) que Duvan baja en 10 minutos. |
| Mando PS5 no se detecta al primer intento | Media | El Input System detecta cualquier gamepad como `Gamepad.current`. Si DualSense no funciona, el profe puede jugar con teclado/ratón sin perder ninguna mecánica P0. |
| Mic del casco no calibra bien (ruido, hardware) | Media | El propio sistema de calibración detecta esto y activa **fallback automático a tecla Espacio**. La mecánica de respiración sigue funcionando. |
| Mic detecta exhale de forma muy ruidosa (falsos positivos) | Media | El threshold se calcula del jugador real en calibración, no fijo. Además, exigimos amplitud sostenida 2.5s, no picos puntuales. Si aún falla → fallback. |
| El build da errores el miércoles | Media | Hacer un build de prueba el lunes en la noche, no esperar al miércoles. |
| El minijuego de combinación no comunica bien la respuesta | Media | Mostrar texto de feedback ("clic mecánico", "se traba", "se abre"). |
| Lo construido el domingo no integra bien el lunes | Media | Domingo: construir TODO en `Dev_Duvan` aislado. Lunes: portar a la escena real con cuidado. |

---

## 9. Decisiones validadas (2026-04-25)

1. ✅ **Pivote del puzzle a respiración + estrés** confirmado. Esto es el núcleo del juego.
2. ✅ **Respiración por mic del casco/auriculares (P0)** con fallback a tecla Espacio si la calibración falla. El mic del DualSense **NO** se usa para respirar.
3. ✅ **Control dual P0**: teclado/ratón **Y** gamepad (DualSense u otro), ambos al mismo nivel. Features especiales del DualSense (haptics, adaptive triggers) quedan en P2.
4. ✅ **Instrucciones paso a paso por la GUI de Unity** estilo changelog 005 (Hierarchy → Add Component → tal campo). Yo escribo los `.cs`, tú haces los pasos.
5. ✅ **Intro y outro = pantallas de texto + audio**, sin cinemática 3D.
6. ✅ **Slice = aula + pasillo corto + habitación final**.
7. ✅ **Game Over reinicia desde último checkpoint** (no desde el principio del juego). Si en la práctica los checkpoints quedan muy juntos o el sistema da problemas, reiniciamos desde el principio del nivel.

### Decisiones adicionales validadas (2026-04-25, sesión de tarde)

8. ✅ **Mecánicas de movimiento**:
   - **Caminar / correr / mirar**: ya implementadas en `PlayerController.cs`.
   - **Agachar (Ctrl)**: ya implementado en `PlayerController.cs` (lerp altura 1.8 → 1.0). NO hay que tocarlo.
   - **Saltar**: NO se implementa. No encaja en horror primera persona y no hay puzzle que lo requiera.
9. ✅ **Linterna = narrative unlock, no inventario**. Al inicio bloqueada. Tras abrir la cerradura ocurre el apagón y aparece overlay "Tenías una linterna en el bolsillo." → desde ese momento F (o Square del mando) la enciende/apaga. NO se equipa, NO se desequipa, no hay slot.
10. ✅ **Inventario = diario de notas leídas**. Tecla **I** / **Touchpad del mando** abre un panel con todas las notas que el jugador ya ha leído. Permite releerlas para resolver la cerradura. NO hay items físicos, NO hay drag&drop, NO hay equipar. Solo lectura.
    - Arquitectura del `Inventory.cs`: queda preparada para soportar items genéricos en el futuro (post-entrega), pero en el slice solo se usa para notas.
11. ✅ **Espacios del slice (3 espacios encadenados)**:
    - **Aula 4-B** (existente, Henry) — intro, tutorial, primer susto, primera nota, exploración.
    - **Pasillo oscuro** (cubos + luces apagadas + linterna) — apagón, segundo susto, segunda nota.
    - **Sala final** (cubos formando una habitación pequeña + pedestal con cerradura) — cerradura, abrir, outro.
    - **Truco visual**: cubos a oscuras con linterna se ven como horror creíble (precedentes: Outlast, Amnesia). La oscuridad esconde la falta de detalle.
12. ✅ **Hardware verificado el 2026-04-25**:
    - Mando DualSense: detectado por Windows, todos los botones y sticks responden.
    - Mic del casco: barra de Input reacciona claramente entre silencio / voz / exhale → la mecánica de respiración va a funcionar.

---

## 10. Lo que NO vamos a hacer (lista de scope cortado)

- Las 3 intrusiones completas (solo Observer en P0; los otros dos si entran como decoración).
- IA de "El Que Se Sienta Delante" como personaje complejo.
- Cinemáticas 3D con timeline.
- Sistema de guardado en archivo (cada partida es desde el inicio).
- Configuración avanzada (sensibilidad, volumen) — un slider de volumen master máximo.
- Subtítulos sincronizados con tiempos exactos.
- Localización a otros idiomas.
- Build para otra plataforma que no sea Windows.

Si algo de esto entra, entra como bonus en martes/miércoles, NO antes.

---

## 11. Cómo trabajamos tú y yo

### División clara de roles

**Lo que yo hago (en código, no toca tu tiempo)**:
- Todos los `.cs` de mecánicas.
- **UI generada por código** (procedural). Esto es la clave para que el plan sea realista: en lugar de pedirte que armes Canvases, Images, Texts, Sliders y anclajes en Unity, mis scripts construyen toda la UI en `Awake()` cuando arranca la escena. Tú solo arrastras un GameObject "HUDBootstrapper" a la escena y la UI aparece sola.
- Los `.md` de cada changelog.
- Diseño de los acertijos, números de combinación, textos de notas, tips de respiración.

**UI que generaré por código (no la armas tú en Unity)**:
- Barra de estrés del HUD.
- Círculo pulsante de respiración.
- Panel de calibración del micrófono (aparece al inicio).
- Overlay de lectura de notas.
- UI de la cerradura por combinación.
- Pantalla de Game Over.
- Reticle de la mira (punto / ojo / mano).
- Tip de la pantalla de carga.

**Lo que tú haces en Unity (mínimo indispensable)**:
- Configurar 5 acciones nuevas en `InputSystem_Actions.inputactions` (la primera vez es la más larga; es UI confusa pero te la paso click por click).
- Arrastrar mis scripts a los GameObjects correctos.
- Asignar referencias del Inspector cuando un script tenga campos `[SerializeField]` que apunten a otros objetos.
- **Posicionamiento de mundo 3D**: nota sobre un pupitre, cerradura sobre el escritorio, Empty marcando la salida. Esto solo lo puedes hacer tú porque depende de cómo se ve tu escena.
- Crear el SpotLight de la linterna como hijo de la cámara.
- Crear las escenas de Main Menu y Game Over (escenas vacías con un GameObject que tenga mi script).

### Formato de instrucciones

- Cada paso lo recibes formato changelog 005: "Hierarchy → click derecho → Create Empty → nombrar X → Add Component Y → poner valor Z".
- Si algo no funciona, mándame screenshot del Inspector o del Console y lo arreglamos.
- Cada bloque grande del plan se cierra con un mini-changelog en `Document/Changelog/` numerado (006, 007, 008…) para que quede rastro.
- El plan vive aquí. Si cambia, editamos este archivo y dejamos en `Document/Changelog/` el motivo del cambio.

---

## 12. Definition of Done — miércoles 29

Para considerar la entrega completa:

- [ ] Build `.exe` que arranca sin errores en una máquina con Windows.
- [ ] Main menu funcional con Jugar / Salir, navegable por teclado, mouse y gamepad.
- [ ] Pantalla de carga con 1 tip de regulación emocional.
- [ ] Pantalla de calibración del micrófono con fallback a tecla Espacio.
- [ ] Una partida jugable de 30–45 minutos con principio, medio y fin.
- [ ] **Control dual funcional**: el jugador puede completar la partida usando solo teclado/ratón **o** solo gamepad.
- [ ] Sistema de estrés visible y reactivo a eventos.
- [ ] Mecánica de respiración por mic (con fallback teclado) que efectivamente baja el estrés.
- [ ] 1 puzzle de combinación con su pista en notas del entorno.
- [ ] 1 momento de intrusión (Observer).
- [ ] 1 momento de oscuridad con linterna.
- [ ] Outro con el mensaje pedagógico.
- [ ] Game Over funcional con reinicio.
- [ ] Changelog de la entrega.

---

## 13. Personalización visual y polish (sin diseñador)

Toda la UI se construye por código, pero **no significa "fea"**. Significa que controlamos cada píxel programáticamente y se puede personalizar a fondo sin Henry. Esta sección queda estipulada para no olvidar el plan de pulido.

### Qué SÍ podemos lograr nosotros

**1. Tipografía** (el factor más importante para que se vea diseñado).

Google Fonts → fuentes gratuitas, descargables como `.ttf`, libres para uso comercial. Se importan a `Assets/Art/Fonts/`, se convierten a TextMeshPro con un clic, y mis scripts las usan vía `[SerializeField] TMP_FontAsset`.

Recomendaciones para *Están Dentro*:
- **Special Elite** — máquina de escribir vintage. Perfecta para notas del juego.
- **Creepster** — clásico horror. Buena para Main Menu y Game Over.
- **VT323** — terminal CRT antigua. Muy efectiva para HUD si buscamos estética analógica.
- **IM Fell English** — libro antiguo. Buena para narración / outro.

Plan: Duvan elige **1 fuente para títulos + 1 para cuerpo**.

**2. Paleta de colores** — expuestos en cada script con `[SerializeField] Color` para ajustar desde Inspector sin tocar código.

Propuesta base (modificable):
- Fondo `#0A0A0A` (negro casi puro).
- Estrés alto `#8B1A1A` (rojo apagado).
- Texto `#E8E2D5` (blanco hueso).
- Acento calmo `#3A5A3A` (verde apagado) o `#C9A961` (ámbar).

**3. Iconos y elementos visuales** (gratis, CC0 / CC BY):
- **game-icons.net** — 4000+ iconos vectoriales (ojos, manos, llaves, calaveras, pulmones para respiración).
- **Kenney.nl → UI Pack** — botones, marcos, paneles minimalistas CC0.
- **itch.io** sección free assets — packs UI horror.

**4. Texturas de fondo** (pantallas de carga, Game Over, menú):
- Pexels / Unsplash → texturas de papel grunge gratuitas.
- O las generamos por código: vignette + ruido + grano de película.

**5. Efectos visuales programáticos** (los hago yo en código, sin asset):
- Easing en animaciones (curvas, no lineal).
- Texto letra-a-letra estilo máquina de escribir (notas, intro, outro).
- Pulso en barra de estrés cuando supera 70.
- Shake leve al subir bruscamente el estrés.
- Vignette dinámica que se cierra con el estrés (HDRP nativo).
- Saturación que cae con estrés alto (mundo se vuelve gris).
- Estática de TV sutil en momentos de pánico máximo.

### Lo que NO vamos a lograr solos (límites honestos)

- Logo custom dibujado con identidad gráfica → reemplazo: título tipográfico ("ESTÁN DENTRO" en Creepster con leve glow).
- Ilustraciones originales (retratos, splash art) → no entran.
- Iconos exactamente alineados al lore → usamos los neutros de game-icons.net.
- Modelos 3D de objetos del mundo (lonchera, llave detallada, pizarra) → si Henry no entrega, son cubos. Pero **la UI no depende de él**.

### Principio de implementación

Yo escribo los scripts de UI desde el inicio con campos `[SerializeField]` para color, fuente, curva de animación, intensidad de efecto, etc. **Todo se ajusta desde el Inspector, sin volver al código.** Eso permite que el martes (día de polish) iteremos visual sin que yo reescriba scripts.

### Tiempo extra estimado para polish

| Polish | Tiempo | Cuándo |
|---|---|---|
| Importar 1-2 fuentes y aplicarlas en TMP | 30 min | Sábado/Lunes en descansos |
| Definir paleta y aplicarla a todos los `[SerializeField]` | 30 min | Lunes |
| Vignette + saturación reactivas al estrés | 1h | Martes |
| Texto letra-a-letra en notas + intro/outro | 30 min | Martes |
| Estática + shake en momentos de tensión | 1h | Martes |
| Buffer ajustes finos | 30 min | Martes |
| **Total** | **~4h** | Distribuidas lunes/martes |

Estas 4h salen del bloque "pulido" del martes. **No comen el plan principal.**

### Tareas concretas para no olvidar

- [ ] **Mañana (domingo) en algún descanso (10 min)** Duvan entra a https://fonts.google.com, elige 1 fuente título + 1 fuente cuerpo, descarga `.ttf` y los deja en `Assets/Art/Fonts/`.
- [ ] **Lunes** importamos las fuentes a TextMeshPro y las cableamos a los scripts.
- [ ] **Lunes** Duvan elige paleta final (puede arrancar con la propuesta base, ajustar después).
- [ ] **Martes** sesión dedicada a polish visual (4h aprox.) — pasada por cada pantalla: calibración, HUD, notas, cerradura, Game Over, menú, intro, outro.
- [ ] **Opcional**: si Duvan tiene referencia visual (Phasmophobia, Outlast, Layers of Fear, Fears to Fathom, MADiSON), compartirla antes del lunes para afinar la dirección.

### Resultado esperado

UI estilo "indie horror pulido" — no AAA, pero claramente trabajada, coherente y profesional para un proyecto universitario. El profe verá cuidado en los detalles aunque no haya artista en el equipo.

---

## Pivote 2026-04-26 (tarde) — Fallback teclado como modo principal

Después del intento con DualSense, Unity abrió el device correctamente pero el RMS quedó en 0.00001 (Unity no recibe el stream, probablemente captura exclusiva por otra app). Tras ~1h debugando entre casco y DualSense, decisión validada con Duvan: **avanzar con fallback teclado como modo principal** y dejar el sistema de mic intacto por si en otra máquina funciona automáticamente.

Justificación:
- El fallback ya es estricto: el jugador debe sostener Space durante INHALA y soltarlo durante EXHALA. Si no aprieta nada, el ciclo no cuenta.
- La pedagogía se mantiene: el ritmo 4-4-2 visual + guía verbal + feedback ✓ enseña la técnica respiratoria. El mic era ideal pero no esencial.
- Tiempo recuperado: ~1h que va a Bloque 4 (interacción + linterna).
- Si el profe en su máquina conecta un mic que sí funcione, el sistema lo detecta automáticamente y usa mic.

Lo que NO cambia: el resto del minijuego, la calibración inicial (sigue corriendo, solo cae a fallback si no detecta), los textos del tutorial.

## Pivote 2026-04-26 — Mic DualSense en lugar de casco

Durante el bloque 3 del domingo se descubrió que el casco con jack 3.5mm de Duvan no es detectable como device separado por Unity. Windows lo enruta al "Varios micrófonos (Realtek)" que contiene también el mic interno del laptop, y el soplo del casco no se diferencia del ruido ambiente. Calibración con casco da gap exhale-noise negativo → fallback automático.

En cambio, el mic del DualSense (`Wireless Controller`) responde claramente al exhale suave en pruebas de Windows. Decisión validada con Duvan: **usamos el mic del DualSense como P0**, contradiciendo la tabla original de la sección 6.

Implicaciones:
- El tutorial debe enseñar al jugador cómo sostener el mando: ambas manos cerca de la boca, mic entre los gatillos apuntando a la cara, a 10-15 cm. Sin esa posición la detección no funciona.
- En la entrega del miércoles el profe debe usar el mando para la demo. Si decide jugar solo con teclado/ratón, el `BreathingInputProvider` cae al fallback automático (no se conecta el mando → no hay device).
- El `minThresholdGap` se baja de 0.02 a 0.005 porque la señal del DualSense es menor que la de un mic dedicado.
- El fallback teclado se endurece para evitar abuso: en INHALA hay que mantener Space, en EXHALA hay que soltarlo. Si nunca se aprieta Space, el ciclo no cuenta.

La tabla de la sección 6 queda obsoleta. La razón principal por la que se descartó el DualSense ("distancia 50 cm en las manos") se resuelve con la instrucción del tutorial: jugador acerca el mando a la boca durante la respiración.

---

**Próximo paso**: las 7 decisiones quedaron validadas el 2026-04-25. Mañana domingo 26 arrancamos con el bloque mañana del plan: action maps duales en Input System, extender `PlayerController`, y `StressSystem` + `BreathingMinigame` + `BreathingInputProvider` + `MicCalibration`.

Antes de arrancar mañana, ten listo:
- Unity abierto en el proyecto.
- Tus auriculares **con micrófono** conectados al PC.
- El mando DualSense (u otro gamepad) conectado por USB para verificar que Windows lo ve.
- Asegurarte que en Windows → Configuración → Sonido, el mic default sea el de tus auriculares.
