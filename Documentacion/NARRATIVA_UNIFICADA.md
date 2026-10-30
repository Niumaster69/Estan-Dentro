# Están Dentro — Narrativa unificada y flujo del Capítulo 1 (propuesta, 19-oct-2026)

> Pedido de Henry: replantear flujo, lógica y textos. Alguien que juega 5–15 minutos en una feria tiene que salir
> entendiendo **quién es, qué pasó y por qué le importa**, sin quedarse con "¿y el papá qué?". La meta es una sintonía
> profunda, como el prólogo de *The Last of Us*.
> Base: las 4 versiones que había (ver §6) y los documentos de la fase 1 (GDD + Plan de Producción, que piden un
> **videojuego educativo sobre salud mental**). Cuando Henry y el equipo aprueben esto, se actualiza el GDD.

---

## 1. La historia en una frase

**Mateo cumple 16 años. Su papá, en vez de dejarlo tomar el bus, lo lleva al colegio. Chocan. Mateo despierta de noche
en su salón vacío y cree que su papá murió por su culpa. Ese colegio es su mente en coma, y todo lo que encuentra son
cosas que su papá le está diciendo desde el otro lado.**

Por qué esta versión:
- Es la del coma, que el equipo **ya había cerrado como canon** el 3-may.
- Cumple el objetivo **educativo**: culpa, trauma y respiración como herramienta.
- Mantiene el colegio de terror y el "perderse con sentido": una mente atrapada da vueltas, como en *Fenómeno Siniestro*.
- De la versión disociativa del GDD salen "ellos", **los que están dentro**: la Culpa (el Iracundo), el Miedo y el niño que fue (el Infante). Dan miedo, pero son partes de él.
- Se descarta el papá atrapado en los ductos (versión B) y el final "Tú sí (moriste)", que contradecía todo lo demás.

**La regla de oro: todo lo que el jugador encuentra en el colegio ya lo vio en el prólogo.** Así reconoce cada pista
("¡la lonchera que me hizo Pa!") y nunca se queda con la duda.

---

## 2. Flujo completo (≈ 13–15 min; versión feria ≈ 10 min)

| # | Momento | Dónde | Duración | Qué siente el jugador | Qué aprende |
|---|---|---|---|---|---|
| 0 | Aviso de salud mental + título | pantalla | 0:20 | seriedad | que es un juego sobre emociones reales |
| 1 | **Prólogo jugable "17 de mayo"** | carro de Pa, mañana | 3:00 | cariño, normalidad, presagio | quién es Mateo, quién es Pa, las 3 claves |
| 2 | El choque y el pitido | negro → hospital → campana | 0:30 | golpe | algo terrible pasó |
| 3 | Despertar en el 4°B (noche) | Salón 4°B | 0:30 | desconcierto | mirar, el objetivo "Ahora" |
| 4 | Salón: cajón → casillero → lonchera → apagón | 4°B | 5:00 | inquietud que crece | leer, candados, Q, linterna |
| 5 | La rejilla y los ductos | ductos | 1:30 | claustrofobia, alguien lo sigue | respirar, gatear |
| 6 | Primer piso: secretaría → rectoría | P1 | 2:30 | casi llegar | deducir 1705 en la pizarra |
| 7 | **Final "Ese día no murió nadie"** | rectoría = cuarto de hospital | 1:00 | giro y alivio triste | la verdad + gancho del cap. 2 |
| 8 | Texto de respiración + créditos | pantalla | 0:40 | calma | la técnica de respirar es real |

### 2.1 Prólogo jugable "17 de mayo" (lo nuevo y lo más importante)
**Estilo *The Last of Us*:** corto, jugable y con mucho cariño, para que duela después.

**Escena:** carro de Pa, 6:50 a. m., lluvia suave en Bogotá. Mateo va en el asiento del copiloto. Se puede mirar alrededor y tocar 4 cosas. No se camina.

| Interacción | Qué pasa | Qué deja sembrado |
|---|---|---|
| El **cuaderno** en sus piernas | Mateo repasa: *"Ejercicios pág. 142. Examen el viernes."* | **142** (casillero) |
| La **lonchera** que hizo Pa (con candadito rojo) | Pa: *"Te puse candado para que no te la roben. La clave es tu ruta, la 46. Cero, cuatro, seis."* | **046** (lonchera) y su diseño |
| La **radio** | Suena una canción vieja que Pa canta mal; Mateo se ríe | el vínculo, el tono |
| El **celular** | Mensaje del grupo del curso: *"¡Feliz cumple, Mendoza! 🎂"*. Fondo de pantalla: Mateo y Pa | **17/05**, su cumpleaños |

- **Diálogo** (2 respuestas cortas para elegir, sin consecuencia, solo para sentir):
  - Pa: *"El bus es más cómodo, ya sé. Pero hoy cumples dieciséis y te traigo yo."*
  - Mateo, opción A: *"Gracias, Pa."* / opción B: *"Igual iba a llegar tarde."*
