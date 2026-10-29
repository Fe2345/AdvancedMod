"""保留原几何节点，在副本裁去外围水面并配置俯视水色循环。"""
import bpy
import math
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / 'docs/assets/water-vortex-model'
OUT.mkdir(parents=True, exist_ok=True)
FRAMES, SIZE, RADIUS = 24, 400, .78

def enum(obj, prop, value):
    values = [i.identifier for i in obj.bl_rna.properties[prop].enum_items]
    if value not in values:
        raise ValueError((prop, value, values))
    setattr(obj, prop, value)

source = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type == 'NODES' for m in o.modifiers))
scene = bpy.data.scenes.new('WaterVortex_Clean_24')
obj = source.copy()
obj.data = source.data.copy()
obj.name = 'WaterVortex_Only'
scene.collection.objects.link(obj)
modifier = next(m for m in obj.modifiers if m.type == 'NODES')
modifier.node_group = modifier.node_group.copy()
group = modifier.node_group
group.name = 'WaterVortex_Trimmed_Loop24'
nodes, links = group.nodes, group.links
grid = next(n for n in nodes if n.bl_idname == 'GeometryNodeMeshGrid')
grid.inputs['Vertices X'].default_value = 501
grid.inputs['Vertices Y'].default_value = 501
time = next(n for n in nodes if n.type == 'GROUP' and n.node_tree.name == 'G_Time Info')
time.inputs['Factor'].default_value = FRAMES / (2 * math.pi)
time.inputs['Offset'].default_value = -1
time.inputs['Rel Offset'].default_value = True
# 对帧号取模，避免浮点 2π 的微小残差造成高频细纹在接缝处变化。
phase = nodes.new('ShaderNodeValue')
phase.label = '24帧循环相位（逐圈精确归零）'
phase.outputs[0].driver_add('default_value').driver.expression = '2*pi*((frame-1)%24)/24'
for link in list(time.outputs['Time'].links):
    target = link.to_socket
    links.remove(link)
    links.new(phase.outputs[0], target)
output = next(n for n in nodes if n.type == 'GROUP_OUTPUT')
prior = output.inputs['Geometry'].links[0].from_socket
position = nodes.new('GeometryNodeInputPosition')
separate = nodes.new('ShaderNodeSeparateXYZ')
combine = nodes.new('ShaderNodeCombineXYZ')
length = nodes.new('ShaderNodeVectorMath')
enum(length, 'operation', 'LENGTH')
compare = nodes.new('ShaderNodeMath')
enum(compare, 'operation', 'GREATER_THAN')
compare.inputs[1].default_value = RADIUS
compare.label = '外围水面裁切半径（可调）'
delete = nodes.new('GeometryNodeDeleteGeometry')
enum(delete, 'domain', 'FACE')
links.new(position.outputs[0], separate.inputs[0])
links.new(separate.outputs['X'], combine.inputs['X'])
links.new(separate.outputs['Y'], combine.inputs['Y'])
links.new(combine.outputs[0], length.inputs[0])
links.new(length.outputs['Value'], compare.inputs[0])
links.new(compare.outputs[0], delete.inputs['Selection'])
links.new(prior, delete.inputs['Geometry'])
smooth = nodes.new('GeometryNodeSetShadeSmooth')
links.new(delete.outputs[0], smooth.inputs['Geometry'])

palette = ['10345e', '145581', '18769f', '249bbb', '41bdd3', '78dae5', 'c6f4f5']
material = bpy.data.materials.new('WaterVortex_Blue')
material.use_nodes = True
mn, ml = material.node_tree.nodes, material.node_tree.links
mn.clear()
geo = mn.new('ShaderNodeNewGeometry')
xyz = mn.new('ShaderNodeSeparateXYZ')
depth = mn.new('ShaderNodeMapRange')
depth.inputs['From Min'].default_value = -3.28
depth.inputs['From Max'].default_value = .05
depth.inputs['To Min'].default_value = .03
depth.inputs['To Max'].default_value = .58
dot = mn.new('ShaderNodeVectorMath')
enum(dot, 'operation', 'DOT_PRODUCT')
dot.inputs[1].default_value = (-.45, -.55, .7)
normal = mn.new('ShaderNodeMath')
enum(normal, 'operation', 'MULTIPLY')
normal.inputs[1].default_value = .4
add = mn.new('ShaderNodeMath')
enum(add, 'operation', 'ADD')
ramp = mn.new('ShaderNodeValToRGB')
enum(ramp.color_ramp, 'interpolation', 'CONSTANT')
for e in list(ramp.color_ramp.elements)[1:]:
    ramp.color_ramp.elements.remove(e)
for i, code in enumerate(palette):
    e = ramp.color_ramp.elements[0] if i == 0 else ramp.color_ramp.elements.new(i / len(palette))
    rgb = [int(code[j:j+2],16)/255 for j in (0,2,4)]
    e.color = (*[v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4 for v in rgb], 1)
emission = mn.new('ShaderNodeEmission')
outmat = mn.new('ShaderNodeOutputMaterial')
ml.new(geo.outputs['Position'], xyz.inputs[0])
ml.new(xyz.outputs['Z'], depth.inputs['Value'])
ml.new(geo.outputs['Normal'], dot.inputs[0])
ml.new(dot.outputs['Value'], normal.inputs[0])
ml.new(depth.outputs['Result'], add.inputs[0])
ml.new(normal.outputs[0], add.inputs[1])
ml.new(add.outputs[0], ramp.inputs[0])
ml.new(ramp.outputs['Color'], emission.inputs['Color'])
ml.new(emission.outputs[0], outmat.inputs['Surface'])
setmat = nodes.new('GeometryNodeSetMaterial')
setmat.inputs['Material'].default_value = material
links.new(smooth.outputs[0], setmat.inputs['Geometry'])
links.new(setmat.outputs[0], output.inputs['Geometry'])

camera_data = bpy.data.cameras.new('WaterVortex_TopCamera')
enum(camera_data, 'type', 'ORTHO')
camera_data.ortho_scale = RADIUS * 2 * 1.08
camera = bpy.data.objects.new('WaterVortex_TopCamera', camera_data)
scene.collection.objects.link(camera)
camera.location = (0, 0, 10)
camera.rotation_euler = (0, 0, 0)
scene.camera = camera
scene.render.resolution_x = scene.render.resolution_y = SIZE
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
enum(scene.render.image_settings, 'file_format', 'PNG')
enum(scene.render.image_settings, 'color_mode', 'RGBA')
scene.render.fps = FRAMES
scene.frame_start, scene.frame_end = 1, FRAMES
try:
    scene.view_settings.view_transform = 'Standard'
except TypeError as error:
    raise RuntimeError(str(error))
bpy.context.window.scene = scene
scene.frame_set(1)
scene.render.filepath = str(OUT/'preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'WaterVortex-Clean-Blue-24.blend'), copy=True)
bpy.ops.render.render(write_still=True)
(OUT/'raw').mkdir(exist_ok=True)
for frame in range(1, FRAMES + 2):
    scene.frame_set(frame)
    scene.render.filepath = str(OUT/'raw'/f'frame_{frame:04d}.png')
    bpy.ops.render.render(write_still=True)
scene.frame_set(1)
print('完成可调裁切、水色、24帧循环和400像素俯视配置', str(OUT))
