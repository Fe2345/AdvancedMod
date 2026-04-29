using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 统一解析反应显示文本。
    /// </summary>
    public static class ReactionTextResolver
    {
        /// <summary>
        /// 获取反应对应的显示文本。
        /// </summary>
        public static string GetDisplayText(ReactionType reaction)
        {
            return reaction switch
            {
                ReactionType.ThunderWind => "雷风",
                ReactionType.Overloaded => "超载",
                _ => string.Empty,
            };
        }
    }
}
