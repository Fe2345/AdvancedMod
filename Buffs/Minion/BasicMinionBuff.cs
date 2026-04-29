using AdvancedMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Buffs.Minion
{
    /// <summary>
    /// An abstract class that gives the baseline code needed to make a buff dedicated to a minion.
    /// </summary>
    public abstract class BasicMinionBuff : ModBuff
    {
        //必须实现：召唤物本体的弹幕
        protected abstract int MinionProjectileType { get; }

        //必须实现：控制召唤物存货的bool变量。
        protected abstract ref bool MinionBool { get; }

        //Buff所属的玩家
        protected Player BuffOwner { get; private set; }

        
        protected AdvancedPlayer BuffModdedOwner { get; private set; }

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            BuffOwner = player;
            BuffModdedOwner = player.Advanced();

            if (player.ownedProjectileCounts[MinionProjectileType] > 0)
                MinionBool = true;

            if (!MinionBool)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }

            else
                player.buffTime[buffIndex] = 36000; //没死就重置为10min，维持存活
        }
    }
}
