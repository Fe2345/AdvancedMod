using System.Collections.Generic;
using System.Linq;
using AdvancedMod.Utils;
using AdvancedMod.Utils.Render;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Systems.Graphics.Render
{
    //基本绘制系统
    public class RenderSystem : ModSystem
    {
        #region Fields/Properties
        private static RenderTargetLease PixelationTarget_BeforeAllTiles;

        private static RenderTargetLease PixelationTarget_BeforeSolidTiles;

        private static RenderTargetLease PixelationTarget_BeforeNPCs;

        private static RenderTargetLease PixelationTarget_AfterNPCs;

        private static RenderTargetLease PixelationTarget_BeforeProjectiles;

        private static RenderTargetLease PixelationTarget_AfterProjectiles;

        private static RenderTargetLease PixelationTarget_AfterPlayers;

        private static RenderTargetLease PixelationTarget_AfterDusts;

        private static RenderTargetLease PixelationTarget_AfterEverything;

        /// <summary>
        /// Whether the system is currently rendering any primitives.
        /// </summary>
        public static bool CurrentlyRendering
        {
            get;
            private set;
        }
        #endregion

        #region Loading
        public override void Load()
        {
            if (Main.dedServ)
                return;

            Main.QueueMainThreadAction(() =>
            {
                PixelationTarget_BeforeAllTiles = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_BeforeSolidTiles = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_BeforeNPCs = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_AfterNPCs = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_BeforeProjectiles = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_AfterProjectiles = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_AfterPlayers = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_AfterDusts = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
                PixelationTarget_AfterEverything = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (w, h) => (w / 2, h / 2));
            });

            BasicDrawLayerSystem.OnDrawLayer += DrawTargetScaled;
            BasicDrawLayerSystem.OnPrepareDraw += DrawToTargets;
            //On_Main.DrawBackgroundBlackFill += DrawTarget_BeforeAllTiles;
            //On_Main.DoDraw_Tiles_Solid += DrawTarget_BeforeSolidTiles;
            //On_Main.DoDraw_DrawNPCsOverTiles += DrawTarget_NPCs;
            //On_Main.DrawProjectiles += DrawTarget_Projectiles;
            //On_Main.DrawPlayers_AfterProjectiles += DrawTarget_Players;
            //On_Main.DrawDust += DrawTarget_AfterDusts;
            //On_Main.DrawInfernoRings += DrawTarget_AfterEverything;
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            BasicDrawLayerSystem.OnDrawLayer -= DrawTargetScaled;
            BasicDrawLayerSystem.OnPrepareDraw -= DrawToTargets;
        }
        #endregion

        #region Drawing To Targets
        private void DrawToTargets()
        {
            if (Main.gameMenu)
            {
                return;
            }

            var beforeAllTiles = new List<IPixelRender>();
            var beforeSolidTiles = new List<IPixelRender>();
            var beforeNPCs = new List<IPixelRender>();
            var afterNPCs = new List<IPixelRender>();
            var beforeProjectiles = new List<IPixelRender>();
            var afterProjectiles = new List<IPixelRender>();
            var afterPlayers = new List<IPixelRender>();
            var afterDusts = new List<IPixelRender>();
            var afterEverything = new List<IPixelRender>();

            // Check every active projectile.
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                // If the projectile is active, a mod projectile, and uses the interface, add it to the list of primitives to draw this frame.
                if (projectile.ModProjectile != null && projectile.ModProjectile is IPixelRender pixelPrimitiveProjectile)
                {
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeAllTiles))
                        beforeAllTiles.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeSolidTiles))
                        beforeSolidTiles.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeNPCs))
                        beforeNPCs.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterNPCs))
                        afterNPCs.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeProjectiles))
                        beforeProjectiles.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterProjectiles))
                        afterProjectiles.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterPlayers))
                        afterPlayers.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterDusts))
                        afterDusts.Add(pixelPrimitiveProjectile);
                    if (pixelPrimitiveProjectile.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterEverything))
                        afterEverything.Add(pixelPrimitiveProjectile);
                }
            }

            // Check every active NPC.
            foreach (NPC npc in Main.ActiveNPCs)
            {
                // If the NPC is active, a mod NPC, and uses the interface, add it to the list of primitives to draw this frame.
                if (npc.ModNPC != null && npc.ModNPC is IPixelRender pixelPrimitiveNPC)
                {
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeAllTiles))
                        beforeAllTiles.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeSolidTiles))
                        beforeSolidTiles.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeNPCs))
                        beforeNPCs.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterNPCs))
                        afterNPCs.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.BeforeProjectiles))
                        beforeProjectiles.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterProjectiles))
                        afterProjectiles.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterPlayers))
                        afterPlayers.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterDusts))
                        afterDusts.Add(pixelPrimitiveNPC);
                    if (pixelPrimitiveNPC.LayerToRenderTo.HasFlag(BasicDrawLayer.AfterEverything))
                        afterEverything.Add(pixelPrimitiveNPC);
                }
            }

            CurrentlyRendering = true;

            DrawPrimsToRenderTarget(PixelationTarget_BeforeAllTiles.Target, BasicDrawLayer.BeforeAllTiles, beforeAllTiles);
            DrawPrimsToRenderTarget(PixelationTarget_BeforeSolidTiles.Target, BasicDrawLayer.BeforeSolidTiles, beforeSolidTiles);
            DrawPrimsToRenderTarget(PixelationTarget_BeforeNPCs.Target, BasicDrawLayer.BeforeNPCs, beforeNPCs);
            DrawPrimsToRenderTarget(PixelationTarget_AfterNPCs.Target, BasicDrawLayer.AfterNPCs, afterNPCs);
            DrawPrimsToRenderTarget(PixelationTarget_BeforeProjectiles.Target, BasicDrawLayer.BeforeProjectiles, beforeProjectiles);
            DrawPrimsToRenderTarget(PixelationTarget_AfterProjectiles.Target, BasicDrawLayer.AfterProjectiles, afterProjectiles);
            DrawPrimsToRenderTarget(PixelationTarget_AfterPlayers.Target, BasicDrawLayer.AfterPlayers, afterPlayers);
            DrawPrimsToRenderTarget(PixelationTarget_AfterDusts.Target, BasicDrawLayer.AfterDusts, afterDusts);
            DrawPrimsToRenderTarget(PixelationTarget_AfterEverything.Target, BasicDrawLayer.AfterEverything, afterEverything);

            CurrentlyRendering = false;
        }

        private static void DrawPrimsToRenderTarget(RenderTarget2D renderTarget, BasicDrawLayer layer, List<IPixelRender> pixelPrimitives)
        {
            // Swap to the target regardless, in order to clear any leftover content from last frame. Not doing this results in the final frame lingering once it stops rendering.

            using (renderTarget.Scope(clearColor: Color.Transparent))
            {
                if (pixelPrimitives.Any())
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null);

                    foreach (var pixelPrimitiveDrawer in pixelPrimitives)
                        pixelPrimitiveDrawer.RenderPixelatedPrimitives(Main.spriteBatch, layer);

                    Main.spriteBatch.End();
                }
            }
        }
        #endregion

        #region Target Drawing
        private static void DrawTargetScaled(BasicDrawLayer drawLayer)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(ReturnAssociatedRenderTarget(drawLayer), Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        private static RenderTarget2D ReturnAssociatedRenderTarget(BasicDrawLayer drawLayer)
        {
            return drawLayer switch
            {
                BasicDrawLayer.BeforeAllTiles => PixelationTarget_BeforeAllTiles.Target,
                BasicDrawLayer.BeforeSolidTiles => PixelationTarget_BeforeSolidTiles.Target,
                BasicDrawLayer.BeforeNPCs => PixelationTarget_BeforeNPCs.Target,
                BasicDrawLayer.AfterNPCs => PixelationTarget_AfterNPCs.Target,
                BasicDrawLayer.BeforeProjectiles => PixelationTarget_BeforeProjectiles.Target,
                BasicDrawLayer.AfterProjectiles => PixelationTarget_AfterProjectiles.Target,
                BasicDrawLayer.AfterPlayers => PixelationTarget_AfterPlayers.Target,
                BasicDrawLayer.AfterDusts => PixelationTarget_AfterDusts.Target,
                _ => PixelationTarget_AfterEverything.Target
            };
        }
        #endregion
    }
}

