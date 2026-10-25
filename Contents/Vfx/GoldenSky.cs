using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Contents.Vfx;

public sealed record GoldenSkyStyle
{
    public Color Zenith { get; init; } = new(212, 146, 48);
    public Color Horizon { get; init; } = new(242, 197, 99);
    public Color Light { get; init; } = new(255, 211, 113);
    public float BackgroundOpacity { get; init; } = 0.78f;
    public float MistOpacity { get; init; } = 0.09f;
    public float BeamOpacity { get; init; } = 0.28f;
    public int BeamCount { get; init; } = 7;
    public int MistCount { get; init; } = 8;
}

/// <summary>只读取环境时钟；各背景深度只提交一次，不修改宿主 SpriteBatch。</summary>
public sealed class GoldenSky : CustomSky
{
    public const string Key = "AdvancedMod:GoldenSky";
    private const float BackgroundDepth = 7, MistDepth = 5, BeamDepth = 3;
    private readonly EnvironmentController.Layer layer;
    private readonly GoldenSkyStyle style;
    private readonly Action resetRequests;
    private bool active;
    public GoldenSky(EnvironmentController.Layer layer, Action resetRequests, GoldenSkyStyle style = null)
    {
        this.layer = layer; this.resetRequests = resetRequests; this.style = style ?? new();
        if (this.style.BeamCount < 0 || this.style.BeamCount > 64 || this.style.MistCount < 0 || this.style.MistCount > 64 ||
            !ValidOpacity(this.style.BackgroundOpacity) || !ValidOpacity(this.style.MistOpacity) || !ValidOpacity(this.style.BeamOpacity))
            throw new ArgumentException("天空样式预算或透明度无效。");
    }
    private static bool ValidOpacity(float value) => float.IsFinite(value) && value >= 0 && value <= 1;
    public override void Activate(Vector2 position, params object[] args) => active = true;
    public override void Deactivate(params object[] args) => active = false;
    public override bool IsActive() => active || layer.Visible;
    public override void Reset() { active = false; resetRequests(); }
    public override void Update(GameTime gameTime) { }
    public override float GetCloudAlpha() => 1 - layer.Intensity * 0.55f;
    public override void Draw(SpriteBatch batch, float minDepth, float maxDepth)
    {
        float alpha = layer.Intensity;
        if (alpha <= 0) return;
        int width = Main.screenWidth, height = Main.screenHeight;
        float time = (float)layer.Time;
        if (minDepth < BackgroundDepth && maxDepth >= BackgroundDepth)
            for (int y = 0; y < height; y++)
            {
                // 按像素行采样渐变，避免粗分带与重叠条纹。
                float t = (float)y / Math.Max(1, height - 1);
                batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, y, width, 1),
                    Color.Lerp(style.Zenith, style.Horizon, t) * (alpha * style.BackgroundOpacity));
            }
        if (minDepth < MistDepth && maxDepth >= MistDepth)
            for (int i = 0; i < style.MistCount; i++)
            {
                float phase = time * 0.12f + i * 0.73f;
                float x = ((i * 0.37f + time * 0.008f - Main.screenPosition.X / width * 0.015f) % 1.6f + 1.6f) % 1.6f - 0.3f;
                batch.Draw(VfxTextures.SoftPoint, new Rectangle((int)((x - 0.4f) * width),
                    (int)((0.1f + (float)i / Math.Max(1, style.MistCount) * 0.6f) * height), width, height / 5),
                    style.Light * (alpha * style.MistOpacity * (0.8f + 0.2f * MathF.Sin(phase))));
            }
        if (minDepth < BeamDepth && maxDepth >= BeamDepth)
            for (int beam = 0; beam < style.BeamCount; beam++)
            {
                float phase = beam * 2.17f;
                float x = (beam + 0.5f) / style.BeamCount * width + MathF.Sin(time * 0.14f + phase) * width * 0.018f;
                float intensity = alpha * style.BeamOpacity * (0.8f + MathF.Sin(time * 0.6f + phase) * 0.12f);
                var texture = VfxTextures.SkyBeam;
                batch.Draw(texture, new Vector2(x, -height * 0.05f), null, style.Light * intensity,
                    -0.04f, new Vector2(texture.Width * 0.5f, 0), new Vector2(width * 0.045f / texture.Width, height * 1.1f / texture.Height), SpriteEffects.None, 0);
            }
    }
}
