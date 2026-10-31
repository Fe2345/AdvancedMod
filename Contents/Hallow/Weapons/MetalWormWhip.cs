using System;
using System.Collections.Generic;
using System.IO;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

public sealed class MetalWormWhip : ModItem
{
    public override string Texture => MetalWormRules.TexturePath;
    public override void SetDefaults()
    {
        Item.DefaultToWhip(ModContent.ProjectileType<MetalWormWhipProjectile>(), MetalWormRules.Damage,
            MetalWormRules.Knockback, ContentSamples.ItemsByType[ItemID.SwordWhip].shootSpeed, MetalWormRules.UseTicks);
        Item.width = 30; Item.height = 48;
        Item.rare = ItemRarityID.Pink; Item.value = Item.sellPrice(gold: 8);
        ElementAttacks.ConfigureWeapon(Item, new TagSet(new[] { new TagEntry(false, TagEnum.Electro), new TagEntry(false, TagEnum.Hallow) }), 1, "Hallow.MetalWormWhip");
    }
    public override bool MeleePrefix() => true;
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        spriteBatch.Draw(TextureAssets.Item[Type].Value, position, MetalWormRules.HeadFrame, drawColor, 0,
            MetalWormRules.HeadFrame.Size() * .5f, 44f / MetalWormRules.HeadFrame.Height, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        spriteBatch.Draw(TextureAssets.Item[Type].Value, Item.Center - Main.screenPosition, MetalWormRules.HeadFrame,
            lightColor, rotation, MetalWormRules.HeadFrame.Size() * .5f, 48f / MetalWormRules.HeadFrame.Height * scale, SpriteEffects.None, 0);
        return false;
    }
}

public sealed class MetalWormWhipProjectile : ModProjectile
{
    private bool probeSpawned;
    private int initialDamage;
    private readonly List<Vector2> points = new();
    public override string Texture => MetalWormRules.TexturePath;
    public override void SetStaticDefaults() => ProjectileID.Sets.IsAWhip[Type] = true;
    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.SwordWhip);
        // 原版控制点长度与物品基础使用时长相乘，补偿28→24以保持迪朗达尔长度。
        Projectile.WhipSettings.RangeMultiplier *= (float)ContentSamples.ItemsByType[ItemID.SwordWhip].useAnimation / MetalWormRules.UseTicks;
    }
    public override void OnSpawn(IEntitySource source)
    {
        initialDamage = Projectile.damage;
        var data = Projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(new TagSet(new[] { new TagEntry(false, TagEnum.Electro), new TagEntry(false, TagEnum.Hallow) }), "Hallow.MetalWormWhip", true);
        data.ElementAmount = 1;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || damageDone <= 0) return;
        // 网络创建实例可能只经过SetDefaults／ReceiveExtraAI，没有本地OnSpawn。
        // 本鞭不衰减后续命中伤害，首击前从已同步的damage补齐快照。
        if (initialDamage <= 0) initialDamage = Projectile.damage;
        target.GetGlobalNPC<MetalWormMark>().Mark(target, Projectile.owner);
        Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.MinionAttackTargetUpdate, number: Projectile.owner);
        if (probeSpawned) return;
        probeSpawned = true;
        Vector2 away = (Main.player[Projectile.owner].Center - target.Center).SafeNormalize(-Vector2.UnitY);
        Vector2 spawn = target.Center + away * (target.Size.Length() * .5f + MetalWormRules.ProbeOffset);
        int damage = Math.Max(1, (int)(initialDamage * MetalWormRules.ProbeDamageFactor));
        int element = Main.rand.Next(1, DestroyerPhaseOneRules.ElementCount + 1);
        if (Main.netMode == NetmodeID.Server)
        {
            // 由owner生成以使用其原生identity与AI参数初始化；服务器只发送已确认的一次生成指令。
            var packet = Mod.GetPacket(); packet.Write(ProbePacketKind); packet.Write((byte)Projectile.owner);
            packet.Write(spawn.X); packet.Write(spawn.Y); packet.Write(damage); packet.Write(Projectile.knockBack);
            packet.Write((short)target.whoAmI); packet.Write((byte)element); packet.Send(Projectile.owner);
        }
        else SpawnProbe(spawn, damage, Projectile.knockBack, Projectile.owner, target.whoAmI, element);
    }
    internal const byte ProbePacketKind = 132;
    internal static void ReceiveProbe(BinaryReader reader)
    {
        int owner = reader.ReadByte(); Vector2 spawn = new(reader.ReadSingle(), reader.ReadSingle());
        int damage = reader.ReadInt32(); float knockback = reader.ReadSingle(); int target = reader.ReadInt16(), element = reader.ReadByte();
        if (Main.netMode != NetmodeID.MultiplayerClient || owner != Main.myPlayer || target < 0 || target >= Main.maxNPCs ||
            element < 1 || element > DestroyerPhaseOneRules.ElementCount || !Main.player[owner].active || Main.player[owner].dead) return;
        SpawnProbe(spawn, damage, knockback, owner, target, element);
    }
    private static void SpawnProbe(Vector2 spawn, int damage, float knockback, int owner, int target, int element) =>
        Projectile.NewProjectile(Main.player[owner].GetSource_Misc("MetalWormConfirmedHit"), spawn, Vector2.Zero,
            ModContent.ProjectileType<MetalWormProbe>(), damage, knockback, owner, target + 1, element);
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(probeSpawned); writer.Write(initialDamage); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        bool spawned = reader.ReadBoolean(); int damage = reader.ReadInt32();
        if (Main.netMode != NetmodeID.Server) { probeSpawned = spawned; initialDamage = damage; }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        points.Clear(); Projectile.FillWhipControlPoints(Projectile, points);
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 delta = points[i + 1] - points[i];
            Rectangle frame = i == points.Count - 2 ? MetalWormRules.HeadFrame : i == 0 ? MetalWormRules.TailFrame : MetalWormRules.BodyFrame;
            // 头朝鞭尖、尾朝握点；按控制点间距拉伸身体，绘制宽度不改变原生命中宽度。
            float length = i == points.Count - 2 ? MetalWormRules.DrawWidth * 1.5f : Math.Max(1, delta.Length() + 2);
            Main.EntitySpriteDraw(texture, (points[i] + points[i + 1]) * .5f - Main.screenPosition, frame,
                Lighting.GetColor(points[i].ToTileCoordinates()), delta.ToRotation() + MathHelper.PiOver2,
                frame.Size() * .5f, new Vector2(MetalWormRules.DrawWidth / frame.Width, length / frame.Height), SpriteEffects.None);
        }
        return false;
    }
}
