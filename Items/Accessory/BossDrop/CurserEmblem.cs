﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.BossDrop
{
    public class CurserEmblem : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/CurserEmblem";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 42;
            Item.height = 42;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 11);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(ModContent.GetInstance<DamageClasses.CurseDamage>()) += 0.15f;
        }
    }
}
