# 元素攻击开发入口与实机验收

## 武器与弹幕

使用 `AdvancedMod.Basic.Utils.ElementAttacks` 配置现有 Item 或生成弹幕。武器的正常近战行为仍由 TML 执行；无需自行调用 StrikeNPC，也无需在 Modify 中手工扣元素。

```csharp
// ModItem.SetDefaults 中，在 Item 基础属性设置完成后调用。
ElementAttacks.ConfigureWeapon(Item,
    new TagSet(new[] { new TagEntry(false, TagEnum.Pyro) }),
    elementAmount: 1.5, attackId: "AdvancedMod:测试火剑");

// 消耗与 CD 修正属于这个物品自身，不会传到弹幕。
var weapon = Item.GetGlobalItem<CombatGlobalItem>();
weapon.ElementConsumptionMultiplier = 0.5f;
weapon.ReactionCooldownMultiplier = 1f;
```

```csharp
// ModItem.Shoot 中，damage 使用 TML 传入的已计算值。
int index = ElementAttacks.NewProjectile(source, position, velocity, type,
    damage, knockback,
    new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }),
    elementAmount: 1, owner: player.whoAmI,
    attackId: "AdvancedMod:测试水弹");
return false; // 已经生成，不再让原版重复生成一次。
```

NewProjectile 封装保留原 `IEntitySource`，让 TML 正常处理源物品、面板、暴击等信息。标签和元素量在 GlobalProjectile.OnSpawn 中、首次网络包发送前写入；由于该 Hook 晚于 ModProjectile.OnSpawn， `ModProjectile.OnSpawn` 中尚不能读取本次参数。索引耗尽时原样返回原生失败索引，不写入其他槽位。不要从当前手持物品推断来源；子弹幕需要显式传自己的标签／元素量。两个修正保留弹幕自身定义，不继承武器或父弹幕。

NPC 接触伤害可用 `ConfigureNpcContact(npc, tags, elementAmount, attackId)` 配置；当前 NPC 接触入口两个修正均为 1。

联机中 NPC 元素状态及反应由服务器结算；玩家受伤保留 TML 所属客户端权威。物品／弹幕通过 NetSend/ExtraAI 同步标签、元素量和独立修正，NPC 快照随 SyncNPC 下发，玩家快照经服务器校验所属槽后转发。镜像不参与静置反应。真实专用服务器＋双客户端共 22 项断言已通过，详见 TODO 与 testing 下的报告。

## 原生入口与提交

- 物品→NPC：包住 TML `Player.ProcessHitAgainstNPC` 的完整单目标调用。
- 弹幕→NPC：包住 `Projectile.Damage`，在真实 Modify 阶段准备目标方案，在 `StrikeNPC` 返回后记录实际伤害，原入口完整返回后提交。目标忙碌时只排队该目标的重试，重试仍走原生碰撞与免疫，且过滤其他目标；失效弹幕不复活。
- NPC／敌对弹幕→玩家：包住带 `out HurtInfo` 的完整 `Player.Hurt`，包含免疫、取消、闪避与伤害执行。重入时同步返回值为 0，最终排队结果通过 `ElementHitSystem.Resolved` 观察，调用者不能把立即返回的 0 当作排队请求的最终结果。
- Global／ModPlayer 的 Calculate 入口读取 Reaction 生成的最终标签，反应系数通过 SourceDamage 比率作用一次，保留原生防御／暴击／其他修饰器。
- 只在原入口正常返回且实际伤害大于 0 时提交。取消、免疫、闪避和零伤害不提交；致死仍可提交并产生反应事实。使用目标 ElementTarget 实例身份检查阻止旧事务写入复用实体，不用 active=false 否定已成功的致死命中。
- 异常释放执行权，清空未处理队列，不回滚引擎已造成的伤害。第三方直接构造 HitInfo、绕开上述完整入口的伤害不属于本轮兼容承诺。

`PostUpdateEverything` 扫描有元素或 CD 状态的存活目标，每刻先递减再尝试一次静置反应。NPC 使用原生 incoming modifiers／ToHitInfo／StrikeNPC；玩家走原生 Hurt，仍遵守玩家免疫与闪避。静置来源为环境，Calculate 保持纯计算边界。当前临时反应表来自 `DevelopmentReactions`，系数 2／1.5／1；后续正式效果替换该表。

本机入口依据安装的 TML 1.4.4.9+2026.08.3.0 方法体核对，并与 [NPC HitModifiers](https://docs.tmodloader.net/docs/stable/struct_n_p_c_1_1_hit_modifiers.html)、[Player](https://docs.tmodloader.net/docs/stable/class_player.html)、[Projectile](https://docs.tmodloader.net/docs/stable/class_projectile.html) 的官方契约对照。更新 TML 后需要复验 detour 与执行顺序。

## 重跑实机测试

开发内容位于 `Contents/Development`。`/elementtest` 使用真实测试 NPC／弹幕与玩家 Hurt 入口，报告写到当前存档根目录的 `Logs/AdvancedMod/element-live-tests.json`。命令会修改测试玩家生命、防御和元素状态，只在测试世界使用。

自动化脚本 `Tests/Run-ElementLive.ps1`：

1. 在项目 `obj` 下准备独立存档目录，放入测试角色的 `Players/testPlayer.plr`／`.tplr`、测试世界的 `Worlds/testWorld.wld`／`.twld` 副本，以及两个依赖模组。
2. `Mods/enabled.json` 只启用 AdvancedMod、Luminance、SubworldLibrary。自动测试环境变量只由脚本设置，不写入系统配置。
3. 传入 `-TmlDirectory` 与 `-TestSaveDirectory` 运行脚本。它编译、打包到隔离目录，启动真实客户端并用 `-skipselect testPlayer:testWorld` 进入世界。
4. 测试运行时关闭自动保存，写报告后退出客户端。脚本校验报告时间，拒绝把旧报告当作本轮成功；存在失败项则报错。

测试记录与当前结果见 [TODO](TODO.md)。此自动化使用真实客户端和复制存档，区别于纯计算测试；它不等同于手工武器手感、画面或联机验收。

## 联机自动验收
运行 Tests/Run-ElementNetwork.ps1，传入 -TmlDirectory。脚本在 obj/element-network/时间戳 下建立服务器和两个客户端的独立存档，端口默认 17777，仅使用本机 TCP。测试报告必须由本轮三个进程分别生成，超时、进程退出或任意 FAIL 都使脚本失败。

NPC 当前采用每 6 刻一次的全量元素快照，全部元素槽共允许最多 2048 个白值分量；CD 镜像可能落后最多一个同步间隔，客户端不据此裁定反应。目标死亡／复用后原生 SyncNPC 及实例身份负责隔离旧请求。玩家元素状态不持久化，离开世界和重生清空。协议沿用 TML 对所属客户端攻击配置的信任，不承诺防作弊。

生成工具保留原始 IEntitySource；标签在 GlobalProjectile.OnSpawn 中写入，早于原版首次发包。ModProjectile.OnSpawn 执行在此前，因此不应在该回调中读取工具参数。嵌套原版生成不会继承父级标签；嵌套工具调用使用自己的配置。武器动态配置调用 Item.NetStateChanged，近战请求前确保当前物品包先送达。
