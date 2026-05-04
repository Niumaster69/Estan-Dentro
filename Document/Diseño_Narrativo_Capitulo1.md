# Diseño Narrativo — Capítulo 1
## "El colegio al que nunca llegué"

**Fecha del diseño:** 2026-05-02
**Autores:** Duvan + Claude
**Estado:** **Cerrado y confirmado** (incluido el twist del padre vivo y la inclusion de micro-momentos atmosfericos durante gameplay).

---

## 1. Resumen ejecutivo (1 parrafo)

Un adolescente despierta en su aula vacia. El reloj esta detenido. Las luces parpadean. Una sombra adulta se asoma en la distancia y le sube el corazon. Conforme explora, encuentra cartas que pertenecen a un dia que el no recuerda. Su padre lo llevaba al colegio en su cumpleaños. Hubo un accidente. Lo que el jugador cree que es un juego de horror sobre un fantasma encerrado en una escuela termina siendo el viaje interno de un chico en coma intentando despertar. La respiracion no es solo mecanica: es lo que lo trae de vuelta.

---

## 2. Mensaje pedagogico

> **"El miedo te hace creer cosas que no son. Respira. Mira lo que es real. Volve."**

Variantes para cinematicas/dialogos:
- "El cuerpo recuerda lo que la mente bloquea."
- "No estabas en el aula. Estabas dentro de vos."
- "Lo que no podes soltar te encierra. Respirar es la llave."

---

## 3. La verdad del juego (canon — solo para el equipo)

> Esta seccion **no se le revela explicitamente al jugador**. Se sembra en cartas, audio subliminal, indicios visuales. El jugador puede llegar a la verdad por inferencia.

### Hechos canonicos

1. El protagonista (16 años, sin nombre — para que el jugador se proyecte) cumplia años el **17 de mayo de 2023**.
2. Su madre fallecio años antes (cancer, no se especifica). El padre y el hijo viven solos.
3. Como regalo de cumpleaños, su padre se ofrece a llevarlo al colegio en auto en lugar de que tome el bus de siempre. Quiere pasar el dia juntos: lo lleva a la escuela y lo recoge para celebrar a la noche.
4. Sale del bus de siempre por capricho del padre. **Carga incipiente de responsabilidad**: si hubiera tomado el bus, nada habria pasado.
5. **El accidente ocurre camino al colegio.** Otro vehiculo (no es relevante quien tuvo la culpa). Choque de costado.
6. **El padre sobrevive con heridas serias pero vive.** El protagonista entra en coma traumatico.
7. **Tres años despues** (es ahora el dia del juego), el protagonista sigue en coma. El padre lo visita cada semana. Cada cumpleaños del hijo (17/05) trae flores y se queda mas tiempo del normal.

### El "juego" es el espacio mental del coma

- El protagonista construyo en su mente el aula a la que iba ese dia, como si el viaje hubiera terminado.
- Como no puede cargar con la culpa de que su padre este vivo "por culpa de el" (por haberle pedido que lo llevara), su mente reprime la realidad y construye una version donde **el padre tambien murio**.
- Cada "loop" del juego es un intento del cerebro del protagonista de procesar el trauma. Cada partida que el jugador juega es un intento mas. Esto justifica narrativamente las partidas multiples + logros (mas intentos = mas piezas que recuerda).
- El **Observer** es el padre tratando de alcanzarlo desde "afuera" (al lado de la cama del hospital, sosteniendole la mano, hablandole). Como la mente del protagonista lo cree muerto, su presencia genera estres en lugar de paz.
- La **respiracion masculina** del loop subliminal del changelog 003 (la que ya esta implementada como audio continuo en el aula) es **literalmente** la respiracion del padre velandolo al lado de la cama del hospital. El protagonista la oye subliminalmente todo el tiempo y no sabe por que.

### El twist al final del capitulo

- En la ultima sala (Sala de Profesores) el protagonista encuentra una **placa "En memoria de Mendoza"**. Inicialmente lee solo "Apellido" y asume que es de su padre.
- Al revisar la inscripcion completa con la luz justa, la placa dice **"En memoria de Carolina Mendoza"**. Era de su madre, no del padre.
- El padre sigue vivo. Las visitas que muestra la Carta 6 son reales.
- Aceptar que su padre vive = soltar la culpa = despertar.

### Final ambiguo

- Cinematica final: pantalla blanca. Una mano sostiene la mano del protagonista. **No se ve quien.**
- El jugador puede asumir que es el padre. Puede asumir que es otra persona. Puede asumir que es metafora pura. Queda abierto.
- La respiracion subliminal del loop pasa de fondo a primer plano un segundo, luego silencio. Fin.

---

## 4. La contraseña: **1705**

Formato: DDMM (dia y mes del accidente, formato latinoamericano).

- **17** = 17 (cumpleaños del protagonista, dia del accidente)
- **05** = mayo

