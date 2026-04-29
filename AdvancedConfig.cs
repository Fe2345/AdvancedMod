using AdvancedMod.Utils;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace AdvancedMod
{
    [BackgroundColor(49, 32, 36, 216)]
    public class AdvancedConfig : ModConfig
    {
        public static AdvancedConfig Instance;

        public override ConfigScope Mode => ConfigScope.ClientSide;

        #region Graphics Changes
        [Header("Graphics")]

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(false)]
        public bool DisableGravityScreenSwap { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool TextEffects { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool Afterimages { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(false)]
        public bool Photosensitivity { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool EnableVanillaTextureEdits { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool SunkenSeaBackgroundDistortion { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool FancyBackgroundVisuals { get; set; }

        private const int MinParticleLimit = 500;
        private const int MaxParticleLimit = 10000;

        [BackgroundColor(192, 54, 64, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(MinParticleLimit, MaxParticleLimit)]
        [DefaultValue(5000)]
        public int ParticleLimit { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0f, 10f)]
        [DefaultValue(1f)]
        public float ScreenshakePower { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [DefaultValue(true)]
        public bool StealthInvisibility { get; set; }

        [BackgroundColor(192, 54, 64, 192)]
        [SliderColor(224, 165, 56, 128)]
        [DefaultValue(1.0f)]
        [Range(0.0f, 1.0f)]
        public float EnergyShieldOpacity { get; set; }

        //[BackgroundColor(192, 54, 64, 192)]
        //[DefaultValue(TileBlendingQuality.Normal)]
        //public TileBlendingQuality TileTextureBlendingQuality { get; set; }

        #endregion

        [DefaultValue(false)]
        public bool enableConsole { get; set; }
        
    }
}

