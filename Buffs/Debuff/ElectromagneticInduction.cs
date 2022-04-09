using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Buffs.Debuff
{
    public class ElectromagneticInduction : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("电磁感应");
            Description.SetDefault("你对电磁波过敏");
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            longerExpertDebuff = false;
            canBeCleared = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense -= 20;
            player.lifeRegen = (int)(player.lifeRegen * 0.5);
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<NPCs.AdvancedGlobelNPC>().TheWorld = true;
        }
    }
}