### Por que esta fecha

- Es el cumpleaños del protagonista. El accidente ocurrio el dia que cumplia 16 años. Cada año que pasa en coma, su cumpleaños es el aniversario del accidente. **Esta es la herida central**.
- Para el jugador, llegar a esa cifra y abrir la cerradura es el momento en que **el juego le confirma**: ese dia te marco para siempre. Pero tambien: ese dia se puede volver a vivir si volves.

### Como se siembra (no se entrega gratis)

- El **17** aparece en multiples lugares antes de poder ser leido como dia: numero del salon (17B), numero del bus que NO tomo, hora marcada en el reloj parado del aula (las 17:00), edicion del periodico abandonado (numero 17 de la serie escolar), fecha en cuaderno de matematicas.
- El **05** (mayo) aparece como mes en boleto del bus, en la tarjeta del hospital, en el calendario de la pared con un dia marcado, en la firma de una carta del padre.
- El jugador puede juntar las pistas en cualquier orden. La cerradura acepta solo 1705.

### Ajuste tecnico

`CombinationLock.cs` actualmente tiene combinacion `7-4-2-9` (changelog 007). Cambiar a `1-7-0-5`. Cambio trivial en el Inspector del prefab o en el script.

---

## 5. Estructura de tres actos + outro

```
INICIO
  └─ Boot del juego
  └─ MainMenu (boton JUGAR)
  └─ CINEMATICA 1 — INTRO (texto + audio + imagen abstracta)
  └─ Pantalla de carga con tip de respiracion

ACTO 1 — DESPERTAR  (Salon Principal — escena ya construida)
  └─ Spawn + secuencia PlayerWakeUp
  └─ Exploracion inicial
  └─ CARTA 1 + CARTA 2 (cotidianas, antes del horror)
  └─ Primera intrusion del Observer (lejano)
  └─ Tutorial in-game de respiracion
  └─ MINI-PUZZLE 1: encontrar como subir al ducto
  └─ Subida al ducto (transicion)

ACTO 2 — TRANSITO  (Ductos / Laberinto — escena ya construida)
  └─ Linterna obligatoria
  └─ Bifurcaciones (laberinto suave)
  └─ Intrusiones mas frecuentes
  └─ CARTA 3 (encontrada en una caja del ducto)
  └─ CINEMATICA 2 — FLASHBACK DEL ACCIDENTE (al encontrar Carta 3)
  └─ CARTA 4 (mas adelante en el ducto)
  └─ Salida al pasillo de la escena 3 (transicion)

ACTO 3 — REVELACION  (Escena Secundaria multi-sala — escena ya construida)
  └─ Pasillo central con 3 puertas
    ├─ Sala de descanso        → CARTA 5 + indicio visual
    ├─ Tercera sala (a definir)→ indicio visual fuerte
    └─ Sala de juntas profesores → CARTA 6 + cerradura final
  └─ MINI-PUZZLE 2: ingresar 1705 en la cerradura
  └─ Apertura de la caja: cuaderno de visitas del hospital
  └─ Lectura del cuaderno (descubrimiento sin texto explicito)

OUTRO
  └─ CINEMATICA 3 — DESPERTAR AMBIGUO
  └─ EndOfChapterOverlay (ya implementado: tiempo, logros, puntos)
  └─ Vuelta al MainMenu
```

---

## 6. Flujo de juego paso a paso

### CINEMATICA 1 — INTRO (al pulsar JUGAR)

Estructura tipo "Cinematic_Intro" que ya existe (texto+audio, no 3D).

```
[0:00-0:04]  Pantalla negra. Silencio absoluto.
[0:04-0:08]  Texto desvanece "Hoy cumplo 16."
[0:08-0:12]  Texto desvanece "Pa me lleva al cole."
[0:12-0:16]  [Audio: motor de auto encendiendo, voz adulta tarareando bajo]
[0:16-0:20]  Texto desvanece "Hace tres años que mama no esta."
[0:20-0:25]  Texto desvanece "Hoy quiere que celebremos."
[0:25-0:30]  [Audio: motor en carretera, conversacion lejana ininteligible]
[0:30-0:33]  Texto desvanece "Pero..."
[0:33-0:34]  [Audio: chirrido de frenos, impacto, pantalla blanca un frame]
[0:34-0:38]  Silencio total. Pantalla negra.
[0:38-0:42]  Texto: "Despertame, pa."
[0:42-0:46]  Fade out.
```

**Audios necesarios:** motor auto (suave), tarareo masculino bajo, chirrido de frenos, impacto seco. Carlos puede armar esto con stocks.

### Pantalla de carga (LoadingScreenController existente)

Tip rotativo: una de estas frases (random):
- "La respiracion lenta calma al sistema nervioso."
- "Inhalar 4. Sostener 4. Exhalar 6."
- "Mirar a 5 metros relaja la vision peripherica."
- "Si el corazon se acelera, no es peligro. Es energia."

