#nullable enable
using System;
using System.IO;
using System.Reflection;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using AdvancedMod.Basic.Utils;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>NPC 由服务器结算；玩家受伤沿用 TML 所属客户端权威。镜像不运行反应。</summary>
public sealed class ElementNetwork : ModSystem
{
    internal const int SyncInterval = 6;
    private const int MaxAttackIdLength = 256;
    private enum Packet : byte { PlayerState, ItemHit, ReactionLife, ReactionVisual }
    private static readonly MethodInfo itemHit = typeof(Player).GetMethod("ProcessHitAgainstNPC", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly ulong[] lastItemSequence = new ulong[Main.maxPlayers];
    private static ulong itemSequence;
    private static readonly ulong[,] lastItemTick = new ulong[Main.maxPlayers, Main.maxNPCs];
    private static readonly ulong[,] nextItemTick = new ulong[Main.maxPlayers, Main.maxNPCs];
    private static readonly long[,] lastItemIdentity = new long[Main.maxPlayers, Main.maxNPCs];
    public override void Load()
    {
        IL_Projectile.Damage += NpcAuthority;
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += Spawn;
    }
    public override void Unload()
    {
        IL_Projectile.Damage -= NpcAuthority;
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float -= Spawn;
        ElementAttacks.SpawnDepth = 0;
    }
    private static int Spawn(On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float orig,
        IEntitySource source, float x, float y, float vx, float vy, int type, int damage, float knockback, int owner, float ai0, float ai1, float ai2)
    {
        ElementAttacks.SpawnDepth++;
        try { return orig(source, x, y, vx, vy, type, damage, knockback, owner, ai0, ai1, ai2); }
        finally { ElementAttacks.SpawnDepth--; }
    }
    public override void OnWorldLoad() { Array.Clear(lastItemSequence); Array.Clear(lastItemTick); Array.Clear(nextItemTick); Array.Clear(lastItemIdentity); itemSequence = 0; }

    internal static void ResetConnection(int index)
    {
        lastItemSequence[index] = 0;
        for (int i = 0; i < Main.maxNPCs; i++) { lastItemTick[index, i] = 0; nextItemTick[index, i] = 0; }
    }

    private static void NpcAuthority(ILContext il)
    {
        var c = new ILCursor(il);
        // 只替换 NPC 碰撞循环前的 owner 判定，不修改 Main.myPlayer，也不接管切砖或 PvP。
        if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdarg0(),
            i => i.MatchLdfld<Projectile>("owner"), i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.OpCode == OpCodes.Bne_Un || i.OpCode == OpCodes.Bne_Un_S,
            i => i.MatchLdcR4(1f))) throw new InvalidOperationException("无法定位弹幕 NPC 权威判定，停止加载以避免双重伤害。");
        c.Index += 3;
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<int, Projectile, int>>((local, p) =>
            p.friendly && !p.hostile && p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.IsManagedAttack
                ? Main.netMode == NetmodeID.Server ? p.owner : local
                : local);
    }

    internal static void WriteAttack(BinaryWriter w, AttackProfile profile, double amount, float consumption, float cd)
    {
        w.Write(profile.IsManagedAttack);
        w.Write(profile.AttackId ?? "");
        w.Write(profile.SourceItemId ?? "");
        w.Write((byte)profile.Tags.Count);
        foreach (var tag in profile.Tags) { w.Write((int)tag.type); w.Write(tag.cannotBeCovered); }
        w.Write(amount); w.Write(consumption); w.Write(cd);
    }
    internal static (AttackProfile Profile, double Amount, float Consumption, float Cd) ReadAttack(BinaryReader r)
    {
        bool managed = r.ReadBoolean();
        string attackId = r.ReadString(), sourceItemId = r.ReadString();
        if (attackId.Length > MaxAttackIdLength || sourceItemId.Length > MaxAttackIdLength) throw new InvalidDataException("攻击标识过长。");
        int count = r.ReadByte();
        if (count > Enum.GetValues<TagEnum>().Length) throw new InvalidDataException("标签数量非法。");
        var tags = new TagEntry[count];
        for (int i = 0; i < count; i++) { var type = (TagEnum)r.ReadInt32(); tags[i] = new(r.ReadBoolean(), type); }
        double amount = r.ReadDouble(); float consumption = r.ReadSingle(), cd = r.ReadSingle();
        if (!double.IsFinite(amount) || amount < 0 || !float.IsFinite(consumption) || consumption <= 0 || !float.IsFinite(cd) || cd < 0)
            throw new InvalidDataException("攻击元素参数非法。");
        return (new(new TagSet(tags), attackId.Length == 0 ? null : attackId, managed,
            sourceItemId.Length == 0 ? null : sourceItemId), amount, consumption, cd);
    }

    internal static void SendPlayer(Player player, int to = -1, int ignore = -1)
    {
        var packet = ModContent.GetInstance<ElementNetwork>().Mod.GetPacket();
        packet.Write((byte)Packet.PlayerState);
        packet.Write((byte)player.whoAmI);
        player.GetModPlayer<CombatPlayer>().Elements.WriteState(packet);
        packet.Send(to, ignore);
    }
    internal static void RequestItem(Player player, NPC npc, Rectangle rectangle)
    {
        // 配置可能刚在本刻修改，确保物品描述先于命中请求到达服务器。
        NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: player.selectedItem);
        var packet = ModContent.GetInstance<ElementNetwork>().Mod.GetPacket();
        packet.Write((byte)Packet.ItemHit); packet.Write(++itemSequence);
        packet.Write((short)npc.whoAmI); packet.Write(npc.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity);
        packet.Write((byte)player.selectedItem);
        packet.Write(player.itemAnimation);
        packet.Write(rectangle.X); packet.Write(rectangle.Y); packet.Write(rectangle.Width); packet.Write(rectangle.Height);
        packet.Send();
        player.SetMeleeHitCooldown(npc.whoAmI, Math.Max(1, player.itemAnimation));
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        switch ((Packet)reader.ReadByte())
        {
            case Packet.ReactionLife:
                int cost = reader.ReadInt32();
                if (Main.netMode == NetmodeID.MultiplayerClient && cost > 0)
                {
                    ApplyLifeCost(Main.LocalPlayer, cost);
                    NetMessage.SendData(MessageID.PlayerLifeMana, number: Main.myPlayer);
                }
                break;
            case Packet.ReactionVisual:
                var start = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                var end = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                var reaction = (TagEnum)reader.ReadInt32();
                if (Main.netMode == NetmodeID.MultiplayerClient && float.IsFinite(start.X) && float.IsFinite(start.Y) &&
                    float.IsFinite(end.X) && float.IsFinite(end.Y) && Vector2.DistanceSquared(start, end) <= AmplificationReactions.ChainRadius * AmplificationReactions.ChainRadius)
                    DrawReaction(start, end, reaction);
                break;
            case Packet.PlayerState:
                int index = reader.ReadByte();
                var state = new ElementTarget(ElementTarget.DefaultOperationLimit);
                state.ReadState(reader);
                if (index >= Main.maxPlayers || Main.netMode == NetmodeID.Server && (index != sender || !Main.player[index].active)) return;
                if (Main.netMode == NetmodeID.MultiplayerClient && index == Main.myPlayer) return;
                Main.player[index].GetModPlayer<CombatPlayer>().SetNetworkState(state);
                if (Main.netMode == NetmodeID.Server) SendPlayer(Main.player[index], ignore: sender);
                break;
            case Packet.ItemHit:
                ulong sequence = reader.ReadUInt64(); int npcIndex = reader.ReadInt16(); long identity = reader.ReadInt64();
                int slot = reader.ReadByte(); int animation = reader.ReadInt32();
                var rectangle = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers || sequence <= lastItemSequence[sender]) return;
                lastItemSequence[sender] = sequence;
                var player = Main.player[sender];
                if (!player.active || player.dead || animation <= 0 || slot >= player.inventory.Length || slot != player.selectedItem || npcIndex < 0 || npcIndex >= Main.maxNPCs) return;
                var npc = Main.npc[npcIndex]; var item = player.inventory[slot];
                // 请求到达时服务器动画可能已结束；保留命中时剩余帧数，并按武器动画上限钳制。
                int remaining = Math.Min(animation, Math.Max(1, Math.Max(item.useAnimation, player.itemAnimationMax)));
                if (!npc.active || identity != npc.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity ||
                    !item.GetGlobalItem<CombatGlobalItem>().Profile.IsManagedAttack || item.noMelee || item.damage <= 0 ||
                    lastItemIdentity[sender, npcIndex] == identity && nextItemTick[sender, npcIndex] > Main.GameUpdateCount) return;
                // 只接受与当前挥击尺寸及玩家位置相符的碰撞提示，伤害由服务器重新读取。
                float reach = (item.width + item.height) * player.GetAdjustedItemScale(item) + player.width + player.height;
                if (rectangle.Width <= 0 || rectangle.Height <= 0 || rectangle.Width > reach || rectangle.Height > reach ||
                    Vector2.Distance(rectangle.Center.ToVector2(), player.Center) > reach) return;
                if (lastItemIdentity[sender, npcIndex] == identity && lastItemTick[sender, npcIndex] == Main.GameUpdateCount) return;
                lastItemIdentity[sender, npcIndex] = identity;
                lastItemTick[sender, npcIndex] = Main.GameUpdateCount;
                // 原生 meleeNPCHitCooldown 只在所属客户端递减；服务器按游戏刻保存等价截止时间。
                player.SetMeleeHitCooldown(npcIndex, 0);
                int oldLife = npc.life;
                itemHit.Invoke(player, new object[] { item, rectangle, player.GetWeaponDamage(item), player.GetWeaponKnockback(item), npcIndex });
                if (npc.life != oldLife || !player.CanHitNPCWithMeleeHit(npcIndex)) nextItemTick[sender, npcIndex] = Main.GameUpdateCount + (ulong)remaining;
                npc.netUpdate = true;
                break;
            default: throw new InvalidDataException("未知元素网络消息。");
        }
    }

    internal static void PayReactionLife(Player player, int cost)
    {
        ApplyLifeCost(player, cost);
        if (Main.netMode != NetmodeID.Server) return;
        var packet = ModContent.GetInstance<ElementNetwork>().Mod.GetPacket();
        packet.Write((byte)Packet.ReactionLife); packet.Write(cost); packet.Send(player.whoAmI);
    }
    private static void ApplyLifeCost(Player player, int cost)
    {
        if (!player.active || player.dead) return;
        player.statLife = Math.Max(0, player.statLife - cost);
        if (player.statLife == 0 && Main.netMode != NetmodeID.Server)
            player.KillMe(PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromLiteral($"{player.name} 耗尽了反应所需的生命。")), cost, 0);
    }
    internal static void ShowReaction(Vector2 start, Vector2 end, TagEnum reaction)
    {
        if (Main.netMode != NetmodeID.Server) { DrawReaction(start, end, reaction); return; }
        var packet = ModContent.GetInstance<ElementNetwork>().Mod.GetPacket();
        packet.Write((byte)Packet.ReactionVisual);
        packet.Write(start.X); packet.Write(start.Y); packet.Write(end.X); packet.Write(end.Y); packet.Write((int)reaction);
        packet.Send();
    }
    private static void DrawReaction(Vector2 start, Vector2 end, TagEnum reaction)
    {
        int dustType = reaction == TagEnum.HydroElectro ? DustID.Electric : reaction == TagEnum.PytoCryo ? DustID.IceTorch : DustID.Water;
        const float spacing = 4;
        int count = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(start, end) / spacing));
        for (int i = 0; i <= count; i++)
        {
            var dust = Dust.NewDustPerfect(Vector2.Lerp(start, end, (float)i / count), dustType, Vector2.Zero);
            dust.noGravity = true;
        }
    }
}
