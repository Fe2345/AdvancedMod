using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ObjectData;

namespace AdvancedMod.Tiles
{
	public class ElectromagneticWorkStation : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = false;//是否是实体物块
			Main.tileSolidTop[Type] = false;//是否是上半部分可踩踏
			Main.tileTable[Type] = false;//是否和桌子相同

			//只在虚体方块时可以使用
			Main.tileLavaDeath[Type] = true;//岩浆破坏
			Main.tileWaterDeath[Type] = true;//水破坏

			Main.tileNoAttach[Type] = true;//防止平铺
			Main.tileCut[Type] = false;//武器摧毁

			Main.tileFrameImportant[Type] = true;//常用
			Main.tileMergeDirt[Type] = true;//是否可以和泥土合并

			Main.tileBlockLight[Type] = true;//光线可以穿过
			Main.tileLighted[Type] = false;//自主发光

			ModTranslation name = CreateMapEntryName();
			name.SetDefault("电磁操作台");
			AddMapEntry(new Color(100, 200, 200), name);

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.Width = 4;
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);

			AdjTiles = new int[] { TileID.WorkBenches,TileID.HeavyWorkBench,TileID.Furnaces,TileID.Hellforge,
									TileID.Anvils,TileID.Bottles,TileID.AlchemyTable,TileID.Sawmill,TileID.Loom,
									TileID.Tables,TileID.Chairs,TileID.CookingPots,TileID.TinkerersWorkbench,
									TileID.ImbuingStation,TileID.DyeVat,TileID.DemonAltar,TileID.LihzahrdAltar,
									TileID.MythrilAnvil,TileID.AdamantiteForge,TileID.Bookcases
			};

			TileID.Sets.CountsAsWaterSource[Type] = true;
			TileID.Sets.CountsAsHoneySource[Type] = true;
			TileID.Sets.CountsAsLavaSource[Type] = true;

			MinPick = 100;
			MineResist = 4f;

			//TileID.Sets.Ore[Type] = true;//设置为矿石
			//Main.tileSpelunker[Type] = true; // 被洞穴探险药水高亮表示
			//Main.tileValue[Type] = 410; // 在金属探测仪中的优先级

			
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.5f;
			g = 0.5f;
			b = 0.5f;
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY)
		{
			Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 16, ModContent.ItemType<Items.Tiles_Item.ElectromagneticWorkStationItem>());
		}
	}
}