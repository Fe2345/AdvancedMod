// 2026-10-26：元素颜色与反应显示键；多元素取首标签，静置按反应回落。
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Element.Presentation;

internal static class ElementTextStyle
{
    internal static TagEnum? SelectElement(TagSet tags, TagEnum? reaction = null)
    {
        foreach (var entry in tags) if (ElementSlots.FromTag(entry.type).HasValue) return entry.type;
        return reaction switch
        {
            TagEnum.HydroElectro => TagEnum.Electro,
            TagEnum.HydroAnemo => TagEnum.Hydro,
            TagEnum.PyroCryo => TagEnum.Pyro,
            _ => null
        };
    }

    internal static (byte R, byte G, byte B) Rgb(TagEnum element) => element switch
    {
        TagEnum.Pyro => (180, 32, 48),
        TagEnum.Hydro => (64, 160, 255),
        TagEnum.Electro => (185, 110, 255),
        TagEnum.Cryo => (150, 235, 255),
        TagEnum.Chlorophyte => (100, 220, 70),
        TagEnum.Anemo => (80, 225, 175),
        _ => throw new System.ArgumentOutOfRangeException(nameof(element), "只接受六种伤害元素")
    };

    internal static string ReactionKey(TagEnum reaction)
    {
        if (!ElementSlots.ReactionFromTag(reaction).HasValue)
            throw new System.ArgumentOutOfRangeException(nameof(reaction));
        return $"Mods.AdvancedMod.Reactions.{reaction}";
    }
}
