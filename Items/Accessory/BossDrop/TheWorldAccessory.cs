using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.BossDrop
{
    public class TheWorldAccessory : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("世界");
            Tooltip.SetDefault($"\"时间被停止了\"\n免疫世界减益\n摁下绑定热键暂停时间\n时间暂停期间只有你和你的弹幕可以自由移动");
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.buffImmune[ModContent.BuffType<Buffs.Debuff.TheWorld>()] = true;
        }
    }
}
