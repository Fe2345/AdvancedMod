# 2026-10-30：从原场景提取五层材质与全部依赖，生成独立 Godot 资源导出工程。
import pathlib,re,json
root=pathlib.Path(__file__).resolve().parents[2]
s=(root/'Assets/Effects/IdyllicReference/original.tscn').read_text(encoding='utf-8')
blocks={m[2]:(m[1],m[3]) for m in re.findall(r'(\[sub_resource type="([^"]+)" id="([^"]+)"\]\n)(.*?)(?=\n\[|\Z)',s,re.S)} if False else {}
for m in re.finditer(r'\[sub_resource type="([^"]+)" id="([^"]+)"\]\n(.*?)(?=\n\[|\Z)',s,re.S): blocks[m[2]]=(m[1],m[3])
ids=['jv6v4','ehesh','1oqfl','vhaoy','whbtt']; needed=set()
def add(k):
 if k in needed:return
 needed.add(k)
 for dep in re.findall(r'SubResource\("([^"]+)"\)',blocks[k][1]):add(dep)
for i in ids:add('ShaderMaterial_'+i)
p=root/'obj/idyllic-godot';p.mkdir(exist_ok=True)
(p/'project.godot').write_text('[application]\nconfig/name="Idyllic Export"\n[rendering]\nrenderer/rendering_method="gl_compatibility"\n',encoding='utf-8')
(p/'original.gdshader').write_bytes((root/'Assets/Effects/IdyllicReference/original.gdshader').read_bytes())
out='[gd_scene load_steps=80 format=3]\n[ext_resource type="Shader" path="res://original.gdshader" id="4_83rsy"]\n'
for k,(typ,b) in blocks.items():
 if k in needed:out+=f'[sub_resource type="{typ}" id="{k}"]\n{b}\n'
out+='[node name="CosmicSlash" type="Node2D"]\n'
for n,i in enumerate(ids):out+=f'[node name="Layer{n}" type="ColorRect" parent="."]\nmaterial = SubResource("ShaderMaterial_{i}")\noffset_left = -80.0\noffset_top = -80.0\noffset_right = 80.0\noffset_bottom = 80.0\n'
(p/'cosmic.tscn').write_text(out,encoding='utf-8')
layers=[]
for i in ids:
 d={}
 for k,v in re.findall(r'shader_parameter/(\w+) = (.*)',blocks['ShaderMaterial_'+i][1]):
  if v.startswith('SubResource'):d[k]=re.search('"([^"]+)"',v)[1]
  elif v.startswith('Vector'):d[k]=[float(x) for x in v[v.index('(')+1:-1].split(',')]
  elif v.startswith('Packed'):continue
  elif v in ['true','false']:d[k]=v=='true'
  else:d[k]=float(v)
 layers.append(d)
(root/'Assets/Idyllic/layers.json').write_text(json.dumps(layers,indent=2),encoding='utf-8')
shader=(p/'original.gdshader').read_text(encoding='utf-8');license=shader[shader.index('/*'):shader.index('*/')+2]
(root/'Assets/Idyllic/LICENSE.txt').write_text(license+'\nSource: https://github.com/Alkaliii/spellbook-gd\n',encoding='utf-8')
print('Prepared',len(needed),'resources')
