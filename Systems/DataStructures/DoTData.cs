using AdvancedMod.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities.Terraria.Utilities;

namespace AdvancedMod.Systems.DataStructures
{
    //DoT类数据结构
    public class DoTData
    {
        /// <summary>
        /// Determines the behavior of the debuff.
        /// </summary>
        public enum DoTBehavior
        {
            Default, //默认，被动结算并不可暴击
            Crit, //可暴击
            Activity //可暴击，但是只可被主动结算
        }

        public enum DoTType
        {
            Pyro, //火系，主打高倍率单攻
            Hydro, //水系，主打治疗
            Anemo, //风系，主打提供减抗
            Electro, //雷系，主打AOE
            Cryo, //冰系，主打百分比伤害
            Default //回退值，子类不应使用，仅作为未指定类型时的默认值
        }
        //一般结算：基础伤害+(1+易伤)*(倍率*(1-抗性)*攻击力*f(层数)-防御)

        #region DoT Data
        //结算方式
        public virtual DoTBehavior Behavior { get; set; } = DoTBehavior.Default;
        // IMPORTANT:由于需要绘制层数特效，所有子类必须显示指定自身所属类型，防止层数混淆
        public virtual DoTType Type { get; set; } = DoTType.Default;
        //基础攻击力
        public virtual float BasicDMG { get; set; } = 0f;
        //DoT攻击力，取决于武器
        public virtual float Attack { get; set; } = 0f;
        //DoT倍率，取决于武器
        public virtual float Multiplier { get; set; } = 1f;
        //防御力
        public virtual float Defense { get; set; } = 0f;
        //抗性
        public virtual float Resistance { get; set; } = 0f;
        //易伤（受到伤害提高）
        public virtual float Vulnerability { get; set; } = 0f;
        //层数
        public virtual int Stacks { get; set; } = 1;
        //最大层数
        public virtual int MaxStack { get; set; } = 1;
        //触发结算还需要的游戏刻
        public virtual int DamageColdDown { get; set; } = 0;
        //结算间隔
        public virtual int TickRate { get; set; } = 60;
        #endregion

        public DoTData()
        {
            //PlayerUpdateMethod = DefaultUpdateOnPlayer;
            //NPCLifeRegenMethod = BaseUpdateNPCLifeRegen;
        }
        /// <summary>
        /// Allows using DebuffBehavior to determine preset behavior
        /// "electric" causes debuffs to scale 4x when moving
        /// Uses default behavior if no known key is inputed
        /// </summary>
        /// <param name="behavior">This determines the type of behavior of the debuff</param>
        public DoTData(DoTBehavior behavior, float basicDMG, float attack, float multiplier,NPC npc, int stacks, int maxStacks)
        {
            Behavior = behavior;
            BasicDMG = basicDMG;
            Attack = attack;
            Multiplier = multiplier;
            Defense = npc.defense;
            Resistance = npc.Advanced().DotResistance;
            Vulnerability = npc.Advanced().Vulnerability;
            Stacks = stacks;
            MaxStack = maxStacks;
        }

        /// <summary>
        /// UNIMPLEMENTED. WILL BE IN A FUTURE PR
        /// The default debuff DoT functionality on a player
        /// </summary>
        public void DefaultUpdateOnPlayer(Player player, int buffType, ref int buffIndex, ref int damage)
        {

        }

        // 生命周期钩子一：第一次施加时
        public virtual void OnApply(NPC npc) { }

        // 生命周期钩子二：叠层时
        public virtual void AddStack(NPC npc, DoTData incomingData)
        {
            //叠层机制：均分攻击力和倍率；如果新data的叠层上限更高，则解禁上限；否则，只会视是否攻击力/倍率更高来更新倍率
            if (incomingData == null) return;
            if (incomingData.MaxStack > MaxStack)
            {
                MaxStack = incomingData.MaxStack;
            }
            if (Stacks < MaxStack)
            {
                // 将新攻击力和倍率纳入平均值
                Attack = (Stacks * Attack + incomingData.Attack) / (Stacks + 1);
                Multiplier = (Stacks * Multiplier + incomingData.Multiplier) / (Stacks + 1);

                Stacks++;
                OnStackIncrease(npc);

                if (Stacks == MaxStack)
                {
                    OnMaxStacks(npc);
                }
            }
            else 
            {
                if (incomingData.Attack > Attack)// 已经到达上限，如果新攻击力更高，则替换其中一份旧的攻击力和倍率占比
                { 
                    Attack = ((Stacks - 1) * Attack + incomingData.Attack) / Math.Max(1, Stacks);
                } 
                if (incomingData.Multiplier > Multiplier)
                {
                    Multiplier = ((Stacks - 1) * Multiplier + incomingData.Multiplier) / Math.Max(1, Stacks);
                }
                
            }
        }

        // 生命周期钩子三：触发特定行为
        public virtual void OnStackIncrease(NPC npc) { }

        // 生命周期钩子四：达到满层时触发
        public virtual void OnMaxStacks(NPC npc) { }

        // 生命周期钩子五：每帧常驻效果更新（通常用于提供基于层数的Debuff效果）
        public virtual void UpdateEffect(NPC npc) { }

        /// <summary>
        /// DoT对NPC的结算效果
        /// </summary>
        public virtual void UpdateTick(NPC npc, ref int damageOut)
        {
            var modNpc = npc.Advanced();
            float rawDamage = BasicDMG + (1 + Vulnerability) * (Multiplier * (1 - Resistance) * Attack * Stacks - Defense);

            if (rawDamage < 1f) rawDamage = 1f;

            //对应的每秒伤害 (DPS) 为：
            float dps = rawDamage * (60f / TickRate);

            // 在 Terraria 中，lifeRegen 每减小2代表每秒流失1生命值
            int regenPenalty = (int)Math.Round(dps * 2f);
            if (regenPenalty < 2) regenPenalty = 2; // 至少保证有1点DPS

            modNpc.ApplyDotDamage(npc, ref regenPenalty, ref npc.lifeRegen);

            // 为了能够让 Terraria 弹出与之对应的伤害数字
            int displayDamage = regenPenalty / 2;
            if (damageOut < displayDamage)
            {
                damageOut = displayDamage;
            }
        }
    }
}