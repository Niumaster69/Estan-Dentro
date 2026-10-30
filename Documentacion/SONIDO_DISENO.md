# Sonido de Están Dentro: qué suena, por qué y qué falta

Fecha: 19-oct-2026. Auditoría completa en este archivo; el cableado está en `Assets/Editor/Colegio/SonidosColegio.cs`.

## Reglas (para que se sienta natural)
1. **Todo sonido nace de algo que se ve.** Si suena un casillero, hay un casillero cerca. Nada de pizarrón sonando en un pasillo.
2. **Entrada → durante → salida.** Cada evento avisa (crujido, latido), pasa (golpe) y se calma (respiración, silencio).
3. **El silencio también es sonido.** El colegio de noche es casi mudo: zumbido bajo, lluvia lejana, el cuerpo de Mateo
   (respiración, pasos, latido). Los sustos funcionan porque antes no sonaba nada.
4. **Cortos y con variación.** Un clic dura menos de medio segundo, cambia un poco de tono cada vez y nunca se repite igual.
5. **Espacial.** Lo del mundo suena desde su sitio (3D) y baja con la distancia; lo de Mateo (respiración, latido) suena “adentro”.

## Lo que se arregló hoy
| Problema | Arreglo |
|---|---|
| El sonido del **pizarrón** (`pizaron.mp3`) sonaba al azar en todo el colegio (venía del salón viejo) | Quitado. Los eventos sueltos ahora son golpes, crujidos y una silla lejana, cada 30–70 s y más bajitos |
| El casillero se oía **17 s** cada vez | Se corta a 1,6 s con fundido (`SonidoCorto`) |
| Sustos con clips de 28 s (stinger), 3 min (latido) y 8 s (grito) | Se cortan a 2,5–4,5 s con fundido |
| La brisa de fondo estaba a 0,004 (no se oía) | 0,12: se siente el aire del edificio sin tapar nada |
| 17 candados se abrían en silencio; 57 puertas cerradas no sonaban al jalarlas | Clic de acierto y jaloneo puestos en todos |
| Al despertar no había latido ni jadeo | Latido + jadeo puestos (ya existían, nadie los había conectado) |
| Cajones, objetos, mochila y candado que cae: mudos | Sonido de cajón, de tomar y golpe metálico |

## Mapa de sonidos: evento → sonido → por qué
| Evento | Sonido (hoy) | Por qué | ¿Sirve? |
|---|---|---|---|
| Despertar en 4°B | Latido + jadeo | Mateo vuelve en sí asustado; el latido es el hilo con el monitor del hospital | Sirve; falta un pitido de oído (tinnitus) |
| Ambiente del colegio | Brisa baja + golpes/crujidos raros | Edificio viejo de noche: el aire y la madera “trabajan” solos | Sirve provisional; falta zumbido de tubos y lluvia en ventanas |
| Abrir casillero | Bisagra metálica (1,6 s) | Es lata delgada: chirrido corto y seco | Mejorable: falta “clac” del pestillo |
| Tomar objeto | “Pickup” genérico | Feedback de que entró al inventario | **No suena natural**: debe sonar a lo que es (pilas, monedas, tiza, papel) |
| Recoger / leer nota | Mismo “pickup” | — | **Cambiar por papel** (doblar, desdoblar) |
| Girar rueda del candado | Clic de dígito | Clic mecánico real de la rueda | Sirve |
| Candado abre | Clic de acierto | El gancho que salta | Sirve |
| Candado no abre | (nada) | Debe sonar un jalón seco del gancho que no cede | **Falta** |
| Forzar con pata de cabra | Jaloneo de puerta (1 s) | Hace ruido a propósito: el monstruo lo oye | **Corto**: falta palanca + metal que revienta |
| Candado cae al piso | Golpe metálico corto | Peso pequeño de metal contra baldosa | Provisional |
| Puerta cerrada | Jaloneo | La chapa que no cede | Sirve |
| Linterna | Clic (era igual al clic de menú) | Interruptor de plástico | **Cambiar**: que no sea el mismo de la UI |
| Marcar con tiza | (nada) | Raspar tiza en pared | **Falta** |
| Pasos de Mateo | (nada) | Lo que más da inmersión: baldosa, madera, charco | **Falta, prioridad 1** |
| Susto “portazo” | Crujido → golpe + jadeo → respiro | La puerta avisa, golpea y Mateo se calma | Falta un portazo de lata de verdad (el golpe dura 0,3 s) |
| Susto “cae” | Golpe + jadeo, lata rodando | Algo se cae de adentro | Falta sonido de lata rodando |
| Susto “celular” | Latido + texto | Vibra con mensaje de Pa | **Falta vibración de celular** |
| Susto “susurro” | Grito lejano + luces que parpadean | El clip se llama susurro pero es un grito | **Cambiar por un susurro real** |
| Recuerdo en casillero | Papel | Es una foto o un papelito | Igual que nota |
| Prólogo: motor, lluvia, limpiabrisas, radio, choque, monitor, timbre | Sintetizados (`04_Audio/sintesis_prologo.py`) | Provisionales, para tener la escena andando | **Reemplazar todos** por grabaciones |

