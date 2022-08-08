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
        internal static ModKeybind QuickHeal;
        internal static ModKeybind TimeStop;

        public static List<int> Bosses = new List<int>
        {
            NPCID.KingSlime,NPCID.EyeofCthulhu,NPCID.EaterofWorldsHead,NPCID.BrainofCthulhu,NPCID.QueenBee,NPCID.WallofFlesh,
            NPCID.SkeletronHead,NPCID.Deerclops,
            NPCID.QueenSlimeBoss,NPCID.Retinazer,NPCID.Spazmatism,NPCID.TheDestroyer,NPCID.SkeletronPrime,NPCID.Plantera,
            NPCID.HallowBoss,NPCID.Golem,NPCID.DukeFishron,NPCID.CultistBoss,NPCID.MoonLordCore,NPCID.PirateShip,
            NPCID.Pumpking,NPCID.MourningWood,NPCID.Everscream,NPCID.SantaNK1,NPCID.IceQueen,NPCID.MartianSaucer,
            NPCID.DD2DarkMageT1,NPCID.DD2DarkMageT3,NPCID.DD2OgreT2,NPCID.DD2OgreT3,NPCID.DD2Betsy,
            NPCID.LunarTowerSolar,NPCID.LunarTowerVortex,NPCID.LunarTowerNebula,NPCID.LunarTowerStardust,NPCID.DungeonGuardian,
            ModContent.NPCType<NPCs.Boss.TreeDiagrammer>()
        };

        public override void AddRecipes()
        {
            Recipe.Create(ItemID.LuckyHorseshoe,1)
                .AddIngredient(ItemID.Cloud, 50)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.IceSkates, 1)
                .AddIngredient(ItemID.IceBlock, 50)
                .AddIngredient(ItemID.IronBar, 10)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.CloudinaBottle, 1)
                .AddIngredient(ItemID.Cloud, 15)
                .AddIngredient(ItemID.Bottle, 1)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.WhoopieCushion)
                .AddIngredient(ItemID.PinkGel, 44)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.ShinyRedBalloon)
                .AddIngredient(ItemID.Cloud, 20)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.FishFinder)
                .AddIngredient(ItemID.FishronBossBag)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.GoblinTech)
                .AddIngredient(ItemID.TinkerersWorkshop)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.AmmoBox)
                .AddIngredient(ItemID.MusketBall, 3996)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.AnkhShield)
                .AddIngredient(ItemID.SpectreBar, 5)
                .AddTile(TileID.HeavyWorkBench)
                .Register();

            Recipe.Create(ItemID.AvengerEmblem)
                .AddIngredient(ModContent.ItemType<Items.Accessory.BossDrop.CurserEmblem>())
                .AddIngredient(ItemID.SoulofSight)
                .AddIngredient(ItemID.SoulofMight)
                .AddIngredient(ItemID.SoulofFright)
                .AddTile(TileID.MythrilAnvil)
                .Register();

            Recipe.Create(ItemID.CrimtaneBar)
                .AddIngredient(ItemID.DemoniteBar)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.DemoniteBar)
                .AddIngredient(ItemID.CrimtaneBar)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.CrimtaneOre)
                .AddIngredient(ItemID.DemoniteOre)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.DemoniteOre)
                .AddIngredient(ItemID.CrimtaneOre)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.TissueSample)
                .AddIngredient(ItemID.ShadowScale)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.ShadowScale)
                .AddIngredient(ItemID.TissueSample)
                .AddTile(TileID.Anvils)
                .Register();

            Recipe.Create(ItemID.Apple)
                .AddIngredient(ItemID.Wood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.Apricot)
                .AddIngredient(ItemID.Wood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.Banana)
                .AddIngredient(ItemID.PalmWood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.BlackCurrant)
                .AddIngredient(ItemID.Ebonwood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.BloodOrange)
                .AddIngredient(ItemID.Shadewood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.Cherry)
                .AddIngredient(ItemID.BorealWood, 10)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.TinBar)
                .AddIngredient(ItemID.CopperBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.CopperBar)
                .AddIngredient(ItemID.TinBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.LeadBar)
                .AddIngredient(ItemID.IronBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.IronBar)
                .AddIngredient(ItemID.LeadBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.TungstenBar)
                .AddIngredient(ItemID.SilverBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.SilverBar)
                .AddIngredient(ItemID.TungstenBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.PlatinumBar)
                .AddIngredient(ItemID.GoldBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.GoldBar)
                .AddIngredient(ItemID.PlatinumBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.PalladiumBar)
                .AddIngredient(ItemID.CobaltBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.CobaltBar)
                .AddIngredient(ItemID.PalladiumBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.OrichalcumBar)
                .AddIngredient(ItemID.MythrilBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.MythrilBar)
                .AddIngredient(ItemID.OrichalcumBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.AdamantiteBar)
                .AddIngredient(ItemID.TitaniumBar)
                .AddTile(TileID.WorkBenches)
                .Register();

            Recipe.Create(ItemID.TitaniumBar)
                .AddIngredient(ItemID.AdamantiteBar)
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
                    new List<int>{ ModContent.ItemType<Items.Tiles_Item.Relics.TreeDiagrammerRelicItem>(),ModContent.ItemType<Items.Accessory.BossDrop.SiliconCapacitance>()},
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
            QuickHeal = KeybindLoader.RegisterKeybind(this, "Terrarian Soul Quick Heal", "L");

            string Fargo = "FargowiltasSouls";
            if (ModLoader.TryGetMod(Fargo,out Mod fargo))
            {
                Bosses.Add(Utils.Tool.GetModNPC(Fargo, "DeviBoss"));
                Bosses.Add(Utils.Tool.GetModNPC(Fargo, "AbomBoss"));
                Bosses.Add(Utils.Tool.GetModNPC(Fargo, "MutantBoss"));
                Bosses.Add(Utils.Tool.GetModNPC(Fargo, "CosmosChampion"));
            }

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