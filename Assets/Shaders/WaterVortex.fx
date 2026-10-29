// 2026-10-27：水带数据调试和显式背景透射；FNA D3D9 Effect。
float4x4 MatrixTransform;
texture Field;
texture Background;
sampler FieldSampler : register(s0) = sampler_state { Texture=<Field>; MinFilter=LINEAR; MagFilter=LINEAR; MipFilter=NONE; AddressU=CLAMP; AddressV=CLAMP; };
sampler BackgroundSampler : register(s1) = sampler_state { Texture=<Background>; MinFilter=LINEAR; MagFilter=LINEAR; MipFilter=NONE; AddressU=CLAMP; AddressV=CLAMP; };
float2 Texel;
float3 LightDirection;
float2 FlowSpeed;
float Time;
float Mode;
float Specular;
float Refraction;
float Reflection;
float Opacity=1;
struct VSIn { float4 Position:POSITION0; float4 Color:COLOR0; float2 UV:TEXCOORD0; };
struct VSOut { float4 Position:POSITION0; float2 UV:TEXCOORD0; float2 ScreenUV:TEXCOORD1; };
VSOut VS(VSIn v) { VSOut o; o.Position=mul(v.Position,MatrixTransform); o.UV=v.UV; o.ScreenUV=o.Position.xy*float2(0.5,-0.5)+0.5; return o; }
float4 PS(VSOut v):COLOR0
{
    float4 f=tex2D(FieldSampler,v.UV);
    float2 grad=float2(tex2D(FieldSampler,v.UV+float2(Texel.x,0)).y-tex2D(FieldSampler,v.UV-float2(Texel.x,0)).y,
        tex2D(FieldSampler,v.UV+float2(0,Texel.y)).y-tex2D(FieldSampler,v.UV-float2(0,Texel.y)).y)/(4*Texel);
    float2 p=v.UV*2-1;
    float r=max(length(p),0.06);
    float2 tangent=float2(-p.y,p.x)/r;
    // 周期角谐波无 UV 累积拉伸；解析导数在局部切向/径向基底转换。
    float theta=atan2(p.y,p.x);
    float wave=cos(theta*18+r*35-Time*(FlowSpeed.x*18-FlowSpeed.y*35))*0.0015;
    grad+=wave*(35*p/r+18*tangent/r)*smoothstep(0.08,0.18,r);
    float3 n=normalize(float3(-grad,1));
    if(Mode>0.5 && Mode<1.5) return float4(f.xxx/2,1);
    if(Mode>1.5 && Mode<2.5) return float4(f.www,1);
    if(Mode>2.5 && Mode<3.5) return float4(f.yyy*0.7+0.5,1);
    if(Mode>3.5 && Mode<4.5) return float4(f.zzz,1);
    if(Mode>4.5) return float4(n*0.5+0.5,1);
    float2 uv=v.ScreenUV;
    float3 bg=pow(tex2D(BackgroundSampler,saturate(uv+n.xy*Refraction*f.z)).rgb,2.2);
    float3 transmit=exp(-float3(2.2,0.65,0.32)*f.z);
    float fresnel=0.02+0.98*pow(1-saturate(n.z),5);
    float3 light=normalize(LightDirection);
    float spec=pow(saturate(dot(n,normalize(light+float3(0,0,1)))),90)*Specular;
    float3 environment=lerp(float3(0.025,0.07,0.1),float3(0.65,0.82,0.95),saturate(n.y*0.5+0.5));
    float3 color=bg*transmit+float3(0.025,0.19,0.23)*(1-transmit);
    color=lerp(color,environment,saturate(fresnel+Reflection*0.2))+spec;
    float alpha=f.w*Opacity;
    return float4(pow(saturate(color),1/2.2)*alpha,alpha);
}
technique Water { pass P { VertexShader=compile vs_3_0 VS(); PixelShader=compile ps_3_0 PS(); } }

