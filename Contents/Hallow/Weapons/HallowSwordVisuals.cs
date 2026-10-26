using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using AdvancedMod.Basic.Tag;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>天裁的元素配色及简约剑形；虚影与反击共用同一套线段尺寸。</summary>
internal static class HallowSwordVisuals
{
    internal const float BladeThickness = 4;
    internal const float GuardLength = 28;
    internal const float GuardThickness = 3;
    private static Texture2D monochrome;
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
    internal static void Unload() { var texture = monochrome; monochrome = null; if (texture != null) Main.QueueMainThreadAction(texture.Dispose); }
    internal static Color ElementColor(TagEnum? element) => element switch
    {
        TagEnum.Pyro => new Color(255, 110, 45),
        TagEnum.Hydro => new Color(65, 155, 255),
        TagEnum.Electro => new Color(185, 110, 255),
        TagEnum.Cryo => new Color(135, 235, 255),
        _ => new Color(71, 233, 170)
    };

    /// <summary>握点为剑柄短横线中心，长线终点与真实剑尖一致，所有尺寸随武器缩放。</summary>
    internal static void DrawSimple(Vector2 grip, float angle, float radius, Color color, float opacity)
    {
        float scale = radius / HallowBladeRules.BladeLength;
        Vector2 forward = angle.ToRotationVector2(), side = new(-forward.Y, forward.X);
        Line(grip, grip + forward * radius, BladeThickness * scale, color * opacity);
        Line(grip - side * GuardLength * scale / 2, grip + side * GuardLength * scale / 2,
            GuardThickness * scale, color * opacity);
    }

    private static void Line(Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 delta = to - from;
        // MagicPixel资源不是1×1，必须裁出单像素，才能让缩放向量表示实际线段长宽。
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
            delta.ToRotation(), new Vector2(0, 0.5f), new Vector2(delta.Length(), width), SpriteEffects.None);
    }
}

public sealed class HallowSwordVisualSystem : ModSystem
{
    public override void Unload() => HallowSwordVisuals.Unload();
}
