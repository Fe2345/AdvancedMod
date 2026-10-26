using System;
using System.IO;
using System.Text.Json;
using AdvancedMod.Basic.Graphics.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>显式提供原XNB路径时，在真实GPU比较原字节码与重建FX；正常游戏不运行。</summary>
internal static class HallowShaderLiveTests
{
    internal static void Run()
    {
        string path = Environment.GetEnvironmentVariable("ADVANCEDMOD_HALLOW_SHADER_REFERENCE");
        if (string.IsNullOrEmpty(path)) return;
        var device = Main.instance.GraphicsDevice;
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var texture = device.Textures[1]; var sampler = device.SamplerStates[1];
        const int width = 128, height = 64;
        try
        {
            using var stream = File.OpenRead(path); using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != 'X' || reader.ReadByte() != 'N' || reader.ReadByte() != 'B') throw new InvalidDataException("不是XNB");
            reader.ReadByte(); reader.ReadByte();
            if (reader.ReadByte() != 0) throw new InvalidDataException("参考文件需要未压缩XNB");
            reader.ReadInt32(); int count = reader.Read7BitEncodedInt();
            for (int i = 0; i < count; i++) { reader.ReadString(); reader.ReadInt32(); }
            reader.Read7BitEncodedInt(); reader.Read7BitEncodedInt();
            using var original = new Effect(device, reader.ReadBytes(reader.ReadInt32()));
            var rebuilt = ModContent.Request<Effect>("AdvancedMod/Assets/Shaders/HallowSlash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            var results = new System.Collections.Generic.List<object>();
            foreach (bool cloud in new[] { false, true })
            foreach (float time in new[] { 0f, 0.37f, 2.1f })
            {
                Color[] Render(Effect effect, bool reconstruct)
                {
                    device.SetRenderTarget(target); device.Clear(Color.Transparent);
                    device.BlendState = BlendState.Opaque; device.DepthStencilState = DepthStencilState.None; device.RasterizerState = RasterizerState.CullNone;
                    effect.Parameters["uWorldViewProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0, width, height, 0, -1, 1));
                    effect.Parameters["uColor"].SetValue(new Vector3(0, 162 / 255f, 232 / 255f));
                    effect.Parameters["uSecondaryColor"].SetValue(new Vector3(71 / 255f, 233 / 255f, 60 / 255f));
                    effect.Parameters["FlameColor"].SetValue(new Vector3(71 / 255f, 233 / 255f, 60 / 255f));
                    effect.Parameters["uTime"].SetValue(time); effect.Parameters["uWidth"].SetValue(1f);
                    effect.Parameters["uAlpha"].SetValue(1f); effect.Parameters["uDarkshade"].SetValue(0.2f);
                    var sample = cloud ? VfxTextures.SlashCloud : VfxTextures.SlashWhite;
                    if (reconstruct) { effect.Parameters["uOpacity"].SetValue(1f); effect.Parameters["uImage1"].SetValue(sample); }
                    else effect.Parameters["flipped"].SetValue(false);
                    device.Textures[1] = sample; device.SamplerStates[1] = SamplerState.LinearWrap;
                    VertexPositionColorTexture V(float x, float y)
                    {
                        var uv = new Vector2(x / width, y / height);
                        return new(new Vector3(x, y, 0), Color.White, reconstruct ? Vector2.One - uv : uv);
                    }
                    var vertices = new[] { V(0, 0), V(width, 0), V(0, height), V(width, 0), V(width, height), V(0, height) };
                    effect.CurrentTechnique.Passes["TrailPass"].Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
                    device.SetRenderTargets(targets);
                    var pixels = new Color[width * height]; target.GetData(pixels); return pixels;
                }
                var expected = Render(original, false); var actual = Render(rebuilt, true);
                int max = 0, litPixels = 0, glowPixels = 0; long sum = 0;
                for (int i = 0; i < actual.Length; i++)
                {
                    var a = actual[i]; var b = expected[i];
                    if (b.R + b.G + b.B > 0) litPixels++;
                    if (b.R > b.A || b.G > b.A || b.B > b.A) glowPixels++;
                    foreach (int error in new[] { Math.Abs(a.R - b.R), Math.Abs(a.G - b.G), Math.Abs(a.B - b.B), Math.Abs(a.A - b.A) })
                    { max = Math.Max(max, error); sum += error; }
                }
                results.Add(new { cloud, time, litPixels, glowPixels, maximumByteError = max,
                    meanByteError = (double)sum / (actual.Length * 4), status = max <= 2 && litPixels > 0 && glowPixels > 0 ? "PASS" : "FAIL" });
            }
            File.WriteAllText(Path.Combine(Main.SavePath, "HallowShaderComparison.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            device.SetRenderTargets(targets); device.Viewport = viewport;
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.Textures[1] = texture; device.SamplerStates[1] = sampler;
        }
    }
}
