#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Calculate.DamageOverTime;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离服务器与两个客户端的真实网络显示验收。</summary>
public sealed class DotNetworkTests : ModSystem
{
    private const int Marker = 9133;
    private string? role;
    private int elapsed, doneTicks, initialLife;
    private NPC? target;
    private bool submitted, reported;
    private readonly List<(Color Color, int Amount)> npcText = new();
    private readonly List<(Color Color, int Amount)> playerText = new();
    public override void Load()
    {
        role = Environment.GetEnvironmentVariable("ADVANCEDMOD_DOT_NET_TEST");
        if (string.IsNullOrEmpty(role)) return;
        On_Player.LoadPlayer += Rename;
        On_CombatText.NewText_Rectangle_Color_int_bool_bool += Observe;
    }
    public override void Unload()
    {
        On_Player.LoadPlayer -= Rename;
        On_CombatText.NewText_Rectangle_Color_int_bool_bool -= Observe;
    }
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    {
        var data = orig(path, cloud); data.Player.name = "DotNet-" + role; return data;
    }
    private int Observe(On_CombatText.orig_NewText_Rectangle_Color_int_bool_bool orig,
        Rectangle bounds, Color color, int amount, bool critical, bool dot)
    {
        if (dot && amount > 0)
        {
            var npc = Main.npc.FirstOrDefault(n => n.active && n.ai[2] == Marker);
            if (npc is not null && Vector2.DistanceSquared(bounds.Center.ToVector2(), npc.Center) < 16)
                npcText.Add((color, amount));
            else if (color == ElementTextSystem.ColorFor(TagEnum.Hydro) || color == ElementTextSystem.ColorFor(TagEnum.Cryo))
                playerText.Add((color, amount));
        }
        return orig(bounds, color, amount, critical, dot);
    }
    private static AttackProfile Profile(string id, TagEnum tag) => new(new TagSet(new[] { new TagEntry(false, tag) }), id);
    public override void PostUpdateEverything()
    {
        if (string.IsNullOrEmpty(role) || reported || Main.gameMenu) return;
        Main.autoSave = false;
        if (Main.netMode == NetmodeID.Server)
        {
            if (target is null)
            {
                if (Main.player.Count(p => p.active && p.name.StartsWith("DotNet-", StringComparison.Ordinal)) < 2) return;
                int index = NPC.NewNPC(new EntitySource_Misc("持续伤害联机验收"), Main.spawnTileX * 16 + 240, Main.spawnTileY * 16 - 180, ModContent.NPCType<ElementProbeNPC>());
                target = Main.npc[index]; target.ai[2] = Marker; initialLife = target.life;
                target.lifeRegenCount = -720;
                target.AddBuff(BuffID.Poisoned, 120);
                DotService.TrySubmit(CombatContext.ForDamageOverTime(target, Profile("联机火", TagEnum.Pyro)), new(6), out _);
                DotService.TrySubmit(CombatContext.ForDamageOverTime(target, Profile("联机雷", TagEnum.Electro)), new(4), out _);
                NetMessage.SendData(MessageID.NPCBuffs, number: index);
                return;
            }
            if (++elapsed < 240) return;
            target.ai[0] = 1; target.netUpdate = true;
            Save(new { role, npcDamage = initialLife - target.life, managed = target.GetGlobalNPC<DotNpc>().ManagedText });
        }
        else
        {
            Main.LocalPlayer.immune = true; Main.LocalPlayer.immuneTime = 600;
            var npc = Main.npc.FirstOrDefault(n => n.active && n.ai[2] == Marker);
            if (npc is null) return;
            if (role == "client1" && !submitted)
            {
                submitted = true;
                var player = Main.LocalPlayer; player.lifeRegenCount = -240;
                DotService.TrySubmit(CombatContext.ForDamageOverTime(player, Profile("联机水", TagEnum.Hydro)), new(20), out _);
                DotService.TrySubmit(CombatContext.ForDamageOverTime(player, Profile("联机冰", TagEnum.Cryo)), new(30), out _);
            }
            if (npc.ai[0] != 1 || ++doneTicks < 60) return;
            int Sum(List<(Color Color, int Amount)> values, TagEnum tag) => values.Where(v => v.Color == ElementTextSystem.ColorFor(tag)).Sum(v => v.Amount);
            Save(new { role, npcTotal = npcText.Sum(t => t.Amount), pyro = Sum(npcText, TagEnum.Pyro), electro = Sum(npcText, TagEnum.Electro),
                hydro = Sum(playerText, TagEnum.Hydro), cryo = Sum(playerText, TagEnum.Cryo),
                pyroCount = npcText.Count(t => t.Color == ElementTextSystem.ColorFor(TagEnum.Pyro)),
                electroCount = npcText.Count(t => t.Color == ElementTextSystem.ColorFor(TagEnum.Electro)),
                managed = npc.GetGlobalNPC<DotNpc>().ManagedText });
        }
    }
    private void Save(object report)
    {
        string path = Path.Combine(Main.SavePath, "Logs", "AdvancedMod", "dot-network.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        reported = true;
    }
}
