// 2026-10-26：元素伤害数字与反应文字；沿用原版 CombatText 字体及绘制。
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element.Presentation;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Element.Tml;

public sealed class ElementTextSystem : ModSystem
{
    private sealed class Pending(Entity target, ReactionPlan plan)
    {
        internal readonly Entity Target = target;
        internal readonly Rectangle Bounds = target.Hitbox;
        internal readonly ReactionPlan Plan = plan;
        internal int Damage;
        internal bool Crit;
    }
    private static readonly Dictionary<long, Pending> pending = new();
    private static readonly ReactionTextLayout layout = new();
    private static readonly Dictionary<CombatText, ReactionTextLayout.Label> labels = new();
    private static (Player Player, TagEnum? Element)? hurtScope;

    public override void Load()
    {
        ElementHitSystem.Resolved += Complete;
        On_CombatText.Update += UpdateText;
        On_CombatText.NewText_Rectangle_Color_string_bool_bool += NewText;
        On_CombatText.NewText_Rectangle_Color_int_bool_bool += DamageText;
        On_Player.Hurt_HurtInfo_bool += PlayerHurt;
    }
    public override void Unload()
    {
        ElementHitSystem.Resolved -= Complete;
        On_CombatText.Update -= UpdateText;
        On_CombatText.NewText_Rectangle_Color_string_bool_bool -= NewText;
        On_CombatText.NewText_Rectangle_Color_int_bool_bool -= DamageText;
        On_Player.Hurt_HurtInfo_bool -= PlayerHurt;
        Clear();
    }
    public override void OnWorldUnload() => Clear();
    private static void Clear()
    {
        foreach (var text in labels.Keys) text.active = false;
        labels.Clear(); layout.Clear(); pending.Clear(); hurtScope = null;
    }

    internal static void Prepare(CombatContext context, ReactionPlan plan)
    {
        Entity target = (Entity?)context.TargetNpc ?? context.TargetPlayer!;
        pending.Add(plan.Hit.HitId, new Pending(target, plan));
    }

    internal static void PrepareNpc(CombatContext context, ReactionPlan plan, ref NPC.HitModifiers modifiers)
    {
        Prepare(context, plan);
        if (!ElementTextStyle.SelectElement(plan.Hit.Tags, plan.Definition?.Tag).HasValue) return;
        Pending entry = pending[plan.Hit.HitId];
        NPC npc = context.TargetNpc!;
        modifiers.ModifyHitInfo += (ref NPC.HitInfo hit) =>
        {
            if (hit.HideCombatText || hit.InstantKill || npc.lifeMax <= 1 || npc.HideStrikeDamage) return;
            entry.Damage = hit.Damage;
            entry.Crit = hit.Crit;
            // 此标志由原版 StrikeNPC 网络包同步，客户端不会再生成第二份白字。
            hit.HideCombatText = true;
        };
    }

    private static void Complete(ElementResolution result)
    {
        if (!pending.Remove(result.Plan.Hit.HitId, out var entry) || !result.Committed) return;
        TagEnum? element = ElementTextStyle.SelectElement(result.Plan.Hit.Tags, result.Plan.Definition?.Tag);
        if (element is null) return;
        var reactions = result.Presentations.Select(p => p.ReactionTag).ToArray();
        ElementNetwork.ShowElementText(entry.Bounds, entry.Damage, entry.Crit, element.Value, reactions,
            entry.Target is Player player ? player.whoAmI : -1);
    }

    private static void PlayerHurt(On_Player.orig_Hurt_HurtInfo_bool orig, Player player, Player.HurtInfo info, bool quiet)
    {
        var previous = hurtScope;
        TagEnum? element = null;
        if (ElementHitSystem.CurrentPlayerTextPlan(player) is { } plan)
            element = ElementTextStyle.SelectElement(plan.Hit.Tags, plan.Definition?.Tag);
        else if (info.DamageSource.TryGetCausingEntity(out Entity source))
        {
            var tags = source switch
            {
                NPC npc => npc.GetGlobalNPC<CombatGlobalNPC>().Profile.Tags,
                Projectile projectile => projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags,
                _ => TagSet.Empty
            };
            element = ElementTextStyle.SelectElement(tags);
        }
        hurtScope = (player, element);
        try { orig(player, info, quiet); }
        finally { hurtScope = previous; }
    }

    private static int DamageText(On_CombatText.orig_NewText_Rectangle_Color_int_bool_bool orig,
        Rectangle rectangle, Color color, int amount, bool dramatic, bool dot)
    {
        if (hurtScope is { Element: { } element } scope && rectangle == scope.Player.Hitbox &&
            (color == CombatText.DamagedFriendly || color == CombatText.DamagedFriendlyCrit)) color = ColorFor(element);
        return orig(rectangle, color, amount, dramatic, dot);
    }

    private static int NewText(On_CombatText.orig_NewText_Rectangle_Color_string_bool_bool orig,
        Rectangle rectangle, Color color, string text, bool dramatic, bool dot)
    {
        int index = orig(rectangle, color, text, dramatic, dot);
        // 原版池复用时撤销旧标签，不能误把新伤害数字当作反应文字。
        if (index >= 0 && index < Main.combatText.Length && labels.Remove(Main.combatText[index], out var old)) layout.Remove(old);
        return index;
    }

    private static void UpdateText(On_CombatText.orig_Update orig, CombatText text)
    {
        if (!labels.TryGetValue(text, out var label)) { orig(text); return; }
        text.lifeTime = --label.RemainingTicks;
        if (label.RemainingTicks <= 0)
        {
            text.active = false; labels.Remove(text); layout.Remove(label);
        }
    }

    internal static Color ColorFor(TagEnum element)
    {
        var rgb = ElementTextStyle.Rgb(element);
        return new Color(rgb.R, rgb.G, rgb.B);
    }

    internal static void Show(Rectangle bounds, int damage, bool crit, TagEnum element, TagEnum[] reactions)
    {
        if (Main.dedServ) return;
        Color color = ColorFor(element);
        if (damage > 0) CombatText.NewText(bounds, color, damage, crit);
        foreach (var reaction in reactions)
        {
            string name = Language.GetTextValue(ElementTextStyle.ReactionKey(reaction));
            Vector2 size = FontAssets.CombatText[0].Value.MeasureString(name);
            float x = bounds.Left - ReactionTextLayout.TargetGap - size.X / 2, y = bounds.Center.Y;
            // 被其他代码清除的原版文字不应继续占用排斥区。
            foreach (var stale in labels.Where(p => !p.Key.active).ToArray())
            { labels.Remove(stale.Key); layout.Remove(stale.Value); }
            if (!layout.CanPlace(x, y)) continue;
            int index = CombatText.NewText(new Rectangle((int)x, (int)y, 0, 0), color, name);
            if (index < 0 || index >= Main.combatText.Length) continue; // 原版文字池已满。
            var label = layout.Add(x, y, Main.rand.NextFloat());
            CombatText text = Main.combatText[index];
            text.position = new Vector2(x, y) - size / 2;
            text.velocity = Vector2.Zero;
            text.rotation = label.Rotation;
            text.lifeTime = ReactionTextLayout.LifetimeTicks;
            text.alpha = text.scale = 1f;
            labels.Add(text, label);
        }
    }
}
