using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>天裁之刃：物品只发起一段攻击，持握弹幕负责实际姿态和命中。</summary>
public sealed class HeavensJudgment : ModItem
{
    internal HallowBladeCombo Combo { get; private set; } = new();
    public override string Texture => $"Terraria/Images/Item_{ItemID.Excalibur}";
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.damage = HallowBladeRules.BaseDamage;
        Item.crit = HallowBladeRules.BonusCrit;
        Item.DamageType = DamageClass.Melee;
        Item.useTime = Item.useAnimation = HallowBladeRules.BaseUseTicks;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.knockBack = HallowBladeRules.Knockback;
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(gold: 5, silver: 9, copper: 1);
        Item.shoot = ModContent.ProjectileType<HeavensJudgmentSwing>();
        Item.shootSpeed = 1;
    }
    public override bool MeleePrefix() => true;
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var direction = velocity.SafeNormalize(new Vector2(player.direction, 0));
        var (stage, angle) = Combo.Begin(direction.ToRotation(), Main.GameUpdateCount);
        // 直接使用原生已计算的动画时长，包含攻速、前缀与其他模组 UseSpeed 修正，不重复除攻速。
        int duration = System.Math.Max(1, player.itemAnimationMax);
        int index = ElementAttacks.NewProjectile(source, player.MountedCenter, direction, type, damage, knockback,
            TagSet.Empty, 0, player.whoAmI, stage, angle, duration, "Hallow.HeavensJudgment");
        if (index >= Main.maxProjectiles) Combo.Reset();
        return false;
    }
    public override ModItem Clone(Item newEntity)
    {
        var clone = (HeavensJudgment)base.Clone(newEntity);
        clone.Combo = new();
        return clone;
    }
}
