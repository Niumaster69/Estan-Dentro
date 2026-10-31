# Están Dentro — el Observador con huesos y su forma de caminar (rama de mejoras, 30-oct).
# Antes avanzaba a saltos porque el modelo (Assets/Art/Observador/Observador.fbx) era una sola malla quieta. Mixamo pide
# subir el modelo a su página con una cuenta; esto hace lo mismo sin salir de Blender:
#   - un esqueleto simple a la medida del cuerpo (piernas largas, brazos que cuelgan hasta las rodillas, cabeza caída)
#   - pesos por cercanía a cada hueso (suaves entre los dos más cercanos)
#   - dos animaciones en bucle: "Caminar" (lenta, rígida, los brazos colgando atrasados) y "Quieto" (respira y tuerce la cabeza)
# Uso:
#   blender -b --factory-startup -P Herramientas/blender/observador_camina.py -- Assets/Art/Observador/Observador.fbx Assets/Art/Observador/Observador_Rig.fbx
import bpy, math, sys
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
entrada, salida = args[0], args[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=entrada)
cuerpo = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
cuerpo.name = "Observador"

# ------------------------------------------------------------------ esqueleto (medidas sacadas de la malla: mira hacia -Y)
# piernas en x ±0,09..0,17 hasta 1,0 m; brazos en x ±0,28..0,37 de 1,6 m a 0,4 m; cuello 1,8; cabeza adelante y ladeada
HUESOS = [
    # nombre, cabeza, cola, padre
    ("Cadera",    (0, -0.05, 0.98), (0, -0.06, 1.22), None),
    ("Espalda",   (0, -0.06, 1.22), (0, -0.08, 1.45), "Cadera"),
    ("Pecho",     (0, -0.08, 1.45), (0, -0.10, 1.66), "Espalda"),
    ("Cuello",    (0, -0.10, 1.66), (0.03, -0.20, 1.86), "Pecho"),
    ("Cabeza",    (0.03, -0.20, 1.86), (0.10, -0.32, 2.17), "Cuello"),
]
for lado, s in (("L", 1), ("R", -1)):
    HUESOS += [
        ("Muslo." + lado,   (0.13 * s, -0.05, 0.98), (0.13 * s, -0.04, 0.52), "Cadera"),
        ("Pierna." + lado,  (0.13 * s, -0.04, 0.52), (0.13 * s, -0.01, 0.07), "Muslo." + lado),
        ("Pie." + lado,     (0.13 * s, -0.01, 0.07), (0.13 * s, -0.16, 0.01), "Pierna." + lado),
        ("Hombro." + lado,  (0.08 * s, -0.09, 1.62), (0.25 * s, -0.09, 1.62), "Pecho"),
        ("Brazo." + lado,   (0.27 * s, -0.09, 1.60), (0.32 * s, -0.09, 1.08), "Hombro." + lado),
        ("Antebrazo." + lado, (0.32 * s, -0.09, 1.08), (0.33 * s, -0.10, 0.62), "Brazo." + lado),
        ("Mano." + lado,    (0.33 * s, -0.10, 0.62), (0.33 * s, -0.10, 0.38), "Antebrazo." + lado),
    ]

