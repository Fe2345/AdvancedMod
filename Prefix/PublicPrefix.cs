using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Prefix
{
    public class Advanced : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("先进");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 1.44f;
            knockbackMult = 1.44f;
            useTimeMult = 0.56f;
            scaleMult = 1.44f;
            shootSpeedMult = 1.44f;
            manaMult = 0.56f;
            critBonus += 44;
        }

        public override bool CanRoll(Item item)
        {
            List<int> AdvancedItems = new List<int>
            {
                ItemID.Zenith,ItemID.EmpressBlade
            };

            if (ModLoader.TryGetMod("FargowiltasSouls",out Mod fargo))
            {
                if ((bool)fargo.Call("EMode"))
                {
                    //Moonlord Weapons
                    AdvancedItems.Add(ItemID.Terrarian);
                    AdvancedItems.Add(ItemID.Meowmere);
                    AdvancedItems.Add(ItemID.StarWrath);
                    AdvancedItems.Add(ItemID.SDMG);
                    AdvancedItems.Add(ItemID.LastPrism);
                    AdvancedItems.Add(ItemID.LunarFlareBook);
                    AdvancedItems.Add(ItemID.RainbowCrystalStaff);
                    AdvancedItems.Add(ItemID.MoonlordTurretStaff);
                    AdvancedItems.Add(ItemID.Celeb2);
                    //Empress Of Night Weapons
                    AdvancedItems.Add(ItemID.PiercingStarlight);
                    AdvancedItems.Add(ItemID.RainbowWhip);
                    AdvancedItems.Add(ItemID.FairyQueenMagicItem);
                    AdvancedItems.Add(ItemID.FairyQueenRangedItem);
                    AdvancedItems.Add(ItemID.SparkleGuitar);
                }
            }

            foreach (var type in AdvancedItems)
            {
                if (item.type == type) return true;
            }
            return false;
        }

        public override float RollChance(Item item)
        {
            return 30f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 2f;
        }
    }
}
