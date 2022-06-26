using Terraria.ModLoader;
using Terraria;

namespace AdvancedMod.Items.Tiles_Item.Relics
{
    public class TreeDiagrammerRelicItem : BaseRelic
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者圣物");
        }

        protected override int TileType => ModContent.TileType<Tiles.Relics.TreeDiagrammerRelic>();
    }
}
