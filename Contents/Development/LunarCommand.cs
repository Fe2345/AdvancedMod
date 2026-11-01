using System.Globalization;
using AdvancedMod.Basic.Global;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

// 仅提供观察 UI 的手动赋值入口，不接入能量获取／消耗事务。
public sealed class LunarCommand : ModCommand
{
    public override string Command => "lunar";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/lunar <月曜度>";
    public override string Description => "单人开发测试：设置月曜度（0～720）";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Terraria.Main.netMode != Terraria.ID.NetmodeID.SinglePlayer)
        {
            caller.Reply("月曜度测试命令仅限单人使用；联机事务尚未接入。");
            return;
        }
        if (args.Length != 1 || !float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float rate))
            throw new UsageException(Usage);
        var state = caller.Player.GetModPlayer<PlayerSpecialState>();
        state.LunarRate = rate;
        caller.Reply($"月曜度：{state.LunarRate:0.##}/{PlayerSpecialState.MAX_LUNAR_RATE:0}");
    }
}
