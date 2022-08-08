using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace AdvancedMod
{
    public class AdvancedConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("NPC配置")]
        [Label("允许 异变 生成")]
        [Tooltip("禁用会导致Mod进程无法正常进行")]
        [DefaultValue(true)]
        public bool MutationSpawn;

        [Label("允许 疯子 生成")]
        [Tooltip("此NPC对游戏平衡造成较大破坏，建议关闭")]
        [DefaultValue(true)]
        public bool MadmanSpawn;

        [Label("允许 观察者 生成")]
        [Tooltip("此NPC为后期NPC，可选择关闭")]
        [DefaultValue(true)]
        public bool WatcherSpawn;

        [Header("游戏难度配置")]
        [Label("Disable Nurse Heal During Boss Alive")]
        [Tooltip("Only enable in Expert or Master(TML1.4) Mode World.If you find it`s too difficult,please disable this option")]
        [DefaultValue(false)]
        public bool disableNurseHeal;

        [Label("启用无伤模式")]
        [Tooltip("启用后，任何形式的伤害都将秒杀你。仅供无伤人使用。")]
        [DefaultValue(false)]
        public bool NoHitMode;

        [Header("辅助性内容配置")]
        [Label("可捕捉NPC")]
        [Tooltip("启用后，本Mod的城镇NPC将可以被虫网捕捉")]
        [DefaultValue(false)]
        public bool CanCatchNPC;

        [Header("模组联动配置")]
        [Label("在BossChecklist上显示由本Mod添加的Boss")]
        [Tooltip("需要安装BossChecklist Mod来显示这些Boss")]
        [DefaultValue(true)]
        public bool BossChecklist;

        [Label("在NPC对话中推荐辅助性Mod")]
        [Tooltip("会推荐更好的体验、Fargowiltas、BossChecklist、RecipeBroswer、Advanced Mod DLC")]
        [DefaultValue(true)]
        public bool RecommendMods;
    }
}
