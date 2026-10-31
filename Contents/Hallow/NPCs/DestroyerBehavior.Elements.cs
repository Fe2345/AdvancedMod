using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed partial class DestroyerBehavior
{
    public ProbeElement SegmentElement { get; internal set; }
    // 探测怪账本完成初始化且归零后允许进入二阶段。
    public static bool IsPhaseTwoReady(NPC head)
    {
        var state = head.GetGlobalNPC<DestroyerBarState>();
        return state.Maximum > 0 && state.Remaining == 0;
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        base.OnSpawn(npc, source);
        if (Hallow && npc.type == NPCID.TheDestroyerBody && Main.netMode != NetmodeID.MultiplayerClient)
            SegmentElement = (ProbeElement)Main.rand.Next(1, DestroyerPhaseOneRules.ElementCount + 1);
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (!Hallow) return;
        int root = npc.type == NPCID.TheDestroyer ? npc.whoAmI : npc.realLife;
        if (root < 0 || root >= Main.maxNPCs || !Main.npc[root].active || Main.npc[root].type != NPCID.TheDestroyer) return;
        var state = Main.npc[root].GetGlobalNPC<DestroyerBarState>();
        modifiers.FinalDamage *= Math.Clamp(1f - state.Remaining * DestroyerPhaseOneRules.ReductionPerProbe, 0f, 1f);
    }

    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer)
    { base.SendExtraAI(npc, bits, writer); writer.Write((byte)SegmentElement); if (npc.type == NPCID.TheDestroyer) WritePhaseTwo(writer); }

    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader)
    { base.ReceiveExtraAI(npc, bits, reader); SegmentElement = DestroyerPhaseOneRules.Decode(reader.ReadByte()); if (npc.type == NPCID.TheDestroyer) ReadPhaseTwo(reader); }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        var head = HeadBehavior(npc);
        if (head?.Hidden == true) return false;
        if (!Hallow || npc.type != NPCID.TheDestroyerBody || npc.ai[2] != 0 && head?.PhaseTwo != true || SegmentElement == ProbeElement.None) return true;
        var original = npc.frame;
        if (head?.PhaseTwo == true) npc.frame.Y = 0;
        DestroyerElementDrawing.Draw(npc, SegmentElement, spriteBatch, screenPos, drawColor);
        npc.frame = original;
        return false;
    }
}
