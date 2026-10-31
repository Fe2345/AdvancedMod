# 风刃 demo（2026-10-30）

## 启动与操作

在模组工程编译并重新加载后进入世界，聊天输入 `/idyllic reset`，默认固定弦长、60°、进度 0.3 暂停。`/idyllic play` 单次播放，`/idyllic loop` 切换循环，`/idyllic pause` 暂停/恢复。

| 命令 | 含义 |
|---|---|
| `/idyllic angle 120` | 可分别输入 120、90、60、45、30、15，保持当前进度 |
| `/idyllic progress 0.3` | 暂停并设置归一化进度 0–1；端点为透明 |
| `/idyllic mode chord` / `radius` | 固定弦长 / 固定半径 |
| `/idyllic size 640` | 本地形状尺寸，32–1024；默认 640 |
| `/idyllic thickness 24` | 独立径向厚度尺度，1–48；小尺寸自动限制到 R/8 防止翻折 |
| `/idyllic feather 1` | 原羽化尺度乘数，0.1–3 |
| `/idyllic rotation 180` | 伪透视之后的整体朝向，周期 360° |
| `/idyllic brightness 1` | 整体亮度倍率，0–3 |
| `/idyllic original` | 原始 CosmicSlash 五层形状，使用同样的浅白半透明配色，不使用新角域重参数化 |
| `/idyllic reset` | 恢复默认参数并暂停在 0.3 |
| `/idyllic status` / `stop` | 显示参数 / 停止并关闭参数显示 |

参数持续显示在屏幕左上角。尺寸是变换前单位，伪透视会缩短屏幕上的长度；原始基准保持原 ColorRect 画面，在 size=640 时采用原始显示尺度。原始基准锁定原径向形状，因此 thickness 只影响新增角度模式。

## 几何与纹理

`ArcAngleDegrees` 定义于本地形状平面，先构造圆弧，再进行原始 70° X / 11° Y / 90° FOV 伪透视的逆映射，最后整体旋转。旋转不会进入角域裁剪，因此不存在跨 ±π 接缝跳变。

固定弦长：R=L/(2 sin(alpha/2))；固定半径：R=size、L=2 R sin(alpha/2)。两模式在 60° 重合。弧顶为局部原点，用 2R sin²(theta/2) 计算弓高，避免小角度相减精度损失；只提交实际弧段的 128 段网格，不为完整大圆建立 RenderTarget。

| 角度 | L（固定弦长） | R | 弓高 |
|---|---:|---:|---:|
| 120° | 640 | 369.50 | 184.75 |
| 90° | 640 | 452.55 | 132.55 |
| 60° | 640 | 640.00 | 85.74 |
| 45° | 640 | 836.20 | 63.65 |
| 30° | 640 | 1236.39 | 42.13 |
| 15° | 640 | 2451.61 | 20.97 |

形状/头尾/网格蒙版以 u∈[0,1] 覆盖整个弧段；原极坐标的 X 仍是径向、Y 仍是角向。内部纹理单独乘实际弧长相对默认弧长的比例，头尾渐隐不跟着纹理缩放。默认种子来自原 FastNoiseLite（0），进度显式驱动所有层，不读取全局时间。

thickness 是径向坐标尺度，最终可见厚度还受原始多边形、羽化及伪透视影响，不能等同于屏幕每处的像素宽度。不同角度固定此参数可避免整图非均匀缩放造成的增厚。

## 原实现对应关系

完整源码与场景保存在 `Assets/Effects/IdyllicReference`；版权和 MIT 条款位于 `Assets/Idyllic/LICENSE.txt`。`Assets/Idyllic/layers.json` 保留各层全部场景参数。

| 原层 | 保留的关键机制 |
|---|---|
| SlashShadow | Rhombius、2.0 羽化、0.6 能量、紫色透明渐变、偏移 −8 |
| SlashGlitter | 细尺度 cellular 噪声、Rhombius、单向展开、偏移 −10 |
| SlashCounterGlitter | 反向滚动/不同 mask、单向展开、inset=1 |
| SlashBase | Taper、多边形距离场、双三次羽化、11 样本色散、紫白渐变 |
| SlashHighlight | Taper、指数羽化、16 倍原发光参数、ease-out 能量退场、偏移 −11 |

