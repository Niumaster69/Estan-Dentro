# Están Dentro — Decisiones e ideas

> Lo que Henry ya decidió y las ideas que no se pueden perder. Si algo cambia, se actualiza aquí con la fecha.

## Decisiones tomadas
| Fecha | Decisión |
|---|---|
| 18-oct | Seguimos en **Unity HDRP**: hay 69 scripts funcionando y la RTX 3050 de 4 GB no aguanta bien el editor de UE5. La calidad se escala para quien tenga PC potente. Si la prueba en HDRP no convence, se reevalúa Unreal con algo concreto en mano. |
| 18-oct | Objetivo visual: **realismo máximo** aprovechando HDRP, con calidades escalables (ray tracing en Ultra y Cinemático). |
| 18-oct | Premisa: **"te quedas encerrado en el colegio"** estilo *Fenómeno Siniestro* (Grave Encounters). Te pierdes, pero con sentido: hay que aprender el edificio para avanzar. Acertijos y llaves estilo Resident Evil. |
| 18-oct | Estilos elegidos: **F1 (Recuerdo)** y **F2 (Pesadilla)** como base; **A2** (luz roja de emergencia) para persecuciones; **B** (claustro colonial) para el ala antigua (rectoría, secretaría, vestíbulo, capilla, sala de profesores). |
| 18-oct | Estética: **colegio colombiano real**: ladrillo tolete, zócalo de azulejo teal, piso en ajedrez, pupitres de tubo, rejas verdes, jacarandá, golosa, tarima de izada de bandera. Nada "genérico de IA". |
| 18-oct | Texturas fotográficas CC0 (Poly Haven) a escala real medida, con anti-repetición. |
| 18-oct | Se reutilizan los modelos de Henry donde tienen identidad (lonchera, linterna, casilleros, rectoría…); en Unity se usan sus prefabs. |

## Reglas de diseño (para que el jugador entienda sin explicaciones)
- **Dorado / cálido = Recuerdo = a salvo. Teal / frío = Pesadilla = peligro. Rojo = persecución.**
- Puntos de referencia siempre visibles: el **patio**, el **jacarandá**, la **pila** y la **tarima con el asta**. Al "romperse" el colegio, estos puntos cambian de lugar o de forma, y así el jugador nota que algo está mal.
- Cada evento importante tiene **sonido + efecto visual** al entrar, mientras dura y al salir.

## Espacio del jugador (regla, 18-oct)
- El jugador mide unos **0,6 m de ancho**. Entre muebles debe quedar **0,6 m como mínimo**, y en las rutas principales (pasillos, salas de eventos) **0,9–1,2 m**.
- Las salas largas se amueblan **a lo largo**, como el auditorio, y no a lo ancho a la fuerza.
- Nunca se tapan puertas ni ventanas: `contra_pared` lo revisa solo.

## Edificio (resumen legible)
- **Sótano**: escalera de servicio, calderas, archivo muerto, depósito, cuarto de bombas, túnel de servicio → **SALIDA REAL**.
- **P1**: Escalera A, Baños, Cuarto eléctrico (baja al sótano), Escalera B, **Auditorio** (norte), **Rectoría**, **Vestíbulo/Portería** (puerta principal = **SALIDA FALSA**), **Secretaría**, **Enfermería**, **Capilla**, **Cocina**, **Cafetería**.
- **P2**: Escaleras A y B, Baños, Cuarto de aseo, Aula 7°A, Aula 7°B, **Sala de profesores**, **Biblioteca** (sur), Lab. de química, Lab. de biología, Aula 6°A, Aula 6°B.
- **P3**: Escalera A (sube a la azotea), Escalera B, Baños, Archivo de notas, Salón 4°A, **Salón 4°B (INICIO)**, Salón 4°C, Salón 5°A, Depósito de pupitres, Salón 5°B, Sala de cómputo, Aula de música, Salón 3°A, Salón 3°B.
- **Azotea**: caseta de la Escalera A y tanques de agua (final alternativo opcional).

