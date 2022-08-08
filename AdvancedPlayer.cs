using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.GameInput;
using Terraria.ID;
using AdvancedMod.Utils;
using System.Collections.Generic;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public static int Energy;
        public static int UsedSiliconHeartCount;

        public bool RecievedInitBag;
        public bool RecievedBoss1Bag;
        public bool RecievedBoss2Bag;
        public bool RecievedBoss3Bag;
        public bool RecievedFleshWallBag;
        public bool RecievedMechBossBag;
        public bool RecievedPlanteraBag;
        public bool RecievedMoonlordBag;
        public bool RecievedGodOfEyeBag;
        public bool RecievedGodOfTimeBag;

        public static float Electricity;

        public static bool SymbolOfTown;
        public static bool SiliconCapacotance;
        public static bool SiliconArmorEquip;
        public static bool ClestialCloak;

        public static Vector2 DeathPosition;

        public static int TimeStopLeft;
        public static int TimeStopCD;

        public override void SaveData(TagCompound tag)
        {
            tag.Add("Energy", Energy);
            tag.Add("UsedSiliconHeartCount", UsedSiliconHeartCount);

            List<string> Bags = new List<string>();
            if (RecievedInitBag) Bags.Add("RecievedInitBag");
            if (RecievedBoss1Bag) Bags.Add("RecievedBoss1Bag");
            if (RecievedBoss2Bag) Bags.Add("RecievedBoss2Bag");
            if (RecievedBoss3Bag) Bags.Add("RecievedBoss3Bag");
            if (RecievedFleshWallBag) Bags.Add("RecievedFleshWallBag");
            if (RecievedMechBossBag) Bags.Add("RecievedMechBossBag");
            if (RecievedPlanteraBag) Bags.Add("RecievedPlanteraBag");
            if (RecievedMoonlordBag) Bags.Add("RecievedMoonlordBag");
            if (RecievedGodOfEyeBag) Bags.Add("RecievedGodOfEyeBag");
            if (RecievedGodOfTimeBag) Bags.Add("RecievedGodOfTimeBag");

            tag.Add("Bags", Bags);
        }

        public override void LoadData(TagCompound tag)
        {
            Energy = tag.GetInt("Energy");
            UsedSiliconHeartCount = tag.GetInt("UsedSiliconHeartCount");
            IList<string> Bags = tag.GetList<string>("Bags");
            RecievedInitBag = Bags.Contains("RecievedInitBag");
            RecievedBoss1Bag = Bags.Contains("RecievedBoss1Bag");
            RecievedBoss2Bag = Bags.Contains("RecievedBoss2Bag");
            RecievedBoss3Bag = Bags.Contains("RecievedBoss3Bag");
            RecievedFleshWallBag = Bags.Contains("RecievedFleshWallBag");
            RecievedMechBossBag = Bags.Contains("RecievedMechBossBag");
            RecievedPlanteraBag = Bags.Contains("RecievedPlanteraBag");
            RecievedMoonlordBag = Bags.Contains("RecievedMoonlordBag");
            RecievedGodOfEyeBag = Bags.Contains("RecievedGodOfEyeBag");
            RecievedGodOfTimeBag = Bags.Contains("RecievedGodOfTimeBag");
        }

        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
        {
            if (ModContent.GetInstance<AdvancedConfig>().NoHitMode) Player.dead = true;

            if (SymbolOfTown)
            {
                if (Main.rand.NextBool(5))
                {
                    for (int i = 0;i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].townNPC)
                        {
                            Main.npc[i].life -= (int)damage;
                        }
                    }
                }
                
                damage = 0;
            }

            if (SiliconArmorEquip)
            {
                Vector2 vel = Utils.Tool.GetClosestNPC(Player.Center,false).Center - Player.Center;
                Projectile.NewProjectile(Player.GetSource_OnHurt(Utils.Tool.GetClosestNPC(Player.Center, false)),
                                        Player.Center,Vector2.Normalize(vel)*2,ModContent.ProjectileType<Projectiles.Weapons.TreeDiagrammerLightingFriendly>(),
                                        80,0
                    );
            }

            if (SiliconCapacotance)
            {
                Electricity *= 0.5f;
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (Player.HasBuff(ModContent.BuffType<Buffs.Not_DeBuff.Fate>()))
            {
                Player.statLife = Player.statLifeMax2;
                return false;
            }

            return true;
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            DeathPosition = Player.position;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (AdvancedMod.TransportDeathPosition.JustPressed)
            {
                if (DeathPosition != Vector2.Zero) Player.position = DeathPosition;
            }

            if (Tool.AccessoryEquiped(ModContent.ItemType<Items.Accessory.Souls.TerrarianSoul>(),Player) && AdvancedMod.QuickHeal.JustPressed)
            {
                for (int i = 0; i < 10; i++)
                {
                    int heal = Main.rand.Next(20) + 30;
                    Player.HealEffect(heal);
                    Player.statLife += heal;
                }
            }
            /*
            if (Tool.AccessoryEquiped(ModContent.Find<ModItem>("AdvancedModDLC", "DeadSoul").Type,Player))
            {
                Projectile.NewProjectile(Player.GetSource_Accessory(ModContent.Find<ModItem>("AdvancedModDLC", "DeadSoul").Entity),
                                                                    Player.Center, Vector2.Zero,
                                                                    ModContent.Find<ModProjectile>("CalamityMod", "DemonshadeRedDevil").Type,
                                                                    114514, 1, Player.whoAmI, 0, 0
                    );
            }
            */
            if (Tool.AccessoryEquiped(ModContent.ItemType<Items.Accessory.BossDrop.TheWorldAccessory>(),Player) && AdvancedMod.TimeStop.JustPressed)
            {
                TimeStopLeft = 300;
                TimeStopCD = 3600;
            }
        }

        public override void OnEnterWorld(Player player)
        {
            /*
            if (AdvancedConfig.enableEnergySystem)
            {
                AdvancedMod.GUI.IsVisible = true;
            }
            */
        }

        public override void PreUpdate()
        {
            SiliconCapacotance = Tool.AccessoryEquiped(ModContent.ItemType<Items.Accessory.BossDrop.SiliconCapacitance>(), Player);
            ClestialCloak = Tool.AccessoryEquiped(ModContent.ItemType<Items.Accessory.Curser.CelestialCloak>(), Player);


            if (SiliconCapacotance)
            {
                if (Electricity < 100)
                {
                    Electricity += 0.05f;
                }
                else
                {
                    Electricity = 100;
                }
                Player.GetDamage(DamageClass.Generic) += (float)(0.5 * Electricity / 100);
            }

            if (AdvancedWorld.MutationMode && Main.masterMode && Utils.Tool.CheckBossAlive()) Player.AddBuff(ModContent.BuffType<Buffs.Debuff.VoidPressure>(), 60);

            if (TimeStopLeft > 0) TimeStopLeft -= 1;
            if (TimeStopLeft < 0) TimeStopLeft = 0;
            if (TimeStopCD > 0) TimeStopCD -= 1;
            if (TimeStopCD < 0) TimeStopCD = 0;
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (Player.HasBuff<Buffs.Not_DeBuff.KingCrimson>())
            {
                return false;
            }
            return true;
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (Player.HasBuff<Buffs.Not_DeBuff.KingCrimson>())
            {
                return false;
            }
            return true;
        }
    }
}
