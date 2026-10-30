# Están Dentro — Propuesta: misiones, HUD y estilo visual (19-oct-2026)

> Análisis pedido por Henry antes de seguir con el juego. **Problema:** los objetivos pasan muy rápido y el
> jugador no alcanza a verlos. El juego debe entenderse sin una introducción larga. El estilo del juego debe
> ir con el del menú. **Regla:** reformar lo que ya existe; no tirarlo.

---

## 1. Qué hay hoy y por qué falla

| Sistema | Cómo está | Problema |
|---|---|---|
| Misiones (`Inventory.Missions`) | Lista de textos sueltos. Cada script la llena a su manera (`AddMission` en 20 sitios). | No hay una cadena de pasos. Una misión aparece **después** de tropezar con el objeto (p. ej. "Busca el código del armario" sale al tocar el candado). |
| Dónde se ven | Círculo arriba a la derecha → abre el inventario en la pestaña *Misiones*. | Con el mouse bloqueado (FPS) casi nadie lo abre. **El objetivo actual nunca está a la vista.** |
| Avisos (`ObjectiveHUD.Notify`) | Cartel abajo al centro, 4 s. | Se pisan entre sí y aparecen **al mismo tiempo** que la nota que estás leyendo (captura: "Nueva misión: Sal del salón" debajo de la nota). Desaparecen solos. |
| Notas (`Note`) | Panel oscuro con letra genérica; la nota **desaparece** del mundo al leerla. | No usa el estilo del menú. Si no anotaste el número, tienes que buscarlo en el inventario. |
| Menús que congelan el tiempo | Nota, candado, inventario, respiración, calibración del micrófono, pausa, Game Over… (10 en total) | Cada uno pausa por su cuenta. Si se abren dos a la vez, el juego se queda congelado (la prueba automática se trabó justo ahí). |
| Mira (`InteractionSystem`) | Rayo sin filtro de capas. | **Choca con el propio jugador** (`Hit 'Player' a 0,00 m`), así que a veces "no hay nada" aunque estés mirando el objeto. |
| Sistemas duplicados | Hay `Inventory` en `InventoryManager` y en `_Systems`. Hay `BlackoutEvent` en `_Events` y en `BlackoutManager`. | Uno se destruye solo. Lo que estaba conectado al otro deja de funcionar sin avisar. |
| Inicio | Pantalla negra de 3 s, 3 parpadeos, desenfoque… y luego nada que diga qué hacer. | Se tarda en arrancar y aun así no sabes qué hacer. |
| Luz | Exposición automática, paredes claras, día. | Se ve **lavado y brillante**. El menú es oscuro, con luz roja y cálida: parecen dos juegos distintos. |

---

## 2. Cómo lo resuelven los juegos AAA

| Juego | Qué hace | Qué nos sirve |
|---|---|---|
| **Hogwarts Legacy** | La misión **seguida** se ve siempre en una línea arriba a la derecha (objetivo y sub-paso). Diario con misiones principales y secundarias. *Revelio*: un botón resalta lo interactuable cerca. Los tutoriales salen la primera vez que se necesita el control y no vuelven. | Línea fija de "qué hago ahora". Un botón para **revelar** lo cercano. Tutoriales en contexto. |
| **Resident Evil 2/4/Village (remakes)** | HUD casi vacío. El objetivo está en el **mapa** y en la pausa. El mapa pinta las salas: **rojo = quedan cosas, azul = vacía**. Los objetos clave dicen dónde se usan y se descartan solos (✔) cuando ya no sirven. Los archivos quedan guardados. | **Mapa que se va pintando** (perderse con sentido). Objetos clave que dicen para qué sirven. |
| **Outlast / Amnesia** | Sin marcadores. El **cuaderno del protagonista** se actualiza con sus pensamientos ("Tengo que encontrar la salida…"). | El diario como voz del personaje: da miedo y a la vez orienta. |
| **The Last of Us / Uncharted** | Sin flechas: guían la luz, el color y el diseño del nivel. Si estás perdido **unos minutos**, el personaje comenta una pista; si sigues perdido, la cámara sugiere. | **Pistas escalonadas por tiempo**, sin que el jugador las pida. |
| **Dead Space** | Botón que dibuja en el piso una línea hacia el objetivo, **solo si lo pides**. | Ayuda a pedido para quien se frustra. |
| **Half-Life / Portal** | Se aprende jugando en una sala segura, sin texto largo. | El **4°B es la sala tutorial**: sin peligro, todo se aprende ahí. |

