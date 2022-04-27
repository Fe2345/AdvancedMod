using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using System;
using System.Collections.Generic;

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

        public override void PostSetupContent()
        {
            Mod BossChecklist = ModLoader.GetMod("BossChecklist");
            if (BossChecklist != null)
            {
                BossChecklist.Call("AddBoss",
                    9.5f,
                    ModContent.NPCType<NPCs.Boss.TreeDiagrammer>(),
                    this,
                    "树状图设计者",
                    (Func<bool>)(() => AdvancedWorld.downedTreeDiagrammer),
                    ModContent.ItemType<Items.Summon.DiagrammerWreckage>(),
                    ModContent.ItemType<Items.Weapon.Lantern_Of_Middle_Autemn>(),
                    new List<int> {ItemID.GreaterHealingPotion,ModContent.ItemType<Items.Mateiral.SiliconBar>(),ItemID.SoulofLight,ItemID.IronBar,ItemID.Wire },
                    "使用[i:{ModContent.ItemType<Items.Summon.DiagrammerWreckage>}]召唤",
                    "树状图设计者 杀死了所有玩家！",
                    "AdvancedMod/NPCs/Boss/TreeDiagrammer",
                    "AdvancedMod/NPCs/Boss/TreeDiagrammer_Head_Boss",
                    (Func<bool>)(() => AdvancedWorld.MutationMode)
                    );
            }
        }

        public override object Call(params object[] args)
        {
            if (args[0].Equals("MutationMode"))
            {
                return AdvancedWorld.MutationMode;
            }
            else if (args[0].Equals("downedPolarMessager"))
            {
                return AdvancedWorld.downedPolarMessager;
            }
            else if (args[0].Equals("downedTreeDiagrammer"))
            {
                return AdvancedWorld.downedTreeDiagrammer;
            }
            else if (args[0].Equals("downedTriangle"))
            {
                return AdvancedWorld.downedTriangle;
            }
            else if (args[0].Equals("downedGodOfEye"))
            {
                return AdvancedWorld.downedGodOfEye;
            }
            else if (args[0].Equals("downedThrougher"))
            {
                return AdvancedWorld.downedThrougher;
            }
            else if (args[0].Equals("downedGodOfTime"))
            {
                return AdvancedWorld.downedGodOfTime;
            }
            else if (args[0].Equals("downedTheWorld"))
            {
                return AdvancedWorld.downedTheWorld;
            }
            else if (args[0].Equals("downedMutationBosses"))
            {
                return AdvancedWorld.downedMutationBosses;
            }


            return false;
        }
    }
}