using AdvancedMod.Buffs.Minion;
using AdvancedMod.Projectiles.Accessory;
using AdvancedMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

public class InfiniteCrystal : ModItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.CopperBroadsword}";
    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 42;
        Item.value = 
        Item.rare = ItemRarityID.Green;
        Item.accessory = true;
        Item.expert = true;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        var modPlayer = player.Advanced();
        modPlayer.InfiniteCrystal = true;
        if (player.whoAmI == Main.myPlayer)
        {
            if (player.FindBuffIndex(ModContent.BuffType<InfiniteCrystalBuff>()) == -1)
            {
                player.AddBuff(ModContent.BuffType<InfiniteCrystalBuff>(), 3600, true);
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<InfiniteCrystalProj>()] < 1)
            {
                //创建召唤物
                var source = player.GetSource_Accessory(Item);
                int p = Projectile.NewProjectile(source, player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<InfiniteCrystalProj>(), 0, 1f, player.whoAmI);
                
            }
        }
    }
}