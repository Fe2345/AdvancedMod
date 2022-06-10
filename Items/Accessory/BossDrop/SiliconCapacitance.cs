using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.BossDrop
{
    public class SiliconCapacitance : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("硅晶电容");
            Tooltip.SetDefault($"电容会随时间推移积累电量\n你的攻击力会随电量而变化\n每次受击会损失一半电量\n异变模式");
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 42;
            Item.height = 42;
            Item.rare = ItemRarityID.Expert;
            Item.value = Item.sellPrice(platinum: 11);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (hideVisual)
            {
                AdvancedPlayer.SiliconCapacotance = false;
            }
        }
    }
}
