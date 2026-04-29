using System;
using Terraria;
using Terraria.ModLoader;
using AdvancedMod.Utils.Render;
using AdvancedMod.Utils.Sprite;

namespace AdvancedMod.Systems.Graphics
{
    //基础绘制层级系统，注入各类绘制的钩子
    internal sealed class BasicDrawLayerSystem : ModSystem
    {
        public static event Action OnPrepareDraw;
        public static event Action<BasicDrawLayer> OnDrawLayer;
        public static event Action<BasicDrawLayer> OnDrawLayerLate;

        public static event Action OnBeforeAllTiles;
        public static event Action OnBeforeSolidTiles;
        public static event Action OnBeforeNPCs;
        public static event Action OnAfterNPCs;
        public static event Action OnBeforeProjectiles;
        public static event Action OnAfterProjectiles;
        public static event Action OnAfterPlayers;
        public static event Action OnAfterDusts;
        public static event Action OnAfterEverything;

        public override void OnModLoad()
        {
            On_Main.CheckMonoliths += CheckMonoliths;
            On_Main.DrawBackgroundBlackFill += BasicDrawLayer_DrawToLayer_BeforeAllTiles;
            On_Main.DoDraw_Tiles_Solid += BasicDrawLayer_DrawToLayer_BeforeSolidTiles;
            On_Main.DoDraw_DrawNPCsOverTiles += BasicDrawLayer_DrawToLayer_NPCs;
            On_Main.DrawProjectiles += BasicDrawLayer_DrawToLayer_Projectiles;
            On_Main.DrawPlayers_AfterProjectiles += BasicDrawLayer_DrawToLayer_AfterPlayers;
            On_Main.DrawDust += BasicDrawLayer_DrawToLayer_AfterDusts;
            On_Main.DrawInfernoRings += BasicDrawLayer_DrawToLayer_AfterEverything;
        }

        public override void Unload()
        {
            OnPrepareDraw = null;
            OnDrawLayer = null;
            OnDrawLayerLate = null;

            OnBeforeAllTiles = null;
            OnBeforeSolidTiles = null;
            OnBeforeNPCs = null;
            OnAfterNPCs = null;
            OnBeforeProjectiles = null;
            OnAfterProjectiles = null;
            OnAfterPlayers = null;
            OnAfterDusts = null;
            OnAfterEverything = null;
        }

        private static void CheckMonoliths(On_Main.orig_CheckMonoliths orig)
        {
            orig();
            OnPrepareDraw?.Invoke();
        }

        #region BasicDrawLayer Systems Drawing
        private static void BasicDrawLayer_DrawForLayer(BasicDrawLayer drawLayer)
        {
            OnDrawLayer?.Invoke(drawLayer);
            OnDrawLayerLate?.Invoke(drawLayer);

            var callback = drawLayer switch
            {
                BasicDrawLayer.BeforeAllTiles => OnBeforeAllTiles,
                BasicDrawLayer.BeforeSolidTiles => OnBeforeSolidTiles,
                BasicDrawLayer.BeforeNPCs => OnBeforeNPCs,
                BasicDrawLayer.AfterNPCs => OnAfterNPCs,
                BasicDrawLayer.BeforeProjectiles => OnBeforeProjectiles,
                BasicDrawLayer.AfterProjectiles => OnAfterProjectiles,
                BasicDrawLayer.AfterPlayers => OnAfterPlayers,
                BasicDrawLayer.AfterDusts => OnAfterDusts,
                BasicDrawLayer.AfterEverything => OnAfterEverything,
                _ => null
            };
            callback?.Invoke();
        }

        private static void BasicDrawLayer_DrawToLayer_BeforeAllTiles(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
        {
            Main.spriteBatch.End(out var ss);
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.BeforeAllTiles);
            Main.spriteBatch.Begin(ss);
            orig(self);
        }

        private static void BasicDrawLayer_DrawToLayer_BeforeSolidTiles(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
        {
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.BeforeSolidTiles);
            orig(self);
        }

        private static void BasicDrawLayer_DrawToLayer_NPCs(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.BeforeNPCs);
            orig(self);
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.AfterNPCs);
        }

        private static void BasicDrawLayer_DrawToLayer_Projectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.BeforeProjectiles);
            orig(self);
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.AfterProjectiles);
        }

        private static void BasicDrawLayer_DrawToLayer_AfterPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
        {
            orig(self);
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.AfterPlayers);
        }

        private static void BasicDrawLayer_DrawToLayer_AfterDusts(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.AfterDusts);
        }

        private static void BasicDrawLayer_DrawToLayer_AfterEverything(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            orig(self);
            Main.spriteBatch.End(out var ss);
            BasicDrawLayer_DrawForLayer(BasicDrawLayer.AfterEverything);
            Main.spriteBatch.Begin(ss);
        }
        #endregion
    }
}
