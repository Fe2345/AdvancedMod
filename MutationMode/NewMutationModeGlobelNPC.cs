using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.MutationMode
{
    public class NewMutationModeGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public List<MutationModeNPCBehaviour> MutationModeNpcBehaviours = new List<MutationModeNPCBehaviour>();

        public bool FirstTick = true;
        public bool enteredSetDefaults;

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            InitBehaviourList(npc);

            /*
            if (!Main.dedServ)
            {
                bool recolor = SoulConfig.Instance.BossRecolors && AdvancedWorld.MutationMode;
                if (recolor || FargowiltasSouls.Instance.LoadedNewSprites)
                {
                    FargowiltasSouls.Instance.LoadedNewSprites = true;
                    foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                    {
                        behaviour.LoadSprites(npc, recolor);
                    }
                }
            }
            */

            if (AdvancedWorld.MutationMode) //needs to be like this to avoid bestiary/npcloot issues and crashes
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.SetDefaults(npc);
                }
            }
        }

        private void InitBehaviourList(NPC npc)
        {
            // TODO Try caching this again? Last attempt caused major fails
            IEnumerable<MutationModeNPCBehaviour> behaviours = MutationModeNPCBehaviour.AllMutationModeNpcBehaviours
                .Where(m => m.Matcher.Satisfies(npc.type));

            // To make sure they're always in the same order
            // TODO is ordering needed? Do they always have the same order?
            behaviours.OrderBy(m => m.GetType().FullName, StringComparer.InvariantCulture);

            MutationModeNpcBehaviours = behaviours.Select(m => m.NewInstance()).ToList();
        }

        #region Behaviour Hooks
        public override bool PreAI(NPC npc)
        {
            if (!AdvancedWorld.MutationMode)
                return true;

            bool result = true;

            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                if (FirstTick)
                    behaviour.OnSpawn(npc);

                result &= behaviour.PreAI(npc);
            }

            FirstTick = false;

            return result;
        }

        public override void AI(NPC npc)
        {
            base.AI(npc);

            if (!AdvancedWorld.MutationMode)
                return;

            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                behaviour.AI(npc);
            }
        }

        public override bool PreNPCLoot(NPC npc)
        {
            bool result = base.PreNPCLoot(npc);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.PreNPCLoot(npc);
                }
            }

            return result;
        }

        public override void NPCLoot(NPC npc)
        {
            base.NPCLoot(npc);

            if (!AdvancedWorld.MutationMode || npc.SpawnedFromStatue)
                return;

            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                behaviour.NPCLoot(npc);
            }
        }

        /*
        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.OnKill(npc);
                }
            }
        }

        public override bool SpecialOnKill(NPC npc)
        {
            bool result = base.SpecialOnKill(npc);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.SpecialOnKill(npc);
                }
            }

            return result;
        }
        */

        public override bool CanHitPlayer(NPC npc, Player target, ref int CooldownSlot)
        {
            bool result = true;

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.CanHitPlayer(npc, target, ref CooldownSlot);
                }
            }

            return result;
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            bool? result = base.CanBeHitByItem(npc, player, item);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.CanBeHitByItem(npc, player, item);
                }
            }

            return result;
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            bool? result = base.CanBeHitByProjectile(npc, projectile);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.CanBeHitByProjectile(npc, projectile);
                }
            }

            return result;
        }

        public override void OnHitPlayer(NPC npc, Player target, int damage, bool crit)
        {
            base.OnHitPlayer(npc, target, damage, crit);

            if (!AdvancedWorld.MutationMode)
                return;

            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                behaviour.OnHitPlayer(npc, target, damage, crit);
            }
        }

        public override void OnHitNPC(NPC npc, NPC target, int damage, float knockback, bool crit)
        {
            base.OnHitNPC(npc, target, damage, knockback, crit);

            if (!AdvancedWorld.MutationMode)
                return;

            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                behaviour.OnHitNPC(npc, target, damage, knockback, crit);
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            base.ModifyHitByItem(npc, player, item, ref damage, ref knockback, ref crit);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.ModifyHitByItem(npc, player, item, ref damage, ref knockback, ref crit);
                }
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            base.ModifyHitByProjectile(npc, projectile, ref damage, ref knockback, ref crit, ref hitDirection);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.ModifyHitByProjectile(npc, projectile, ref damage, ref knockback, ref crit, ref hitDirection);
                }
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, int damage, float knockback, bool crit)
        {
            base.OnHitByItem(npc, player, item, damage, knockback, crit);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.OnHitByItem(npc, player, item, damage, knockback, crit);
                }
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, int damage, float knockback, bool crit)
        {
            base.OnHitByProjectile(npc, projectile, damage, knockback, crit);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.OnHitByProjectile(npc, projectile, damage, knockback, crit);
                }
            }
        }

        public override bool StrikeNPC(NPC npc, ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            bool result = base.StrikeNPC(npc, ref damage, defense, ref knockback, hitDirection, ref crit);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.StrikeNPC(npc, ref damage, defense, ref knockback, hitDirection, ref crit);
                }
            }

            return result;
        }

        public override void HitEffect(NPC npc, int hitDirection, double damage)
        {
            base.HitEffect(npc, hitDirection, damage);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.HitEffect(npc, hitDirection, damage);
                }
            }
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            base.UpdateLifeRegen(npc, ref damage);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    behaviour.UpdateLifeRegen(npc, ref damage);
                }
            }
        }

        public override bool CheckDead(NPC npc)
        {
            bool result = base.CheckDead(npc);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.CheckDead(npc);
                }
            }

            return result;
        }

        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            Color? result = base.GetAlpha(npc, drawColor);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result = behaviour.GetAlpha(npc, drawColor);
                }
            }

            return result;
        }

        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            bool? result = base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            if (AdvancedWorld.MutationMode)
            {
                foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                {
                    result &= behaviour.DrawHealthBar(npc, hbPosition, ref scale, ref position);
                }
            }

            return result;
        }

        public void NetSync(NPC npc)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = AdvancedMod.Instance.GetPacket();
            packet.Write((byte)22); // New maso sync packet id
            packet.Write(npc.whoAmI);
            packet.Write(npc.type);

            int bytesLength = 0;
            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
                bytesLength += behaviour.GetBytesNeeded();
            packet.Write(bytesLength);

            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                behaviour.NetSend(packet);
            }

            packet.Send();
        }

        public void NetRecieve(BinaryReader reader)
        {
            foreach (MutationModeNPCBehaviour behaviour in MutationModeNpcBehaviours)
            {
                behaviour.NetRecieve(reader);
            }
        }
        #endregion
    }
}