using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mount
{
    public class FishOutOfWaterMount : ModMount
    {
        public override void SetStaticDefaults()
        {
            MountData.runningFrameCount = 8;
            MountData.runningFrameDelay = 12;
            MountData.runningFrameStart = 0;
            MountData.heightBoost = 5;
            MountData.fallDamage = 0;
            MountData.runSpeed = 20;
            MountData.acceleration = 0.5f;
            MountData.blockExtraJumps = true;
            MountData.totalFrames = 8;
            MountData.usesHover = true;
            /*
            if (Main.netMode != NetmodeID.Server)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
                //mountData.textureWidth = mountData.frontTexture.Width;
                //mountData.textureHeight = mountData.frontTexture.Height;
            }
            */
        }

        public override void UpdateEffects(Player player)
        {
            float MaxFallVel = 15f;
            float DownAccelerate = 0.5f;
            float MaxUpVel = 15f;
            float UpAccelerate = 0.5f;
            player.gravity = 0; //玩家没有重力
            player.maxFallSpeed = MaxFallVel; //控制下落最大速度
                                          //当按下“下”方向键时，如果Y轴速度没有达到最大则持续增加Y轴速度
            if (player.controlDown && player.velocity.Y <= MaxFallVel)
            {
                //这个值一般为零点几，大于一的加速度就已经很夸张了
                player.velocity.Y += DownAccelerate;
            }
            else if ((player.controlUp || player.controlJump) && player.velocity.Y >= -MaxUpVel)
            {
                //当按下“上”方向键或者“跳跃”时，如果Y轴速度没有达到最高速度则持续增加
                player.velocity.Y -= UpAccelerate; //同理
            }
            else if (player.velocity.Y > 1.5) player.velocity.Y--; //不控制方向时自动开始停车
            else if (player.velocity.Y < -1.5) player.velocity.Y++;
            else
            {
                player.velocity.Y = 0; //速度低于一定程度时则停止运动
                                       //player.fullRotation
            }
            player.fallStart = (int)(player.position.Y / 16f);//让你的坐骑拥有一个合理的坠落判定
        }
    }
}
