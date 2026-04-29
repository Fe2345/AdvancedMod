using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AdvancedMod.Utils;

/// <summary>
///     Handles preserving the contents of <see cref="RenderTarget2D" />s.
///     <br />
///     For most cases, <see cref="RenderTargetScope"/> is preferred for
///     temporarily swapping render targets.
/// </summary>
public static class RenderTargetPreserver
{
    /// <summary>
    ///     Forcefully sets the usage of a given set of render target bindings to
    ///     preserve contents.
    /// </summary>
    /// <param name="bindings"></param>
    public static void PreserveBindings(RenderTargetBinding[] bindings)
    {
        foreach (var binding in bindings)
        {
            if (binding.RenderTarget is not RenderTarget2D rt)
            {
                continue;
            }

            rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }
    }

    /// <summary>
    ///     A utility method that gets the current bindings from the graphics
    ///     device and ensures they're preserved.
    /// </summary>
    /// <returns></returns>
    public static RenderTargetBinding[] GetAndPreserveCurrentBindings()
    {
        var bindings = Main.instance.GraphicsDevice.GetRenderTargets();
        {
            PreserveBindings(bindings);
        }

        return bindings;
    }
}

