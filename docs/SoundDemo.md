# 程序化声波 Demo

实现日期：2026-10-25。基于本机 tModLoader 1.4.4.9+2026.08.3.0、.NET 8、FNA；最低依赖沿用 build.txt。这是一套可运行的开发机制，尚非正式武器。

## 使用

重新构建并加载模组，在聊天框输入下列命令。场景位于玩家上方120像素，左侧240像素处的哨兵30刻后发射；哨兵15秒自动消失。每次场景命令先清除自己上一次的声波场景。

| 命令 | 观察内容 |
| --- | --- |
| `/sounddemo single target` | 单列竖直波面向右平移、衰减，命中中间靶标一次 |
| `/sounddemo reflect target` | 左侧发射，中心45°反射面将波转向上方，命中上方靶标 |
| `/sounddemo same target` | 左右同相会合，融合波向右，命中右侧360像素处靶标 |
| `/sounddemo opposite target` | 相差π，会合后保留较暗的绿色元素载波，伤害降低、风量提高 |
| `/sounddemo partial target` | 相差π/2，伤害和风量介于两者之间 |
| `/sounddemo stress target` | 四个方向发射并经过中心反射面，检查多波去重和清理 |
| `/sounddemo inspect` | 一次性输出服务器波包状态和靶标累计伤害、已提交风量 |
| `/sounddebug` | 本地开关波包 ID、相位、能量、反射次数、融合类别标签 |
| `/sounddemo clear` | 清除自己的波包、哨兵和测试靶标 |

省略 `target` 不生成靶标，可对实际敌人测试。靶标复用已有 ElementProbeNPC，零防御、关闭伤害波动和暴击、接触伤害为零；靶标保留到 clear 或下次场景命令，以便 inspect。世界退出不保存这些临时实体。命令属于 World 类型，联机由服务器执行；调试标签属于 Chat 类型，只改变本地画面。

## 机制与数值

`SoundWave` 是实际 ModProjectile。它保存单调递增 ulong ID、起点、方向／速度、波长、相位、能量、累计距离、当前包年龄、剩余寿命、反射次数、融合深度以及已访问哨兵 ID。ID 在同次模组加载内不复用，世界退出清空实体，不作为跨存档标识。

- 初始速度6像素/刻，波长32，包络长宽各128，寿命180刻，初始能量1、伤害100、风量1。每刻能量乘0.993；低于0.025或寿命终点销毁。测试参数集中在 SoundRules。
- `PreUpdateProjectiles` 在权威端收集快照、统一移动，然后按 ID 顺序先处理反射，再处理融合。新生成的包本刻不再进入交互列表。消耗使用原生 Kill，使原包无法继续 Damage。
- 精确碰撞采用有向矩形 SAT，渲染波峰全部位于相同包络内；峰间空隙仍属于连续波包的伤害范围。局部NPC免疫设为-1，每包对每个NPC最多一次；新反射／融合波是独立攻击。不同体节是不同NPC，尚未做多体节敌人的统一命中预算。
- 风标签使用 `TagEnum.Anemo`，通过 ElementAttacks 进入已有 Calculate／Element 原生 Damage 事务。绘制不修改伤害或元素状态。目标已有水等元素时会按原反应系统发生反应，而非绕过系统直接写库存。

哨兵是同一种 `SoundSentry` 弹幕：可按朝向发射一次，也可只接收。反射使用 `d' = d - 2(d·n)n`，相位取接收位置的入射相位，不额外翻转π。能量乘0.92，元素／伤害分配类型继承，寿命不重置。源哨兵也记入访问集合，后续原线路不能再次被同一哨兵接收；融合合并双方历史。反射深度最多4，达到上限继续传播而不再反射。

同所有者、同波长、包络重叠且融合深度小于3的两包可以融合。以两包中心中点为会合采样位置，各自局部相位为：

`θ = φ + 2π dot(x - center, direction) / wavelength`

把振幅写成 `a = sqrt(E * coherence)`，原始包 coherence=1。相量相加 `z = a1 exp(iθ1) + a2 exp(iθ2)`，因此 `|z|` 满足平方根干涉公式。输出相位为 arg(z)，近零时保留第一包采样相位；输出一致性 `q = |z|² / (sqrt(E1)+sqrt(E2))²`，限制在0到1。

游戏化预算与相量平方分开：`Eout = 0.9(E1+E2)`，没有凭空翻倍相量能量。

| 输出 | 公式 | 等能同相 q=1 | 等能反相 q=0 |
| --- | --- | --- | --- |
| 伤害白值 | round(100 Eout (0.2+0.8q)) | 180 | 36 |
| 风附着 | Eout (2-1.5q) | 0.9 | 3.6 |

表中输入各为能量1且尚未继续传播；实机到靶标后还包含路途衰减。相消的剩余预算转为元素载波，所以仍有最低伤害与可见波纹，不声称是完全消失的物理声场。再次融合使用现存 coherence，不把已相消载波恢复成满振幅。

输出方向为能量加权方向，正对碰近零时沿较早 ID 的入射方向；这个优先级只决定方向，不决定干涉类别。已针对 sin(π) 浮点残差设置相对容差。新包寿命取两者较小值，距离取较大累计值，反射深度取较大值，融合深度取较大值加1。

