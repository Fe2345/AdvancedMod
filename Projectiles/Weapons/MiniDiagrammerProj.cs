using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;

namespace AdvancedMod.Projectiles.Weapons
{
    public class MiniDiagrammerProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 3;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.scale = 1.1f;
            // 召唤物必备的属性
            //Main.projPet[Type] = true;
            Projectile.netImportant = true;
            Projectile.minionSlots = 1;
            Projectile.minion = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;

            Player player = Main.player[Projectile.owner];
            AdvancedPlayer advancedPlayer = player.GetModPlayer<AdvancedPlayer>();

            if (player.dead) advancedPlayer.MiniDiagrammer = false;
            if (advancedPlayer.MiniDiagrammer) Projectile.timeLeft = 2;
            player.AddBuff(ModContent.BuffType<Buffs.Not_DeBuff.MiniDiagrammer>(), 2);

            //status control
            if (Vector2.Distance(Projectile.Center,player.Center) > 8000)
            {
                Projectile.ai[0] = 3;
            }
            else
            {
                NPC enermy = Tool.FindClosestEnermy(Projectile.Center);
                if (enermy == null || Vector2.Distance(enermy.Center,player.Center) > 8000)
                {
                    Projectile.ai[0] = 1;
                }
                else
                {
                    Projectile.ai[0] = 2;
                }
            }

            switch (Projectile.ai[0])
            {
                case 1f:
                    //8000内，无敌人
                    if (Vector2.Distance(Projectile.Center, player.Center) != 400)
                    {
                        Projectile.velocity =  Vector2.Normalize(Tool.ChaseAround(Projectile.velocity, Projectile.Center, player.Center, 400, 0.25f)) * 10;
                    }
                    else
                    {
                        Projectile.velocity =  Vector2.Normalize(Tool.CircleAround(Projectile.velocity, Projectile.Center, player.Center, 400)) * 10;
                    }
                    break;
                case 2f:
                    NPC enermy = Tool.FindClosestEnermy(Projectile.Center);
                    if (Vector2.Distance(Projectile.Center,enermy.Center) <= 800)
                    {
                        if (Projectile.ai[1] % 45 == 0)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromAI(),Projectile.Center,Vector2.Normalize(enermy.Center - Projectile.Center)*20,ModContent.ProjectileType<Projectiles.Weapons.TreeDiagrammerLaserFriendly>(),100,1);
                        }
                    }
                    else
                    {
                        Vector2 diff = enermy.Center - Projectile.Center;
                        Projectile.velocity = Vector2.Normalize((20 * Projectile.velocity + 5 * diff) / 25) * 10;
                    }
                    break;
                case 3f:
                    Projectile.velocity = Vector2.Normalize(Tool.ChaseAround(Projectile.velocity, Projectile.Center, player.Center, 400, 0.25f))*10;
                    break;
            }
        }
    }
}
