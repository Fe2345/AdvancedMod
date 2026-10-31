using Microsoft.Xna.Framework;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>金属蠕虫鞭与刃杖联动参数；储量以八分之一单位计，避免连续扣减误差。</summary>
internal static class MetalWormRules
{
    internal const int Damage = 75, UseTicks = 24, TagDamage = 12, TagTicks = 240;
    internal const float Knockback = 2, ProbeDamageFactor = .5f, DrawWidth = 30;
    internal const int ProbeFireTick = 180, ProbeLifetime = 240, ProbeSyncTicks = 30, ShotLifetime = 180;
    internal const float ProbeOrbitRadius = 112, ProbeOrbitSpeed = .035f, ProbeMoveSpeed = 8,
        ProbeAcceleration = .3f, ProbeApproachTicks = 12, ProbeSearchRadius = 800;
    internal const float ProbeOffset = 56, ShotSpeed = 9;
    internal const int BladeCapacity = 10, BladeNormalCost = 2, BladeMarkedCost = 1;
    internal const double BladeAttachment = .25;
    internal const float BladeReturnSpeed = 16, BladeReturnAcceleration = .8f, BladeCatchRadius = 24, BladeRecallDistance = 3000;
    internal const string TexturePath = "AdvancedMod/Assets/Textures/Hallow/MetalWormWhip";
    // Imagegen原图保留不改；绘制时裁切三个独立部件，替换素材时只需调整此表。
    internal static readonly Rectangle HeadFrame = new(175, 100, 370, 780);
    internal static readonly Rectangle BodyFrame = new(175, 900, 370, 470);
    internal static readonly Rectangle TailFrame = new(175, 1380, 370, 690);
}
