using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Tag;

/// <summary>作者确认的测试参数，不自动登记到正式游戏。</summary>
static class TestReactions
{
    public const float BaseCooldown = 60;
    // 故意逆序登记，验证按槽编号而非注册顺序选反应。
    public static ReactionTable Table { get; } = new(
        new(TagEnum.PytoCryo, 1.0, BaseCooldown, new(1, 1.5), new(4, 1)),
        new(TagEnum.HydroAnemo, 1.5, BaseCooldown, new(2, 1), new(6, 1)),
        new(TagEnum.HydroElectro, 2.0, BaseCooldown, new(2, 1), new(3, 1)));
}
