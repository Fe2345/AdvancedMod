using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Buffs.Debuff
{
    public class VoidPressure : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("虚空威压");
            Description.SetDefault("虚空模式的压力让你体力不支\n生命再生速度减少一半");
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen = (int)(player.lifeRegen * 0.5f);
        }
    }
}
