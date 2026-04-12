# ESTÁN DENTRO - Guía de Trabajo en Equipo

## Equipo

| Nombre | Rol | Trabaja principalmente en |
|---|---|---|
| Henry Ortega | Artista 3D / Diseñador | Modelos, escenas, iluminación, materiales, post-procesado |
| Duvan Lozano | Programador / Scrum Master | Scripts, shaders, IA, audio, sistemas, builds |
| Carlos Mario del Valle | Narrativa / Audio / QA | Archivos de audio, textos narrativos, playtesting |

---

## PARTE 1 - Configuración Inicial (solo una vez)

### Paso 1: Instalar Git

1. Descargar Git desde https://git-scm.com/download/win
2. Instalar con las opciones por defecto (Next, Next, Next...)
3. Abrir una terminal (cmd, PowerShell o Git Bash) y verificar:

```bash
git --version
```

Si aparece algo como `git version 2.xx.x` está bien.

### Paso 2: Instalar Git LFS

Git LFS maneja los archivos pesados (modelos 3D, texturas, audio). Sin esto los archivos grandes no se descargan.

1. Descargar desde https://git-lfs.com
2. Instalar
3. En la terminal ejecutar:

```bash
git lfs install
```

### Paso 3: Configurar tu identidad en Git

Esto solo se hace una vez. Reemplaza con tu nombre y correo:

```bash
git config --global user.name "Tu Nombre"
git config --global user.email "tu@correo.com"
```

Ejemplo para Henry:

```bash
git config --global user.name "Henry Ortega"
git config --global user.email "henry@correo.com"
```

### Paso 4: Clonar el repositorio

Ir a la carpeta donde quieras tener el proyecto (por ejemplo el Escritorio) y ejecutar:

```bash
cd Desktop
git clone https://github.com/Niumaster69/Estan-Dentro.git
```

Esto crea una carpeta `Estan-Dentro` con todo el proyecto.

### Paso 5: Abrir en Unity

1. Abrir Unity Hub
2. Click en "Open" o "Add project from disk"
3. Seleccionar la carpeta `Estan-Dentro` que se creó al clonar
4. Unity va a importar todo (puede tardar unos minutos la primera vez porque regenera la carpeta Library)
5. Listo, ya puedes trabajar

---

## PARTE 2 - Flujo de Trabajo Diario

### ANTES de empezar a trabajar: SIEMPRE bajar cambios

Cada vez que vayas a trabajar, lo primero es traer los cambios que los demás hayan subido:

```bash
cd Estan-Dentro
git pull
```

Si Unity está abierto, después del pull dale click en Unity para que recargue los cambios. Si te pide reimportar, acepta.

### MIENTRAS trabajas

Trabaja normal en Unity. Guarda tus cambios frecuentemente (Ctrl+S en Unity).

### CUANDO termines: subir tus cambios

#### Paso 1 - Ver qué cambió

```bash
git status
```

Esto te muestra en rojo los archivos que modificaste o creaste.

#### Paso 2 - Agregar los archivos que quieres subir

Para agregar archivos específicos:

```bash
git add Assets/Art/Models/Pupitre.fbx
git add Assets/Art/Models/Pupitre.fbx.meta
```

Para agregar todo lo que cambió:

```bash
git add -A
```

#### Paso 3 - Crear un commit (guardar el cambio con un mensaje)

```bash
git commit -m "Descripcion corta de lo que hiciste"
```

Ejemplos de buenos mensajes:

```bash
git commit -m "Agregar modelo de pupitre doble con 3 variantes de desgaste"
git commit -m "Implementar movimiento WASD y camara con raton"
git commit -m "Agregar audio de respiracion infantil y goteo organico"
```

#### Paso 4 - Subir al repositorio

```bash
git push
```

### Resumen rápido del flujo diario

```
git pull                          ← Bajar cambios de los demás
... trabajar en Unity ...
git add -A                        ← Preparar mis cambios
git commit -m "Lo que hice"       ← Guardar con mensaje
git push                          ← Subir al repositorio
```

---

## PARTE 3 - Estructura de Carpetas

Cada quien trabaja en carpetas específicas. Esto evita conflictos:

```
Assets/
├── Scenes/
│   ├── MainMenu/               → Escena del menú principal
│   ├── Chapter1_Salon4B/       → Escena principal del Capítulo 1
│   └── Dev/                    → Escenas de prueba personales
│       ├── Dev_Henry.unity     → Solo Henry la toca
│       └── Dev_Duvan.unity     → Solo Duvan la toca
│
├── Scripts/                    → DUVAN trabaja aquí
│   ├── Player/                 → Movimiento, cámara, controles
│   ├── Intrusions/             → Observador, Iracundo, Infante
│   ├── AI/                     → El Que Se Sienta Delante
│   ├── Audio/                  → Sistemas de audio
│   ├── Cinematics/             → Cinemáticas
│   ├── Puzzle/                 → Lógica del puzzle
│   └── UI/                     → Menús, pausa, opciones
│
├── Art/                        → HENRY trabaja aquí
│   ├── Models/                 → Modelos 3D (.fbx, .blend)
│   ├── Materials/              → Materiales de Unity
│   ├── Textures/               → Texturas (.png, .psd, .tga)
│   ├── Shaders/                → Shaders visuales
│   └── Animations/             → Animaciones
│
├── Audio/                      → CARLOS MARIO trabaja aquí
│   ├── SFX/                    → Efectos: golpes, susurros, chasquidos
│   ├── Ambient/                → Loops: ambiente aula, subliminal
│   └── Cinematics/             → Audio de cinemáticas
│
├── Prefabs/                    → Objetos reutilizables (todos)
│   ├── Environment/            → Pupitres, sillas, armarios, pizarra
│   ├── Interactables/          → Lonchera, llave, candado
│   └── FX/                     → Efectos visuales
│
├── PostProcessing/             → Perfiles visuales por identidad
├── Resources/                  → Assets cargados en runtime
└── Settings/                   → Configuración HDRP (no tocar)
```

