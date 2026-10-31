// 2026-10-30：风刃调参命令与客户端实例播放控制；不生成 Projectile。
using System;
using System.Collections.Generic;
using System.Globalization;
using AdvancedMod.Basic.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Visuals=AdvancedMod.Basic.Graphics.Vfx;
namespace AdvancedMod.Contents.Vfx;
public sealed class IdyllicDemo : ModSystem
{
    internal IdyllicSettings Settings=new();
    internal IdyllicEffect Current;
    private EffectHandle handle;
    internal bool Repeat,Visible;
    internal static readonly int[] Angles={120,90,60,45,30,15};
    public override void Load(){if(!Main.dedServ)Main.QueueMainThreadAction(()=>IdyllicMaterial.Load(Mod));}
    public override void Unload(){if(!Main.dedServ)Main.QueueMainThreadAction(IdyllicMaterial.Unload);}
    internal void Play(bool paused=false,float progress=0)
    {
        Visuals.Kill(handle);Visible=true;
        Current=new IdyllicEffect(Main.LocalPlayer.Center+new Vector2(0,-130),Settings,progress,paused);
        handle=Visuals.Create(Current);
    }
    internal void Stop(){Visuals.Kill(handle);Current=null;Repeat=Visible=false;}
    public override void OnWorldUnload()=>Stop();
    public override void PostUpdateEverything()
    {if(!Main.dedServ&&!Main.gameMenu&&Repeat&&(Current==null||Current.IsFinished))Play();}
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if(!Visible||Main.gameMenu)return;
        layers.Add(new LegacyGameInterfaceLayer("AdvancedMod: 风刃参数",()=>{
            Utils.DrawBorderString(Main.spriteBatch,Status(),new Vector2(24,92),Color.LightCyan,.75f);
            return true;
        },InterfaceScaleType.UI));
    }
    internal string Status()=> $"Idyllic {(Settings.Original?"Original":"Arc")}  Angle={Settings.ArcAngleDegrees:0}  Mode={(Settings.FixedRadius?"Radius":"Chord")}\nSize={Settings.Size:0} Thickness={Settings.Thickness:0.0} Feather={Settings.Feather:0.00} Rotation={Settings.Rotation:0}\nOpacity={Settings.Opacity:0.00}  Progress={Current?.Progress??0:0.000} Paused={Current?.Paused??false} Loop={Repeat}  /idyllic help";
}
public sealed class IdyllicCommand : ModCommand
{
    public override string Command=>"idyllic";
    public override CommandType Type=>CommandType.Chat;
    public override string Description=>"五层风刃纯视觉演示";
    public override string Usage=>"/idyllic play|loop|pause|progress 0..1|angle 15..120|mode chord/radius|size 32..1024|thickness 1..48|feather .1..3|rotation 度|brightness 0..3|opacity 0..1|original|reset|stop|status";
    public override void Action(CommandCaller caller,string input,string[] args)
    {
        if(Main.dedServ)return;
        var d=ModContent.GetInstance<IdyllicDemo>();string command=args.Length==0?"play":args[0].ToLowerInvariant();
        float Value(){if(args.Length!=2||!float.TryParse(args[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float v)||!float.IsFinite(v))throw new ArgumentException("请输入一个有限数值");return v;}
        try{
            switch(command){
                case "play": d.Repeat=false;d.Play();break;
                case "loop":d.Repeat=!d.Repeat;if(d.Repeat)d.Play();break;
                case "pause":if(d.Current==null||d.Current.IsFinished)d.Play(true,.3f);else d.Current.Paused=!d.Current.Paused;break;
                case "progress":d.Play(true,Math.Clamp(Value(),0,1));break;
                case "angle":d.Settings=d.Settings with{ArcAngleDegrees=Value(),Original=false};break;
                case "mode":if(args.Length!=2||args[1] is not("chord" or "radius"))throw new ArgumentException("mode chord 或 radius");d.Settings=d.Settings with{FixedRadius=args[1]=="radius"};break;
                case "size":d.Settings=d.Settings with{Size=Value()};break;
                case "thickness":d.Settings=d.Settings with{Thickness=Value()};break;
                case "feather":d.Settings=d.Settings with{Feather=Value()};break;
                case "rotation":d.Settings=d.Settings with{Rotation=Value()};break;
                case "brightness":d.Settings=d.Settings with{Brightness=Value()};break;
                case "opacity":d.Settings=d.Settings with{Opacity=Value()};break;
                case "original":d.Settings=d.Settings with{Original=true};break;
                case "reset":d.Settings=new();d.Repeat=false;d.Play(true,.3f);break;
                case "stop":d.Stop();break;
                case "status":break;
                default:caller.Reply(Usage);return;
            }
            d.Settings=d.Settings.Validated();
            if(d.Current!=null)d.Current.Settings=d.Settings;
            if(command is "angle" or "mode" or "original" && (d.Current==null||d.Current.IsFinished))d.Play(true,.3f);
            caller.Reply(d.Status());
        }catch(ArgumentException e){caller.Reply(e.Message+"\n"+Usage);}
    }
}

