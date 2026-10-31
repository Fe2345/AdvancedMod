// 2026-10-30：隔离客户端中的真实 FNA 材质渲染、六角度与动画帧采集。
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Rendering;
using AdvancedMod.Contents.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace AdvancedMod.Contents.Development;
public sealed class IdyllicLiveTests : ModSystem
{
    private static bool Enabled=>Environment.GetEnvironmentVariable("ADVANCEDMOD_IDYLLIC_TEST")=="1";
    private int ticks;
    private bool done;
    public override void Load(){if(Enabled&&!Main.dedServ)On_Main.Draw+=Draw;}
    public override void Unload()=>On_Main.Draw-=Draw;
    public override void PostUpdateEverything(){if(Enabled&&!Main.gameMenu){Main.autoSave=false;ticks++;
        if(ticks==60){var demo=ModContent.GetInstance<IdyllicDemo>();demo.Settings=demo.Settings with{Original=true};demo.Play(true,.3f);}
    }}
    private void Draw(On_Main.orig_Draw orig,Main self,GameTime time)
    {
        orig(self,time);
        if(!Enabled||done||Main.gameMenu||ticks<120)return;
        done=true;
        string path=Path.Combine(Main.SavePath,"IdyllicCaptures");Directory.CreateDirectory(path);
        var results=new List<object>();
        try
        {
            var device=self.GraphicsDevice;
            var screenPixels=new Color[Main.screenWidth*Main.screenHeight];device.GetBackBufferData(screenPixels);
            using(var screenshot=new Texture2D(device,Main.screenWidth,Main.screenHeight)){
                screenshot.SetData(screenPixels);
                using var file=File.Create(Path.Combine(path,"in-game.png"));screenshot.SaveAsPng(file,screenshot.Width,screenshot.Height);
            }
            var prior=device.GetRenderTargets();var viewport=device.Viewport;
            using var target=new RenderTarget2D(device,Main.screenWidth,Main.screenHeight,false,SurfaceFormat.Color,DepthFormat.None);
            using var effect=new BasicEffect(device);
            using var batch=new SpriteBatch(device);
            var queue=new VfxDrawQueue();
            void Capture(string name,float progress,bool gallery=false,bool original=false,bool radius=false,float rotation=0,Color? bg=null)
            {
                device.SetRenderTarget(target);device.Clear(bg??new Color(22,24,35));queue.Clear();
                var labels=new List<(string,Vector2)>();
                int count=gallery?6:1;
                for(int i=0;i<count;i++)
                {
                    int angle=gallery?IdyllicDemo.Angles[i]:60;
                    var center=gallery?new Vector2(220+(i%3)*420,280+(i/3)*350):new Vector2(Main.screenWidth/2,Main.screenHeight/2);
                    var settings=new IdyllicSettings{ArcAngleDegrees=angle,Original=original,FixedRadius=radius,Rotation=rotation};
                    var instance=new IdyllicEffect(Main.screenPosition+center,settings,progress,true);
                    instance.CollectDrawCommands(queue,new(Main.screenPosition,new(Main.screenWidth,Main.screenHeight)));
                    var geometry=new IdyllicArcGeometry(angle,settings.Size,radius);
                    labels.Add(($"{(original?"Original":angle+" deg")}   L={geometry.ChordLength:0.0} R={geometry.Radius:0.0}",center+new Vector2(-180,115)));
                }
                queue.Draw(effect);
                var pixels=new Color[Main.screenWidth*Main.screenHeight];target.GetData(pixels);
                int changed=0;var background=bg??new Color(22,24,35);
                foreach(var c in pixels)if(c!=background)changed++;
                if(progress>0&&progress<1&&changed==0)throw new Exception("材质未绘制："+name);
                batch.Begin();
                Utils.DrawBorderString(batch,$"Idyllic / {name} / p={progress:0.00} / width=24 / feather=1",new Vector2(30,30),Color.White,.8f);
                foreach(var label in labels)Utils.DrawBorderString(batch,label.Item1,label.Item2,Color.White,.7f);
                batch.End();
                device.SetRenderTargets(prior);device.Viewport=viewport;
                using(var file=File.Create(Path.Combine(path,name+".png")))target.SaveAsPng(file,target.Width,target.Height);
                results.Add(new{name,changedPixels=changed});
            }
            try{
                Capture("original",.3f,original:true);
                Capture("original-light",.3f,original:true,bg:new Color(200,205,215));
                Capture("angles-chord",.3f,gallery:true);
                Capture("angles-radius",.3f,gallery:true,radius:true);
                Capture("background-light",.3f,bg:new Color(200,205,215));
                Capture("rotation-179",.3f,rotation:179);
                Capture("rotation-181",.3f,rotation:181);
                Capture("rotation-minus179",.3f,rotation:-179);
                for(int i=0;i<=24;i++){Capture("animation-"+i.ToString("D2"),i/24f);Capture("original-animation-"+i.ToString("D2"),i/24f,original:true);}
            }finally{device.SetRenderTargets(prior);device.Viewport=viewport;}
            File.WriteAllText(Path.Combine(path,"report.json"),JsonSerializer.Serialize(new{tml=BuildInfo.tMLVersion.ToString(),results},new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(Exception e){File.WriteAllText(Path.Combine(path,"failure.txt"),e.ToString());Mod.Logger.Error(e);}
        Main.instance.Exit();
    }
}


