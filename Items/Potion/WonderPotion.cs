using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;

namespace AdvancedMod.Items.Potion
{
    public class WonderPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("奇迹药水");
            DisplayName.AddTranslation(GameCulture.English, "Wonder Potion");
            Tooltip.SetDefault("将会有奇迹发生");
            Tooltip.AddTranslation(GameCulture.English,"There will be Wonder happens.");
        }

        public override void SetDefaults()
        {
            item.width = 14;
            item.height = 24;
            item.useAnimation = 17;
            item.useTime = 17;
            item.maxStack = 30;
            item.rare = ItemRarityID.Pink;
            item.value = Item.sellPrice(0, 0, 50, 0);
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.UseSound = SoundID.Item3;
            item.consumable = true;
            item.useTurn = true;
            item.buffType = ModContent.BuffType<Buffs.Not_DeBuff.Wonder>();
            item.buffTime = 60000;
        }
    }
}
