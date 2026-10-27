"""以参数化轮廓生成天裁虚影；白色遮罩供元素染色，独立侧面供后续伪3D变换。"""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/Textures/Hallow'
PREVIEW = ROOT / 'docs/assets/hallow-phantom'
SIZE = (64, 256)
SAMPLE = 4
GRIP = (32, 204)
TIP = (32, 12)
# 细长尖锋、轻微不对称刃线、根部外展肩部；不复刻镂空和环形装饰。
BLADE = [(32,12),(34.7,23),(36.1,42),(35.7,79),(35.8,126),
         (36.8,163),(39,181),(42,190),(39,198),(25,198),
         (22,190),(26.4,180),(28,161),(27.7,116),(27.8,66),(28.2,35),(30,20)]
GUARD = [(15,198),(49,198),(49,203),(15,203)]
HANDLE = [(29.5,203),(34.5,203),(34.5,224),(36,228),(32,232),(28,228),(29.5,224)]
EDGE_WIDTH = 2.5
GLOW_RADIUS = 2.0
GLOW_ALPHA = 0.55

def mask(polygons):
    im = Image.new('L', (SIZE[0]*SAMPLE, SIZE[1]*SAMPLE))
    d = ImageDraw.Draw(im)
    for poly in polygons:
        d.polygon([(round(x*SAMPLE),round(y*SAMPLE)) for x,y in poly], fill=255)
    return im.resize(SIZE, Image.Resampling.LANCZOS)

def white(alpha):
    # PNG使用直通Alpha；透明像素归零，游戏内容管线负责预乘。
    im = Image.new('RGBA', SIZE, (255,255,255,0))
    im.putalpha(alpha)
    im.putdata([(255,255,255,a) if a else (0,0,0,0) for a in alpha.tobytes()])
    return im

front = mask([BLADE, GUARD, HANDLE])
side = mask([[(32,12),(32+EDGE_WIDTH/2,27),(32+EDGE_WIDTH/2,228),
              (32,232),(32-EDGE_WIDTH/2,228),(32-EDGE_WIDTH/2,27)]])
# 柔光与实心轮廓分离，后续可以独立调亮而不模糊剑锋。
glow = front.filter(ImageFilter.GaussianBlur(GLOW_RADIUS)).point(lambda a: round(a*GLOW_ALPHA))
DEST.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
for suffix, alpha in [('Front',front),('Side',side),('Glow',glow)]:
    white(alpha).save(DEST / f'HeavensJudgmentPhantom{suffix}.png')

def tint(alpha, color):
    im=Image.new('RGBA',SIZE,(*color,255)); im.putalpha(alpha); return im

# 将正面宽度按cos压缩、厚度按sin显露，作为材质可读性预览，并非最终动画。
def yaw_preview(degrees, color):
    c,s=abs(math.cos(math.radians(degrees))),abs(math.sin(math.radians(degrees)))
    result=Image.new('RGBA',SIZE)
    for alpha,factor,rgb in [(side,s,tuple(round(v*.68) for v in color)),(glow,c,color),(front,c,color)]:
        width=max(1,round(SIZE[0]*factor))
        layer=tint(alpha,rgb).resize((width,SIZE[1]),Image.Resampling.BICUBIC)
        result.alpha_composite(layer,((SIZE[0]-width)//2,0))
    return result

canvas=Image.new('RGBA',(1100,620),(19,25,35,255)); d=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
d.text((24,16),'天裁虚影 · 程序化轮廓初稿（预览放大2倍）',font=font,fill='white')
source=Image.open(ROOT/'Assets/Textures/Hallow/HeavensJudgmentBlade.png').convert('RGBA')
canvas.alpha_composite(source.resize((96,480),Image.Resampling.NEAREST),(30,70))
d.text((25,560),'原剑参考',font=font,fill='white')
for i,(angle,color,label) in enumerate([(0,(71,233,170),'正面 · 未共鸣'),(45,(80,235,190),'45° · 风'),(75,(80,235,190),'75° · 风'),(90,(80,235,190),'侧面厚度'),(0,(185,110,255),'正面 · 雷'),(0,(255,110,45),'正面 · 火')]):
    x=170+i*150
    canvas.alpha_composite(yaw_preview(angle,color).resize((128,512),Image.Resampling.NEAREST),(x,50))
    d.text((x,560),label,font=font,fill='white')
canvas.convert('RGB').save(PREVIEW/'preview.png')
assert GRIP[1]-TIP[1] == 192
assert all(front.getpixel((x,y)) == 0 for x,y in [(0,0),(63,0),(0,255),(63,255)])
print(f'已生成三层64×256遮罩；握点{GRIP}，尖端{TIP}，距离192。')
