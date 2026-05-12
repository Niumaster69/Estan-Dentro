# Estan Dentro — Estructura narrativa del juego

> **Documento privado, no subido al repo.** Diseño narrativo macro: arcos, actos, ritmo, simbologia.
> Para entender como cada mecanica del codigo cuenta una parte de la historia.

---

## 1. La premisa estructural

Estan Dentro es un **horror psicologico de aprendizaje progresivo**, dividido en 3 capitulos. Cada capitulo desarrolla una **regla del mundo** distinta y cierra con una revelacion que recontextualiza lo anterior.

**Capitulo 1 — "El aula"** (en desarrollo): aprende a sobrevivir al aislamiento.
**Capitulo 2 — "Los ductos"** (planeado): aprende a moverte entre las dos versiones del colegio.
**Capitulo 3 — "El sotano"** (planeado): aprende a confrontar lo que tu padre ya enfrento.

Cada capitulo se subdivide en 3 actos. El Capitulo 1 tiene Acto 1 jugable, Acto 2 esqueleto, Acto 3 pendiente.

---

## 2. La pregunta dramatica de cada capitulo

Toda historia bien estructurada tiene una pregunta que la audiencia quiere responder. Estan Dentro plantea tres:

| Capitulo | Pregunta dramatica | Pregunta pedagogica oculta |
|---|---|---|
| 1 | ¿Como salgo de aqui? | ¿Puedo controlar mi miedo? |
| 2 | ¿Donde esta mi padre? | ¿Puedo confiar en lo que veo? |
| 3 | ¿Es realmente mi padre? | ¿Que estoy dispuesto a perder por sacarlo? |

Las preguntas pedagogicas son las que el sistema de respiracion + estres realmente entrena. El jugador piensa que esta resolviendo un puzzle, pero esta aprendiendo a regular su propia ansiedad.

---

## 3. Los tres actos del Capitulo 1 (estructura clasica)

### Acto 1 — "El aula" (Salon Principal — implementado)

**Funcion narrativa:** establecer el mundo, las reglas, el conflicto.

| Beat | Que pasa | Mecanica que lo cuenta |
|---|---|---|
| Hook | Mateo despierta encerrado | Wake-up cinematico, HUD "Sal del salon" |
| Inciting incident | Encuentra cartas firmadas por su padre muerto | Sistema de notas + Inventory |
| Primera resistencia | La puerta no abre | Trigger de puerta cerrada |
| Plan A | Buscar pistas en el aula | Exploracion + lectura de notas |
| Punto medio | Resuelve el casillero (142) | CombinationLock + LockedContainer |
| Subida de tension | Lonchera apaga las luces (blackout) | LightsController + BlackoutEvent |
| Climax del acto | Linterna revela mensaje oculto: "Subi, hijo." | HiddenMessage + Flashlight |
| Cliffhanger | Trepa al ducto del techo | DuctoTechoInteractable cinematica |

**Promesa al jugador al final del acto:** "no estas solo, tu padre esta vivo en alguna parte de aqui".

### Acto 2 — "Los ductos" (planeado)

**Funcion narrativa:** desestabilizar al jugador. Mostrar que el mundo es mas grande y mas extraño de lo que parece.

Beats propuestos:

| Beat | Que pasa | Mecanica propuesta |
|---|---|---|
| Disorientacion | Mateo gatea por los ductos. La perspectiva cambia | Camara baja, FOV reducido |
| Primera intrusion | Susurros en los oidos. Pensamientos que no son de el | Sistema de intrusiones + texto subliminal |
| Bifurcacion | Dos caminos: bajar al colegio normal o seguir hacia luz amarilla | Decision narrativa con consecuencias |
| Encuentro con el Observador | Aparece de pie al final de un ducto | ObserverTrigger + drenaje de estres |
| Crisis respiratoria | El espacio se cierra. Mateo entra en panico | BreathingMinigame forzado |
| Resolucion del acto | Mateo cae al "otro" colegio (espejo, pero mal) | Cambio de escena + iluminacion saturada distinta |

**Promesa al final del acto:** "el lugar al que entraste no es el colegio que conoces — es el lugar donde tu padre vive ahora".

### Acto 3 — "El sotano" (planeado)

**Funcion narrativa:** climax y revelacion. La pregunta dramatica del capitulo se responde — pero la respuesta abre una pregunta peor.

Beats propuestos:

