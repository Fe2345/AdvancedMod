using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedMod.Utils.Render
{
    /// <summary>
    /// A general collection of points in Terraria's overall draw order that graphical systems in the mod draw to.
    /// </summary>
    [Flags]
    public enum BasicDrawLayer
    {
        BeforeAllTiles = 1 << 0,
        BeforeSolidTiles = 1 << 1,
        BeforeNPCs = 1 << 2,
        AfterNPCs = 1 << 3,
        BeforeProjectiles = 1 << 4,
        AfterProjectiles = 1 << 5,
        AfterPlayers = 1 << 6,
        AfterDusts = 1 << 7,
        AfterEverything = 1 << 8,
    }
}
