using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class Fate : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("命运");
            DisplayName.AddTranslation(GameCulture.Chinese, "命运");
            DisplayName.AddTranslation(GameCulture.English, "Fate");
            Description.SetDefault("你将躲避你死亡的命运");
            Description.AddTranslation(GameCulture.Chinese, "你将躲避你死亡的命运");
            Description.AddTranslation(GameCulture.English, "you will avoid you fate of dye");

            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
            this.canBeCleared = true;
            Main.lightPet[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
    }
}