| Beat | Que pasa | Mecanica propuesta |
|---|---|---|
| Apertura calma | Pasillo silencioso. Las paredes susurran nombres | Audio espacial + pulse de stress lento |
| Encuentro indirecto | Ve a Esteban a traves de un vidrio empañado | Trigger visual + mensaje oculto |
| Persecucion final | "Ellos" lo persiguen al darse cuenta de que llego | Stress maximo + carrera contra el tiempo |
| Confrontacion | Llega a la silla. Esteban esta sentado, de espaldas | Camara cinematografica controlada |
| Revelacion | Esteban se da vuelta. Tiene su cara. **Pero no es el** | Cinematica scripted |
| Decision final | Mateo elige: ¿se queda? ¿escapa solo? | Eleccion narrativa con dos finales |
| Cierre | EndOfChapterOverlay con resumen + cliff | Sistema implementado, listo para usar |

**Promesa al final del acto:** "lo que rescates puede no ser tu padre. Lo que dejes atras puede haberlo sido. Capitulo 2."

---

## 4. Como las mecanicas del codigo cuentan la historia

Cada sistema implementado tiene **funcion ludica** y **funcion narrativa**. La buena conexion entre las dos es lo que hace que el jugador "sienta" la historia, no solo la lea.

### Sistema de estres (`StressSystem`)
- **Funcion ludica:** te da un fail state suave (game over si llega a 100).
- **Funcion narrativa:** materializa el control mental que "ellos" ejercen. No es "vida" — es "cuanto les estas dejando entrar". Cuando colapsas, no morís: te tienen.

### Sistema de respiracion (`BreathingMinigame`)
- **Funcion ludica:** baja el estres cuando lo haces bien.
- **Funcion narrativa:** el padre ya le enseño a Mateo, en su infancia, a respirar para dormir cuando tenia pesadillas. **El mismo gesto que el padre uso es lo que ahora salva al hijo**. Es legado tactico de un padre que sabia que esto iba a pasar.

### El Observador (`ObserverTrigger`)
- **Funcion ludica:** evento aleatorio de horror visual con drenaje de estres.
- **Funcion narrativa:** "ellos" tienen una jerarquia. El Observador es el mas bajo — el que recolecta. Si te ve, te marca. Que solo aparezca cuando lo miras directamente es regla del genero (Slender, Outlast) y refuerza el mensaje implicito: **mirar es vulnerabilidad**.

### Linterna (`Flashlight` + `HiddenMessage`)
- **Funcion ludica:** revela props especificos en oscuridad.
- **Funcion narrativa:** la luz ultravioleta es *escritura del padre*. Cada vez que la usas, es Esteban hablandote. La linterna es **el padre**, en cierto sentido. Por eso esta escondida en una lonchera con la fecha de nacimiento de Mateo (046 = 04 de junio).

### Inventory + cartas (`Inventory`, `Note`)
- **Funcion ludica:** sistema de coleccionables relechables.
- **Funcion narrativa:** las notas son **dialogos retrasados**. Mateo nunca puede contestar a sus padres en tiempo real, pero puede releer lo que le dijeron y entenderlo de nuevo. El inventario de notas es la unica manera en que la familia puede tener una "conversacion".

### Cerraduras (`CombinationLock`)
- **Funcion ludica:** puzzle de combinacion clasico.
- **Funcion narrativa:** son **pruebas de pertenencia**. Solo alguien que comparte la historia familiar puede abrirlas. 142 = año de entrada del padre al colegio. 046 = fecha de nacimiento del hijo. Sin esa intimidad familiar, no podes pasar.

### Cinematica del ducto (`DuctoTechoInteractable`)
- **Funcion ludica:** transicion espectacular entre escenas.
- **Funcion narrativa:** Mateo **deja de ser pasivo**. Hasta ahora reaccionaba; ahora actua: arrastra una mesa, destornilla, sube. Es el **momento bisagra** del capitulo: pasa de "estoy encerrado" a "voy a buscarlo". La coreografia de camara con bob/dip refuerza la sensacion de esfuerzo fisico, no solo narrativo.

### Logros (`cerradura_primera`, `notas_completas`, etc.)
- **Funcion ludica:** rejugabilidad y completionism.
- **Funcion narrativa:** son los **estandares que el padre habria querido que el hijo cumpliera**. "Sin equivocarte" en la cerradura = "te concentraste". "Lee todas las notas" = "me prestaste atencion". El sistema de puntos al final no es ego — es un padre orgulloso del otro lado.

---

## 5. Simbolos y motivos recurrentes

