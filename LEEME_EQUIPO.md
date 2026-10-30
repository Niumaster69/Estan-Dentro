# Están Dentro: qué cambió (para el equipo y sus Claude)

Este archivo resume, para cualquier persona (o asistente de IA) que abra el repo, **de qué base se partió, qué se replanteó,
qué se rediseñó y qué está implementado hoy**. El detalle día por día está en `Documentacion/HECHO.md`, lo pendiente en
`Documentacion/POR_HACER.md` y las decisiones en `Documentacion/DECISIONES_E_IDEAS.md`.

- Motor: **Unity 6000.3.23f1, HDRP**. Abrir esta carpeta (`EstanDentro`) con Unity Hub.
- Rama de trabajo: `integracion/estan-dentro-game` (parte del `main` del equipo, entrega del Capítulo 1 del 13-may).
- Arte: Blender 5.2 (los `.blend` y scripts de Blender viven fuera de este repo; aquí llegan los `.fbx`).
- Archivos grandes (`.fbx`, `.png`, `.wav`, `.mp3`, `.jpg`, `.exr`…) van por **Git LFS**: instalar `git lfs` antes de clonar.

---

## 1. La base (lo que había hasta mayo)

Capítulo 1 entregado: menú, cinemática, Acto 1 en el salón 4B (cartas, casillero 142, lonchera 046, linterna, mensaje
oculto, ducto del techo), Acto 2 en los ductos y Acto 3 en las salas finales (puertas con 1705, video final, créditos).
Sistemas que funcionaban y **se conservaron**: estrés, respiración (micrófono o teclado), inventario, notas, cerraduras,
puertas, guardado por API, logros, ajustes y mando.

Problemas detectados (18-oct, `Documentacion/PLAN_REPLANTEAMIENTO.md`):
- No había amenaza: era un escape room con atmósfera.
- No había edificio: un solo salón al doble de tamaño real y escalas rotas en los modelos.
- Había 3 o 4 versiones de la historia que chocaban entre sí.
- El juego dependía de la API en el PC de Duvan.

## 2. El replanteamiento (decidido por Henry, 18 al 21-oct)

- **Premisa**: Mateo se queda dormido en clase, despierta de noche en el colegio vacío y tiene que salir. El estilo
  es *Fenómeno Siniestro* con acertijos y llaves estilo Resident Evil. La historia (accidente, coma, Pa) se descubre
  en cartas, recuerdos y voces.
- **Narrativa unificada**: ver `Documentacion/NARRATIVA_UNIFICADA.md`.
- **Se sigue en Unity HDRP**: se apunta a realismo, con calidades escalables.
- **Colegio colombiano real**: ladrillo tolete, zócalo teal, piso en ajedrez, pupitres de tubo y jacarandá.
- **Código de color**: dorado/cálido = Recuerdo (a salvo), teal = Pesadilla (peligro), rojo = Alterado (persecución).
- **Guardado local** con la nube opcional, para no depender del servidor (`Documentacion/GUARDADO_LOCAL_Y_NUBE.md`).

## 3. Lo rediseñado

- **Edificio completo** de 3 pisos, sótano y azotea, a escala real, modelado en Blender. Las aulas miden 9×7 m y los pisos
  3,2 m de alto. Se exporta por partes: `Assets/Art/Edificio/*.fbx`. La escena principal es
  `Assets/Scenes/Colegio/Colegio.unity`.
- **Capítulo 1 lineal** en el colegio nuevo: 4°B → ducto → rectoría → ducto → 4°B → pasillo del piso 3 → aula de
  música (recuerdo con Pa).
- **Viaje por ductos sin recargar el colegio** (`Scripts/Colegio/ViajeDuctos.cs`): el colegio "se duerme", se cargan
  los ductos encima y, al volver, todo sigue como lo dejaste.
- **Inventario como "Cuaderno de Mateo"** con la mochila: los objetos aparecen dibujados en crayola y hay pestaña de pistas.
- **Prólogo nuevo en el carro** con Pa, bajo la lluvia, hasta el accidente: `Assets/Scenes/Prologo` (`ConstruirPrologo.cs`).
- **Estrés como mecánica**:
  - Q = concentrarse/respirar.
  - El estrés alto rompe la mente (punto 4).
  - En los ductos, el colapso hace que Mateo se desmaye y despierte al inicio del ducto.

## 4. Lo implementado nuevo (resumen)

### La mente de Mateo: Normal / Pesadilla / Alterado (`Assets/Scripts/Mente/`)
- `EstadoMente.cs` maneja los tres estados y sus transiciones (estilo Jinx en *Arcane*). Usa un pase de pantalla completa
  de HDRP (`Resources/Mente/MenteFullscreen.shader`) con:
  - aberración, glitch, garabatos a lápiz en el borde y contorno de dibujo;
  - onda de choque y destello;
  - FOV, desenfoque y niebla.
- `ControlMente.cs` decide qué dispara cada estado:
  - un recuerdo del accidente rompe el mundo;
  - el estrés alto mantenido también lo rompe;
  - al resolver el recuerdo, todo vuelve a la normalidad.
- `MundoPesadilla.cs` cambia los materiales por su versión dañada (`Art/Edificio/Materials_Pesadilla`) y tiñe las luces.
- **El Observador** (`Observador.cs`, modelo en `Art/Observador`):
  - solo aparece en la Pesadilla, en el borde de la vista;
  - si lo miras fijo o corres cerca, te ve: pasa a Alterado y te caza;
  - si te escondes, te pierde;
  - si te alcanza, Mateo colapsa.
- **Recuerdos**: la foto de Pa en la rectoría (la llave del 4°B solo existe en la Pesadilla) y el pasillo del 4°B
  (Pesadilla hasta llegar al aula de música).
- Teclas de prueba: F6 = Pesadilla, F7 = Alterado, F8 = Normal.

### Otros sistemas nuevos
- Voces de Mateo con ElevenLabs (`Resources/Voces/`): piensa en voz alta al coger objetos, al leer notas y en los recuerdos.
- Escondites, escalada, sillas que se arrastran, cajones, casilleros con candado y clave, pata de cabra y rejillas de ducto.
- Sonidos CC0, pasos por tipo de piso y aguacero.
- **Prueba automática** de toda la ruta: abrir el ejecutable con `-autoprueba <carpeta>`. Deja capturas y `autoprueba.txt`.
- Herramientas de editor en `Assets/Editor/Colegio/`:
  - `ArreglosColegio.Batch` arma y repara la escena;
  - `CompilarColegio.CompilarBatch` hace el build.

## 5. Cómo probar

1. Instalar Git LFS y clonar. Abrir con Unity **6000.3.23f1**.
2. Escena de inicio: `Assets/Scenes/MainMenu/MainMenu.unity`. El orden de escenas en el build es
   MainMenu → Prólogo → Colegio → Ductos.
3. Build por línea de comandos:
   `Unity.exe -batchmode -quit -projectPath . -executeMethod EstanDentro.EditorColegio.CompilarColegio.CompilarBatch -salida <ruta\Juego.exe>`

## 6. Lo que sigue

Ver `Documentacion/POR_HACER.md`. Lo próximo:
- números cambiados en la Pesadilla como clave de un candado;
- animación de caminar del Observador;
- rediseñar los ductos para que sean más lógicos;
- mapa estilo RE en el cuaderno;
- guardado por puntos de control.
