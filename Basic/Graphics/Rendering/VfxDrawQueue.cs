using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace AdvancedMod.Basic.Graphics.Rendering;

public enum VfxMaterial { SoftGlow, PathCore, SlashBody, SlashRibbon, SlashRibbonGlow }

/// <summary>当前只接入 PostDrawTiles；稳定排序保留相同顺序的提交关系。</summary>
public sealed class VfxDrawQueue
{
    private readonly record struct Command(VfxMesh Mesh, VfxMaterial Material, int Order, int Sequence);
    private readonly List<Command> commands = new();
    public void Add(VfxMesh mesh, VfxMaterial material, int order = 0)
    { if (mesh.Count > 0) commands.Add(new(mesh, material, order, commands.Count)); }
    public void Clear() => commands.Clear();
    internal void Draw(BasicEffect effect)
    {
        commands.Sort(static (a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
        var device = Main.instance.GraphicsDevice;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var sampler = device.SamplerStates[0]; var texture = device.Textures[0];
        var indices = device.Indices; var bindings = device.GetVertexBuffers();
        try
        {
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            device.SamplerStates[0] = SamplerState.LinearClamp;
            effect.VertexColorEnabled = true;
            effect.TextureEnabled = false;
            effect.World = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0);
            effect.View = Main.GameViewMatrix.TransformationMatrix;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            foreach (var command in commands)
            {
                effect.TextureEnabled = command.Material is VfxMaterial.SlashRibbon or VfxMaterial.SlashRibbonGlow;
                if (effect.TextureEnabled) effect.Texture = VfxTextures.SlashBands;
                // 颜色统一预乘；辉光采用 One/One，避免再次乘入 Alpha。
                device.BlendState = command.Material is VfxMaterial.SoftGlow or VfxMaterial.SlashRibbonGlow ? GlowBlend : BlendState.AlphaBlend;
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, command.Mesh.Vertices, 0, command.Mesh.Count / 3);
                }
            }
        }
        finally
        {
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = raster;
            device.SamplerStates[0] = sampler; device.Textures[0] = texture;
            device.Indices = indices; device.SetVertexBuffers(bindings);
        }
    }
    private static BlendState glowBlend;
    private static BlendState GlowBlend => glowBlend ??= new BlendState
    {
        ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One
    };
    internal static void Unload() { glowBlend?.Dispose(); glowBlend = null; }
}
