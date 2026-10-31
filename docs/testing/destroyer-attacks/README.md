# 毁灭者弹幕与预警验收（2026-10-30）

本轮实现五类Crash攻击、一／二阶段魔焰粒子尾迹和Ultimate无伤害预览。攻击频率过高及后半段近乎不动仅记录，运动参数未调整。设计与源码依据见[专项研究](../../Destroyer-Attack-Research.md)及[Hallow设计](../../Hallow.md)。

## 已执行验证

| 验证 | 结果 | 报告 |
| --- | --- | --- |
| 构建 | 0警告、0错误 | 隔离客户端脚本构建输出 |
| 客户端弹幕专项 | 32/32 PASS | [report.json](report.json) |
| 真实TCP服务器＋两客户端（含晚加入） | 47/47 PASS | [服务器](network-server.json)、[客户端1](network-client1.json)、[客户端2](network-client2.json) |
| 一阶段回归 | 45/45 PASS | [phase1-regression.json](phase1-regression.json) |
| 二阶段流程回归 | 35/35 PASS | [phase2-regression.json](phase2-regression.json) |

复现脚本：`Tests/Run-DestroyerAttackLive.ps1`、`Tests/Run-MechanicalNetwork.ps1 -PhaseTwo -Attacks`、`Tests/Run-DestroyerPhaseOneLive.ps1`、`Tests/Run-DestroyerPhaseTwoLive.ps1`。使用隔离存档和测试包。

客户端专项检查各类数量、伤害倍率、元素／神圣资格、来源归属、运动和光束碰撞；通过原生`Projectile.Damage`实际造成玩家受伤和神圣反应，验证充能增加，没有直接写入计数。联机覆盖实际反应回执、满20次释放流程和晚加入状态；弹幕展示夹具固定年龄及寿命以保留同步样本，不证明自然生命周期或高延迟战斗表现。

## 实机图像

已查看[魔焰](Fire.png)、[风刃](Wind.png)、[逐节激光](Laser.png)、[水旋风](Water.png)、[寒霜](Frost.png)和[Ultimate关键帧](ultimate-sequence.png)。Ultimate图由真实客户端调用正式PreDraw，固定年龄绘制六帧，便于核对起始、旋转、停留与鞭形；不是自然战斗连续录像。

Ultimate预警30刻旋转180度、15刻保持终止方向，然后18刻鞭形扫过相同半圆。鞭形使用原版头／体／尾贴图绘制，实际NPC链隐藏、关闭接触伤害；预览弹幕不能造成伤害。正式鞭打判定尚未接入。

## 验证边界

- 伤害以原版探测怪`GetAttackDamage_ForProjectiles(25,22)`为基准，水25%、寒霜50%、其余100%；速度、范围及时长为集中配置的试验值。
- 通过受控夹具不代表完整战斗平衡通过。头部攻击过密、后半段运动不足仍待后续调整。
- 旋风返程转向及未释放数低于40时自动脱离未在本轮修改。
- 正式Ultimate伤害、人工高延迟及完整战斗手感未验收。
