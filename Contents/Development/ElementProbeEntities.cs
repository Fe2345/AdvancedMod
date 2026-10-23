using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>只由开发测试入口生成的无防御靶标。</summary>
public sealed class ElementProbeNPC : ModNPC
{
    internal static System.Action<Projectile, NPC> AfterProjectileHit;
    public override string Texture => "Terraria/Images/NPC_1";
    public override void SetDefaults()
    {
        NPC.width = 32;
        NPC.height = 48;
        NPC.lifeMax = 100000;
        NPC.damage = 100;
        NPC.defense = 0;
        NPC.knockBackResist = 0;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        NPC.npcSlots = 0;
    }
    public override bool CheckActive() => false;
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        modifiers.DamageVariationScale *= 0;
        modifiers.DisableCrit();
    }
    public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) =>
        AfterProjectileHit?.Invoke(projectile, NPC);
}

public sealed class ElementProbeProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 120;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
    }
}

/// <summary>测试旗标仅在同步测试期间启用，不改变正常游玩。</summary>
public sealed class ElementProbePlayer : ModPlayer
{
    internal static bool Cancel;
    internal static bool Dodge;
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Cancel) modifiers.Cancel();
    }
    public override bool FreeDodge(Player.HurtInfo info) => Dodge;
}