**Patrón común:**
1. El objetivo **siempre** se puede consultar en un toque.
2. Los avisos **esperan** a que termines lo que estás haciendo.
3. Las ayudas crecen con el tiempo que llevas atascado.
4. Lo diegético (cuaderno, mapa en la pared, voz del personaje) mantiene la inmersión.

---

## 3. Propuesta para Están Dentro

### 3.1 El Cuaderno de Mateo (reemplaza al inventario actual)
Un cuaderno escolar (rayado, letra de máquina de escribir *Special Elite*, colores del menú). Se abre con **Tab**. Pestañas:
- **Ahora:** el objetivo actual escrito como pensamiento de Mateo, más los pasos ya hechos tachados.
- **Pistas:** cada nota leída queda copiada. Lo importante va **subrayado** (p. ej. "pág. **142**"), según la dificultad.
- **Objetos:** cada objeto dice para qué sirve ("Destornillador — los tornillos de la rejilla del techo"). Cuando ya no sirve, sale tachado.
- **Plano:** el plano de evacuación del colegio (está pegado en los pasillos del juego). Se pinta como en RE: **sala vista**, **quedan cosas**, **cerrada con candado**.

### 3.2 HUD mínimo, con el estilo del menú
- **Línea de objetivo** arriba a la izquierda, con letra de máquina. Se ve 8 s cuando cambia y luego se desvanece. Vuelve:
  - si el jugador toca **Tab** un momento;
  - si lleva 40 s sin avanzar;
  - si se acerca a algo relacionado.
- **Cola de avisos:** nunca se pisan. Esperan a que cierres la nota o el candado. Duran ≥ 5 s y se pueden releer en el cuaderno.
- **Tarjeta de objeto clave** (estilo RE): al recoger algo importante se ve el objeto en 3D y su nombre, y se cierra con E. No se va sola.
- **Mira con intención:**
  - El punto cambia de icono: mano (tomar), ojo (leer), candado (código), puerta.
  - Al lado sale el nombre: "Casillero — candado de 3 dígitos".
  - Las teclas solo aparecen **las primeras veces**.
- **Concentrarse** (nuestro *Revelio*, atado a la respiración): mantener **Q** hace que Mateo respire hondo y que lo interactuable cercano brille 2 s. Con el estrés alto no funciona: el miedo nubla. Así el sistema de estrés también sirve para orientarse.

### 3.3 Misiones como datos, no como textos sueltos
- Un archivo por capítulo (`ScriptableObject`) con la cadena de pasos.
- Cada paso se completa con un evento: recoger X, resolver el candado Y, entrar a la zona Z.
- Un solo `GestorMisiones` muestra el paso siguiente, actualiza el cuaderno y lanza las pistas.
- **Ventaja:** el diseñador ve todo el capítulo en un solo lugar. Si un paso no tiene cómo cumplirse, la prueba automática lo detecta.

**Capítulo 1 como cadena:**

| # | Objetivo en el cuaderno | Se cumple con | Pista 1 (3 min) | Pista 2 (6 min) |
|---|---|---|---|---|
| 1 | "¿Dónde están todos? La puerta no abre." | Intentar abrir la puerta del 4°B | — | El picaporte brilla |
| 2 | "Mi casillero tiene candado… ¿qué número?" | Leer la nota del cajón del profe | "El profe siempre dejaba la tarea en su escritorio." | El escritorio brilla |
| 3 | "Pág. 142… ¿el casillero?" | Candado 142 | "Ciento cuarenta y dos… tres números, como el candado." | — |
| 4 | "Una carta de papá. La lonchera…" | Candado 046 | "Papá siempre usaba la misma clave." | La lonchera brilla |
| 5 | "Está oscuro. La linterna (F)." | Encender la linterna | Aviso de tecla | — |
| 6 | "Las paredes dicen cosas… la rejilla del techo." | Quitar la rejilla con el destornillador | "El aire viene de arriba." | La rejilla brilla |
| 7 | (ductos) "Sigue el aire." | Salir de los ductos | — | — |
| 8 | "La rectoría. Cuatro dígitos: día y mes." | Candado 1705 | "17 de mayo… la fecha del cuaderno." | La hoja brilla |
| 9 | Final | Entrar a la rectoría | — | — |