---

## PARTE 4 - Reglas para Evitar Conflictos

### Regla 1: NUNCA dos personas editando la misma escena

Las escenas de Unity (.unity) son archivos gigantes. Si dos personas las editan al tiempo, Git no puede juntar los cambios y se genera un conflicto muy difícil de resolver.

**Solución:** Avisar antes de tocar una escena.

```
Henry: "Voy a trabajar en Chapter1_Salon4B"
Duvan: "Dale, no la toco hasta que termines"
...
Henry: "Listo, hice push"
Duvan: "Hago pull, ya la tengo"
```

### Regla 2: Usar Prefabs para todo

En lugar de armar objetos directo en la escena, crear Prefabs:

1. Henry modela un pupitre y lo mete como Prefab en `Prefabs/Environment/`
2. Henry arrastra el Prefab a la escena para posicionarlo
3. Duvan puede abrir el Prefab (doble click), agregarle scripts, y guardar
4. Los cambios del Prefab se reflejan automáticamente en la escena

Esto permite que cada uno trabaje en su parte sin tocar la escena.

### Regla 3: Escenas de prueba personales

Cada uno puede crear su propia escena en `Scenes/Dev/` para probar cosas:

- `Dev_Henry.unity` → Henry prueba iluminación, posiciones
- `Dev_Duvan.unity` → Duvan prueba scripts, mecánicas

Nadie toca la escena del otro.

### Regla 4: Siempre hacer pull antes de trabajar

Si no haces pull y trabajas sobre una versión vieja, cuando intentes push Git te va a rechazar. Siempre:

```bash
git pull
```

ANTES de empezar.

### Regla 5: Incluir los archivos .meta

Unity crea archivos `.meta` junto a cada archivo. **Siempre hay que subirlos.** Si subes un modelo sin su `.meta`, a los demás les va a dar error.

```bash
# BIEN - subir el archivo Y su meta
git add Assets/Art/Models/Pupitre.fbx
git add Assets/Art/Models/Pupitre.fbx.meta

# MÁS FÁCIL - subir todo
git add -A
```

---

## PARTE 5 - Qué hacer si algo sale mal

### "git push me dice que hay cambios remotos"

Alguien subió cambios mientras trabajabas. Solución:

```bash
git pull
```

Si no hay conflicto, Git junta los cambios automáticamente. Después haz push de nuevo.

### "Tengo un conflicto en una escena .unity"

Esto pasa si dos personas editaron la misma escena. Opciones:

**Opción A** - Quedarse con la versión del otro (perder tus cambios en la escena):

```bash
git checkout --theirs Assets/Scenes/Chapter1_Salon4B/Salon4B.unity
git add Assets/Scenes/Chapter1_Salon4B/Salon4B.unity
git commit -m "Resolver conflicto: tomar escena del compañero"
```

**Opción B** - Quedarse con tu versión (el otro pierde sus cambios en la escena):

```bash
git checkout --ours Assets/Scenes/Chapter1_Salon4B/Salon4B.unity
git add Assets/Scenes/Chapter1_Salon4B/Salon4B.unity
git commit -m "Resolver conflicto: mantener mi escena"
```

Lo mejor es que **esto nunca pase** si siguen la Regla 1.

### "Unity me muestra errores después de hacer pull"

1. Cerrar Unity
2. Borrar la carpeta `Library/` dentro del proyecto
3. Abrir Unity de nuevo (va a reimportar todo, tarda un poco)

### "Subí algo que no debía"

Avisa al grupo. Duvan puede revertir el cambio con Git.

---

## PARTE 6 - Changelog (Registro de Cambios)

Cada tarea completada se documenta en `Document/Changelog/` con un número secuencial:

```
Document/Changelog/
├── 001_Estructuracion_Proyecto_y_Repositorio.md
├── 002_FPS_Controller.md
├── 003_Blockout_Salon_4B.md
├── ...
```

Cada archivo debe incluir:

```markdown
# NNN - Nombre de la tarea

**Fecha:** AAAA-MM-DD
**Sprint:** N - Nombre del sprint
**Responsable:** Nombre

## Qué se hizo
- Punto 1
- Punto 2

## Archivos creados o modificados
- ruta/al/archivo

## Notas para el equipo
- Lo que los demás necesiten saber
```

---

## PARTE 7 - Referencia Rápida de Comandos

| Quiero... | Comando |
|---|---|
| Bajar cambios | `git pull` |
| Ver qué cambié | `git status` |
| Agregar todo | `git add -A` |
| Agregar un archivo | `git add ruta/archivo` |
| Guardar cambio con mensaje | `git commit -m "mensaje"` |
| Subir cambios | `git push` |
| Ver historial de cambios | `git log --oneline` |
| Ver quién cambió un archivo | `git log --oneline ruta/archivo` |

---

## Contacto y Comunicación

- Antes de editar una escena → avisar en el grupo
- Si algo se rompe → avisar a Duvan
- Dudas sobre Git → leer esta guía o preguntar a Duvan
- Bugs encontrados → Carlos Mario los documenta en el reporte de QA
