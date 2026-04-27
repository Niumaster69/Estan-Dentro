<<<<<<< HEAD
# 005 - Skeleton de la Intrusion del Observador + sistema de mirada

**Fecha:** 2026-04-22
**Sprint:** 2 - Mecanicas Core e Intrusiones
**Responsable:** Duvan Lozano
**Rama sugerida:** `feature/observer-intrusion` (o seguir en `feature/silhouettes` si se quiere agrupar).
**Tarea relacionada:** ID 7 - Intrusion del Observador (parte desbloqueada: deteccion de mirada)

---

## Que se hizo

Esqueleto de deteccion de mirada sostenida (gaze-held) reutilizable para todas las intrusiones que lo necesiten. Por ahora solo cableado con la pizarra para disparar la intrusion del Observador.

### Scripts creados
- `Assets/Scripts/Intrusions/IntrusionManager.cs`
  - Singleton por escena. Enum `IntrusionType { None, Observer, Iracundo, Infante }`.
  - `Trigger(IntrusionType)` lanza `Debug.Log` + UnityEvent generico + UnityEvent por tipo.
  - Los eventos estan listos para que Sprint 3 enganche post-procesado (Henry ID 10) y audios (Carlos) sin tocar codigo.
- `Assets/Scripts/Intrusions/Gaze/GazeTargetBase.cs`
  - Abstract base con `GazeEnter/GazeStay/GazeExit`, tiempo acumulado, umbral `holdDurationSec` (default 3.5s) y `resetOnLookAway`.
  - UnityEvents `onGazeEnter`, `onGazeHeld`, `onGazeExit` para wiring en Inspector.
- `Assets/Scripts/Intrusions/Gaze/GazeDetector.cs`
  - Va en la camara. Cada frame lanza un `Physics.Raycast` por el forward y notifica al `GazeTargetBase` apuntado.
  - Respeta oclusion: si hay un muro entre camara y objetivo, el muro lo intercepta.
  - Gizmo en Scene view: linea amarilla cuando no mira nada, roja cuando mira un target.
- `Assets/Scripts/Intrusions/Gaze/ObserverTrigger.cs`
  - `GazeTargetBase` concreto para la pizarra. Al completar el hold pide a `IntrusionManager.Trigger(Observer)` y aplica un cooldown (default 6s) para evitar spam.

### Regla de diseño respetada
- El jugador "mira fijamente" la pizarra durante 3.5s → disparo.
- `resetOnLookAway = true` → si rompe el contacto visual, empieza de cero. Respeta la intencionalidad.
- Evento repetible con cooldown → coherente con la necesidad de narrativa evolutiva de las intrusiones.

---

## Guia de instalacion en Unity

### Paso 1 - Agregar GazeDetector a la camara del jugador

1. En Hierarchy expande el Player hasta la `Main Camera`.
2. Selecciona la Main Camera.
3. Inspector → **Add Component** → busca `Gaze Detector`.
4. Configura:
   - `Max Distance` = 50 (o lo que cubra el aula).
   - `Hit Layers` = Everything (puedes afinarlo despues).
   - `Source Camera` = deja vacio si esta en la misma camara (se auto-asigna).
   - `Draw Gizmo` = ✓ (util para debug).
   - `Log Target Changes` = ✓ al principio para ver en Console cada vez que enfocas un target.

### Paso 2 - Crear el IntrusionManager

1. Hierarchy → click derecho espacio vacio → **Create Empty** → nombrar `IntrusionManager`.
2. **Add Component** → `Intrusion Manager`.
3. Deja los UnityEvents vacios por ahora. `Log To Console` = ✓.
4. Guarda Ctrl+S.

### Paso 3 - Montar el ObserverTrigger en la pizarra

La escena **no tiene un GameObject llamado "pizarra"**. El modelo del aula viene de Blender como un mesh unico con submaterials (entre ellos `tableroVerde.mat`). Opciones:

**Opcion A (rapida, recomendada para prototipo):**
1. Hierarchy → `GameObject → Create Empty` hijo del `Salon`.
2. Nombrarlo `PizarraTrigger`.
3. Posicionarlo sobre la pared con la pizarra (mueve el Empty con la herramienta W hasta que su pivote quede en el centro de la pizarra).
4. **Add Component → Box Collider**. Ajusta `Size` para que la Box cubra el area visible de la pizarra (por ejemplo `Size = (3, 1.5, 0.1)`).
5. **Add Component → Observer Trigger**.
   - `Hold Duration Sec` = 3.5
   - `Reset On Look Away` = ✓
   - `Cooldown Sec` = 6
6. El Box Collider **no** debe ser trigger (`Is Trigger = off`) — asi bloquea el raycast y a la vez cuenta como "algo mirable".

**Opcion B (precisa, cuando Henry separe la pizarra como submesh):**
1. Identifica el GameObject del submesh `tableroVerde` dentro del modelo del aula.
2. A ese GameObject anadele directamente el `MeshCollider` (si no lo tiene) + `ObserverTrigger`.
3. No necesitas un Empty adicional.

### Paso 4 - Probar

1. Play.
2. Mueve la camara hasta enfocar la pizarra. Con `Log Target Changes` deberias ver en Console:
   `[Gaze] Enter: PizarraTrigger`.
3. Manten la mirada 3.5 segundos sin mover demasiado. Debe salir:
   `[Intrusion] Triggered: Observer @ Xs`.
4. Aparta la mirada y vuelve a enfocar: contador se resetea, puedes volver a disparar (tras el cooldown de 6s).

### Gizmos utiles
- Selecciona la Main Camera con Play corriendo: veras la linea de raycast (amarilla = sin target, roja = target detectado).
- Selecciona `PizarraTrigger` en Scene view: veras la caja verde del BoxCollider que cubre la pizarra.

---

## Archivos creados

- `Assets/Scripts/Intrusions/IntrusionManager.cs`
- `Assets/Scripts/Intrusions/Gaze/GazeTargetBase.cs`
- `Assets/Scripts/Intrusions/Gaze/GazeDetector.cs`
- `Assets/Scripts/Intrusions/Gaze/ObserverTrigger.cs`
- `Document/Changelog/005_Skeleton_Intrusion_Observador.md`

Escena modificada: `Chapter1_Salon4B.unity` (al agregar GazeDetector, IntrusionManager y PizarraTrigger).

---

## Notas para el equipo

- **Henry (ID 10):** cuando termines el perfil de post-procesado del Observador (frio/clinico, sin grano, bordes afilados), ponte un `Volume` en la escena con ese perfil, desactivalo por defecto y suscribe su `SetActive(true)` al `On Observer` del `IntrusionManager` desde el Inspector. Igual de importante: separar la pizarra como submesh (u otro GameObject) permite usar la Opcion B del Paso 3 y evita que el BoxCollider de placeholder quede en el build final.
- **Carlos:** cuando tengas el SFX del Observador, crea un `AudioSource` en la escena, ponle el clip, desactiva Play On Awake y suscribe `AudioSource.Play` al `On Observer` del `IntrusionManager`. Para el CM3: valida que la intrusion se dispara al mirar la pizarra 3.5s y no antes; y que no se puede spamear el disparo (cooldown de 6s).
- **Duvan (yo):** el siguiente paso propio es ensamblar la **reaccion visual** del Observador dentro del `IntrusionManager.onObserver` (por ejemplo, cambiar el parametro `_Opacity` maximo de las siluetas, acelerar su `fadeSpeed`, activar un Volume), pero eso requiere las entregas de Henry.

---

## Proximos pasos (Sprint 2)

- **ID 7** ensamblado final cuando entreguen:
  - Textos de pizarra (CM2).
  - Perfil post-proc del Observador (Henry ID 10).
- **ID 3.6 Iracundo** — bloqueado por marco agrietado + audios metalicos. El `IntrusionManager.onIracundo` ya tiene su gancho listo; falta la logica de *cuando* se dispara (¿Iracundo se dispara por otro gesto distinto al gaze? revisar GDD).
- **ID 9 Infante** — bloqueado por lonchera (Henry) + cancion infantil (Carlos). Mismo patron.
=======
# 005 - Skeleton de la Intrusion del Observador + sistema de mirada