### ACTO 1 — Salon Principal

| # | Momento | Que pasa | Audio | Trigger |
|---|---|---|---|---|
| 1 | Spawn | Player aparece sentado en pupitre (PlayerWakeUp) | F07-F10 (ya en brief) | Auto |
| 2 | Look around | Camara mira lentamente alrededor del aula | A01 (ambient base) + loop subliminal del changelog 003 | Auto, parte de PlayerWakeUp |
| 3 | Control devuelto | Jugador puede moverse libremente | A01 continuo | PlayerWakeUp termina |
| 4 | Detalles ambientales | Reloj de pared parado en 17:00, calendario con un dia marcado, pizarra con la fecha del accidente, mochila propia abierta en el pupitre | Stingers eventuales (B01-B04) | Pasivos |
| 5 | Carta 1 | Jugador encuentra cuaderno de matematicas en su pupitre. Lee | Audio sutil al levantar | Interaccion con pupitre |
| 6 | Primera intrusion | Una sombra adulta aparece al fondo del aula. No se acerca. Stress sube | C02 (intrusion Observer) | Trigger por gaze hacia un punto especifico (la pizarra o el pasillo) |
| 7 | Tutorial respiracion | Si stress > umbral, el minijuego de respiracion arranca con tutorial | G01-G05 | Stress alto + primera vez |
| 8 | Carta 2 | En el bolsillo de su propia chaqueta colgada en la silla. Lee | Audio papel | Interaccion con la chaqueta |
| 9 | Puerta bloqueada | Intentar abrir la puerta principal: no funciona. Mensaje sutil | Sound de puerta atascada | Interaccion |
| 10 | Mini-puzzle 1 | Notar que la rejilla del ducto esta floja (visual: tornillos sueltos). Mover la silla del profesor para alcanzarla. Quitar rejilla | Sonidos mecanicos | Interaccion + interaccion |
| 11 | Subida | Trepar al ducto. Camera baja, ambient cambia | Whoosh + transicion ambient | Interaccion final |

### ACTO 2 — Ductos

| # | Momento | Que pasa | Audio | Trigger |
|---|---|---|---|---|
| 1 | Entrada al ducto | Camara mas cerrada (FOV reducido). Linterna automatica activa | Linterna click + A02 (ambient blackout) | Auto |
| 2 | Avanzar | El loop subliminal sube de volumen progresivamente | El audio del accidente se vuelve mas claro: motor + freno lejano | Trigger por distancia |
| 3 | Bifurcacion | Dos caminos. Uno tiene la Carta 3. El otro tiene un dead-end con un indicio visual | Stingers | Pasivo |
| 4 | Carta 3 | Caja escondida con un boleto del bus. Lee | Audio papel + lapso del audio del accidente que queda | Interaccion |
| 5 | Cinematica 2 | Tras leer Carta 3, fade a blanco, flashback del accidente | Audio completo: motor, conversacion, frenazo, blanco | Auto al cerrar la carta |
| 6 | Vuelta al ducto | Player vuelve donde estaba pero con stress al maximo | Heartbeat F06 alto | Auto |
| 7 | Intrusion fuerte | El Observer aparece al final de un tramo del ducto, mas cerca que antes | C02 (intrusion fuerte) | Distancia |
| 8 | Respiracion forzada | Jugador casi obligado a usar el minijuego de respiracion | G01-G05 | Stress > umbral |
| 9 | Carta 4 | Antes de la salida, foto familiar arrugada en una esquina | Audio papel | Interaccion |
| 10 | Salida | Bajar a un pasillo de la escena 3 | Whoosh + cambio ambient | Interaccion |

### CINEMATICA 2 — FLASHBACK DEL ACCIDENTE (trigger: Carta 3)

```
[0:00-0:02]  Fade a blanco. Ambient se corta a silencio.
[0:02-0:08]  Imagen abstracta o vista subjetiva: salpicadero de auto, cinturon, vista parcial de un brazo adulto al volante.
             [Audio: motor auto, voz adulta cantando bajo "Feliz cumpleaños..."]
[0:08-0:14]  Texto que se desvanece sobre la imagen: "16 años."
             [Audio sigue]
[0:14-0:18]  Texto: "Pa, gracias por traerme."
             [Audio: voz tarareando se mezcla con interferencia leve]
[0:18-0:20]  La imagen distorsiona. Linea recta a curva.
             [Audio: chirrido de frenos lejano se acerca]
[0:20-0:21]  Pantalla blanca. Un frame. Silencio absoluto.
[0:21-0:24]  Negro. Pulso cardiaco lento aparece.
[0:24-0:26]  Vuelta al ducto.
```

### ACTO 3 — Escena Secundaria (multi-sala)

