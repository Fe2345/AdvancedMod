using System;
using AdvancedMod.Contents.Hallow.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using R = AdvancedMod.Contents.Hallow.NPCs.DestroyerAttackRules;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed partial class DestroyerBehavior
{
    public DestroyerAttackKind SelectedAttack { get; private set; }
    public bool CrashAttackFired { get; private set; }
    private void SelectCrashAttack()
    {
        CrashAttackFired = false;
        if (Main.netMode != NetmodeID.MultiplayerClient) SelectedAttack = (DestroyerAttackKind)Main.rand.Next(5);
    }
    private void TickCrashAttack(NPC head)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || CrashAttackFired || head.Distance(Main.player[head.target].Center) > R.TriggerDistance) return;
        CrashAttackFired = true; SpawnCrashAttack(head, SelectedAttack); head.netUpdate = true;
    }
    internal static void SpawnCrashAttack(NPC head, DestroyerAttackKind kind)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        Vector2 target = Main.player[head.target].Center;
        // 测试版以原探测怪本次投射物伤害作基准；倍率仅在生成时应用一次。
        int baseline = head.GetAttackDamage_ForProjectiles(25, 22);
        float multiplier = kind == DestroyerAttackKind.Water ? R.WaterMultiplier : kind == DestroyerAttackKind.Frost ? R.FrostMultiplier : 1;
        int damage = Math.Max(1, (int)(baseline * multiplier));
        void Spawn(NPC source, Vector2 position, Vector2 velocity, float angle = 0)
            => Projectile.NewProjectile(source.GetSource_FromAI(), position, velocity, ModContent.ProjectileType<DestroyerCrashProjectile>(), damage, 0, Main.myPlayer, (float)kind, angle);
        switch (kind)
        {
            case DestroyerAttackKind.Fire:
                float offset = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < R.FireCount; i++) Spawn(head, head.Center, (offset + MathHelper.TwoPi * i / R.FireCount).ToRotationVector2() * R.FireSpeed);
                break;
            case DestroyerAttackKind.Wind:
                for (int i = 0; i < R.WindCount; i++)
                {
                    Vector2 direction = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                    Spawn(head, target + direction * R.WindSpawnRadius, -direction * R.WindSpeed);
                }
                break;
            case DestroyerAttackKind.Laser:
                int slot = (int)head.ai[0];
                for (int i = 0; i < R.LaserCount && slot >= 0 && slot < Main.maxNPCs; i++)
                {
                    NPC part = Main.npc[slot];
                    if (!part.active || part.type != NPCID.TheDestroyerBody || part.realLife != head.whoAmI) break;
                    Spawn(part, part.Center, (target - part.Center).SafeNormalize(Vector2.UnitY) * R.LaserSpeed); slot = (int)part.ai[0];
                }
                break;
            case DestroyerAttackKind.Water:
                float phase = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < R.WaterCount; i++) Spawn(head, target, Vector2.Zero, phase + MathHelper.TwoPi * i / R.WaterCount);
                break;
            case DestroyerAttackKind.Frost:
                Spawn(head, head.Center, (target - head.Center).SafeNormalize(Vector2.UnitY)); break;
        }
    }
    private void SpawnUltimatePreview(NPC head)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        Vector2 radial = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
        Vector2 root = Main.player[head.target].Center + radial * R.WhipRootDistance;
        float sign = UltimateRepetition % 2 == 0 ? 1 : -1;
        float start = (-radial).ToRotation() - sign * MathHelper.PiOver2;
        Projectile.NewProjectile(head.GetSource_FromAI(), root, Vector2.Zero, ModContent.ProjectileType<DestroyerUltimatePreview>(), 0, 0, Main.myPlayer, head.whoAmI, start, sign);
    }
}
