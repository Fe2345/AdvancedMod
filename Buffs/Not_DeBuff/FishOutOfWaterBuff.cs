using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class FishOutOfWaterBuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_168";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("离水之鱼");
            Description.SetDefault("小怪而已");

            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
            Main.lightPet[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<Items.Mount.FishOutOfWaterMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }
}
