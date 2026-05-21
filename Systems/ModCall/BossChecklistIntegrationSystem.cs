using AdvancedMod.Items.Summon;
using AdvancedMod.NPCs.Boss.TreeDiagrammer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AdvancedMod.Systems.ModCall
{
    public class BossChecklistIntegrationSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            RegisterBossChecklistEntries();
        }

        private void RegisterBossChecklistEntries()
        {
            if (!ModLoader.TryGetMod("BossChecklist", out Mod bossChecklistMod))
            {
                return;
            }

            if (bossChecklistMod.Version < new Version(1, 6))
            {
                return;
            }

            string internalName = nameof(TreeDiagrammer);

            float progression = 11.5f;

            Func<bool> downed = () => AdvancedWorld.downedTreeDiagrammer;

            int bossType = ModContent.NPCType<TreeDiagrammer>();

            int spawnItem = ModContent.ItemType<SuspiciousCommunicator>();

            List<int> collectibles = new List<int>
            {
                //ModContent.ItemType<SpaceBossRelic>(),
                //ModContent.ItemType<SpaceBossTrophy>(),
                //ModContent.ItemType<SpaceBossPetItem>()
            };

            var customPortrait = (SpriteBatch spriteBatch, Rectangle rectangle, Color color) =>
            {
                Texture2D texture = ModContent.Request<Texture2D>("AdvancedMod/Assets/Textures/NPCs/TreeDiagrammer").Value;

                Vector2 position = new Vector2(
                    rectangle.X + rectangle.Width / 2f - texture.Width / 2f,
                    rectangle.Y + rectangle.Height / 2f - texture.Height / 2f
                );

                spriteBatch.Draw(texture, position, color);
            };

            bossChecklistMod.Call(
                "LogBoss",
                Mod,
                internalName,
                progression,
                downed,
                bossType,
                new Dictionary<string, object>
                {
                    ["spawnItems"] = spawnItem,
                    ["collectibles"] = collectibles,
                    ["spawnInfo"] = Language.GetText("Mods.AdvancedMod.BossChecklist.TreeDiagrammer.SpawnInfo"),
                    ["despawnMessage"] = Language.GetText("Mods.AdvancedMod.BossChecklist.TreeDiagrammer.DespawnMessage").WithFormatArgs(internalName),
                    ["customPortrait"] = customPortrait
                }
            );
        }
    }
}