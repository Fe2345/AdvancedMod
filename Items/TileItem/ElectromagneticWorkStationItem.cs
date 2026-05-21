using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Tiles;

namespace AdvancedMod.Items.TileItem
{
	public class  ElectromagneticWorkStationItem : ModItem
	{
        public override string Texture => $"{Mod.Name}/Assets/Textures/Items/TileItem/ElectromagneticWorkStationItem";

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
			.AddIngredient(ModContent.ItemType<Items.Material.SiliconBar>(), 5)
			.Register();
	}
}
