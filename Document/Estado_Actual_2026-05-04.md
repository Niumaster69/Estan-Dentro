# Estado actual del proyecto — 2026-05-04

Documento corte de progreso al cierre del Sprint 3. Resume narrativa, flujo de juego, sistemas implementados y lo que falta.

---

## 1. Narrativa del Capitulo 1 (en una linea)

El jugador despierta en el salon 4B del colegio sin saber por que esta encerrado. Resolviendo pistas dejadas por sus padres descifra dos cerraduras (casillero y lonchera), recupera una linterna que revela un mensaje oculto en la pared, y trepa al ducto del techo para escapar al Acto 2.

Doc canonico de narrativa: `Document/Diseño_Narrativo_Capitulo1.md`.

Datos clave:
- Apellido del personaje: **Mendoza**
- Codigo casillero: **142**
- Codigo lonchera: **046**
- Twist: **el padre esta vivo** (revelado en flashbacks futuros)

---

## 2. Flujo de juego — Acto 1 (Salon Principal)

```
1. Wake-up cinematico → "Sal del salon"
2. Player explora, encuentra:
   - Carta 1, Carta 2 (en mesas)
   - Pista de Ma (necesita abrir casillero) → codigo 142
3. Casillero abre → entrega DESTORNILLADOR + Pista de Pa
4. Pista de Pa indica codigo 046 → lonchera
5. Lonchera abre → entrega LINTERNA + LIGHTS-OUT (blackout cinematico)
6. Linterna apunta a la pared correcta → revela "Subi, hijo. Todavia estas abajo."
7. Player apunta al ducto del techo + tiene destornillador → activa cinematica:
   a. Mira arriba al ducto
   b. Destornillador suena (afloja tornillos)
   c. Caida de la rejilla por fisica
   d. Mesa rueda al lugar (sliding cinematico)
   e. Player trepa al escritorio
   f. Player se mete en el ducto
   g. Fade + slides + loading
8. Carga Acto 2 (Ductos de ventilacion)
```

Cinematica orquestada por `DuctoTechoInteractable.cs` con coreografia de camara (bob, wobble, breathing, flinch, head-tilt al destornillar).

---

## 3. Sistemas implementados (lo que ya funciona)

### Bloque A — Decisiones operativas
- Settings centralizados, Pause menu, Overlays con `OverlayBlocker`.

### Bloque B — Rediseño UX
- Menu principal cinematografico (estilo Outlast/Visage) con Ken Burns + glitch.
- Pantalla de carga con tips temáticos.
- Calibracion de mic integrada al click "Jugar".
- Inventory unificado con tabs (Items / Notas / Misiones).
- Item preview 3D rotatorio via RenderTexture.
- HUD de objetivo con circulo top-right (clickeable) + toast bottom.

### Bloque C — Narrativa Cap 1
- C.1 HUD de objetivo "Sal del salon".
- C.2 Inventory items + Flashlight (linterna).
- C.3 LightsController + HiddenMessage (mensaje oculto en pared, visible con linterna).
- C.4 Casillero (CombinationLock + Animator + LockedContainer).
- C.5 Lonchera (mismo sistema + trigger blackout).
- C.6 Ducto del techo + cinematica de subida orquestada.

### Bloque E — Persistencia API
Ver `Como_Conectamos_API.md` para detalles. Resumen:
- API backend en somee con 5 entidades (Jugador, Partida, Logro, Tema, LogroXPartida).
- Cliente Unity en `Assets/Scripts/Network/`: ApiClient, ApiBootstrapper, GameSession, ChapterFlow, Dtos.
- Auto-perfil silencioso al primer arranque (cero friccion).
- 5 logros con triggers automaticos: cerradura sin fallar, leer 3 notas, sobrevivir sin colapsar, ciclos de respiracion sin fallar, no disparar al observador.
- Save games: menu con "Nueva partida" / "Continuar" + lista de partidas con [Retomar] / [Borrar].

---

## 4. Pendientes

### Crítico (bloquean entrega)
- **Audio (Carlos)**: faltan clips de mesa rodando, destornillador, ducto cayendo, ambient salon, voces. Brief listo en `BRIEF_CARLOS_AUDIO_CINEMATICAS.md`.
- **Acto 2 (Ductos de ventilacion)**: escena cargada pero sin gameplay. Definir mecanica de gateo + breathing minigame.
- **Acto 3 (Final del capitulo)**: no implementado. Aqui se dispara `ChapterFlow.EndChapter(true)` + `EndOfChapterOverlay`.

### Importante (no bloquean pero quedan feos sin esto)
- **Bloque D — Cinematicas Flashback**: triggers en lugares clave del salon que activan recuerdos.
- **Cinematica intro** (postpuesta desde Sprint 2).
- **Lista de indicios visuales para Henry**: que props/decoración 3D necesita modelar.

### Bug pendiente bajo investigacion
- Usuario reporto: al "Retomar" partida, parece crear una partida nueva. Pendiente diagnosticar con logs (mi codigo de Resume NO crea partida — probablemente click accidental en "Nueva partida" o malentendido).

---

## 5. Como esta organizado el codigo

```
Assets/
├── Scripts/
│   ├── Breathing/        — minijuego de respiracion + StressSystem hook
│   ├── Editor/           — editor scripts (spawneo de cartas)
│   ├── Interaction/      — Note, CombinationLock, Flashlight, HiddenMessage,
│   │                       LockedContainer, DuctoTechoInteractable, SimpleOpenable
│   ├── Intrusions/       — Observador (gaze trigger) + IntrusionManager
│   ├── Inventory/        — Inventory + InventoryOverlay + ItemPreviewOverlay
│   ├── Network/          — ApiClient, ApiBootstrapper, GameSession, ChapterFlow, Dtos
│   ├── Player/           — PlayerController, PlayerWakeUp
│   ├── Stress/           — StressSystem (con hook de logro)
│   └── UI/               — MainMenu, Settings, Pause, ObjectiveHUD,
│                            EndOfChapterOverlay, ProfileOverlay, PartidasOverlay,
│                            LoadingScreen, CinematicController, OverlayBlocker
└── Scenes/Level-1/       — 3 actos del Capitulo 1
```

---

## 6. Equipo

- **Duvan**: mecanicas, FX, integracion API, UI flows
- **Henry**: 3D, post-procesado, animaciones FBX
- **Carlos**: audio, QA

---

## Proximas sesiones

Orden recomendado:
1. **Esperar audios de Carlos** y enchufarlos en Inspector.
2. **Acto 2 gameplay**: mecanica de ductos + breathing minigame integrado.
3. **Acto 3 gameplay** y trigger de fin de capitulo + `EndOfChapterOverlay`.
4. **Cinematicas Flashback** (Bloque D).
5. **Cinematica intro** (postpuesta).