Pasillo central con 3 puertas visibles. El jugador puede entrar en el orden que quiera.

#### Sala de Descanso (cafeteria/sala estudiantes)

| Detalle | Que es |
|---|---|
| Mesa central | Bandeja con un plato de torta a medio servir, una vela apagada |
| Indicio visual | Cartel "Felices 16, [iniciales del protagonista]" colgado en una pared |
| Carta 5 | Audio guardado en un viejo telefono o reproductor (ver contenido en seccion 8) |
| Audio sala | Eco lejano de "Feliz cumpleaños", voces infantiles que se desvanecen |

#### Tercera sala (proponemos: enfermeria)

| Detalle | Que es |
|---|---|
| Camilla | Una camilla con sabanas removidas. Almohada hundida (alguien estuvo aca recien) |
| Indicio visual fuerte | Espejo. Al mirarlo, el reflejo no es exactamente el protagonista. Es el mismo pero con vendajes en la cabeza. Solo aparece 2 segundos y vuelve a la normalidad. NO hay carta aca, solo el indicio. |
| Plus | Boletin medico anonimo en la mesita: "Paciente PED-046, dia 1095 en coma. Estable." (1095 dias = 3 años) |

#### Sala de Juntas de Profesores

| Detalle | Que es |
|---|---|
| Mesa larga | Sillas vacias |
| Pizarra trasera | Hay un nombre escrito y borrado, aun visible |
| Placa de pared | "En memoria de Carolina Mendoza". El nombre completo se lee solo si se acerca y enfoca |
| Carta 6 | Cuaderno con anotaciones del padre (ver seccion 8) |
| Cerradura final | Caja sobre la mesa. Cerradura de 4 digitos. Combinacion: 1705 |

#### Mini-puzzle 2 — la cerradura

- Jugador junta los 4 digitos (1, 7, 0, 5) leyendo cartas + indicios visuales.
- Activa el mecanismo de la cerradura (LockOverlay existente).
- Si pone la combinacion correcta: caja se abre.
- Dentro de la caja: cuaderno de visitas del hospital, ultima entrada es de hoy. Firma: "Pa".
- Sin texto adicional. El jugador entiende.

### CINEMATICA 3 — DESPERTAR AMBIGUO (outro)

```
[0:00-0:03]  La luz del aula empieza a apagarse, lampara por lampara.
[0:03-0:08]  El protagonista queda en penumbra. Solo se escucha la respiracion subliminal masculina, ahora claramente cerca.
[0:08-0:14]  Voz suave, ambigua (no se sabe si es padre, si es el mismo protagonista, si es ambas):
             "Ya hiciste lo que tenias que hacer aca."
[0:14-0:18]  "Respira una vez mas."
[0:18-0:22]  Pantalla blanca. Un frame.
[0:22-0:26]  Frame estatico: una mano sostiene la mano del protagonista. NO se ve a quien pertenece la otra mano.
[0:26-0:30]  Pantalla negra.
[0:30-0:34]  Texto: "No estaba en el aula."
[0:34-0:38]  Texto: "Estaba dentro de mi."
[0:38-0:42]  Texto: "Y ahora se respirar."
[0:42-0:46]  Texto: "Fin del Capitulo 1"
[0:46+]      EndOfChapterOverlay aparece.
```

---

## 7. Las 6 cartas (texto completo)

Cada carta usa el sistema `Note` existente (`NoteOverlay`). Se registra en Inventory al ser leida.

### CARTA 1 — Cuaderno de matematicas
**Ubicacion:** sobre el pupitre del protagonista en el Salon Principal. Visible al despertar.
**Pista:** dia (17). Indicio narrativo: "todo era normal".

```
17 / 05 / 2023

Tarea para hoy: ejercicios pag 142.
Examen viernes.
Estudiar geometria 4 hs.

(garabato de un planeta dibujado)
```

### CARTA 2 — Nota en el bolsillo
**Ubicacion:** bolsillo de la chaqueta colgada en la silla. Hay que interactuar con la chaqueta.
**Pista:** narrativa pura, sienta el tono. Pequeño easter egg con el dia.

```
Pa,

Gracias por traerme hoy.
Se que el bus es mas comodo para vos
pero esto es mejor.

Te quiero. Nos vemos a la salida.

— M.M.
```

### CARTA 3 — Boleto del bus (ducto)
**Ubicacion:** caja escondida en una bifurcacion del ducto.
**Pista:** dia + mes completos (la carta mas importante del puzzle).

```
TRANSPORTE ESCOLAR — LINEA 5

Boleto valido: 17/05/2023
Salida 7:00 hs

Pase de estudiante: 0046

[matasellos: NO USADO]
```

**Indicio importante:** "no usado". El jugador entiende que el protagonista no tomo el bus ese dia. ¿Por que?

