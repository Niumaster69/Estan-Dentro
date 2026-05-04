# Document/ — Indice maestro

Carpeta de documentacion del proyecto **Estan Dentro**. Cada archivo cumple un proposito distinto. Este README es el punto de entrada: si abres la carpeta y no sabes por donde empezar, lee primero esto.

---

## Estructura

```
Document/
  README.md                          ← este archivo
  PLAN_ENTREGA_FINAL.md              ← vision del juego + plan de la entrega del 29-abr
  GUIA_DE_TRABAJO_EQUIPO.md          ← onboarding (Git, LFS, identidades)
  BRIEF_CARLOS_AUDIO_CINEMATICAS.md  ← brief tecnico para Carlos (audio + cinematicas)
  Arquitectura_Sistemas.md           ← inventario de sistemas y scripts del juego
  Flujo_API_Estan_Dentro.md          ← cuando y por que se dispara cada endpoint
  Archive/                           ← docs historicos que ya cumplieron su proposito
  Changelog/                         ← registro cronologico de implementacion
    001..009 ...
```

---

## Por donde empezar segun lo que necesites

### Soy nuevo en el equipo
1. `GUIA_DE_TRABAJO_EQUIPO.md` — Git, LFS, configuracion inicial
2. `PLAN_ENTREGA_FINAL.md` seccion 0-3 — vision del juego y pivote del puzzle
3. `Arquitectura_Sistemas.md` — que sistemas existen y donde

### Voy a tocar codigo del juego
1. `Arquitectura_Sistemas.md` — donde vive cada sistema y que dispara cada uno
2. Si toco persistencia: `Flujo_API_Estan_Dentro.md`
3. Antes de commitear: revisar el ultimo `Changelog/NNN_*` para ver el formato

### Voy a tocar la API
1. `Flujo_API_Estan_Dentro.md` — endpoints, triggers, mapeo logros
2. `Changelog/008_Diseño_API_Backend_y_Esquema_BD.md` — decisiones de diseno y justificacion
3. Codigo de la API: `C:\Users\duvan\OneDrive\Escritorio\Universidad\ApiJuego\EstanDentro.Api\` (proyecto separado)

### Voy a entregar al profe
1. `PLAN_ENTREGA_FINAL.md` — referencia de alcance y pivote
2. `Changelog/008` seccion "Para la guia de entrega final" — insumos para defender decisiones

### Carlos (audio / cinematicas)
1. `BRIEF_CARLOS_AUDIO_CINEMATICAS.md` — brief completo de lo que se necesita

---

## Convenciones de documentacion

- **Changelog**: numerado `Changelog/NNN_Titulo.md`, uno por tarea o sistema, escrito al cerrar la sesion. Incluye fecha, sprint, responsable, que se hizo, bugs encontrados, estado al cierre. Ver formato en cualquier archivo existente.
- **Docs estructurales** (este README, Arquitectura, Flujo_API, Plan): viven en `Document/` directamente, sin numero, se actualizan in-place cuando algo cambia.
- **Briefs** (BRIEF_*): documentos para terceros que reciben tareas concretas. Se mantienen mientras el destinatario tenga trabajo pendiente.
- **Archive/**: aqui van los docs que ya no son operativos pero conservan valor historico (por ejemplo planes de fechas pasadas).

---

## Ultima actualizacion

2026-05-02 — Reorganizacion de carpeta + creacion de README, Arquitectura_Sistemas y Flujo_API_Estan_Dentro. Movido `PLANIFICACION_LUNES_PENDIENTES.md` a Archive.
