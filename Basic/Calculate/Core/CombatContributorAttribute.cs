using System;
using System.Reflection;

namespace AdvancedMod.Basic.Calculate.Core;

/// <summary>由内容显式声明贡献者身份；同一项目中重复的 ID 由编译分析器报错。</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CombatContributorAttribute : Attribute
{
    public int Id { get; }

    public CombatContributorAttribute(int id) => Id = id;

    /// <summary>加载登记时读取同一份声明；身份不由实例属性或注册顺序生成。</summary>
    public static int GetId(Type contributorType)
    {
        ArgumentNullException.ThrowIfNull(contributorType);
        var declaration = contributorType.GetCustomAttribute<CombatContributorAttribute>(inherit: false);
        if (declaration is null)
            throw new ArgumentException($"贡献者类型 {contributorType.FullName} 未声明 CombatContributor 特性。", nameof(contributorType));
        return declaration.Id;
    }
}
