# EstÃ¡n Dentro â€” piezas del cuarto del hospital que no hay en Poly Haven (final del capÃ­tulo 1).
# Se corre sin abrir Blender:
#   blender -b --factory-startup -P Herramientas/blender/hospital_props.py -- Assets/Art/Hospital/Hospital_Props.fbx
# Cada pieza queda en el origen con su propio nombre; Unity (Editor/Mejoras/MejorasEscena.cs) las coloca sobre la cama.
import bpy, bmesh, math, sys
from mathutils import Vector

salida = sys.argv[sys.argv.index("--") + 1]

bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(nombre, color, rough=0.8, emision=None):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Roughness"].default_value = rough
    if emision:
        p.inputs["Emission Color"].default_value = (*emision, 1)
        p.inputs["Emission Strength"].default_value = 1.0
    return m

M_SABANA = mat("Hosp_Sabana", (0.82, 0.84, 0.86), 0.9)
M_COLCHON = mat("Hosp_Colchon", (0.55, 0.62, 0.66), 0.7)
M_PIEL = mat("Hosp_Piel", (0.72, 0.52, 0.42), 0.6)
M_PELO = mat("Hosp_Pelo", (0.05, 0.04, 0.035), 0.7)
M_PLASTICO = mat("Hosp_Plastico", (0.78, 0.78, 0.76), 0.45)
M_METAL = mat("Hosp_Metal", (0.62, 0.63, 0.65), 0.3)
M_PANTALLA = mat("Hosp_Pantalla", (0.0, 0.02, 0.01), 0.2, emision=(0.05, 0.9, 0.45))
M_BOLSA = mat("Hosp_Bolsa", (0.85, 0.9, 0.92), 0.15)


def obj_de_bm(nombre, bm, material, suave=True):
    me = bpy.data.meshes.new(nombre)
    bm.to_mesh(me); bm.free()
    if suave:
        for pol in me.polygons: pol.use_smooth = True
    o = bpy.data.objects.new(nombre, me)
    bpy.context.collection.objects.link(o)
    o.data.materials.append(material)
    return o


def caja(nombre, tam, material, bisel=0.0, centro=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * tam[0], v.co.y * tam[1], v.co.z * tam[2])) + Vector(centro)
    if bisel > 0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bisel, segments=3, affect='EDGES')
    return obj_de_bm(nombre, bm, material, suave=bisel > 0)


def cilindro(nombre, radio, alto, material, centro=(0, 0, 0), lados=24):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=lados, radius1=radio, radius2=radio, depth=alto)
    for v in bm.verts: v.co += Vector(centro)
    return obj_de_bm(nombre, bm, material)


def esfera(nombre, radios, material, centro=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * radios[0], v.co.y * radios[1], v.co.z * radios[2])) + Vector(centro)
    return obj_de_bm(nombre, bm, material)


# ---------------- cama: colchÃ³n, almohada, sÃ¡bana con la silueta de Mateo (largo en X, cabeza hacia -X) -------------
L, A = 1.95, 0.9                      # largo y ancho del colchÃ³n
caja("Colchon", (L, A, 0.16), M_COLCHON, bisel=0.03, centro=(0, 0, 0.08))
almohada = esfera("Almohada", (0.26, 0.36, 0.07), M_SABANA, centro=(-L / 2 + 0.3, 0, 0.21))


def alto_cuerpo(u, v):
    """Relieve del cuerpo bajo la sÃ¡bana. u: 0 = cabecera, 1 = pies; v: -0.5..0.5 a lo ancho."""
    h = 0.0
    def bulto(cu, cv, su, sv, alto):
        return alto * math.exp(-((u - cu) / su) ** 2 - ((v - cv) / sv) ** 2)
    h += bulto(0.36, 0.0, 0.14, 0.20, 0.17)     # pecho y abdomen
    h += bulto(0.52, 0.0, 0.10, 0.22, 0.12)     # cadera
    h += bulto(0.72, -0.09, 0.16, 0.07, 0.09)   # pierna
    h += bulto(0.72, 0.09, 0.16, 0.07, 0.09)
    h += bulto(0.95, -0.08, 0.03, 0.05, 0.13)   # pies
    h += bulto(0.95, 0.08, 0.03, 0.05, 0.13)
    h += bulto(0.30, -0.27, 0.12, 0.05, 0.07)   # brazos a los lados
    h += bulto(0.30, 0.27, 0.12, 0.05, 0.07)
    return h


