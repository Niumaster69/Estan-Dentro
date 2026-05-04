# Planificación lunes 27 — Pendientes críticos para la entrega

**Fecha:** 2026-04-26 (cierre del domingo, después de 16 bloques)
**Para retomar:** lunes 27 mañana
**Entrega:** miércoles 29

Este doc analiza los 4 problemas críticos que Duvan flaggeó al cierre del domingo, propone soluciones priorizadas y deja un plan de acción concreto para los 3 días que quedan.

---

## Estado actual del slice

End-to-end funcional:
- MainMenu → cinemática intro → wake-up → aula con todas las mecánicas → Game Over → reinicio.
- Pause → inventario / ajustes / salir al menú.
- Calibración mic con fallback teclado automático.
- Cerradura código `7-4-2-9` → apagón → linterna.

Lo que NO está cerrado todavía: lo que sigue.

---

## Problema 1: La mecánica educativa de respiración no funciona con mic

### Diagnóstico
- En la máquina de Duvan, ni el mic del casco (jack 3.5mm que Windows mezcla con array Realtek) ni el del DualSense (Unity recibe `RMS = 0.00001` aunque Windows sí capta) funcionan.
- Decisión vigente: fallback teclado como modo principal (Space sostenido en INHALA + soltar en EXHALA).
- Tutorial del minijuego in-game ya implementado.

### Por qué es crítico
- El profesor pidió específicamente que el jugador respire físicamente. Sin eso, el mensaje pedagógico (regular ansiedad/miedo en la vida real) se diluye a "apretar Space rítmicamente".
- Es el diferenciador del juego.

### Opciones a evaluar (decidir lunes)

**Opción A — Conseguir un mic USB que sí funcione en Unity**
- Cualquier mic USB barato suele aparecer como device propio en `Microphone.devices` y funcionar sin pelearse con drivers.
- Probar antes del miércoles para tener certeza.
- Costo: ~$15-30 USD un mic USB básico.
- **Pro:** resuelve el problema técnico definitivo.
- **Con:** dependencia de hardware nuevo.

**Opción B — Endurecer el fallback teclado para que sea pedagógico**
- Que el fallback exija CONSISTENCIA TEMPORAL muy estricta: si el ritmo se aleja del 4-4-2 más allá de cierta tolerancia, el ciclo no cuenta.
- Agregar audio guía del metrónomo (clave para que el jugador "respire con el ritmo" aunque sea con teclas).
- Pedagógicamente: enseña el patrón 4-4-2 (variante de 4-7-8) que es técnica real, aunque no detecte exhale físico.
- **Pro:** sin dependencia de hardware nuevo.
- **Con:** nunca va a ser igual de efectivo que respirar de verdad.

**Opción C — Combinar A + B**
- Sistema sigue intentando mic (si el profe usa otra PC con mic funcional, automáticamente lo usa).
- Si no hay mic, fallback estricto + audio guía.
- Demo en máquina de Duvan funciona aunque sea con fallback.
- Si Duvan consigue mic USB: demo perfecta.

### Recomendación
**Opción C**. Conseguir mic USB barato esta semana. Mientras tanto, endurecer fallback teclado + agregar audio metrónomo.

### Acciones lunes
- [ ] Decisión: ¿conseguir mic USB? Decidir presupuesto.
- [ ] Hacer fallback teclado más estricto: requerir Space presionado durante TODA la fase INHALA (no solo 3s mínimo) y soltado durante TODA la fase EXHALA (no solo 2s sostenido). Con tolerancia mínima.
- [ ] Agregar visual feedback claro: barra de "consistencia respiratoria" que muestra qué tan bien el jugador está manteniendo el ritmo.

---

## Problema 2: El potencial del DualSense no se aprovecha

### Diagnóstico
- Solo usamos el mando como input estándar (botones, sticks, R2 para Crouch, Triangle para respirar fallback, Touchpad para inventario).
- NO usamos: vibración (haptics), adaptive triggers (resistencia variable), lightbar (color reactivo).
- El plan original lo tenía como P2.

### Por qué importa
- El DualSense es uno de los pocos mandos con haptics avanzados. Para horror, vibración bien usada agrega MUCHO al feel.
- Lightbar reactivo da feedback visual en el mando mismo (el jugador SIENTE el estado del juego en sus manos).
- Es bonus impactante para el demo.

### Opciones a evaluar (decidir lunes)

**Opción A — Solo vibración básica reactiva al estrés**
- Pulsos suaves cuando estrés sube de 60 (ritmo cardíaco acelerado).
- Pulso fuerte único al disparar intrusión (sobresalto).
- Implementación: `Gamepad.current.SetMotorSpeeds(low, high)` desde Unity Input System. ~30 min.
- **Pro:** alto impacto, bajo esfuerzo.

