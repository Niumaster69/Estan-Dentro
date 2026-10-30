# Están Dentro — HECHO

> Registro de lo terminado. Regla: cuando algo de `POR_HACER.md` se termina, **se borra de allá y se anota aquí**
> (fecha, qué, dónde quedó). Las decisiones y las ideas están en `DECISIONES_E_IDEAS.md`.
> Raíz del proyecto: `C:\Users\henry\Desktop\Universidad\Estan Dentro\` (abajo, las rutas son relativas a esa carpeta).

---

## 30-oct-2026 — Rama `mejoras/duvan-claude` (Duvan + Claude): la respiración vuelve a ser el centro
> Propuesta completa con razones: página "Están Dentro — Remaster" (Duvan la comparte). Todo pasa por el sí de Henry;
> lo que cambia el diseño se puede apagar desde el Inspector.
- [x] **Respirar con Pa** (`Scripts/Colegio/Concentrarse.cs`, reescrito; mismo nombre y tecla Q/LB): ya no es "mantener Q y el
  estrés baja solo". Es seguir el ritmo de Pa: INHALA 4 s (mantén) · SOSTÉN 4 s (mantén) · EXHALA 6 s (suelta). A tiempo baja el
  estrés; a destiempo lo sube un poco; un ciclo completo bien hecho da alivio extra y, calmado, sigue el "Revelio" de antes.
  Guía en pantalla (anillo que crece y se encoge + cuenta), voz de Pa desde el hospital (`Voz.Lugar.Lejos`) en los dos primeros
  ciclos, micrófono calibrado vale para exhalar. Contadores `CiclosBuenos/CiclosTotales` para el final.
  Voces: `Resources/Voces/Respirar/` (cortadas con ffmpeg de `Prologo/respira_conmigo_mijo_cuatro_sosten.wav`). Falta grabar "seis".
- [x] Arreglo: `Concentrarse` ponía `FactorVelocidad` en cada cuadro y pisaba el 0,55 de `SillaArrastrable`; ahora solo al cambiar.
- [x] **Escondite con aguantar la respiración** (`Colegio/Escondite.cs`, `Mente/Observador.cs`): el Observador ya no te pierde
  al instante; ronda el escondite (`rondaEscondite` 4,5 s). Con él cerca hay que mantener Q (aguantar el aire, máx. 6 s): respirar
  con él al lado o pasarse de los pulmones hace jadear a Mateo y lo delata (`Observador.Delatar`). Barra de aire en pantalla.
- [x] **Pasillo en bucle** (`Mente/BuclePasillo.cs`, colocado por `Editor/Mejoras/MejorasEscena.cs` en x 12,6 del pasillo
  norte del P3): en la Pesadilla del pasillo, al llegar a la esquina noroeste Mateo vuelve 8 m, otra vez frente al 4°B. Se rompe
  con 2 ciclos bien respirados (o a la 6.ª vuelta). `ArreglosColegio.Batch` ya llama a `MejorasEscena.Aplicar()`.
- [x] **Final del hospital** (NARRATIVA_UNIFICADA §2.3) en `RecuerdoAula.cs` (casilla `finalHospital`, encendida): después de
  "…Pa. Quiero volver." el aula se apaga en el cuarto del hospital (pitido de monitor `Resources/Sonidos/monitor_pitido.wav`), los
  cinco textos del giro, la última respiración con Pa (el monitor se calma con cada ciclo), ESTÁN DENTRO + gancho del capítulo 2
  y la pantalla **"Lo que aprendiste"** con el rango de calma (S/A/B/C) y las líneas 192 opc. 4 / 106.
- [x] Herramientas de revisión por línea de comandos (`Editor/Mejoras/MejorasRevision.cs`): volcado de puertas, escondites y
  zonas; planta del piso desde arriba; capturas de la ruta en estado normal, apagón con linterna y Pesadilla.
- [x] **Jacarandá en flor** (POR_HACER F): el follaje del modelo (color plano verde) pasa a lavanda, material propio en
  `Assets/Art/Mejoras/`. Lo aplica `MejorasEscena.Aplicar()`.
- [x] **Calidad gráfica en Ajustes > Video** (`UI/Settings.cs`, `UI/SettingsOverlay.cs`): Alta / Media / Baja = los tres niveles
  de QualitySettings (High Fidelity, Balanced, Performant). Con tarjetas de 4 GB o menos empieza en Media. Se guarda y se aplica
  en `Settings.ApplyAll()`.
- [x] **Prueba automática**: nuevo paso 9c (el bucle devuelve a Mateo 8 m, dos ciclos con Pa cuentan y bajan el estrés, después
  el bucle ya no gira) y el final del hospital con sus capturas. `Concentrarse.SimularQ` deja que el robot respire.
  Resultado 30-oct: todo OK salvo las 2 fallas viejas de la prueba (celular del prólogo y pata de cabra).
  Encontró y quedaron arreglados: la respiración se cortaba al final de cada exhalación (nunca contaba un ciclo) y el bucle
  disparaba dos veces porque Mateo tiene dos colliders.

## 22-oct-2026 (madrugada, mientras Henry dormía)
- [x] Segundo recuerdo: al salir del 4°B al pasillo del piso 3 el colegio se rompe (`Recuerdo_Pasillo4B`, `GatilloZona.cs`); el Observador ronda el pasillo largo; llegar al aula de música deshace la Pesadilla ("…Aquí no entra. Aquí no.") y empieza el recuerdo bonito con Pa.
- [x] Cada recuerdo pasa una sola vez; al resolver, el Observador se va de una (`ControlMente.Resolver`).
- [x] Voces de Mateo para las 7 frases nuevas de la mente (ElevenLabs, voz Sebastian; cortes a mano con `04_Audio/palabras_toma.js`). En `Resources/Voces/Colegio`.
- [x] Papeles que flotaban sobre las mesas (p. ej. PROP_papel.004 a 9 cm) bajan hasta su apoyo (`ArreglosColegio.PapelesApoyados`).
- [x] Ruta automática: 2 fallas, las dos de la prueba (el robot no apunta bien al celular del prólogo; la pata de cabra está en el cuarto del celador, fuera de la ruta lineal).

## 21-oct-2026 (noche) — La mente de Mateo: Normal / Pesadilla / Alterado
- [x] Transiciones con HDRP (`Scripts/Mente/EstadoMente.cs` + `Resources/Mente/MenteFullscreen.shader`): garabatos de lápiz neón que hierven con borde orgánico, el mundo dibujado a líneas (profundidad + normales del G-buffer), onda, glitch, negativo, vértigo (FOV), desenfoque, sombras teñidas, niebla, grano, sonido (zumbido, susurros, latido).
- [x] El mundo cambia: 72 materiales dañados (`Art/Edificio/Materials_Pesadilla`, de `tex_pesadilla`), luces frías y bajas (`MundoPesadilla.cs`). Objetos que solo existen en la Pesadilla (`SoloEnPesadilla`).
- [x] Qué dispara cada estado (`ControlMente.cs`): recuerdo del accidente (anclado) o estrés ≥80 % 10 s → Pesadilla; respirar o resolver → Normal. En la rectoría: leer la foto → Pesadilla → la llave del 4°B aparece en el piso junto a la rejilla caída → tomarla la resuelve.
- [x] El Observador (`02_Arte3D_Blender/scripts_blender/observador.py`, `Scripts/Mente/Observador.cs`): figura de 2,2 m, aparece lejos al borde de la vista; mirarlo ~2 s, acercarse o correr cerca → Alterado y te caza a trompicones; te pierde si te escondes, le cortas la vista 5 s o te alejas; si te alcanza, colapso.
- [x] Las manos del HUD ya no se quedan en el menú. Prueba: F6 Pesadilla, F7 Alterado, F8 normal. Fotos: `-fotosmente <carpeta>`. Ruta automática OK (3 fallas viejas de la prueba).

## 21-oct-2026 (tarde) — Voces de Mateo en el colegio
- [x] 41 frases de Mateo (voz Sebastian) en una sola toma de ElevenLabs, cortadas con los tiempos de Scribe (`04_Audio/cortes_toma.js` + `RecortarAudio.Varios`), igualadas a -18 dB, en `Resources/Voces/Colegio`. Suenan solas con el subtítulo (`ObjectiveHUD`). Frase nueva de Pa en el prólogo. Scripts: `bajar_recientes.js`, `cortes_toma.js`.

## 21-oct-2026 — Ruta del capítulo 1: 4°B → rectoría → 4°B → piso 3 → recuerdo
- [x] **Ductos de ida y vuelta sin recargar el colegio** (`ViajeDuctos.cs`): el colegio queda como lo dejaste.
- [x] **Rectoría**: se cae por la rejilla de encima de los archivadores (`ducto_rectoria.py`), llave del 4°B y foto del Día de la familia en el escritorio; se vuelve subiendo con una silla a los archivadores. El final viejo de la rectoría se quitó.
- [x] **4°B**: la puerta se abre con la llave (`LockedDoor.llaveId`). Pupitres de estudiante arrastrables (el más cercano con pintura amarilla); chorreones de pintura amarilla en el casillero (se mueven con su puerta), tarro volcado y charco.
- [x] **Piso 3**: 13 salones cerrados, barricadas de pupitres en las dos escaleras, **aula de música** abierta con el recuerdo de Pa cantando (`RecuerdoAula.cs`) y el fin del capítulo → menú.
- [x] **Menú**: Nueva partida empieza en el 4°B; botón PRÓLOGO aparte. Avisos del sistema fuera: lo que dice Mateo sale como subtítulo (y con su voz si existe el audio en Resources/Voces/Colegio).
- [x] Prueba automática de la ruta completa: 108 OK (3 fallas viejas de la prueba).

## 20-oct-2026 (noche) — Menú, mochila estilo cuaderno, manos, estrés como mecánica
- [x] **Menú principal** dentro del juego compilado (antes no estaba: "no puedo cargar el menú"); Nueva partida → prólogo sin calibrar micrófono; Continuar → colegio.
- [x] **Mochila estilo B** (cuaderno abierto: "En mis manos" / "En la mochila", letra a mano, dibujos pegados con cinta, número en lo que se junta). 6 lugares; sin bolsillo (monedas, tiza y llaves ocupan un lugar). Prototipo de estilos: `01_Documentacion/prototipo_mochila/prototipo_mochila.html`.
- [x] **Manos en pantalla** abajo a la derecha (`ManosHUD.cs`) con la barrita de pila bajo la linterna (se quitó la pila suelta). **Objetos en la mano visibles** frente a la cámara, estilo Hello Neighbor (`ManosVisibles.cs`).
- [x] **Estrés como mecánica** (`Stress/Aguante.cs`): borde rojo que late; estrés alto mucho rato baja el aguante (la vista se cierra en negro) y si llega a 0 colapsa; a oscuras sube el estrés. **Respirar = mantener Q** (baja el estrés, camina lento; calmado, además brilla lo que sirve). La ventana de respiración ya no sale sola.
- [x] Tablero de cerca con letra a mano (`textos_pared.py`), mensajes de la linterna con letra a mano, reloj nuevo a las 6:52, papelitos pegados en puertas, 3 cajones, casillero del 142 se cierra, silla suelta junto al casillero, Q solo marca lo visible, sin la acción vieja en los ductos.

## 20-oct-2026 (tarde) — Mochila con dibujos, tablero, pila de la linterna
- [x] **Mochila (I)**: Mateo se agacha y la abre; manos (2), mochila (4) y bolsillo; los objetos son dibujos a lápiz sacados de su modelo (`DibujosObjetos.cs`) y se arrastran con el mouse (a la mano, a la mochila o afuera = al piso). El cuaderno pasó a **Tab** (`MochilaOverlay.cs`). Prueba rápida de los dibujos: `ProbarDibujos.Batch`.
- [x] **Tablero** se mira de cerca aunque no haya tiza (lee la fecha y la pista del 142) y hay un pedazo de tiza en su repisa (`TableroInteractivo.cs`).
- [x] **Pila de la linterna** en pantalla (`BateriaHUD.cs`); con poca pila el estrés sube despacio (hasta 60 %).
- [x] La lata del susto se cae del casillero en vez de aparecer saltando. Quitadas las teclas de prueba del estrés en el juego (R lo ponía en 0 al forzar candados).

## 20-oct-2026 — Nivel 1 lineal, objetos a la vista, escalada al ducto
- [x] **Nivel 1 lineal** (estilo Resident Evil): 22 puertas del primer piso y el sótano cerradas con llave (sin abrir la respiración), portón principal cerrado por fuera, rejas entre las columnas del patio (`rejas_patio.py`). Revisión automática de alcance: `ArreglosColegio.RevisarAlcanceBatch` (desde los ductos solo se llega a la galería, el zaguán, la secretaría y la rectoría).
- [x] **Lo de adentro se ve**: casilleros, lonchera y contenedores muestran el objeto (modelos en `Resources/Objetos`, de `exportar_objetos.py` + `ObjetosSueltos.cs`, colocados por `BotinVisible.cs`); otra E lo coge. Los casilleros se vuelven a cerrar.
- [x] **Escalada al ducto del 4°B**: arrastrar la silla → subirse → subirse al casillero (agachado) → quitar la rejilla → entrar (`SillaArrastrable.cs`, `Escalada.cs`). Hueco real en el techo con ducto de lámina (`hueco_ducto_4b.py`). Las otras 37 rejillas ya no son interactuables.
- [x] **Cajón del escritorio del profe** que sale de verdad con la nota adentro (`Cajon.cs`, `ArreglosColegio.CajonProfe`).
- [x] Puertas con 1 m libre (`amueblar.despejar_puertas`: escritorio de la enfermería, matera, globo, cajas); escalones del escenario de 16 cm; una sola clave en la rectoría (la del candado); notas ya leídas solo se recuerdan; papelitos se recogen; misiones arriba a la izquierda (sin la estrella); voz de Mateo sin dirección; Esc en el cuaderno no abre la pausa; cámara sin ver a través de paredes (plano cercano 5 cm); lluvia lejana fuera del carro.
- [x] **Respiración, fallas graves**: calibración sin micrófono ya no se queda en negro; el game over no deja el juego bloqueado; el sonido vuelve donde iba; no se abre encima de otras ventanas.

## 19-oct-2026 (noche) — Aguacero, orden del prólogo, guía de teclas, portacandados
- [x] **Ambiente que no llegaba al juego**: los perfiles de volumen (prólogo y colegio) se guardaban vacíos; en el .exe no había niebla, exposición fija ni colores. Ahora se guardan dentro del archivo (`ConstruirPrologo`, `ConstruirColegio.PerfilColegio`).
- [x] **Aguacero en el prólogo**: cielo gris cerrado, niebla a ~110 m, doble lluvia, asfalto empapado, exposición EV 12,3. Pantallas del celular y la radio visibles.
- [x] **Rayas blancas del carro**: eran la pintura blanca de la carrocería en piezas de adentro → `Moldura_Interior` gris plata; cromos más oscuros; sonda de reflejos en la cabina.
- [x] **Orden del prólogo**: nada avanza solo; espera lonchera, cuaderno y celular; la radio al final → Pa canta → "usted canta horrible" → "¿le cambia de emisora?" → la radio dice "Buscando…" (`PantallaRadio.cs`, `pantalla_radio.py`) → choque.
- [x] **Voz de Pa** ecualizada con medición (`04_Audio/espectro_voces.js`, `EqCabina.VozPa`).
- [x] **Guía de teclas**: punto redondo que crece; tecla dibujada junto al punto con la acción (`Interactable.Accion`, `UI/GuiaTeclas.cs`); avisos de primera vez para I, Q, F, T y G; "Mueve el mouse" fijo hasta el primer movimiento.
- [x] **Portacandado** en los 17 casilleros con candado y **cadena** de la rectoría colgando en U (`props_extra.py`, `amueblar.py`, `rehacer_candados.py`; respaldo `EstanDentro_Edificio_antes_portacandado.blend`).

## 19-oct-2026 (tarde) — Prólogo realista: calle, vehículos, Mateo bien sentado
- [x] Avenida nueva (`02_Arte3D_Blender/scripts_blender/prologo_calle.py`): texturas Poly Haven, casas con tejado, locales con persiana/toldo/aviso, edificios de 6-8 pisos, lotes, rejas, balcones, postes con cables, luminarias, árboles, paraderos SITP, charcos, cruces con calle lateral, cebra y semáforos. Materiales en `ConstruirPrologo.MaterialesCalle`. El tramo siguiente ya no aparece tarde (`finCalle`).
- [x] Vehículos de Sketchfab (CC-BY, `prologo_vehiculos.py`): camión Chevrolet Jumbo, bus SITP, Logan, Spark, Aveo y taxi (Spark amarillo). Van dentro de un contenedor (antes quedaban mirando al piso).
- [x] Mateo (Blender, con respaldos `Prologo_Auto_antes_*.blend`): 92 %, recostado, rodillas y pies doblados, torso comprimido, sin cabeza (primera persona) y cuello tapado, subido 5 cm. Ojos a 1,21 m; la cabeza gira desde el cuello. Palanca en D.
- [x] Celular con pantalla de bloqueo (`pantalla_celular.py`), se ilumina al tocarlo. Lonchera con candadito (`prologo_candadito.py`) que se levanta para verlo.
- [x] Cada cosa se toca una vez y solo en silencio (no se cruzan conversaciones); el canto de Pa no se monta con otras voces.
- [x] Colegio: mochila junto a los pies de Mateo; 94 sillas quitadas para dejar pasillos (fijas); tablero más claro con tiza; casilleros con chirrido; notas leídas en voz alta (la carta de Pa como recuerdo).

## 19-oct-2026 (mañana) — Prólogo: carril, lluvia, plumillas, música y voces
- [x] El Corolla va por su carril: avenida de 2 carriles por sentido, doble línea amarilla al centro (`ConstruirPrologo.cs`). El tráfico y el camión van por el carril contrario.
- [x] Agua en el parabrisas por fuera (`Scripts/Prologo/AguaParabrisas.cs`): gotas que caen, las grandes se escurren, las plumillas (con brazo y eje) suben por el vidrio y barren. Siguen hasta el choque. Ya no llueve dentro de la cabina.
- [x] El vidrio del carro ya no refleja el cielo entero (parecía una pared rosada).
- [x] Pausas entre frases más cortas; si hay silencio, Pa saca conversación (desayuno, estadio, cicla, descanso).
- [x] Radio prendida desde el comienzo: "La cumbia pasando" (Chogo El Bandeño, CC BY-NC-SA) con filtro de parlante de carro. Al tocarla salta al pedazo "…el cantar de esta trompeta que cuando no ríe, llora" y Pa lo canta encima (voz del cantante pasada a la de Pa en ElevenLabs).
- [x] Después del choque: silencio, "Lost Dream Loop" (SergeQuadrado, CC BY-NC) con el título ESTÁN DENTRO; luego el monitor, la voz de Pa y la campana. Créditos en `04_Audio/CREDITOS_SONIDOS.md`.
- [x] Voces ElevenLabs (llave en `D:\elevenlabs`): Pa = Fernando, Mateo = Sebastian. 14 frases de Pa y 12 de Mateo, 2 tomas cada una en `04_Audio/voces/prologo`. Las elegidas van a `Assets/Resources/Voces/Prologo` con `04_Audio/poner_voces.js` (`tomas.json` dice qué toma va con qué frase). `Scripts/Audio/Voz.cs` las suena con ecualización de carro, pensamiento o lejos.
- [x] Herramientas en `04_Audio`: `transcribir.js` (letra con tiempos), `aislar_voz.js`, `cambiar_voz.js`, `voz_elevenlabs.js`; recorte de audio con Unity (`Editor/Colegio/RecortarAudio.cs`). Cuenta gratis: las voces de la biblioteca solo se usan desde la página; se bajan por la API del historial.

## 18-oct-2026 — Organización y repositorios
- [x] Carpetas creadas: `01_Documentacion`, `02_Arte3D_Blender` (edificio, props, personajes, texturas, exportados_fbx, scripts_blender), `03_Juego_Unity`, `04_Audio`, `05_Cinematicas`, `06_Builds`, `90_Repos_Referencia`. Explicadas en `LEEME.md`.
- [x] Clonados en `90_Repos_Referencia\`: `Niumaster69_Estan-Dentro` (historial completo del juego), `AdrenMii_Estan-Dentro_Game` (el más nuevo, 29-may), y dos repos de Machine Learning que NO son el juego.
- [x] Proyecto de trabajo: `03_Juego_Unity\EstanDentro`. Rama local `integracion/estan-dentro-game`, commit `71a113f`. Une el historial de Niumaster69 con los cambios del 29-may. **Sin push**: faltan las llaves.
- [x] El `.mov` de 1,6 GB (`Assets/Audio/Cinematics/Completo musica saundtrak corto.mov`) quedó fuera de git en `.git/info/exclude`.
- [x] Diagnóstico inicial en `01_Documentacion\PLAN_REPLANTEAMIENTO.md`.

## 18-oct — Herramientas
- [x] Blender 5.2 + complemento **Blender MCP**, tomado de github.com/ahujasid/blender-mcp. Se revisó antes de instalarlo: escucha solo en localhost:9876 y la telemetría viene apagada.
- [x] `uv` instalado en `C:\Users\henry\.local\bin`. Servidor `blender` registrado en Claude Code (config de usuario).
- [x] Conector propio: `02_Arte3D_Blender\scripts_blender\blender_cmd.ps1`. Usa `-Code` o `-File` para ejecutar en el Blender abierto y `-Shot` para tomar captura del visor.
- [x] Al abrir Blender, el servidor arranca solo con `scripts_blender\iniciar_servidor.py`.

## 18-oct — Edificio (Blender)
Archivo: `02_Arte3D_Blender\edificio\EstanDentro_Edificio.blend`. Generador: `scripts_blender\edificio_lib.py`, que se reconstruye entero con `construir_todo()`.
- [x] **Planta**: anillo de 48 × 36 m alrededor de un patio de 28 × 16 m. Galerías de 3 m. Altura de piso a piso 3,6 m, losas de 0,2 m y muros de 0,2 m.
- [x] **Niveles**: sótano (S, −3,6), P1 (0), P2 (3,6), P3 (7,2) y azotea (10,8). Las salas de cada piso están en `PISOS` dentro de `edificio_lib.py`; la lista legible está en `DECISIONES_E_IDEAS.md`.
- [x] **Escaleras en U**: tramos de 1,8 m, 12 contrahuellas de 15 cm, huella de 28 cm y losa inclinada (más de 3 m de altura libre).
  - Escalera A (NW): de P1 a la azotea.
  - Escalera B (SE): de P1 a P3.
  - Escalera de servicio (SW): del sótano a P1.
  - Las tres tienen caja cerrada (sin huecos en el descanso), cuarto bajo la escalera en la base, barandas en los huecos y una ventana solo a la altura del descanso.
- [x] **Sótano**: calderas, archivo muerto, depósito, cuarto de bombas, túnel de servicio y SALIDA REAL al final del túnel.
- [x] **Azotea**: pretiles, caseta de la Escalera A y tanques de agua.
- [x] **Sin solapes**: cada pieza nueva se recorta contra las anteriores (`Malla.caja`). Detector exacto `revisar_solapes()`, que compara la forma real de las caras: **0 solapes**.
- [x] **Solo caras visibles** (`crear_todo()`): se quitan las caras tapadas, unas 24 mil (−30 %).
- [x] **Estructura**: columnas de 40 × 40 dentro de la galería al ras del patio, vigas de borde de 35 cm bajo cada losa, losas al ras de la fachada y antepechos y pretiles al ras del borde.
- [x] **Acabados por cara de muro** según la sala de enfrente (`ACABADOS`, `acabado_y_piso()`). Ladrillo afuera; azulejo teal hasta 1,1 m + crema en pasillos; zócalo verde aceite en aulas; madera en biblioteca, auditorio y música; cal + terracota en el ala antigua; azulejo blanco en baños, laboratorios, cocina y enfermería; concreto en servicios y sótano.
- [x] **Pisos por sala**: ajedrez en pasillos y cafetería; granito en aulas; madera; barro colonial; baldosa de baño; concreto en el sótano. La azotea tiene acabado propio.
- [x] **Carpintería**: ventanas con marco de madera, parteluces, travesaño, vidrio y alféizar, y rejas en el P1 hacia la calle. Puertas con marco, umbral y hojas como objetos separados `PUERTA_<nivel>_<n>` con origen en la bisagra (`bisagra_sentido`).
- [x] **Entorno**: reja del colegio (muro bajo de ladrillo, pilares cada 3 m, barrotes) y portón doble. Plazoleta. El terreno no pasa bajo el edificio (causaba parpadeo).
- [x] Etiquetas de salas en las colecciones `Etiquetas_*`, ocultas en la vista y en el render.

## 18-oct — Materiales
- [x] 22 texturas CC0 de Poly Haven en 2K (color, normal, rugosidad) en `02_Arte3D_Blender\texturas\polyhaven\`, con su lista en `texturas.json`.
- [x] `scripts_blender\materiales_lib.py`:
  - `PBR` con textura y tinte para el Recuerdo y para la Pesadilla.
  - `SIMPLES`: cerámica, plástico, vidrio, tela, papel, emisivos.
  - `aplicar_estado('recuerdo'|'pesadilla')` y `rehacer_todos()`.
- [x] **Escala real medida** (`ESCALA_REAL`): ladrillo de 40 × 13 a tolete de 24,5 × 6,5 cm; azulejo de pasillo girado a horizontal; ajedrez a 35 cm; azulejo de baño a 30 cm.
- [x] **Anti-repetición**: en yeso, cal y concreto se hace doble muestreo rotado y desplazado con máscara de ruido de unos 3 m; en todos los materiales hay variación macro de tono y rugosidad (15–30 m).
- [x] UVs en metros reales en todo el edificio.
- [x] Cada material guarda en propiedades personalizadas lo que necesita Unity: `tex_recuerdo`, `tex_pesadilla`, tintes, `tam_real_m`, `rotacion_grados` y `antirepeticion`.

## 18-oct — Patio
- [x] **Jacarandá** de Poly Haven, optimizado para juego por `scripts_blender\jacaranda_juego.py`: tronco LOD1 + ramas simplificadas + 319 racimos de hojas en tarjetas, en total 147 k triángulos (el original tiene 1,6 M).
  - Archivo: `02_Arte3D_Blender\props\polyhaven\jacaranda_tree\jacaranda_juego.blend`.
  - Plantado en (18, 18), con la copa recortada al contorno del patio para que no entre a las galerías.
- [x] **Pila octagonal** en (31, 18): escalón, bordillo de granito, azulejo teal, pedestal y taza.
- [x] **Golosa** pintada del 1 al 10 + CIELO en (28,6–29,2, 20,8–25,2), en la colección `Decals_pintura`: en Unity se convierte en decals.
- [x] **Tarima de izada de bandera** (sur del patio) con dos escalones y el asta.
- [x] **Jardinera octagonal con banca** alrededor del jacarandá.
- [x] **4 rejillas de desagüe** y **tapa de alcantarilla** en (34,2, 15,2), marcada `interactuable` como ruta al sótano.

## 18-oct — Estilo visual
- [x] Renders de referencia en `01_Documentacion\referencias_estilo\`: A colegio pintado, A2 estado alterado, B claustro colonial, C ladrillo bogotano, D brutalista, F1 fusión Recuerdo y F2 fusión Pesadilla. Generador: `scripts_blender\estilos_muestra.py`.
- [x] Henry eligió A2, B, F1 y F2.
- [x] Revisados los 4 tableros de Pinterest de Henry (descargados en el scratchpad de la sesión). El análisis está en `DECISIONES_E_IDEAS.md`.
- [x] Vistas del avance en `01_Documentacion\avances_edificio\` (01–15).
- [x] Prueba con Cycles: la luz rebotada hace la mayor diferencia. HDRP con GI o ray tracing logra lo mismo.

## 18-oct — Escaleras rehechas (según los pines de Henry)
- [x] Tramos macizos con losa inclinada lisa por debajo. **Ojo central abierto de 30 cm**: carriles `E_C1 = (29.1, 30.95)` y `E_C2 = (31.25, 33.0)`, sin muro central.
- [x] Barandas metálicas: un barrote apoyado en el centro de **cada peldaño**, pasamanos paralelo a la pendiente y vuelta en el descanso. Solo hay barandas en el borde del ojo y del hueco del piso de arriba, no contra muros.
- [x] Ventana grande en el descanso (1,6 m).

## 18-oct — Exterior y salida por el sótano
- [x] **Cerramiento completo**: `REJA = (-6, -8, 54, 54)`. Tiene muro bajo de ladrillo, pilares cada unos 3 m y barrotes verdes. Portón principal (x 21–27, frente) y **portón trasero encadenado** (x 22–26), con la cadena como interactuable `Cadena_porton_trasero`.
- [x] **Salida del sótano**: el túnel llega a la fachada norte y sube por una escalera empinada (18 × 20 cm) a una **entrada de sótano con puertas diagonales** estilo *IT*, pegada al edificio (`bulkhead()`, x 4–7, y 36,1–41). Las puertas y el candado son interactuables. La caseta suelta que había antes se eliminó.
- [x] **Cuarto de herramientas del celador** (38–45 × 45–50): ladrillo, techo de lámina, puerta y ventana alta.
- [x] **Cuartos detrás de las escaleras**: cuarto del celador, bombillos y fusibles, y bodegas (`CUARTOS_ESCALERA`).
- [x] `revisar_solapes()` = 0.

## 18-oct — Muebles (primera pasada)
- [x] `scripts_blendermueblar.py` (clase `Sala`, una función por tipo de sala y `amueblar_nivel()`). Hay 1.417 objetos en `Muebles_S/P1/P2/P3`.
  - Aulas: tablero, reloj, crucifijo, escritorio y silla del profesor, unos 30 pupitres, cartelera, casilleros, estante con libros, papelera, 4 fluorescentes y ventilador.
  - Salón 4°B: lonchera (candado 046), cartas 1 y 2, linterna y casillero (candado 142), marcados como interactuables.
  - Pasillos: casilleros dobles, carteleras con banca, fluorescentes cada 4 m, cámaras, extintores, letreros de salida y bebedero.
  - Baños, biblioteca, laboratorios, cómputo, música, capilla, rectoría, secretaría, portería, enfermería, cafetería, cocina, auditorio, sala de profesores, depósito de pupitres, aseo, archivo, cuarto eléctrico y sótano (calderas, estanterías, cajas, lámparas de jaula).
- [x] Renders en `01_Documentacionvances_edificio`–`23`.
- [x] Corregido (captura de Henry): escritorio y silla del profesor estaban al revés. Ahora los cajones van del lado del profesor y el panel liso hacia los alumnos; la silla mira al salón. También en rectoría y secretaría.
- [x] Pinterest completo: se bajaron con Chrome los pines que faltaban (44 de *Spot estilo 2* y 50 de *Elementos*).

## 18-oct — Correcciones por las capturas de Henry (espacio del jugador)
- [x] Pupitre: el espaldar de madera ahora va **por delante** de los tubos, con la misma inclinación.
- [x] Error del bisel: las caras biseladas quedaban con el primer material de la pieza (asientos grises). Corregido en `Pieza.caja`; se regeneraron los 51 prototipos.
- [x] **Auditorio a lo largo**: escenario en el extremo este (x 33,5–37,9), filas mirando al escenario con pasillo central de 1,2 m y pasillos laterales de 0,6 m. Quedan 3 m libres atrás y 3,5 m frente al escenario. Las puertas se movieron a x 11,6 y x 31.
- [x] **Aulas con pasillos**: los pupitres van cada 1,15 m (dejan unos 0,6 m libres; el jugador mide 0,6 m).
- [x] **Cuartos de escalera amueblados**:
  - Celador: **tablero de llaves** junto a la puerta (`tablero_llaves`, interactuable), escritorio, silla, radio y archivador.
  - Bombillos y fusibles: estanterías y caja de fusibles interactuable.
  - Bodegas: estanterías, cajas y balde.
- [x] `contra_pared` revisa puertas y ventanas y corre el mueble hasta 2 m. Lo que no cabe queda en `AVISOS`; por ahora solo una estantería del archivo de notas.
- [x] Ventanas de baños subidas a 2,05–2,75 m para que no choquen con los cubículos.
- [x] Renders 24–26 en `avances_edificio`.

## 18-oct — Segunda ronda de salas, barrio y luces
- [x] `props_extra.py`: 39 objetos nuevos. TV de tubo con su mueble, escobas, traperos, galones, estantes de químicos, lavadero, carrito de aseo, bolsas de basura, canecas, ollas, estantes de cocina, lavaplatos industrial, atril de suero, silla de ruedas, báscula, afiches, gabinete de medicinas, cortinas de camilla, máquina de escribir, sillas de espera, bandera, globo, fichero, estanterías de biblioteca (doble cara y de pared), esqueleto, microscopios, campana extractora, ducha de emergencia, vitrina de trofeos, sofá, mesa de centro, papeles regados y cajas de archivo.
- [x] Salas rehechas:
  - **Química**: mesones contra la ventana, campana, ducha de emergencia y tabla periódica.
  - **Biología**: islas con microscopios, esqueleto y vitrinas.
  - **Cómputo**: TV de tubo.
  - **Rectoría**: escritorio grande, bandera, trofeos y zona de visitas.
  - **Secretaría**: 2 puestos con máquina de escribir, archivadores y sala de espera.
  - **Vestíbulo**: portería, sillas de espera, trofeos y bandera.
  - **Enfermería**: camillas con cortinas (una corrida, con silueta), suero, silla de ruedas, báscula y gabinete.
  - **Cafetería**: bancas a los lados de las mesas; estaban debajo.
  - **Cocina**: isla, estufas, lavaplatos industrial, estantes con ollas y canecas.
  - **Aseo**: lavadero, químicos, escobas, carrito, galones y bolsas.
  - **Archivo**: archivadores en fila, cajas y papeles.
  - **Biblioteca**: hileras de estanterías de madera, mesas de lectura con lámpara, mostrador, fichero y globo.
- [x] **Barrio** (`barrio_lib.py`): calles, andenes, casas de 2 a 4 pisos con rejas, balcones y tanques, bloques de apartamentos detrás, postes, ciudad lejana de bajo detalle (170 edificios) y cerros. Cielo HDRI `kloppenheim_06_puresky` (tarde) y `kloppenheim_02_puresky` (noche, para la Pesadilla). Se descartó `construction_yard`: lo cercano salía gigante.
- [x] **Luces del modo normal**: 179 luces reales, una por fluorescente y por lámpara de jaula (`luces_interiores()`).
- [x] Cada objeto tiene su `funcion` (contenedor, recogible, empujable, escondite o decorativo).
- [x] Fotos nuevas en `avances_edificio\salones\RESUMEN_salones_1-4.jpg` (se reemplazaron las viejas).

## 18-oct — Tercera ronda (capturas de Henry)
- [x] Verificadores automáticos `revisar_muebles()` (objetos que se atraviesan) y `revisar_apoyos()` (objetos en el aire). Resultado final: **0 y 0**.
- [x] `sobre()`: pone objetos encima de muebles usando la orientación real y la altura exacta de la tapa (con un rayo).
- [x] Mesón de laboratorio con grifo de cuello de ganso que apunta a la poceta. Química con **cristalería** (erlenmeyer, beakers, probetas, gradillas).
- [x] Cafetería: bancas paralelas a las mesas, **barra de servicio** con vitrina, **2 máquinas dispensadoras** y dispensador de agua.
- [x] Cocina: isla con ollas encima, estufas y lavaplatos industrial.
- [x] Sala de profesores tipo **centro de mando**: mesa redonda, 8 sillas mirando al centro, **TV grande** en la pared, sofá, cafetera, dispensador y casilleros.
- [x] Biblioteca reorganizada en hileras con zona de lectura. Rectoría sin muebles encimados. **Escaleras en la tarima** del auditorio.
- [x] Aseo convertido en **almacén**: inodoros y lavamanos de repuesto, espejos, rollos de papel y estanterías.
- [x] Mochilas al lado de los pupitres (no dentro) y loncheras sobre la tabla.
- [x] Reparado `amueblar.py`: tenía tildes dañadas que dejaban salas sin amueblar.
- [x] Puertas del **exterior** con hoja: portón principal y cuarto de herramientas (`carpinteria('Entorno')`). En total hay 57 hojas de puerta.

## 18-oct — Rejillas y escritorios
- [x] **38 rejillas de ventilación** en el techo: una por sala, en la esquina más lejana a la puerta, más 2 por piso en los pasillos. Son objetos `REJILLA_<nivel>_<n>` con lamas, tornillos y el vacío negro detrás, `funcion = 'ducto'`.
- [x] **Red de ductos**: cada rejilla tiene `ducto_salida`, que es la rejilla vecina del mismo piso. 6 tienen además `ducto_salida_alterna`, una salida "imposible" a otro piso (Fenómeno Siniestro). La red completa está en `scene['red_ductos']` (JSON).
- [x] El escritorio del profesor va del lado del tablero **más lejos de la puerta**. Verificado: ningún escritorio queda a menos de 1,6 m de una puerta.
- [x] Escritorios, mesas y archivadores quedaron marcados `bloquea_puerta`: se pueden arrastrar para bloquear una entrada (lento y ruidoso). Los que también se registran llevan `tambien_contenedor`.

## 18-oct — Correcciones (cafetería, secretaría, aseo, puertas)
- [x] Cafetería: las máquinas de **snacks** y **gaseosas** y el dispensador de agua van contra la pared de la entrada, con 1,2 m libres al frente. Hay 2 filas de mesas con pasillo. Las máquinas tienen `funcion='dispensador'` y `dispensa` = snack, gaseosa o agua.
- [x] Secretaría: zona de espera amable, con sillas contra la pared mirando a los escritorios, 2 mesas de centro, materas, dispensador de agua, cartelera, reloj y cuadro. Sin papeles regados (modo normal).
- [x] Aseo: almacén ordenado con 3 estibas en fila central (inodoros, lavamanos y rollos alineados), espejos recostados contra la pared y galones en fila.
- [x] **Puertas a medida**: holgura de 3 mm a los lados, entre hojas y arriba, y 5 mm sobre el umbral. Antes eran 2 cm.
- [x] Sala de profesores: las sillas ya miran hacia la mesa.
- [x] `limpiar_choques()`: si un objeto pequeño y movible choca con un mueble, se quita el pequeño. Verificación: 0 choques, 0 en el aire, 0 caras solapadas.

## 19-20-oct (madrugada) — Prólogo en el carro jugable en Unity
- [x] **Escena `Prologo`** (primera del juego): `Assets/Scenes/Prologo/Prologo.unity`, armada por `Editor/Colegio/ConstruirPrologo.cs` (menú Estan Dentro → 7). Control: `Scripts/Prologo/ControlPrologo.cs`.
- [x] Carro real: Toyota Corolla 2014 con interior (Sketchfab, CC-BY). Pa (Sketchfab) manejando con la pose **Driving de Mixamo**, manos sobre el aro a las 10 y las 2, sin deformar ni atravesar el timón (medido sobre la piel). Mateo con cuerpo (Sketchfab), cabeza oculta para la cámara. Créditos: `CREDITOS_MODELOS.md`.
- [x] Guion completo: mirar sin caminar, 4 cosas para tocar (cuaderno 142, lonchera 046 roja, radio con Pa cantando, celular «¡Feliz cumple, Mendoza!»), respuesta 1/2, presagio (cambiar la canción), **choque natural**: el carro no se detiene, hay tráfico en sentido contrario, un camión rojo invade el carril, Mateo levanta la vista y grita «¡PA!», frenazo, golpe → negro → monitor → voz → campana → despierta en el colegio.
- [x] Sonidos del prólogo: motor, lluvia en el techo, limpiabrisas, radio, choque, monitor, timbre (Freesound CC0). Lluvia con partículas. Pitido de oído al despertar y zumbido de tubos en el colegio.
- [x] Prueba automática: prólogo completo OK + colegio: 91 OK, 2 fallas pequeñas de antes.

## 19-oct (noche, 8) — Sonidos de verdad (CC0) y pasos
- [x] Con el sí de Henry: 63 opciones bajadas de Freesound (solo CC0) + 3 paquetes CC0 de Kenney → 44 sonidos elegidos y preparados (`04_Audio/buscar_freesound.py`, `04_Audio/preparar_sonidos.py`) en `Assets/Resources/SonidosJuego/`. Créditos: `04_Audio/CREDITOS_SONIDOS.md`.
- [x] Pasos según el piso y respiración que sube con el estrés (`Scripts/Colegio/CuerpoMateo.cs`). Correr hace ruido.
- [x] Papel en notas y recuerdos, monedas, pestillo, cuaderno, tiza, linterna, candado (no abre / forzar / cae), 4 sustos y lluvia de fondo. Porqué de cada uno: `SONIDO_DISENO.md`.
- [x] Prueba automática: 83 OK, 2 fallas pequeñas de antes.

## 19-oct (noche, 7) — Recuerdos y sustos en casilleros, sonido, puntero, números del candado, prólogo empezado
- [x] **10 recuerdos** en casilleros (foto del 4°B, lista de asistencia, boleto de bus, tarjeta, dibujo, nota de mamá, carné, cuaderno de un compañero, papelito, recorte de periódico). Van a PISTAS y se leen al abrirlos. Textos: `Scripts/Colegio/Recuerdos.cs`. Reparto: `amueblar.botin_casilleros`.
- [x] **6 sustos** en casilleros (portazo, cae, celular, susurro), cada uno con entrada, durante y salida y una sola vez. `Scripts/Colegio/Sustos.cs`.
- [x] **Sonido, primera pasada** (`01_Documentacion/SONIDO_DISENO.md`): fuera el pizarrón que sonaba en todo el colegio; los clips largos se cortan con fundido (`Scripts/Audio/SonidoCorto.cs`); 270 campos vacíos conectados (candados, puertas, despertar, cajones, objetos). `Editor/Colegio/SonidosColegio.cs`.
- [x] **Puntero**: solo se ve cuando el jugador tiene el control (no en menús, pausa, despertar ni mirando un candado). `InteractionSystem.cs`.
- [x] **Números del candado**: solo el de enfrente y del ancho de la rueda; ya no salen el de arriba ni el de abajo.
- [x] **Papeles**: la prueba revisa que ninguno tenga física ni se mueva solo (0 de 58 se movieron).
- [x] **Prólogo (empezado)**: carro por dentro, Pa al volante, cuaderno, lonchera, celular y radio, y una avenida con postes. `02_Arte3D_Blender/scripts_blender/prologo_lib.py` → `02_Arte3D_Blender/prologo/Prologo_Auto.blend` → `Assets/Art/Prologo/*.fbx`. Sonidos provisionales sintetizados en `Assets/Resources/Prologo/` (`04_Audio/sintesis_prologo.py`).
- [x] Prueba automática: 81 OK, 2 fallas pequeñas (pata de cabra difícil de alcanzar; un papel a 9 cm del apoyo).

## 19-oct (noche, 6) — Candados con clave, pata de cabra, dibujos en crayola y arreglos
**Prueba automática: 74 pasos OK, 1 falla menor** (la mira de la prueba roza el escritorio donde está la pata de cabra).
- [x] **16 casilleros con candado** (de latón, 3 ruedas), cada uno con **clave propia** y un **papelito con la pista** a pocos metros:
  - Textos como "Mi clave: 6-3-4. Que no se me olvide otra vez. — Valen" (`amueblar.candados_casilleros`).
  - Se abren con la cámara sobre el candado.
- [x] **Pata de cabra** sobre el escritorio del cuarto del celador (P1):
  - **Ocupa las dos manos** y no cabe en la mochila; al tomarla, lo que tenías en las manos pasa solo a la mochila.
  - Con la cámara sobre un candado, **[R] lo fuerza**: salta sin la clave, suena fuerte, sube el estrés y deja un **ruido** (`Ruido.cs`) para el monstruo del modo normal.
- [x] **Puertas de casillero corregidas:** giraban sobre el eje "arriba" del modelo, que viene acostado de Blender, y **se metían en el piso** (Henry: "desaparece la puerta"). Ahora giran sobre la vertical y la prueba comprueba que queden de pie. Las animaciones originales (prefabs) no se tocaron.
- [x] **Números del candado pegados a las ruedas** (un lienzo en el mundo por rueda), no flotando en la pantalla. La rueda elegida va en ámbar.
- [x] **Dibujos del cuaderno en crayola:**
  - Color de cada material, con trazos diagonales y huecos de papel, más las líneas a lápiz (`dibujos_objetos.py`).
  - Linterna amarilla y pilas cobre.

## 19-oct — Casilleros que se abren, pilas, tiza, monedas y candados en primera persona
**Prueba automática: 0 fallas en 57 pasos.** Capturas en `avances_edificio\capitulo1_prueba\` (`inspeccion_candado_*`, `marca_tiza`).
- [x] **Casilleros con puerta de verdad:**
  - Los FBX de Henry ya traían la puerta aparte; ahora se separa al importar (`props_lib._partir_puertas`).
  - La puerta queda como pieza hija con su bisagra.
  - En Unity, `Casillero.cs` la gira 105° con sonido y queda **entreabierta**.
- [x] **Botín en ~100 casilleros** (`amueblar.botin_casilleros`): 29 monedas, 20 pilas, 21 tizas, 29 vacíos. 24 dobles quedan trabados y sirven de **escondite**.
- [x] **Pilas:** la linterna dura 4 min encendida, titila al final y **Mateo cambia las pilas solo** si tiene (`Flashlight.cs`).
- [x] **Tiza:** con **T** se marca una X en la pared o el piso que se mira; cada tiza da 6 marcas (`MarcaTiza.cs`).
- [x] **Monedas, pilas y tiza se apilan** en un solo lugar (×3).
- [x] **Candados en primera persona** (Henry: "acercamiento de cámara, a nuestro modo", `InspeccionCandado.cs`):
  - La cámara se acerca al candado 3D real y el mundo no se pausa.
  - **Las ruedas giran de verdad** (36° por número; piezas aparte en Blender) y los números se ven sobre cada rueda.
  - Al acertar, el candado cae frente a ti, la cámara vuelve y se abre lo de adentro. Al fallar, se sacude y dice "No abre".
  - La pantalla clásica queda solo para cerraduras sin candado 3D.
- [x] **Bug grave corregido:**
  - Al armar la escena se tomaba el jugador de la escena VIEJA, así que **Concentrarse (Q), la tiza y el despertar corto nunca estaban en el juego**. Ahora sí.
  - Los textos de las paredes también se aplicaban a veces a la escena vieja.
- [x] Monstruo: en **modo feria no persigue**; solo presencias guionizadas (silueta, golpes en ductos). Persecución solo en modo normal.

## 19-oct (noche, 4) — El Cuaderno de Mateo: inventario con manos y mochila, objetos dibujados
**Prueba automática: 0 fallas en 49 pasos.** Capturas: `tarjeta_destornillador`, `cuaderno_objetos`, `cuaderno_pistas`.
- [x] **Estilo elegido por Henry:** tarjeta como cuaderno (sin foto ni impresora para objetos comunes; eso queda solo para el celular) e inventario como cuaderno.
- [x] **`InventoryOverlay.cs` reescrito como el Cuaderno de Mateo** (dos hojas rayadas con margen rojo):
  - Pestañas OBJETOS / PISTAS / OBJETIVOS.
  - En Objetos: EN LAS MANOS 2 / EN LA MOCHILA 4 / EN EL BOLSILLO, y a la derecha el dibujo, qué es, para qué sirve y dónde está.
  - Al recoger algo se abre en esa hoja con el sello **NUEVO** y "[E] seguir".
- [x] **Manos y mochila sin enredos** (`Inventory.cs`):
  - Lo recogido va a una mano libre; si no hay, a la mochila.
  - Cuando algo hace falta, Mateo lo **saca solo** (`Usar`): la rejilla saca el destornillador y F saca la linterna, guardando lo menos usado si las manos están llenas.
  - En el cuaderno: Espacio pasa de mano a mochila; G suelta.
  - Si guardas la linterna prendida, se apaga.
- [x] **Objetos dibujados a lápiz:**
  - `scripts_blender/dibujos_objetos.py` usa Line Art de Blender 5.2 (Freestyle ya no existe) y saca un PNG por objeto a `Assets/Resources/Dibujos/`.
  - Hechos: linterna, destornillador, mochila, pilas, llaves, moneda, radio, Brick Game, galón y lonchera.
  - Modelos nuevos en `props_extra.py`: destornillador y moneda.
- [x] Propuesta de objetos y casilleros: `OBJETOS_Y_CASILLEROS.md`.

## 19-oct (noche, 3) — Textos nuevos, puertas y ambientación
- [x] **Textos de la narrativa unificada** (coma + Pa vivo) en el juego: cuaderno del profe, carta de Pa, nota para Pa, hoja temblorosa, escritos en la pared, salida de los ductos, final provisional y Game Over. Todos en un solo archivo: `Editor/Colegio/TextosCapitulo1.cs`.
- [x] **Las puertas se abren lejos del jugador** (`LockedDoor.abrirLejosDelJugador`), también después de abrir un candado.
- [x] **Ambientación** (Henry: "mucho contraste, poca ambientación"):
  - Luz de rebote suave bajo cada tubo.
  - Niebla volumétrica más densa.
  - Etalonaje: tonemapping neutro, menos contraste, sombras levantadas y rojizas, oclusión ambiental, bloom leve, viñeta y grano de película.
  - Capturas en `avances_edificio\unity\`.
- [x] Muestrario de estilos de tarjeta e inventario: https://claude.ai/artifact/Hxqa3NsNedTqFMop7j16uf

## 19-oct — Guardado local (sin servidor), listo para nube
- [x] El servidor de Duvan (`estandentro.somee.com`) ya no existe. Ahora **todo se guarda en el PC**:
  - `AlmacenLocal.cs` guarda las mismas tablas: Jugadores, Partidas, Logros, LogroXPartida.
  - `ApiClient.cs` usa la nube solo si `StreamingAssets/nube.json` tiene una URL.
  - Guía: `GUARDADO_LOCAL_Y_NUBE.md`.
- [x] Se crea el perfil local (también si el guardado en el PC era del servidor viejo) y la partida aunque se entre sin menú.
- [x] Prueba automática: **0 fallas en 47 pasos**, con 2 logros guardados.

## 19-oct — Inventario de toda la partida, manos y mochila, tarjeta de objeto
**Prueba automática: 0 fallas en 43 pasos.** Capturas en `scratchpad` (las importantes se copiaron a `avances_edificio\capitulo1_prueba`).
- [x] **Un solo inventario para toda la partida** (`Inventory.cs`):
  - Sobrevive al cambio de escena; antes se perdía todo al entrar y salir de los ductos, y en los ductos ni existía.
  - Se crea solo si falta y se vacía con `NuevaPartida()` (partida nueva, reintentar tras Game Over, fin del capítulo).
- [x] **Capacidad** (regla de Henry):
  - Sin mochila, **2 objetos**, uno en cada mano. Con la mochila de Mateo, **6**.
  - Monedas, llaves, notas y fotos van al bolsillo y no ocupan lugar.
  - Si no cabe, el objeto se queda donde estaba y Mateo lo dice. Lo de un candado ya abierto se toma volviendo a mirarlo.
  - **G suelta** en el piso el último objeto recogido.
  - La pestaña del inventario muestra "MANOS 1/2" o "MOCHILA 3/6".
- [x] **La mochila de Mateo** está en el piso junto a su pupitre en el 4°B (`amueblar._salon_inicio`, `MochilaEquipable.cs`).
- [x] **Tarjeta de objeto** (Henry: "recoge algo, sale una notificación corta y no sabe qué pasó"):
  - La primera vez que se recoge algo, el juego se detiene y muestra el objeto en 3D girando, **qué es**, **para qué sirve**, su tecla y dónde quedó ("Lo llevas en la mano · manos 1/2").
  - Se cierra con E (o con I para ver el inventario).
  - Espera a que se cierre la nota o el candado.
  - Textos en `CatalogoObjetos.cs`; vista en `ItemPreviewOverlay.cs` (reescrito, con su propia luz y exposición).
  - Se quitaron los avisos cortos que repetían lo mismo.

## 19-oct (noche) — Capítulo 1 completo de noche, candados distintos y base arreglada
**Prueba automática: 0 fallas en 30 pasos** (de despertar a final). Capturas e informe en `avances_edificio\capitulo1_prueba\`.
- [x] **Noche** (decisión de Henry):
  - Cielo `kloppenheim_02` y luna azulada con sombras.
  - **Exposición fija** (EV 4) en lugar de la automática que lavaba la imagen.
  - Tubos más débiles y cálidos.
  - Valores en `ConstruirColegio.NOCHE`.
  - Se quitó el segundo sol que venía de Blender. El apagón no apaga la luna y ahora también apaga el brillo de los tubos (`ApagarEmisivos.cs`).
- [x] **Candados distintos** para reconocerlos (y fotografiarlos):
  - **Latón de 3 ruedas** en el casillero.
  - **Candadito rojo de maleta de 3 ruedas** en la lonchera.
  - **Candado de acero de 4 ruedas con cadena** en la rectoría.
  - Modelos en `props_extra.py` (`candado_laton/maleta/cadena`) y colocados por `amueblar.candados_capitulo1()`. Fotos en `avances_edificio\candados\`.
  - En Unity (`CandadoVisual.cs`): mirar el candado abre su cerradura y al resolverla **se suelta y cae**.
- [x] **Pantalla del candado nueva** (`LockOverlay.cs`):
  - Dibuja cada candado con su forma y color, con ruedas que muestran el número anterior y el siguiente.
  - Las ruedas empiezan en números al azar.
  - Controles: A/D, W/S, rueda del mouse, clic, números del teclado, Enter y Esc.
- [x] **Concentrarse con Q** (`Concentrarse.cs`): se mantiene Q, Mateo respira y lo usable cercano brilla 2,8 s. No funciona con el estrés alto (más del 60 %).
- [x] **Fallas de base corregidas**:
  - `Pausa.cs` es el único dueño de `Time.timeScale` (12 menús migrados). Antes, con dos menús abiertos el juego se congelaba o corría detrás de una nota.
  - Las notas van en fila. Antes se perdía la carta de papá del casillero.
  - El candado se cierra antes de abrir lo de adentro.
  - Los avisos van en fila, duran 5 s como mínimo y esperan a que cierres la nota o el candado.
  - La mira ignora el cuerpo del jugador y las zonas invisibles.
  - Se quitó el inventario duplicado.
  - Notas y avisos usan la letra del menú; la nota se ve como **hoja de cuaderno**.
- [x] **Bugs de lógica del capítulo 1**:
  - Leer la nota del profe **borraba el escritorio**: ahora las notas quedan en el mundo.
  - **La puerta del 4°B estaba abierta** y se podía bajar a la rectoría sin resolver nada: ahora está trabada.
  - La caja de choque de los pupitres **tapaba las cartas y la lonchera**: ahora llega a 0,68 m.
  - Al volver de los ductos el jugador mira la puerta de la rectoría.
  - El despertar es corto (1 parpadeo).
- [x] Herramienta `Editor/Colegio/DiagnosticoAlcance.cs`: dice por qué un objeto no se alcanza.

## 19-oct — Pasillos entre pupitres y análisis de misiones
- [x] **Pasillos en las aulas** (`amueblar.py`, función `aula`): columnas a 1,35 m y filas a 1,2 m, lo que deja pasillos de ~75 cm (el jugador mide 70 cm) y ~1 m junto a las paredes. El 4°B quedó con 20 pupitres en 4×5.
- [x] Depósito de pupitres: las pilas van alineadas con el de abajo (antes cada uno giraba al azar y quedaba "flotando").
- [x] `scripts_blender\rehacer_muebles.py`: re-amuebla y revisa choques y apoyos sin abrir la ventana de Blender. Resultado: 0 choques y 0 flotando. Copia de antes en `edificio\EstanDentro_Edificio_antes_pasillos.blend`.
- [x] Unity: pupitres, sillas y bancas con **choque de caja sólida** (`ConstruirColegio.EsAsiento`), porque sus barras servían de escalón y el jugador se subía encima.
- [x] Prueba automática: usa reloj real y tiene un "vigilante" que anota qué menú congela el tiempo.
- [x] Análisis de misiones, HUD y estilo comparado con juegos AAA: `PROPUESTA_MISIONES_HUD_ESTILO.md`.

## 18-oct (noche) — Capítulo 1 pasado al colegio nuevo
- [x] `Assets/Editor/Colegio/PortarCapitulo1.cs` (corre solo dentro de *ConstruirColegio.Escena*). Copia la lógica de la escena vieja `EscenaUno-salonPrincipal` al Salón 4°B:
  - Cajón del escritorio del profe → nota con "ejercicios pág 142" y la fecha 17/05.
  - Casillero → clave 142 → destornillador + carta de papá (clave de la lonchera 0-4-6).
  - Lonchera → clave 046 → linterna + apagón (BlackoutEvent) + miedo a la oscuridad.
  - 4 mensajes ocultos en las paredes del 4°B (se ven con la linterna). Cartas 1 y 2 con textos originales.
  - Rejilla del techo del 4°B: con destornillador cae y la segunda vez lleva a la escena vieja de ductos (`RejillaDucto.cs` ahora carga escena).
  - Salida de los ductos → vuelve al colegio, spawn `salas_finales` frente a la rectoría (P1, ranura S1).
  - Rectoría (`PUERTA_P1_005`) cerrada con clave 1705; pistas en la secretaría; final (`EndGameTrigger`) dentro de la rectoría.
  - Spawn de inicio en un pupitre del 4°B mirando al tablero. Apagón, respiración, intrusiones y sonido global copiados.
- [x] Pupitres y sillas ya no se deslizan (fijos). Solo se empujan los muebles que trancan puertas (120 kg).
- [x] Ejecutable recompilado con 2 escenas (colegio + ductos), 522 MB.
- [x] `Assets/Scripts/Colegio/AutoPrueba.cs`: jugador automático. Se activa abriendo el exe con `-autoprueba "carpeta"`; recorre todo el capítulo, toma capturas y escribe `autoprueba.txt`.
- [x] `Assets/Editor/Colegio/InventarioCapitulo1.cs` → `01_Documentacion/capitulo1_inventario.txt` (cómo estaba armado el capítulo 1 viejo).

## 18-oct — Ejecutable para probar
- [x] Blender guardado y cerrado.
- [x] **Ejecutable de Windows** `06_Builds\Colegio\EstanDentro_Colegio.exe` (443 MB, solo la escena del colegio). Menú de Unity *Estan Dentro / 6. Compilar ejecutable del colegio* (`Assets/Editor/Colegio/CompilarColegio.cs`).
- [x] Probado: arranca en la RTX 3050 (DX12) con el efecto de despertar del capítulo 1, el HUD de estrés y la interacción funcionando. La API sin conexión pasa a modo offline sola.

## 18-oct — Fugas de luz y techo cuadriculado (Unity)
- [x] **Fugas de luz del sol** en uniones muro-techo (aparecían según la posición de la cámara):
  - La estructura (`Edificio_*`, `Puertas_*`) proyecta sombra por **ambas caras** (`ShadowCastingMode.TwoSided`), porque en Blender se quitaron las caras ocultas.
  - Sol con sombra de **4096**, normal bias 0,2 y contact shadows.
  - Volumen con `HDShadowSettings` (distancia de sombra de 70 m y 4 cascadas) y `ContactShadows`.
  - Verificado con capturas antes y después en `avances_edificio\fugas_antes_despues.jpg` (menú *Estan Dentro / 5. Capturas de fugas de luz*).
- [x] **Techo y paredes sin cuadrícula** con `scripts_blender\antirepeticion.py`: texturas horneadas de 4K que cubren 4×4 losetas.
  - Pasa-altos FFT para quitar las manchas fijas de la loseta y una copia girada 90° mezclada con manchas periódicas.
  - Manchas nuevas de baja frecuencia y variación de rugosidad.
  - Se aplicó a las 10 texturas sin patrón (yeso, cal, concreto). El exportador las usa solas (`antirepeticion.json`) con la escala de UV ajustada.
  - Comparación en `avances_edificio\techo_antes_despues.jpg`.

## 18-oct — Paso a Unity HDRP
- [x] **Escena jugable** `Assets/Scenes/Colegio/Colegio.unity`, agregada a Build Settings. Tiene 57 puertas `LockedDoor`, 852 objetos funcionales, 186 luces HDRP, cielo HDRI, exposición automática, niebla volumétrica, y el jugador y los sistemas del capítulo 1 (InventoryManager, _Systems, _AudioManager, Player). El jugador aparece en el Salón 4°B.
- [x] Capturas desde Unity (menú *Estan Dentro / 4. Capturas de revisión*) en `01_Documentacion\avances_edificio\unity\`.
- [x] Corregido un error de fondo en `Pieza.unir()`: pintaba caras viejas con el material de piezas nuevas (por eso los asientos de los pupitres salían grises).
- [x] Proyecto abierto con **Unity 6000.3.23f1** (se actualizó desde 6000.3.8f1). Primera importación con 0 errores de compilación.
- [x] Exportador `scripts_blender\exportar_unity.py`: hornea la escala real de cada textura en las UVs y exporta 21 FBX a `Assets/Art/Edificio/`.
  - Los grupos son Edificio_S/P1/P2/P3/AZ, Puertas_*, Muebles_*, Entorno, Jacaranda, Barrio, Fondo_lejano y Luces (vacíos con sus datos).
  - Cada objeto lleva sus propiedades personalizadas.
  - Se exportan además `materiales.json`, `red_ductos.json`, las texturas y los cielos HDRI.
- [x] Scripts de juego en `Assets/Scripts/Colegio/`: `DatosBlender`, `Recogible`, `Contenedor`, `Dispensador`, `Escondite` y `RejillaDucto`.
- [x] Editor en `Assets/Editor/Colegio/`:
  - `ColegioPostprocesador`: ajusta la importación y copia las propiedades de Blender a `DatosBlender`.
  - `ConstruirColegio`, menú **Estan Dentro**: crea los materiales HDRP Lit con máscara de suavidad, remapea los FBX, arma la escena `Assets/Scenes/Colegio/Colegio.unity` con colliders, puertas `LockedDoor` (giro por código según la bisagra), objetos funcionales, `HeavyProp`, luces HDRP, cielo HDRI con exposición automática y niebla volumétrica. Además copia los sistemas y el jugador del salón del capítulo 1 y pone al jugador en el Salón 4°B.

## 18-oct — Muebles (librería)
- [x] Revisados los 32 FBX de Henry (`03_Juego_Unity\EstanDentro\Assets\Art\Models`). Se definió cuáles se reutilizan y con qué escala (tabla `HENRY` en `props_lib.py`).
- [x] `scripts_blender\props_lib.py`:
  - 50 generadores de muebles a medida real.
  - Prototipos compartidos (`PROTO_<tipo>`) y la función `colocar()`.
  - Los modelos de Henry guardan `prefab_unity`.