## Archivos que sobran o están mal nombrados
- `UI Submenu open.WAV`: 26 MB y 90 s; no es un sonido de UI.
- `Flashlight onoff.wav` y `UI Click.wav` son el mismo archivo.
- Sin uso: `classroom.mp3` (sirve para un recuerdo del salón con gente), `seat_belt.wav` (**sirve para el prólogo**: el cinturón),
  `destornillador.wav`, `rejilla.wav`, 3 de sillas arrastrándose, `Crujido 3`, `UI BackCancel`.
- No se borra nada sin el sí de Henry.

## Lo que hace falta conseguir (lista de compras)
Prioridad 1 (inmersión): pasos en baldosa/madera (6 variaciones), papel (tomar, desdoblar), respiración normal y agitada,
zumbido de fluorescentes, lluvia en ventana, vibración de celular, susurro real, candado que no abre, palanca forzando metal,
portazo de casillero metálico, lata rodando, tiza en pared.
Prólogo: motor de carro viejo por dentro, lluvia en el techo del carro, limpiabrisas, radio AM, cinturón, choque, monitor de hospital, timbre de colegio.

**De dónde, sin pagar y con permiso de uso:** Sonniss GDC Game Audio Bundle (gratis, uso comercial), Freesound filtrado
por CC0, Pixabay Sounds. Hay que descargar: necesita el sí de Henry (archivos de internet).

## Segunda pasada (19-oct, noche): sonidos de verdad

44 sonidos nuevos CC0 (Freesound + Kenney), recortados, con el volumen parejo y bucles sin corte (`04_Audio/preparar_sonidos.py`).
Están en `Assets/Resources/SonidosJuego/`. Créditos: `04_Audio/CREDITOS_SONIDOS.md`.

