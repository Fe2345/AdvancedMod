using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

public sealed class RainCloudDrop : ModProjectile
{
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainFriendly}";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.RainFriendly);
        AIType = ProjectileID.RainFriendly;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = true;
    }
    public override void OnSpawn(IEntitySource source)
    {
        // 每个雨滴自行配置，既不依赖当前手持物品，也不继承父弹幕修正。
        var data = Projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }), "Hallow.RainCloudDrop", isManagedAttack: true);
        data.ElementAmount = 0.10;
        data.ElementConsumptionMultiplier = 0.075f;
        data.ReactionCooldownMultiplier = 0.1f;
    }
}
