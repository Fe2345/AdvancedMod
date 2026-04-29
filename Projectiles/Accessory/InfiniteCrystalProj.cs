
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Projectiles.Accessory
{
    public class InfiniteCrystalProj : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.AbigailCounter}";

        //召唤物在场时，拥有与NPC类似的DoTData字典，用于收集附近死亡NPC的DoT数据，并为其他NPC附着
        public Dictionary<int, Systems.DataStructures.DoTData> ActiveDoTs = new Dictionary<int, Systems.DataStructures.DoTData>();
        public override void SetDefaults()
        {
            
        }

        public override void AI()
        {
            //
        }

        public override bool? CanDamage()
        {
            return false; //无论如何，无法造成伤害
        }
    }
}