| Simbolo | Significado | Donde aparece |
|---|---|---|
| **El aula 4B** | El umbral, el lugar sagrado/maldito | Toda la historia |
| **El ducto del techo** | El "entre", la conexion entre mundos | Acto 1 climax, Acto 2 entero |
| **La linterna** | La voz del padre, la verdad oculta | Desde Acto 1 hasta el final |
| **El destornillador** | Herramienta de fuga + simbolo de "construir tu salida" | Acto 1 |
| **Las cartas** | Comunicacion fuera del tiempo | Toda la historia |
| **El espejo / agua quieta** | Ver el otro lado del colegio | Acto 2 (planeado) |
| **La silla vacia** | El lugar del padre que aun esta | Acto 3 (climax) |

**Motivo recurrente:** la respiracion. Aparece como mecanica, como sonido ambiente (el ducto respira), como dialogo del padre ("respira despacio"), como amenaza (los que estan dentro entran por la boca abierta). El juego entero esta organizado alrededor de **inhalar y exhalar**: tension y alivio, oscuridad y luz, perderse y encontrarse.

---

## 6. Pacing emocional del Capitulo 1

Esto es lo que el jugador deberia *sentir*, en orden:

```
Despertar
   │
   ├─► Confusion ─────────────► (1-2 min)
   │
   ├─► Curiosidad ────────────► (5 min explorando aula)
   │
   ├─► Esperanza pequeña ─────► (al abrir el casillero)
   │
   ├─► Inquietud creciente ───► (cuando lee la nota del padre)
   │
   ├─► Susto controlado ──────► (blackout de la lonchera)
   │
   ├─► Asombro ─────────────► (mensaje oculto en la pared)
   │
   ├─► Determinacion ────────► (subiendo al ducto)
   │
   └─► Cliffhanger ─────────► (fade a negro, "voy a buscarlo")
```

**Importante:** el primer susto fuerte (blackout) debe ocurrir aprox. en el minuto 12-15, no antes. Si el jugador entra en horror muy temprano, **se inmuniza** al horror del Acto 2 y 3. La curva de tension tiene que crecer despacio para que el climax importe.

---

## 7. La voz autoral (lo que esta historia es realmente)

Estan Dentro no es un juego sobre fantasmas. Es un juego sobre:

1. **Aprender a respirar bajo presion** — algo que muchos adolescentes no saben hacer y deberian aprender. La respiracion en el juego es literalmente educacion emocional disfrazada de mecanica.

2. **Heredar el dolor de los padres**. Mateo no eligio entrar al 4B; el colegio lo eligio porque su padre estuvo ahi. ¿Cuanto de lo que somos es nuestra eleccion y cuanto es lo que nos transmitieron? Pregunta clasica de horror gotico.

3. **El amor como protocolo de supervivencia**. Las pistas que Esteban deja no son trampas ni acertijos crueles — son cuidados retroactivos de un padre que sabe que su hijo va a estar en peligro y le deja todo lo que tiene: codigos, instrucciones, una linterna, una respiracion enseñada años atras.

El subtitulo del juego — "Lo que no puedo ver" — apunta a las dos cosas que el protagonista no puede ver: **lo que esta dentro de las paredes** y **lo que su padre dejo dentro de el**.

---

## 8. Como contar todo esto **sin texto explicativo**

El error mas grande que podria cometer este juego es explicar todo en cinematicas largas. La regla:

> **Si lo puede mostrar una mecanica, no lo cuenta una cinematica.**
> **Si lo puede contar una nota, no lo dice un dialogo.**
> **Si lo puede sugerir un sonido, no lo describe una nota.**

Lista de cosas a no explicar nunca y dejar que el jugador deduzca:

- Por que el padre dejo cartas (deduccion: "le dejo todo preparado").
- Que es exactamente "ellos" (sugerido por intrusiones, susurros, presencias visuales).
- Por que el aula 4B (mostrar que el padre era profesor practicante ahi, dejar que el jugador conecte).
- Que paso con la primera victima (sugerido por el pupitre del fondo del aula con un grabado distinto al de los demas).

---

## 9. Roadmap narrativo a futuro

| Fase | Que se hace | Quien |
|---|---|---|
| **Sprint 4** | Audio del Acto 1 + Ductos como gameplay | Carlos + Duvan |
| **Sprint 5** | Acto 3 (sotano) + EndOfChapterOverlay enchufado | Duvan |
| **Sprint 6** | Cinematicas Flashback (Bloque D) | Duvan + Henry |
| **Sprint 7+** | Capitulo 2 desde cero | Equipo completo |

---

## 10. Una linea para vender el juego

> *"Estan Dentro es lo que pasa cuando un colegio te encierra para terminar lo que empezo con tu padre."*

Si no podes contar el juego en una linea, no lo entiende nadie. Esta linea contiene:
- Lugar (colegio).
- Conflicto (te encierra).
- Vinculo emocional (tu padre).
- Promesa (algo no terminado, va a terminar ahora).
