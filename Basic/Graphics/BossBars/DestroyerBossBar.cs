using System;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ReLogic.Content;

namespace AdvancedMod.Basic.Graphics.BossBars;

/// <summary>毁灭者实验血条；通过官方单条血条入口完全替换默认绘制。</summary>
public sealed class DestroyerBossBar : ModBossBar
{
    public const float WidthFraction = 0.56f;
    public const float MaximumWidth = 920f;
    public const float BarHeight = 7f;
    public const float BottomMargin = 90f;
    public const float CircleRadius = 17f;
    private const int CircleSegments = 96;
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);
    private static readonly Color Red = new(239, 76, 71);
    private static readonly Color Gold = new(255, 209, 101);
    public static int DrawCount { get; private set; }
    private Asset<Texture2D> hallowIcon;
    public override void Load()
    {
        if (!Main.dedServ) hallowIcon = ModContent.Request<Texture2D>("AdvancedMod/Assets/UI/HallowIcon", AssetRequestMode.ImmediateLoad);
    }
    public override void Unload() => hallowIcon = null;
    public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax, ref float shield, ref float shieldMax)
    {
        if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs) return false;
        NPC npc = Main.npc[info.npcIndexToAimAt];
        if (!npc.active || npc.type != NPCID.TheDestroyer || npc.life <= 0) return false;
        life = npc.life; lifeMax = npc.lifeMax; shield = shieldMax = 0; return true;
    }
    public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
    {
        var state = npc.GetGlobalNPC<DestroyerBarState>();
        var behavior = npc.GetGlobalNPC<DestroyerBehavior>();
        bool hallow = behavior.Hallow;
        Main.instance.LoadNPC(NPCID.Probe);
        Texture2D icon = TextureAssets.Npc[NPCID.Probe].Value;
        Rectangle frame = icon.Frame(1, Main.npcFrameCount[NPCID.Probe]);
        int energy = state.Remaining, maximum = state.Maximum;
        if (hallow && behavior.PhaseTwo)
        { icon = hallowIcon.Value; frame = icon.Bounds; energy = behavior.ReactionCount; maximum = DestroyerPhaseTwoRules.ChargeMaximum; }
        DrawBar(spriteBatch, Language.GetTextValue("Mods.AdvancedMod.BossBars.Destroyer.DisplayName"),
            Language.GetTextValue("Mods.AdvancedMod.BossBars.Destroyer." + (hallow ? "HallowDescription" : "Description")),
            drawParams.LifeMax > 0 ? drawParams.Life / drawParams.LifeMax : 0,
            state.YellowRatio, energy, maximum, icon, frame);
        DrawCount++;
        return false;
    }
    /// <summary>实验预览与正式入口共用同一绘制；贴图、帧和能量上下限由调用方传入。</summary>
    internal static void DrawBar(SpriteBatch sb, string name, string title, float red, float yellow, int energy, int maximum, Texture2D icon, Rectangle frame)
    {
        float width = Math.Min(MaximumWidth, Main.screenWidth * WidthFraction);
        Vector2 center = new(Main.screenWidth * 0.5f, Main.screenHeight - BottomMargin);
        Vector2 left = center - new Vector2(width * 0.5f, 0);
        Fill(sb, left, width, BarHeight + 4, 1, new Color(206, 183, 154));
        Fill(sb, left + new Vector2(2, 0), width - 4, BarHeight, 1, new Color(51, 36, 40));
        Fill(sb, left + new Vector2(2, 0), width - 4, BarHeight, Math.Clamp(Math.Max(red, yellow), 0, 1), Gold);
        Fill(sb, left + new Vector2(2, 0), width - 4, BarHeight, Math.Clamp(red, 0, 1), Red);
        Text(sb, name, center + new Vector2(0, -47), 1f, Color.White);
        Text(sb, title, center + new Vector2(0, -23), 0.7f, new Color(240, 224, 205));
        if (maximum <= 0) return;
        Vector2 circle = left + new Vector2(width - CircleRadius, 30);
        Ring(sb, circle, CircleRadius, 1, new Color(41, 48, 61), 6);
        Ring(sb, circle, CircleRadius, Math.Clamp((float)energy / maximum, 0, 1), Gold, 3);
        float scale = 23f / Math.Max(frame.Width, frame.Height);
        sb.Draw(icon, circle, frame, Color.White, 0, frame.Size() * 0.5f, scale, SpriteEffects.None, 0);
        Text(sb, $"{energy}/{maximum}", circle + new Vector2(-55, -8), 0.65f, Color.White);
    }
    private static void Text(SpriteBatch sb, string text, Vector2 pos, float scale, Color color)
        => Terraria.Utils.DrawBorderString(sb, text, pos, color, scale, 0.5f, 0);
    private static void Fill(SpriteBatch sb, Vector2 left, float width, float height, float fraction, Color color)
    {
        // 逐行绘制六边形遮罩并裁切进度，保持两端尖角与描边不随血量拉伸。
        for (int row = 0; row < (int)height; row++)
        {
            float inset = Math.Abs(row + 0.5f - height * 0.5f);
            float end = Math.Min(width - inset, width * fraction);
            if (end > inset) sb.Draw(TextureAssets.MagicPixel.Value, new Vector2(left.X + inset, left.Y - height * 0.5f + row), Pixel, color, 0, Vector2.Zero, new Vector2(end - inset, 1), SpriteEffects.None, 0);
        }
    }
    private static void Ring(SpriteBatch sb, Vector2 center, float radius, float fraction, Color color, float thickness)
    {
        float sweep = MathHelper.TwoPi * fraction;
        int count = (int)Math.Ceiling(CircleSegments * fraction);
        for (int i = 0; i < count; i++)
        {
            float a = -MathHelper.PiOver2 + sweep * i / count, b = -MathHelper.PiOver2 + sweep * (i + 1) / count;
            Vector2 start = center + a.ToRotationVector2() * radius, end = center + b.ToRotationVector2() * radius;
            sb.Draw(TextureAssets.MagicPixel.Value, start, Pixel, color, (end - start).ToRotation(), new Vector2(0, 0.5f), new Vector2(Vector2.Distance(start, end) + 0.5f, thickness), SpriteEffects.None, 0);
        }
    }
}
