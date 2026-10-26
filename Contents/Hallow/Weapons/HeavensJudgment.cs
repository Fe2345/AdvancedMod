using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>天裁之刃：物品只发起一段攻击，持握弹幕负责实际姿态和命中。</summary>
public sealed partial class HeavensJudgment : ModItem
{
    internal HallowBladeCombo Combo { get; private set; } = new();
    public override string Texture => "AdvancedMod/Assets/Textures/Hallow/HeavensJudgmentIcon";
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
    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player)
    {
        if (player.ownedProjectileCounts[Item.shoot] > 0 || player.ownedProjectileCounts[ModContent.ProjectileType<HallowGuard>()] > 0 ||
            player.ownedProjectileCounts[ModContent.ProjectileType<HallowResonance>()] > 0) return false;
        if (player.altFunctionUse != 2) return true;
        var environment = HallowActionRules.Environment(player);
        return player.controlDown && environment.HasValue && environment != Resonance || player.GetModPlayer<HallowGuardPlayer>().CanStart;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var direction = velocity.SafeNormalize(new Vector2(player.direction, 0));
        if (player.altFunctionUse == 2)
        {
            var environment = HallowActionRules.Environment(player);
            if (player.controlDown && environment.HasValue && environment != Resonance)
                Projectile.NewProjectile(source, player.Center, direction, ModContent.ProjectileType<HallowResonance>(), 0, 0,
                    player.whoAmI, HallowActionRules.Encode(environment));
            else if (player.GetModPlayer<HallowGuardPlayer>().CanStart)
                Projectile.NewProjectile(source, player.Center, direction, ModContent.ProjectileType<HallowGuard>(), 0, 0, player.whoAmI);
            return false;
        }
        var (stage, angle) = Combo.Begin(direction.ToRotation(), Main.GameUpdateCount);
        // 直接使用原生已计算的动画时长，包含攻速、前缀与其他模组 UseSpeed 修正，不重复除攻速。
        int duration = System.Math.Max(1, player.itemAnimationMax);
        int index = ElementAttacks.NewProjectile(source, player.MountedCenter, direction, type, damage, knockback,
            HallowActionRules.Tags(Resonance), Resonance.HasValue ? HallowActionRules.Attachment : 0,
            player.whoAmI, stage, angle, duration, "Hallow.HeavensJudgment");
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