**Opción B — Vibración + lightbar reactivo**
- Vibración como en A.
- Lightbar cambia color según estrés (verde calmo, amarillo medio, rojo alto). Pulsa cuando hay intrusión.
- Implementación HD del lightbar: `DualSenseGamepadHID.SetLightBarColor()` (Input System tiene API). ~1h en total.
- **Pro:** muy impactante visualmente.

**Opción C — Vibración + lightbar + adaptive triggers**
- Lo de B + R2 (Crouch) tiene resistencia mayor cuando estrés alto (cuesta más agacharse cuando tenés miedo).
- Implementación de adaptive triggers requiere DualSense API más compleja. ~2h.
- **Pro:** experiencia DualSense completa.
- **Con:** más scope.

### Recomendación
**Opción B (vibración + lightbar)**. Impacto alto, esfuerzo manejable. Adaptive triggers para post-entrega.

### Acciones lunes/martes
- [ ] Implementar `DualSenseFeedback` script que escucha StressSystem.OnStressChanged y aplica vibración.
- [ ] Agregar lightbar reactivo al mismo evento.

---

## Problema 3: Falta planificar todo el audio

### Diagnóstico
**Lo único que tenemos:**
- `SubliminalLoopController.cs` con un loop ambient (Sprint 1, changelog 003).

**Lo que NO tenemos (lista completa):**

| # | Audio | Cuándo se dispara | Importancia |
|---|---|---|---|
| 1 | Ambient base sutil del aula | Continuo pero MUY bajo | Alto |
| 2 | Tic ocasional / chasquido | Aleatorio cada 30-90s en exploración | Medio |
| 3 | Susurro distante | Cuando estrés > 50 | Alto |
| 4 | Golpe en pizarra / chillido | Al disparar intrusión Observer | Crítico |
| 5 | Pasos del jugador | Al caminar | Medio |
| 6 | Pasos del Observador detrás | Cuando intrusión está activa (panning estéreo) | Alto |
| 7 | Click mecánico | Al cambiar dígito de cerradura | Medio |
| 8 | Click metálico fuerte | Al confirmar cerradura correcta | Medio |
| 9 | Buzzer / negativo | Al confirmar cerradura incorrecta | Bajo |
| 10 | Golpe seco + parpadeo | Al disparar BlackoutEvent | Crítico |
| 11 | Zumbido grave | Durante el blackout (continuo bajo) | Alto |
| 12 | Click linterna | Al encender/apagar linterna | Bajo |
| 13 | Inhalar/exhalar guía | Sincronizado con minijuego respiración | Crítico |
| 14 | Latido cardíaco | Cuando estrés > 80 (continuo crescendo) | Alto |
| 15 | Voz en off intro | Cinematic_Intro narrativo | Medio |
| 16 | Whoosh transición | Al cambiar de escena | Bajo |

### Por qué es crítico
- El audio es 50% del horror. Sin audio, el juego se siente plano.
- El profe va a notar inmediatamente la falta.

### De dónde sacar los audios

**Carlos** (responsable de audio en el equipo):
- Si entrega, integrar lo que dé.
- Confirmación: ¿Carlos va a entregar audio para el miércoles? Hablar con él.

**Freesound.org** (CC0 / CC BY libres):
- Bajar los críticos:
  - "Door slam horror" → para apagón.
  - "Whisper female creepy" → para susurro.
  - "Heartbeat slow" → para latido.
  - "Click mechanical" → cerradura.
  - "Buzz hum low" → blackout.
- Tiempo: ~30 min para bajar 8-10 sounds críticos.

**ZapSplat / OpenGameArt** alternativas también.

### Recomendación
1. Hablar con Carlos lunes mañana. Confirmar qué entrega.
2. Lo que falte, bajar de freesound el lunes tarde.
3. Integrar audio críticos (intrusión, blackout, respiración) primero. Resto si queda tiempo.

### Acciones lunes
- [ ] Hablar con Carlos: ¿qué entrega y cuándo?
- [ ] Si Carlos no entrega los críticos: bajar de freesound (intrusión, blackout, respiración guía, latido).
- [ ] Crear `AudioManager` que centraliza el playback de SFX (one-shot) por nombre.
- [ ] Conectar a eventos: `StressSystem.OnCollapse`, `IntrusionManager.onObserver`, `BlackoutEvent.OnBlackout`, `BreathingMinigame` (inhale/exhale tick).

---

## Problema 4: El loop ambient continuo no genera horror real

### Diagnóstico
- Ahora hay un `SubliminalLoopController` que toca un loop continuo todo el tiempo.
- Duvan flagged: un sonido continuo se vuelve white noise. El terror real viene del CONTRASTE silencio ↔ sonido inesperado, no de un loop perpetuo.

### Por qué tiene razón
- Outlast, Visage, Madison: usan SILENCIO mucho más que sonido. El silencio prolongado genera tensión. El sonido inesperado en momento clave provoca el sobresalto.
- Loop continuo = el cerebro lo filtra. Pierde poder.

### Estructura recomendada (capas de audio)

