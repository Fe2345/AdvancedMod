using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.GameInput;
using Terraria.ID;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public static int Energy;
        public static int UsedSiliconHeartCount;
        public static bool RecievedInitBag;

        public static bool SymbolOfTown;
        public static bool SiliconArmorEquip;

        public static Vector2 DeathPosition;

        public override void Initialize()
        {
            Energy = 2048;
            UsedSiliconHeartCount = 0;
            RecievedInitBag = false;
        }

        public override void SaveData(TagCompound tag)
        {
            tag.Add("Energy", Energy);
            tag.Add("UsedSiliconHeartCount", UsedSiliconHeartCount);
            tag.Add("RecievedInitBag", RecievedInitBag);
        }

        public override void LoadData(TagCompound tag)
        {
            Energy = tag.GetInt("Energy");
            UsedSiliconHeartCount = tag.GetInt("UsedSiliconHeartCount");
            RecievedInitBag = tag.GetBool("RecievedInitBag");
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
    }
}
