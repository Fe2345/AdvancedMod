# 导电闪电源码调查（2026-10-26）

## 结论与范围

已定位本机 Terraria 1.4.5.8 的雷雨闪电：视觉主体是池化粒子 StormLightningParticle，并非必须依赖弹幕。路径由 LightningGenerator 生成，StormLightningDrawer 使用 VertexStrip 与 StormLightning 着色器绘制。作者随后确认双端点、密集折角、有限整体偏移和客户端独立随机；现已完成适配，并按“独立效果实机检查→导电接入→机制回归”的顺序验证。

证据来自本机 `D:/Program Files (x86)/Steam/steamapps/common/Terraria/Terraria.exe` 的反编译；同时核对项目使用的 tModLoader.dll。临时反编译文件保存在被忽略的 obj/LightningResearch.*.cs，不参与源码发布。

## 原版位置

- `Terraria.GameContent.Drawing.ParticleOrchestrator.Spawn_StormLightning`：从池中取粒子，选择 StormLightning 配置，准备路径，加入 Main.ParticleSystem_World_OverPlayers。天气分支还触发震屏、背景闪白、雷声和落点粒子；这些不必随导电视觉一起移植。
- `Terraria.GameContent.LightningGenerator.StormLightning`：天气预设长度1000、步长8、4层随机角度、角度强度0.9；支持分叉和地形／液体截断。
- `LightningGenerator.GenerateBolt`：每步重新朝向目标，再叠加多层随机角度；不同层以不同频率刷新。横向偏离影响随机角度回正，接近末段后减少随机性。此机制是软回正，不保证硬性横向上限或始终向前。
- `LightningGenerator.GetArcSurgeWeaponGenerator(start, end)`：原版已有两点电弧预设，SourceRotationLimit=0、Length=1、步长6、5层、角度强度0.7；与粒子的双端点 Prepare 配合传入 end-start，因此能从指定起点生成。
- `Terraria.Graphics.Renderers.StormLightningParticle.Prepare/Update/Draw`：生成一次折线并保存，逐刻更新寿命和光照，交给 Drawer 绘制；不是每帧完全重抽形状。
- `Terraria.Graphics.StormLightningDrawer.Draw`：顶点带、明暗过渡、沿路径推进的显隐波以及宽度衰减。几何形状与发光材质分离。

项目当前 tModLoader 程序集中未找到 LightningGenerator、StormLightningParticle、StormLightningDrawer、LCG32Random。这不是可以直接引用的当前 tML API。其 Main.NewLightning/UpdateWeather 主要管理天气闪白、延迟雷声；不能据此替代1.4.5的折线闪电。移植还需核对新着色器及资源依赖，未验证像素级复现。

## 当前实现与使用

- `Basic/Graphics/Geometry/LightningPath.cs`：固定A/B，每1.5像素采样，最多512段；五层扰动，细层频繁刷新、粗层幅度递减。为保证主轴单调，采用等距前进加法线偏移，而非直接移植角度游走。偏移上限为长度6.5%、最多7像素，两端各18%逐渐收紧；80像素链段的中心线横向偏移不超过5.2像素，此上限不包括发光半宽。
- `Basic/Graphics/LightningEffect.cs`：一次生成路径快照，复用WorldEffect生命周期，不持有NPC。连续顶点带限制尖角接缝延伸；寿命0.3秒，初始半宽3.5像素，末半段与寿命后半程缩窄；保留沿路径推进的显隐波及先增强再衰减的亮度。
- `Assets/Shaders/Lightning.fx/.xnb`：兼容FNA的SM2材质，自行实现白色细亮芯和带色柔光，加色混合。由VfxDrawQueue加载并恢复绘制状态；不宣称与1.4.5着色器像素一致。当前只做单主干，不增加分叉、雷声、震屏或落点破坏。
- `ElementNetwork.DrawReaction`：非零HydroElectro链段使用新闪电，颜色复用雷元素色；同点触发及其他反应继续原有粒子。沿用网络起终点，各客户端独立随机，不增加网络字段。伤害、选敌、衰减、视线及不附着机制不变，视觉路径不参与碰撞。

客户端独立调用：`Vfx.Create(new LightningEffect(start, end, color, Main.rand.Next()))`。几何参数集中在LightningPath，时序与宽度集中在LightningEffect。

材质编译：`python Tools/ShaderBytecode.py compile Assets/Shaders/Lightning.fx Assets/Shaders/Lightning.xnb`。实机入口：`Tests/Run-LightningLive.ps1`，只有环境变量ADVANCEDMOD_LIGHTNING_TEST=1才启用自动验收；使用obj/lightning-live隔离存档。

## 验证状态

- 构建通过：分析器启用，0警告0错误；材质编译通过。打包仍有既有图片类型警告，正常Mods包已更新。
- 离线通过：Vfx测试36/36，新增端点、单调性、偏移、密集折角、退化输入及生命周期验证。
- 实机通过：独立阶段5/5；最终接入后11/11（包含5张截图回读），既有元素机制15/15；峰值、淡出、到期和链伤截图已检查。
- 未执行：多人专项、OpenGL／Vulkan后端与第三方光照兼容。证据见[验证记录](testing/2026-10-26-lightning.md)。
