using System;
using AdvancedMod.DamageClasses.Element.Data;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element.Services
{
    /// <summary>
    /// 根据玩家当前的协同攻击配置，为一次命中派生额外弹幕。
    /// </summary>
    public static class CoordinatedAttackDispatcher
    {
        private const float BaseProjectileSpeed = 6f;

        /// <summary>
        /// 为本次命中分发协同攻击弹幕。
        /// </summary>
        public static void Dispatch(Player? sourcePlayer, NPC target, AttackPayload payload)
        {
            if (sourcePlayer is null || !sourcePlayer.active)
            {
                return;
            }

            if ((payload.Flags & Definitions.AttackFlags.SuppressCoordinatedAttack) != 0)
            {
                return;
            }

            if (payload.ContextSnapshot.CoordinatedAttacks.Entries.Count == 0)
            {
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient && sourcePlayer.whoAmI != Main.myPlayer)
            {
                return;
            }

            Vector2 origin = sourcePlayer.MountedCenter;
            Vector2 direction = target.Center - origin;

            if (direction.LengthSquared() <= 0.001f)
            {
                direction = new Vector2(sourcePlayer.direction, 0f);
            }

            direction.Normalize();
            IEntitySource source = sourcePlayer.GetSource_FromThis("ElementCoordinatedAttack");
            DamageClass elementDamageClass = ModContent.GetInstance<ElementDamage>();

            foreach (CoordinatedAttackStatSet.Entry entry in payload.ContextSnapshot.CoordinatedAttacks.Entries.Values)
            {
                float speed = Math.Max(1f, BaseProjectileSpeed * entry.SpeedMultiplier);
                Vector2 spawnPosition = origin + direction * 18f;
                Vector2 velocity = direction * speed;
                int damage = Math.Max(1, (int)Math.Round(sourcePlayer.GetTotalDamage(elementDamageClass).ApplyTo(entry.Damage)));
                int projectileId = Projectile.NewProjectile(source, spawnPosition, velocity, entry.ProjectileType, damage, 0f, sourcePlayer.whoAmI, (float)entry.Element, entry.BuildUp);

                if (Main.projectile.IndexInRange(projectileId))
                {
                    Main.projectile[projectileId].originalDamage = damage;
                }
            }
        }
    }
}
