using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.GameInput;
using Terraria.ID;
using AdvancedMod.Utils;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public static int Energy;
        public static int UsedSiliconHeartCount;

        public static bool RecievedInitBag;
        public static bool RecievedBoss1Bag;
        public static bool RecievedBoss2Bag;
        public static bool RecievedBoss3Bag;
        public static bool RecievedFleshWallBag;
        public static bool RecievedMechBossBag;
        public static bool RecievedPlanteraBag;
        public static bool RecievedMoonlordBag;
        public static bool RecievedGodOfEyeBag;
        public static bool RecievedGodOfTimeBag;

        public static float Electricity;

        public static bool SymbolOfTown;
        public static bool SiliconCapacotance;
        public static bool SiliconArmorEquip;
        public static bool ClestialCloak;

        public static Vector2 DeathPosition;

        public override void SaveData(TagCompound tag)
        {
            tag.Add("Energy", Energy);
            tag.Add("UsedSiliconHeartCount", UsedSiliconHeartCount);
            tag.Add("RecievedInitBag", RecievedInitBag);
            tag.Add("RecievedBoss1Bag", RecievedBoss1Bag);
            tag.Add("RecievedBoss2Bag", RecievedBoss2Bag);
            tag.Add("RecievedBoss3Bag", RecievedBoss3Bag);
            tag.Add("RecievedFleshWallBag",RecievedFleshWallBag);
            tag.Add("RecievedMechBossBag",RecievedMechBossBag);
            tag.Add("RecievedPlanteraBag", RecievedPlanteraBag);
            tag.Add("RecievedMoonlordBag", RecievedMoonlordBag);
            tag.Add("RecievedGodOfEyeBag", RecievedGodOfEyeBag);
            tag.Add("RecievedGodOfTimeBag", RecievedGodOfTimeBag);
        }

        public override void LoadData(TagCompound tag)
        {
            Energy = tag.GetInt("Energy");
            UsedSiliconHeartCount = tag.GetInt("UsedSiliconHeartCount");
            RecievedInitBag = tag.GetBool("RecievedInitBag");
            RecievedBoss1Bag = tag.GetBool("RecievedBoss1Bag");
            RecievedBoss2Bag = tag.GetBool("RecievedBoss2Bag");
            RecievedBoss3Bag = tag.GetBool("RecievedBoss3Bag");
            RecievedFleshWallBag = tag.GetBool("RecievedFleshWallBag");
            RecievedMechBossBag = tag.GetBool("RecievedMechBossBag");
            RecievedPlanteraBag = tag.GetBool("RecievedPlanteraBag");
            RecievedMoonlordBag = tag.GetBool("RecievedMoonlordBag");
            RecievedGodOfEyeBag = tag.GetBool("RecievedGodOfEyeBag");
            RecievedGodOfTimeBag = tag.GetBool("RecievedGodOfTimeBag");
        }

        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
        {

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
        }
    }
}
