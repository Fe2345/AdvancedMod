// Reference:DDmod的刀光.xnb
float4x4 uWorldViewProjection;
float3 uColor;
float3 uSecondaryColor;
float3 FlameColor;
float uTime;
float uWidth = 1;
float uAlpha = 1;
float uDarkshade = 0.2;
float uOpacity = 1;
texture uImage1;
sampler2D WaveSampler : register(s1) = sampler_state
{
    Texture = <uImage1>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Wrap;
    AddressV = Wrap;
};
struct VertexInput { float4 Position : POSITION0; float4 Color : COLOR0; float2 UV : TEXCOORD0; };
struct VertexOutput { float4 Position : POSITION0; float2 UV : TEXCOORD0; };
VertexOutput VertexMain(VertexInput input)
{
    VertexOutput output;
    output.Position = mul(input.Position, uWorldViewProjection);
    // 现有网格U=1为剑尖所在的新轨迹；原版U=0为新轨迹。
    // 固定外缘V=1，不随旋向翻转，避免反挥时内外高光交换。
    output.UV = 1 - input.UV;
    return output;
}
float SmoothThird(float x)
{
    float t = saturate(x * 3.00029993);
    return t * t * (3 - 2 * t);
}
float4 PixelMain(VertexOutput input) : COLOR0
{
    float2 uv = input.UV;
    float3 color = lerp(FlameColor, uSecondaryColor, SmoothThird(uv.x));
    color = lerp(color, uColor, SmoothThird(uv.x - 0.333299994));
    // 字节码先将角度折回[-pi,pi]再sincos；HLSL编译器负责相同的SM2展开。
    float2 waveUV = float2(0.5 + 0.5 * sin((uv.x * 0.5 - uTime * 0.45) * 5.4), uv.y * 3);
    float wave = tex2D(WaveSampler, waveUV).r;
    float intensity = wave * pow(saturate(1 - uv.x - uv.y * wave * uWidth), uAlpha);
    // 原版忽略顶点色；RGB独立于低Alpha形成发光。仅额外乘当前特效的整体退场透明度。
    return float4(color * intensity, intensity * uDarkshade) * uOpacity;
}
technique HallowSlash
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexMain();
        PixelShader = compile ps_2_0 PixelMain();
    }
}
