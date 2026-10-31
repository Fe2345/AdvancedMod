# 毁灭者弹幕与旋转预警源码调查（2026-10-30）

## 原版魔焰为何有尾迹

本机1.4.4.9反编译Projectile.cs中，CursedFlameHostile为96号：aiStyle=8，scale最终为1.3。AI的95／96分支每次更新在position+velocity生成75号CursedTorch粒子，继承弹幕速度、alpha=100、scale=3×弹幕scale，且noGravity=true。连续生成与粒子后续衰减形成尾迹，不依赖一条oldPos贴图条带。

原实现接管了探测怪PinkLaser并返回PreAI=false，只画96号贴图，原生aiStyle=8不会执行；虽然补过默认尺寸CursedTorch，但没有原版的大粒子参数。现在一／二阶段共用DestroyerFlameVisuals.Emit恢复对应参数，保留各自的运动、伤害及元素语义，不通过切换原版弹幕类型引入额外AI副作用。

## Fargo预警与运动

通过GitHub插件读取Fargo-Team/FargosSoulsMod默认分支源文件，正文存于obj/DestroyerAttackResearch。本轮未运行Fargo模组；以下是源码证据。

- [GlowLine.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/Projectiles/GlowLine.cs)：CanDamage返回false。AI维护counter、锚点、rotation、alpha；第3／4类对应Abomination剑招，使用AngleLerp逐渐指向剑生成方向。PreDraw沿rotation方向拉伸光条贴图，长度通常3000，毁灭者专用分支5000，使用透明度与低Alpha光色表现。它的Colliding提供线段检测，但CanDamage仍禁止伤害；不能把预警类的线段检测等同于实际攻击。
- [ArcTelegraph.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/Projectiles/ArcTelegraph.cs)：ShouldUpdatePosition=false，跟随父NPC计算朝向；按角度采样扇区边缘，用VertexStrip与Vertex_ArcTelegraph shader绘制，Colliding=false。
- [DestroyerScanTelegraph.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/Projectiles/Masomode/DestroyerScanTelegraph.cs)：跟随毁灭者头部位置与速度方向；把半径、角宽、锚点和屏幕参数传入shader，随剩余时间收束扫描范围。它也是无碰撞提示，不是鞭打轨迹。
- [AbomSword.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/Bosses/AbomBoss/AbomSword.cs)：实际剑攻击是另一个弹幕，按ai角速度／MaxUpdates转动单位方向，宽度随生命周期变化，PreDraw调用Luminance PrimitiveRenderer绘制剑形条带。说明预警和攻击可各自控制时钟与表现，不应让预警自己变成碰撞伤害。

读取时的正文Blob SHA（不是提交SHA，不伪称固定提交）：

| 文件 | Blob SHA |
| --- | --- |
| GlowLine.cs | 3f5a0413aeda798e2d2f18b3ae1eea466b3f4927 |
| ArcTelegraph.cs | 97ea1298f8c30ea7fe2e8a42d4302c6affd8b635 |
| DestroyerScanTelegraph.cs | d437e5d04459aac538dce80fcae9c05f1f05651f |
| AbomSword.cs | b8305232f3cb0dc385ac628c66a55fd15a607df3 |

## 对本项目草图的落地

作者确认的需求是固定扫过半圆并停在终止线，因此不照搬Fargo持续AngleLerp追踪玩家的分支。每一鞭生成时由服务器锁定玩家位置、侧面根部与扫向；预警角度=start+sign×π×clamp(timer/30)，前30刻完成180度，之后15刻保持终止线。起止边界、外缘和已扫过淡扇区辅助表达完整范围。用官方ModProjectile／PreDraw即可实现，不添加IL Hook，也不复制Fargo贴图或shader。

随后18刻使用原版头身尾贴图构成曲线鞭形。曲线点始终限制在相同的半圆内；显示长度1200、根部距锁定玩家700、节段绘制缩放0.5为首版视觉参数，未当作正式伤害尺寸。三鞭分别取样根部与目标，预警和鞭形均CanDamage=false；原本NPC链保持隐藏并关闭接触伤害。

Crash每次进入1024范围只触发一次抽中的技能；12向火焰、6个随机圆周位置向锁定圆心发射风刃、前8个身体各自瞄准、4旋风绕锁定圆心缩圈、头部发射后锁定方向的寒霜光束。作者确认伤害基准沿用GetAttackDamage_ForProjectiles(25,22)，水25%、寒霜50%、其余100%，均带对应元素与神圣资格，首版附着量1。所有试调参数集中在DestroyerAttackRules。

运动频率与后半段停滞问题仅记入TODO，本轮不调整Crash／Idle节奏或跟随路径；旋风返程和低于40未释放时自动脱离同样仍未改动。
