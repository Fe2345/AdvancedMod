using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>读取引擎同一次扣防御运算，不额外抽取随机数，也不改变原伤害。</summary>
public sealed class NpcDamageObservation : ModSystem
{
    internal sealed class Sample
    {
        internal float BeforeMinimum = float.NaN;
    }
    internal static Sample Pending;
    private ILHook hook;
    public override void Load() => hook = new ILHook(typeof(NPC.HitModifiers).GetMethod(nameof(NPC.HitModifiers.GetDamage)), il =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, i => i.OpCode == OpCodes.Sub,
            i => i.MatchLdcR4(1f), i => i.MatchCall(typeof(Math), nameof(Math.Max))))
            throw new InvalidOperationException("无法定位 NPC 保底前扣防御运算，拒绝使用不可靠的导电终止判定。");
        cursor.Remove();
        cursor.EmitDelegate<Func<float, float, float>>((damage, defense) =>
        {
            float remaining = damage - defense;
            if (Pending is { } sample) sample.BeforeMinimum = remaining;
            return remaining;
        });
    });
    public override void Unload() { hook?.Dispose(); hook = null; Pending = null; }
}