arm_data = bpy.data.armatures.new("Esqueleto")
arm = bpy.data.objects.new("Esqueleto", arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
for n, cab, col, padre in HUESOS:
    b = arm_data.edit_bones.new(n)
    b.head, b.tail = Vector(cab), Vector(col)
    if padre: b.parent = arm_data.edit_bones[padre]; b.use_connect = False
bpy.ops.object.mode_set(mode='OBJECT')

# ------------------------------------------------------------------ pesos: los dos huesos más cercanos, suaves
def dist_segmento(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length

segs = {n: (Vector(c), Vector(t)) for n, c, t, _ in HUESOS if not n.startswith("Hombro")}
grupos = {n: cuerpo.vertex_groups.new(name=n) for n, *_ in HUESOS}
mw = cuerpo.matrix_world
for v in cuerpo.data.vertices:
    p = mw @ v.co
    cand = []
    for n, (a, b) in segs.items():
        # las piernas no toman lo de los brazos ni al revés: separados por x
        if n.startswith(("Muslo", "Pierna", "Pie")) and abs(p.x) > 0.24: continue
        if n.startswith(("Brazo", "Antebrazo", "Mano")) and abs(p.x) < 0.22 and p.z < 1.5: continue
        if n.endswith(".L") and p.x < -0.02: continue
        if n.endswith(".R") and p.x > 0.02: continue
        cand.append((dist_segmento(p, a, b), n))
    cand.sort()
    usa = cand[:2]
    pesos = [1.0 / max(d, 0.01) ** 4 for d, _ in usa]
    tot = sum(pesos)
    for (d, n), w in zip(usa, pesos):
        grupos[n].add([v.index], w / tot, 'REPLACE')

cuerpo.parent = arm
mod = cuerpo.modifiers.new("Esqueleto", 'ARMATURE'); mod.object = arm

# ------------------------------------------------------------------ animaciones
FPS = 30
bpy.context.scene.render.fps = FPS
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
for pb in arm.pose.bones: pb.rotation_mode = 'XYZ'

def limpiar():
    for pb in arm.pose.bones:
        pb.rotation_euler = (0, 0, 0); pb.location = (0, 0, 0)

def clave(f):
    for pb in arm.pose.bones:
        pb.keyframe_insert("rotation_euler", frame=f)
        if pb.name == "Cadera": pb.keyframe_insert("location", frame=f)

def nueva(nombre):
    accion = bpy.data.actions.new(nombre)
    arm.animation_data_create(); arm.animation_data.action = accion
    return accion

R = math.radians
acciones = []

# CAMINAR: 1,4 s por ciclo (dos pasos), rígido, la cadera sube y baja, los brazos cuelgan y se mecen tarde
acc = nueva("Caminar"); N = int(1.4 * FPS)
for f in range(0, N + 1, 2):
    t = f / N * 2 * math.pi
    limpiar()
    pb = arm.pose.bones
    s = math.sin(t); c = math.cos(t)
    # piernas: el muslo va de adelante a atrás; la rodilla se dobla cuando la pierna vuelve
    pb["Muslo.L"].rotation_euler = (R(-24) * s, 0, 0)
    pb["Muslo.R"].rotation_euler = (R(24) * s, 0, 0)
    pb["Pierna.L"].rotation_euler = (R(38) * max(0.0, math.sin(t + 1.2)), 0, 0)
    pb["Pierna.R"].rotation_euler = (R(38) * max(0.0, math.sin(t + 1.2 + math.pi)), 0, 0)
    pb["Pie.L"].rotation_euler = (R(-12) * max(0.0, -s), 0, 0)
    pb["Pie.R"].rotation_euler = (R(-12) * max(0.0, s), 0, 0)
    # cadera: sube en cada paso y se ladea; la espalda gira al revés (rígido)
    pb["Cadera"].location = (0, 0.03 * abs(math.sin(t)), 0)
    pb["Cadera"].rotation_euler = (0, R(5) * s, R(3) * c)
    pb["Espalda"].rotation_euler = (R(6), R(-6) * s, 0)
    pb["Pecho"].rotation_euler = (R(4), R(-4) * s, 0)
    # brazos: cuelgan, se mecen poco y tarde (lag), el codo apenas
    lag = t - 0.9
    pb["Brazo.L"].rotation_euler = (R(14) * math.sin(lag), 0, R(-3))
    pb["Brazo.R"].rotation_euler = (R(-14) * math.sin(lag), 0, R(3))
    pb["Antebrazo.L"].rotation_euler = (R(8) + R(6) * math.sin(lag - 0.6), 0, 0)
    pb["Antebrazo.R"].rotation_euler = (R(8) - R(6) * math.sin(lag - 0.6), 0, 0)
    pb["Mano.L"].rotation_euler = (R(10) * math.sin(lag - 1.1), 0, 0)
    pb["Mano.R"].rotation_euler = (R(-10) * math.sin(lag - 1.1), 0, 0)
    # cabeza: caída, se mece al revés de la cadera y da un tic en cada paso
    tic = R(7) if (f % (N // 2)) in (6, 8) else 0
    pb["Cuello"].rotation_euler = (R(8), 0, R(-4) * c)
    pb["Cabeza"].rotation_euler = (R(5), tic, R(6) * c + tic)
    clave(f)
acciones.append(acc)

# QUIETO: 3 s, respira (pecho), la cabeza se tuerce despacio y vuelve de golpe
acc = nueva("Quieto"); N = 3 * FPS
for f in range(0, N + 1, 3):
    t = f / N * 2 * math.pi
    limpiar()
    pb = arm.pose.bones
    pb["Pecho"].rotation_euler = (R(2) * math.sin(t * 2), 0, 0)
    pb["Espalda"].rotation_euler = (R(3), 0, 0)
    giro = R(16) * (f / N) if f < N * 0.85 else R(16) * (1 - (f - N * 0.85) / (N * 0.15))
    pb["Cabeza"].rotation_euler = (R(4), giro * 0.4, giro)
    pb["Cuello"].rotation_euler = (R(6), 0, giro * 0.3)
    pb["Brazo.L"].rotation_euler = (R(2) * math.sin(t), 0, R(-2))
    pb["Brazo.R"].rotation_euler = (R(-2) * math.sin(t + 1), 0, R(2))
    pb["Mano.L"].rotation_euler = (R(6) * math.sin(t * 3), 0, 0)
    clave(f)
acciones.append(acc)

bpy.ops.object.mode_set(mode='OBJECT')
# cada acción en su pista NLA: el FBX las exporta como clips separados
arm.animation_data.action = None
for a in acciones:
    a.use_fake_user = True
    tr = arm.animation_data.nla_tracks.new(); tr.name = a.name
    tr.strips.new(a.name, 1, a)
for o in bpy.context.scene.objects: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=salida, use_selection=True, add_leaf_bones=False, bake_anim=True,
                         bake_anim_use_nla_strips=True, bake_anim_use_all_actions=False, bake_anim_simplify_factor=0.0,
                         apply_scale_options='FBX_SCALE_ALL', object_types={'ARMATURE', 'MESH'})
print("LISTO", salida, [a.name for a in acciones])