| Archivo | Seg. | Por qué |
|---|---|---|
| `pasos/baldosa_0..4 (variaciones)` | 0.1 | Pasos en baldosa de colegio: zapato de suela dura sobre piso duro |
| `pasos/madera_0..4 (variaciones)` | 0.27 | Pasos sobre tarima/madera (salón, escenario) |
| `cuerpo/respiracion_miedo.wav` | 12.5 (bucle) | Respiración agitada cuando el estrés sube: el cuerpo avisa antes que la vista |
| `cuerpo/tinnitus.wav` | 8.0 | Pitido en el oído al despertar: Mateo viene de un golpe (el choque) |
| `cuerpo/ropa_0..4 (variaciones)` | 0.61 | Ropa al agacharse / guardar en la mochila |
| `objetos/papel_tomar.wav` | 1.06 | Tomar una nota o un recuerdo: suena a papel, no a "pickup" de videojuego |
| `objetos/papel_abrir.wav` | 1.42 | Desdoblar la nota al leerla |
| `objetos/monedas.wav` | 0.75 | Monedas en la mano |
| `objetos/objeto_tomar.wav` | 0.27 | Tomar un objeto chico (pilas, tiza, destornillador): roce de mano, discreto |
| `objetos/mochila.wav` | 0.72 | Ponerse la mochila: correa y tela |
| `objetos/cuaderno_abrir.wav` | 0.15 | Abrir el Cuaderno de Mateo |
| `objetos/cuaderno_pagina.wav` | 0.43 | Cambiar de pestaña en el cuaderno |
| `objetos/cuaderno_cerrar.wav` | 0.23 | Cerrar el cuaderno |
| `objetos/tiza.wav` | 1.4 | Marcar una X con tiza en la pared |
| `objetos/linterna.wav` | 0.18 | Interruptor de la linterna (ya no es el mismo clic del menú) |
| `casillero/pestillo.wav` | 0.23 | Pestillo del casillero al abrir: clac metálico corto |
| `casillero/portazo.wav` | 2.0 | Susto: casillero de lata que se cierra de golpe |
| `casillero/lata_rueda.wav` | 1.48 | Susto: una lata cae del casillero y rueda |
| `candado/no_abre.wav` | 1.0 | Clave equivocada: el gancho se sacude y no cede |
| `candado/forzar.wav` | 1.79 | Forzar con la pata de cabra: fuerte a propósito, el monstruo lo oye |
| `candado/cae.wav` | 0.21 | El candado forzado cae a la baldosa |
| `sustos/celular_vibra.wav` | 4.1 | Susto: un celular vibra dentro del casillero (mensaje de Pa) |
| `sustos/susurro.wav` | 3.52 | Susto: un susurro de verdad (el de antes era un grito) |
| `ambiente/fluorescente.wav` | 28.5 (bucle) | Zumbido de tubos fluorescentes: el "silencio" de un colegio de noche |
| `ambiente/lluvia_ventana.wav` | 58.5 (bucle) | Lluvia en las ventanas: llueve desde el prólogo, une las dos escenas |
| `ambiente/timbre.wav` | 3.36 | Timbre del colegio: pasa del monitor del hospital al despertar en 4°B |
| `prologo/carro_dentro.wav` | 58.5 (bucle) | Motor y rodadura desde adentro de un carro andando |
| `prologo/lluvia_techo.wav` | 58.5 (bucle) | Lluvia fuerte sobre el techo del carro |
| `prologo/limpiabrisas.wav` | 2.57 | Una pasada del limpiabrisas (se repite al ritmo de la animación) |
| `prologo/radio_sintonia.wav` | 5.73 | Mateo mueve la radio: estática y sintonía AM |
| `prologo/choque.wav` | 3.4 | El choque: frenazo y golpe (lo más fuerte del juego) |
| `prologo/monitor.wav` | 18.5 (bucle) | Monitor del hospital tras el choque: el hilo con el final |
| `prologo/cinturon.wav` | 3.0 | Cinturón de seguridad (ya estaba en el proyecto sin usar) |

**Conectados en el juego:** pasos (baldosa/madera, más suaves agachado, correr hace ruido que el monstruo oye) y respiración que sube con el estrés (`Scripts/Colegio/CuerpoMateo.cs`); papel en notas y recuerdos; monedas; pestillo del casillero; cuaderno (abrir, pasar página, cerrar); tiza; linterna; candado que no abre, forzar y caer; los 4 sustos; lluvia en las ventanas de fondo.
**Falta conectar:** zumbido de fluorescentes (debe apagarse con el apagón), tinnitus al despertar, timbre, y todo lo del prólogo (cuando exista la escena).
**Henry:** escúchalos en el juego; si alguno no cuadra, cambiarlo es elegir otra opción en `04_Audio/descargas/freesound/<uso>/` (hay 3 por sonido) y volver a correr `preparar_sonidos.py`.
