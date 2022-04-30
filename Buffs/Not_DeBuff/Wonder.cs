using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Buffs.Not_DeBuff
{
    public class Wonder : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("奇迹");
            Description.SetDefault("将会有奇迹发生");

            Main.buffNoSave[Type] = false;
            Main.debuff[Type] = false;
            this.canBeCleared = true;
            Main.lightPet[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            Color color = new Color(0, 0, 0);
            switch(Main.rand.Next(3000))
            {
                case 1:
                    for (int i = 0;i < Main.npc.Length; i++)
                    {
                        if (!Main.npc[i].friendly && !Main.npc[i].boss)
                        {
                            Main.npc[i].life = 0;
                        }
                    
                        if (!Main.npc[i].friendly && Main.npc[i].boss)
                        {
                            Main.npc[i].life = 1;
                        } 
                    }
                    Main.NewText("正在发生奇迹！", color);
                    break;
                case 2:
                    player.statLife = player.statLifeMax2;
                    Main.NewText("正在发生奇迹！", color);
                    break;
                case 3:
                    for (int i = 0; i < Main.projectile.Length; i++)
                    {
                        Main.projectile[i].Kill();
                    }
                    Main.NewText("正在发生奇迹！", color);
                    break;
            }
        }
    }
}
