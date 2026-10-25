using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AdvancedMod.Basic.Graphics.Rendering;

/// <summary>在绘制线程按需创建的共享软遮罩；世界切换保留，模组卸载统一释放。</summary>
public static class VfxTextures
{
    private const int MaskSize = 128;
    private static Texture2D softPoint, skyBeam;
    public static Texture2D SoftPoint => softPoint ??= Create(false);
    public static Texture2D SkyBeam => skyBeam ??= Create(true);
    private static Texture2D Create(bool beam)
    {
        if (Main.dedServ) throw new InvalidOperationException("专用服务器不能请求视觉纹理。");
        var pixels = new Color[MaskSize * MaskSize];
        for (int y = 0; y < MaskSize; y++)
        for (int x = 0; x < MaskSize; x++)
        {
            float u = (float)x / (MaskSize - 1), v = (float)y / (MaskSize - 1);
            float dx = u * 2 - 1, dy = v * 2 - 1;
            float alpha = beam ? MathF.Pow(Math.Max(0, 1 - dx * dx), 3) * MathF.Pow(1 - v, 1.5f) :
                MathF.Pow(Math.Max(0, 1 - dx * dx - dy * dy), 3);
            pixels[y * MaskSize + x] = Color.White * alpha;
        }
        var texture = new Texture2D(Main.instance.GraphicsDevice, MaskSize, MaskSize);
        texture.SetData(pixels);
        return texture;
    }
    internal static void Unload()
    {
        softPoint?.Dispose(); skyBeam?.Dispose(); softPoint = skyBeam = null;
    }
}