- **El presagio:** Mateo le pide cambiar la canción y Pa aparta la vista para buscar en la radio. Luces de frente.
- **Corte a negro.** Se oye el pitido de un monitor de hospital. Una voz lejana: *"…respira conmigo, mijo. Cuatro… sostén…"*. El pitido se vuelve la **campana del colegio**.
- **Sin texto explicativo.** El jugador no sabe aún que es un coma; solo sabe que hubo un choque y que algo no cuadra.

### 2.2 Capítulo 1 en el colegio (cadena de objetivos)
La línea **"Ahora"** está escrita como pensamiento de Mateo. Las pistas por tiempo son las de `PROPUESTA_MISIONES_HUD_ESTILO.md`.

| Paso | "Ahora" (Mateo piensa) | Se resuelve con | Recompensa / qué revela |
|---|---|---|---|
| 1 | *"¿Qué hago aquí? Es de noche… ¿y Pa?"* | probar la puerta del 4°B (trabada) | se entiende que estás encerrado |
| 2 | *"Mi casillero. ¿Cuál era la clave?"* | nota del cajón del profe (*pág. 142*) → candado de latón | destornillador + carta de Pa |
| 3 | *"La lonchera de Pa… la clave era mi ruta."* | 046 → candadito rojo | linterna + **apagón** |
| 4 | *"No veo nada. La linterna (F)."* | encender la linterna | mensajes en las paredes |
| 5 | *"El aire viene de la rejilla del techo."* | destornillador en la rejilla | ductos |
| 6 | (ductos) *"Sigue el aire."* | salir | P1, frente a la rectoría |
| 7 | *"La rectoría tiene cadena. Cuatro dígitos."* | pizarra: hoja + cuaderno → **17/05** | **1705** |
| 8 | *"Diecisiete, cero, cinco. Mi cumpleaños."* | candado de cadena | la puerta se abre |
| 9 | *"…¿Pa?"* | entrar | final |

### 2.3 Final "Ese día no murió nadie" (reemplaza los 7 textos de hoy)
- **Escena:** la rectoría se deforma en un **cuarto de hospital**, en luz cálida. Pa está sentado junto a una cama, de espaldas, leyendo en voz baja el cuaderno: *"…ejercicios de la página ciento cuarenta y dos…"*. En la cama hay alguien con una pulsera: **PED-046**.
- **Textos** (uno por uno, sobre la escena):
  1. *"Diecisiete de mayo. Tu cumpleaños."*
  2. *"Ese día no murió nadie."*
  3. *"Pa está vivo. Te visita todos los días."*
  4. *"El que no ha vuelto eres tú."*
  5. *"Tres años dormido."*
- Pa (voz, sin girarse): *"Respira conmigo, mijo. Cuatro… sostén… seis."* El jugador **respira de verdad**: se usa el minijuego de respiración.
- El monitor se acelera y la imagen se rompe. En rojo, la letra Creepster: **ESTÁN DENTRO**. Debajo: *"Algo no quiere que despiertes."* → *Capítulo 2: Los pasillos*.
- **Lo que logra:** un giro claro (no murió nadie; tú estás en coma), un alivio triste y un **gancho** (¿qué no quiere que despierte?). Ya no queda la duda del papá.

### 2.4 Modo feria (para la gente que juega 10 min y sigue al siguiente puesto)
- El ejecutable abre **directo en el prólogo**, sin menú ni inicio de sesión. Opción `-feria`.
- Pistas por tiempo más rápidas: a los 90 s y a los 3 min.
- **Ayudas: Completas** por defecto.
- Si nadie toca nada durante 2 minutos, vuelve solo al título.
- Al terminar, los créditos van a la pantalla de título con *"Pulsa una tecla para jugar"*.

---

## 3. Personajes (canon)

| Personaje | Quién es | Cómo aparece en el cap. 1 |
|---|---|---|
| **Mateo Mendoza (M.M.)** | 16 años el 17/05/2023, 4°B. Ingenioso, un poco distraído. Cree que mató a su papá. | Es el jugador. Sus pensamientos son la línea "Ahora". |
| **Pa** (sin nombre, siempre "Pa") | Papá viudo, cariñoso, canta mal. Vivo. Visita a Mateo todos los días en el hospital. | Prólogo; cartas; su voz se cuela desde el hospital; final. |
| **Carolina Mendoza** | La mamá, murió de cáncer antes de la historia. | Placa "En memoria de" en el colegio. Cap. 2. |
| **El Observador** | La presencia de Pa del otro lado: no da miedo, pero asusta porque no se entiende. | Escribe en la pizarra y deja la hoja temblorosa. |
| **La Culpa** (el Iracundo del GDD) | Lo que Mateo cree de sí mismo. | Los escritos rojos *"Fue tu culpa"*. El monstruo del sótano (cap. 2–3). |
| **El niño** (el Infante) | Mateo pequeño, el que todavía confía. | Cap. 2. |