# sÃ¡bana: malla que cubre desde el pecho hasta los pies y cuelga por los lados
bm = bmesh.new()
NU, NV = 70, 40
u0, u1 = 0.2, 1.06                       # empieza bajo los hombros y pasa un poco del borde de los pies
v0, v1 = -0.72, 0.72                      # mÃ¡s ancha que el colchÃ³n: cuelga
filas = []
for i in range(NU + 1):
    u = u0 + (u1 - u0) * i / NU
    fila = []
    for j in range(NV + 1):
        v = v0 + (v1 - v0) * j / NV
        x = -L / 2 + u * L
        y = v * A
        z = 0.18 + alto_cuerpo(min(u, 1.0), v)
        afuera = max(0.0, abs(v) - 0.5) / 0.22   # lo que sobra cuelga por el lado
        if afuera > 0:
            y = math.copysign(A / 2 + 0.02 + 0.03 * afuera, v)
            z = 0.18 - 0.3 * afuera + 0.008 * math.sin(u * 22)
        if u > 1.0:                                # y por los pies
            x = L / 2 + 0.02
            z = min(z, 0.18) - (u - 1.0) / 0.06 * 0.25
        z += 0.004 * math.sin(u * 23 + v * 9) * math.cos(v * 17)   # arrugas suaves
        fila.append(bm.verts.new((x, y, z)))
    filas.append(fila)
for i in range(NU):
    for j in range(NV):
        bm.faces.new((filas[i][j], filas[i + 1][j], filas[i + 1][j + 1], filas[i][j + 1]))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
sab = obj_de_bm("Sabana", bm, M_SABANA)
mod = sab.modifiers.new("grosor", "SOLIDIFY"); mod.thickness = 0.01

# cabeza en la almohada, de perfil hacia el techo (se ve de lejos y con poca luz)
esfera("Cabeza", (0.11, 0.085, 0.1), M_PIEL, centro=(-L / 2 + 0.33, 0.0, 0.33))
esfera("Pelo", (0.115, 0.092, 0.07), M_PELO, centro=(-L / 2 + 0.29, 0.0, 0.37))

# ---------------- monitor de signos vitales sobre su pie ----------------
caja("Monitor_Cuerpo", (0.36, 0.14, 0.28), M_PLASTICO, bisel=0.015, centro=(0, 0, 1.2))
# la pantalla es un plano con UV (la textura del pulso corre sobre ella en Unity: Colegio/MonitorECG.cs)
bm = bmesh.new()
bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5, calc_uvs=True)
for v in bm.verts:
    x, y = v.co.x * 0.29, v.co.y * 0.2
    v.co = Vector((x, -0.0715, 1.21 + y))          # de pie, mirando hacia -Y (adelante del monitor)
obj_de_bm("Monitor_Pantalla", bm, M_PANTALLA, suave=False)
cilindro("Monitor_Pie", 0.018, 1.06, M_METAL, centro=(0, 0, 0.53))
for k in range(5):                                           # base de cinco patas
    a = k * 2 * math.pi / 5
    p = caja(f"Monitor_Pata{k}", (0.26, 0.025, 0.02), M_METAL, centro=(0.13, 0, 0.03))
    p.rotation_euler[2] = a
    p.name = f"Monitor_Pata{k}"

# ---------------- suero: palo, gancho, bolsa y tubo ----------------
cilindro("Suero_Palo", 0.012, 1.95, M_METAL, centro=(0, 0, 0.975))
caja("Suero_Gancho", (0.22, 0.01, 0.01), M_METAL, centro=(0, 0, 1.92))
esfera("Suero_Bolsa", (0.06, 0.025, 0.11), M_BOLSA, centro=(0.09, 0, 1.78))
curva = bpy.data.curves.new("Suero_TuboCurva", "CURVE"); curva.dimensions = "3D"
sp = curva.splines.new("BEZIER"); sp.bezier_points.add(2)
for bp, co in zip(sp.bezier_points, [(0.09, 0, 1.66), (0.25, -0.2, 1.0), (0.55, -0.45, 0.62)]):
    bp.co = co; bp.handle_left_type = bp.handle_right_type = "AUTO"
curva.bevel_depth = 0.003
tubo = bpy.data.objects.new("Suero_Tubo", curva); bpy.context.collection.objects.link(tubo)
tubo.data.materials.append(M_BOLSA)
for k in range(4):
    a = k * math.pi / 2
    p = caja(f"Suero_Pata{k}", (0.22, 0.02, 0.015), M_METAL, centro=(0.11, 0, 0.02))
    p.rotation_euler[2] = a

# todo a malla (el tubo es curva) y exportar
for o in list(bpy.context.scene.objects):
    o.select_set(True)
bpy.context.view_layer.objects.active = sab
bpy.ops.object.convert(target="MESH")
bpy.ops.export_scene.fbx(filepath=salida, use_selection=False, object_types={"MESH"},
                         apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                         bake_space_transform=True, mesh_smooth_type="FACE", use_mesh_modifiers=True)
print("[hospital_props] exportado", salida, len(bpy.context.scene.objects), "objetos")
