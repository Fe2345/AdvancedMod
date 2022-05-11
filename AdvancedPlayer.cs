using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.GameInput;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public static int Energy;
        public static int UsedSiliconHeartCount;
        public static bool RecievedInitBag;

        public static bool SymbolOfTown;

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
                if (Main.rand.Next(5) == 1)
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