---

## 4. Textos del juego reescritos (para reemplazar los actuales)

Las tildes van completas: la fuente Special Elite las tiene.

**Cajón del profe** — título *"Cuaderno de Mateo"*
> 17 / 05 / 2023
> Tarea: ejercicios pág. 142.
> Examen el viernes.
> (y en la esquina, con otra letra:) *¡Feliz cumple, Mendoza!*

**Carta en el casillero** — título *"Una carta de Pa"*
> Mateo:
> Si estás leyendo esto, es que ya sabes buscar.
> Tu lonchera tiene la clave de siempre: tu ruta. 0 - 4 - 6.
> No te asustes de la oscuridad. Yo estoy aquí.
> — Pa

**En un pupitre** (antes "Mensaje de papá", que estaba mal titulado) — título *"Una nota que nunca le di"*
> Pa:
> Gracias por traerme hoy.
> Sé que el bus es más cómodo para vos, pero esto es mejor.
> Te quiero. Nos vemos a la salida.
> — M.M.

**Pizarra del 4°B** (tiza, la escribe el Observador): *"Nadie se fue. Estás tú solo."*

**Mensajes con la linterna** (antes eran 4 frases sueltas; ahora cuentan algo):
- *"Fue tu culpa"* (rojo, temblado: la Culpa)
- *"No fue tu culpa"* (tiza, firme, junto al anterior: Pa)
- *"Aún hay una salida"*
- *"Sigue el aire"* (señala la rejilla)

**Secretaría** — hoja doblada, título *"Letra temblorosa"*
> Lo que olvidaste te trajo aquí.
> Cuatro dígitos. Día y mes.
> El día que cumpliste dieciséis.
> No lo olvides otra vez.

**Secretaría** — reloj detenido en **17:05** (foto para la pizarra).

**Rectoría cerrada:** *"Tiene una cadena con un candado grande de 4 ruedas."*

**Ductos** — al salir: *"Saliste."* / *"Pero no al mismo lugar."*

**Game Over:** *"TE DEJASTE LLEVAR"* / *"El miedo no decide por ti. Respira y vuelve a intentarlo."*

**Créditos:** se mantiene el texto de respiración y la línea de salud mental (192 opción 4 / 106 Bogotá).

---

## 5. Qué hay que producir (por orden)

| # | Tarea | Quién | Peso |
|---|---|---|---|
| 1 | Reemplazar textos de notas, mensajes, final y game over (§4) | Claude | bajo |
| 2 | Final nuevo en la rectoría: hospital, Pa de espaldas, cama PED-046, respiración guiada | Claude (escena) + Carlos (voz de Pa) | medio |
| 3 | **Prólogo en el carro**: modelo del carro por dentro (Blender), lluvia, 4 interacciones, diálogo, choque | Claude (escena y modelo) + Carlos (voces, radio, choque) | alto |
| 4 | Línea "Ahora" y cadena de misiones del §2.2 | Claude | medio |
| 5 | Pizarra de investigación con las pistas de este capítulo | Claude | alto |
| 6 | Modo feria | Claude | bajo |
| 7 | Actualizar el **GDD y Plan de Producción** con esta narrativa | Henry + equipo (Claude redacta) | medio |

**Voces que necesitamos de Carlos:** Pa en el carro (4–5 frases), Pa en el hospital ("respira conmigo…"), la canción de la radio, y los sonidos del choque, el monitor y la campana.

---

## 6. De dónde sale cada cosa (versiones anteriores)

| Versión | Qué se toma | Qué se deja |
|---|---|---|
| A. GDD (disociativa) | "ellos" como partes de Mateo (Culpa, Miedo, Niño); objetivo educativo; respiración | la familia muerta, el protagonista sin nombre y de 8–10 años |
| B. Guion "Esteban" | Mateo Mendoza, el 4°B, el casillero 142 | el papá atrapado en los ductos, la mamá viva, la parte sobrenatural |
| C. Coma (canon del 3-may) | **casi todo**: 17/05, coma, Pa vivo, M.M., Carolina, 1705, PED-046, el boleto del bus | el final "Tú sí" (decía que estaba muerto) |
| D. Colegio de noche (18-oct) | el colegio completo como mente, el encierro, los bucles, el monstruo | — |

## 7. Decisiones (Henry, 19-oct)
- [x] Historia: **coma + Pa vivo**, final "Ese día no murió nadie".
- [x] Prólogo: **dentro del carro** (3 min jugables).
- [x] El hospital se ve **al final del capítulo 1**.
- [x] **Menú con dos modos**:
  - **Feria**: arranca rápido, más ayudas, vuelve solo al título si nadie juega; que se juegue fácil y deje algo.
  - **Normal**: la versión completa que se irá desarrollando (por si se sube a Steam).
- Contexto: el juego ya no es para la materia (era del semestre pasado), es para presentarlo en la u. El enfoque de salud mental se mantiene porque le da sentido a la historia, no por requisito.