### 3.4 Empezar sin introducción larga
- Despertar de **5 s** como máximo (un parpadeo y la vista se aclara). La primera línea de objetivo aparece **enseguida**.
- La cinemática y el texto del papá son **opcionales**: se encuentran jugando (cartas, recuerdos) y se pueden saltar.
- El 4°B es la sala tutorial: mirar, leer, candado, linterna, empujar. Sin monstruo ni Game Over.
- Ajuste **Ayudas: Ninguna / Sutiles / Completas** en el menú de ajustes (afecta las pistas por tiempo y el subrayado).

### 3.5 Estilo visual unificado (menú = juego)
- **Paleta del menú:**
  - fondo casi negro;
  - texto crema `#F2EDD9`;
  - acento ámbar `#D9B347`;
  - peligro rojo oscuro `#9E1F1F`;
  - luz cálida roja.
- **Fuentes:** *Creepster* solo para títulos grandes (capítulo, final). *Special Elite* para todo lo demás: notas, cuaderno, HUD y avisos.
- **Luz del juego:**
  - **Exposición fija** por zona, con un volumen por zona en vez de la automática, que hoy lava la imagen.
  - Interiores más oscuros, con contraste y luz cálida de bombillo.
  - El día de afuera visto por las ventanas, pero sin inundar el salón.
  - Tras el apagón: solo la linterna y el rojo de emergencia, como en el menú.
- **Notas en papel de verdad:** hoja rayada o cuaderno sobre fondo oscuro, letra de máquina, manchas. La nota **no desaparece** del mundo: queda leída y con una marca.

---

## 4. Orden de trabajo propuesto

1. **Arreglar lo roto de base:**
   - la mira no debe chocar con el jugador;
   - quitar los sistemas duplicados;
   - un solo gestor de pausa, para que dos menús no se congelen entre sí;
   - exposición fija y escena más oscura.
2. **Gestor de misiones** con datos, y el capítulo 1 pasado a esa cadena.
3. **HUD nuevo:** línea de objetivo, cola de avisos, mira con icono y nombre, tarjeta de objeto clave.
4. **Cuaderno de Mateo** (Ahora, Pistas, Objetos) con el estilo del menú. Las notas pasan al mismo estilo.
5. **Pistas por tiempo** y **Concentrarse (Q)**.
6. **Plano** que se va pintando (cuando haya más de un piso jugable).
7. La prueba automática recorre la cadena de misiones y avisa si algún paso no se puede cumplir.

## 5. Decisiones para Henry
- [ ] ¿El cuaderno **pausa** el juego (como RE) o el mundo sigue mientras lo lees (más tensión, como Amnesia)?
- [ ] ¿Marcadores en pantalla **nunca** (solo pistas y brillo), o una línea de ayuda a pedido como en Dead Space?
- [x] "Concentrarse" con Q: **sí** (19-oct). Hecho.
- [x] Luz: **de noche** desde el inicio (19-oct). Hecho.

---

## 6. Tablero de investigación (idea de Henry, 19-oct) — REEMPLAZA a las pistas sueltas del cuaderno

