using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class KingCrimson : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("绯红之王");
            Description.SetDefault("时间被删除了");

            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
            Main.lightPet[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<NPCs.AdvancedGlobelNPC>().KingCrimson = true;
        }
    }
}
