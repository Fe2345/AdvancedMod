# VFX 框架使用与验收

日期：2026-10-24。框架在 `Basic/Graphics`，三个基础示例在 `Contents/Vfx`。

## 当前范围

- 世界特效使用普通 C# 实例，不新增 Projectile，不进行伤害／碰撞，也不改变元素机制。
- 只接入 `PostDrawTiles` 世界绘制阶段与 `CustomSky` 背景。没有声明尚未实现的实体前后绘制层。
- 时间单位是秒，位置单位是世界像素，描边宽度是半宽。每次完整游戏更新推进 1/60 秒；暂停不推进，Draw 不推进时间。
- `build.txt` 声明 Luminance 最低依赖 1.0.12；本机真实加载 tML 1.4.4.9+2026.08.3.0、Luminance 1.0.14。当前后端直接使用 FNA BasicEffect、自建网格与程序化软遮罩，没有依赖 Luminance 的版本敏感轨迹接口。
- 首版材质是线芯、柔光、扫掠主体、软光点和天空光柱；没有离屏模糊、像素化或自定义流光 shader。

## 世界实例

```csharp
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Contents.Vfx;

// 机制先自行生效，再创建纯视觉；以下参数只是表现参数。
EffectHandle handle = Vfx.Create(new GoldenWard(new WardVisualState(center, radius)));
Vfx.SetTarget(handle, new WardVisualState(nextCenter, nextRadius, rotation));

// 传送时明确要求定位，避免跨地图平滑追随。
Vfx.SetTarget(handle, new WardVisualState(nextCenter, nextRadius, rotation, Snap: true));

// 可提交最终目标，但从当前显示半径和透明度开始收束。
Vfx.Stop(handle, new WardVisualState(finalCenter, finalRadius));
// 或立即删除；对无效／旧句柄均安全无操作。
Vfx.Kill(handle);
```

`Create(effect, delay)` 接收非负秒数；在延迟结束前停止会直接取消。同一轮创建再停止也不显示。重复停止不重置进度，停止后的状态／轨迹命令被忽略。新播放必须创建新实例。

句柄为非零 `ulong`，清理世界不重置计数器。它不是实体 `whoAmI` 或网络标识。每个调用方保存自己的句柄；关闭来源时主动停止，不要保留读取源实体的闭包。

默认最多 256 个实例（包含待启动）。达到预算时拒绝新实例、返回零句柄，并释放其独占资源；当前未实现按距离／重要性抢占旧实例。创建后实例所有权归管理器，不能把同一个实例再次提交。`SetTarget<T>` 与 `AppendSample<T>` 要匹配实例实现的强类型接口。自定义状态如包含可变数组，调用方必须先复制或转移所有权。

## 剑气姿态

```csharp
using AdvancedMod.Basic.Graphics.Geometry;

EffectHandle trail = Vfx.Create(new SlashTrailEffect());
Vfx.AppendSample(trail, new SlashPoseSample(innerWorld, outerWorld, Vfx.Time));
// 同一完整游戏刻可提交多个姿态；时间须单调，子更新可在本刻范围内细分。
Vfx.AppendSample(trail, new SlashPoseSample(nextInner, nextOuter, sampleTime));
// 传送、切换来源、突变方向时显式断开。
Vfx.AppendSample(trail, new SlashPoseSample(newInner, newOuter, sampleTime, BreakBefore: true));
Vfx.Stop(trail);
```

固定容量缓存保存刀刃内外端；重复／非有限／逆序样本被拒绝，超过传送阈值自动断开。距离较大的正常段线性补样，不能还原未采集到的真实旋转路径，快速转动应在来源处提供足够姿态。停止后的残留按样本时间消失，不依赖原弹幕存活。

`/vfx slash` 是按已知挥动曲线独立播放的示例，并未接入当前手持武器的真实挥动或伤害；正式武器应在自己的姿态更新处提交上述快照。

## 天空请求

