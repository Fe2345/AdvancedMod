using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public class SiliconPlateMail : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("精硅板甲");
            Tooltip.SetDefault("提高15%移动速度");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold:5);
            Item.defense = 32;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.2f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            bool ArmorSetted = head.type == ModContent.ItemType<Items.Armor.SiliconHelmet>() && legs.type == ModContent.ItemType<Items.Armor.SiliconGreaves>();
            if (ArmorSetted)
            {
                AdvancedPlayer.SiliconArmorEquip = true;
            }
            else
            {
                AdvancedPlayer.SiliconArmorEquip = false;
            }
            return ArmorSetted;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "受伤将向最近的目标发射闪电\n免疫电磁感应、触电、带电、电磁束缚";
            player.buffImmune[ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>()] = true;
            player.buffImmune[BuffID.Electrified] = true;
        }
    }
}
