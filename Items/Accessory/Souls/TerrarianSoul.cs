using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class TerrarianSoul : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/TerrarianSoul";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("泰拉人之魂");
            Tooltip.SetDefault("\"你好，泰拉瑞亚的热心人！\"\n拥有小动物保护指南的效果\n摁下快速治疗键可快速治疗并回复大量血量\n你的攻击会撒下刺球\n受击会引发共鸣权杖爆炸伤害敌人\n重铸价格降低50%");

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
            player.dontHurtCritters = true;
        }
    }
}
