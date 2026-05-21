using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// 保存玩家在元素输出侧的基础数值聚合结果。
    /// </summary>
    public sealed class ElementOffenseStatSet
    {
        private readonly Dictionary<ElementType, float> elementDamageBonus = [];

        /// <summary>
        /// 元素精通总值。
        /// </summary>
        public int ElementMastery { get; private set; }

        /// <summary>
        /// 按元素区分的额外伤害加成。
        /// </summary>
        public IReadOnlyDictionary<ElementType, float> ElementDamageBonus => elementDamageBonus;

        /// <summary>
        /// 重置本轮聚合结果。
        /// </summary>
        public void Reset()
        {
            ElementMastery = 0;
            elementDamageBonus.Clear();
        }

        /// <summary>
        /// 增加指定元素伤害加成。
        /// </summary>
        public void AddElementDamageBonus(ElementType element, float value)
        {
            if (element == ElementType.None)
            {
                return;
            }

            if (!elementDamageBonus.TryAdd(element, value))
            {
                elementDamageBonus[element] += value;
            }
        }

        /// <summary>
        /// 增加元素精通。
        /// </summary>
        public void AddElementMastery(int value)
        {
            ElementMastery += value;
        }

        /// <summary>
        /// 创建一个用于快照的只读副本。
        /// </summary>
        public ElementOffenseStatSet CreateSnapshot()
        {
            ElementOffenseStatSet snapshot = new();
            snapshot.ElementMastery = ElementMastery;

            foreach (KeyValuePair<ElementType, float> pair in elementDamageBonus)
            {
                snapshot.elementDamageBonus[pair.Key] = pair.Value;
            }

            return snapshot;
        }
    }
}
