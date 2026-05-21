using AdvancedMod.DamageClasses.Element.Players;
using Terraria;

namespace AdvancedMod.DamageClasses.Element.Interfaces
{
    /// <summary>
    /// 允许护甲套装向玩家元素数值聚合层提供额外套装加成。
    /// 原生已支持的套装加成应只写入 <see cref="Player"/>，元素专属套装语义才写入 <see cref="ElementCombatPlayerState"/>。
    /// </summary>
    public interface IElementArmorSetProvider
    {
        /// <summary>
        /// 当前物品所代表的套装是否已完整穿戴。
        /// </summary>
        bool IsArmorSetActive(Player player);

        /// <summary>
        /// 应用套装加成，并保持原生字段与元素专属状态的边界清晰。
        /// </summary>
        void ApplyArmorSetStats(Player player, ElementCombatPlayerState state);
    }
}
