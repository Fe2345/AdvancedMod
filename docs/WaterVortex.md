# 二维伪三维水龙卷实验

实际编写日期：2026-10-27。定位是可运行、可调参的视觉路线实验；不是完整三维流体或已经优化的战斗特效。

2026-10-27 后续：作者提供 Blender 龙卷模型后，新增独立的 [400×400 俯视烘焙贴图](assets/water-sprite-top/README.md)。该路线是 24 帧分层旋转动画，使用 +Z→-Z 正交视角；本页原程序化水体实验仍保留，尚未将其替换为正式玩法中的水旋涡效果。下文关于无需烘焙的论述属于此前实验阶段，并不否定本次作者明确要求的贴图制作。

## 启动与文件

在工作区根目录使用 PowerShell 7：

```powershell
pwsh -File Tools/WaterPreview/Run.ps1 -Vortex
pwsh -File Tools/WaterPreview/Run.ps1
pwsh -File Tools/WaterPreview/Run.ps1 -Test
pwsh -File Tools/WaterPreview/Run.ps1 -Capture -Vortex
```

独立入口为 `Tools/WaterPreview`，核心是 `Basic/Graphics/Water`，shader 为 `Assets/Shaders/WaterVortex.fx` / `.xnb`。预览项目通过 Compile Include 直接使用模组中的核心源码；没有第二套算法。主项目原有的 Tools 排除规则继续生效。

环境：Windows、.NET 8 运行时、.NET SDK、Python、DirectX June 2010 `d3dcompiler_43.dll`。引用本机 TML 附带的 FNA 23.10.0.0、FNA3D 23.10、SDL 2.28.2；工程路径可用 `-p:TmlDirectory=...` 覆盖。没有使用 MonoGame/MGFX。采用现有 `Tools/ShaderBytecode.py` 编译 D3D9 Effect 并封装 XNB；已在 FNA D3D11 和 TML 真实客户端加载。SM3 路径只据本机实测成立，不宣称所有旧设备支持。

```powershell
python Tools/ShaderBytecode.py compile Assets/Shaders/WaterVortex.fx Assets/Shaders/WaterVortex.xnb
dotnet build Tools/WaterPreview -c Release -p:NuGetAudit=false -m:1 -nr:false
dotnet Tools/WaterPreview/bin/Release/net8.0/WaterPreview.dll --shaded --preset Tools/WaterPreview/Presets/vortex.json
dotnet Tools/WaterPreview/bin/Release/net8.0/WaterPreview.dll --shaded --background 'C:/path/image.png'
```

从根目录启动；找不到文件会明确报错。默认窗口 1000×760，关闭 VSync。预设包含固定 Seed；时间由宿主传入，不依赖逐帧随机数。导出模式以 1/30 秒推进，输出每 10 帧一张真实 GPU 图像，默认共 12 秒。截图会覆盖同名上次运行文件。

## 操作

| 操作 | 功能 |
|---|---|
| 空格 / `.` / S / R | 暂停 / 单步 1/60 秒 / 0.2 倍慢放 / 时间归零 |
| Tab | 两水带 / 完整旋涡 |
| 上下箭头 | 选择旋转、径向扰动传播速度、宽度、分支、阈值、纵深、厚度、高光、折射、反射 |
| 左右箭头 | 连续调整选中值，标题显示数值 |
| D | 最终 / 原始场 / 遮罩 / 高度 / 厚度 / 法线 |
| B | 棋盘 / 暗 / 亮背景 |
| I / F / O | 1/5/10 实例 / 128/256/512 场 / 分散与重叠 |
| L | 出现、持续、消失的 12 秒透明度包络 |
| F5 / F9 | 保存 / 读取 `Tools/WaterPreview/preset.json` |
| F12 / Esc | 截图 / 退出 |

标题显示 CPU 生成、上传和绘制提交的滑动平均；GPU 耗时不可用。参数面板采用窗口标题与键盘，不依赖字体内容管线。窗口可拖动缩放。图片背景通过 `--background` 加载；`--bg 1/2` 直接选择暗/亮；`--view 0..5` 直接选择调试通道。

