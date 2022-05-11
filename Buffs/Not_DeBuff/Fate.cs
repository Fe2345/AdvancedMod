using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class Fate : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("命运");
            Description.SetDefault("你将躲避你死亡的命运");

            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
            Main.lightPet[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
    }
}
