using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element
{
    /// <summary>
    /// Damage class used by the new element combat system and its prototype weapons.
    /// </summary>
    public class ElementDamage : DamageClass
    {
        /// <summary>
        /// Keeps the class aligned with generic damage while suppressing crit scaling.
        /// </summary>
        /// <param name="player">The player receiving the default stat setup.</param>
        public override void SetDefaultStats(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += 0f;
        }

        /// <summary>
        /// Disables the standard crit calculation path for element damage.
        /// </summary>
        public override bool UseStandardCritCalcs => false;

        /// <summary>
        /// Hides crit chance from the tooltip because this damage class does not use normal crit rules.
        /// </summary>
        /// <param name="player">The player whose tooltip is being built.</param>
        /// <param name="lineName">The tooltip stat line being queried.</param>
        /// <returns><see langword="true"/> when the line should be shown; otherwise <see langword="false"/>.</returns>
        public override bool ShowStatTooltipLine(Player player, string lineName)
        {
            return !lineName.Equals("CritChance");
        }
    }
}
