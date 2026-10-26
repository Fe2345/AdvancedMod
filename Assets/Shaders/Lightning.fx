// 复现原版顶点带的细亮芯与柔光；几何、推进波和寿命衰减由顶点数据提供。
float4x4 uWorldViewProjection;
struct VertexInput { float4 Position : POSITION0; float4 Color : COLOR0; float2 UV : TEXCOORD0; };
struct VertexOutput { float4 Position : POSITION0; float4 Color : COLOR0; float2 UV : TEXCOORD0; };
VertexOutput VertexMain(VertexInput input)
{
    VertexOutput output;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.UV = input.UV;
    return output;
}
float4 PixelMain(VertexOutput input) : COLOR0
{
    float edge = saturate(1 - abs(input.UV.y * 2 - 1));
    float glow = edge * edge * edge;
    float core = pow(edge, 16);
    // 顶点RGB已预乘；白芯使用相同透明度，加色混合保持黑背景透明。
    float3 light = input.Color.rgb * glow * 0.65 + input.Color.a * core;
    return float4(light, 0);
}
technique Lightning
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexMain();
        PixelShader = compile ps_2_0 PixelMain();
    }
}
