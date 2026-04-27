# 004 - Shader de Siluetas Periféricas + Controller + Manager

**Fecha:** 2026-04-22
**Sprint:** 2 - Mecánicas Core e Intrusiones
**Responsable:** Duvan Lozano
**Rama sugerida:** `feature/silhouettes`
**Tarea relacionada:** ID 12 - Shader de siluetas periféricas (8h)

---

## Qué se hizo

Sistema de siluetas periféricas según GDD: "Lo que más importa emocionalmente es lo que más se distorsiona y evita." La silueta es invisible cuando la miras de frente y aparece con fuerza en la visión periférica.

### Lógica de opacidad por ángulo
- `ángulo < 15°` → opacidad 0 (desaparece cuando la miras)
- `15°–45°` → opacidad gradual con curva suave (smoothstep)
- `ángulo > 45°` → opacidad máxima

### Scripts creados
- `Assets/Scripts/Intrusions/SilhouetteController.cs`
  - Calcula ángulo cámara↔silueta cada frame.
  - Escribe `_Opacity` al material vía `MaterialPropertyBlock` (no duplica materiales).
  - Parámetros expuestos: inner/outer angle, maxOpacity, fadeSpeed, pulso de tensión.
  - Modo `lockToView` para la silueta adherida estilo *Lisa* de P.T. (El Que Se Sienta Delante).
  - Gizmos: dibuja los conos de 15° y 45° desde la cámara en el editor.
- `Assets/Scripts/Intrusions/SilhouetteManager.cs`
  - Instancia siluetas en anclas de pupitre al iniciar.
  - Reshuffle aleatorio en muerte/reinicio (regla del GDD: "el salón recuerda").
  - Hook `SetCoweringScale` para cuando Sprint 3 conecte la intrusión del Iracundo.
  - Spawnea al "stalker" como silueta orbital adherida al jugador.

### Assets pendientes (hacer hoy desde Unity)
- `Assets/Art/Shaders/Silhouette.shadergraph` (HDRP Unlit).
- `Assets/Art/Materials/M_Silhouette.mat` (material basado en el shader).
- `Assets/Prefabs/FX/Silhouette.prefab` (quad con el material + SilhouetteController).
- `Assets/Scenes/Chapter1_Salon4B/Chapter1_Salon4B.unity` (escena principal).

---

## Guía paso a paso para probar HOY

### Paso 1 — Crear el Shader Graph (5 min)

1. En Unity, click derecho en `Assets/Art/Shaders/` → **Create → Shader Graph → HDRP → Unlit Shader Graph**.
2. Nómbralo `Silhouette`.
3. Doble click para abrirlo. En **Graph Inspector** → **Graph Settings**:
   - Surface Type: **Transparent**
   - Blending Mode: **Alpha**
   - Receive Fog: **off** (para que no lo afecte el fog del aula)
   - Alpha Clip: **off**
4. En **Blackboard** (panel izquierdo, botón **+**) añade estas properties **con los nombres de referencia exactos**:
   - `_BaseColor` — tipo **Color**, default `(0, 0, 0, 1)` (negro).
   - `_Opacity` — tipo **Float**, default `1`, Mode **Default**.
   - `_NoiseScale` — tipo **Float**, default `8`.
   - `_NoiseThreshold` — tipo **Float**, default `0.35`, Mode **Slider** 0..1.
   - `_DissolveEdge` — tipo **Float**, default `0.05`.
   > Importante: el **Reference** debe ser `_Opacity`, no `Opacity_abc123`. Edita la property, campo "Reference".
5. Grafo mínimo:
   - Arrastra `_BaseColor` al nodo **Fragment → Base Color**.
   - Para el Alpha: nodo **Simple Noise** (input UV = `UV`, Scale = `_NoiseScale`) → **Step** (Edge = `_NoiseThreshold`) → **Multiply** por `_Opacity` → conectar a **Fragment → Alpha**.
   - (Opcional, más nice) Añadir **Smoothstep** en vez de Step, con Edge1 = `_NoiseThreshold - _DissolveEdge`, Edge2 = `_NoiseThreshold + _DissolveEdge` para un borde disolvente suave.
6. **Save Asset** (botón arriba a la izquierda).

### Paso 2 — Crear el material

1. Click derecho en `Assets/Art/Materials/` → **Create → Material**, nómbralo `M_Silhouette`.
2. En el Inspector, campo **Shader**: busca `Shader Graphs/Silhouette`.
3. Verifica que aparecen las properties en el Inspector del material. Ajusta color base a negro con alpha en 1.

### Paso 3 — Crear el prefab de silueta

1. En la escena (por ahora `Dev_Duvan.unity`), **GameObject → 3D Object → Quad**.
2. Escálalo a `(0.8, 1.8, 1)` → silueta humana de referencia.
3. Arrástrale `M_Silhouette` al Mesh Renderer.
4. **Add Component → SilhouetteController** (lo encuentras como `Estan Dentro → Intrusions → Silhouette Controller` o buscando por nombre).
5. En el Controller: deja `targetCamera` vacío (usa Camera.main) o arrastra tu cámara del FPS.
6. Arrastra ese GameObject desde la Hierarchy a `Assets/Prefabs/FX/` para convertirlo en **Prefab**. Nómbralo `Silhouette`.
7. Bórralo de la escena (ya lo instanciará el Manager).

### Paso 4 — Crear anclas de pupitre