### CARTA 4 — Foto familiar arrugada (ducto)
**Ubicacion:** en el suelo cerca de la salida del ducto.
**Pista:** narrativa, conexion emocional.

```
[Imagen: foto vieja de un niño pequeño con un hombre adulto. Ambos sonriendo.
El hombre lleva al niño en brazos. Mas atras se intuye una figura femenina
desenfocada — la madre.

Atras de la foto, escritura a mano:]

"Tu primer dia de cole.
5 años. Vos. Yo. Mama."
```

### CARTA 5 — Audio guardado (sala de descanso)
**Ubicacion:** reproductor antiguo o celular viejo sobre una mesa.
**Pista:** narrativa fuerte. **Aqui el padre habla.**

Al activarlo, suena el audio. **El texto se transcribe en el NoteOverlay** (accesibilidad y porque hay jugadores sin auriculares).

```
[Audio: voz adulta masculina, calmada, con leve interferencia de hospital
 (pitido lejano de monitor cardiaco):]

"Hijo, soy yo.

Hoy ya van... [pausa] ...tres años desde el accidente.
Vine a verte como cada año.

El doctor dice que escuchas. No se si es verdad.

Pero te hablo igual.

Cumplis 19 hoy.
Te traje torta. La voy a comer al lado tuyo.

Cuando estes listo, despertate.
Te espero.

— Pa"

[Pitido del monitor cardiaco continua. Fade out.]
```

**Esta es la carta clave del twist.** El jugador entiende que el padre **vive** y que estamos en un hospital. Pero como el protagonista lo "interpreta" como un mensaje del mas alla, no lo procesa.

### CARTA 6 — Cuaderno del padre (sala de juntas)
**Ubicacion:** dentro de la caja de la cerradura, despues de ingresar 1705.
**Pista:** confirmacion del twist + cierre.

```
[Cuaderno de visitas al hospital. Ultimas paginas:]

ENERO 2024 — Sigue dormido. Le hable de mama. Esperaba que reaccionara.
JUNIO 2024 — Le traje su libro favorito. Lo deje al lado.
MAYO 2025 — 17 de mayo. Cumple 18. Le canté.
DICIEMBRE 2025 — Esta noche fue Navidad. Vine igual.
HOY (17/05/2026) — Cumple 19. Le voy a leer la carta que le escribi.

[Hay un sobre cerrado pegado al cuaderno. Sin abrir.]

[En el sobre, escrito a mano:]
"Para cuando despiertes."
```

**No abre el sobre.** Eso es para el Capitulo 2 (si lo hay). El jugador termina con la pregunta abierta.

---

## 8. El Observer: progresion en 4 momentos

| # | Cuando | Como aparece | Distancia | Reaccion del jugador esperada |
|---|---|---|---|---|
| 1 | Acto 1, exploracion inicial del aula | Sombra adulta al fondo del pasillo del aula. No mira directo. Se desvanece si te acercas | Lejos | "¿Qué fue eso?" |
| 2 | Acto 2, mitad del ducto | Asoma desde una bifurcacion. Tiene **algo en la mano** (vago, parece una flor) | Media | "Esta mas cerca. Algo lleva." |
| 3 | Acto 2, antes de salir del ducto | Esta al final del ducto bloqueando. Cuando el jugador se acerca, levanta la mano (gesto suave). Stress sube fuerte | Cerca | Posible respiracion obligatoria. Si respira, el Observer baja la mano y desaparece dejando paso. |
| 4 | Acto 3, tras abrir la cerradura | El Observer aparece al lado del protagonista por primera vez **a la misma altura, no detras**. No hay stress. Le pone la mano en el hombro. Cinematica de outro arranca. | Pegado | Catarsis |

**Diseño visual sugerido:** el Observer es siempre la misma figura. Va perdiendo su distorsion progresivamente. En el #4 se ve casi como un hombre normal, con vendajes leves en la mano y heridas leves en la cara (sobrevivio). El reveal queda implicito.

---

## 8bis. Micro-momentos atmosfericos durante gameplay

Estos NO son cinematicas que pausan el juego — son **eventos cortos** que ocurren mientras el jugador esta moviendose. Duran entre 0.3 y 3 segundos. El jugador puede no notar todos en una primera partida; algunos solo los descubre al rejugar (refuerza el sistema de partidas multiples).

Categorias:

### A. Destellos de luz (faros de auto pasando)

**Donde:** principalmente en los Ductos, opcional en pasillos largos del Acto 3.
**Como:** una luz blanca calida cruza brevemente el espacio (de izquierda a derecha o desde un angulo). Como si estuvieras al lado de una carretera y un coche pasara con los faros encendidos.
**Cuando:** cada 30-60 segundos en los ductos, aleatorio. Se intensifica antes de la cinematica del flashback.
**Tecnico:** un Light direccional que se enciende en un AnimationCurve por 0.4s con un movimiento por una trayectoria, despues se apaga.

