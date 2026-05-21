using AdvancedMod.DamageClasses.Element.Interfaces;
using AdvancedMod.DamageClasses.Element.Players;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Armor.Element
{
    /// <summary>
    /// 熔骨胸甲。
    /// </summary>
    public sealed class MoltenBoneBreastplate : ModItem, IElementStatProvider
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.HallowedPlateMail}";

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 20;
            Item.value = Item.buyPrice(silver: 110);
            Item.rare = ItemRarityID.Orange;
            Item.defense = 9;
            Item.bodySlot = ContentSamples.ItemsByType[ItemID.HallowedPlateMail].bodySlot;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddIngredient(ItemID.Bone, 45)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public void ApplyElementStats(Player player, ElementCombatPlayerState state, Item item)
        {
            player.GetDamage(DamageClass.Generic) += 0.07f;
        }
    }
}
