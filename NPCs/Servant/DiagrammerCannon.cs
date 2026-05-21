using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.NPCs.Servant;
using AdvancedMod.NPCs.Boss.TreeDiagrammer;

namespace AdvancedMod.NPCs.Servant;
public sealed class DiagrammerCannon : ModNPC
{
    public override string Texture => $"{Mod.Name}/Assets/Textures/NPCs/DiagrammerCannon";

    private const float Tile = 16f;

    private int BossIndex => (int)NPC.ai[0];

    public override void SetDefaults()
    {
        NPC.width = 120;
        NPC.height = 80;
        NPC.lifeMax = 12000;
        NPC.damage = 90;
        NPC.defense = 20;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        NPC.netAlways = true;
    }

    public override void AI()
    {
        if (!IsBossValid(out NPC boss))
        {
            NPC.active = false;
            return;
        }

        Player player = Main.player[boss.target];
        Vector2 destination = new Vector2(player.Center.X, player.Center.Y - 25f * Tile);
        MoveTo(destination, 16f, 10f);

        if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 30 == 0)
        {
            SpawnDeathTreeLaser(player);
        }
    }

    public override void OnKill()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        if (!IsBossValid(out NPC boss))
        {
            return;
        }

        if (boss.ModNPC is TreeDiagrammer modBoss)
        {
            modBoss.NotifyCannonKilled();
        }
    }

    private bool IsBossValid(out NPC boss)
    {
        boss = null;

        if (BossIndex < 0 || BossIndex >= Main.maxNPCs)
        {
            return false;
        }

        NPC candidate = Main.npc[BossIndex];

        if (!candidate.active || candidate.type != ModContent.NPCType<TreeDiagrammer>())
        {
            return false;
        }

        boss = candidate;
        return true;
    }

    private void MoveTo(Vector2 destination, float maxSpeed, float inertia)
    {
        Vector2 desiredVelocity = destination - NPC.Center;

        if (desiredVelocity.LengthSquared() > maxSpeed * maxSpeed)
        {
            desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;
        }

        NPC.velocity = (NPC.velocity * (inertia - 1f) + desiredVelocity) / inertia;
    }

    private void SpawnDeathTreeLaser(Player player) { }
}