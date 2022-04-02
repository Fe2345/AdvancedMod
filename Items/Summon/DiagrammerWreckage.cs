using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Summon
{
    public class DiagrammerWreckage : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("设计者残骸");
            Tooltip.SetDefault("召唤树状图设计者");
        }

        public override void SetDefaults()
        {
            item.width = 24;
            item.height = 18;
            item.rare = ItemRarityID.Purple;
            item.maxStack = 999;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.consumable = false;
            item.value = Item.buyPrice(1);
        }

        public override bool CanUseItem(Player player)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Boss.TreeDiagrammer>()))//若BOSS存在，无法使用
            {
                return false;
            }
            return true;
        }

        public override bool UseItem(Player player)
        {
            Color color = new Color(175, 75, 255);
            Main.NewText("树状图设计者已苏醒!",color);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Boss.TreeDiagrammer>());

            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 6);
            recipe.AddIngredient(ItemID.IronBar, 5);
            recipe.AddIngredient(ItemID.Wire, 10);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
