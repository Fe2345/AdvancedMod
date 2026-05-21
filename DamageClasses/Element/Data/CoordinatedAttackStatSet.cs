using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// 保存协同攻击相关的聚合配置。
    /// </summary>
    public sealed class CoordinatedAttackStatSet
    {
        /// <summary>
        /// 描述某个元素协同攻击通道的配置。
        /// </summary>
        public sealed class Entry
        {
            /// <summary>
            /// 协同攻击对应的元素。
            /// </summary>
            public ElementType Element { get; init; } = ElementType.None;

            /// <summary>
            /// 协同攻击命中的基础伤害。
            /// </summary>
            public int Damage { get; init; }

            /// <summary>
            /// 协同攻击附着量。
            /// </summary>
            public int BuildUp { get; init; }

            /// <summary>
            /// 协同攻击发射的弹幕类型。
            /// </summary>
            public int ProjectileType { get; init; }

            /// <summary>
            /// 协同攻击的弹速倍率。
            /// </summary>
            public float SpeedMultiplier { get; init; } = 1f;
        }

        private readonly Dictionary<ElementType, Entry> entries = [];

        /// <summary>
        /// 当前已注册的协同攻击通道。
        /// </summary>
        public IReadOnlyDictionary<ElementType, Entry> Entries => entries;

        /// <summary>
        /// 重置本轮聚合结果。
        /// </summary>
        public void Reset()
        {
            entries.Clear();
        }

        /// <summary>
        /// 设置指定元素的协同攻击通道。
        /// </summary>
        public void SetEntry(ElementType element, int damage, int buildUp, int projectileType, float speedMultiplier = 1f)
        {
            if (element == ElementType.None)
            {
                return;
            }

            entries[element] = new Entry
            {
                Element = element,
                Damage = damage,
                BuildUp = buildUp,
                ProjectileType = projectileType,
                SpeedMultiplier = speedMultiplier,
            };
        }

        /// <summary>
        /// 创建一个用于快照的只读副本。
        /// </summary>
        public CoordinatedAttackStatSet CreateSnapshot()
        {
            CoordinatedAttackStatSet snapshot = new();

            foreach (KeyValuePair<ElementType, Entry> pair in entries)
            {
                snapshot.entries[pair.Key] = new Entry
                {
                    Element = pair.Value.Element,
                    Damage = pair.Value.Damage,
                    BuildUp = pair.Value.BuildUp,
                    ProjectileType = pair.Value.ProjectileType,
                    SpeedMultiplier = pair.Value.SpeedMultiplier,
                };
            }

            return snapshot;
        }
    }
}
