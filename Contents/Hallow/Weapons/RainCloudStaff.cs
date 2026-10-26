using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

public sealed class RainCloudStaff : ModItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.NimbusRod}";
    public override void SetStaticDefaults()
    {
        ItemID.Sets.ShimmerTransformToItem[ItemID.NimbusRod] = Type;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
    }
    public override void SetDefaults()
    {
        // 使用时长、魔力、音效与外观尺寸沿用原版雨云魔杖。
        Item.CloneDefaults(ItemID.NimbusRod);
        Item.damage = 40;
        Item.DamageType = DamageClass.Summon;
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(gold: 3);
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<RainCloud>();
        Item.buffType = ModContent.BuffType<RainCloudBuff>();
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        player.AddBuff(Item.buffType, RainCloud.BuffDuration);
        int index = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
        if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}

public sealed class RainCloudBuff : ModBuff
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.NimbusRod}";
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<RainCloud>()] > 0)
            player.buffTime[buffIndex] = RainCloud.BuffDuration;
        else
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}
