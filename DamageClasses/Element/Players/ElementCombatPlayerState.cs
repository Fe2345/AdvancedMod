using System.Collections.Generic;
using System.Linq;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Interfaces;
using Terraria;

namespace AdvancedMod.DamageClasses.Element.Players
{
    /// <summary>
    /// 聚合玩家在一帧内对元素战斗系统提供的全部数值上下文。
    /// </summary>
    public sealed class ElementCombatPlayerState
    {
        /// <summary>
        /// 元素输出侧聚合结果。
        /// </summary>
        public ElementOffenseStatSet Offense { get; } = new();

        /// <summary>
        /// 协同攻击聚合结果。
        /// </summary>
        public CoordinatedAttackStatSet CoordinatedAttacks { get; } = new();

        /// <summary>
        /// 重置本轮聚合结果。
        /// </summary>
        public void Reset()
        {
            Offense.Reset();
            CoordinatedAttacks.Reset();
        }

        /// <summary>
        /// 从玩家当前装备中收集元素数值贡献。
        /// </summary>
        public void CollectFromEquipment(Player player)
        {
            HashSet<object> processedArmorSetProviders = [];

            foreach (Item equippedItem in player.armor)
            {
                if (equippedItem?.IsAir != false)
                {
                    continue;
                }

                if (equippedItem.ModItem is IElementStatProvider statProvider)
                {
                    statProvider.ApplyElementStats(player, this, equippedItem);
                }

                if (equippedItem.ModItem is IElementArmorSetProvider armorSetProvider &&
                    processedArmorSetProviders.Add(armorSetProvider) &&
                    armorSetProvider.IsArmorSetActive(player))
                {
                    armorSetProvider.ApplyArmorSetStats(player, this);
                }
            }
        }

        /// <summary>
        /// 导出当前用于命中快照的战斗上下文。
        /// </summary>
        public CombatContextSnapshot ExportSnapshot()
        {
            ReactionDamageModifierSet globalReactionModifiers = new()
            {
                ReactionTypeMult = 1f,
                BasicMult = 1f,
                MasteryMult = 1f,
                ExtraMult = 1f,
            };

            Dictionary<ElementType, ReactionDamageModifierSet> byElement = Offense.ElementDamageBonus
                .ToDictionary(
                    pair => pair.Key,
                    pair => new ReactionDamageModifierSet
                    {
                        ElementMult = 1f + pair.Value,
                        ReactionTypeMult = 1f,
                        BasicMult = 1f,
                        MasteryMult = 1f,
                        ExtraMult = 1f,
                    });

            return new CombatContextSnapshot
            {
                ElementOffense = Offense.CreateSnapshot(),
                CoordinatedAttacks = CoordinatedAttacks.CreateSnapshot(),
                ReactionStats = new ReactionStatSnapshot
                {
                    GlobalModifiers = globalReactionModifiers,
                    ByElement = byElement,
                    ElementMastery = Offense.ElementMastery,
                    BuildUpAdd = new Dictionary<ElementType, float>(),
                    BuildUpMult = new Dictionary<ElementType, float>(),
                    ConsumeMult = new Dictionary<ReactionType, float>(),
                },
            };
        }
    }
}
