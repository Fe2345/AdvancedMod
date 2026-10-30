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
    internal const int ExpandTicks = 45, HoldTicks = 30, ReturnTicks = 45;
    internal const int CommitTick = ExpandTicks + HoldTicks, ResonanceTicks = CommitTick + ReturnTicks, PhantomCount = 6;
    internal const float LiftTicks = ExpandTicks / 2f;
    internal const float OrbitRadius = 0.65f, OrbitFlattening = 0.32f, OrbitPerspective = 0.16f;
    internal const float OrbitTurn = MathHelper.TwoPi, PhantomOpacity = 0.8f;

    /// <summary>展开与收回角度同向递增，停顿时姿态固定；收回末段才放下本体。</summary>
    internal static (float Lift, float Spread, float Turn) ResonancePose(float age)
    {
        static float Smooth(float value) => MathHelper.SmoothStep(0, 1, Math.Clamp(value, 0, 1));
        float expand = Smooth((age - LiftTicks) / (ExpandTicks - LiftTicks));
        float close = Smooth((age - CommitTick) / ReturnTicks);
        float lift = Smooth(age / LiftTicks) * (1 - Smooth((age - (ResonanceTicks - LiftTicks)) / LiftTicks));
        return (lift, expand * (1 - close), OrbitTurn * (expand + close));
    }
    internal const float CounterRange = 512, CounterOffset = 192, GuardOffset = 36, GuardThickness = 24;
    internal const float CounterSpeed = 16, CounterTurn = 0.16f;
    internal const int CounterLifetime = 120;
    internal const double Attachment = 1.5;
    internal const float GuardDamageMultiplier = 0.5f;
    // 保持旧存档的1—4编码；风使用标签对应的6，跳过尚不支持的叶绿编码5。
    internal static byte Encode(TagEnum? element) => element is >= TagEnum.Pyro and <= TagEnum.Cryo or TagEnum.Anemo ? (byte)((int)element.Value + 1) : (byte)0;
    internal static TagEnum? Decode(int value) => value is >= 1 and <= 4 or 6 ? (TagEnum)(value - 1) : null;
    // 神圣仅表示异化资格；未共鸣及零附着反击不会因此凭空触发反应或获得擢升。
    internal static TagSet Tags(TagEnum? element) => element.HasValue
        ? new(new[] { new TagEntry(false, element.Value), new TagEntry(false, TagEnum.Hallow) })
        : new(new[] { new TagEntry(false, TagEnum.Hallow) });
    internal static bool Perfect(int age) => age >= 0 && age < PerfectTicks;
    internal static bool EligibleProjectile(bool hostile, int damage, float speed, int updates, float size) =>
        hostile && damage > 1 && speed * updates > 1 && size < 300;
    internal static bool InFront(Vector2 center, Vector2 direction, Vector2 target) =>
        Vector2.DistanceSquared(center, target) <= CounterRange * CounterRange && Vector2.Dot(target - center, direction) >= 0;
    internal static TagEnum? ChooseEnvironment(bool lava, bool water, bool ice, bool cloud, bool thunder = false) =>
        lava ? TagEnum.Pyro : water ? TagEnum.Hydro : ice ? TagEnum.Cryo : cloud ? TagEnum.Anemo : thunder ? TagEnum.Electro : null;
    internal static TagEnum? Environment(Player player)
    {
        bool ice = false, cloud = false, thunder = false;
        int y = (int)((player.Bottom.Y + 2) / 16);
        for (int x = (int)(player.Left.X / 16); x <= (int)((player.Right.X - 1) / 16); x++)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            if (!tile.HasUnactuatedTile) continue;
            ice |= tile.TileType is TileID.IceBlock or TileID.CorruptIce or TileID.FleshIce or TileID.HallowedIce or TileID.BreakableIce;
            cloud |= tile.TileType is TileID.Cloud or TileID.RainCloud or TileID.SnowCloud;
            thunder |= tile.TileType is TileID.Sand or TileID.Pearlsand or TileID.Pearlstone or TileID.HallowedGrass;
        }
        return ChooseEnvironment(player.lavaWet, player.wet && !player.honeyWet && !player.shimmerWet && !player.lavaWet, ice, cloud, thunder);
    }
}
