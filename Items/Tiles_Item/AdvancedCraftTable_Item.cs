using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Tiles;

namespace AdvancedMod.Items.Tiles_Item
{
	public class  AdvancedCraftTable_Item : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("先进锻造台");
			Tooltip.SetDefault("用于合成各种困难模式物品");
		}

		public override void SetDefaults()
		{
			item.width = 40;
			item.height = 27;
			item.maxStack = 999;
			item.value = 100;
			item.rare = ItemRarityID.Blue;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.useTime = 10;
			item.useAnimation = 20;

			//add
			item.consumable = true;//消耗品
			item.createTile = ModContent.TileType<Tiles.AdvancedCraftTable>();//放置贴图

			item.UseSound = SoundID.Item10;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ModContent.ItemType<Items.Mateiral.SiliconBar>(), 5);
			recipe.SetResult(this, 1);
			recipe.AddRecipe();
		}
	}
}
