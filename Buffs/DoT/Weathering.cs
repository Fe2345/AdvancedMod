using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using AdvancedMod.Systems.DataStructures;
using AdvancedMod.Buffs.DoT.Data;

namespace AdvancedMod.Buffs.DoT
{
    public class Weathering : ModBuff
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Buffs/Weathering";

        public override void SetStaticDefaults()
        {
            Main.pvpBuff[Type] = true;
            Main.debuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.TryGetGlobalNPC(out AdvancedGlobelNPC advancedNPC))
            {
                // 如果NPC身上没有该DoT的数据（例如通过原版AddBuff方法施加），则为其添加默认的DoT数据
                if (!advancedNPC.ActiveDoTs.ContainsKey(Type))
                {
                    advancedNPC.AddOrUpdateDoT(npc, Type, npc.buffTime[buffIndex], new WeatheringDoTData());
                }
            }

            // 下面可以添加一些视觉效果，例如生成灰尘(Dust)
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, Terraria.ID.DustID.Sandnado, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 对于玩家的视觉效果等处理
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(player.position, player.width, player.height, Terraria.ID.DustID.Sandnado, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }
    }
}
