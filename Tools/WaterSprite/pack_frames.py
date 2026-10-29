"""将 Blender 烘焙结果归一到共享色板，输出透明贴图和预览。"""
from pathlib import Path
import json
import hashlib
import argparse
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description='打包已有水旋涡渲染帧')
parser.add_argument('--vortex-model', action='store_true', help='使用新漏斗旋涡模型的目录和水色色板')
args = parser.parse_args()
OUT = ROOT / 'docs/assets/water-sprite-top'
COUNT, WIDTH, HEIGHT = 24, 400, 400
COLORS = ['167ba6', '249bc3', '47bdd9', '88deec', 'd0f7fa',
          '123b65', '195581', '246f9d', '318cb3', '49aac8',
          '176a9d', '208dba', '35afd0', '68cfe5', 'adf0f4']
if args.vortex_model:
    OUT = ROOT / 'docs/assets/water-vortex-model'
    COLORS = ['10345e', '145581', '18769f', '249bbb', '41bdd3', '78dae5', 'c6f4f5']
palette = [tuple(bytes.fromhex(c)) for c in COLORS]

def pixelize(path):
    im = Image.open(path).convert('RGBA')
    assert im.size == (WIDTH, HEIGHT)
    result = Image.new('RGBA', im.size)
    cache = {}
    pixels = []
    for r, g, b, a in im.get_flattened_data():
        if a < 128:
            pixels.append((0, 0, 0, 0))
            continue
        rgb = (r, g, b)
        if rgb not in cache:
            cache[rgb] = min(palette, key=lambda c: sum((c[i] - rgb[i]) ** 2 for i in range(3)))
        pixels.append((*cache[rgb], 255))
    result.putdata(pixels)
    return result

frames = [pixelize(OUT / 'raw' / f'frame_{i:04d}.png') for i in range(1, COUNT + 1)]
(OUT / 'frames').mkdir(exist_ok=True)
strip = Image.new('RGBA', (WIDTH, HEIGHT * COUNT))
grid = Image.new('RGBA', (WIDTH * 6, HEIGHT * 4))
preview = []
for i, frame in enumerate(frames):
    frame.save(OUT / 'frames' / f'WaterTornado_{i:02d}.png')
    strip.paste(frame, (0, i * HEIGHT))
    grid.paste(frame, (i % 6 * WIDTH, i // 6 * HEIGHT))
    bg = Image.new('RGB', (WIDTH, HEIGHT), '#182433')
    bg.paste(frame, mask=frame.getchannel('A'))
    preview.append(bg)
strip.save(OUT / 'WaterTornado_24_vertical.png')
grid.save(OUT / 'WaterTornado_24_grid.png')
preview[0].save(OUT / 'WaterTornado_preview.gif', save_all=True, append_images=preview[1:],
                duration=[40, 40, 40, 40, 40, 50] * 4, loop=0, disposal=2)
contact = Image.new('RGB', grid.size, '#182433')
contact.paste(grid, mask=grid.getchannel('A'))
contact = contact.resize((grid.width // 2, grid.height // 2), Image.Resampling.NEAREST)
draw = ImageDraw.Draw(contact)
for i in range(COUNT):
    draw.text((i % 6 * WIDTH // 2 + 5, i // 6 * HEIGHT // 2 + 5), f'{i + 1:02}', fill='white')
contact.save(OUT / 'contact.png')
end = pixelize(OUT / 'raw/frame_0025.png')
seam_difference = sum(a != b for a, b in zip(frames[0].get_flattened_data(), end.get_flattened_data()))
bounds = [f.getbbox() for f in frames]
assert all(b and b[0] > 0 and b[1] > 0 and b[2] < WIDTH and b[3] < HEIGHT for b in bounds), bounds
unique = len({hashlib.sha256(f.tobytes()).hexdigest() for f in frames})
assert unique == COUNT
assert seam_difference == 0, seam_difference
assert Image.open(OUT / 'WaterTornado_preview.gif').n_frames == COUNT
report = {'frame_count': COUNT, 'frame_size': [WIDTH, HEIGHT], 'sheet_size': list(strip.size),
          'fps': 24, 'loop_duration_ms': 1000, 'unique_frames': unique,
          'palette': COLORS, 'alpha': [0, 255], 'frame_25_vs_1_different_pixels': seam_difference,
          'bounds': bounds, 'source_frame': 33, 'layer_turns_per_loop': [1, 2, 1],
          'atlas_size': list(grid.size), 'atlas_columns': 6, 'atlas_rows': 4,
          'camera': '+Z looking -Z, orthographic', 'game_integration': False}
if args.vortex_model:
    report['source_frame'] = None
    report['layer_turns_per_loop'] = [1]
    report['animation'] = '原几何节点整体旋转，时间改为24帧一圈'
    report['trim_radius'] = .78
(OUT / 'manifest.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k not in ('bounds', 'palette')}, ensure_ascii=False))
