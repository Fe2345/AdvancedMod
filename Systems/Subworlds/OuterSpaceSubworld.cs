using System;
using System.Collections.Generic;
using AdvancedMod.Biomes.OuterSpace;
using SubworldLibrary;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace AdvancedMod.Systems.Subworlds
{
    public sealed class OuterSpaceSubworld : Subworld
    {
        private const int AsteroidCount = 8;

        public override int Width => Main.maxTilesX;

        public override int Height => Main.maxTilesY;

        public override bool ShouldSave => false;

        public override List<GenPass> Tasks => new()
        {
            new PassLegacy("清空外太空", ClearWorld),
            new PassLegacy("生成小行星带", GenerateAsteroidBelt)
        };

        public override void OnEnter()
        {
            SubworldSystem.hideUnderworld = true;
        }

        private static void ClearWorld(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Clearing outer space";

            for (int x = 0; x < Main.maxTilesX; x++)
            {
                progress.Value = x / (double)Main.maxTilesX * 0.5;

                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Main.tile[x, y].ClearEverything();
                }
            }
        }

        private static void GenerateAsteroidBelt(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Forming asteroid belt";

            int segmentWidth = Math.Max(1, Main.maxTilesX / AsteroidCount);
            int baseY = Math.Max(80, (int)(Main.maxTilesY * 0.2f));
            int yJitter = Math.Max(12, (int)(Main.maxTilesY * 0.03f));
            int xJitter = Math.Max(8, segmentWidth / 6);

            for (int asteroidIndex = 0; asteroidIndex < AsteroidCount; asteroidIndex++)
            {
                progress.Value = 0.5 + asteroidIndex / (double)AsteroidCount * 0.5;

                int segmentCenterX = segmentWidth * asteroidIndex + segmentWidth / 2;
                int centerX = segmentCenterX + WorldGen.genRand.Next(-xJitter, xJitter + 1);
                int centerY = baseY + WorldGen.genRand.Next(-yJitter, yJitter + 1);

                centerX = Math.Clamp(centerX, 80, Main.maxTilesX - 80);
                centerY = Math.Clamp(centerY, 80, Main.maxTilesY - 80);

                AsteroidGenSystem.GenerateRandomAsteroidAt(centerX, centerY);
            }
        }
    }
}
