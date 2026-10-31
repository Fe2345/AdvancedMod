namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>运动试验参数，不代表最终战斗平衡；攻击参数独立放在DestroyerAttackRules。</summary>
internal static class DestroyerPhaseTwoRules
{
    internal const int ChargeMaximum = 20;
    internal const float CruiseSpeed = 14, CrashSpeed = 21, TurnDegrees = 2.5f;
    internal const float Runway = 1100, StartMinimum = 900, StartMaximum = 1200, PassDistance = 500;
    internal const int PrepareTicks = 180, RepositionTicks = 60, TelegraphTicks = 30;
    internal const int AccelerationTicks = 30, RecoveryTicks = 42, IdleTicks = 150, CrashTimeout = 120;
    internal const int UltimateWarningTicks = 45, UltimateStrikeTicks = 18, UltimateRepeats = 3;
    internal const float PredictionTicks = 12, PredictionLimit = 128;
}
