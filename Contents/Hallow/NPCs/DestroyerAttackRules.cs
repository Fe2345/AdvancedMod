namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>首版弹幕测试参数；运动节奏独立保留，后续按实机手感调整。</summary>
internal static class DestroyerAttackRules
{
    internal const int FireCount = 12, WindCount = 6, LaserCount = 8, WaterCount = 4;
    internal const float TriggerDistance = 1024, FireSpeed = 9, WindSpeed = 12, LaserSpeed = 16;
    internal const float WindSpawnRadius = 650, WaterRadius = 420, WaterTurns = 2;
    internal const int ShotLifetime = 180, WaterLifetime = 120, FrostLifetime = 45;
    internal const float FrostLength = 1400, FrostWidth = 24;
    internal const double ElementAmount = 1;
    internal const float WaterMultiplier = .25f, FrostMultiplier = .5f;
    internal const float WhipRootDistance = 700, WhipLength = 1200, WhipBend = .4f, WhipScale = .5f;
    internal const int WarningSweepTicks = 30, WarningHoldTicks = 15, WhipSamples = 80;
    internal const float FlameScale = 1.3f, FlameDustScale = 3f;
}
