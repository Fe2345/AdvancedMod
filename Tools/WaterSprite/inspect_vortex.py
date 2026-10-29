"""只读检查新旋涡模型的对象、节点连接与动画。"""
import bpy
import json
from pathlib import Path

out = Path(__file__).resolve().parents[2] / 'docs/assets/water-vortex-model'
out.mkdir(parents=True, exist_ok=True)
data = {'scenes': [], 'objects': [], 'groups': []}
for s in bpy.data.scenes:
    data['scenes'].append({'name': s.name, 'frames': [s.frame_start, s.frame_end, s.frame_current], 'engine': s.render.engine})
for o in bpy.data.objects:
    data['objects'].append({'name': o.name, 'type': o.type, 'location': list(o.location), 'size': list(o.dimensions),
        'materials': [m.name if m else None for m in o.data.materials] if hasattr(o.data, 'materials') else [],
        'modifiers': [{'name': m.name, 'type': m.type, 'group': m.node_group.name if m.type == 'NODES' and m.node_group else None,
                       } for m in o.modifiers]})
for g in bpy.data.node_groups:
    group = {'name': g.name, 'nodes': [], 'links': []}
    for n in g.nodes:
        node = {'name': n.name, 'type': n.bl_idname, 'label': n.label,
            'inputs': {str(i)+':'+s.name: str(s.default_value) for i,s in enumerate(n.inputs) if hasattr(s,'default_value') and not s.is_linked}}
        if n.type == 'GROUP': node['group'] = n.node_tree.name
        if hasattr(n,'operation'): node['operation'] = n.operation
        group['nodes'].append(node)
    group['links'] = [(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in g.links]
    data['groups'].append(group)
(out/'inspection.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'scenes':data['scenes'],'objects':data['objects'],'groups':[(g['name'],len(g['nodes'])) for g in data['groups']]},ensure_ascii=False))
