// 2026-10-25：加载时注册当前模组的所有效果，缓存定义与事件订阅者。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Effects;

public sealed class EffectSystem : ModSystem
{
    internal static EffectGraph Graph { get; private set; }
    internal static IReadOnlyDictionary<Type, AccessoryEffect> Definitions { get; private set; }
    internal static (Type Type, IEffectOnHitNPC Handler)[] HitHandlers { get; private set; }

    public override void Load()
    {
        var all = Mod.Code.GetTypes();
        foreach (var type in all)
        {
            bool declared = type.GetCustomAttributes<IncludesEffectAttribute>(false).Any();
            bool concrete = !type.IsAbstract && typeof(EffectDefinition).IsAssignableFrom(type);
            if ((declared || concrete) && (!EffectGraph.IsValid(type) || !typeof(AccessoryEffect).IsAssignableFrom(type)))
                throw new InvalidOperationException($"模组效果必须为具有 public 无参构造函数的非泛型具体 AccessoryEffect：{type}");
        }
        var types = all.Where(t => !t.IsAbstract && typeof(AccessoryEffect).IsAssignableFrom(t)).ToArray();
        Graph = new EffectGraph(types);
        Definitions = types.ToDictionary(t => t, t => (AccessoryEffect)Activator.CreateInstance(t));
        HitHandlers = Definitions.Where(p => p.Value is IEffectOnHitNPC)
            .Select(p => (p.Key, (IEffectOnHitNPC)p.Value)).ToArray();
    }

    public override void Unload()
    {
        Graph = null;
        Definitions = null;
        HitHandlers = null;
    }
}
