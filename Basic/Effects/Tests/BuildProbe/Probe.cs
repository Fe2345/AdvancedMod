// 2026-10-25：由构建符号切换合法菱形与预期编译失败声明。
using AdvancedMod.Basic.Effects;

#if SELF
[IncludesEffect(typeof(A))]
#elif CYCLE
[IncludesEffect(typeof(B))]
#elif BAD_TARGET
[IncludesEffect(typeof(string))]
#elif BAD_WEIGHT
[IncludesEffect(typeof(B), -1)]
#else
[IncludesEffect(typeof(B)), IncludesEffect(typeof(C))]
#endif
public sealed class A : EffectDefinition { }
#if CYCLE
[IncludesEffect(typeof(A))]
#else
[IncludesEffect(typeof(D))]
#endif
public sealed class B : EffectDefinition { }
[IncludesEffect(typeof(D))]
public sealed class C : EffectDefinition { }
public sealed class D : EffectDefinition { }