## Referencias de Henry (Pinterest, analizadas el 18-oct)
- *Colegio estilo 1* = nostalgia: hora dorada, ventanales, pasillos largos, casilleros, ajedrez, ladrillo → **estado Recuerdo**.
- *Spot estilo 2* = abandono: pintura que se cae, pasillos teal tipo hospital, óxido, grafiti, agua, naturaleza que invade → **estado Pesadilla**.
- *Sótano* = túneles redondos, pozos con escalera de gato, agua y escaleras oxidadas, más **sueños raros** (cama en un prado, pasillo de flores), estos cerca de la verdad.
- *Elementos* = objetos para usar o decorar (Brick Game, radio, CRT, archivadores, tableros eléctricos, tuberías, letreros de peligro, lavamanos viejo, lámparas industriales, cámaras CCTV) y **criaturas como dibujos de tinta de niño con ojos blancos** → dirección para los monstruos.

## Eventos (con sonido y efectos) — para no olvidar
### Viento que desata un recuerdo (idea de Henry, 18-oct; se parece a juegos como *Beyond: Two Souls*)
- **Dónde**: patio, bajo el jacarandá (o en otros puntos con ventanas abiertas).
- **Entrada**:
  - Ráfaga de viento lateral (Wind Zone de Unity).
  - Las hojas y flores moradas se desprenden y flotan de lado (VFX Graph con partículas de hojas que siguen el viento).
  - El sonido sube: viento + hojas + campana lejana.
- **Durante (recuerdo)**:
  - Transición a la paleta cálida: bloom, viñeta suave y grano.
  - Voz o sonido del recuerdo.
  - El mundo puede quedar más lento.
- **Salida**:
  - Las hojas terminan de caer.
  - El color vuelve al estado actual.
  - Un sonido de cierre, como un suspiro, y aparece una pista nueva o cambia algo del entorno.
- **Técnico**: trigger + Timeline + Volume de post-proceso con peso animado + VFX Graph + AudioMixer snapshot.

### Golosa
- En el Recuerdo los números van en orden. En la Pesadilla algunos aparecen cambiados o marcados: esa secuencia es la clave de un candado.
- Sonido de tiza o de saltos de niños al acercarse.

### Tapa de alcantarilla y rejillas
- Si sueltas un objeto por la rejilla, se escucha caer abajo: así se descubre que hay un túnel.
- La tapa abre la ruta al sótano.

### Charcos
- En la Pesadilla reflejan la otra versión del colegio (como el Observador). Técnico: planar o screen-space reflection con otra capa.

### Pila
- En el Recuerdo tiene agua y suena correr el agua. En la Pesadilla está seca, con hojas podridas y una llave en el fondo.

### Salida falsa y bucles (Fenómeno Siniestro)
- La puerta principal del vestíbulo devuelve al jugador al pasillo del P3.
- Pasillos que se alargan. El jacarandá aparece del lado equivocado.

### Sótano con monstruo + patio trasero (idea de Henry, 18-oct; estilo *Hello Neighbor*)
- **Ruta**: túnel de servicio del sótano → escalera recta → **caseta** en el patio trasero (3,6–7,4 × 39,8–47,4 m) → **cuarto de herramientas del celador** (38–45 × 45–50 m) → **portón trasero encadenado** (x 22–26, y 54).
- **Salida final**: se corta la cadena del portón trasero con la **cizalla** del celador.
- **Monstruo del sótano**: patrulla calderas, archivo, depósito, cuarto de bombas y túnel. Oye ruidos (pasos en agua, objetos que caen). Sin armas: esconderse (cuarto bajo la escalera, estanterías, detrás de las calderas) y aguantar la respiración (minijuego existente).
- **Regla de ida y vuelta**: los objetos grandes se cargan **de a uno**, con las dos manos (escalera de tijera, cizalla, galón de gasolina, batería del generador, llave de tubo). Para usar lo del cuarto de herramientas hay que volver a entrar al edificio por el sótano y llevar cada objeto donde sirve: así se cruza el sótano varias veces, y en cada pasada el monstruo está más alerta.
- **Cuarto de herramientas** (referencias de tus pines "Elementos"): herramientas oxidadas, llaves inglesas, galón rojo, escalera de tijera, extintor, tubos, cajas eléctricas, caja fuerte, botiquín, linterna de campaña, lámpara de queroseno, cilindro de gas.
- Cada cruce tiene sonido de entrada (puerta pesada, eco), tensión durante (respiración, goteo, pasos del monstruo en 3D) y salida (alivio, la luz del exterior).