### B. Sonidos fantasma del accidente

**Donde:** transversal al juego entero, pero mas frecuentes en los Ductos.
**Como:** un frenazo lejano, un motor revolucionando, un golpe metalico. Cada uno suena por 0.5-1.5s y se desvanece.
**Cuando:** triggeneados por proximidad a ciertos puntos o por estres alto.
**Audio:** Carlos puede generar / encontrar libres. Volumen entre -16 y -10 dB.

### C. Susurros del padre

**Donde:** Acto 1 muy ocasional, Acto 2 frecuente, Acto 3 intenso cerca del clímax.
**Como:** voz masculina muy baja, frases cortas casi inaudibles:
- "Hijo... ¿podes oirme?"
- "Respira."
- "Estoy aca."
- "Despertate."
- "Te traje torta."
- "Cumplis 19 hoy."
**Cuando:** cada 90-120 segundos cuando el jugador esta quieto. Mas frecuente cerca del clímax.
**Importante:** estos susurros son lo que el padre realmente esta diciendo al lado de la cama del hospital. Filtrados a traves del coma. El jugador puede no entenderlos del todo en primera escucha.

### D. Vision subjetiva del auto (microsegundos)

**Donde:** Ductos exclusivamente. Mas en el dead-end de la bifurcacion.
**Como:** durante 0.3-0.5 segundos la camara se distorsiona — un cinturon de seguridad aparece atravesando la pantalla, motion blur lateral como si el auto estuviera en marcha, vista subjetiva del salpicadero brevemente. Despues vuelve a la vista normal del ducto.
**Cuando:** triggers especificos en los ductos. NO al azar — se sienten siempre "deja vu" controlado.
**Tecnico:** un overlay UI que se enciende por 0.4s con animacion + Camera con efecto Camera Shake leve.

### E. La mano del padre (POV)

**Donde:** UNA SOLA VEZ en el juego, momento clave.
**Como:** durante 1.5 segundos, el jugador ve por POV una mano adulta cubriendo SU mano (la que el ve normalmente sosteniendo la linterna). La mano adulta es calida, con un anillo de bodas. Despues desaparece.
**Cuando:** justo antes de la cinematica del flashback en el ducto, o justo antes del Observer #3 (decidir cual). Es un momento sagrado.
**Por que es importante:** es la pista visual mas directa del twist. El jugador VE a alguien tomando su mano. Solo al final entiende que es el padre velandolo.

### F. Espejismos de personas en pasillos lejanos

**Donde:** Acto 3, en el pasillo central de la escena multi-sala.
**Como:** una silueta de un adulto cruza brevemente al fondo del pasillo (puerta a puerta) y desaparece. No es el Observer (esta a otra escala, otra forma de moverse). Es un eco visual.
**Cuando:** cada vez que el jugador entra al pasillo central viniendo de una sala. 
**Visual:** silueta semi-transparente, sin rostro, caminando lento.

### G. Pulso del monitor cardiaco

**Donde:** Acto 3, especialmente en la enfermeria.
**Como:** un pitido electronico medico (el clasico "beep... beep... beep" del monitor cardiaco de hospital). Se filtra muy sutil al ambient. NO es el del minijuego de respiracion (que es heartbeat fisiologico) — es claramente medico.
**Cuando:** continuo en la enfermeria a -25 dB. Sube a -18 dB cuando el jugador esta cerca del espejo o de la camilla.
**Por que:** es la confirmacion auditiva de que estamos en un hospital. El jugador empieza a atar cabos.

### H. Sombras adultas que no corresponden a la geometria

**Donde:** Acto 1 sutil, Acto 3 mas presente.
**Como:** una sombra alargada de adulto se proyecta en una pared cuando no hay nadie. Dura 1-2 segundos y se va.
**Cuando:** ocasional, no triggereable predeciblemente. Aleatorio cuando la luz cambia.

### I. "Feliz cumpleaños" tarareado

**Donde:** principalmente Sala de Descanso, ocasional en Salon Principal.
**Como:** tres notas del "Feliz cumpleaños" tarareadas por voz masculina. No la cancion entera — solo el primer "feliz, feliz, feliz...".
**Cuando:** trigger por proximidad a la torta en sala de descanso. Tambien una vez random en el Acto 1.
**Por que:** es un eco de lo que el padre realmente esta cantando ahora mismo en el hospital. Brutal cuando el jugador entiende.

### J. Cambio temporal del entorno (microflash de "antes")

**Donde:** UNA SOLA VEZ en Acto 1, en el Salon Principal.
**Como:** durante 1 segundo, el aula tiene **sus compañeros sentados, profesor al frente, luces brillantes, colores vivos**. Como una foto de "como era antes". Inmediatamente vuelve al aula vacia y oscura.
**Cuando:** primera vez que el jugador llega al centro del salon despues de PlayerWakeUp.
**Por que:** muestra al jugador una version del aula antes del horror — momentaneo pero memorable.

