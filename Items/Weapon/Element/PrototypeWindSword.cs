using AdvancedMod.DamageClasses.Element;
using AdvancedMod.DamageClasses.Element.Definitions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Weapon.Element
{
    /// <summary>
    /// Minimal melee prototype used to inject wind hits into the new element pipeline.
    /// </summary>
    public sealed class PrototypeWindSword : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.IronBroadsword}";

        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.DamageType = ModContent.GetInstance<ElementDamage>();
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = true;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            ElementCombatSystem.ProcessItemHit(player, target, Item, hit, damageDone, ElementType.Wind);
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            ElementCombatSystem.ModifyItemHit(player, target, Item, ref modifiers, ElementType.Wind);
        }
    }
}
