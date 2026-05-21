using AdvancedMod.Items.TileItem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AdvancedMod.Tiles
{
    public class ElectromagneticWorkStation : ModTile
    {
        public override string Texture => $"{Mod.Name}/Assets/Textures/Tiles/ElectromagneticWorkStation";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileSolidTop[Type] = false;
            Main.tileTable[Type] = false;

            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = true;

            Main.tileNoAttach[Type] = true;
            Main.tileCut[Type] = false;

            Main.tileFrameImportant[Type] = true;
            Main.tileMergeDirt[Type] = true;

            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;

            AddMapEntry(new Color(100, 200, 200), CreateMapEntryName());

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
            TileObjectData.addTile(Type);

            AdjTiles = new int[]
            {
                TileID.WorkBenches,
                TileID.HeavyWorkBench,
                TileID.Furnaces,
                TileID.Hellforge,
                TileID.Anvils,
                TileID.Bottles,
                TileID.AlchemyTable,
                TileID.Sawmill,
                TileID.Loom,
                TileID.Tables,
                TileID.Chairs,
                TileID.CookingPots,
                TileID.TinkerersWorkbench,
                TileID.ImbuingStation,
                TileID.DyeVat,
                TileID.DemonAltar,
                TileID.LihzahrdAltar,
                TileID.MythrilAnvil,
                TileID.AdamantiteForge,
                TileID.Bookcases
            };

            TileID.Sets.CountsAsWaterSource[Type] = true;
            TileID.Sets.CountsAsHoneySource[Type] = true;
            TileID.Sets.CountsAsLavaSource[Type] = true;

            MinPick = 100;
            MineResist = 4f;

            RegisterItemDrop(ModContent.ItemType<ElectromagneticWorkStationItem>());
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.5f;
            g = 0.5f;
            b = 0.5f;
        }
    }
}