using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace AdvancedMod
{
    public class AdvancedMod : Mod
    {
        public override void AddRecipes()
        {
            ModRecipe modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.Cloud, 50);
            modRecipe.SetResult(ItemID.LuckyHorseshoe, 1);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.IceBlock, 10);
            modRecipe.AddIngredient(ItemID.IronBar, 5);
            modRecipe.AddTile(TileID.Anvils);
            modRecipe.SetResult(ItemID.IceSkates,1);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.Cloud, 15);
            modRecipe.AddIngredient(ItemID.Bottle, 1);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.SetResult(ItemID.CloudinaBottle,1);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.PinkGel, 10);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.SetResult(ItemID.WhoopieCushion,1);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.Cloud, 20);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.SetResult(ItemID.ShinyRedBalloon);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.FishronBossBag, 1);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.SetResult(ItemID.FishFinder);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.Wire, 20);
            modRecipe.AddIngredient(ItemID.TinkerersWorkshop);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.SetResult(ItemID.GoblinTech);
            modRecipe.AddRecipe();

            modRecipe = new ModRecipe(this);
            modRecipe.AddIngredient(ItemID.MusketBall, 9999);
            modRecipe.AddTile(TileID.HeavyWorkBench);
            modRecipe.SetResult(ItemID.AmmoBox);
            modRecipe.AddRecipe();
        }
    }
}