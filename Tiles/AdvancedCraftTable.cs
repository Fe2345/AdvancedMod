﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Tiles
{
	public class AdvancedCraftTable : ModTile
	{
		public override void SetDefaults()
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
			name.SetDefault("先进锻造台");
			AddMapEntry(new Color(100, 200, 200), name);

			minPick = 100;
			mineResist = 4f;

			soundType = SoundID.Tink;

			//TileID.Sets.Ore[Type] = true;//设置为矿石
			//Main.tileSpelunker[Type] = true; // 被洞穴探险药水高亮表示
			//Main.tileValue[Type] = 410; // 在金属探测仪中的优先级

			drop = ModContent.ItemType<Items.Tiles_Item.AdvancedCraftTable_Item>();
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.5f;
			g = 0.5f;
			b = 0.5f;
		}
	}
}