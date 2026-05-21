using AdvancedMod.DamageClasses.Element.Players;
using Terraria;

namespace AdvancedMod.DamageClasses.Element.Interfaces
{
    /// <summary>
    /// 允许装备向玩家元素数值聚合层提供贡献。
    /// 原生已支持的增伤、暴击、减伤等字段应只写入 <see cref="Player"/>；
    /// 只有元素精通、分元素倍率、协同攻击等元素专属字段写入 <see cref="ElementCombatPlayerState"/>。
    /// </summary>
    public interface IElementStatProvider
    {
        /// <summary>
        /// 应用当前装备的数值贡献，并保持原生字段与元素专属状态的边界清晰。
        /// </summary>
        void ApplyElementStats(Player player, ElementCombatPlayerState state, Item item);
    }
}
