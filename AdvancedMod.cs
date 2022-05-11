using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;

namespace AdvancedMod
{
    public class AdvancedMod : Mod
    {
        internal static AdvancedMod Instance;

        internal UI.EnergySystem energySystemUI;
        internal UserInterface GUI;

        internal static ModKeybind TransportDeathPosition;

        public override void AddRecipes()
        {
            CreateRecipe(ItemID.LuckyHorseshoe,1)
                .AddIngredient(ItemID.Cloud, 50)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.IceSkates, 1)
                .AddIngredient(ItemID.IceBlock, 50)
                .AddIngredient(ItemID.IronBar, 10)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.CloudinaBottle, 1)
                .AddIngredient(ItemID.Cloud, 15)
                .AddIngredient(ItemID.Bottle, 1)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.WhoopieCushion)
                .AddIngredient(ItemID.PinkGel, 44)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.ShinyRedBalloon)
                .AddIngredient(ItemID.Cloud, 20)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.FishFinder)
                .AddIngredient(ItemID.FishronBossBag)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.GoblinTech)
                .AddIngredient(ItemID.TinkerersWorkshop)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.AmmoBox)
                .AddIngredient(ItemID.MusketBall, 3996)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            CreateRecipe(ItemID.AnkhShield)
                .AddIngredient(ItemID.SpectreBar, 5)
                .AddTile(TileID.HeavyWorkBench)
                .Register();
        }

        public override void PostSetupContent()
        {
            /*
            Mod Checklist = ModLoader.GetMod("BossChecklist");
            if (Checklist != null)
            {
                 Checklist.Call("AddBoss",
                    9.5f,
                    ModContent.NPCType<NPCs.Boss.TreeDiagrammer>(),
                    this,
                    "Tree Diagrammer",
                    (Func<bool>)(() => AdvancedWorld.downedTreeDiagrammer),
                    ModContent.ItemType<Items.Summon.DiagrammerWreckage>(),
                    ModContent.ItemType<Items.Weapon.Lantern_Of_Middle_Autemn>(),
                    new List<int> {ItemID.GreaterHealingPotion,ModContent.ItemType<Items.Mateiral.SiliconBar>(),ItemID.SoulofLight,ItemID.IronBar,ItemID.Wire },
                    $"Use{ModContent.ItemType<Items.Summon.DiagrammerWreckage>()} to summon.",
                    "Tree Diagrammer  Killed All Players!",
                    "AdvancedMod/NPCs/Boss/TreeDiagrammer",
                    "AdvancedMod/NPCs/Boss/TreeDiagrammer_Head_Boss",
                    (Func<bool>)(() => AdvancedPlayer.RecievedInitBag)
                    );
            }
            */
            
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

        public override void Load()
        {
            Instance = this;

            TransportDeathPosition = KeybindLoader.RegisterKeybind(this,"Transport to Latest Death Position", "O");

            /*
            energySystemUI = new UI.EnergySystem();
            energySystemUI.Activate();
            GUI.SetState(energySystemUI);
            */
            
        }

        /*
        public override void UpdateUI(GameTime gameTime)
        {
            
            if (GUI.IsVisible)
            {
                GUI?.Update(gameTime);
                
            }
            base.UpdateUI(gameTime);
            
        }
        */

        /*
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            
            //Ѱ��һ������ΪVanilla: Mouse Text�Ļ��Ʋ㣬Ҳ���ǻ�������������һ�㣬���ҷ�����һ�������
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            //Ѱ�ҵ�����ʱ
            if (MouseTextIndex != -1)
            {
                //�����Ʋ㼯�ϲ���һ����Ա����һ�������ǲ���ĵط����������ڶ��������ǻ��Ʋ�
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
                   //�����ǻ��Ʋ������
                   "Test : ExampleUI",
                   //��������������
                   delegate
                   {
               //��Visible����ʱ����UI����ʱ��
               if (GUI.IsVisible)
                   //����UI������exampleUI��Draw������
                   energySystemUI.Draw(Main.spriteBatch);
                       return true;
                   },
                   //�����ǻ��Ʋ������
                   InterfaceScaleType.UI)
               );
            }
            base.ModifyInterfaceLayers(layers);
            
        }
        */
    }
}