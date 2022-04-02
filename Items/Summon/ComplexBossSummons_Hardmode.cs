using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Summon
{
    public class ComplexBossSummons_Hardmode : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("复合式BOSS召唤物（困难模式）");
            Tooltip.SetDefault("在森林夜晚召唤双子魔眼\n在腐化/猩红夜晚召唤毁灭者\n在神圣夜晚召唤机械骷髅王\n在丛林召唤世纪之花\n在海洋召唤猪龙鱼公爵\n在白天召唤月亮领主");
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
            if (player.ZoneJungle)
            {
                return true;
            }
            else if (player.ZoneBeach)
            {
                return true;
            }
            else if ((!Main.dayTime) && (player.ZoneCrimson | player.ZoneCorrupt))
            {
                return true;
            }
            else if ((!Main.dayTime) && (player.ZoneHoly))
            {
                return true;
            }
            else if (!Main.dayTime)
            {
                return true;
            }
            else if (Main.dayTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool UseItem(Player player)
        {
            if (player.ZoneJungle)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.Plantera);
            }
            else if (player.ZoneBeach)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.DukeFishron);
            }
            else if ((!Main.dayTime) && (player.ZoneCrimson | player.ZoneCorrupt))
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.TheDestroyer);
            }
            else if ((!Main.dayTime) && (player.ZoneHoly))
            {
                NPC.SpawnOnPlayer(player.whoAmI, 127);
            }
            else if (!Main.dayTime)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.Retinazer);
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.Spazmatism);
            }
            else if (Main.dayTime)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.MoonLordCore);
            }

            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
