// 2026-10-27：仅环境变量开启的烘焙贴图验收，不绑定正式战斗效果。
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

public sealed class WaterSpritePreviewSystem : ModSystem
{
    private const int FrameSize = 400;
    private const int FrameCount = 24;
    private const int Columns = 6;
    private static readonly int[] DisplaySizes = { 200, 300, 400 };
    private Texture2D atlas;
    private string atlasPath;
    private double seconds;
    private int captures;

    public override void Load()
    {
        atlasPath = Environment.GetEnvironmentVariable("ADVANCEDMOD_WATER_SPRITE_TEST");
        if (!Main.dedServ && !string.IsNullOrWhiteSpace(atlasPath))
            On_Main.Draw += Draw;
    }

    public override void Unload()
    {
        On_Main.Draw -= Draw;
        atlas?.Dispose();
        atlas = null;
    }

    private void Draw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
        if (Main.gameMenu) return;
        if (atlas is null)
        {
            using var stream = File.OpenRead(atlasPath);
            atlas = Texture2D.FromStream(self.GraphicsDevice, stream);
            if (atlas.Width != FrameSize * Columns || atlas.Height != FrameSize * (FrameCount / Columns))
                throw new InvalidDataException("水旋涡图集必须为 2400×1600。");
        }
        seconds += gameTime.ElapsedGameTime.TotalSeconds;
        int frame = (int)(seconds * FrameCount) % FrameCount;
        var source = new Rectangle(frame % Columns * FrameSize, frame / Columns * FrameSize, FrameSize, FrameSize);
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        for (int i = 0; i < DisplaySizes.Length; i++)
        {
            int size = DisplaySizes[i];
            int x = (int)(Main.screenWidth * ((i + .5f) / DisplaySizes.Length)) - size / 2;
            int y = Main.screenHeight / 2 - size / 2;
            Main.spriteBatch.Draw(atlas, new Rectangle(x, y, size, size), source, Color.White);
            Utils.DrawBorderString(Main.spriteBatch, $"{size} px | source 400 | 24 fps", new Vector2(x, y + size + 8), Color.White, .7f);
        }
        Main.spriteBatch.End();
        if (captures < 2 && seconds >= 3 + captures * 2)
        {
            string dir = Path.Combine(Main.SavePath, "WaterSpriteCaptures");
            Directory.CreateDirectory(dir);
            var device = self.GraphicsDevice;
            int width = device.PresentationParameters.BackBufferWidth;
            int height = device.PresentationParameters.BackBufferHeight;
            var pixels = new Color[width * height];
            device.GetBackBufferData(pixels);
            using var texture = new Texture2D(device, width, height);
            texture.SetData(pixels);
            using var output = File.Create(Path.Combine(dir, $"comparison-{captures}.png"));
            texture.SaveAsPng(output, width, height);
            File.WriteAllText(Path.Combine(dir, "result.txt"), $"atlas={atlas.Width}x{atlas.Height}; frame={frame}; viewport={width}x{height}; sizes=200,300,400; sampler=PointClamp; captures={captures + 1}");
            captures++;
        }
        if (seconds > 7) Main.instance.Exit();
    }
}
