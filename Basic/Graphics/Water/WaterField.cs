// 2026-10-27：宿主无关的有限支撑连续水带场；同带取最大值，同层求和。
using System;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Basic.Graphics.Water;

public sealed class WaterSettings
{
    public bool Fusion { get; set; } = true;
    public int Seed { get; set; } = 17;
    public float Rotation { get; set; } = 0.65f;
    public float Radial { get; set; } = 0.15f;
    public float Width { get; set; } = 0.15f;
    public float Branch { get; set; } = 0.12f;
    public float Threshold { get; set; } = 0.45f;
    public float Depth { get; set; } = 0.45f;
    public float Thickness { get; set; } = 0.6f;
    public float Specular { get; set; } = 0.65f;
    public float Refraction { get; set; } = 0.035f;
    public float Reflection { get; set; } = 0.25f;
}

public sealed class WaterField
{
    public int Size { get; }
    // X 原始场，Y 表面高度，Z 视线厚度，W 遮罩。数据保持线性浮点。
    public Vector4[][] Layers { get; }
    private readonly float[] band;
    private const int Segments = 120;
    public WaterField(int size)
    {
        if (size != 128 && size != 256 && size != 512) throw new ArgumentOutOfRangeException(nameof(size));
        Size = size; Layers = new[] { new Vector4[size * size], new Vector4[size * size] }; band = new float[size * size];
    }
    public static float Smooth(float a, float b, float x) { float v = Math.Clamp((x-a)/(b-a),0,1); return v*v*(3-2*v); }
    public static float Kernel(float q) { float v = Math.Max(0, 1-q*q); return v*v*v; }
    public void Generate(double seconds, WaterSettings p)
    {
        float t = (float)seconds;
        foreach (var layer in Layers) Array.Clear(layer);
        int count = p.Fusion ? 2 : 8;
        for (int b=0;b<count;b++)
        {
            Array.Clear(band);
            int layer = p.Fusion || b<6 ? 0 : 1;
            Vector2 previous = Point(0,b,t,p,out float previousWidth);
            for(int s=1;s<=Segments;s++)
            {
                Vector2 next = Point((float)s/Segments,b,t,p,out float width);
                Raster(previous,next,previousWidth,width);
                previous=next; previousWidth=width;
            }
            for(int i=0;i<band.Length;i++) Layers[layer][i].X += band[i];
        }
        for(int l=0;l<2;l++) for(int y=0;y<Size;y++) for(int x=0;x<Size;x++)
        {
            int i=y*Size+x; float f=Layers[l][i].X;
            float nx=(x+0.5f)/Size*2-1, ny=(y+0.5f)/Size*2-1, r=MathF.Sqrt(nx*nx+ny*ny);
            float mask=Smooth(p.Threshold-0.035f,p.Threshold+0.035f,f);
            float round= MathF.Sqrt(Math.Max(0,f-p.Threshold));
            float h=round*0.15f + (p.Fusion?0:p.Depth*(r*r-1)+l*0.22f);
            float thick=p.Thickness*round*(0.8f+0.2f*MathF.Sin(r*12-t));
            Layers[l][i]=new(f,h,thick,mask);
        }
    }
    private static Vector2 Point(float u,int b,float t,WaterSettings p,out float width)
    {
        width=Math.Max(0.015f,p.Width)*(0.8f+0.2f*MathF.Sin(u*17-t*1.3f+b));
        if(p.Fusion)
        {
            float gap=0.018f+0.27f*(0.5f+0.5f*MathF.Cos(t*0.65f));
            float waist; // 宽度变化由连续局部包络控制。
            waist=MathF.Exp(-MathF.Pow((u-0.5f)*5,2));
            width*=1-0.48f*waist*(0.5f+0.5f*MathF.Sin(t*0.65f));
            return new((u-0.5f)*1.65f,(b==0?-1:1)*(gap+0.2f*MathF.Pow((u-0.5f)*2,2))+0.025f*MathF.Sin(u*12+t));
        }
        int arm=b%3;
        float r=0.12f+u*0.73f;
        float angle=arm*MathHelper.TwoPi/3+u*8.2f+t*p.Rotation+0.5f*MathF.Sin(t*p.Rotation-u*3)+0.08f*MathF.Sin(t*p.Radial+u*14+p.Seed);
        float branch=p.Branch*MathF.Sin(u*MathHelper.Pi)*MathF.Sin(t*0.7f+u*9+arm);
        if(b>=3) { angle+=branch; r+=0.025f; width*=0.48f; }
        if(b>=6) { angle+=0.7f; width*=0.7f; }
        width*=0.35f+0.65f*u;
        r+=0.018f*MathF.Sin(u*24+t*p.Radial*5+arm);
        return new(r*MathF.Cos(angle),r*MathF.Sin(angle));
    }
    private void Raster(Vector2 a,Vector2 b,float wa,float wb)
    {
        float w=Math.Max(wa,wb), scale=Size/2f;
        int x0=Math.Clamp((int)((Math.Min(a.X,b.X)-w+1)*scale),0,Size-1), x1=Math.Clamp((int)((Math.Max(a.X,b.X)+w+1)*scale)+1,0,Size-1);
        int y0=Math.Clamp((int)((Math.Min(a.Y,b.Y)-w+1)*scale),0,Size-1), y1=Math.Clamp((int)((Math.Max(a.Y,b.Y)+w+1)*scale)+1,0,Size-1);
        Vector2 d=b-a; float len=Math.Max(d.LengthSquared(),1e-12f);
        for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++)
        {
            Vector2 v=new((x+0.5f)/scale-1,(y+0.5f)/scale-1);
            float s=Math.Clamp(Vector2.Dot(v-a,d)/len,0,1);
            float q2=Vector2.DistanceSquared(v,a+d*s)/MathF.Pow(wa+(wb-wa)*s,2);
            if(q2>=1) continue;
            float k=1-q2; k=k*k*k; int i=y*Size+x;
            band[i]=Math.Max(band[i],k);
        }
    }
}

