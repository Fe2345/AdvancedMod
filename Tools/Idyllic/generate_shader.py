# 2026-10-30：将场景五层常量展开到 HLSL，保留原作者 MIT 声明。
import pathlib,json,re
root=pathlib.Path(__file__).resolve().parents[2]
layers=json.loads((root/'Assets/Idyllic/layers.json').read_text())
s=(root/'Tools/Idyllic/material.fx.template').read_text(encoding='utf-8-sig')
prefix,body=s.split('// LAYER_BEGIN',1);body=body[body.index('\n')+1:].split('// LAYER_END')[0]
license=(root/'Assets/Idyllic/LICENSE.txt').read_text(encoding='utf-8').split('Source:')[0]
out=license+'\n'+prefix
for i,d in enumerate(layers):
 b=body.replace('_LAYER','_'+str(i))
 for k,v in sorted(d.items(),key=lambda kv:-len(kv[0])):
  if isinstance(v,list):value='float'+str(len(v))+'('+','.join(str(x) for x in v)+')'
  elif isinstance(v,(int,float)):value=str(float(v))
  else:continue
  b=re.sub(r'\b'+k+r'\b','('+value+')',b)
 out+=b
(root/'Assets/Shaders/IdyllicSlash.fx').write_text(out,encoding='utf-8')