### K. La silla del padre

**Donde:** Acto 1, Salon Principal.
**Como:** en el pupitre del lado del protagonista (donde "deberia" estar nadie en una escuela), aparece **un saco/chaqueta colgado en la silla durante 2 segundos** despues de que el jugador lo ve. Cuando se aleja y vuelve, la silla esta vacia. Pero la chaqueta era de adulto.
**Cuando:** trigger por gaze.

---

**Distribucion sugerida** (para que no se sobrecargue):

| Categoria | Acto 1 | Acto 2 | Acto 3 |
|---|---|---|---|
| A — Destellos faros | — | ✅ frecuente | opcional pasillos |
| B — Sonidos accidente | sutil | ✅ frecuente | sutil |
| C — Susurros padre | muy ocasional | ✅ regular | ✅ intenso clímax |
| D — Vision auto | — | ✅ key moments | — |
| E — Mano POV | — | ✅ una vez | — |
| F — Espejismos pasillo | — | — | ✅ cada entrada |
| G — Monitor cardiaco | — | — | ✅ enfermeria |
| H — Sombras | sutil | — | regular |
| I — Feliz cumpleaños | una vez | — | ✅ sala descanso |
| J — Cambio temporal | ✅ una vez | — | — |
| K — Silla del padre | ✅ una vez | — | — |

**Idea pedagogica:** los micro-momentos premian al jugador atento. Si pasas corriendo, el juego sigue funcionando. Si te detenes a observar, descubris piezas del rompecabezas. Esto refuerza el mensaje "respira y mira lo que es real".

---

## 9. Indicios visuales sembrados en el ambiente

Estos son detalles atmosfericos que Henry puede ir agregando al modelado/iluminacion de las escenas. **Ninguno es obligatorio** para que la narrativa funcione, pero entre mas haya, mas rica la experiencia.

### Salon Principal
- **Reloj de pared parado** en las 17:00 (con segundero quieto)
- **Calendario** con el 17 de mayo marcado en rojo
- **Pizarra**: "17/05/2023" escrito en el encabezado (fecha de la clase)
- **Mochila propia** abierta en el pupitre, con un dibujo de un planeta (matchea Carta 1)
- **Numero del aula:** "17B" en la puerta
- **Una silla volteada** del lado del aula (lugar vacio donde "alguien" deberia estar)

### Ductos
- **Tornillos oxidados** y goteo visible (el goteo subliminal del audio)
- **Marcas en las paredes** del ducto que sugieren que alguien paso muchas veces (sus propios intentos previos del loop?)
- **Una unica luz parpadeante** en una bifurcacion

### Sala de descanso
- **Cartel "Felices 16, M.M."** colgado a medio caer
- **Torta de cumpleaños** a medio servir, vela apagada
- **Globos** medio desinflados pegados al techo
- **Foto polaroid** en el corcho: el protagonista con su padre y otra figura difusa (la madre, viva en la foto)

### Enfermeria (3a sala)
- **Espejo que refleja diferente** durante 2 segundos al pasar (vendajes en cabeza)
- **Camilla con almohada hundida**, sabanas tibias visualmente (vapor sutil?)
- **Boletin medico**: "Paciente PED-046, dia 1095 en coma. Estable. Visita reciente: hoy."
- **Flores frescas** en la mesita (alguien estuvo aca hoy)

### Sala de Juntas
- **Placa "En memoria de Carolina Mendoza"** en la pared
- **Pizarra atras** con un nombre borrado pero aun visible (puede ser el nombre del protagonista)
- **Sillas perfectamente ordenadas** (no es una sala que se use a diario)
- **La caja con la cerradura** sobre la mesa central

---

## 10. Mapeo escena → eventos → logros

Cuando se enganchen los hooks (Bloque 3 de Fase 5), estos son los eventos que disparan los 5 logros del catalogo:

| Evento | Escena | Logro | Codigo |
|---|---|---|---|
| Resolver `CombinationLock` (1705) sin fallar ningun digito | Sala de juntas | Maestro de cerraduras | `cerradura_primera` |
| Leer Cartas 1-6 todas | Distribuido | Lector compulsivo | `notas_completas` |
| Completar capitulo sin fallar ciclos del minijuego de respiracion | Toda la partida | Aire en orden | `respiracion_zen` |
| Completar capitulo sin que `StressSystem.OnCollapse` se dispare | Toda la partida | Mente fria | `superviviente` |
| Completar capitulo sin disparar al Observer (no quedarse mirandolo) | Toda la partida | Sin que te vean | `sigilo_observer` |

