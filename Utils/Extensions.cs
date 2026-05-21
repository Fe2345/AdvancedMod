using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace AdvancedMod.Utils
{
    public static partial class AdvancedExtensions
    {
        public static AdvancedPlayer Advanced(this Player player) => player.GetModPlayer<AdvancedPlayer>();
        public static AdvancedGlobelNPC Advanced(this NPC npc) => npc.GetGlobalNPC<AdvancedGlobelNPC>();

        //工具方法：检测玩家/NPC附近是否存在指定弹幕
        public static ModProjectile GetProjInDistance(NPC npc,ModProjectile proj, float distance)
        {

            return null;
        }
        public static ModProjectile GetProjInDistance(Player player, float distance)
        {
            return null;
        }

        public static Vector2 AddJitter(
            Vector2 point,
            UnifiedRandom random,
            float jitterXTiles,
            float jitterYTiles)
        {
            float Tile = 16f;
            float offsetX = RandomRange(random, -jitterXTiles * Tile, jitterXTiles * Tile);
            float offsetY = RandomRange(random, -jitterYTiles * Tile, jitterYTiles * Tile);

            return new Vector2(point.X + offsetX, point.Y + offsetY);
        }

        private static float RandomRange(UnifiedRandom random, float minValue, float maxValue)
        {
            return minValue + (float)random.NextDouble() * (maxValue - minValue);
        }
    }

    /// <summary>
    /// 用途：实现一次独立的追加伤害。该伤害吃玩家职业增伤，吃目标防御和减伤，可暴击，可走自定义乘区，并且不被 NPC.immune 无敌帧吞掉
    /// </summary>
    public sealed class ExtraStrikePlayer : ModPlayer
    {
        internal bool InExtraStrike;
    }

    public static class ExtraStrikeHelper
    {
        public static bool TryStrikeExtraDamage(
            Player player,
            NPC target,
            int baseDamage,
            DamageClass damageClass,
            int hitDirection,
            float baseKnockback = 0f,
            float sourceMultiplier = 1f,
            float finalMultiplier = 1f,
            float critChanceBonus = 0f,
            bool canCrit = true,
            bool damageVariation = true
        )
        {
            if (player == null || target == null || damageClass == null)
                return false;

            if (!player.active || player.dead)
                return false;

            if (!target.active || target.life <= 0 || target.dontTakeDamage || target.immortal)
                return false;

            if (baseDamage <= 0)
                return false;

            if (hitDirection == 0)
                hitDirection = player.Center.X < target.Center.X ? 1 : -1;

            if (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI != Main.myPlayer)
                return false;

            ExtraStrikePlayer modPlayer = player.GetModPlayer<ExtraStrikePlayer>();

            if (modPlayer.InExtraStrike)
                return false;

            int sourceDamage = (int)Math.Round(player.GetTotalDamage(damageClass).ApplyTo(baseDamage));

            if (sourceDamage < 1)
                sourceDamage = 1;

            float knockback = player.GetTotalKnockback(damageClass).ApplyTo(baseKnockback);

            float critChance = player.GetTotalCritChance(damageClass) + critChanceBonus;

            if (critChance < 0f)
                critChance = 0f;

            bool crit = canCrit && Main.rand.NextDouble() * 100.0 < critChance;

            NPC.HitModifiers modifiers = target.GetIncomingStrikeModifiers(damageClass, hitDirection);

            modifiers.ArmorPenetration += player.GetTotalArmorPenetration(damageClass);

            modifiers.SourceDamage *= sourceMultiplier;
            modifiers.FinalDamage *= finalMultiplier;

            NPC.HitInfo extraHit = modifiers.ToHitInfo(
                baseDamage: sourceDamage,
                crit: crit,
                baseKnockback: knockback,
                damageVariation: damageVariation,
                luck: player.luck
            );

            modPlayer.InExtraStrike = true;

            try
            {
                player.StrikeNPCDirect(target, extraHit);
            }
            finally
            {
                modPlayer.InExtraStrike = false;
            }

            return true;
        }
    }
}