## 程序化渲染

复用 Basic/Graphics 的 WorldEffect、VfxMesh 和 PostDrawTiles 后端。SoundVisual 是可复用于波包、哨兵与交汇脉冲的局部组件；只接收不可变姿态快照。不生成逐帧纹理、不创建新 Shader、不用大量粒子，也不修改 SpriteBatch 状态。网格顶点缓存随实例复用，BasicEffect 和渲染状态恢复仍由已有后端管理。

波峰位置使用 `s = mλ - φλ/(2π)`，中心随实际波包移动，等价于有限包络内的周期行波。每条峰由中央亮脊向左右透明渐变，横向两端再渐隐；厚度、波长、颜色、透明度独立配置。亮度由 sqrt(E)、coherence 和边缘包络共同控制。相长波更亮、更厚；相消波更薄、暗且偏饱和绿。

反射／融合子包新生时，在其起点通过同一 VFX 管理器创建0.3秒压缩波面反馈；间距从半波长展开。反射旧包的持续视觉随 Kill 立即移除。融合反馈读取逻辑 coherence，因此不是无条件叠亮。晚加入按包年龄不补播已过时的瞬时反馈。

## 网络与预算

服务器裁定发射、交互、唯一 ID 与风附着；客户端只重建运动和表现，现有托管命中管线禁止客户端再次对NPC结算。ExtraAI 传递波状态、剩余寿命与反射历史；波包出生后一次性发送，纯视觉不逐帧发包。

本机源码中 NewProjectile 仅在 `Owner == Main.myPlayer` 时填 ai 参数并自动广播；服务器替玩家生成必须显式填入哨兵参数，子波在继承字段完整后显式 SyncProjectile。销毁沿用 CombatGlobalProjectile 的服务器 KillProjectile 通知，哨兵也使用零元素量托管入口以复用该路径。

同时最多48个波包、12个哨兵；交汇检测是受限 O(n²)，不建立全屏声场。按所有者隔离交互，暂不支持队友波融合；玩家退出或死亡结束哨兵，已发出的波继续耗尽剩余寿命。达到预算而无法生成子波时保留原波，避免丢失攻击。

原生弹幕槽已满时拒绝生成，避免默认 NewProjectile 回收无关旧弹幕。服务器在原生身份空间轮转分配空闲 identity，减少旧销毁回传与紧邻槽位复用冲突；自定义 ulong ID 用于机制和调试，不替代原生身份。表现由每刻姿态快照续期，若引擎越界直接令实体失活而未回调 OnKill，最多0.1秒后自行清理。

## 验证和文件职责

| 文件 | 职责 |
| --- | --- |
| Contents/Acoustics/SoundRules.cs | 集中配置、相量公式、反射、方向退化与SAT |
| Contents/Acoustics/SoundWave.cs | 波包原生碰撞／免疫、元素面板、ExtraAI与视觉绑定 |
| Contents/Acoustics/SoundSentry.cs | 测试哨兵单次发射、朝向、生命周期 |
| Contents/Acoustics/SoundDemoSystem.cs | 权威调度、反射／融合、命令和可选靶标 |
| Contents/Acoustics/SoundVisual.cs | 渐变波峰、接收与融合压缩条纹 |
| Contents/Development/SoundLiveTests.cs | 隔离单人真实AI与命中、截图 |
| Contents/Development/SoundNetworkTests.cs | 专服／双客户端TCP检查 |
| Tests/Acoustics | 独立数学、几何、生命周期回归工程 |
| Tests/Run-SoundLive.ps1、Run-SoundNetwork.ps1 | 构建、打包、隔离运行、报告检查 |
| Localization 两语言文件 | 波包和哨兵名称 |
| docs/TODO.md、CHANGES.md、testing | 当前状态、迭代原因、实际证据 |

运行入口：

```powershell
dotnet build AdvancedMod.csproj --no-restore -p:BuildMod=false -v:minimal
dotnet run --project Tests/Acoustics/Acoustics.Tests.csproj -- docs/testing/2026-10-25-sound-offline.json
pwsh -File Tests/Run-SoundLive.ps1
pwsh -File Tests/Run-SoundNetwork.ps1 -TmlDirectory 'D:\Program Files (x86)\Steam\steamapps\common\tModLoader'
```

实机脚本复用项目已有隔离策略，需要 obj/sound-live 下的测试角色、世界及三个模组。正常游戏不启用测试环境变量、不自动操作角色、不截图。结果见 [本轮验收记录](testing/2026-10-25-sound.md)。

## 当前限制

这是固定数值的召唤类测试攻击，尚无物品、配方、持续寻敌、玩家属性成长或正式音效。波包穿墙；传播速度固定且低于包络厚度，尚不支持任意高速移动／跨帧连续扫掠。干涉采用中点单次采样，不是完整交叠区域逐点相消。哨兵与波包使用独立预算，未接入正式武器的召唤栏位管理。

待实机专项：主机模式、晚加入／断线重连、高延迟／丢包、混合第三方弹幕、暂停与缩放、运行中切换分辨率、世界退出与模组重载。继承当前项目的客户端信任模型，不是反作弊协议。正式武器阶段需再确认数值成长、每条攻击链的多目标总预算、召唤管理和艺术表现。
