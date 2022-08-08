using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Symbols
{
    public class SymbolOfTown : ModItem
    {
        public override void SetStaticDefaults()
        {
            string Tip = "有五分之一概率将你所受伤害由任意城镇NPC承受\n根据当前世界上的城镇NPC提供商店优惠\n世界上所有城镇NPC防御+44\n防止你伤害小动物\n摁下[快速治疗]键为你回复大量血量\n你的攻击会抛出刺球\n你受击会制造共鸣权杖爆炸";

            DisplayName.SetDefault("城镇之符");

            if (ModLoader.TryGetMod("AdvancedModDLC", out Mod dlc))
            {
                if (ModLoader.TryGetMod("FargowiltasSouls", out Mod fargo))
                {
                    Tip += "\n免疫突变驾到、突变毒牙、突变啃啄、憎恶驾到、憎恶毒牙、热恋\n你的攻击会为你回复血量\n拥有闪烁崇心、憎恶手杖、突变体之眼的效果";

                }

                if (ModLoader.TryGetMod("CalamityMod",out Mod calamity))
                {
                    Tip += "\n绯红恶魔将为你而战\n你拥有海王祝福的效果\n每次重铸会返还10%价格";
                }
            }
            Tooltip.SetDefault(Tip);
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AdvancedPlayer.SymbolOfTown = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = Recipe.Create(this.Type);
            recipe.AddIngredient(ModContent.ItemType<Items.Accessory.Souls.TerrarianSoul>());
            /*
            if (ModLoader.TryGetMod("AdvancedModDLC",out Mod dlc))
            {
                if (ModLoader.TryGetMod("FargowiltasSouls",out Mod fargo))
                {
                    if (ModContent.Find<ModItem>("AdvancedModDLC","MasochistSoul") != null)
                    {
                        recipe.AddIngredient(ModContent.Find<ModItem>("AdvancedModDLC", "MasochistSoul"));
                    }
                    
                }
                if (ModLoader.TryGetMod("CalamityMod",out Mod calamity))
                {
                    if (ModContent.Find<ModItem>("AdvancedModDLC","DeadSoul") != null)
                    {
                        recipe.AddIngredient(ModContent.Find<ModItem>("AdvancedModDLC", "DeadSoul"));
                    }
                    
                }
            }
            */
            recipe.AddTile<Tiles.ElectromagneticWorkStation>();
            recipe.Register();
        }
    }
}