**Nota narrativa:** los logros tienen sentido en este marco. "Sin que te vean" no es esquivar a un enemigo — es no dejar que la culpa te paralice. "Aire en orden" es haber sabido respirar en cada crisis. "Mente fria" es no haber colapsado emocionalmente. Todos los logros son metaforas de regulacion emocional cumplida. Eso refuerza el mensaje pedagogico.

---

## 11. Decisiones operativas — CERRADAS 2026-05-03

| # | Decisión | Resultado |
|---|---|---|
| 1 | Apellido del protagonista | **Mendoza** |
| 2 | 3ª sala del Acto 3 | **Enfermería** |
| 3 | Formato de cinemáticas | **Intro y Outro: texto+audio (CinematicController). Flashback: 3D mínimo (salpicadero + cinturón + brazo del padre — pasarle a Henry).** |
| 4 | Voces (Carta 5 + tarareo Cinematica 1) | **TTS provisional ahora, Carlos graba después** |
| 5 | Cambio `CombinationLock` 7-4-2-9 → 1-7-0-5 | **Hecho** (script + 3 escenas: Dev_Duvan, Dev_Henry, EscenaUno-salonPrincipal) |

### Casting cerrado 2026-05-03

| Personaje | Nombre |
|---|---|
| Protagonista | **Mateo Mendoza** (iniciales M.M., el juego solo muestra las iniciales o "Pa" en cartas) |
| Padre | sin nombre explícito (siempre "Pa" o "Papa") |
| Madre | **Carolina Mendoza** (placa del Acto 3) |

### Decisión estructural — escenas (cerrada 2026-05-03)

**Level1 = carpeta `Assets/Scenes/Level-1/` con sus 3 escenas como los 3 actos:**

- Acto 1 → `EscenaUno-salonPrincipal.unity`
- Acto 2 → `EsecenaUno-DuctosDeVentilacion.unity` (typo en el nombre, renombrar cuando se pueda)
- Acto 3 → `EscenaUno-salonSecundario.unity`

Transiciones entre actos via `SceneManager.LoadScene`. **Cleanup hecho:** `Assets/Scenes/Chapter1_Salon4B/` eliminado del repo y de Build Settings.

**Pendiente Build Settings:** agregar `EsecenaUno-DuctosDeVentilacion.unity` y `EscenaUno-salonSecundario.unity` (no están registradas, sin eso no cargan en build). Se hace en el Bloque C cuando se configuren las transiciones.

---

## 12. Plan de trabajo para mañana

### Bloque A — Validar narrativa (15 min)
- Duvan lee este documento, valida o ajusta
- Cerrar las 6 decisiones pendientes (seccion 11)

### Bloque B — Rediseño visual UX (90 min)
- Menu principal con tono narrativo
- Pantalla de carga con tip emocional rotativo
- Mecanica de respiracion: alternativa al circulo (vignette + onda? — hay que decidir)
- SettingsOverlay: ajustar layout de los 4 botones inferiores

### Bloque C — Implementar narrativa (60 min)
- Cambiar combinacion de CombinationLock a 1705
- Crear las 6 notas concretas usando el sistema `Note` existente, con los textos definitivos
- Posicionar las 6 notas en sus ubicaciones (esto requiere las escenas abiertas en Unity)
- Configurar trigger de Cinematica 2 (al cerrar Carta 3)
- Configurar transiciones entre las 3 escenas

### Bloque D — Implementar cinematicas (60 min)
- Cinematica 1 (intro) usando CinematicController existente
- Cinematica 2 (flashback) usando CinematicController
- Cinematica 3 (outro) usando CinematicController + integrar con EndOfChapterOverlay

### Bloque E — Hooks API (Fase 5 Bloque 3) (45 min)
- POST /api/Partidas al pulsar Jugar
- Hooks de los 5 logros
- PUT /api/Partidas + abrir EndOfChapterOverlay al fin de cinematica 3

### Bloque F — Indicios visuales si queda tiempo
- Pasarle a Henry la lista de la seccion 9 para que vaya integrando

**Total estimado:** 4-5 hs de trabajo conjunto. Se puede partir en 2 sesiones si es mucho.

---

## 13. Referencias

- Loop subliminal: `Document/Changelog/003_Loop_Subliminal_Accidente.md`
- Brief de audios y cinematicas: `Document/BRIEF_CARLOS_AUDIO_CINEMATICAS.md` (especialmente seccion "Cinematica 3 - Opcion A")
- Sistemas implementados que se usan: `Document/Arquitectura_Sistemas.md`
- Persistencia (logros): `Document/Flujo_API_Estan_Dentro.md`

### Inspiracion

- **Returnal** (loop tras accidente, trauma familiar reconstruido en bucle)
- **Silent Hill 2** (el monstruo es metafora del trauma)
- **Hellblade: Senua's Sacrifice** (psicosis, voces internas)
- **Spiritfarer** (soltar a quien amas)
- **What Remains of Edith Finch** (memoria familiar fragmentada)