**Capa 1 — Ambient base (continuo MUY bajo)**
- Solo el rumor de un aula vacía. Ruido del edificio, lejano.
- Volumen bajísimo (-25 dB del master).
- Apenas perceptible. No "música", solo presencia.

**Capa 2 — Stingers (eventos puntuales en momentos clave)**
- Disparados por eventos del juego.
- Volumen normal (no muy alto pero claro).
- Lista mínima:
  - Crujido de madera al pasar cerca de un pupitre (random raro).
  - Gota de agua que cae (random raro).
  - Tic-tac de reloj acelerándose cuando estrés sube de 40.
  - Susurro indistinguible cuando estrés > 60 (corto, 1-2s).
  - Golpe seco al disparar intrusión.

**Capa 3 — Latido cardíaco (reactivo al estrés)**
- Inactivo cuando estrés < 50.
- Aparece a estrés 50, ritmo lento.
- Acelera con estrés más alto.
- Crescendo a estrés 80+.
- Cuando bajás estrés con respiración, el latido también baja.

**Capa 4 — Audio guía del minijuego respiración**
- Solo activa cuando el círculo del minijuego está visible.
- Tono suave para INHALA (sube), tono más bajo para EXHALA (baja), silencio en PAUSA.
- Puede ser una voz susurrada que dice "inhala... exhala..." o solo tonos puros.

### Acciones lunes/martes
- [ ] Refactorizar `SubliminalLoopController` para que sea solo la Capa 1 (ambient base bajo).
- [ ] Crear `AudioStingerSystem` que dispara stingers random cada N segundos en exploración.
- [ ] Crear `HeartbeatSystem` que escucha StressSystem y reproduce latido reactivo.
- [ ] Audio guía del minijuego: agregar AudioSource al `_Breathing` con clips inhale/exhale, sincronizado con las fases del BreathingMinigame.

---

## Plan de acción concreto — lunes/martes/miércoles

### Lunes 27 (estimado 4-5h)
- [ ] **Build .exe de prueba** (30 min) — verificar que todo compila empaquetado.
- [ ] **Limpieza pre-entrega** (10 min) — apagar debug keys, logs.
- [ ] **Pasillo final + sala outro** (1.5h) — cierre del slice (CRÍTICO de Definition of Done).
- [ ] **Hablar con Carlos** sobre audio.
- [ ] **Mic USB**: decidir si comprar/conseguir uno hoy.
- [ ] **Endurecer fallback teclado** del minijuego (30 min).

### Martes 28 (estimado 4-5h)
- [ ] **DualSense vibración + lightbar** (1h).
- [ ] **AudioManager + integración de SFX críticos** (2h).
  - Intrusion sound, blackout, click cerradura, latido reactivo.
  - Audio guía del minijuego de respiración.
- [ ] **Refactorizar SubliminalLoop** a sistema de capas (1h).
- [ ] **Frases de pizarra dinámicas** (45 min) — usar las 10 frases de Carlos sobre `Cubo.002`.
- [ ] **Polish tipografía** en otros overlays (Note, Lock, Inventory, Settings).

### Miércoles 29 (estimado 2-3h)
- [ ] **Build final**.
- [ ] **Test completo end-to-end** en una máquina limpia si es posible.
- [ ] **Probar con mic USB** si lo conseguiste.
- [ ] **Changelog 008** (final del slice).
- [ ] **Entrega**.

---

## Lo que requiere DECISIÓN ANTES de codear el lunes

1. **Mic USB**: ¿comprar/conseguir? Decisión hoy domingo o mañana lunes temprano.
2. **Carlos**: ¿qué audio va a entregar y cuándo?
3. **Multi-escena (Salón / Ductos / Oficina)**: con tan poco tiempo, ¿lo dejamos para post-entrega y entregamos con Salón único + pasillo + outro? **Mi recomendación: SÍ, dejar multi-escena para después de la entrega.** El slice con 1 escena cierra el Cap. 1 perfectamente con pasillo + outro.
4. **DualSense feedback**: ¿vibración + lightbar es prioridad sobre frases de pizarra? Mi recomendación: VIBRACION sí, lightbar nice to have.

---

## Riesgos a vigilar

- **Build .exe**: nunca lo probamos. Puede romper el primer intento (típico de Unity con HDRP). Por eso lunes mañana sí o sí.
- **Audio**: si Carlos no entrega, el martes vas a estar bajando assets sin parar. Mejor confirmar lunes mañana.
- **Mic**: si no se consigue mic USB, la entrega va con fallback teclado. El profe puede preguntar específicamente y debemos tener respuesta clara.

---

## Archivos relevantes (para retomar mañana)

- `Document/PLAN_ENTREGA_FINAL.md` — plan maestro (con pivotes del 26).
- `Document/Changelog/006_*.md` — bloques 1-5.
- `Document/Changelog/007_*.md` — bloques 6-14.
- `Document/PLANIFICACION_LUNES_PENDIENTES.md` — este archivo.
- Memoria del proyecto: `~/.claude/.../memory/project_estan_dentro.md`.
