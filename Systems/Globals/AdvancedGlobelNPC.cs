using AdvancedMod.DamageClasses.Element.Adapters;
using AdvancedMod.DamageClasses.Element.State;
using AdvancedMod.Projectiles.Accessory;
using AdvancedMod.Systems.DataStructures;
using AdvancedMod.Utils;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod
{
    public class AdvancedGlobelNPC : GlobalNPC
    {
        private readonly NpcElementAdapter elementAdapter = new();

        //自定义NPC乘区
        public float Vulnerability = 0f; //易伤区
        //伤害抗性区
        public float MeleeResistance = 0f;
        public float RangedResistance = 0f;
        public float MagicResistance = 0f;
        public float SummonResistance = 0f;
        public float DotResistance = 0f;

        // 存储该NPC身上所有激活的DoT数据，Key为Buff的Type
        public Dictionary<int, Systems.DataStructures.DoTData> ActiveDoTs = new Dictionary<int, Systems.DataStructures.DoTData>();

        /// <summary>
        /// Gets the element adapter that owns this NPC's prototype thunder-wind state.
        /// </summary>
        public NpcElementAdapter ElementAdapter => elementAdapter;

        /// <summary>
        /// Gets the NPC-side element component used by the prototype system.
        /// </summary>
        public ElementComponent ElementComponent => elementAdapter.Component;
        public override bool InstancePerEntity
        {
            get
            {
                return true;
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(
        NPC npc,
        int numPlayers,
        float balance,
        float bossAdjustment
        )
        {
            
            if (!AdvancedWorld.MutationMode)
                return;

            if (!Main.GameModeInfo.IsExpertMode)
                return;

            if (npc.lifeMax <= 0)
                return;
            float targetLifeMultiplier = AdvancedWorld.EndMode ? 4.5f : 3.5f;
            float currentDifficultyLifeMultiplier = Main.GameModeInfo.EnemyMaxLifeMultiplier;

            if (currentDifficultyLifeMultiplier <= 0f
                || float.IsNaN(currentDifficultyLifeMultiplier)
                || float.IsInfinity(currentDifficultyLifeMultiplier))
            {
                throw new InvalidOperationException("当前游戏难度的敌怪生命倍率无效。");
            }

            float correctionMultiplier = targetLifeMultiplier / currentDifficultyLifeMultiplier;

            float finalMultiplier = correctionMultiplier;

            if (npc.boss)
                finalMultiplier *= balance * bossAdjustment;

            npc.lifeMax = ScaleLife(npc.lifeMax, finalMultiplier);
        }

        public override void SetDefaults(NPC npc)
        {
            
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            //LeadingConditionRule VModeRule = new LeadingConditionRule(new ItemDropRules.EndModeDropRule());
            //add npcloot
            
        }

        //DoT伤害生效的Hook
        public void ApplyDotDamage(NPC npc,ref int damage,ref int lifeRegen)
        {
            if (lifeRegen > 0) lifeRegen = 0;
            lifeRegen -= damage;
        }

        public override void ResetEffects(NPC npc)
        {
            // 每帧重置这些属性，以防随时间无限积累
            Vulnerability = 0f;
            MeleeResistance = 0f;
            RangedResistance = 0f;
            MagicResistance = 0f;
            SummonResistance = 0f;
            DotResistance = 0f;

            var keysToRemove = new List<int>();
            foreach (var key in ActiveDoTs.Keys)
            {
                if (!npc.HasBuff(key))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                ActiveDoTs.Remove(key);
            }
            UpdateDoTEffects(npc);
        }

        public void UpdateDoTEffects(NPC npc)
        {
            // 遍历所有生效的DoT并触发其每帧常驻效果
            foreach (var dot in ActiveDoTs.Values)
            {
                dot.UpdateEffect(npc);
            }
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            // 遍历所有生效的DoT并触发结算
            foreach (var dot in ActiveDoTs.Values)
            {
                dot.UpdateTick(npc, ref damage);
            }
            base.UpdateLifeRegen(npc, ref damage);
        }

        public void AddOrUpdateDoT(NPC npc, int buffType, int time, Systems.DataStructures.DoTData newData)
        {
            npc.AddBuff(buffType, time);

            if (ActiveDoTs.TryGetValue(buffType, out Systems.DataStructures.DoTData existingData))
            {
                existingData.AddStack(npc, newData);
            }
            else
            {
                ActiveDoTs[buffType] = newData;
                newData.OnApply(npc);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 centerAboveNpc = new(npc.Center.X - screenPos.X, npc.Top.Y - screenPos.Y - 34f);
            bool hasElementGauge = ElementComponent.Thunder.CurrentGauge > 0 ||
                ElementComponent.Wind.CurrentGauge > 0 ||
                ElementComponent.Pyro.CurrentGauge > 0;
            bool showReactionText = !string.IsNullOrWhiteSpace(ElementComponent.LastReactionText) &&
                Main.GameUpdateCount - ElementComponent.LastReactionTick <= 60;

            if (hasElementGauge)
            {
                string gaugeText = $"T:{ElementComponent.Thunder.CurrentGauge} W:{ElementComponent.Wind.CurrentGauge} P:{ElementComponent.Pyro.CurrentGauge}";
                Terraria.Utils.DrawBorderString(spriteBatch, gaugeText, centerAboveNpc, Color.White, 0.8f, 0.5f, 0f);
                centerAboveNpc.Y -= 18f;
            }

            if (showReactionText)
            {
                Terraria.Utils.DrawBorderString(spriteBatch, ElementComponent.LastReactionText, centerAboveNpc, Color.Gold, 0.9f, 0.5f, 0f);
            }

            if (ActiveDoTs.Count == 0) return;

            // 按Type分组，求出Stack总和，并选取该组的第一个BuffID用于获取其图标
            var groupedDoTs = ActiveDoTs
                .GroupBy(kvp => kvp.Value.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    TotalStacks = g.Sum(kvp => kvp.Value.Stacks),
                    RepresentativeBuffId = g.First().Key
                })
                .ToList();

            if (groupedDoTs.Count == 0) return;

            float iconSpacing = 28f; // 图标之间的横向间距
            float startY = npc.Top.Y - screenPos.Y - Specials.DotStacksUIOffset(npc); // 在NPC头顶偏上位置绘制
            float totalWidth = groupedDoTs.Count * iconSpacing;
            float startX = npc.Center.X - screenPos.X - (totalWidth / 2f) + (iconSpacing / 2f);

            for (int i = 0; i < groupedDoTs.Count; i++)
            {
                var group = groupedDoTs[i];
                Vector2 drawPos = new Vector2(startX + i * iconSpacing, startY);

                Texture2D buffTexture = null;
                switch (group.Type)
                {
                    case DoTData.DoTType.Anemo:
                        buffTexture = ModContent.Request<Texture2D>("AdvancedMod/Assets/Textures/Buffs/Weathering").Value;
                        break;
                    case DoTData.DoTType.Electro:
                        buffTexture = ModContent.Request<Texture2D>("AdvancedMod/Assets/Textures/Buffs/Electric").Value;
                        break;
                    default:
                        buffTexture = ModContent.Request<Texture2D>("AdvancedMod/Assets/Textures/Buffs/Weathering").Value; // 默认图标
                        break;
                }
                Vector2 origin = buffTexture.Size() / 2f;

                // 绘制Debuff图标：略微半透明，且缩放适配防止过大
                float scale = 0.8f; 
                spriteBatch.Draw(buffTexture, drawPos, null, Color.White * 0.9f, 0f, origin, scale, SpriteEffects.None, 0f);

                // 绘制层数数字：图标正下方
                string stackText = group.TotalStacks.ToString();
                Vector2 textPos = drawPos + new Vector2(0, 14f);
                // 使用描边文字以防背景太亮看不清
                Terraria.Utils.DrawBorderString(spriteBatch, stackText, textPos, Color.White, 0.8f, 0.5f, 0f);
            }
        }

        public override void OnKill(NPC npc)
        {

            //如果附近存在InfiniteCrystalProj召唤物，则收集DoTData转送给召唤物
            var minion = AdvancedExtensions.GetProjInDistance(npc, ModContent.GetInstance<InfiniteCrystalProj>(),1000);
            if (minion != null)
            {
                
            }
        }

        private static int ScaleLife(int currentLifeMax, float multiplier)
        {
            if (currentLifeMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentLifeMax), "NPC 当前生命值必须大于 0。");

            if (multiplier <= 0f || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
                throw new ArgumentOutOfRangeException(nameof(multiplier), "生命倍率必须是大于 0 的有限数。");

            double scaled = currentLifeMax * (double)multiplier;

            if (scaled > int.MaxValue)
                throw new OverflowException("NPC 生命值超过 int.MaxValue。");

            return Math.Max(1, (int)Math.Round(scaled, MidpointRounding.AwayFromZero));
        }
    }
}
