# 001 - Estructuración del Proyecto y Repositorio

**Fecha:** 2026-04-12  
**Sprint:** 1 - Fundamentos y Setup  
**Responsable:** Duvan Lozano  
**Tarea relacionada:** ID 3 - Configurar proyecto Unity HDRP, estructura de carpetas, Git

---

## Qué se hizo

### 1. Estructura de carpetas del proyecto
Se organizó la carpeta `Assets/` con la siguiente estructura profesional para Unity:

```
Assets/
├── Scenes/
│   ├── MainMenu/           → Escena del menú principal
│   ├── Chapter1_Salon4B/   → Escena principal del Capítulo 1
│   └── Dev/                → Escenas de prueba personales (no tocar las de otros)
├── Scripts/
│   ├── Player/             → FPS controller, cámara, movimiento
│   ├── Intrusions/         → Sistemas de intrusión (Observador, Iracundo, Infante)
│   ├── AI/                 → IA de El Que Se Sienta Delante
│   ├── Audio/              → Sistemas de audio, loops, espacialización
│   ├── Cinematics/         → Cinemáticas (Antes del Silencio, Después del Ruido)
│   ├── Puzzle/             → Lógica del puzzle de perspectiva cruzada
│   └── UI/                 → Menús, pausa, opciones
├── Art/
│   ├── Models/             → Modelos 3D (.fbx, .blend)
│   ├── Materials/          → Materiales de Unity
│   ├── Textures/           → Texturas (.png, .psd, .tga)
│   ├── Shaders/            → Shaders personalizados (siluetas, reflejos, etc.)
│   └── Animations/         → Animaciones
├── Audio/
│   ├── SFX/                → Efectos de sonido puntuales
│   ├── Ambient/            → Loops ambientales y subliminal
│   └── Cinematics/         → Audio de cinemáticas
├── Prefabs/
│   ├── Environment/        → Pupitres, sillas, armarios, pizarra, ventanas
│   ├── Interactables/      → Lonchera, llave, candado, conducto
│   └── FX/                 → Efectos visuales y partículas
├── PostProcessing/         → Perfiles: Neutro, Observador, Iracundo, Infante
├── Resources/              → Assets cargados en runtime
└── Settings/               → Configuración HDRP (ya existía)
```

### 2. Repositorio Git inicializado
- Se creó `.gitignore` para Unity (ignora Library/, Temp/, Logs/, archivos de IDE, etc.)
- Se creó `.gitattributes` con Git LFS para archivos pesados (modelos 3D, texturas, audio)
- Se configuró Unity YAML merge para escenas y prefabs

### 3. Sistema de changelog
- Se creó `Document/Changelog/` para documentar cada tarea completada
- Cada archivo se numera secuencialmente: `001_`, `002_`, `003_`...

---

## Reglas de trabajo en equipo con Git

### Escenas (MUY IMPORTANTE)
- **Nunca dos personas editan la misma escena al mismo tiempo**
- Antes de editar una escena, avisar en el grupo
- Usar la carpeta `Scenes/Dev/` para pruebas personales (ej: `Dev_Duvan.unity`, `Dev_Henry.unity`)

### Prefabs
- Henry: crear objetos como Prefabs en `Prefabs/` y asignar posición en la escena
- Duvan: crear scripts en `Scripts/` y asignarlos a los Prefabs sin tocar la escena

### Audio
- Carlos Mario: dejar los archivos de audio editados en `Audio/SFX/`, `Audio/Ambient/` o `Audio/Cinematics/`
- Duvan: integrar los audios en los sistemas de Unity

### Flujo de Git
1. `git pull` antes de empezar a trabajar
2. Trabajar en tu parte
3. `git add` + `git commit` con mensaje descriptivo
4. `git push`
5. Si hay conflicto en escena → la persona que la estaba editando primero tiene prioridad

---

## Próximo paso
- Conectar con repositorio remoto en GitHub
- Henry y Carlos Mario hacen `git clone` del repo
- Empezar con las tareas individuales del Sprint 1
