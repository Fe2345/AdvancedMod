using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class Reward_Of_The_God_Of_Eye : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("魔眼之神的恩赐");
            Description.SetDefault("生命回复大大提高");

            Main.buffNoSave[Type] = false;
            Main.debuff[Type] = false;
            this.canBeCleared = true;
            Main.lightPet[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen = 200;
        }
    }
}
