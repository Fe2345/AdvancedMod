using System;
using AdvancedMod.Basic.Calculate.Core;
using Identity = AdvancedMod.Basic.Calculate.Core.CombatContributorAttribute;

if (CombatContributorAttribute.GetId(typeof(First)) != TestIds.First ||
    CombatContributorAttribute.GetId(typeof(Second)) != TestIds.Second)
    throw new Exception("运行时读取的 ID 与特性声明不一致。");

try
{
    CombatContributorAttribute.GetId(typeof(UndeclaredChild));
    throw new Exception("未声明特性的子类不应继承父类 ID。");
}
catch (ArgumentException) { }
Console.WriteLine("通过：唯一 ID 构建、特性读取、非继承身份。");

internal static class TestIds
{
    public const int First = 1001;
#if DUPLICATE_IDS
    public const int Second = First;
#else
    public const int Second = First + 1;
#endif
}

[CombatContributor(TestIds.First)]
internal partial class First { }

// 分部声明不应被误判为两个贡献者。
internal partial class First { }

// 别名及常量表达式必须按语义解析，不能靠正则查重。
[Identity(TestIds.Second)]
internal sealed class Second { }

internal sealed class UndeclaredChild : First { }

#if NON_CONSTANT_ID
[CombatContributor(Environment.TickCount)]
internal sealed class DynamicIdentity { }
#endif

// 同名但来自其他命名空间的特性不能被误判。
namespace Unrelated
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class CombatContributorAttribute : Attribute
    {
        public CombatContributorAttribute(int id) { }
    }

    [CombatContributor(TestIds.First)]
    internal sealed class Other { }
}
