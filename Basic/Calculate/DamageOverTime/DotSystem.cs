#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Calculate.DamageOverTime;

/// <summary>仅替换两个生命恢复方法内的整数跳字，不修改原生扣血分支。</summary>
public sealed class DotSystem : ModSystem
{
    private static readonly HashSet<DotTargetState> tracked = new();
    private ILHook? npcHook;
    private ILHook? playerHook;
    internal static void Track(DotTargetState state) => tracked.Add(state);
    internal static void Forget(DotTargetState state) => tracked.Remove(state);

    public override void Load()
    {
        try
        {
            npcHook = new ILHook(typeof(NPC).GetMethod("UpdateNPC_BuffApplyDOTs", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingMethodException("找不到 NPC 生命恢复入口。"), il => PatchText(il, 2));
            playerHook = new ILHook(typeof(Player).GetMethod(nameof(Player.UpdateLifeRegen))!, il => PatchText(il, 6));
        }
        catch { Unload(); throw; }
    }

    internal static void PatchText(ILContext il, int expectedCalls)
    {
        var cursor = new ILCursor(il);
        int count = 0;
        while (cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<CombatText>(nameof(CombatText.NewText)) &&
            i.Operand is MethodReference method && method.Parameters.Count == 5 &&
            method.Parameters[2].ParameterType.MetadataType == MetadataType.Int32))
        {
            cursor.Remove();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Rectangle, Color, int, bool, bool, Entity, int>>(RouteText);
            count++;
        }
        if (count != expectedCalls)
            throw new InvalidOperationException($"生命恢复跳字入口发生变化：{il.Method.Name} 找到 {count} 处，预期 {expectedCalls} 处，停止加载以避免重复或丢失数字。");
    }

    private static int RouteText(Rectangle bounds, Color color, int damage, bool dramatic, bool dot, Entity target)
    {
        bool managed = target switch
        {
            NPC npc => npc.GetGlobalNPC<DotNpc>().ManagedText,
            Player player => player.GetModPlayer<DotPlayer>().ManagedText,
            _ => false
        };
        if (!dot || damage <= 0 || !managed) return CombatText.NewText(bounds, color, damage, dramatic, dot);
        // 标记只接管显示，镜像仍沿原版更新生命恢复；数字由权威侧同步。
        bool authority = target is NPC ? Main.netMode != NetmodeID.MultiplayerClient
            : Main.netMode != NetmodeID.Server && target.whoAmI == Main.myPlayer;
        if (!authority) return Main.maxCombatText;
        DotTargetState? state = target switch
        {
            NPC npc => npc.GetGlobalNPC<DotNpc>().GetState(npc),
            Player player => player.GetModPlayer<DotPlayer>().State,
            _ => null
        };
        if (!dot || damage <= 0 || state is null)
            return CombatText.NewText(bounds, color, damage, dramatic, dot);
        var allocation = state.Ledger.Allocate(damage);
        foreach (var text in allocation.Separate) state.Text.Add(text);
        state.NativeTextDamage = checked(state.NativeTextDamage + allocation.NativeDamage);
        state.Bounds = bounds;
        state.NeedsLifeSync = true;
        // 没有独立文字也可能消费了 opt-out 来源，仍需在帧末处理同步与清理。
        Track(state);
        return Main.maxCombatText;
    }

    public override void PostUpdateEverything()
    {
        foreach (var state in tracked.ToArray())
        {
            int playerIndex = state.Target is Player player ? player.whoAmI : -1;
            foreach (var text in state.Text.Entries) DotNetwork.Publish(state.Bounds, text, playerIndex);
            if (state.NativeTextDamage > 0) DotNetwork.PublishNative(state.Bounds, state.NativeTextDamage, playerIndex);
            state.Text.Clear();
            state.NativeTextDamage = 0;
            if (state.NeedsLifeSync)
            {
                if (state.Target is NPC npc && Main.netMode == NetmodeID.Server)
                {
                    npc.netUpdate = true;
                    if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs) Main.npc[npc.realLife].netUpdate = true;
                }
                else if (state.Target is Player owner && Main.netMode == NetmodeID.MultiplayerClient && owner.whoAmI == Main.myPlayer)
                    NetMessage.SendData(MessageID.PlayerLifeMana, number: owner.whoAmI);
                state.NeedsLifeSync = false;
            }
            if (!state.IsAlive) state.Reset();
            else if (!state.Ledger.HasBalance) Forget(state);
        }
    }

    private static void Clear()
    {
        foreach (var state in tracked.ToArray()) state.Reset();
        tracked.Clear();
    }
    public override void OnWorldUnload() => Clear();
    public override void OnWorldLoad() => Clear();
    public override void Unload()
    {
        npcHook?.Dispose(); playerHook?.Dispose(); npcHook = playerHook = null;
        Clear();
    }
}
