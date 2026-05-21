using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using AdvancedMod.NPCs.Boss.TreeDiagrammer;

namespace AdvancedMod.Items.Summon
{
    public class SuspiciousCommunicator : ModItem
    {
        public override string Texture => $"{Mod.Name}/Assets/Textures/Items/Summon/SuspiciousCommunicator";

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 18;
            Item.rare = ItemRarityID.Purple;
            Item.maxStack = 999;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.value = Item.buyPrice(1);
        }

        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<TreeDiagrammer>()) || !Main.hardMode)//若BOSS存在，无法使用
            {
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<TreeDiagrammer>());

            return true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.SoulofFlight, 6)
            .AddIngredient(ItemID.IronBar, 5)
            .AddIngredient(ItemID.Wire, 10)
            .Register();
    }
}