```csharp
EnvironmentHandle sky = Vfx.AcquireEnvironment(GoldenSky.Key, new EnvironmentTarget(0.85f));
Vfx.SetEnvironmentTarget(sky, new EnvironmentTarget(0.6f, Priority: 10));
Vfx.ReleaseEnvironment(sky);
```

同样式取所有来源强度最大值；异样式按请求优先级选择，平级按样式 ID 的序数排序。最多保留当前样式和一个淡出样式。请求归零后平滑淡出，淡出中重新请求从当前强度回升。`CustomSky.Update` 不推进第二套时间。

新样式在客户端 `PostSetupContent` 调用 `WorldVfxSystem.RegisterSky`，绑定自己的 `CustomSky`。该天空的 `Reset` 应调用 `Environment.ResetStyle` 清理对应请求与显示层。不要使用一个共享布尔值代替来源令牌；来源死亡／断线时由来源适配器释放令牌。

## 样式与三叶结

`WardStyle`、`SlashStyle`、`GoldenSkyStyle` 保存颜色、宽度、时长与预算。领域 `KnotPath` 保存归一化中心线与下穿缺口，实例只变换缓存。参考图按三段对称贝塞尔弧拟合，不填充宽条；每段在与下一段相交处下穿，线芯和柔光都断开，不用黑色补丁。调整轮廓可替换基础弧控制点、采样间距及缺口长度，无需修改管理器。

## 手工验收

模组重新构建并加载后，在聊天框输入：

| 命令 | 行为 |
| --- | --- |
| `/vfx demo` | 15 秒本地综合演示：剑气、领域、新旧共存、两个天空来源与撤销、淡出重启 |
| `/vfx ward` | 在本地玩家处创建领域，旧领域自行收束 |
| `/vfx delay` | 延迟 2 秒创建领域；立即执行 stop 验证待启动取消 |
| `/vfx move` | 将领域目标移到鼠标位置并调整半径 |
| `/vfx stop` | 正常收束；连续执行两次检查幂等 |
| `/vfx kill` | 立即移除当前领域 |
| `/vfx slash` | 播放一次独立剑气曲线 |
| `/vfx sky` | 增加一个天空请求；可重复执行 |
| `/vfx release` | 撤销最近一个天空请求 |
| `/vfx clear` | 收束示例的所有效果、释放示例全部天空请求 |

这些命令在多人游戏中也仅作用于当前客户端，没有网络广播。不能将其视为机制同步或晚加入重建的验证。

## 自动验证

```powershell
dotnet build AdvancedMod.csproj --no-restore -p:BuildMod=false -v:minimal
dotnet run --project Tests/Vfx/Vfx.Tests.csproj -- docs/testing/2026-10-24-vfx-offline.json
pwsh -File Tests/Run-VfxLive.ps1
```

实机脚本只允许 `obj` 内隔离存档；默认 `obj/vfx-live`，需事先准备 `Players/testPlayer.plr`、`Worlds/testWorld.wld` 和启用三项依赖的 `Mods` 目录。脚本打包到该隔离目录，自动演示关闭自动保存，结束后直接退出。保持游戏窗口焦点，否则 Terraria 单人模式会暂停更新；这不是 VFX 计时故障。

只有设置 `ADVANCEDMOD_VFX_LIVE_TEST=1` 时才注册截图钩子。在完整 Draw 后按 6、18、90、245、365、615、660、850 游戏刻读取后缓冲，输出到隔离目录 `VfxCaptures`，同时记录实例数、天空请求数与强度。正常游戏不注册钩子、不读回 GPU。脚本检查本轮报告时间与 8 张截图状态，防止把上次结果当作本轮成功；截图生成成功不等于视觉验收通过。

详细结果见 [验证记录](testing/2026-10-24-vfx.md)。待手工补充暂停恢复、镜头缩放、世界切换；待正式机制接入后再做来源失效、晚加入与专服／联机验收。
