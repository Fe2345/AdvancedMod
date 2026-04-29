using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace AdvancedMod.Utils
{
    public class Specials
    {
        //定义各类特殊数值，如对特定NPC独特设置的特效数值，或者是一些特殊的伤害计算参数等
        public static float DotStacksUIOffset(NPC npc)
        {
            if (npc.type == NPCID.KingSlime)
            {
                return 90f; //史莱姆王
            }
            return 30f; //默认值
        }

        public static Dictionary<string,float> InfiniteCrystalPhaseData()
        {
            //InfiniteCrystal召唤物的成长性数据
            float distance = 1200f; //基础索敌距离
            if (Main.hardMode) distance += 400f; //困难模式
            if (NPC.downedPlantBoss) distance += 400f; //世纪之花
            if (NPC.downedMoonlord) distance += 500f; //月球领主

            float MaxStacks = 30f; //基础最大层数
            if (NPC.downedBoss3) MaxStacks += 20f; //又是这个超囊的boss
            if (Main.hardMode) MaxStacks += 25f; //困难模式
            if (NPC.downedMechBoss1) MaxStacks += 15f;
            if (NPC.downedMechBoss2) MaxStacks += 15f;
            if (NPC.downedMechBoss3) MaxStacks += 15f;
            if (NPC.downedPlantBoss) MaxStacks += 15f;
            if (NPC.downedGolemBoss) MaxStacks += 10f;
            if (NPC.downedTowers) MaxStacks += 20f;
            if (NPC.downedMoonlord) MaxStacks += 20f;
            if (AdvancedWorld.TheEnd) MaxStacks = -1; //击败最终boss后，解除上限


            return new Dictionary<string, float>
            {
                { "Distance", distance },
                { "MaxStacks",MaxStacks },
                { "DamageMultiplier", 1f }, //伤害倍率，后续可以根据阶段增加
                { "HealthMultiplier", 1f } //生命倍率，后续可以根据阶段增加
            };
        }
    }
}
