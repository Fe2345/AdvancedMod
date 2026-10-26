using System.IO;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.Weapons;

public sealed partial class HeavensJudgment
{
    public TagEnum? Resonance { get; private set; }
    internal void SetResonance(TagEnum? element)
    {
        Resonance = HallowActionRules.Decode(HallowActionRules.Encode(element));
        ElementAttacks.ConfigureWeapon(Item, HallowActionRules.Tags(Resonance), Resonance.HasValue ? HallowActionRules.Attachment : 0, "Hallow.HeavensJudgment");
    }
    internal void CommitResonance(Player owner, TagEnum element)
    {
        SetResonance(element);
        if (Main.netMode == NetmodeID.MultiplayerClient && owner.whoAmI == Main.myPlayer)
            NetMessage.SendData(MessageID.SyncEquipment, number: owner.whoAmI, number2: owner.selectedItem);
    }
    public override void SaveData(TagCompound tag) => tag["resonance"] = HallowActionRules.Encode(Resonance);
    public override void LoadData(TagCompound tag) => SetResonance(HallowActionRules.Decode(tag.GetByte("resonance")));
    public override void NetSend(BinaryWriter writer) => writer.Write(HallowActionRules.Encode(Resonance));
    public override void NetReceive(BinaryReader reader) => SetResonance(HallowActionRules.Decode(reader.ReadByte()));
}
