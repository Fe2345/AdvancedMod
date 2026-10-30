# 原神风格 Boss 血条源码调查

日期：2026-10-29。以下为首轮公开源码调查与候选方案的历史记录。后续作者已确定最终覆盖全部 Boss、首轮使用 ModBossBar 适配毁灭者；当前实现和验证口径以 [实验说明](BossBar-Usage.md) 与 TODO 为准。

## 需求与结论

目标为屏幕下方的细长血条，两端尖角，上方 Boss 名称与称号，下方右端圆形特殊能量提示；能量图标贴图、上限及当前值可控。

官方 API 已能实现，无需因改变形状而引入 IL Hook 或删除整个界面层。需要区分：
- 整套样式替换：ModBossBarStyle.PreventDraw 返回 true，随后在该样式的 Draw 自绘。灾厄和 Infernum 使用此路线。
- 指定 Boss 替换：给 NPC.BossBar 分配 ModBossBar，在 PreDraw 自绘后返回 false。仍可保留官方选怪与 ModifyInfo 数据采集。
- 全局拦截默认绘制：GlobalBossBar.PreDraw 可修改参数或自绘并返回 false，但不能保证拦到其他 ModBossBarStyle 的独立绘制。

调查后作者选择先以指定 Boss 的 ModBossBar 进行毁灭者实验；未新增通用接口或注册器。

## 已核实的模组实践

### 灾厄 Calamity

