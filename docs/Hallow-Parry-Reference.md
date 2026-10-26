# 远古方舟格挡参考（2026-10-25）

来源为CalamityTeam/CalamityModPublic，固定提交1a8cebd27ec5615316b78f71973446b5528d2b78。当前远古方舟物品类叫TrueArkoftheAncients；不能误用断裂方舟对应的ArkoftheAncientsParryHoldout。以下仅为源码核对，未进行灾厄实机验证。

## 实际规则

- 右键生成短动作持握弹幕，初始化时锁定鼠标方向，剑沿该方向向前伸出再收回；不依赖持续按住右键，也没有持续半伤格挡阶段。
- ParryTime=15，判定条件Timer<=15（代码包含端点），未设置extraUpdates；约0.25秒有效动作。MaxTime=340，整段生命周期约5.67秒，动作结束后的约325刻显示冷却条。成功不会缩短该生命周期。
- 每次最多成功一次。不能在自身普通挥刀弹幕存在时起手；各方舟系列的招架持握弹幕互斥，因此共用这道冷却门槛。冷却弹幕并不阻止恢复左键普通攻击，没有成功招架自动推进普通连招的代码。
- 方向判定不是正面半圆：使用玩家前方剑刃线段与目标碰撞框相交。线长100×scale，碰撞宽参数24；scale在有效窗口约1.4起步，起点偏移由10×(1+0.8sin(Timer/15×π))控制。
- 敌对弹幕要求active、hostile、damage>1、速度长度×(extraUpdates+1)>1、尺寸向量长度<300，且与剑线相交。成功把该弹幕flatDR至少设为30、flatDRTimer至少设为60；伤害应用处是FinalDamage.Flat减去flatDR。此路径不删除／反弹弹幕，不给玩家通用无敌，不能解释为必定全免。
- 直接击中NPC同样触发招架成功；仅当NPC.damage>0时给予玩家通用无敌帧：基础60刻，longInvince再加30刻。没有接触伤害的NPC不会因此给予通用无敌。
- 两类成功均把该武器Charge填到10。普通挥刀每次消耗1层，充能伤害倍率1.25，并有额外远程攻击变化；不是天裁要求的两把追踪反击剑。

## 源码

- [招架弹幕](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/Melee/TrueArkOfTheAncients_ParryHoldout.cs)
- [物品与使用门槛](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Items/Weapons/Melee/TrueArkoftheAncients.cs)
- [无敌帧计算](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Utilities/PlayerUtils.cs)
- [无敌帧常量](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Balancing/BalancingConstants.cs)
- [弹幕减伤应用](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/Projectiles/CalamityGlobalProjectile.cs)

## 天裁最终取舍

作者在调研后明确调整为持续右键、起手360刻冷却、20刻完美窗口；每次最多成功一次，松开或成功结束。保留弹幕伤害／速度／尺寸筛选与剑线碰撞；普通50%、完美100%减伤均持续60刻，只保护完成招架的玩家且绑定被招架来源，不新增无敌帧。完美成功刷新原连段衔接期限，不自动挥刀；满足正面512半圆存在敌人的条件才派生±192双剑。

最初60刻完美窗口方案已撤销，不再列为待实现。共鸣、反击及实现验证状态见[Hallow设计](Hallow.md)与[TODO](TODO.md)。
