# 水体实验预览器

2026-10-27：独立 FNA 宿主，直接共享 `Basic/Graphics/Water`，不依赖 Terraria 类型。

在仓库根目录运行 `pwsh -File Tools/WaterPreview/Run.ps1 -Vortex`。

- [完整说明与操作表](../../docs/WaterVortex.md)
- [实测性能与限制](../../docs/WaterVortex-Performance.md)
- [融合预设](Presets/fusion.json)、[旋涡预设](Presets/vortex.json)
- [真实融合动画](../../docs/assets/water/fusion.gif)、[真实旋涡动画](../../docs/assets/water/vortex.gif)

`Benchmark.ps1` 测量全部 18 配置；`Run-Tml.ps1` 使用本机已有的隔离测试存档验证最小接入。后者首次运行需要 `obj/lightning-live` 中的测试角色、世界与依赖模组。

截图/GIF 导出使用 GPU 实际画面；动画不是烘焙运行依赖。使用系统 Python 可编译 shader，导出 GIF 另外需要 Pillow。
