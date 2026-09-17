using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.State
{
    /// <summary>
    /// NPC 侧元素状态容器。
    /// 通过 <see cref="States"/> 字典按元素类型索引，新增元素只需注册即可。
    /// </summary>
    public sealed class ElementComponent
    {
        /// <summary>
        /// 已注册的元素状态字典。
        /// </summary>
        public Dictionary<ElementType, ElementState> States { get; } = new();

        /// <summary>
        /// 按元素类型获取对应的状态，元素未注册时返回 null。
        /// </summary>
        public ElementState? this[ElementType element]
        {
            get
            {
                States.TryGetValue(element, out ElementState? state);
                return state;
            }
        }

        /// <summary>
        /// 最近一次需要显示的反应文字。
        /// </summary>
        public string LastReactionText { get; set; } = string.Empty;

        /// <summary>
        /// 最近一次记录反应文字的游戏帧。
        /// </summary>
        public int LastReactionTick { get; set; }

        /// <summary>
        /// 注册默认的元素状态条目。
        /// </summary>
        public static void RegisterDefaults(ElementComponent component)
        {
            Register(component, ElementType.Thunder);
            Register(component, ElementType.Wind);
            Register(component, ElementType.Pyro);
        }

        /// <summary>
        /// 向容器中注册指定元素的状态条目。
        /// </summary>
        public static void Register(ElementComponent component, ElementType element)
        {
            if (element == ElementType.None)
            {
                return;
            }

            if (!component.States.ContainsKey(element))
            {
                component.States[element] = new ElementState { Element = element };
            }
        }
    }
}