保留实际使用的 distortion、mask、gradient、mesh-wide-mask、各动画驱动及逐层绘制顺序。目标 Cosmic 预设没有引用 CurveTexture，所用动画曲线由原多项式直接移植；其他非目标预设的 CurveTexture 留在完整源场景中，不参与 demo。

Godot 原生生成器导出 20 张纹理。PNG 同时保存 `.png.bin` 原始副本以免 tModLoader 打包转换后路径消失，并保留直通 RGBA。原 blend_mix 的颜色先截断到 LDR 范围，再转换为 FNA AlphaBlend 所需的预乘格式，避免黑边和过曝。此 demo 没有另加全屏 bloom；柔光由原阴影/羽化层提供。

## 资源再生成与运行检查

PowerShell 7，工程根目录：

```powershell
python Tools/Idyllic/prepare.py
Copy-Item Tools/Idyllic/export.gd obj/idyllic-godot/export.gd
# Godot 4.5.1 的可执行文件路径按本机安装位置替换
& obj/godot/Godot_v4.5.1-stable_win64_console.exe --headless --path obj/idyllic-godot --script export.gd --log-file export.log
python Tools/Idyllic/generate_shader.py
python Tools/ShaderBytecode.py compile Assets/Shaders/IdyllicSlash.fx Assets/Shaders/IdyllicSlash.xnb
dotnet build AdvancedMod.csproj -p:BuildMod=false --no-restore -m:1 -nr:false
dotnet run --project Tests/Idyllic/Idyllic.Tests.csproj -p:NuGetAudit=false -m:1 -nr:false
./Tests/Run-IdyllicLive.ps1
```

已提交纹理及 XNB，普通使用无需安装 Godot 或重新生成资源。再生成 Shader 使用现有 d3dcompiler_43 工具。实机脚本复用工程 `obj/element-live` 的隔离测试存档到 `obj/idyllic-live`；没有该测试存档的机器可直接进入自己的世界使用命令。

Godot 对照截图：复制 `Tools/Idyllic/render.gd` 到导出工程后用非 headless Godot 执行。`Tools/Idyllic/check_images.py` 需 Pillow，负责裁切对照、生成动画 GIF 与旋转周期像素检查。

## 验证边界

已在真实 tModLoader 2026.8.3.0 / Terraria 1.4.4.9 / FNA D3D11 / RTX 5060 Laptop 客户端加载模组并渲染；原始参考使用 Godot 4.5.1 OpenGL Compatibility。基准与六角度画面采用客户端离屏 RenderTarget，复用实际 VfxDrawQueue 和相同 HLSL；in-game.png 另记录普通世界入口。

已执行：100 条几何断言、视觉框架 39/39 回归（含新增生命周期与边界测试）。主工程构建 0 警告/0 错误；回归项目出现已有 NuGet 审计缓存联网警告，但测试执行完成。实际截图与像素检查见 `docs/testing/idyllic`。

并非逐像素复制：两引擎的插值/距离场导数与纹理采样有差异，闪烁细节仍可见不同；新增角度模式使用独立弧长噪声采样，因此不会与原整圆极坐标纹理逐点相同。角度比较期间颜色、种子、厚度、进度、透视参数均固定。无联机玩法或伤害逻辑；未宣称进行多人联机验收。


## 2026-10-30 浅白风刃配色
角度模式与 Original 模式默认使用微冷浅白 RGB=(0.92,0.97,1)，整体不透明度为 0.55。沿用五层形状、噪声、羽化和动画，提取原发光颜色的最大通道作为明暗，再赋浅白色；同步缩放预乘 RGB/Alpha 保持正确透明混合。
新增 /idyllic opacity 0..1 调整透明程度，reset 恢复 0.55。original 同样使用浅白配色和 opacity 参数；原紫色截图保留为 original-purple-reference.png 历史对照。

