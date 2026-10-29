"""2026-10-27：将实际 FNA 截图序列编码为 GIF；需要 Pillow，不合成替代渲染。"""
from pathlib import Path
import argparse
from PIL import Image
parser=argparse.ArgumentParser()
parser.add_argument('preset',choices=['fusion','vortex'])
args=parser.parse_args()
paths=sorted(Path('docs/assets/water').glob(f'{args.preset}-0-*.png'))
if not paths: raise RuntimeError('缺少真实截图，请先运行 --capture --shaded')
frames=[Image.open(p).resize((660,502)).convert('RGB') for p in paths]
frames[0].save(f'docs/assets/water/{args.preset}.gif',save_all=True,append_images=frames[1:],duration=333,loop=0)
print(f'已编码 {len(frames)} 帧')
