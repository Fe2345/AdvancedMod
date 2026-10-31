using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>只重染原图中的红色灯芯；金属像素、帧和原版绘制锚点保持不变。</summary>
public sealed class DestroyerElementDrawing : ModSystem
{
    private static readonly Dictionary<(int, ProbeElement, bool), Texture2D> textures = new();
    internal static Texture2D Texture(int type, ProbeElement element, bool glow)
    {
        var key = (type, element, glow);
        if (textures.TryGetValue(key, out var cached)) return cached;
        Main.instance.LoadNPC(type);
        Texture2D source = glow ? (type == NPCID.Probe ? TextureAssets.Probe.Value : TextureAssets.Dest[1].Value) : TextureAssets.Npc[type].Value;
        var pixels = new Color[source.Width * source.Height]; source.GetData(pixels);
        Color tint = DestroyerPhaseOneRules.Color(element);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color p = pixels[i];
            // 发光图也带有原红色，直接乘蓝色会变黑，必须先取出其强度。
            if (glow || p.R > p.G * 1.25f && p.R > p.B * 1.25f)
            {
                float value = Math.Max(p.R, Math.Max(p.G, p.B)) / 255f;
                pixels[i] = new Color((byte)(tint.R * value), (byte)(tint.G * value), (byte)(tint.B * value), p.A);
            }
        }
        var result = new Texture2D(Main.instance.GraphicsDevice, source.Width, source.Height);
        result.SetData(pixels); textures.Add(key, result); return result;
    }
    internal static void Draw(NPC npc, ProbeElement element, SpriteBatch batch, Vector2 screenPos, Color color)
    {
        var texture = Texture(npc.type, element, false);
        var origin = new Vector2(texture.Width / 2, texture.Height / Main.npcFrameCount[npc.type] / 2);
        var position = new Vector2(npc.Center.X, npc.Bottom.Y - origin.Y * npc.scale + 4 + Main.NPCAddHeight(npc)) - screenPos;
        var effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        batch.Draw(texture, position + new Vector2(0, npc.gfxOffY), npc.frame, npc.GetAlpha(color), npc.rotation, origin, npc.scale, effects, 0);
        if (color != Color.Black)
            batch.Draw(Texture(npc.type, element, true), position, npc.frame,
                new Color(255, 255, 255, 0) * npc.Opacity, npc.rotation, origin, npc.scale, effects, 0);
    }
    public override void Unload()
    {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(() => { foreach (var texture in textures.Values) texture.Dispose(); textures.Clear(); });
    }
}
