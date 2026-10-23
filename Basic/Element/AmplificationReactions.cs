using System;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Element;

/// <summary>正式增幅反应本体；异化不在此表中。</summary>
public static class AmplificationReactions
{
    public const double BaseCoefficient = 1.5;
    public const double LifeFraction = 0.015;
    public const double LifeThreshold = 0.25;
    public const double CoefficientPerLife = 0.1;
    public const float ChainDecay = 0.7f;
    public const float ChainRadius = 5 * 16;
    public static ReactionTable Create() => new(
        new(TagEnum.HydroElectro, BaseCoefficient, 60, new(2, 1), new(3, 1.5)),
        new(TagEnum.HydroAnemo, BaseCoefficient, 40, new(2, 1.5), new(6, 0.75)),
        new(TagEnum.PytoCryo, BaseCoefficient, 40, new(1, 1.5), new(4, 1.5)));

    /// <summary>仅计算拟支付量；实际扣血必须等待原生命中成功。</summary>
    public static int LifeCost(int life, int maximumLife) =>
        life <= 0 || maximumLife <= 0 || life < maximumLife * LifeThreshold
            ? 0 : (int)Math.Ceiling(life * LifeFraction);
}
