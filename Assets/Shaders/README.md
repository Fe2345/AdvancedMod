# 着色器源码与构建

`HallowSlash.fx` 是DDmod刀光字节码的逻辑重建，`HallowSlash.xnb` 是由本目录FX编译得到的运行资源。XNB是容器，FX是源码，两者不是互斥的着色器类型。

修改FX后，在Windows、Python 3、已安装DirectX June 2010的环境执行：

```powershell
pwsh -File Tools/Compile-Shaders.ps1 -Python python
dotnet build --no-restore
```

`-Python`可传入Python可执行文件绝对路径。脚本使用本机`d3dcompiler_43.dll`的`D3DCompile(fx_2_0)`，内部编译`vs_2_0`／`ps_2_0`；添加XNA Effect平台封套和未压缩XNB头。不能用DXBC的fx_5_0或MonoGame MGFX文件直接替换。

普通C#构建使用已提交的XNB，不要求玩家安装编译器，也不会自动重编FX。修改源码后必须先执行上面的编译步骤。

## 参数

- `uWorldViewProjection`：世界坐标→屏幕→当前缩放→正交投影。
- `FlameColor`／`uSecondaryColor`／`uColor`：由新轨迹向尾端过渡的三色。
- `uTime`：当前游戏的全局时间。
- `uWidth=1`、`uAlpha=1`、`uDarkshade=0.2`：保持参考材质数值；其中`uDarkshade`仅控制输出Alpha。
- `uOpacity`：本项目新增的整体退场因子，同时缩放RGB与Alpha。
- `uImage1`：纹理槽1。绘制队列依次使用白纹理与程序化周期云纹，使用预乘Alpha混合的One／InverseSourceAlpha系数保留发光RGB。

顶点阶段将当前网格UV转换为原版使用的方向，96宽度和历史轨迹仍由CPU几何控制。原版`flipped`的内外缘选择已由固定几何UV方向取代，不因顺逆挥刀交换内外高光。

复原依据、公式与GPU对照见[复原记录](../../docs/testing/2026-10-26-hallow-shader.md)。
