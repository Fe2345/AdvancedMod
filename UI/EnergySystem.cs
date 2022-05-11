using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace AdvancedMod.UI
{
    public class EnergySystem : UIState
    {
        public static bool Visible;
        public override void OnInitialize()
        {
            UIPanel Panel = new UIPanel();

            Panel.Width.Set(150f, 0f);
            Panel.Width.Set(30f, 0f);
            Panel.Left.Set(-350f, 0f);
            Panel.Top.Set(-10f, 0f);
        }
    }
}
