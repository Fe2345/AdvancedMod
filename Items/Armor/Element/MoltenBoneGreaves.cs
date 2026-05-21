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
    /// 熔骨护腿。
    /// </summary>
    public sealed class MoltenBoneGreaves : ModItem, IElementStatProvider
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.HallowedGreaves}";

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.value = Item.buyPrice(silver: 100);
            Item.rare = ItemRarityID.Orange;
            Item.defense = 8;
            Item.legSlot = ContentSamples.ItemsByType[ItemID.HallowedGreaves].legSlot;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HellstoneBar, 20)
                .AddIngredient(ItemID.Bone, 60)
                .AddTile(TileID.Anvils)
                .Register();
        }

        public void ApplyElementStats(Player player, ElementCombatPlayerState state, Item item)
        {
            player.GetCritChance(ModContent.GetInstance<ElementDamage>()) += 7f;
        }
    }
}