### Puertas en Unity: se REUSA y se mejora (18-oct)
- Se mantiene `Interactable` (base) + `InteractionSystem` (mira y E) + `LockedDoor` (bloqueo, candado integrado `CombinationLock`, sacudida, audio, HUD). Funcionan y ya están probados en el Capítulo 1.
- Se **mejora** `LockedDoor` (o una clase nueva `Puerta` que la extienda):
  - Las hojas `PUERTA_*` del .blend ya traen el **origen en la bisagra** y `bisagra_sentido`, así que la rotación por código funciona sin configurar nada a mano.
  - Se abre **hacia el lado contrario al jugador**, se puede **dejar entreabierta**, empujarla poco a poco (al estilo Outlast) y cerrarla de golpe.
  - **Llave por tipo** (no solo códigos): la llave del tablero del celador abre una zona.
  - Sonidos: crujido según el material (madera o metal), golpe al cerrar y, si está cerrada con llave, el pestillo.
  - El monstruo también abre puertas (con su propio sonido) y se puede **bloquear una puerta con un mueble empujable**.
- Las puertas dobles (portón de entrada) usan dos hojas con el mismo script.
- **Importador automático** (Editor): lee las propiedades del .blend (`tipo`, `funcion`, `interactuable`, `prefab_unity`) y pone solo los componentes. Las hojas de puerta reciben `LockedDoor`, los contenedores `LockedContainer`/`SimpleOpenable`, los empujables `HeavyProp` + Rigidbody, y los escondites un script nuevo `Escondite`.

### Ductos de ventilación "trampa" (Henry, 18-oct)
- Las **rejillas** de salones y pasillos son puertas. El ducto **no está dentro de los muros**: es un espacio aparte, fuera del edificio o en la escena `EsecenaUno-DuctosDeVentilacion`. Se conecta al entrar y salir con un fundido corto o sin corte, poniendo al jugador en la posición correcta.
- Para que el jugador se lo crea: cámara a ras del piso, gateo lento (`CrawlFootsteps` ya existe), eco metálico, golpes (`DuctThumps`), luz que entra por las rejillas de abajo y voces de los salones que pasan por debajo.
- Como no necesita medidas reales, el ducto puede ser **más largo o imposible** (estilo Fenómeno Siniestro): se sale a un piso que no corresponde.
- Cada rejilla visible en un salón se registra con su "salida" en el mapa del ducto.

### Todo objeto tiene función (Henry, 18-oct)
Cada mueble lleva la propiedad `funcion` (en `amueblar.py`, diccionario `FUNCION`), que se exporta a Unity:
- **contenedor** (mochilas, escritorios, archivadores, loncheras, casillero simple, neveras, vitrinas, botiquín, cajas…): se abre o registra y trae **botín según la sala** (tabla de botín). Puede estar vacío, tener una nota, baterías, una llave o un susto.
- **recogible** (linterna, baterías, llaves, cartas, radio, Brick Game, galones…): va al inventario.
- **empujable** (pupitres, sillas, canecas, silla de ruedas, atril de suero, TV…): física. Se puede mover para bloquear una puerta o hacer ruido que atrae al monstruo. Ya existen `HeavyProp` y `PlayerPropPusher`.
- **escondite** (casilleros dobles, cubículos de baño, cuarto bajo la escalera): el jugador se mete y aguanta la respiración.
- **decorativo**: solo ambienta (lámparas, carteleras, estanterías fijas).

