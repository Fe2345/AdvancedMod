using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Tiles;

namespace AdvancedMod.Items.Tiles_Item
{
	public class  ElectromagneticWorkStationItem : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("电磁操作台");
			Tooltip.SetDefault("视作大多数困难模式前制作站、困难模式的砧、困难模式的熔炉\n允许你制作各种Advanced Mod中的困难模式物品");
		}

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 27;
			Item.maxStack = 999;
			Item.value = 100;
			Item.rare = ItemRarityID.Blue;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 10;
			Item.useAnimation = 20;

			//add
			Item.consumable = true;//消耗品
			Item.createTile = ModContent.TileType<Tiles.ElectromagneticWorkStation>();//放置贴图

			Item.UseSound = SoundID.Item10;
		}

		public override void AddRecipes() => CreateRecipe()
			.AddIngredient(ModContent.ItemType<Items.Mateiral.SiliconBar>(), 5)
			.Register();
	}
}
