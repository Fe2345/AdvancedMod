using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>未确定正式获取途径时的开发测试入口，不添加临时合成配方。</summary>
public sealed class HallowBladeCommand : ModCommand
{
    public override string Command => "hallowblade";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/hallowblade";
    public override string Description => "领取一把天裁之刃用于连招验收";
    public override void Action(CommandCaller caller, string input, string[] args) =>
        caller.Player.QuickSpawnItem(caller.Player.GetSource_Misc("HallowBladeTest"), ModContent.ItemType<HeavensJudgment>());
}
