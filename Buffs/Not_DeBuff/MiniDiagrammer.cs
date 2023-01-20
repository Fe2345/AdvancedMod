using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class MiniDiagrammer : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("迷你设计者");
            Description.SetDefault("迷你树状图设计者会为你战斗");
            Main.buffNoSave[Type] = true;
            // 不显示buff时间
            Main.buffNoTimeDisplay[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            AdvancedPlayer modPlayer = player.GetModPlayer<AdvancedPlayer>();
            // 如果当前有属于玩家的僚机的弹幕
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Weapons.MiniDiagrammerProj>()] > 0)
            {
                modPlayer.MiniDiagrammer = true;
            }
            // 如果玩家取消了这个召唤物就让buff消失
            if (!modPlayer.MiniDiagrammer)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
            else
            {
                player.buffTime[buffIndex] = 9999;
            }
        }
        
    }
}
