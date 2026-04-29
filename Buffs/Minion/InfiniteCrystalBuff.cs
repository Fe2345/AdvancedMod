using AdvancedMod.Projectiles.Accessory;
using AdvancedMod.Utils;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Buffs.Minion
{
    public class InfiniteCrystalBuff : BasicMinionBuff
    {
        public override string Texture => $"Terraria/Images/Buff_{BuffID.Poisoned}";
        protected override int MinionProjectileType => ModContent.ProjectileType<InfiniteCrystalProj>();
        protected override ref bool MinionBool => ref BuffOwner.Advanced().InfiniteCrystal;
    }
}