`.fx` 时间戳变化后等待 500 ms 防抖；后台编译，控制台与 `obj/water-preview/shader.log` 显示日志。渲染线程构造、校验并替换 Effect，释放旧实例。编译或参数接口失败时保留旧 Effect，标题明确显示 `OLD EFFECT`。不支持热更新 C# 参数协议；修改协议后需要重新构建程序。

短动画由实际 PNG 编码，需安装 Pillow 的 Python：

```powershell
python Tools/WaterPreview/Export.py fusion
python Tools/WaterPreview/Export.py vortex
```

本机使用 Codex 附带的 Python/Pillow 已实际生成 `docs/assets/water/fusion.gif` 和 `vortex.gif`；系统 Python 没有 Pillow。GIF 是抽帧观察材料，首尾不强制无缝循环，不能用其回放帧率判断程序性能。

## 算法与边界

每条曲线采样为 120 个连续线段，仅遍历线段扩展包围盒。像素到线段求最近距离，宽度沿骨架插值，使用有限支撑核 `K=(1-d²/w²)^3`。**同一条带内取最大值，不对相邻线段求和**，消除接头重复累积；同一深度层的不同水带相加。平滑阈值宽度为 ±0.035。调大作用宽度或降低阈值会提前形成桥；阈值决定水体轮廓，细节不负责伪造断裂。

CPU 保留 float 场；上传 `SurfaceFormat.Vector4`，四通道分别是原始场、表面高度、近似视线厚度、遮罩。没有普通颜色格式饱和。高度包括旋涡口凹陷和截面隆起；厚度另行计算，不把凹陷深度误认为水的厚度。GPU 中心差分求大形状导数，叠加角向周期水纹解析导数，固定屏幕光源计算高光。径向/切向基底转换保留导数方向。

三条主螺旋与三条同层分支能够改变真实遮罩并融合；另外两条在前景深度层，只遮挡不参与后层累积。各实例独立数组和纹理。差速采用有界相位剪切，避免时间越长曲线绕得越密；连续正弦扰动负责局部宽度与分支事件。径向参数控制局部形变传播，**不是质量守恒的净径向输运**。

最终颜色读取宿主显式背景，使用近似指数厚度吸收、Schlick 式 Fresnel、环境渐变反射与 Blinn 高光、法线偏移折射。颜色近似以 gamma 2.2 解码/编码，数据纹理不做 gamma。输出预乘 alpha，alpha 表示覆盖率与生命周期，不重复乘一遍“透明水材质”不透明度。Clamp 采样避免纹理回绕。普通混合的边缘仍在宿主显示颜色空间完成，不宣称严格线性 HDR 合成。

核心复用每实例两张局部浮点纹理，实时 CPU 生成与上传，**没有逐帧创建 RenderTarget，也没有日常 GPU 回读**。预览器只有一张共享窗口 RenderTarget，供画面呈现与显式截图；截图和测试允许回读。没有为每个实例执行整屏后处理。当前 CPU 场路线在多实例时明显慢，下一步更适合 GPU 局部场光栅化，而非立即维护整套烘焙动画。

两层高度场不能表达翻卷背面、闭合水膜或任意深度交叉。上层透射仍取原场景背景，不会折射后层水体；多实例交叠同样有此限制。没有场景深度、SSR、真正焦散、体积散射、物理流体求解或辅助粒子。视觉更接近透明螺旋水带，尚需艺术迭代才能成为厚重水壁。

## TML 接入

`Contents/Development/WaterPreviewSystem.cs` 是仅在 `ADVANCEDMOD_WATER_TEST=1` 时启用的最小实验接入。核心不引用 Terraria。宿主提供 GraphicsDevice、SpriteBatch、Effect、时间、绘制矩形、二维相机矩阵、显式背景纹理、光照方向、参数和可见度。必须在渲染线程调用，且调用前结束自己的 SpriteBatch；资源由宿主生命周期清理。背景应与当前视口对应，不能把正在绑定的渲染目标同时当输入。

