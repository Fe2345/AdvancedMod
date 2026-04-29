using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Weapon.Element
{
    public class CombustibleEjector : ModItem //武器：爆炸物发射器
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.GrenadeLauncher}"; //暂时采用榴弹发射器的贴图

        public override void SetDefaults()
        {
            Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.Element.BurnedGel>(); //弹幕
        }
    }
}
