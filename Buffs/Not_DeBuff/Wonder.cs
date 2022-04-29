using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

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
            switch(Main.rand.Next(50))
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
                    break;
                case 2:
                    player.statLife = player.statLifeMax2;
                    break;
                case 3:
                    for (int i = 0; i < Main.projectile.Length; i++)
                    {
                        Main.projectile[i].Kill();
                    }
                    break;
            }
        }
    }
}
