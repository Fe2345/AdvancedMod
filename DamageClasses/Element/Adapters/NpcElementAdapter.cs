using AdvancedMod.DamageClasses.Element.State;

namespace AdvancedMod.DamageClasses.Element.Adapters
{
    /// <summary>
    /// 持有 NPC 侧元素状态的适配器，负责注册默认元素并集中未来的适配逻辑。
    /// </summary>
    public sealed class NpcElementAdapter
    {
        /// <summary>
        /// 该适配器持有的元素状态容器。
        /// </summary>
        public ElementComponent Component { get; }

        public NpcElementAdapter()
        {
            Component = new ElementComponent();
            ElementComponent.RegisterDefaults(Component);
        }
    }
}
