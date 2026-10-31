// 2026-10-30：五层风刃的客户端纯视觉实例与专用材质资源。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Vfx;

internal sealed record IdyllicSettings
{
    public float ArcAngleDegrees { get; init; } = 60;
    public float Size { get; init; } = 640;
    public float Thickness { get; init; } = 24;
    public float Feather { get; init; } = 1;
    public float Rotation { get; init; }
    public float Brightness { get; init; } = 1;
    public float Opacity { get; init; } = .55f;
    internal static readonly Vector3 WindTint = new(.92f, .97f, 1f);
    public bool FixedRadius { get; init; }
    public bool Original { get; init; }
    public IdyllicSettings Validated()
    {
        foreach(float v in new[]{ArcAngleDegrees,Size,Thickness,Feather,Rotation,Brightness,Opacity})
            if(!float.IsFinite(v)) throw new ArgumentException("参数必须是有限数值");
        var geometry=new IdyllicArcGeometry(ArcAngleDegrees,Size,FixedRadius);
        // 防止极小尺寸与极厚刀刃组合使径向带穿过圆心而翻折。
        return this with { ArcAngleDegrees=Math.Clamp(ArcAngleDegrees,15,120),Size=Math.Clamp(Size,32,1024),
            Thickness=Math.Clamp(Thickness,1,Math.Min(48,geometry.Radius/8)),Feather=Math.Clamp(Feather,.1f,3),Rotation=Rotation%360,
            Brightness=Math.Clamp(Brightness,0,3),Opacity=Math.Clamp(Opacity,0,1) };
    }
}
internal readonly record struct IdyllicDraw(int Layer,float Progress,IdyllicSettings Settings,float TextureScale);
internal sealed class IdyllicEffect : WorldEffect
{
    public const float Lifetime=1.25f;
    private readonly VfxMesh[] meshes={new(),new(),new(),new(),new()};
    internal IdyllicSettings Settings;
    internal Vector2 Center;
    internal float Progress;
    internal bool Paused;
    private bool stopped;
    public IdyllicEffect(Vector2 center,IdyllicSettings settings,float progress=0,bool paused=false)
    {Center=center;Settings=settings.Validated();Progress=progress;Paused=paused;}
    public override bool IsFinished=>stopped||(!Paused&&Progress>=1);
    public override void Stop()=>stopped=true;
    public override void Update(in VfxUpdateContext context){if(!Paused)Progress=Math.Min(1,Progress+context.DeltaSeconds/Lifetime);}
    // 原 Shader 伪透视逆映射。角度在调用此函数之前确定；五层保留各自 inset。
    internal static Vector2 Perspective(Vector2 q)
    {
        float sy=MathF.Sin(MathHelper.ToRadians(11)),cy=MathF.Cos(MathHelper.ToRadians(11));
        float sx=MathF.Sin(MathHelper.ToRadians(70)),cx=MathF.Cos(MathHelper.ToRadians(70));
        float px=q.X+sy*cx,py=q.Y-sx,k=cy*cx;
        float a=k*cy+px*sy,b=k*sy*sx-px*cy*sx,c=px*cy*cx*.5f-k*sy*cx*.5f;
        float d=py*sy,e=k*cx-py*cy*sx,f=py*cy*cx*.5f+k*sx*.5f;
        float det=a*e-b*d;
        return new((c*e-b*f)/det,(a*f-c*d)/det);
    }
    public override void CollectDrawCommands(VfxDrawQueue queue,in VfxDrawContext context)
    {
        if(stopped||Progress<=0||Progress>=1)return;
        var g=new IdyllicArcGeometry(Settings.ArcAngleDegrees,Settings.Size,Settings.FixedRadius);
        float rot=MathHelper.ToRadians(Settings.Rotation),cs=MathF.Cos(rot),sn=MathF.Sin(rot);
        Vector2 Rotate(Vector2 v)=>Center+new Vector2(v.X*cs-v.Y*sn,v.X*sn+v.Y*cs);
        for(int layer=0;layer<5;layer++)
        {
            var mesh=meshes[layer];mesh.Clear();
            float offset=layer==0?-8:layer==4?-11:-10;
            float inset=layer==2?480:481;
            Vector2 Place(float u,float radial)
            {
                Vector2 pos;
                if(Settings.Original)pos=new Vector2((u-.5f)*inset,(radial-.5f)*inset)*(Settings.Size/640);
                else {pos=g.Point(u,-(radial-.46f)*Settings.Thickness/.05f);pos.Y-=g.Sagitta*.5f;pos=Perspective(pos/640)*inset;}
                return Rotate(pos+new Vector2(0,offset));
            }
            int segments=Settings.Original?1:128;
            float r0=Settings.Original?0:.12f,r1=Settings.Original?1:.85f;
            for(int i=0;i<segments;i++)
            {
                float u0=(float)i/segments,u1=(float)(i+1)/segments;
                var a=Place(u0,r0);var b=Place(u0,r1);var c=Place(u1,r0);var d=Place(u1,r1);
                Vector2 UV(float u,float r)=>Settings.Original?new(u,r):new(r,u);
                mesh.Triangle(a,b,c,Color.White,Color.White,Color.White,UV(u0,r0),UV(u0,r1),UV(u1,r0));
                mesh.Triangle(b,d,c,Color.White,Color.White,Color.White,UV(u0,r1),UV(u1,r1),UV(u1,r0));
            }
            queue.AddIdyllic(mesh,new(layer,Progress,Settings,g.ArcLength/(640*MathF.PI/3)));
        }
    }
}
internal static class IdyllicMaterial
{
    private static Effect shader;
    private static readonly Dictionary<string,Texture2D> textures=new();
    private static string[][] names;
    public static void Load(Mod mod)
    {
        if(shader!=null)return;
        shader=ModContent.Request<Effect>("AdvancedMod/Assets/Shaders/IdyllicSlash",ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        // 独立读取流，避免主线程预加载与后台本地化读取争用 .tmod 的共享流。
        using var manifest=mod.GetFileStream("Assets/Idyllic/layers.json",true);
        using var doc=JsonDocument.Parse(manifest);
        names=new string[5][];int i=0;
        foreach(var layer in doc.RootElement.EnumerateArray())
        {
            names[i]=new string[4];int j=0;
            foreach(string key in new[]{"base_mask","distort_texture","color_gradient","mesh_wide_mask"})
            {
                string name=layer.GetProperty(key).GetString();names[i][j++]=name;
                if(textures.ContainsKey(name))continue;
                using var stream=mod.GetFileStream("Assets/Idyllic/"+name+".png.bin",true);
                textures[name]=Texture2D.FromStream(Main.instance.GraphicsDevice,stream);
            }
            i++;
        }
    }
    public static void Draw(GraphicsDevice device,VfxMesh mesh,Matrix matrix,IdyllicDraw draw)
    {
        if(shader==null)return;
        shader.Parameters["uWorldViewProjection"].SetValue(matrix);
        shader.Parameters["Progress"].SetValue(draw.Progress);
        shader.Parameters["Feather"].SetValue(draw.Settings.Feather);
        shader.Parameters["TextureScale"].SetValue(draw.TextureScale);
        shader.Parameters["Original"].SetValue(draw.Settings.Original?1f:0f);
        shader.Parameters["Brightness"].SetValue(draw.Settings.Brightness);
        shader.Parameters["WindOpacity"].SetValue(draw.Settings.Opacity);
        shader.Parameters["WindTint"].SetValue(IdyllicSettings.WindTint);
        string[] parameters={"MaskTexture","DistortTexture","GradientTexture","WideTexture"};
        for(int j=0;j<4;j++)shader.Parameters[parameters[j]].SetValue(textures[names[draw.Layer][j]]);
        shader.CurrentTechnique=shader.Techniques["Layer_"+draw.Layer];
        device.BlendState=BlendState.AlphaBlend;
        shader.CurrentTechnique.Passes[0].Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList,mesh.Vertices,0,mesh.Count/3);
    }
    public static void Unload(){foreach(var t in textures.Values)t.Dispose();textures.Clear();shader=null;names=null;}
}






