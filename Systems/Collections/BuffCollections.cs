using ReLogic.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Systems.Collections
{
    [ReinitializeDuringResizeArrays]
    public static class BuffCollections
    {
        /*
        public static SetFactory Factory = new SetFactory(BuffLoader.BuffCount, "AdvancedMod/BuffID", Search);
        public static IdDictionary Search = IdDictionary.Create<BuffID, int>();

        public static bool[] BuffedByAmalgam = Factory.CreateBoolSet(BuffID.ObsidianSkin, BuffID.Regeneration, BuffID.Swiftness, BuffID.Gills, BuffID.Ironskin, BuffID.ManaRegeneration,
                BuffID.MagicPower, BuffID.Featherfall, BuffID.Spelunker, BuffID.Invisibility, BuffID.Shine, BuffID.NightOwl, BuffID.Battle, BuffID.Thorns, BuffID.WaterWalking,
                BuffID.Archery, BuffID.Hunter, BuffID.Gravitation, BuffID.Tipsy, BuffID.WellFed, BuffID.WellFed2, BuffID.WellFed3, BuffID.Honey, BuffID.WeaponImbueVenom,
                BuffID.WeaponImbueCursedFlames, BuffID.WeaponImbueFire, BuffID.WeaponImbueGold, BuffID.WeaponImbueIchor, BuffID.WeaponImbueNanites, BuffID.WeaponImbueConfetti,
                BuffID.WeaponImbuePoison, BuffID.Lucky, BuffID.Mining, BuffID.Heartreach, BuffID.Calm, BuffID.Builder, BuffID.Titan, BuffID.Flipper, BuffID.Summoning, BuffID.Dangersense,
                BuffID.AmmoReservation, BuffID.Lifeforce, BuffID.Endurance, BuffID.Rage, BuffID.Inferno, BuffID.Wrath, BuffID.Lovestruck, BuffID.Stinky, BuffID.Fishing, BuffID.Sonar,
                BuffID.Crate, BuffID.Warmth, BuffID.SugarRush);

        /// <summary>
        /// If <see langword="true"/> for a buff type, then that buff is a persistent buff.<br/>
        /// Used by The Amalgam to prevent removing their persistence when the accessory is unequipped.<br/>
        /// Defaults to <see langword="false"/>.
        /// </summary>
        public static bool[] IsPersistentBuff = Factory.CreateBoolSet(BuffID.WeaponImbueVenom, BuffID.WeaponImbueCursedFlames, BuffID.WeaponImbueFire, BuffID.WeaponImbueGold,
                BuffID.WeaponImbueIchor, BuffID.WeaponImbueNanites, BuffID.WeaponImbueConfetti, BuffID.WeaponImbuePoison);

        /// <summary>
        /// If <see langword="true"/> for a buff type, then that buff is considered to be a debuff.<br/>
        /// This general-purpose set has several different uses, including reducing buff duration with Radiance, Crown Jewel and its upgrades' debuff effects, and removing debuffs with Cleansing Jelly and its upgrades' auras.<br/>
        /// Defaults to <see langword="false"/>.
        /// </summary>
        public static bool[] IsDebuff = Factory.CreateBoolSet(BuffID.Poisoned, BuffID.Darkness, BuffID.Cursed, BuffID.OnFire, BuffID.Bleeding, BuffID.Confused, BuffID.Slow, BuffID.Weak,
                BuffID.Silenced, BuffID.BrokenArmor, BuffID.CursedInferno, BuffID.Frostburn, BuffID.Chilled, BuffID.Frozen, BuffID.Burning, BuffID.Suffocation, BuffID.Ichor,
                BuffID.Venom, BuffID.Blackout, BuffID.Electrified, BuffID.Rabies, BuffID.Webbed, BuffID.Stoned, BuffID.Dazed, BuffID.VortexDebuff, BuffID.WitheredArmor, BuffID.WitheredWeapon,
                BuffID.OgreSpit, BuffID.BetsysCurse);

        /// <summary>
        /// Used for identify if it's a DoT debuff, which is a type of debuff that applies damage over time to the target. This is used for several different effects, such as applying the effect of the DoT debuff to NPCs, preventing "whip stacking" on NPCs, and drawing the DoT debuff icon below the NPC.<br/>
        /// Transfer Terraria Debuff(which have an effect of DoT) to DoT:TODO
        /// </summary>
        public static bool[] isDot = Factory.CreateBoolSet();

        /// <summary>
        /// If <see langword="true"/> for a buff type, then that buff is a whip tag buff on the player.<br/>
        /// Used to prevent "whip stacking", the ability to grant the player multiple whip buff effects at once.<br/>
        /// Defaults to <see langword="false"/>.
        /// </summary>
        public static bool[] IsSummonTagBuff = Factory.CreateBoolSet(BuffID.CoolWhipPlayerBuff, BuffID.ScytheWhipPlayerBuff, BuffID.SwordWhipPlayerBuff, BuffID.ThornWhipPlayerBuff, BuffType<ProfanedCrystalWhipBuff>());

        /// <summary>
        /// Associates a buff type with its <see cref="SummonTag"/> structure. If a buff type is not a key in this dictionary, then it has no associated <see cref="SummonTag"/>.<br/>
        /// Used for several different effects, such as applying tag effects to NPCs, preventing "whip stacking" on NPCs, and drawing the tag effect icon below the NPC. 
        /// </summary>
        public static Dictionary<int, SummonTag> SummonTagDebuff = new Dictionary<int, SummonTag>
        {
            { BuffID.BlandWhipEnemyDebuff, SummonTag.LeatherWhip },
            { BuffID.BoneWhipNPCDebuff, SummonTag.SpinalTap },
            { BuffID.CoolWhipNPCDebuff, SummonTag.CoolWhip },
            { BuffID.FlameWhipEnemyDebuff, SummonTag.Firecracker },
            { BuffID.MaceWhipNPCDebuff, SummonTag.MorningStar },
            { BuffID.RainbowWhipNPCDebuff, SummonTag.Kaleidoscope },
            { BuffID.ScytheWhipEnemyDebuff, SummonTag.DarkHarvest },
            { BuffID.SwordWhipNPCDebuff, SummonTag.Durendal },
            { BuffID.ThornWhipNPCDebuff, SummonTag.Snapthorn },
            { BuffType<ProfanedCrystalWhipDebuff>(), ProfanedSoulCrystal.SummonTag }
        };

        /// <summary>
        /// Associates a buff type with its alcohol poison level. Used for the Alcohol Poisoning mechanic.<br/>
        /// If a buff type is not a key in this dictionary, then it does not increase alcohol poison level.
        /// </summary>
        public static Dictionary<int, int> AlcoholStrength = new Dictionary<int, int>
        {
            { BuffID.Tipsy, 1 },
            { BuffType<BloodyMaryBuff>(), 1 },
            { BuffType<CaribbeanRumBuff>(), 1 },
            { BuffType<CinnamonRollBuff>(), 1 },
            { BuffType<EverclearBuff>(), 2 },
            { BuffType<EvergreenGinBuff>(), 1 },
            { BuffType<FireballBuff>(), 1 },
            { BuffType<GrapeBeerBuff>(), 1 },
            { BuffType<MargaritaBuff>(), 1 },
            { BuffType<MoonshineBuff>(), 1 },
            { BuffType<MoscowMuleBuff>(), 1 },
            { BuffType<OldFashionedBuff>(), 1 },
            { BuffType<PurpleHazeBuff>(), 1 },
            { BuffType<RedWineBuff>(), 1 },
            { BuffType<RumBuff>(), 1 },
            { BuffType<ScrewdriverBuff>(), 1 },
            { BuffType<StarBeamRyeBuff>(), 1 },
            { BuffType<TequilaBuff>(), 1 },
            { BuffType<TequilaSunriseBuff>(), 1 },
            { BuffType<VodkaBuff>(), 1 },
            { BuffType<WhiskeyBuff>(), 1 },
            { BuffType<WhiteWineBuff>(), 1 }
        };
        */
    }
}
