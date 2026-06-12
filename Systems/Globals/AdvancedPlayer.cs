using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Terraria.GameInput;
using Terraria.ID;
using AdvancedMod.DamageClasses.Element.Players;
using AdvancedMod.Utils;
using System.Collections.Generic;
using SubworldLibrary;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public static int Energy;
        public static int UsedSiliconHeartCount;

        public int UsedStardropCount;
        public bool RecievedInitBag;
        public bool RecievedBoss1Bag;
        public bool RecievedBoss2Bag;
        public bool RecievedBoss3Bag;
        public bool RecievedFleshWallBag;
        public bool RecievedMechBossBag;
        public bool RecievedPlanteraBag;
        public bool RecievedMoonlordBag;
        public bool RecievedGodOfEyeBag;
        public bool RecievedGodOfTimeBag;

        public static float Electricity;

        //accessories
        public bool InfiniteCrystal;
        public bool PrototypeForceWindElement;
        public bool PrototypeReactionExtraWindBurst;

        //virtual values
        public int negativeLife;
        public int extraLife;

        /// <summary>
        /// 玩家在当前帧聚合出的元素战斗数值状态。
        /// </summary>
        public ElementCombatPlayerState ElementCombatState { get; } = new();

        public static Vector2 DeathPosition;

        public static int TimeStopLeft;
        public static int TimeStopCD;

        //summons
        public bool MiniDiagrammer;

        public override void SaveData(TagCompound tag)
        {
            tag.Add("Energy", Energy);
            tag.Add("UsedSiliconHeartCount", UsedSiliconHeartCount);
            tag.Add("UsedStardropCount", UsedStardropCount);

            List<string> Bags = new List<string>();
            if (RecievedInitBag) Bags.Add("RecievedInitBag");
            if (RecievedBoss1Bag) Bags.Add("RecievedBoss1Bag");
            if (RecievedBoss2Bag) Bags.Add("RecievedBoss2Bag");
            if (RecievedBoss3Bag) Bags.Add("RecievedBoss3Bag");
            if (RecievedFleshWallBag) Bags.Add("RecievedFleshWallBag");
            if (RecievedMechBossBag) Bags.Add("RecievedMechBossBag");
            if (RecievedPlanteraBag) Bags.Add("RecievedPlanteraBag");
            if (RecievedMoonlordBag) Bags.Add("RecievedMoonlordBag");
            if (RecievedGodOfEyeBag) Bags.Add("RecievedGodOfEyeBag");
            if (RecievedGodOfTimeBag) Bags.Add("RecievedGodOfTimeBag");

            tag.Add("Bags", Bags);
        }

        public override void LoadData(TagCompound tag)
        {
            Energy = tag.GetInt("Energy");
            UsedSiliconHeartCount = tag.GetInt("UsedSiliconHeartCount");
            UsedStardropCount = tag.GetInt("UsedStardropCount");
            IList<string> Bags = tag.GetList<string>("Bags");
            RecievedInitBag = Bags.Contains("RecievedInitBag");
            RecievedBoss1Bag = Bags.Contains("RecievedBoss1Bag");
            RecievedBoss2Bag = Bags.Contains("RecievedBoss2Bag");
            RecievedBoss3Bag = Bags.Contains("RecievedBoss3Bag");
            RecievedFleshWallBag = Bags.Contains("RecievedFleshWallBag");
            RecievedMechBossBag = Bags.Contains("RecievedMechBossBag");
            RecievedPlanteraBag = Bags.Contains("RecievedPlanteraBag");
            RecievedMoonlordBag = Bags.Contains("RecievedMoonlordBag");
            RecievedGodOfEyeBag = Bags.Contains("RecievedGodOfEyeBag");
            RecievedGodOfTimeBag = Bags.Contains("RecievedGodOfTimeBag");
        }

        public override void ResetEffects()
        {
            MiniDiagrammer = false;
            PrototypeForceWindElement = false;
            PrototypeReactionExtraWindBurst = false;
            ElementCombatState.Reset();
        }

        public override void PostUpdateEquips()
        {
            ElementCombatState.CollectFromEquipment(Player);
        }

        public override void PostUpdate()
        {
            
        }
    }
}
