// 2026-10-27：按环境变量启用的最小 TML 接入；显式场景背景，不绑定实体。
using System;
using AdvancedMod.Basic.Graphics.Water;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

public sealed class WaterPreviewSystem : ModSystem
{
    private WaterRenderer renderer;
    private Effect effect;
    private readonly WaterSettings settings=new();
    private double seconds;
    public override void Load()
    {
        if(!Main.dedServ && Environment.GetEnvironmentVariable("ADVANCEDMOD_WATER_TEST")=="1") On_Main.Draw += Draw;
    }
    public override void Unload()
    {
        On_Main.Draw -= Draw;
        renderer?.Dispose(); renderer=null; effect=null;
    }
    private void Draw(On_Main.orig_Draw orig,Main self,GameTime gameTime)
    {
        orig(self,gameTime);
        if(Main.gameMenu || Main.screenTarget==null) return;
        renderer ??= new WaterRenderer(self.GraphicsDevice,256);
        effect ??= ModContent.Request<Effect>("AdvancedMod/Assets/Shaders/WaterVortex",ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        seconds+=gameTime.ElapsedGameTime.TotalSeconds;
        renderer.Update(seconds,settings);
        renderer.Draw(Main.spriteBatch,effect,Main.screenTarget,new Rectangle(Main.screenWidth/2-250,Main.screenHeight/2-250,500,500),Matrix.Identity,settings,(float)seconds);
        if(seconds>6 && seconds<7)
        {
            string dir=System.IO.Path.Combine(Main.SavePath,"WaterCaptures"); System.IO.Directory.CreateDirectory(dir);
            var device=self.GraphicsDevice;
            int w=device.PresentationParameters.BackBufferWidth,h=device.PresentationParameters.BackBufferHeight;
            var data=new Color[w*h];device.GetBackBufferData(data);
            using var texture=new Texture2D(device,w,h);texture.SetData(data);
            using var output=System.IO.File.Create(System.IO.Path.Combine(dir,"fusion.png"));texture.SaveAsPng(output,w,h);
            seconds=8;
        }
        if(seconds>9) Main.instance.Exit();
    }
}
