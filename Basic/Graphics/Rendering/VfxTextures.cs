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
    private static Texture2D slashWhite, slashCloud;
    public static Texture2D SlashWhite => slashWhite ??= CreateSlashCloud(true);
    public static Texture2D SlashCloud => slashCloud ??= CreateSlashCloud(false);
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
        slashWhite?.Dispose(); slashCloud?.Dispose(); slashWhite = slashCloud = null;
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

    /// <summary>周期性多层值噪声；用于替代原模组Wave图片，边界连续，固定种子不随帧重建。</summary>
    internal static float SlashCloudIntensity(float u, float v)
    {
        static float Hash(int x, int y, int period)
        {
            uint n = (uint)((x % period + period) % period) * 374761393u + (uint)((y % period + period) % period) * 668265263u;
            n = (n ^ (n >> 13)) * 1274126177u;
            return ((n ^ (n >> 16)) & 65535) / 65535f;
        }
        float sum = 0, weight = 0.5f, weights = 0;
        for (int period = 4; period <= 32; period *= 2)
        {
            float x = u * period, y = v * period;
            int ix = (int)MathF.Floor(x), iy = (int)MathF.Floor(y);
            float tx = x - ix, ty = y - iy;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float row0 = MathHelper.Lerp(Hash(ix, iy, period), Hash(ix + 1, iy, period), tx);
            float row1 = MathHelper.Lerp(Hash(ix, iy + 1, period), Hash(ix + 1, iy + 1, period), tx);
            sum += MathHelper.Lerp(row0, row1, ty) * weight;
            weights += weight; weight *= 0.5f;
        }
        return Math.Clamp((sum / weights - 0.2f) * 1.5f, 0, 1);
    }
    private static Texture2D CreateSlashCloud(bool white)
    {
        if (Main.dedServ) throw new InvalidOperationException("专用服务器不能请求视觉纹理。");
        int size = white ? 1 : MaskSize;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float value = white ? 1 : SlashCloudIntensity((float)x / size, (float)y / size);
            pixels[y * size + x] = new Color(value, value, value, 1);
        }
        var texture = new Texture2D(Main.instance.GraphicsDevice, size, size);
        texture.SetData(pixels);
        return texture;
    }
}