> Referencias: la pared de hilos de *Dark*, la oficina de Hank en *Breaking Bad*, el *Case Board* de *Alan Wake 2* y la mesa de planificación de atracos de *GTA Online*.
> **Prototipo jugable:** `01_Documentacion\prototipo_tablero_mateo.html` (también en https://claude.ai/artifact/6CtjaWoPLZF2FE3AXamZvN).

**Cuaderno (lo que llevas encima).** Guarda notas, cartas, **fotos** y objetos clave. Se consulta rápido con Tab, pero no sirve para deducir.

**Fotos.** Mateo tiene un **celular viejo con la pantalla rota**:
- Sirve para fotografiar candados, puertas cerradas, escritos que solo se ven con la linterna y relojes.
- Las fotos van al cuaderno.
- Algunas muestran cosas que no estaban (Pesadilla).

**La pizarra (tablero de investigación):**
- Se despliega en **cualquier tablero de salón o escritorio de profesor**. Son los "cuartos seguros" y puntos de guardado, como las máquinas de escribir de RE. Eso crea el ir y venir que ya queríamos.
- Se pegan las pistas del cuaderno con chinches y se **unen con hilo rojo**.
- Si dos pistas encajan, Mateo escribe la **deducción** en tiza (p. ej. "142 — abre el casillero"). Eso actualiza el objetivo y el plan.
- Si no encajan, el hilo se cae y Mateo dice "No… eso no encaja", sin castigo.
- Puede haber **varias formas válidas** de llegar a la misma deducción. Ejemplo: 17/05 sale de la hoja + el cuaderno, o de la hoja + el reloj.
- El estado de la pizarra es el mismo en todos los tableros. En Pesadilla, a veces aparece **un hilo que Mateo no puso**.
- El mundo **no se pausa** mientras investigas: hay tensión.
- **Ayudas** (ajuste): *Ninguna* = solo manual. *Sutiles* = tras unos minutos parpadean dos pistas que encajan. *Completas* = Mateo las une solo al pegarlas.

**El plano (como la mesa de atracos de GTA Online):**
- Segunda vista del mismo tablero: el plano de evacuación del colegio, piso por piso, generado de `edificio_lib.py`.
- Las salas se pintan: vista y vacía / quedan cosas / cerrada con candado.
- Las fotos se clavan en su lugar del plano.
- Al lado está el **PLAN: Salir del colegio** con sus **preparativos** (abrir el casillero, conseguir luz, llegar al primer piso, clave de la rectoría) y el **final**, bloqueado hasta completarlos.
- Cada capítulo es un plan con sus preparativos; el final de cada capítulo es el "golpe".
- En el HUD **no** va mapa: solo la línea "Ahora". El plano se mira en la pizarra o, en pequeño, en el cuaderno.

**En Unity:**
- `ScriptableObject`s `Pista` (id, tipo, texto, foto) y `Deduccion` (pares que encajan, resultado, qué desbloquea), dentro de cada `Capitulo`.
- La pizarra es un Canvas en el mundo, sobre el tablero real del salón. La cámara hace zoom y el cursor queda libre.
- El hilo es un `LineRenderer` con caída.
- El gestor de misiones del punto 3.3 escucha las deducciones.


---

## 7. Cámara del celular, flash e impresora (idea de Henry, 19-oct)

> Como la cámara de *Outlast*, pero para **ver lo que no viste en el momento** (parecido a *Fatal Frame*).

**La cámara del celular:**
- Se apunta con el clic derecho y se dispara con el izquierdo. El **flash** ilumina un instante todo el cuarto.
- Tiene **pocas cargas de batería**. Comparte las pilas con la linterna, así que hay que decidir.

**La foto ve más que el ojo:**
- En la foto aparecen cosas que no estaban a simple vista: escritos en la pared, una silueta en el pupitre de adelante, números en el tablero, una puerta que no existía.
- La foto se guarda en el cuaderno (pestaña *Fotos*).
- **Asusta:** a veces la foto muestra a "ellos" justo detrás de ti.

**Imprimir para la pizarra:**
- En la **sala de cómputo** o la **secretaría** hay una **impresora vieja**. Solo las fotos impresas se pueden pegar en la pizarra de investigación.
- Eso obliga a ir y volver: un recorrido con sentido entre la impresora y los tableros.
- La impresora necesita luz (caja de fusibles), papel y tinta. Cada cosa puede ser un pequeño objetivo.

**En Unity:**
- La foto es una cámara que pinta a una `RenderTexture` con una capa extra: lo que solo se ve en fotos vive en la capa `SoloFoto`, que la cámara principal no dibuja.
- La foto se guarda como `Texture2D` en el inventario (y en disco para el guardado).
- Impresora: un `Interactable` que convierte fotos en "fotos impresas" (objeto de bolsillo).

**Encaja con:**
- La tarjeta de objeto estilo Polaroid (muestrario B: https://claude.ai/artifact/Hxqa3NsNedTqFMop7j16uf).
- La pizarra y el plano (sección 6).
