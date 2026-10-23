using Terraria.ModLoader;

namespace AdvancedMod;

/// <summary>模组入口，具体生命周期由各 ModSystem 管理。</summary>
public sealed class AdvancedMod : Mod
{
    public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI) =>
        Basic.Element.Tml.ElementNetwork.Receive(reader, whoAmI);
}
