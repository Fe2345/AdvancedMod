// 2026-10-27：独立版和 TML 共用的 GPU 资源；宿主提供背景、矩阵和 Effect。
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AdvancedMod.Basic.Graphics.Water;

public sealed class WaterRenderer : IDisposable
{
    public static void ValidateEffect(Effect effect)
    {
        foreach(string name in new[]{"MatrixTransform","Field","Background","LightDirection","FlowSpeed","Texel","Time","Mode","Specular","Refraction","Reflection","Opacity"})
            if(effect.Parameters[name]==null)throw new InvalidOperationException("水体 Effect 缺少参数："+name);
    }
    public WaterField Field { get; }
    private readonly Texture2D[] textures;
    private bool disposed;
    public static int LiveFieldTextures {get;private set;}
    public WaterRenderer(GraphicsDevice device,int size)
    {
        Field=new(size);
        textures=new[] {new Texture2D(device,size,size,false,SurfaceFormat.Vector4),new Texture2D(device,size,size,false,SurfaceFormat.Vector4)};
        LiveFieldTextures+=2;
    }
    public void Update(double time,WaterSettings settings)
    {
        Field.Generate(time,settings);
        for(int i=0;i<2;i++) textures[i].SetData(Field.Layers[i]);
    }
    // 调用前宿主应结束已有 SpriteBatch；不读取或切换宿主渲染目标。
    public void Draw(SpriteBatch batch,Effect effect,Texture2D background,Rectangle target,Matrix transform,WaterSettings settings,float time,int mode=0,float opacity=1,Vector3? lightDirection=null)
    {
        var viewport=batch.GraphicsDevice.Viewport;
        effect.Parameters["MatrixTransform"].SetValue(transform*Matrix.CreateOrthographicOffCenter(0,viewport.Width,viewport.Height,0,0,1));
        effect.Parameters["Background"].SetValue(background);
        effect.Parameters["LightDirection"].SetValue(lightDirection ?? new Vector3(-0.4f,-0.6f,1));
        effect.Parameters["FlowSpeed"].SetValue(new Vector2(settings.Rotation,settings.Radial));
        effect.Parameters["Texel"].SetValue(new Vector2(1f/Field.Size));
        effect.Parameters["Time"].SetValue(time); effect.Parameters["Mode"].SetValue((float)mode);
        effect.Parameters["Specular"].SetValue(settings.Specular); effect.Parameters["Refraction"].SetValue(settings.Refraction);
        effect.Parameters["Reflection"].SetValue(settings.Reflection); effect.Parameters["Opacity"].SetValue(opacity);
        for(int l=0;l<(settings.Fusion||mode>0?1:2);l++)
        {
            effect.Parameters["Field"].SetValue(textures[l]);
            batch.Begin(SpriteSortMode.Immediate,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullNone,effect);
            batch.Draw(textures[l],target,Color.White); batch.End();
        }
    }
    public void Dispose() { if(disposed)return;foreach(var texture in textures) texture.Dispose();LiveFieldTextures-=2;disposed=true; }
}
