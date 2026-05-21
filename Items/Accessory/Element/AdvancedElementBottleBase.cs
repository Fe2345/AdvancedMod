using AdvancedMod.DamageClasses.Element;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Interfaces;
using AdvancedMod.DamageClasses.Element.Players;
using AdvancedMod.Projectiles.Weapons.Element;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Accessory.Element
{
    /// <summary>
    /// 高级元素瓶的公共逻辑基类。
    /// </summary>
    public abstract class AdvancedElementBottleBase : ModItem, IElementStatProvider
    {
        protected abstract ElementType BoundElement { get; }

        public override string Texture => $"Terraria/Images/Item_{ItemID.BottledWater}";

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 30;
            Item.accessory = true;
            Item.value = Item.buyPrice(silver: 60);
            Item.rare = ItemRarityID.Orange;
        }

        public void ApplyElementStats(Player player, ElementCombatPlayerState state, Item item)
        {
            state.Offense.AddElementDamageBonus(BoundElement, 0.15f);
            state.CoordinatedAttacks.SetEntry(BoundElement, 10, 4, ModContent.ProjectileType<AdvancedElementOrb>(), 2.4f);
        }
    }
}
