# Están Dentro — POR HACER

> Checklist de lo pendiente, en orden de prioridad. Regla: **al terminar algo se borra de aquí y se anota en
> `HECHO.md`** con fecha y ruta. Cada punto trae suficiente detalle para no tener que volver a preguntar.
> Las decisiones y las ideas de diseño están en `DECISIONES_E_IDEAS.md`. Las rutas son relativas a `Estan Dentro\`.
> Limpiada el 18-oct (noche): se quitó todo lo que ya está en `HECHO.md`.

---

## ▶ RAMA `mejoras/remaster-capitulo1` — SEGUIR AQUÍ (30-oct, 5 p. m.)
Entrega: martes 3-nov. Lo hecho está en `HECHO.md` (30-oct). Ciclo de trabajo en el PC de Duvan: `C:\Users\duvan\Dev\ciclo.ps1`
(aplicar a la escena → capturas → compilar → prueba automática; `-sinAplicar -sinCapturas` para solo compilar y probar).
- [ ] **Jugar a mano** el capítulo completo: respirar con Pa (Q, ritmo 4-4-6), el escondite con el Observador cerca
  (aguantar el aire), el bucle del pasillo y el final del hospital. La prueba automática no cubre el escondite.
- [ ] Medir FPS en la GTX 1650 con calidad Media y Alta (Ajustes > Video).
- [ ] Hospital: cara de Mateo más trabajada (hoy es una forma simple), foto de Mateo y Pa en el portarretratos, cortina.
- [ ] Jugar a mano lo nuevo del 30-oct (noche): mapa (M), modo exploración, «hab. 306» y el candado del aula de música,
  el recuerdo de respirar tras el apagón y el Observador caminando (¿asusta? ¿2,1 m/s está bien?).
- [ ] Luz dorada propia del aula de música para el recuerdo de Pa cantando; alfombra de flores bajo el jacarandá.
- [ ] Grabar "seis" con la voz de Pa (hoy solo subtítulo) y las grabaciones de Pa coleccionables.
- [ ] Actualizar la página "Están Dentro — Remaster" con capturas nuevas al terminar cada bloque.

## A. Probar lo que ya existe (lo primero)
### ▶ SEGUIR AQUÍ (actualizado 19-oct tarde)
- [ ] Pesadilla, siguiente: ¿más recuerdos? (el celular de Pa). Probar a mano la persecución en el pasillo del piso 3.
- [ ] **Rediseñar la escena de los ductos** (Henry: "que sea más lógica"): ducto que sale del techo del 4°B, corre sobre el piso 3 y baja por un tiro hasta la rectoría (y al revés).
- [ ] **Volver al 4°B**: ¿el nivel 1 es de ida (ducto sin regreso) o con regreso? Definir con Henry (puertas por color tipo RE).
- [ ] **Posprocesado y shaders** (Henry lo propuso como siguiente).
- [ ] **Escondites en armarios** de diseño distinto a los casilleros, donde quepa la cámara sin atravesar nada.
- [ ] Escaleras: Henry ve "un piso verde" que no deja ver hacia abajo; no se encontró con rayos en la escalera NW: pedir captura.
- [ ] **Respiración**: analizar cómo funciona hoy (fallas) y rediseñarla.
- [ ] **Prólogo largo** (`PROLOGO_GUION_V2.md`): esperar el sí de Henry al guion. Luego: créditos de inicio (piano + guitarra), radio con locutor, foto de la mamá en el parasol, regalo de la linterna en la guantera, semáforo en rojo (el carro frena de verdad), choque en cámara lenta con pitido, campana que se funde con el 4°B.
- [ ] **Voces ElevenLabs** (llave en `D:\elevenlabs\llave.txt`; la cuenta gratis solo deja usar voces de la biblioteca desde la página: se generan ahí y se bajan por la API del historial). Mateo = **Sebastian - Lively Teenager**. Pa = elegir entre las pruebas de `04_Audio\voces\pruebas\`. Ecualizar en Unity para que suenen dentro del carro (pasa-bajos, pasa-altos, eco corto).
- [ ] **Mateo piensa en voz alta en cada acción** (prólogo y colegio): primera vez que coge cada objeto, cada nota o carta ("esta letra es de Pa…"), candados, puertas trabadas. Para que nadie tenga que leer. Hacer la lista de frases y pasarla a Henry antes de generar.
- [ ] Música de los créditos de inicio: original con Eleven Music (1 min = 1.800 créditos) o una gratis; Henry decide.
- [ ] Voces del colegio: lista de acciones lista (enganche central en `ObjectiveHUD.NotifyInternal` y `NoteOverlay.Show`, apagón, susto del celular con la voz de Pa lejos). Hay una hoja vieja que contradice la historia ("Mira los retratos del aula… Tu padre la escribía siempre", Colegio.unity) y el susto "portazo" no está en ningún casillero.
- [ ] **Calle estilo Resident Evil Requiem (inicio)**, sin matar la GPU: fachadas con relieve (marcos, balcones, rejas, toldos, avisos de tiendas bogotanas), texturas reales de Poly Haven (ladrillo, pañete, asfalto mojado con charcos que reflejan), cables de luz entre postes, luces de sodio, carros parqueados. Trucos de perspectiva: lo cercano con detalle y lo lejano con siluetas y niebla en capas; instancias repetidas y LOD; reflejos del asfalto con SSR. Henry: "no tan detallado pero sí mejor de lo que está".
- [ ] Calle con más detalle (estilo The Last of Us, no low poly): fachadas reales de Bogotá, andenes, postes, árboles, tiendas, carros parqueados.
- [ ] **Prólogo, pendientes**: Henry revisa las manos de Pa y el choque (fotos en la sesión); pantalla de créditos con los autores CC-BY (`CREDITOS_MODELOS.md`); la calle es de bloques simples (mejorar fachadas); el cuaderno de Mateo es una caja azul (darle textura de cuaderno); usar la animación completa de Mixamo para que Pa mueva el timón.
- [ ] **Sonido**: Henry escucha y dice cuáles no cuadran (hay 3 opciones por sonido). Falta conectar zumbido de fluorescentes (que se apague con el apagón), tinnitus al despertar y timbre.
- [ ] Un papel a 9 cm sobre su apoyo (`PROP_papel.004`, en 0.65, 0.82, 12.70): bajarlo en `amueblar`.
- [ ] Pata de cabra: ponerla más visible (apoyada en la pared junto al escritorio del celador); la prueba la alcanza con dificultad.
- [ ] Indicador de pilas de la linterna en el cuaderno o el HUD (hoy solo se nota cuando titila).
- [ ] Monstruo en modo feria: presencias guionizadas (silueta al fondo, golpes en los ductos, casillero que se cierra). Persecución solo en modo normal.
- [ ] Mochila: el modelo 3D es una caja simple (en el dibujo se nota). Modelarla mejor (correas, bolsillo, cierre).
- [ ] Cuaderno: falta la pestaña PLANO (con la pizarra) y la de FOTOS (con el celular).
- [ ] **Cámara del celular con flash + impresora** para la pizarra (diseño en `PROPUESTA_MISIONES_HUD_ESTILO.md` §7). Va junto con el cuaderno y la pizarra.
- [ ] Henry revisa la ambientación nueva (rebote, niebla, etalonaje) en su pantalla. Valores en `ConstruirColegio.NOCHE`.
- [ ] **Narrativa unificada** (`NARRATIVA_UNIFICADA.md`): esperar el sí de Henry y del equipo (sección 7). Luego, en orden: textos nuevos, final en el hospital, prólogo en el carro, cadena de misiones, modo feria y actualizar el GDD y el Plan de Producción (`03_Juego_Unity\EstanDentro\Document\GDD_y_Plan_de_Produccion_actualizado.docx`).
- [ ] **Henry prueba el capítulo 1** con `06_Builds\Colegio\EstanDentro_Colegio.exe`:
  - Que se entienda solo, sin explicación.
  - Que la noche no quede demasiado oscura en su pantalla.
  - Que Q se sienta bien.
- [ ] (Si se publica) montar la API en la nube con HTTPS y poner la URL en `StreamingAssets/nube.json` (ver `GUARDADO_LOCAL_Y_NUBE.md`). Preguntar a Duvan por su proyecto `ApiJuego`.
- [ ] Soltar con G solo funciona con objetos recogidos del piso. Falta una forma de dejar lo que salió de un contenedor (p. ej. devolverlo al casillero).
- [ ] **Línea "Ahora" y gestor de misiones** con datos (propuesta 3.2 y 3.3): el capítulo 1 como cadena de 9 pasos con pistas por tiempo.
- [ ] **Cuaderno de Mateo + pizarra de investigación + plano** (propuesta sección 6; prototipo en `prototipo_tablero_mateo.html`).
- [ ] **Celular con cámara** para fotografiar candados, puertas y escritos (las fotos van al cuaderno).
- [ ] La prueba automática no prueba Q ni el teclado real: probarlos a mano.
- [ ] **Subir a GitHub en repositorio NUEVO**: falta que Henry cree el repo vacío y deje el token en `D:\github\token-push.txt`. Leerlo solo por stdin, sin guardarlo en el gestor de credenciales.
- [ ] **Henry juega la escena** `Assets/Scenes/Colegio/Colegio.unity`: caminar todo el colegio, subir y bajar escaleras, abrir puertas, recoger y registrar objetos, esconderse. Anotar lo que falle (colisiones, puertas que giran al revés, objetos sin función).
- [ ] Rendimiento en este portátil (RTX 3050 4 GB): medir FPS. Si va lento, el primer paso es el punto C (luz horneada).

## B. Jugabilidad base (lo que hace que sea un juego)
- [ ] **Puertas** (mejorar `LockedDoor`): abrir hacia el lado contrario al jugador, entreabrir y empujar despacio, llaves por tipo (tablero del celador), sonido según madera o metal, el monstruo también abre puertas, bloquear con un mueble empujable.
- [ ] **Escondite** completo: cámara dentro del casillero (rendijas), salir y entrar con animación, conectado con la respiración (`BreathingMinigame`) y el estrés.
- [ ] **Inventario = mochila** con pocos espacios y bolsillos que la amplían; los objetos pesados van en las manos, de a uno.
- [ ] **Tablas de botín por sala** para los `Contenedor` (qué hay en mochilas, escritorios y archivadores: notas, baterías, monedas, llaves, nada o un susto).
- [ ] **Monstruo del sótano** (estilo *Hello Neighbor*):
  - Patrulla con NavMesh y puntos de ruta.
  - Oye ruidos: pasos en agua, objetos que caen, máquinas.
  - Persigue y pierde de vista al jugador.
  - Se pone más alerta en cada cruce.
- [ ] **Ductos**: escena modular (tramos, codos, T, bajadas) armada según `red_ductos.json`; entrar por una rejilla y salir por otra, con salidas "imposibles".
- [ ] **Salida final**: cizalla en el cuarto de herramientas → cortar la cadena del portón trasero → créditos.
- [ ] **Salida falsa** (portón principal y vestíbulo) que te devuelve adentro, y **bucles de pasillo** (Fenómeno Siniestro).

## C. Iluminación y rendimiento
- [ ] Horneado de luz: Adaptive Probe Volumes y Reflection Probes por sala.
- [ ] Occlusion Culling (el colegio tiene muchas salas cerradas: ahorra mucho).
- [ ] Niveles de calidad Bajo / Medio / Alto / Ultra (HDRP Assets) conectados al menú de ajustes que ya existe (`SettingsOverlay.cs`). Meta: 60 fps en 1080p en este portátil (Alto).
- [ ] Piso del patio más claro (tinte del material `patio`).

## D. Estado Pesadilla (el colegio "se rompe")
- [ ] Cambio por zonas en Unity: texturas de Pesadilla (ya exportadas en `materiales.json`: `textura_pesadilla`, `tinte_pesadilla`), volumen teal, niebla, noche (cielo `kloppenheim_02`), fluorescentes que parpadean o están apagados, luz roja de emergencia (A2).
- [ ] Maleza, enredaderas, agua estancada y decals de sangre o pintura (HELP, manos, números, de los pines) **solo en la Pesadilla**.

## E. Desgaste tipo Resident Evil
- [ ] Decals: humedad, grietas, chorreones, grafiti y manchas de mano (también terminan de romper la repetición en paredes grandes).
- [ ] HDRP Layered Lit con máscara automática de desgaste en bordes, esquinas, zócalo y alrededor de puertas y ventanas. Más fuerte en el sótano.
- [ ] Piezas rotas en puntos clave: esquinas partidas, ladrillos sueltos, varillas.

## F. Contenido que falta modelar
- [ ] **Cuarto de herramientas** amueblado: escalera de tijera, **cizalla**, galón rojo, herramientas oxidadas, cajas eléctricas, caja fuerte, cilindro de gas, botiquín, lámpara de queroseno.
- [ ] **Patio trasero**: maleza, chatarra, llantas, bus escolar abandonado y luces de poste.
- [ ] **Cuarto eléctrico** más completo (tuberías, cables). Celador con su uniforme colgado.
- [ ] Objetos de los pines para rellenar: Brick Game, pizarra mágica, canicas, borradores, esferos sobre los pupitres.
- [ ] Jacarandá: flores moradas, alfombra de flores caídas (Recuerdo) y LOD para Unity.

## G. Eventos con sonido y efectos (entrada, durante y salida)
- [ ] **Viento que desata un recuerdo** en el patio (idea de Henry).
- [ ] **Golosa** como acertijo, **pila** (con agua o seca con una llave), **tapa de alcantarilla** y **charcos** que reflejan la otra versión.

## H. Audio
- [ ] Ambientes por zona y estado, pasos según el tipo de piso, puertas, máquinas y transiciones de recuerdo.
- [ ] Revisar qué audios ya hizo Carlos (`03_Juego_Unity\EstanDentro\Document\BRIEF_CARLOS_AUDIO_CINEMATICAS.md`).

## I. Proyecto y equipo
- [ ] Convertir los `.mov` (4,6 GB) a `.mp4` y moverlos a `05_Cinematicas`.
- [ ] Hacer la API opcional (que el juego funcione sin el servidor de Duvan).
- [ ] Unir la narrativa: propuesta lista en `NARRATIVA_UNIFICADA.md` (eran 4 versiones). Falta aprobarla y pasarla al GDD.
- [ ] **Commit** en la rama local `integracion/estan-dentro-game` (configurar Git LFS para .fbx, .png, .hdr y .blend).
- [ ] Con las llaves (USB): decidir el repositorio (Niumaster69 o AdrenMii) y hacer push.

## Preguntas abiertas para Henry
- [ ] ¿Baños divididos (hombres/mujeres) o uno solo?
- [ ] ¿La historia del coma y el papá queda como capa opcional (recomendado) o es lo principal?
- [ ] ¿Por dónde seguimos: jugabilidad (B), Pesadilla (D) o rendimiento (C)?
