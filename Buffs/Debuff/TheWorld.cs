using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Buffs.Debuff
{
    public class TheWorld : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("世界");
            Description.SetDefault("四蛋都怕我！");
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.mount.Active)
                player.mount.Dismount(player);

            player.controlLeft = false;
            player.controlRight = false;
            player.controlJump = false;
            player.controlDown = false;
            player.controlUseItem = false;
            player.controlUseTile = false;
            player.controlHook = false;
            player.controlMount = false;
            player.velocity = player.oldVelocity;
            player.position = player.oldPosition;

            player.lifeRegen = 0;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<NPCs.AdvancedGlobelNPC>().TheWorld = true;
        }
    }
}
