using System;
using System.IO;

namespace AdvancedMod.Basic.Element;

public sealed partial class ElementTarget
{
    // 有界协议；先完整校验工作副本，再替换镜像，禁止半包污染状态。
    internal const int MaxWirePortions = 2048;
    public void WriteState(BinaryWriter writer)
    {
        int totalCount = 0;
        foreach (var slot in state.Elements) totalCount += slot.Count;
        if (totalCount > MaxWirePortions) throw new InvalidOperationException("元素分量超过网络协议容量。");
        for (int slotIndex = 0; slotIndex < state.Elements.Length; slotIndex++)
        {
            var slot = state.Elements[slotIndex];
            if (slot.Count > MaxWirePortions) throw new InvalidOperationException("元素分量超过网络协议容量。");
            writer.Write((ushort)slot.Count);
            foreach (var part in slot)
            {
                writer.Write(part.Key); writer.Write(part.Value);
                state.HolyElements[slotIndex].TryGetValue(part.Key, out double holy);
                writer.Write(holy);
            }
        }
        foreach (float cooldown in state.Cooldowns) writer.Write(cooldown);
    }

    public void ReadState(BinaryReader reader)
    {
        if (IsResolving) throw new InvalidOperationException("事务执行中不能覆盖元素镜像。");
        var next = new ElementWorkState();
        int totalCount = 0;
        for (int slot = 0; slot < next.Elements.Length; slot++)
        {
            int count = reader.ReadUInt16();
            totalCount += count;
            if (totalCount > MaxWirePortions) throw new InvalidDataException("元素分量过多。");
            for (int i = 0; i < count; i++)
            {
                int panel = reader.ReadInt32();
                double amount = reader.ReadDouble();
                if (panel < 0 || !double.IsFinite(amount) || amount <= 0 || next.Elements[slot].ContainsKey(panel))
                    throw new InvalidDataException("元素分量非法。");
                double holy = reader.ReadDouble();
                if (panel < 0 || !double.IsFinite(amount) || amount <= 0 || !double.IsFinite(holy) || holy < 0 || holy > amount || next.Elements[slot].ContainsKey(panel))
                    throw new InvalidDataException("元素分量非法。");
                next.Attach(new(slot, amount - holy, panel));
                next.Attach(new(slot, holy, panel, true));
            }
        }
        for (int i = 0; i < next.Cooldowns.Length; i++)
        {
            float cd = reader.ReadSingle();
            if (!float.IsFinite(cd) || cd < 0) throw new InvalidDataException("反应 CD 非法。");
            next.Cooldowns[i] = cd;
        }
        state = next;
    }
}
