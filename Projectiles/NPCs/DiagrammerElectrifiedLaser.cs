using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.NPCs
{
    public sealed class DiagrammerElectrifiedLaser : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.EyeLaser}";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EyeLaser);

            Projectile.friendly = false;
            Projectile.hostile = true;

            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;

            AIType = ProjectileID.EyeLaser;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 240);
        }
    }
}