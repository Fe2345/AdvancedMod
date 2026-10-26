# DDmod刀光字节码复原与天裁接入

## 依据

- 只读参考：`../../ModReader/DDmod/Effects/刀光.xnb`，4084字节，SHA256 `E2D7E43E6468DA94CEDEBD66B3D5869E1BE01EF8E63323E99947E6555469FA25`。
- 头部`XNBw 05 00`，Windows XNB v5，未压缩；EffectReader，内容偏移0xA8，16字节XNA Effect封套后为D3D9 Effect。无需解密。
- 像素程序偏移0x7EC：[ps_2_0反汇编](ddmod-shader-bytecode/07ec.asm)；顶点程序偏移0xE98：[vs_2_0反汇编](ddmod-shader-bytecode/0e98.asm)。原编译器版本9.26.952.2844。
- 不能恢复原作者的源文件组织、注释与变量命名；本次恢复的是优化后程序的数学行为。

## 复原公式

原版UV记作`(x,y)`，x=0在新轨迹，x=1在旧轨迹。若flipped则`y=1-y`。

```text
S(t) = q*q*(3-2*q), q=saturate(t*3.00029993)
C = lerp(FlameColor, uSecondaryColor, S(x))
C = lerp(C, uColor, S(x-0.333299994))
sampleUV = (0.5+0.5*sin((0.5*x-0.45*uTime)*5.4), 3*y)
w = texture(uImage1, sampleUV).r
I = w * pow(saturate(1-x-y*w*uWidth), uAlpha)
RGBA = (C*I, I*uDarkshade)
```

证据对应：两组`mul_sat/mad/mul`构造三次平滑函数；预着色器计算`uTime*0.45`和颜色差；`frc/sincos`负责角度周期归约与正弦；`texld`只用红通道；`mad_sat/pow`生成亮度侵蚀；最后RGB与Alpha分别乘强度和uDarkshade。顶点着色器仅作矩阵变换并传UV，像素阶段不使用顶点色。

`uDarkshade=0.2`不是“整层颜色乘0.2”：RGB保持发光强度，Alpha只保留其20%。因此需One／InverseSourceAlpha混合；改用非预乘混合会再次乘Alpha，明显压暗。泰拉刃调用链将同一网格画两遍：白纹理底层与Wave纹理层。

## 本项目适配

- 源码及产物：[HallowSlash.fx](../../Assets/Shaders/HallowSlash.fx)、[HallowSlash.xnb](../../Assets/Shaders/HallowSlash.xnb)。先写FX，再用DirectX 43编译SM2；原始DDmod XNB不作为运行依赖。
- 当前网格新轨迹U=1、外缘V=1，顶点阶段使用`1-UV`；保持192半径、96头宽、历史长度、攻速和命中逻辑。
- 未共鸣沿用原蓝→绿三色；共鸣按Item元素提供三色，保留亮端与暗尾。新增整体退场uOpacity，防止原版忽略顶点色导致残影突然消失。
- 原Wave图片改为固定种子的周期多层值噪声，CPU仅生成一次缓存纹理，时间变化在GPU进行。这一素材替代会影响细节，故不声称与原截图逐像素相同。
- 原固定平行亮纹／重复加法辉光改为白底＋云纹双层；保留独立十字星芒。绘制完成恢复纹理槽0/1、采样器、混合、深度、光栅与顶点索引状态。
- 不涉及环境投影阴影、屏幕后处理Bloom或新增伤害；所恢复的是原刀光材质内部的明暗与发光输出。

## 验证

- 构建0警告0错误；离线32/32通过，包含周期云纹接缝与192/96几何边界。
- GPU比较：同一128×64展开网格、同一白／云纹纹理，在时间0、0.37、2.1对比原Effect与重建Effect；六组RGBA最大字节误差均为0。测试额外检查有非零像素及RGB高于Alpha的发光像素，避免空白误通过。
- 实机四段连招15/15通过，1倍／2倍攻速八张截图已检查；共鸣／格挡动作120/120通过，四色剑气截图已检查。
- 证据：[GPU对照](hallow-shader-2026-10-26/gpu-comparison.json)、[连招报告](hallow-shader-2026-10-26/report.json)、[动作报告](hallow-shader-2026-10-26/actions-report.json)、[离线报告](2026-10-26-hallow-shader-offline.json)。
- 对照程序：`HallowShaderLiveTests`；仅显式设置`ADVANCEDMOD_HALLOW_SHADER_REFERENCE`为本机原XNB路径时运行，不在正常游戏加载参考文件。
- 未验证OpenGL／Vulkan后端、联机视觉同步与其他光照模组兼容；本轮客户端为FNA D3D11。

复现GPU对照：设置上述环境变量后运行`Tests/Run-HallowLive.ps1`或`Tests/Run-HallowActionsLive.ps1`。报告位于隔离存档的`HallowShaderComparison.json`；连招／动作报告由各自脚本校验。
