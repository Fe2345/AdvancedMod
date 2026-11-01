using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Global
{
    public enum LunarSkill { Solar, Vortex, Nebula, Stardust }

    //特殊能量管理
    public class PlayerSpecialState : ModPlayer
    {
        public const float MAX_LUNAR_RATE = 720f; //最大月曜度
        private float lunarRate;
        // 当前选择供后续技能模块读取；本轮不实现技能效果、消耗或联机事务。
        public LunarSkill SelectedLunarSkill { get; set; } = LunarSkill.Solar;

        // 在写入入口约束，避免同一刻内先溢出再被其他逻辑读取；NaN 视为无能量。
        public float LunarRate
        {
            get => lunarRate;
            set => lunarRate = float.IsNaN(value) ? 0f : Math.Clamp(value, 0f, MAX_LUNAR_RATE);
        }
    }
}
