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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Boss.TreeDiagrammer>()))//若BOSS存在，无法使用
            {
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            Color color = new Color(175, 75, 255);
            Main.NewText("树状图设计者已苏醒!",color);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Boss.TreeDiagrammer>());

            return true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.SoulofLight, 6)
            .AddIngredient(ItemID.IronBar, 5)
            .AddIngredient(ItemID.Wire, 10)
            .Register();
    }
}