### Máquinas dispensadoras (Henry, 18-oct)
- Las máquinas de la cafetería **entregan objetos** (gaseosa, snack) con una moneda. Una gaseosa puede servir para algo: distraer al monstruo al tirarla, recuperar estrés, etc. Se define en Unity.

### La mochila es el inventario (Henry, 18-oct)
- El protagonista encuentra **su mochila** en el Salón 4°B: es el inventario visible (al abrirla se ve en 3D, como el `ItemPreviewOverlay` que ya existe).
- Empieza con **pocos espacios** y se **amplía** al encontrar riñoneras o bolsillos, como los bolsos de Resident Evil.
- Los objetos pesados no caben: van **en las manos** (de a uno), y eso obliga a ir y volver.

### Inventario limitado (Henry, 18-oct)
- El inventario tiene **pocos espacios**. Los objetos pesados (escalera, cizalla, galón, batería) ocupan las dos manos. El jugador tiene que **ir, dejar y volver**: eso obliga a cruzar zonas peligrosas (sótano con monstruo) varias veces.
- Hay lugares seguros donde dejar cosas, al estilo de los baúles de Resident Evil: el cuarto del celador puede ser uno.

### Cuartos detrás de las escaleras (18-oct)
- El hall de cada escalera tenía libre una franja de 4 × 2,8 m detrás del tramo 2. Ahora es un cuarto con puerta:
  - **P1, Escalera A: cuarto del celador**, con **tablero de llaves** al estilo Resident Evil (cada llave abre una zona).
  - **P1, Escalera B: bombillos y fusibles** (los fusibles devuelven la luz a una zona).
  - **P2 y P3: bodegas.**

## Historia (pendiente de unificar)
Hay 3 versiones:
1. GDD: niño, identidades disociativas, 5 capítulos.
2. Capítulo 1: Mateo Mendoza en coma, el papá vivo, códigos 142 / 046 / 1705.
3. Premisa nueva: encerrado en el colegio.

**Recomendación**: la premisa nueva manda y la historia del coma y el papá queda como capa opcional (cartas y recuerdos) que se revela al final. **Falta que Henry lo confirme.**

## Los 3 estados de la mente de Mateo — qué los dispara (propuesta 21-oct, falta el sí de Henry)
| Estado | Se dispara por | Se sale por | Cómo se ve |
|---|---|---|---|
| **Normal** (el colegio real) | — | — | Colores del colegio, linterna, lluvia. |
| **Pesadilla** (el colegio roto) | 1) **Recuerdos del accidente**: tocar algo ligado a ese día (el "17 de mayo", la foto, el celular de Pa) → momentos fijos de la historia. 2) **Estrés ≥ 80 % sostenido** unos 10 s: la mente se quiebra. | **Respirar** (Q) hasta bajar el estrés, o **resolver** lo que la Pesadilla esconde (el objeto o número que solo existe ahí). | Garabatos de lápiz teal que hierven en los bordes, el mundo se dibuja a líneas en el golpe, texturas dañadas, niebla teal, luces bajas. Objetos en otros lugares y números cambiados. **El Observador** mira desde lejos. |
| **Alterado** (la persecución) | **El Observador te detecta** dentro de la Pesadilla: lo miras mucho rato, haces ruido o te acercas. | **Esconderte** (casillero o armario) y cortarle la vista → vuelves a la Pesadilla. Si te alcanza, colapso. | Golpe rojo, todo en líneas rojas por un instante, zoom hacia adentro, latido fuerte, luz roja y visión en túnel. |

Flujo: Normal → (recuerdo o estrés) → **Pesadilla** → (te ve) → **Alterado** → (te escondes) → Pesadilla → (respiras o resuelves) → Normal.
Técnica: `Scripts/Mente/EstadoMente.cs` + `Resources/Mente/MenteFullscreen.shader` (pase de pantalla completa de HDRP) + `MundoPesadilla.cs` (72 materiales dañados en `Art/Edificio/Materials_Pesadilla`). Prueba: F6 Pesadilla, F7 Alterado, F8 Normal. Fotos: `-fotosmente <carpeta>`.
