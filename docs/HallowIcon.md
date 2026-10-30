# 神圣提示图标

## 绘制入口核查

本机tModLoader的原版CombatText绘制直接调用`DynamicSpriteFont.DrawString`，不解析聊天图片标签。字体取`FontAssets.CombatText`，围绕文字测量尺寸的中心旋转，使用文字自身scale、alpha和`Main.GameViewMatrix.ZoomMatrix`；倒重力时另行翻转文字屏幕Y位置。

因此保留原版反应文字与伤害数字，在`ElementTextSystem.PostDrawInterface`补绘图标。图标边长取反应名`MeasureString(...).Y`，与文字共享scale、rotation和alpha；以文字中心为旋转中心把图标放在左侧，间距3像素。将原版ZoomMatrix坐标转换到当前UIScaleMatrix坐标，避免UI缩放重复作用。布局排斥区域以图标加文字的整体中心计算。原版文字池复用、到期与世界清理同时撤销图标标记。

## 资源

- `Assets/UI/HallowIcon.png`：64×64透明白色蒙版，直接取`Contents/Vfx/GoldenWard.cs`的三段贝塞尔弧控制点。无光晕、无下穿缺口，可作为后续神圣Boss图标复用。
- `Tools/Generate-HallowIcon.ps1`：读取正式控制点，8倍超采样后缩小，重新生成同一PNG；不修改领域原有的交叉缺口。
- 内容路径：`AdvancedMod/Assets/UI/HallowIcon`。反应提示字绘制时染为浅金色，资源本身可自由染色。

## 资格与同步

仅已成功提交反应的`ReactionPlan.IsHallowed`标记启用图标。只有Hallow攻击资格、未发生异化时不添加；普通反应和数字不添加。反应文字网络包新增一个bool，服务器下发和玩家受伤所属端转发均保留该标志；联机双方须使用同版本模组。

本轮构建0警告0错误，文字布局离线14/14、[单人实机13/13](testing/holy-reactions/icon/report.json)。首轮尺寸检查发现首次异步请求返回占位图，改为模组加载时预载后通过。已检查[默认画面](testing/holy-reactions/icon/palette.png)、[UI125%](testing/holy-reactions/icon/ui-125.png)和[镜头150%](testing/holy-reactions/icon/zoom-150.png)，对应实际缩放值另存JSON。联机图标与倒重力显示尚未专项实机验证。此轮仅提供后续Boss可复用的资源，不修改Boss血条或名称。
