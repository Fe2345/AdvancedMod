using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Element;

/// <summary>当前开发验证用的临时效果；正式玩法完成后替换此表。</summary>
public static class DevelopmentReactions
{
    public const float BaseCooldown = 60;
    public static ReactionTable Create() => new(
        new(TagEnum.HydroElectro, 2.0, BaseCooldown, new(2, 1), new(3, 1)),
        new(TagEnum.HydroAnemo, 1.5, BaseCooldown, new(2, 1), new(6, 1)),
        new(TagEnum.PytoCryo, 1.0, BaseCooldown, new(1, 1.5), new(4, 1)));
}
