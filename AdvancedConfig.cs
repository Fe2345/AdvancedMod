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
        public bool disableNurseHeal;

        [Label("启用无伤模式")]
        [Tooltip("启用后，任何形式的伤害都将秒杀你。仅供无伤人使用。")]
        [DefaultValue(false)]
        public bool NoHitMode;
    }
}
