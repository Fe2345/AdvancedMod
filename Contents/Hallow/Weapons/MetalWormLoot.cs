using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Contents.Hallow.NPCs;

namespace AdvancedMod.Contents.Hallow.Weapons;

public sealed class MetalWormLoot : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.TheDestroyer;
    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !npc.GetGlobalNPC<DestroyerBehavior>().Hallow) return;
        int itemType = ModContent.ItemType<MetalWormWhip>();
        if (Main.expertMode)
        {
            // 原版按参战记录发送个人掉落；保存value，避免接口副作用影响原有金钱掉落。
            float value = npc.value;
            npc.DropItemInstanced(npc.position, npc.Size, itemType);
            npc.value = value;
        }
        else Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, itemType);
    }
}

public sealed class MetalWormCommand : ModCommand
{
    public override string Command => "metalworm";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/metalworm";
    public override string Description => "领取金属蠕虫鞭和刃杖用于联动验收";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        caller.Player.QuickSpawnItem(caller.Player.GetSource_Misc("MetalWormTest"), ModContent.ItemType<MetalWormWhip>());
        caller.Player.QuickSpawnItem(caller.Player.GetSource_Misc("MetalWormTest"), ItemID.Smolstar);
    }
}
