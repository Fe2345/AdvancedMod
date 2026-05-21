namespace AdvancedMod.DamageClasses.Element.Definitions
{
    /// <summary>
    /// 当前元素战斗系统识别的反应类型。
    /// </summary>
    public enum ReactionType
    {
        /// <summary>
        /// 无反应。
        /// </summary>
        None = 0,

        /// <summary>
        /// 雷风原型反应。
        /// </summary>
        ThunderWind = 1,

        /// <summary>
        /// 超载。
        /// </summary>
        Overloaded = 2,

        /// <summary>
        /// 感电。
        /// </summary>
        ElectroCharged = 3,

        /// <summary>
        /// 蒸发。
        /// </summary>
        Vaporize = 4,
    }

    /// <summary>
    /// 定义反应在数值语义上的本质分类。
    /// </summary>
    public enum ReactionSemanticKind
    {
        /// <summary>
        /// 未指定语义。
        /// </summary>
        None = 0,

        /// <summary>
        /// 仅用于过渡期原型逻辑的原型过渡反应。
        /// </summary>
        Prototype = 1,

        /// <summary>
        /// 直接修饰原始命中伤害的增幅反应。
        /// </summary>
        Amplification = 2,

        /// <summary>
        /// 生成独立反应伤害与附加效果的剧变反应。
        /// </summary>
        Transformative = 3,
    }

    /// <summary>
    /// 定义反应如何从触发侧或目标侧消耗元素量。
    /// </summary>
    public enum ReactionConsumeMode
    {
        /// <summary>
        /// 固定消耗。
        /// </summary>
        Fixed = 0,

        /// <summary>
        /// 按当前已存储元素量比例消耗。
        /// </summary>
        RatioOfCurrentGauge = 1,

        /// <summary>
        /// 按本次新附着元素量比例消耗。
        /// </summary>
        RatioOfAppliedAmount = 2,

        /// <summary>
        /// 消耗全部可用元素量。
        /// </summary>
        All = 3,
    }

    /// <summary>
    /// 定义反应伤害的数值计算模型。
    /// </summary>
    public enum ReactionDamageModel
    {
        /// <summary>
        /// 使用规则定义的固定伤害。
        /// </summary>
        FlatByRule = 0,

        /// <summary>
        /// 按原始命中伤害缩放。
        /// </summary>
        ScaleFromSourceDamage = 1,

        /// <summary>
        /// 按消耗的元素量缩放。
        /// </summary>
        ScaleFromConsumedGauge = 2,

        /// <summary>
        /// 结合原始伤害与消耗元素量进行混合计算。
        /// </summary>
        Hybrid = 3,
    }
}