**Fecha:** 2026-04-22
**Sprint:** 2 - Mecanicas Core e Intrusiones
**Responsable:** Duvan Lozano
**Rama sugerida:** `feature/observer-intrusion` (o seguir en `feature/silhouettes` si se quiere agrupar).
**Tarea relacionada:** ID 7 - Intrusion del Observador (parte desbloqueada: deteccion de mirada)

---

## Que se hizo

Esqueleto de deteccion de mirada sostenida (gaze-held) reutilizable para todas las intrusiones que lo necesiten. Por ahora solo cableado con la pizarra para disparar la intrusion del Observador.

### Scripts creados
- `Assets/Scripts/Intrusions/IntrusionManager.cs`
  - Singleton por escena. Enum `IntrusionType { None, Observer, Iracundo, Infante }`.
  - `Trigger(IntrusionType)` lanza `Debug.Log` + UnityEvent generico + UnityEvent por tipo.
  - Los eventos estan listos para que Sprint 3 enganche post-procesado (Henry ID 10) y audios (Carlos) sin tocar codigo.
- `Assets/Scripts/Intrusions/Gaze/GazeTargetBase.cs`
  - Abstract base con `GazeEnter/GazeStay/GazeExit`, tiempo acumulado, umbral `holdDurationSec` (default 3.5s) y `resetOnLookAway`.
  - UnityEvents `onGazeEnter`, `onGazeHeld`, `onGazeExit` para wiring en Inspector.
- `Assets/Scripts/Intrusions/Gaze/GazeDetector.cs`
  - Va en la camara. Cada frame lanza un `Physics.Raycast` por el forward y notifica al `GazeTargetBase` apuntado.
  - Respeta oclusion: si hay un muro entre camara y objetivo, el muro lo intercepta.
  - Gizmo en Scene view: linea amarilla cuando no mira nada, roja cuando mira un target.
- `Assets/Scripts/Intrusions/Gaze/ObserverTrigger.cs`
  - `GazeTargetBase` concreto para la pizarra. Al completar el hold pide a `IntrusionManager.Trigger(Observer)` y aplica un cooldown (default 6s) para evitar spam.

### Regla de diseño respetada
- El jugador "mira fijamente" la pizarra durante 3.5s → disparo.
- `resetOnLookAway = true` → si rompe el contacto visual, empieza de cero. Respeta la intencionalidad.
- Evento repetible con cooldown → coherente con la necesidad de narrativa evolutiva de las intrusiones.

---

## Guia de instalacion en Unity

### Paso 1 - Agregar GazeDetector a la camara del jugador

1. En Hierarchy expande el Player hasta la `Main Camera`.
2. Selecciona la Main Camera.
3. Inspector → **Add Component** → busca `Gaze Detector`.
4. Configura:
   - `Max Distance` = 50 (o lo que cubra el aula).
   - `Hit Layers` = Everything (puedes afinarlo despues).
   - `Source Camera` = deja vacio si esta en la misma camara (se auto-asigna).
   - `Draw Gizmo` = ✓ (util para debug).
   - `Log Target Changes` = ✓ al principio para ver en Console cada vez que enfocas un target.

### Paso 2 - Crear el IntrusionManager

1. Hierarchy → click derecho espacio vacio → **Create Empty** → nombrar `IntrusionManager`.
2. **Add Component** → `Intrusion Manager`.
3. Deja los UnityEvents vacios por ahora. `Log To Console` = ✓.
4. Guarda Ctrl+S.

### Paso 3 - Montar el ObserverTrigger en la pizarra

La escena **no tiene un GameObject llamado "pizarra"**. El modelo del aula viene de Blender como un mesh unico con submaterials (entre ellos `tableroVerde.mat`). Opciones:

**Opcion A (rapida, recomendada para prototipo):**
1. Hierarchy → `GameObject → Create Empty` hijo del `Salon`.
2. Nombrarlo `PizarraTrigger`.
3. Posicionarlo sobre la pared con la pizarra (mueve el Empty con la herramienta W hasta que su pivote quede en el centro de la pizarra).
4. **Add Component → Box Collider**. Ajusta `Size` para que la Box cubra el area visible de la pizarra (por ejemplo `Size = (3, 1.5, 0.1)`).
5. **Add Component → Observer Trigger**.
   - `Hold Duration Sec` = 3.5
   - `Reset On Look Away` = ✓
   - `Cooldown Sec` = 6
6. El Box Collider **no** debe ser trigger (`Is Trigger = off`) — asi bloquea el raycast y a la vez cuenta como "algo mirable".

**Opcion B (precisa, cuando Henry separe la pizarra como submesh):**
1. Identifica el GameObject del submesh `tableroVerde` dentro del modelo del aula.
2. A ese GameObject anadele directamente el `MeshCollider` (si no lo tiene) + `ObserverTrigger`.
3. No necesitas un Empty adicional.

### Paso 4 - Probar

1. Play.
2. Mueve la camara hasta enfocar la pizarra. Con `Log Target Changes` deberias ver en Console:
   `[Gaze] Enter: PizarraTrigger`.
3. Manten la mirada 3.5 segundos sin mover demasiado. Debe salir:
   `[Intrusion] Triggered: Observer @ Xs`.
4. Aparta la mirada y vuelve a enfocar: contador se resetea, puedes volver a disparar (tras el cooldown de 6s).

### Gizmos utiles
- Selecciona la Main Camera con Play corriendo: veras la linea de raycast (amarilla = sin target, roja = target detectado).
- Selecciona `PizarraTrigger` en Scene view: veras la caja verde del BoxCollider que cubre la pizarra.

---

## Archivos creados

- `Assets/Scripts/Intrusions/IntrusionManager.cs`
- `Assets/Scripts/Intrusions/Gaze/GazeTargetBase.cs`
- `Assets/Scripts/Intrusions/Gaze/GazeDetector.cs`
- `Assets/Scripts/Intrusions/Gaze/ObserverTrigger.cs`
- `Document/Changelog/005_Skeleton_Intrusion_Observador.md`

Escena modificada: `Chapter1_Salon4B.unity` (al agregar GazeDetector, IntrusionManager y PizarraTrigger).

---

## Notas para el equipo

- **Henry (ID 10):** cuando termines el perfil de post-procesado del Observador (frio/clinico, sin grano, bordes afilados), ponte un `Volume` en la escena con ese perfil, desactivalo por defecto y suscribe su `SetActive(true)` al `On Observer` del `IntrusionManager` desde el Inspector. Igual de importante: separar la pizarra como submesh (u otro GameObject) permite usar la Opcion B del Paso 3 y evita que el BoxCollider de placeholder quede en el build final.
- **Carlos:** cuando tengas el SFX del Observador, crea un `AudioSource` en la escena, ponle el clip, desactiva Play On Awake y suscribe `AudioSource.Play` al `On Observer` del `IntrusionManager`. Para el CM3: valida que la intrusion se dispara al mirar la pizarra 3.5s y no antes; y que no se puede spamear el disparo (cooldown de 6s).
- **Duvan (yo):** el siguiente paso propio es ensamblar la **reaccion visual** del Observador dentro del `IntrusionManager.onObserver` (por ejemplo, cambiar el parametro `_Opacity` maximo de las siluetas, acelerar su `fadeSpeed`, activar un Volume), pero eso requiere las entregas de Henry.

---

## Proximos pasos (Sprint 2)

- **ID 7** ensamblado final cuando entreguen:
  - Textos de pizarra (CM2).
  - Perfil post-proc del Observador (Henry ID 10).
- **ID 3.6 Iracundo** — bloqueado por marco agrietado + audios metalicos. El `IntrusionManager.onIracundo` ya tiene su gancho listo; falta la logica de *cuando* se dispara (¿Iracundo se dispara por otro gesto distinto al gaze? revisar GDD).
- **ID 9 Infante** — bloqueado por lonchera (Henry) + cancion infantil (Carlos). Mismo patron.
>>>>>>> 0a110bca3c7d383d84ab6cc6c8aec8abc0cf9a75
