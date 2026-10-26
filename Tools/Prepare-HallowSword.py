"""将生成剑图清理为透明游戏资产，并将握点到剑尖映射为192像素。"""
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'docs/assets/hallow-sword/generated-source.png'
DEST = ROOT / 'Assets/Textures/Hallow'
TIP_Y, GRIP_X, GRIP_Y = 36, 512, 1280
RADIUS = 192
SIZE, ORIGIN = (48, 240), (24, 196)
SUPERSAMPLE = 4
im = Image.open(SOURCE).convert('RGBA')
# 保留实心剑身，仅清理低透明度的背景残色和外部光晕。
im.putalpha(im.getchannel('A').point(lambda a: round(max(0, min(1, (a - 16) / 224)) * 255)))
scale = RADIUS / (GRIP_Y - TIP_Y)
k = SUPERSAMPLE
blade = im.transform((SIZE[0]*k, SIZE[1]*k), Image.Transform.AFFINE,
    (1/(scale*k), 0, GRIP_X-ORIGIN[0]/scale, 0, 1/(scale*k), GRIP_Y-ORIGIN[1]/scale),
    Image.Resampling.BICUBIC).resize(SIZE, Image.Resampling.LANCZOS)
# PNG保持直通Alpha，交给tModLoader资源管线预乘；全透明像素的RGB归零。
blade.putdata([(r,g,b,a) if a else (0,0,0,0) for r,g,b,a in blade.getdata()])
DEST.mkdir(parents=True, exist_ok=True)
blade.save(DEST / 'HeavensJudgmentBlade.png')
icon = blade.crop(blade.getbbox()).rotate(-45, resample=Image.Resampling.BICUBIC, expand=True)
icon.thumbnail((44,44), Image.Resampling.LANCZOS)
canvas = Image.new('RGBA',(48,48))
canvas.alpha_composite(icon, ((48-icon.width)//2,(48-icon.height)//2))
canvas.save(DEST / 'HeavensJudgmentIcon.png')
print({'size':blade.size, 'bbox':blade.getbbox(), 'grip':ORIGIN, 'tip':(24,4), 'grip_to_tip':RADIUS})
