using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;
using Microsoft.Xna.Framework.Graphics;

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

            CreateRecipe(ItemID.AvengerEmblem)
                .AddIngredient(ModContent.ItemType<Items.Accessory.BossDrop.CurserEmblem>())
                .AddIngredient(ItemID.SoulofSight)
                .AddIngredient(ItemID.SoulofMight)
                .AddIngredient(ItemID.SoulofFright)
                .AddTile(TileID.MythrilAnvil)
                .Register();

            CreateRecipe(ItemID.CrimtaneBar)
                .AddIngredient(ItemID.DemoniteBar)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(ItemID.DemoniteBar)
                .AddIngredient(ItemID.CrimtaneBar)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(ItemID.CrimtaneOre)
                .AddIngredient(ItemID.DemoniteOre)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(ItemID.DemoniteOre)
                .AddIngredient(ItemID.CrimtaneOre)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(ItemID.TissueSample)
                .AddIngredient(ItemID.ShadowScale)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(ItemID.ShadowScale)
                .AddIngredient(ItemID.TissueSample)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(ItemID.Apple)
                .AddIngredient(ItemID.Wood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(ItemID.Apricot)
                .AddIngredient(ItemID.Wood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(ItemID.Banana)
                .AddIngredient(ItemID.PalmWood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(ItemID.BlackCurrant)
                .AddIngredient(ItemID.Ebonwood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(ItemID.BloodOrange)
                .AddIngredient(ItemID.Shadewood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe(ItemID.Cherry)
                .AddIngredient(ItemID.BorealWood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override void PostSetupContent()
        {
            
            Mod Checklist = ModLoader.GetMod("BossChecklist");
            if (Checklist != null)
            {
                 Checklist.Call("AddBoss",
                    this,
                    $"Mods.AdvancedMod.NPCs.TreeDiagrammer",
                    ModContent.NPCType<NPCs.Boss.TreeDiagrammer>(),
                    9.5f,
                    (Func<bool>)(() => AdvancedWorld.downedTreeDiagrammer),
                    (Func<bool>)(() => true),
                    ModContent.ItemType<Items.Accessory.BossDrop.SiliconCapacitance>(),
                    ModContent.ItemType<Items.Summon.DiagrammerWreckage>(),
                    $"Mods.AdvancedMod.BossChecklist.Summon.TreeDiagrammer",
                    $"Mods.AdvancedMod.BossChecklist.DespawnMessage.TreeDiagrammer",
                    (SpriteBatch sb, Rectangle rect, Color color) => {
                        Texture2D texture = ModContent.Request<Texture2D>("AdvancedMod/NPCs/Boss/TreeDiagrammer").Value;
                        Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                        sb.Draw(texture, centered, color);
                    }
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
            
            //寻找一个名字为Vanilla: Mouse Text的绘制层，也就是绘制鼠标字体的那一层，并且返回那一层的索引
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            //寻找到索引时
            if (MouseTextIndex != -1)
            {
                //往绘制层集合插入一个成员，第一个参数是插入的地方的索引，第二个参数是绘制层
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
                   //这里是绘制层的名字
                   "Test : ExampleUI",
                   //这里是匿名方法
                   delegate
                   {
               //当Visible开启时（当UI开启时）
               if (GUI.IsVisible)
                   //绘制UI（运行exampleUI的Draw方法）
                   energySystemUI.Draw(Main.spriteBatch);
                       return true;
                   },
                   //这里是绘制层的类型
                   InterfaceScaleType.UI)
               );
            }
            base.ModifyInterfaceLayers(layers);
            
        }
        */
    }
}