本轮在 `orig Draw` 后用 `Main.screenTarget` 显示屏幕中心实验水带；这是验证位置、背景与混合的顶层叠加，未接入战斗实体、世界遮挡或正式 VFX 队列。相机矩阵的屏幕 UV 在顶点变换后生成；任意相机旋转下的法线旋转还未适配。测试使用独立复制到 `obj/water-live` 的存档，截图 `docs/assets/water/tml-fusion.png`。已有打包警告 `Image loading failed: unknown image type` 仍存在，不记为无警告打包。

## 原始来源核对

以下均是阅读原始材料后自行实现数学与接口，没有复制整套工程或第三方图片资源。

- [Rendering Metaballs in 2D](https://salvatorespoto.github.io/post/meta-balls/)：采用有限范围场与阈值思想，改为线段距离核。原文 `1-smoothstep` 的展开应是 `1-3x²+2x³`；注释的负号、阈值 0.3/0.4/0.8 与实际代码不一致，未照抄。页面标注版权保留，没有据此认定代码可自由搬运。
- [MetaBalls2DShading](https://github.com/yp3y5akh0v/MetaBalls2DShading)：实际核对提交 `b5a783f84eb05b5261f66d0c10cdabc840a602ac` 的 `MetalBallsShader.shader`，它累加反平方场并交给 Lambert，不含本任务厚度、折射或法线重建。仓库没有找到 LICENSE/COPYING；只作对照，没有采用代码。
- [Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/)：核对双相位重置与权重；本版用不累积 UV 的周期解析水纹，所以没有移植双相位实现。
- [Directional Flow](https://catlikecoding.com/unity/tutorials/flow/directional-flow/)：采用流向基底和导数回转的数学原则；没有复制 Unity 接口或纹理。
- [Looking Through Water](https://catlikecoding.com/unity/tutorials/flow/looking-through-water/)：采用显式背景透射、吸收和避免二次透明混合的原则；没有假设 TML 提供 Unity 深度。Catlike 教程页面未在本轮核实独立源码许可，不复制配套包或图像。
- [Curl-Noise 论文](https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph2007-curlnoise.pdf)：仅思路参考，本版未实现 curl noise，更没有将它当液面求解器。
- [FNA 新游戏](https://fna-xna.github.io/docs/2b%3A-Building-New-Games-with-FNA/)、[FAQ](https://fna-xna.github.io/docs/0%3A-FAQ/)：核对 Game/GraphicsDevice、非 NuGet 依赖路线。使用用户已安装 TML 的 FNA，不在源码交付里另行发布第三方原生包。
- [TML Expert Shader Guide](https://github.com/tModLoader/tModLoader/wiki/Expert-Shader-Guide)：核对 Effect/sampler。指南建议 SM2；本版复杂 shader 用 SM3，兼容结论限定于实际测试的 FNA/TML D3D11。

## 验证状态

- 已构建：独立 Release 与主模组；共享核心均由实际编译使用。
- 离线通过 10 项：确定性、遮罩连通分量 2→1→2、数值有界与时间连续性、CPU 场稳态零托管分配、实例隔离、有效/无效 shader 编译。详见 `docs/testing/water/offline.json`。
- 已实机：独立 FNA 的融合和完整旋涡截图/GIF；TML 最小水带接入截图。
- 已实机：暂停 GPU 图像逐像素一致；窗口、分辨率、实例数变化；水体纹理计数归零，见 `lifecycle.json`。该计数是应用持有纹理，不是驱动显存分析；没有声称长期无驱动泄漏。
- 已实机：语法错误与接口不兼容均保留旧 Effect；有效版本恢复两次，见 `reload.json`。
- 性能矩阵及硬件见 `WaterVortex-Performance.md`；GPU 时间未获取。
- 尚未验证：联机、OpenGL/Vulkan、长时间显存压力、其他模组绘制兼容、正式世界相机和实体挂接。键盘所有操作未逐项人工验收；参数保存读取与本地图片路径有实现，不能以自动构建替代人工交互验收。

补充：调试视图显示主深度层，避免空的前层覆盖主层。shader 编译器输出一条 pow 的一般性负底数警告；当前背景输入来自非负 Color 纹理，最终颜色也经过 saturate。实际加载与运行通过，不将该构建记为无 shader 警告。
