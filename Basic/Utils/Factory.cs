
using System;
using System.Text;
using Terraria.Utilities;


namespace Basic.Utils
{
    public static class WorldRandomFactory
    {
        public static UnifiedRandom Create(
            int worldSeed,
            string systemKey,
            int objectX = 0,
            int objectY = 0)
        {
            if (string.IsNullOrWhiteSpace(systemKey))
                throw new ArgumentException(
                    "System key must not be empty.",
                    nameof(systemKey));

            uint hash = 2166136261u;

            hash = MixInt(hash, worldSeed);

            byte[] keyBytes = Encoding.UTF8.GetBytes(systemKey);
            hash = MixInt(hash, keyBytes.Length);

            unchecked
            {
                foreach (byte b in keyBytes)
                {
                    hash ^= b;
                    hash *= 16777619u;
                }
            }

            hash = MixInt(hash, objectX);
            hash = MixInt(hash, objectY);

            return new UnifiedRandom(unchecked((int)hash));
        }

        private static uint MixInt(uint hash, int input)
        {
            uint value = unchecked((uint)input);

            unchecked
            {
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (byte)(value >> (8 * i));
                    hash *= 16777619u;
                }
            }

            return hash;
        }
    }
}

