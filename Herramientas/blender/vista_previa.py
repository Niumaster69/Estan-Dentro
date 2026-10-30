# Vista previa rápida de un FBX (render Workbench) para revisar modelos sin abrir Blender:
#   blender -b --factory-startup -P Herramientas/blender/vista_previa.py -- entrada.fbx salida.png [x y z]  (x y z = cámara)
import bpy, sys, math
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
entrada, salida = args[0], args[1]
cam_pos = Vector(tuple(float(a) for a in args[2:5])) if len(args) >= 5 else Vector((2.6, -2.4, 1.9))

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=entrada)
objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
centro = sum((o.matrix_world @ Vector(c) for o in objs for c in o.bound_box), Vector()) / (8 * len(objs))

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
bpy.context.collection.objects.link(cam)
cam.location = cam_pos
d = centro - cam.location
cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
cam.data.lens = 35
bpy.context.scene.camera = cam

esc = bpy.context.scene
esc.render.engine = "BLENDER_WORKBENCH"
esc.display.shading.light = "STUDIO"
esc.display.shading.color_type = "MATERIAL"
esc.display.shading.show_shadows = True
esc.display.shading.show_cavity = True
esc.render.resolution_x, esc.render.resolution_y = 1280, 720
esc.render.filepath = salida
bpy.ops.render.render(write_still=True)
print("[vista_previa] ok", salida)
