using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace AdvancedMod
{
    public class AdvancedConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Label("Disable Nurse Heal During Boss Alive")]
        [Tooltip("Only enable in Expert or Master(TML1.4) Mode World.If you find it`s too difficult,please disable this option")]
        [DefaultValue(false)]
        public static bool disableNurseHeal;

        [Label("Enable Energy System")]
        [Tooltip("Energy System Can Increase your damage,life regen,etc. But also decrease it during Energy Value is low.")]
        [DefaultValue(true)]
        public static bool enableEnergySystem;
    }
}
