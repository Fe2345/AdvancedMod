using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace AdvancedMod.Biomes.OuterSpace
{
    public static class AsteroidGenSystem
    {
        private const int StructurePadding = 20;

        public static void GenerateAsteroidAt(int centerX, int centerY, int radiusX, int radiusY, int asteroidSeed)
        {
            Rectangle area = new Rectangle(
                centerX - radiusX - StructurePadding,
                centerY - radiusY - StructurePadding,
                radiusX * 2 + StructurePadding * 2,
                radiusY * 2 + StructurePadding * 2
            );

            GenerateAsteroidBody(centerX, centerY, radiusX, radiusY, asteroidSeed);
            GenerateOreVeins(centerX, centerY, radiusX, radiusY, asteroidSeed);
            FrameArea(area);
        }

        public static void GenerateRandomAsteroidAt(int centerX, int centerY)
        {
            int baseRadius = WorldGen.genRand.Next(110, 190);

            int radiusX = baseRadius + WorldGen.genRand.Next(-15, 20);
            int radiusY = baseRadius + WorldGen.genRand.Next(-15, 20);
            int asteroidSeed = WorldGen.genRand.Next();

            GenerateAsteroidAt(centerX, centerY, radiusX, radiusY, asteroidSeed);
        }

        private static void GenerateAsteroidBody(int centerX, int centerY, int radiusX, int radiusY, int asteroidSeed)
        {
            int rockTile = TileID.Stone;
            int soilTile = TileID.Dirt;

            int minX = centerX - radiusX - 4;
            int maxX = centerX + radiusX + 4;
            int minY = centerY - radiusY - 4;
            int maxY = centerY + radiusY + 4;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!WorldGen.InWorld(x, y, 20))
                    {
                        continue;
                    }

                    double shapeValue = GetAsteroidShapeValue(x, y, centerX, centerY, radiusX, radiusY, asteroidSeed);

                    if (shapeValue > 1.0)
                    {
                        continue;
                    }

                    double edgeDepth = 1.0 - shapeValue;
                    int tileType = edgeDepth < 0.18 ? soilTile : rockTile;

                    PlaceSolidTile(x, y, tileType);
                }
            }

            RoughenOuterShell(centerX, centerY, radiusX, radiusY, asteroidSeed);
        }

        private static double GetAsteroidShapeValue(
            int x,
            int y,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            int asteroidSeed
        )
        {
            double dx = (x - centerX) / (double)radiusX;
            double dy = (y - centerY) / (double)radiusY;

            double angle = Math.Atan2(dy, dx);

            double largeNoise =
                Math.Sin(angle * 3.0 + asteroidSeed * 0.00031) * 0.035 +
                Math.Sin(angle * 5.0 + asteroidSeed * 0.00073) * 0.025 +
                Math.Sin(angle * 8.0 + asteroidSeed * 0.00110) * 0.015;

            double localNoise = HashSigned(x / 5, y / 5, asteroidSeed) * 0.012;

            double allowedRadius = 1.0 + largeNoise + localNoise;
            allowedRadius = ClampDouble(allowedRadius, 0.92, 1.08);

            double normalizedDistance = Math.Sqrt(dx * dx + dy * dy);

            return normalizedDistance / allowedRadius;
        }

        private static double ClampDouble(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static void RoughenOuterShell(int centerX, int centerY, int radiusX, int radiusY, int asteroidSeed)
        {
            int soilTile = TileID.Dirt;

            int minX = centerX - radiusX - 5;
            int maxX = centerX + radiusX + 5;
            int minY = centerY - radiusY - 5;
            int maxY = centerY + radiusY + 5;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!WorldGen.InWorld(x, y, 20))
                    {
                        continue;
                    }

                    Tile tile = Main.tile[x, y];

                    if (!tile.HasTile || tile.TileType != soilTile)
                    {
                        continue;
                    }

                    int exposedSides = CountAdjacentAir(x, y);

                    if (exposedSides >= 3 && Hash01(x, y, asteroidSeed + 99173) < 0.38)
                    {
                        KillTileWithoutItem(x, y);
                    }
                }
            }
        }

        private static int CountAdjacentAir(int x, int y)
        {
            int count = 0;

            if (!Main.tile[x - 1, y].HasTile)
            {
                count++;
            }

            if (!Main.tile[x + 1, y].HasTile)
            {
                count++;
            }

            if (!Main.tile[x, y - 1].HasTile)
            {
                count++;
            }

            if (!Main.tile[x, y + 1].HasTile)
            {
                count++;
            }

            return count;
        }

        private static void GenerateOreVeins(int centerX, int centerY, int radiusX, int radiusY, int asteroidSeed)
        {
            int veinCount = Math.Max(1, (radiusX * radiusY) / 420);

            for (int veinIndex = 0; veinIndex < veinCount; veinIndex++)
            {
                Point16 start = FindInteriorRockPoint(centerX, centerY, radiusX, radiusY, asteroidSeed + veinIndex * 1009);

                if (start == Point16.NegativeOne)
                {
                    continue;
                }

                int length = WorldGen.genRand.Next(18, 46);
                double angle = WorldGen.genRand.NextDouble() * MathHelper.TwoPi;

                CarveOreVein(start.X, start.Y, centerX, centerY, radiusX, radiusY, length, angle, asteroidSeed + veinIndex * 8191);
            }
        }

        private static Point16 FindInteriorRockPoint(int centerX, int centerY, int radiusX, int radiusY, int seed)
        {
            int rockTile = TileID.Stone;

            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = WorldGen.genRand.Next(centerX - radiusX / 2, centerX + radiusX / 2 + 1);
                int y = WorldGen.genRand.Next(centerY - radiusY / 2, centerY + radiusY / 2 + 1);

                if (!WorldGen.InWorld(x, y, 20))
                {
                    continue;
                }

                double shapeValue = GetAsteroidShapeValue(x, y, centerX, centerY, radiusX, radiusY, seed);

                if (shapeValue >= 0.68)
                {
                    continue;
                }

                Tile tile = Main.tile[x, y];

                if (tile.HasTile && tile.TileType == rockTile)
                {
                    return new Point16(x, y);
                }
            }

            return Point16.NegativeOne;
        }

        private static void CarveOreVein(
            int startX,
            int startY,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            int length,
            double angle,
            int seed
        )
        {
            int rockTile = TileID.Stone;
            int oreTile = TileID.LunarOre;

            double x = startX;
            double y = startY;

            for (int step = 0; step < length; step++)
            {
                int tileX = (int)Math.Round(x);
                int tileY = (int)Math.Round(y);

                int brushRadius = Hash01(tileX, tileY, seed) < 0.7 ? 1 : 2;

                for (int offsetX = -brushRadius; offsetX <= brushRadius; offsetX++)
                {
                    for (int offsetY = -brushRadius; offsetY <= brushRadius; offsetY++)
                    {
                        int targetX = tileX + offsetX;
                        int targetY = tileY + offsetY;

                        if (!WorldGen.InWorld(targetX, targetY, 20))
                        {
                            continue;
                        }

                        if (offsetX * offsetX + offsetY * offsetY > brushRadius * brushRadius)
                        {
                            continue;
                        }

                        double shapeValue = GetAsteroidShapeValue(targetX, targetY, centerX, centerY, radiusX, radiusY, seed);

                        if (shapeValue >= 0.72)
                        {
                            continue;
                        }

                        Tile tile = Main.tile[targetX, targetY];

                        if (tile.HasTile && tile.TileType == rockTile)
                        {
                            tile.TileType = (ushort)oreTile;
                        }
                    }
                }

                angle += HashSigned(tileX, tileY, seed + step * 13) * 0.55;

                x += Math.Cos(angle);
                y += Math.Sin(angle);
            }
        }

        private static void PlaceSolidTile(int x, int y, int tileType)
        {
            Tile tile = Main.tile[x, y];

            tile.HasTile = true;
            tile.TileType = (ushort)tileType;
            tile.Slope = SlopeType.Solid;
            tile.IsHalfBlock = false;
            tile.TileFrameX = 0;
            tile.TileFrameY = 0;
            tile.LiquidAmount = 0;
        }

        private static void KillTileWithoutItem(int x, int y)
        {
            Tile tile = Main.tile[x, y];

            tile.HasTile = false;
            tile.TileType = TileID.Dirt;
            tile.Slope = SlopeType.Solid;
            tile.IsHalfBlock = false;
            tile.TileFrameX = 0;
            tile.TileFrameY = 0;
            tile.LiquidAmount = 0;
        }

        private static void FrameArea(Rectangle area)
        {
            int left = Math.Max(1, area.Left - 2);
            int right = Math.Min(Main.maxTilesX - 2, area.Right + 2);
            int top = Math.Max(1, area.Top - 2);
            int bottom = Math.Min(Main.maxTilesY - 2, area.Bottom + 2);

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    WorldGen.SquareTileFrame(x, y, true);
                    WorldGen.SquareWallFrame(x, y, true);
                }
            }
        }

        private static double Hash01(int x, int y, int seed)
        {
            unchecked
            {
                int hash = seed;
                hash ^= x * 374761393;
                hash ^= y * 668265263;
                hash = (hash ^ (hash >> 13)) * 1274126177;
                hash ^= hash >> 16;

                uint unsignedHash = (uint)hash;
                return unsignedHash / (double)uint.MaxValue;
            }
        }

        private static double HashSigned(int x, int y, int seed)
        {
            return Hash01(x, y, seed) * 2.0 - 1.0;
        }
    }
}
