using System.Collections.Generic;
using System.ComponentModel;
using AdvancedMod.Basic.Calculate.Diagnostics;
using Terraria.ModLoader.Config;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>本机开发日志设置，不同步为游戏规则，也不改变计算结果。</summary>
public sealed class CombatTraceConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(false)] public bool Enabled;
    public List<string> SourceIds = new();
    public List<int> PlayerIndices = new();
    public List<string> TargetIds = new();

    internal CombatTraceOptions Options { get; private set; } = CombatTraceOptions.Disabled;
    public override void OnLoaded() => Refresh();
    public override void OnChanged() => Refresh();
    private void Refresh() => Options = new(Enabled, SourceIds ?? new(), PlayerIndices ?? new(), TargetIds ?? new());
}
