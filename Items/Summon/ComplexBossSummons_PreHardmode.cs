﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Summon
{
    public class ComplexBossSummons_PreHardmode : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("复合式BOSS召唤物（困难模式前）");
            Tooltip.SetDefault("在白天召唤史莱姆王\n在夜晚召唤克苏鲁之眼\n在腐化之地召唤世界吞噬怪\n在猩红之地召唤克苏鲁之脑\n在丛林召唤蜂王\n不消耗");
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
            Item.value = Item.sellPrice(silver: 1);
        }

        public override bool CanUseItem(Player player)
        {
            bool BossAlive = NPC.AnyNPCs(NPCID.KingSlime) | NPC.AnyNPCs(NPCID.EyeofCthulhu) | NPC.AnyNPCs(NPCID.EaterofWorldsTail) | NPC.AnyNPCs(NPCID.EaterofWorldsHead) | NPC.AnyNPCs(NPCID.EaterofWorldsBody) | NPC.AnyNPCs(NPCID.BrainofCthulhu) | NPC.AnyNPCs(NPCID.QueenBee);
            if (BossAlive)//若BOSS存在，无法使用
            {
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            Color color = new Color(175, 75, 255);
            if (player.ZoneCorrupt)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.EaterofWorldsBody);
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.EaterofWorldsHead);
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.EaterofWorldsTail);
            }
            else if (player.ZoneCrimson)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.BrainofCthulhu);
            }
            else if (player.ZoneJungle)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.QueenBee);
            }
            else if (Main.dayTime)
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.KingSlime);
            }
            else
            {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.EyeofCthulhu);
            }
            

            return true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddTile(TileID.DemonAltar)
            .Register();
    }
}
