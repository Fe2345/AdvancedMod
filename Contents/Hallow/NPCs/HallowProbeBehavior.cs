using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>探测怪保留完整原版 AI；元素仅改变外观和原生射击生成的弹幕。</summary>
public sealed class HallowProbeBehavior : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.Probe;
    public ProbeElement Element { get; private set; }
    public override void SetDefaults(NPC npc) => Element = ProbeElement.None;
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (source is EntitySource_Parent { Entity: NPC parent } && parent.type == NPCID.TheDestroyerBody &&
            parent.TryGetGlobalNPC<DestroyerBehavior>(out var behavior) && behavior.Hallow)
            Element = behavior.SegmentElement;
    }
    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer) => writer.Write((byte)Element);
    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader) => Element = DestroyerPhaseOneRules.Decode(reader.ReadByte());
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (Element == ProbeElement.None) return true;
        DestroyerElementDrawing.Draw(npc, Element, spriteBatch, screenPos, drawColor);
        return false;
    }
}
