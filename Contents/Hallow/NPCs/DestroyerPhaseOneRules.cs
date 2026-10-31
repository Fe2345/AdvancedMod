using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Element.Presentation;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Contents.Hallow.NPCs;

public enum ProbeElement : byte { None, Water, Fire, Ice, Lightning, Wind }

internal static class DestroyerPhaseOneRules
{
    internal const int ElementCount = 5;
    internal const float ReductionPerProbe = 0.01f;
    internal const double ShotAttachment = 1;
    internal const int ShotLifetime = 180;
    internal const float ReturnSpeed = 9f;
    internal const float ReturnAcceleration = 0.3f;
    internal const float ReturnCatchDistance = 20f;
    internal const float WindWidth = 128f;
    internal const float WindThickness = 6f;
    internal static ProbeElement Decode(byte value) => value is >= 1 and <= ElementCount ? (ProbeElement)value : ProbeElement.None;
    internal static TagEnum Tag(ProbeElement element) => element switch
    {
        ProbeElement.Water => TagEnum.Hydro, ProbeElement.Fire => TagEnum.Pyro,
        ProbeElement.Ice => TagEnum.Cryo, ProbeElement.Lightning => TagEnum.Electro,
        ProbeElement.Wind => TagEnum.Anemo, _ => throw new System.ArgumentOutOfRangeException(nameof(element))
    };
    internal static Color Color(ProbeElement element)
    { var (r, g, b) = ElementTextStyle.Rgb(Tag(element)); return new(r, g, b); }
}
