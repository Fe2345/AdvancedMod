using AdvancedMod.Systems.DataStructures;
using AdvancedMod.Utils;
using Terraria;

namespace AdvancedMod.Buffs.DoT.Data
{
    public class WeatheringDoTData : DoTData
    {
        public override DoTType Type { get; set; } = DoTType.Anemo;  
        public override float BasicDMG { get; set; } = 12f;
        public override int TickRate { get; set; } = 40;

        public override void UpdateEffect(NPC npc)
        {
            var advancedNPC = npc.Advanced();

            // 根据层数计算减抗效果
            if (Stacks > 40)
            {
                advancedNPC.DotResistance -= 0.15f;
            }
            else if (Stacks >= 20)
            {
                advancedNPC.DotResistance -= 0.10f;
            }
            else
            {
                advancedNPC.DotResistance -= 0.05f;
            }
        }
    }
}
