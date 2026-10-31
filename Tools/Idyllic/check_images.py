# 2026-10-30：对两种引擎截图进行裁切对照，并生成实际 GPU 帧序列动图。
from pathlib import Path
from PIL import Image,ImageChops,ImageStat,ImageDraw
import json
p=Path(__file__).resolve().parents[2]/'docs/testing/idyllic'
a=Image.open(p/'godot-original.png').convert('RGB');b=Image.open(p/'original-purple-reference.png').convert('RGB')
box=(490,390,790,530)
x=a.crop(box);y=b.crop(box)
out=Image.new('RGB',(1200,330),(22,24,35));out.paste(x.resize((600,280)),(0,35));out.paste(y.resize((600,280)),(600,35));d=ImageDraw.Draw(out);d.text((20,10),'Godot original / p=0.30',fill='white');d.text((620,10),'tModLoader port / p=0.30',fill='white');out.save(p/'reference-comparison.png')
frames=[Image.open(p/f'animation-{i:02}.png').crop((400,300,880,600)).resize((960,600)) for i in range(25)]
frames[0].save(p/'animation.gif',save_all=True,append_images=frames[1:],duration=52,loop=0)
# 忽略不同标签，只比较特效区域的周期旋转结果。
r1=Image.open(p/'rotation-181.png').crop((400,300,880,550));r2=Image.open(p/'rotation-minus179.png').crop((400,300,880,550))
seam=ImageStat.Stat(ImageChops.difference(r1,r2)).mean
stats={'reference_roi_rgb_mae':ImageStat.Stat(ImageChops.difference(x,y)).mean,'rotation_181_minus179_mae':seam}
(p/'image-checks.json').write_text(json.dumps(stats,indent=2),encoding='utf-8');print(stats)

frames=[Image.open(p/f'original-animation-{i:02}.png').crop((400,300,880,600)).resize((960,600)) for i in range(25)]
frames[0].save(p/'original-animation.gif',save_all=True,append_images=frames[1:],duration=52,loop=0)
