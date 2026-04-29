using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Utils
{
    public static partial class AdvancedExtensions
    {
        public static AdvancedPlayer Advanced(this Player player) => player.GetModPlayer<AdvancedPlayer>();
        public static AdvancedGlobelNPC Advanced(this NPC npc) => npc.GetGlobalNPC<AdvancedGlobelNPC>();

        //工具方法：检测玩家/NPC附近是否存在指定弹幕
        public static ModProjectile GetProjInDistance(NPC npc,ModProjectile proj, float distance)
        {

            return null;
        }
        public static ModProjectile GetProjInDistance(Player player, float distance)
        {
            return null;
        }
        //public static CalamityVanillaAIOverrideNPC AIOverrideNPC(this NPC npc) => npc.GetGlobalNPC<CalamityVanillaAIOverrideNPC>();
        /*
        public static T? AIOverride<T>(this NPC npc)
        {
            VanillaAIOverride aiOverride = npc.GetGlobalNPC<CalamityVanillaAIOverrideNPC>().AIOverride;
            if (aiOverride == null)
                throw new System.Exception("NPC does not have an AI Override");

            if (aiOverride is T ai)
                return ai;

            throw new System.Exception($"NPC's AI Override is not of Type {typeof(T).Name}");
        }
        public static CalamityTileCollisionHarmNPC FlungNPC(this NPC npc) => npc.GetGlobalNPC<CalamityTileCollisionHarmNPC>();
        public static CalamityPolarityNPC PolarityNPC(this NPC npc) => npc.GetGlobalNPC<CalamityPolarityNPC>();
        public static CalamityGlobalItem Calamity(this Item item) => item.GetGlobalItem<CalamityGlobalItem>();
        public static CalamityGlobalProjectile Calamity(this Projectile proj) => proj.GetGlobalProjectile<CalamityGlobalProjectile>();
        public static TransformationPlayer Transformation(this Player player) => player.GetModPlayer<TransformationPlayer>();
        */
    }
}