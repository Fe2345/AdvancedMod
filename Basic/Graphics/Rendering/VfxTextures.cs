using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AdvancedMod.Basic.Graphics.Rendering;

/// <summary>在绘制线程按需创建的共享软遮罩；世界切换保留，模组卸载统一释放。</summary>
public static class VfxTextures
{
    private const int MaskSize = 128;
    private static Texture2D softPoint, skyBeam, slashBands;
    private const int SlashWidth = 256, SlashHeight = 64;
    public static Texture2D SlashBands => slashBands ??= CreateSlashBands();
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
        softPoint?.Dispose(); skyBeam?.Dispose(); slashBands?.Dispose(); softPoint = skyBeam = slashBands = null;
    }

    /// <summary>展开的剑气条带：内缘柔化、外缘亮线和确定性波纹，输出预乘颜色。</summary>
    internal static float SlashIntensity(float u, float v)
    {
        float ripple = MathF.Sin(u * MathHelper.TwoPi * 3 + v * 7) * 0.012f;
        float lines = 0;
        ReadOnlySpan<float> centers = [0.34f, 0.57f, 0.76f, 0.94f];
        for (int i = 0; i < centers.Length; i++)
        {
            float d = (v - centers[i] - ripple * (1 - v)) / (0.018f + i * 0.004f);
            lines += MathF.Exp(-d * d) * (0.25f + i * 0.1f);
        }
        float grain = 0.93f + 0.07f * MathF.Sin(u * 39 + MathF.Sin(v * 17)) * MathF.Sin(u * 17 - v * 13);
        float edge = Math.Clamp(v / 0.22f, 0, 1) * Math.Clamp((1 - v) / 0.035f, 0, 1);
        return Math.Clamp((0.42f + lines) * grain * edge, 0, 1);
    }

    private static Texture2D CreateSlashBands()
    {
        if (Main.dedServ) throw new InvalidOperationException("专用服务器不能请求视觉纹理。");
        var pixels = new Color[SlashWidth * SlashHeight];
        for (int y = 0; y < SlashHeight; y++)
        for (int x = 0; x < SlashWidth; x++)
            pixels[y * SlashWidth + x] = Color.White * SlashIntensity((float)x / (SlashWidth - 1), (float)y / (SlashHeight - 1));
        var texture = new Texture2D(Main.instance.GraphicsDevice, SlashWidth, SlashHeight);
        texture.SetData(pixels);
        return texture;
    }
}
