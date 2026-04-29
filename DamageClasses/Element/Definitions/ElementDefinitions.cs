using System;

namespace AdvancedMod.DamageClasses.Element.Definitions
{
    /// <summary>
    /// 当前元素战斗系统支持的元素类型。
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// 无元素。
        /// </summary>
        None = 0,

        /// <summary>
        /// 雷元素。
        /// </summary>
        Thunder = 1,

        /// <summary>
        /// 风元素。
        /// </summary>
        Wind = 2,

        /// <summary>
        /// 火元素。
        /// </summary>
        Pyro = 3,
    }

    /// <summary>
    /// 描述单个元素输入条目的附着行为标记。
    /// </summary>
    [Flags]
    public enum ElementApplicationFlags
    {
        /// <summary>
        /// 无特殊标记。
        /// </summary>
        None = 0,
    }
}
