using AdvancedMod.DamageClasses.Element;
using AdvancedMod.DamageClasses.Element.Interfaces;
using AdvancedMod.DamageClasses.Element.Players;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Armor.Element
{
    /// <summary>
    /// 熔骨头盔。
    /// </summary>
    public sealed class MoltenBoneHelmet : ModItem, IElementStatProvider, IElementArmorSetProvider
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.HallowedHelmet}";

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(silver: 90);
            Item.rare = ItemRarityID.Orange;
            Item.defense = 8;
            Item.headSlot = ContentSamples.ItemsByType[ItemID.HallowedHelmet].headSlot;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MoltenBoneBreastplate>() &&
                legs.type == ModContent.ItemType<MoltenBoneGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "元素伤害提高10%，受到的伤害降低8%";
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddIngredient(ItemID.Bone, 30)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public void ApplyElementStats(Player player, ElementCombatPlayerState state, Item item)
        {
            state.Offense.AddElementMastery(50);
        }

        public bool IsArmorSetActive(Player player)
        {
            return player.armor[1].type == ModContent.ItemType<MoltenBoneBreastplate>() &&
                player.armor[2].type == ModContent.ItemType<MoltenBoneGreaves>();
        }

        public void ApplyArmorSetStats(Player player, ElementCombatPlayerState state)
        {
            player.GetDamage(ModContent.GetInstance<ElementDamage>()) += 0.10f;
            player.endurance += 0.08f;
        }
    }
}