来源：[BossHealthBarManager.cs](https://github.com/CalamityTeam/CalamityModPublic/blob/1a8cebd27ec5615316b78f71973446b5528d2b78/UI/BossHealthBarManager.cs)。
读取分支 1.4.4，调查时分支提交为 1a8cebd27ec5615316b78f71973446b5528d2b78。公开分支不等同于用户安装包版本。

- BossHealthBarManager 继承 ModBossBarStyle，PreventDraw 为 true；没有覆盖 PreventUpdate。
- Update 扫描活动 NPC，以 Boss、允许显示的小 Boss、模组专用允许标记和排除表决定对象；维护 Bars，最多四条，处理关闭动画后移除。
- Draw 从右下方向上排列，背包或入侵等界面出现时左移，逐条调用 BossHPUI.Draw。
- 使用 SpriteBatch.Draw 画主血量、近期伤害残留和分隔条；目标矩形宽度由血量比例决定。字体单独画百分比、名字和当前／最大生命。
- 多部件使用 OneToMany 映射；分裂蠕虫、月总使用 SpecialHPRequirements 专用计算。InitialMaxLife 仅在新上限更高时更新，避免部件减少导致分母一路缩小。
- 特殊状态来自 CalamityGlobalNPC 的 CurrentlyEnraged、CurrentlyIncreasingDefenseOrDR 等字段；红／灰分隔条和名字光效由状态与动画计时器驱动。剩余肢体或仆从数量单独显示。

借鉴：把战斗数值采集与呈现分开；特殊状态由战斗逻辑提供，不靠 UI 猜测。不能照搬按全场 NPC 类型聚合的逻辑，同种 Boss 同场需要按所属实例过滤。

### Infernum

来源：[BossBarManager.cs](https://github.com/InfernumTeam/InfernumMode/blob/master/Content/BossBars/BossBarManager.cs)、[BaseBossBar.cs](https://github.com/InfernumTeam/InfernumMode/blob/master/Content/BossBars/BaseBossBar.cs)、[InfernumGlobalBossBar.cs](https://github.com/InfernumTeam/InfernumMode/blob/master/Core/GlobalInstances/InfernumGlobalBossBar.cs)。本次读取 master，链接后续可能变化。

- 同样继承 ModBossBarStyle，PreventDraw 为 true，自行维护最多四条血条。
- BaseBossBar 拆分外框、主条、尖端、图标框、无敌覆盖层和阶段标记贴图。
- 主条用 Shader，绘制时切换 SpriteBatch 为 Immediate，使用 Main.UIScaleMatrix，完成后恢复 Deferred；这说明 Shader 可用，但普通细长条不必照搬其切换。
- 依据 Boss 的阶段阈值计算当前阶段内的生命比例，同时单独显示总生命百分比。阶段条可能在进入下一阶段时重新填满，不能把它直接当作普通总血量条。
- 无敌读 dontTakeDamage；增防／减伤和激怒读取灾厄状态，以覆盖层和头像粒子反馈。
- ModCall 可登记阶段信息和自定义图标；自身默认图标优先尝试 Boss 头像。
- 另有 GlobalBossBar 修正默认路线的血量／护盾，例如无尽虚空从 ExtraAI 读取护盾。这条默认路线的修正不能视为其独立样式会自动执行。

借鉴：美术分层、阶段与无敌状态明确分离。其阶段机制、ModCall 和粒子管理并非本需求必需。

### Fargo's Souls

来源：[MutantBossBar.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/BossBars/MutantBossBar.cs)、[CompositeBossBar.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/BossBars/CompositeBossBar.cs)。本次读取 master，链接后续可能变化。

- 已检查的类使用 ModBossBar，而非全局接管样式。
- MutantBossBar.ModifyInfo 提供 NPC 当前／最大生命，排除不活动或城镇状态；GetIconTexture 提供 Boss 头像。
- 特定 AI 状态下 PreDraw 关闭数字并轻微抖动 BarCenter，返回 true，继续默认绘制。
- CompositeBossBar 将木马本体与头、手臂生命合并；最大值使用本体加保存的部件最大生命。
- 暗影冠军以属于该 Boss 的球体数量构造 Shield／ShieldMax。说明护盾可以代表需处理的目标数量，不一定是另一份生命值。

借鉴：ModifyInfo 负责生命／护盾语义，PreDraw 负责呈现。与灾厄不同，此样本不能作为“已经完全自绘”的证据。

## 关闭原版显示后的真实调用链

来源：[BigProgressBarSystem 补丁](https://github.com/tModLoader/tModLoader/blob/c9545cf27d56c456321e1896274c02adf67d0140/patches/tModLoader/Terraria/GameContent/UI/BigProgressBar/BigProgressBarSystem.cs.patch)、[BigProgressBarHelper 补丁](https://github.com/tModLoader/tModLoader/blob/c9545cf27d56c456321e1896274c02adf67d0140/patches/tModLoader/Terraria/GameContent/UI/BigProgressBar/BigProgressBarHelper.cs.patch)、[BossBarLoader](https://github.com/tModLoader/tModLoader/blob/c9545cf27d56c456321e1896274c02adf67d0140/patches/tModLoader/Terraria/ModLoader/BossBarLoader.cs)、[ModBossBar](https://github.com/tModLoader/tModLoader/blob/c9545cf27d56c456321e1896274c02adf67d0140/patches/tModLoader/Terraria/ModLoader/ModBossBar.cs)。
本次核对官方 stable 分支；本项目 targets 指向本机 Steam tModLoader，但尚未将以下源码与本机程序集逐方法比对。

### 整套样式

```text
BigProgressBarSystem.Update
  若未 PreventUpdate：原版选目标、ValidateAndCollectNecessaryInfo
  始终调用 CurrentStyle.Update

BigProgressBarSystem.Draw
  若未 PreventDraw：当前血条.Draw
  始终调用 CurrentStyle.Draw
```

PreventDraw 只跳过默认绘制部分，不跳过样式自己的 Draw。PreventUpdate 同理只跳过默认更新部分。文档括号描述容易产生歧义，应以调用点为准。

样式通过游戏“界面”的 Boss 血条样式选项选择；只有当前样式执行这套入口。注册样式不等于强制替换玩家选中的灾厄或 Infernum 样式。没有需要时不强改玩家设置。

关闭默认绘制后，原来的 ModBossBar／GlobalBossBar.PreDraw、PostDraw 通常不再经过该链，不能期待其他模组的默认绘制修正自然生效。IBigProgressBar 没有统一暴露所有原版血条的生命／护盾缓存；currentBar 是 ModBossBar 时可读取其 Life、LifeMax、Shield、ShieldMax，但仍需保证数据已经正确采集。多条共享一个 ModBossBar 实例时，不能将它的缓存当作每个 NPC 的独立状态。

### 单条或全局 PreDraw

```text
当前血条.Draw
  → BigProgressBarHelper.DrawFancyBar
  → 构造 BossBarDrawParams
  → GlobalBossBar.PreDraw（全部调用并合并结果）
  → 若仍允许且存在对应 ModBossBar：ModBossBar.PreDraw
  → 若允许：DrawFancyBar_TML
  → PostDraw（即使默认绘制被跳过也调用）
```

在自己的 PreDraw 用收到的 SpriteBatch 画完并返回 false，即可取消默认框、头像、血量与默认数字，自己决定全部布局。其他模组的 PostDraw 仍可能叠加内容，不能称为隔离所有模组绘制。不要在这里再次调用默认 DrawFancyBar 试图递归复用。

ModifyInfo 返回值分别为：null 使用基本单 NPC 生命逻辑；true 使用所提供的数据；false 本条无效并允许系统另选目标。直接调用 ModifyInfo 不能完整替代 ValidateAndCollectNecessaryInfo 的语义。

默认 DrawFancyBar_TML 固定有效条尺寸 456×22、贴图 516×348 和六帧切片约定。因此长细条不宜仅更换 BarTexture，应自绘布局。

## 本项目建议（未实施）

### 接入选择

若目标是本项目神圣 Boss 专用条，先采用 NPC.BossBar + ModBossBar.PreDraw；神圣标记属于实例，应在实例已确定且客户端已获知时设置／恢复对应 BossBar，而非按 NPC 类型覆盖所有原版实例。

若目标是所有 Boss 统一替换为原神风格，则采用 ModBossBarStyle；同时需要明确单条目标选择、多条排列及原版特殊血量适配。这比只写绘制函数多一层责任。

两者都不要求额外 UIState 或删除 Vanilla 界面层。样式层只拿数据画画，AI 与联网状态留在 NPC／既有行为组件中。

### 布局与贴图

- 屏幕底部居中锚定；宽度、高度、底部距离、名称／称号间距、能量圆偏移集中配置。UI 缩放和屏幕坐标统一，避免混用世界坐标与 Main.screenPosition。
- 外框为左尖端、中段、右尖端；尖端保持比例，中段延展。血量填充分层绘制，不把整张含尖角贴图横向压缩，否则角度和描边厚度随血量变化。
- 对带斜边的填充，可裁切完整遮罩贴图或使用遮罩 Shader；需要明确空血边缘和极少血量时的形状。
- 名称与称号各用本地化文本；测量实际字宽后居中。称号不是 NPC 自带的通用字段，应由内容明确提供。
- 能量圆位于血条右端下方，图标、底盘／外框、进度环分层。图标贴图与进度遮罩独立，替换任意图标时无需要求图标本身为圆形。
- 环形进度可用贴图加角度裁切 Shader，或圆弧几何。仓库已有 Paths.Arc 与 VfxMesh.Stroke，可优先评估复用几何；现有世界效果渲染入口不能未经检查直接用于 UI，需要 UI 矩阵与绘制状态适配。暂不为此创建新通用设施。
- 贴图只在客户端加载并缓存 Asset<Texture2D>；不要每帧创建 Texture2D 或从磁盘读图片。

### 数据与特殊状态

建议内容至少提供：名称、可选称号、能量图标、是否显示能量、当前能量、最大能量。样式色与尺寸属于视觉配置；生命、能量和无敌等属于战斗实例数据。

显示比例为 Clamp(current / maximum, 0, 1)，先检查 maximum > 0 以及数值有限；负数、超上限只钳制显示，不擅自修改战斗逻辑。未配置能量或最大值无效时建议隐藏，最终规则待作者确认。

特殊能量不要占用 Shield／ShieldMax：护盾和蓄能可同时存在，语义不同。护盾、无敌、激怒、阶段分别读取明确状态；dontTakeDamage 只能表示拒绝伤害，不能自动推出“护盾剩余值”或“已满能量”。

毁灭者等共享生命 Boss 应读所属生命根，不累加各节生命；独立可破坏部件才按战斗设计合并。当前生命、最大生命和阶段切换必须采用同一口径，不能让击毁一只手臂后血条反而变满。

权威能量由服务器／单人逻辑更新，利用已有 ExtraAI 等同步入口传递给客户端。绘制只做显示插值，不在 Draw 改实际能量。晚加入、NPC 槽位复用和同类型多实例都需要验证；无需同步每一帧视觉插值。

## 后续实施前需确定的范围

1. 替换本项目指定 Boss，还是提供所有 Boss 可选的全局样式？
2. 同屏多 Boss 只显示主目标，还是多条纵排？
3. 圆形提示是外环进度，还是图标内部填充？当前建议图标固定、外环进度。
4. 能量满额后的视觉与文本是否需要；未配置称号或能量时如何隐藏。

上述为后续设计选择，不影响本次源码调查完成；未默认批准新抽象。

## 验证状态

- 已完成：三组模组源码、官方绘制调度、生命／护盾接口、现有项目图形工具的静态核查。
- 未执行：候选代码构建、UI 实机、第三方样式兼容、多人／晚加入测试。
- 后续应覆盖：不同分辨率与 UI 缩放、长中文名／称号、0／满／超限能量、无敌／护盾同时存在、Boss 死亡与撤退、同类型双实例、共享生命蠕虫、多部件死亡、默认／灾厄／Infernum 样式选择。

