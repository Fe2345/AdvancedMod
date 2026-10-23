#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>真实 TCP 服务器／客户端验收；仅显式环境变量开启，使用隔离存档。</summary>
public sealed class ElementNetworkTests : ModSystem
{
    private const int Marker = 9123;
    private string? role;
    private int ticks;
    private bool spawned, shot, reported;
    private int target = -1;
    private int idleTarget = -1, meleeTarget = -1;
    private bool hurtPassed, cancelPassed, meleeRequested;
    private int commits, reactions;
    private int damageCalls;
    private string projectileEvidence = "未观察到弹幕";
    public override void Load()
    {
        role = Environment.GetEnvironmentVariable("ADVANCEDMOD_ELEMENT_NET_TEST");
        if (string.IsNullOrEmpty(role)) return;
        On_Player.LoadPlayer += Rename;
        On_Projectile.Damage += ObserveDamage;
        ElementHitSystem.Resolved += Observe;
    }
    public override void Unload()
    {
        On_Player.LoadPlayer -= Rename;
        On_Projectile.Damage -= ObserveDamage;
        ElementHitSystem.Resolved -= Observe;
    }
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    {
        var data = orig(path, cloud);
        data.Player.name = "ElementNet-" + role;
        return data;
    }
    private void Observe(Basic.Element.ElementResolution resolution)
    {
        if (Main.netMode != NetmodeID.Server || !resolution.Committed) return;
        commits++;
        if (resolution.Outcome is not null) reactions++;
    }
    private void ObserveDamage(On_Projectile.orig_Damage orig, Projectile p)
    {
        if (p.type == ModContent.ProjectileType<ElementProbeProjectile>())
        {
            damageCalls++;
            var data = p.GetGlobalProjectile<CombatGlobalProjectile>();
            projectileEvidence = $"owner={p.owner}, managed={data.Profile.IsManagedAttack}, amount={data.ElementAmount}, damage={p.damage}, position={p.Center}, friendly={p.friendly}";
        }
        orig(p);
    }
    public override void PostUpdateEverything()
    {
        if (string.IsNullOrEmpty(role) || reported || Main.gameMenu) return;
        Main.autoSave = false;
        if (Main.netMode == NetmodeID.Server)
        {
            if (!spawned)
            {
                if (Main.player.Where(p => p.active).Count() < 2) return;
                var position = Main.player.Where(p => p.active).First().Center + new Vector2(160, -200);
                target = NPC.NewNPC(new EntitySource_Misc("元素联机自动验收"), (int)position.X, (int)position.Y,
                    ModContent.NPCType<ElementProbeNPC>());
                var npc = Main.npc[target]; npc.ai[0] = Marker;
                npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(3, 1, 100));
                ElementAttacks.ConfigureNpcContact(npc, new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }), 1);
                npc.netUpdate = true;
                idleTarget = SpawnTarget(position + new Vector2(100, 0), Marker + 1);
                Main.npc[idleTarget].GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(1, 1.5, 100));
                Main.npc[idleTarget].GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(4, 1, 100));
                meleeTarget = SpawnTarget(position + new Vector2(200, 0), Marker + 2);
                Main.npc[meleeTarget].GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(3, 1, 100));
                spawned = true;
            }
            if (++ticks < 480) return;
            var n = Main.npc[target];
            var s = n.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot;
            Report(new Dictionary<string, bool>
            {
                ["两个客户端真实 TCP 接入"] = Main.player.Where(p => p.active).Count() == 2,
                ["服务器共四次提交三次反应"] = commits == 4 && reactions == 3,
                ["共享库存及 CD：总伤害300"] = n.lifeMax - n.life == 300 && s.Elements[2].Total == 1 && s.Elements[3].Total == 0 && s.ReactionCooldowns[1] == 0,
                ["玩家所属端状态同步服务器"] = Main.player.Where(p => p.active).Any(p => p.GetModPlayer<CombatPlayer>().Elements.Snapshot.Elements[1].Total == 0.75),
                ["服务器静置反应只扣100"] = Main.npc[idleTarget].lifeMax - Main.npc[idleTarget].life == 100,
                ["客户端近战请求服务器结算200"] = Main.npc[meleeTarget].lifeMax - Main.npc[meleeTarget].life == 200,
                ["服务器穿透耗尽弹幕已销毁"] = !Main.projectile.Any(p => p.active && p.type == ModContent.ProjectileType<ElementProbeProjectile>())
            }, new { life = n.life, commits, reactions, damageCalls, projectileEvidence, targetPosition = n.Center.ToString() });
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var n = Main.npc.Where(n => n.active).FirstOrDefault(n => n.type == ModContent.NPCType<ElementProbeNPC>() && n.ai[0] == Marker);
            if (n is null) return;
            if (!Main.npc.Any(q => q.active && q.ai[0] == Marker + 2)) return;
            if (!shot)
            {
                shot = true;
                int index = ElementAttacks.NewProjectile(new EntitySource_Misc("元素联机客户端攻击"), n.Center,
                    Vector2.Zero, ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0,
                    new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }), 1, Main.myPlayer);
                var p = Main.projectile[index]; p.penetrate = p.maxPenetrate = 1; p.timeLeft = 600; p.netUpdate = true;
                if (role == "client1")
                {
                    var player = Main.LocalPlayer;
                    player.immune = false; Array.Clear(player.hurtCooldowns); player.creativeGodMode = false;
                    player.statDefense = default; player.statLife = player.statLifeMax2 = 5000; player.dead = false;
                    player.GetModPlayer<CombatPlayer>().Elements.Attach(new(3, 1, 100));
                    ElementProbePlayer.Cancel = true;
                    cancelPassed = player.Hurt(PlayerDeathReason.ByNPC(n.whoAmI), 100, 0) == 0 && player.GetModPlayer<CombatPlayer>().Elements.Snapshot.Elements[3].Total == 1;
                    ElementProbePlayer.Cancel = false;
                    hurtPassed = player.Hurt(PlayerDeathReason.ByNPC(n.whoAmI), 100, 0, dodgeable: false) > 0 && player.GetModPlayer<CombatPlayer>().Elements.Snapshot.Elements[3].Total == 0;
                    player.GetModPlayer<CombatPlayer>().Elements.Attach(new(1, 0.75, 123));
                    player.inventory[0] = new Item(ModContent.ItemType<ElementProbeSword>());
                    player.selectedItem = 0;
                    NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: 0);
                    var melee = Main.npc.First(q => q.active && q.ai[0] == Marker + 2);
                    player.Center = melee.Center - new Vector2(28, 0); player.velocity = Vector2.Zero;
                    player.controlUseItem = true;
                    NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
                }
            }
            if (role == "client1" && ticks == 12 && !meleeRequested)
            {
                meleeRequested = true;
                var player = Main.LocalPlayer;
                var melee = Main.npc.First(q => q.active && q.ai[0] == Marker + 2);
                player.Center = melee.Center - new Vector2(28, 0); player.itemAnimation = 20;
                NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
                typeof(Player).GetMethod("ProcessHitAgainstNPC", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player,
                    new object[] { player.HeldItem, melee.Hitbox, 100, 0f, melee.whoAmI });
                player.controlUseItem = false;
                NetMessage.SendData(MessageID.PlayerControls, number: player.whoAmI);
            }
            if (++ticks < 600) return;
            var state = n.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot;
            Report(new Dictionary<string, bool>
            {
                ["NPC 血量镜像300伤害"] = n.lifeMax - n.life == 300,
                ["NPC 附着与 CD 镜像"] = state.Elements[2].Total == 1 && state.Elements[3].Total == 0 && state.ReactionCooldowns[1] == 0,
                ["实体身份同步"] = n.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity > 0,
                ["玩家状态跨客户端同步"] = Main.player.Where(p => p.active).Any(p => p.GetModPlayer<CombatPlayer>().Elements.Snapshot.Elements[1].Portions.TryGetValue(123, out double value) && value == 0.75),
                ["服务器销毁弹幕同步到客户端"] = !Main.projectile.Any(p => p.active && p.type == ModContent.ProjectileType<ElementProbeProjectile>()),
                ["静置反应血量镜像"] = Main.npc.Any(q => q.active && q.ai[0] == Marker + 1 && q.lifeMax - q.life == 100),
                ["近战伤害镜像"] = Main.npc.Any(q => q.active && q.ai[0] == Marker + 2 && q.lifeMax - q.life == 200),
                ["所属玩家受伤与取消边界"] = role != "client1" || hurtPassed && cancelPassed
            }, new { life = n.life, localPlayer = Main.myPlayer, damageCalls, projectileEvidence });
        }
    }
    private static int SpawnTarget(Vector2 position, int marker)
    {
        int index = NPC.NewNPC(new EntitySource_Misc("联机扩展测试"), (int)position.X, (int)position.Y, ModContent.NPCType<ElementProbeNPC>());
        Main.npc[index].ai[0] = marker;
        Main.npc[index].netUpdate = true;
        return index;
    }
    private void Report(Dictionary<string, bool> checks, object details)
    {
        reported = true;
        if (role != "client1") checks.Remove("所属玩家受伤与取消边界");
        string folder = Path.Combine(Main.SavePath, "Logs", "AdvancedMod"); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "element-network-tests.json"), JsonSerializer.Serialize(new
        {
            role, utc = DateTimeOffset.UtcNow, mode = Main.netMode, details,
            results = checks.Select(c => new { name = c.Key, status = c.Value ? "PASS" : "FAIL" })
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
