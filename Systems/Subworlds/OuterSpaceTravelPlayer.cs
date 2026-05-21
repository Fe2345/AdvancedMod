using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Systems.Subworlds
{
    public sealed class OuterSpaceTravelPlayer : ModPlayer
    {
        private const float TopEnterWorldY = 16f;
        private const int BottomExitTilePadding = 10;
        private const int TransitionLockTicks = 300;
        private const float MainWorldReturnY = 64f;

        private int transitionLockTimer;
        private bool returningFromOuterSpace;

        public override void OnEnterWorld()
        {
            transitionLockTimer = 60;

            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (!SubworldSystem.AnyActive() && returningFromOuterSpace)
            {
                returningFromOuterSpace = false;
                Player.Center = new Vector2(Player.Center.X, MainWorldReturnY);
                Player.velocity = Vector2.Zero;
            }
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.gameMenu || Player.dead)
            {
                return;
            }

            if (transitionLockTimer > 0)
            {
                transitionLockTimer--;
                return;
            }

            if (SubworldSystem.IsActive<OuterSpaceSubworld>())
            {
                ExitOuterSpaceAtBottom();
                return;
            }

            EnterOuterSpaceAtTop();
        }

        private void EnterOuterSpaceAtTop()
        {
            if (SubworldSystem.AnyActive())
            {
                return;
            }

            if (!Player.ZoneSkyHeight)
            {
                return;
            }

            transitionLockTimer = TransitionLockTicks;
            SubworldSystem.Enter<OuterSpaceSubworld>();
        }

        private void ExitOuterSpaceAtBottom()
        {
            float bottomExitWorldY = (Main.maxTilesY - BottomExitTilePadding) * 16f;

            if (Player.Bottom.Y < bottomExitWorldY)
            {
                return;
            }

            returningFromOuterSpace = true;
            transitionLockTimer = TransitionLockTicks;
            SubworldSystem.Exit();
        }
    }
}
