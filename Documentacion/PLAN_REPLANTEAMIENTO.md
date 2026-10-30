# Están Dentro — Diagnóstico y replanteamiento (18-oct-2026)

## 1. Estado real (lo que hay)

**Qué funciona (Capítulo 1, entregado el 13-may):** menú, cinemática, Acto 1 en el salón 4B (cartas, casillero 142, lonchera 046,
linterna, mensaje oculto, ducto del techo con cinemática), Acto 2 en los ductos (gateo, golpes, linterna que falla) y Acto 3 en
las salas finales (puertas con 1705, video del giro final, créditos).
Sistemas sólidos: estrés, respiración (micrófono o teclado), inventario, notas, cerraduras, puertas, siluetas/Observador,
guardado por API, logros, ajustes y soporte de mando. **El código es lo mejor del proyecto.**

**Qué falta o está flojo:**
| Área | Problema |
|---|---|
| Enemigo / IA | No existe. `Scripts/AI` está vacía. Sin amenaza no hay "Outlast/RE": hoy es un escape room con atmósfera |
| Edificio | No hay edificio: solo un salón (22×17 m, el doble de un aula real), una "estructura" de 1 piso tipo caja y ductos |
| Modelos 3D | Salón de 329k triángulos con 3 materiales, sin LODs. Escalas rotas: planta de 5,7 m, llaves de 65 cm, caja eléctrica de 11 cm |
| Narrativa | 3 versiones que chocan: GDD (5 capítulos, identidades disociativas, sin HUD, sin linterna hasta el cap. 3) vs. Diseño_Narrativo (Mateo Mendoza en coma, el padre vivo, 3 capítulos) vs. la idea de hoy (Outlast/RE + Fenómeno Siniestro) |
| Dependencias | La API vive en el PC de Duvan / somee: si se cae, el juego no debe depender de ella. Hay 4,6 GB de .mov fuera de git y un .mov de 1,6 GB dentro de Assets |
| Ganchos | El jugador tarda en entender qué hacer; el objetivo está en un HUD y no en el mundo |

## 2. Propuesta: una sola idea clara

**Premisa (gancho en una línea):** *"Te quedaste dormido en clase. Despiertas de noche, el colegio está vacío, y cada puerta
de salida te devuelve adentro."* (Fenómeno Siniestro / Grave Encounters + P.T.)

- **Objetivo único y siempre visible: SALIR.** Nada de entender la historia para jugar. La historia (accidente, coma, padre)
  queda como capa opcional en cartas y flashbacks, para quien la busque. El giro final la revela.
- **Amenaza (Outlast/RE):** una entidad, "El que se sienta delante" (ya está en el GDD), que patrulla, oye ruidos y persigue.
  No hay armas: **esconderse + respirar**. Escondido en un casillero, el minijuego de respiración que ya existe sirve para
  aguantar la respiración y no ser oído. Reutiliza el código y es una mecánica única del juego.
- **Estructura tipo comisaría de RE2:** un edificio interconectado con llaves y atajos, no niveles sueltos. Cada piso se abre con
  un objeto. Volver a zonas conocidas que **cambiaron** (pasillos que se alargan, puertas que llevan a otro piso) es el terror
  de Fenómeno Siniestro.
- **Ritmo:** al principio, a los 30 s ya hay algo raro y a los 3 min aparece la primera amenaza. Después, cada 5–8 min, un susto,
  un respiro y una llave nueva.

## 3. El edificio (3 pisos + sótano, azotea solo como final)

Escala real: altura de piso de 3,2 m, puertas de 0,9×2,1 m, pasillos de 2,4–3 m, aulas de 9×7 m. Kit modular en Blender
(cuadrícula de 1 m) para construir todo con las mismas piezas y texturas "trim sheet": se ve mejor y pesa mucho menos.

| Nivel | Zonas | Papel en el juego |
|---|---|---|
| **Piso 3** | Aula 4B (inicio), aulas 3A–3D, sala de cómputo, aula de música, baños | Tutorial, primeros sustos, se consigue la linterna |
| **Piso 2** | Biblioteca (laberinto de estanterías), laboratorio de ciencias, sala de profesores, enfermería | Primera persecución, puzzles con llaves |
| **Piso 1** | Entrada principal (**salida falsa**: te devuelve al piso 3), rectoría (props ya modelados), cafetería, auditorio, portería | El hub con atajos que conectan todo |
| **Sótano** | Calderas, cuarto eléctrico (caja eléctrica ya modelada), archivo, depósito, ductos | Restaurar la luz, la verdad del accidente, la salida real |
| Azotea (opcional) | Tanques de agua, vista a la ciudad que no existe | Final alternativo |

Escaleras: dos (norte y sur) y una bloqueada, que fuerza rutas. Los ductos que ya existen conectan los pisos en secreto.

## 4. Orden de trabajo recomendado

1. **Base técnica (1 sesión):** abrir el proyecto en Unity 6000.3.23f1, revisar que compila, pasar los .mov a .mp4 comprimido
   y hacer que la API sea opcional (modo sin conexión).
2. **Blockout del edificio:** cajas grises de los 4 niveles en Blender, probadas en Unity caminando. Se decide la planta
   **antes** de modelar bonito.
3. **Rebanada vertical (vertical slice):** piso 3 + escalera + parte del piso 2, terminado al 100 %: enemigo, escondites, 2 llaves,
   luz, sonido. Son 20–30 min que ya enganchan. Se juega con alguien que no conozca el juego y se mira dónde se pierde.
4. **Kit modular final + props mejorados:** se arreglan escalas, materiales PBR y LODs del salón.
5. Pisos 2, 1 y sótano, final y pulido.

## 5. Decisiones pendientes de Henry
- ¿Se aprueba la premisa "salir del colegio" con un enemigo que persigue (sin armas)?
- ¿Qué pasa con la historia del coma y el padre: capa opcional (recomendado) o protagonista?
- ¿Se instala Blender MCP para ver en vivo cómo se modela?
- ¿A qué repositorio se sube después (Niumaster69 con el equipo o AdrenMii propio)?
