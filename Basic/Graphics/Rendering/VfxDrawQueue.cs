using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using AdvancedMod.Contents.Vfx;

namespace AdvancedMod.Basic.Graphics.Rendering;

public enum VfxMaterial { SoftGlow, PathCore, SlashBody, SlashRibbon, SlashRibbonGlow, Lightning }

/// <summary>当前只接入 PostDrawTiles；稳定排序保留相同顺序的提交关系。</summary>
public sealed class VfxDrawQueue
{
    private readonly record struct SlashStyle(Vector3 Primary, Vector3 Secondary, Vector3 Flame, float Opacity);
    private readonly record struct Command(VfxMesh Mesh, VfxMaterial Material, int Order, int Sequence, SlashStyle? Slash = null, IdyllicDraw? Idyllic = null);
    private readonly List<Command> commands = new();
    public void Add(VfxMesh mesh, VfxMaterial material, int order = 0)
    { if (mesh.Count > 0) commands.Add(new(mesh, material, order, commands.Count)); }
    public void Clear() => commands.Clear();
    // 2026-10-30：专用五层材质沿用现有队列与状态恢复，不另建生命周期容器。
    internal void AddIdyllic(VfxMesh mesh, IdyllicDraw draw)
    { if(mesh.Count>0) commands.Add(new(mesh,VfxMaterial.SlashBody,0,commands.Count,Idyllic:draw)); }
    /// <summary>提交刀光材质的三色与整体退场透明度；几何仍由调用方持有。</summary>
    public void AddSlash(VfxMesh mesh, Color primary, Color secondary, Color flame, float opacity)
    {
        if (mesh.Count > 0) commands.Add(new(mesh, VfxMaterial.SlashRibbon, 0, commands.Count,
            new(primary.ToVector3(), secondary.ToVector3(), flame.ToVector3(), opacity)));
    }
    internal void Draw(BasicEffect effect)
    {
        commands.Sort(static (a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
        var device = Main.instance.GraphicsDevice;
        var blend = device.BlendState; var depth = device.DepthStencilState; var raster = device.RasterizerState;
        var sampler = device.SamplerStates[0]; var texture = device.Textures[0];
        var sampler1 = device.SamplerStates[1]; var texture1 = device.Textures[1];
        var sampler2 = device.SamplerStates[2]; var texture2 = device.Textures[2];
        var sampler3 = device.SamplerStates[3]; var texture3 = device.Textures[3];
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
                if(command.Idyllic is {} idyllic)
                {
                    IdyllicMaterial.Draw(device,command.Mesh,effect.World*effect.View*effect.Projection,idyllic);
                    continue;
                }
                if (command.Material == VfxMaterial.Lightning)
                {
                    lightningEffect ??= ModContent.Request<Effect>("AdvancedMod/Assets/Shaders/Lightning", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    lightningEffect.Parameters["uWorldViewProjection"].SetValue(effect.World * effect.View * effect.Projection);
                    device.BlendState = GlowBlend;
                    lightningEffect.CurrentTechnique.Passes["TrailPass"].Apply();
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, command.Mesh.Vertices, 0, command.Mesh.Count / 3);
                    continue;
                }
                if (command.Slash is { } style)
                {
                    slashEffect ??= ModContent.Request<Effect>("AdvancedMod/Assets/Shaders/HallowSlash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                    slashEffect.Parameters["uWorldViewProjection"].SetValue(effect.World * effect.View * effect.Projection);
                    slashEffect.Parameters["uColor"].SetValue(style.Primary);
                    slashEffect.Parameters["uSecondaryColor"].SetValue(style.Secondary);
                    slashEffect.Parameters["FlameColor"].SetValue(style.Flame);
                    slashEffect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
                    slashEffect.Parameters["uOpacity"].SetValue(style.Opacity);
                    // 对照原泰拉刃：白底层与云纹层都用One/InverseSourceAlpha，RGB不再乘低Alpha。
                    device.BlendState = BlendState.AlphaBlend;
                    for (int layer = 0; layer < 2; layer++)
                    {
                        slashEffect.Parameters["uImage1"].SetValue(layer == 0 ? VfxTextures.SlashWhite : VfxTextures.SlashCloud);
                        slashEffect.CurrentTechnique.Passes["TrailPass"].Apply();
                        device.DrawUserPrimitives(PrimitiveType.TriangleList, command.Mesh.Vertices, 0, command.Mesh.Count / 3);
                    }
                    continue;
                }
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
            device.SamplerStates[1] = sampler1; device.Textures[1] = texture1;
            device.SamplerStates[2] = sampler2; device.Textures[2] = texture2;
            device.SamplerStates[3] = sampler3; device.Textures[3] = texture3;
            device.Indices = indices; device.SetVertexBuffers(bindings);
        }
    }
    private static BlendState glowBlend;
    private static Effect slashEffect;
    private static Effect lightningEffect;
    private static BlendState GlowBlend => glowBlend ??= new BlendState
    {
        ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One
    };
    internal static void Unload() { glowBlend?.Dispose(); glowBlend = null; slashEffect = null; lightningEffect = null; }
}
