namespace AdvancedMod.DamageClasses.Element.State
{
    /// <summary>
    /// NPC 侧元素状态容器。
    /// </summary>
    public sealed class ElementComponent
    {
        /// <summary>
        /// 当前雷元素状态。
        /// </summary>
        public ElementState Thunder { get; } = new() { Element = Definitions.ElementType.Thunder };

        /// <summary>
        /// 当前风元素状态。
        /// </summary>
        public ElementState Wind { get; } = new() { Element = Definitions.ElementType.Wind };

        /// <summary>
        /// 当前火元素状态。
        /// </summary>
        public ElementState Pyro { get; } = new() { Element = Definitions.ElementType.Pyro };

        /// <summary>
        /// 最近一次需要显示的反应文字。
        /// </summary>
        public string LastReactionText { get; set; } = string.Empty;

        /// <summary>
        /// 最近一次记录反应文字的游戏帧。
        /// </summary>
        public int LastReactionTick { get; set; }
    }
}
