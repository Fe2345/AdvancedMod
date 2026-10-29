"""在已打开用户水龙卷模型的 Blender 中执行，生成独立像素动画场景。"""
import bpy
import bmesh
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/assets/water-sprite-top'
OUT.mkdir(parents=True, exist_ok=True)
FRAMES = 24
WIDTH, HEIGHT = 400, 400
SOURCE_FRAME = 33

def linear(rgb):
    return tuple(v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in rgb)

def color(hexcode):
    return (*linear(tuple(int(hexcode[i:i+2], 16) / 255 for i in (0, 2, 4))), 1)

source = bpy.data.objects['Cube'].users_scene[0]
bpy.context.window.scene = source
source.frame_set(SOURCE_FRAME)
obj = bpy.data.objects['Cube']
evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
mesh = bpy.data.meshes.new_from_object(evaluated)
scene = bpy.data.scenes.new('WaterSprite_24')
bpy.context.window.scene = scene
scene.render.engine = source.render.engine
scene.render.resolution_x = WIDTH
scene.render.resolution_y = HEIGHT
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.color_depth = '8'
scene.render.fps = FRAMES
scene.frame_start, scene.frame_end = 1, FRAMES
try:
    scene.view_settings.view_transform = 'Standard'
except TypeError:
    pass
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1

# 按原始几何节点的三个材质层拆分；保留源场景及原材质。
palettes = [
    ['167ba6', '249bc3', '47bdd9', '88deec', 'd0f7fa'],
    ['123b65', '195581', '246f9d', '318cb3', '49aac8'],
    ['176a9d', '208dba', '35afd0', '68cfe5', 'adf0f4'],
]
turns = [1, 2, 1]
for index, palette in enumerate(palettes):
    layer_mesh = mesh.copy()
    bm = bmesh.new()
    bm.from_mesh(layer_mesh)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != index], context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(layer_mesh)
    bm.free()
    layer = bpy.data.objects.new(f'WaterLayer_{index}', layer_mesh)
    scene.collection.objects.link(layer)
    layer.matrix_world = obj.matrix_world.copy()
    mat = bpy.data.materials.new(f'WaterPalette_{index}')
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    geo = nodes.new('ShaderNodeNewGeometry')
    dot = nodes.new('ShaderNodeVectorMath')
    dot.operation = 'DOT_PRODUCT'
    dot.inputs[1].default_value = (-.4, -.6, .7)
    remap = nodes.new('ShaderNodeMapRange')
    remap.inputs['From Min'].default_value = -1
    remap.inputs['From Max'].default_value = 1
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    for e in list(ramp.color_ramp.elements)[1:]:
        ramp.color_ramp.elements.remove(e)
    for i, hexcode in enumerate(palette):
        e = ramp.color_ramp.elements[0] if i == 0 else ramp.color_ramp.elements.new(i / len(palette))
        e.color = color(hexcode)
    emission = nodes.new('ShaderNodeEmission')
    emission.inputs['Strength'].default_value = 1
    output = nodes.new('ShaderNodeOutputMaterial')
    links.new(geo.outputs['Normal'], dot.inputs[0])
    links.new(dot.outputs['Value'], remap.inputs['Value'])
    links.new(remap.outputs['Result'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], emission.inputs['Color'])
    links.new(emission.outputs[0], output.inputs['Surface'])
    layer_mesh.materials.clear()
    layer_mesh.materials.append(mat)
    for poly in layer_mesh.polygons:
        poly.material_index = 0
        poly.use_smooth = True
    driver = layer.driver_add('rotation_euler', 2).driver
    driver.expression = f'-2*pi*(frame-1)/{FRAMES}*{turns[index]}'

bpy.data.meshes.remove(mesh)
camera_data = bpy.data.cameras.new('WaterSpriteCamera')
camera_data.type = 'ORTHO'
radius = max(math.hypot(v.co.x, v.co.y) for o in scene.objects if o.type == 'MESH' for v in o.data.vertices)
camera_data.ortho_scale = 2 * radius * 1.08
camera = bpy.data.objects.new('WaterSpriteCamera', camera_data)
scene.collection.objects.link(camera)
camera.location = (0, 0, 30)
camera.rotation_euler = (0, 0, 0)
scene.camera = camera
scene.frame_set(1)
scene.render.filepath = str(OUT / 'raw' / 'frame_')
(OUT / 'raw').mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'WaterTornado-Top-400.blend'), copy=True)
for frame in range(1, FRAMES + 2):
    scene.frame_set(frame)
    scene.render.filepath = str(OUT / 'raw' / f'frame_{frame:04d}.png')
    bpy.ops.render.render(write_still=True)
scene.frame_set(1)
print('准备完成', str(OUT), '单帧', WIDTH, HEIGHT)