1. En la Hierarchy, `GameObject → Create Empty`, nómbralo `SeatAnchors`.
2. Dentro de ese objeto, crea varios empty GameObjects posicionados sobre los pupitres (cada Empty = un asiento). Rota cada uno mirando hacia la pizarra.
3. Si Henry todavía no terminó el blockout, pon 6–8 a mano en cualquier posición para probar la mecánica.

### Paso 5 — Crear el Manager

1. `GameObject → Create Empty`, nómbralo `SilhouetteManager`.
2. **Add Component → SilhouetteManager**.
3. En el Inspector del Manager:
   - `Player Camera`: arrastra tu cámara del FPS (o deja vacío para Camera.main).
   - `Silhouette Prefab`: arrastra `Silhouette.prefab`.
   - `Seat Anchors`: arrastra todos los Empties que creaste dentro de `SeatAnchors`.
   - `Initial Occupancy`: 6–8.
   - `Spawn Stalker`: ✓ (para probar El Que Se Sienta Delante).
   - `Stalker Lock Distance`: 2.5
   - `Stalker Lock Angle`: 60 (queda siempre a 60° de tu forward).

### Paso 6 — Probar

1. Play.
2. Mueve el mouse lentamente mirando un pupitre con silueta: debe **desvanecerse** al apuntar directo (< 15°) y **aparecer fuerte** si la sacas de la vista (> 45°).
3. Para el stalker: nunca debes poder enfocarlo bien — siempre está en la periferia. Gira sobre ti mismo y verás que "sigue estando ahí".
4. Con el Inspector del Controller seleccionado en un prefab instanciado en runtime, puedes ver los **conos gizmo** (verde 15°, naranja 45°) para validar los umbrales.

---

## Paso 7 — Crear la escena principal Chapter1_Salon4B

1. Abre tu escena actual y **guárdala** antes (Ctrl+S).
2. `File → New Scene` → elige **HDRP Template** (Basic Outdoors o Empty — da igual, luego se limpia).
3. `File → Save As` → guardar en `Assets/Scenes/Chapter1_Salon4B/Chapter1_Salon4B.unity`.
4. En la nueva escena, limpia lo que no sirva del template (árboles, terrain, etc.) y deja:
   - Un `Volume` (GameObject → Volume → Global Volume) con un perfil básico.
   - Directional Light tenue + ambient oscuro (luego Henry mete el perfil correcto).
   - Player prefab (o duplica la configuración que tengas en `Dev_Duvan`).
5. `File → Build Settings` → **Scenes In Build**:
   - Arrastra `Chapter1_Salon4B.unity` al principio.
   - Quita `OutdoorsScene.unity` (es la default del template HDRP).
6. Por ahora deja el salón vacío: Henry integrará su blockout aquí cuando lo traiga.

### Regla de trabajo nueva para el equipo
- **Prototipar** en `Dev_Duvan`, `Dev_Henry`, `Dev_Carlos`.
- **Integrar** siempre en `Chapter1_Salon4B.unity` cuando el sistema esté validado.
- **Avisar en el grupo** antes de tocar `Chapter1_Salon4B.unity` (regla 1 del GDD: nunca dos personas editando la misma escena).

---

## Archivos creados o modificados

- `Assets/Scripts/Intrusions/SilhouetteController.cs` (nuevo)
- `Assets/Scripts/Intrusions/SilhouetteManager.cs` (nuevo)
- `Assets/Art/Shaders/` (carpeta nueva)
- `Assets/PostProcessing/` (carpeta nueva — Henry llenará con los 4 perfiles, ID 10)
- `Document/Changelog/004_Shader_Siluetas_Perifericas.md` (nuevo)

Pendientes que Duvan debe crear en Unity hoy:
- `Assets/Art/Shaders/Silhouette.shadergraph`
- `Assets/Art/Materials/M_Silhouette.mat`
- `Assets/Prefabs/FX/Silhouette.prefab`
- `Assets/Scenes/Chapter1_Salon4B/Chapter1_Salon4B.unity`

---

## Notas para el equipo

- **Henry:** cuando termines los 4 perfiles de post-procesado (ID 10), impórtalos en `Assets/PostProcessing/`. Para el Observador, recuerda: frío/clínico, sin grano, bordes afilados. Duvan los va a usar en la intrusión del Observador (ID 7).
- **Henry:** cuando tengas modelo de silueta humanoide (plano recortado o mesh), reemplaza el Quad placeholder del prefab `Silhouette.prefab`. El material y script siguen iguales.
- **Carlos:** cuando juegues el test (CM3), valida que:
  - Al mirar **directo** a un pupitre con silueta, la silueta **no debe verse**.
  - En el borde de la visión, debe estar **claramente visible**.
  - La del stalker **nunca debe poder enfocarse bien** (siempre orbita).
- **Todos:** a partir de ahora, la escena principal del Capítulo 1 es `Chapter1_Salon4B.unity`. Las Dev_* son solo para prototipar.

---

## Próximos pasos (Sprint 2 — lo que sigue)

- ID 7 Intrusión del Observador (bloqueado parcialmente por textos de pizarra CM2 + perfil post-proc Henry ID 10, pero el esqueleto de detección de mirada puede hacerse sin eso).
- ID 3.6 Intrusión del Iracundo (bloqueado por marco agrietado + audios metálicos).
- ID 9 Intrusión del Infante (bloqueado por modelo de lonchera de Henry + canción infantil de Carlos).
