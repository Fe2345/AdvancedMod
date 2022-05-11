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
            //DisplayName.AddTranslation(GameCulture.English, "Wonder Potion");
            Tooltip.SetDefault("将会有奇迹发生");
            //Tooltip.AddTranslation(GameCulture.English,"There will be Wonder happens.");
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 24;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.useTurn = true;
            Item.buffType = ModContent.BuffType<Buffs.Not_DeBuff.Wonder>();
            Item.buffTime = 60000;
        }
    }
}
