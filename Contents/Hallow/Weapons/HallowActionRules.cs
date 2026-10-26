using System;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>天裁右键动作的集中参数与环境选择，均不受攻速缩短。</summary>
internal static class HallowActionRules
{
    internal const int PerfectTicks = 20, GuardCooldown = 360, ProtectionTicks = 60;
    internal const int ResonanceTicks = 60, LiftTicks = 15, CommitTick = 30, PhantomCount = 6;
    internal const float CounterRange = 512, CounterOffset = 192, GuardOffset = 36, GuardThickness = 24;
    internal const float CounterSpeed = 16, CounterTurn = 0.16f;
    internal const int CounterLifetime = 120;
    internal const double Attachment = 1.5;
    internal const float GuardDamageMultiplier = 0.5f;
    internal static byte Encode(TagEnum? element) => element is >= TagEnum.Pyro and <= TagEnum.Cryo ? (byte)((int)element.Value + 1) : (byte)0;
    internal static TagEnum? Decode(int value) => value is >= 1 and <= 4 ? (TagEnum)(value - 1) : null;
    internal static TagSet Tags(TagEnum? element) => element.HasValue ? new(new[] { new TagEntry(false, element.Value) }) : TagSet.Empty;
    internal static bool Perfect(int age) => age >= 0 && age < PerfectTicks;
    internal static bool EligibleProjectile(bool hostile, int damage, float speed, int updates, float size) =>
        hostile && damage > 1 && speed * updates > 1 && size < 300;
    internal static bool InFront(Vector2 center, Vector2 direction, Vector2 target) =>
        Vector2.DistanceSquared(center, target) <= CounterRange * CounterRange && Vector2.Dot(target - center, direction) >= 0;
    internal static TagEnum? ChooseEnvironment(bool lava, bool water, bool ice, bool cloud) =>
        lava ? TagEnum.Pyro : water ? TagEnum.Hydro : ice ? TagEnum.Cryo : cloud ? TagEnum.Electro : null;
    internal static TagEnum? Environment(Player player)
    {
        bool ice = false, cloud = false;
        int y = (int)((player.Bottom.Y + 2) / 16);
        for (int x = (int)(player.Left.X / 16); x <= (int)((player.Right.X - 1) / 16); x++)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            if (!tile.HasUnactuatedTile) continue;
            ice |= tile.TileType is TileID.IceBlock or TileID.CorruptIce or TileID.FleshIce or TileID.HallowedIce or TileID.BreakableIce;
            cloud |= tile.TileType is TileID.Cloud or TileID.RainCloud or TileID.SnowCloud;
        }
        return ChooseEnvironment(player.lavaWet, player.wet && !player.honeyWet && !player.shimmerWet && !player.lavaWet, ice, cloud);
    }
}
