using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using AdvancedMod.Basic.Tag;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>天裁的元素配色及程序化剑形；虚影与反击共用轮廓材质。</summary>
internal static class HallowSwordVisuals
{
    // 保留反击原有碰撞宽度，视觉材质加宽不扩大伤害判定。
    internal const float BladeThickness = 4;
    private static Texture2D monochrome;
    private static Texture2D phantomFront, phantomSide, phantomGlow;

    private static Texture2D PhantomMask(ref Texture2D cached, string name)
    {
        if (cached != null) return cached;
        var source = ModContent.Request<Texture2D>("AdvancedMod/Assets/Textures/Hallow/HeavensJudgmentPhantom" + name).Value;
        var pixels = new Color[source.Width * source.Height];
        source.GetData(pixels);
        // 遮罩只读取Alpha，再显式生成预乘白色，避免PNG透明边缘的白RGB造成硬白边。
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.White * (pixels[i].A / 255f);
        cached = new Texture2D(Main.instance.GraphicsDevice, source.Width, source.Height);
        cached.SetData(pixels);
        return cached;
    }
    private static readonly Vector2 Grip = new(24, 196);
    private const float GripToTip = 192;
    internal static void DrawBody(Vector2 grip, float angle, float radius, TagEnum? element, Color lightColor)
    {
        var source = ModContent.Request<Texture2D>("AdvancedMod/Assets/Textures/Hallow/HeavensJudgmentBlade").Value;
        Texture2D texture = source;
        Color color = Color.Lerp(lightColor, Color.White, 0.7f);
        if (element.HasValue)
        {
            if (monochrome == null)
            {
                var data = new Color[source.Width * source.Height]; source.GetData(data);
                for (int i = 0; i < data.Length; i++)
                {
                    // 资源已预乘，保留Alpha并对预乘RGB去色，避免直接乘青绿贴图压暗火色。
                    byte value = (byte)System.Math.Max(data[i].R, System.Math.Max(data[i].G, data[i].B));
                    data[i] = new Color(value, value, value, data[i].A);
                }
                monochrome = new Texture2D(Main.instance.GraphicsDevice, source.Width, source.Height); monochrome.SetData(data);
            }
            texture = monochrome; color = ElementColor(element);
        }
        Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null, color, angle + MathHelper.PiOver2,
            Grip, radius / GripToTip, SpriteEffects.None);
    }
    internal static void Unload()
    {
        foreach (var texture in new[] { monochrome, phantomFront, phantomSide, phantomGlow })
            if (texture != null) Main.QueueMainThreadAction(texture.Dispose);
        monochrome = phantomFront = phantomSide = phantomGlow = null;
    }
    internal static Color ElementColor(TagEnum? element) => element switch
    {
        TagEnum.Pyro => new Color(255, 110, 45),
        TagEnum.Hydro => new Color(65, 155, 255),
        TagEnum.Electro => new Color(185, 110, 255),
        TagEnum.Cryo => new Color(135, 235, 255),
        TagEnum.Anemo => new Color(80, 235, 190),
        _ => new Color(71, 233, 170)
    };

    private static readonly Vector2 PhantomGrip = new(32, 204);
    private const float PhantomSideShade = 0.68f;

    /// <summary>白色轮廓统一染色；纵轴偏转压缩正面并显露厚度，侧面不会消失。</summary>
    internal static void DrawPhantom(Vector2 grip, float angle, float radius, Color color, float opacity, float yaw)
    {
        float scale = radius / GripToTip;
        float face = MathF.Cos(yaw), edge = MathF.Abs(MathF.Sin(yaw));
        SpriteEffects flip = face < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        void Layer(Texture2D texture, float width, Color tint, SpriteEffects effects)
        {
            if (width < 0.001f) return;
            Main.EntitySpriteDraw(texture, grip - Main.screenPosition, null, tint * opacity,
                angle + MathHelper.PiOver2, PhantomGrip, new Vector2(width, 1) * scale, effects);
        }
        Layer(PhantomMask(ref phantomGlow, "Glow"), MathF.Max(MathF.Abs(face), edge * 0.15f), color, flip);
        Layer(PhantomMask(ref phantomSide, "Side"), edge, color * PhantomSideShade, SpriteEffects.None);
        Layer(PhantomMask(ref phantomFront, "Front"), MathF.Abs(face), color, flip);
    }

    /// <summary>反击复用新虚影正面纹理，握点到剑尖仍等于实际剑长。</summary>
    internal static void DrawSimple(Vector2 grip, float angle, float radius, Color color, float opacity)
    {
        DrawPhantom(grip, angle, radius, color, opacity, 0);
    }


}

public sealed class HallowSwordVisualSystem : ModSystem
{
    public override void Unload() => HallowSwordVisuals.Unload();
}
