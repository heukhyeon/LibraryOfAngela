using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    class LoAConfigs
    {
        public ILoAMod mod;

        public Assembly Assembly { get; private set; }

        public string packageId { get => mod.packageId; }

        public ArtworkConfig ArtworkConfig { get; private set; }

        public AssetBundleConfig AssetBundleConfig { get; private set; }
        public MapConfig MapConfig { get; private set; }
        public CorePageConfig CorePageConfig { get; private set; }
        public BattlePageConfig BattlePageConfig { get; private set; }
        public StoryConfig StoryConfig { get; private set; }

        public EmotionConfig EmotionConfig { get; private set; }

        public SuccessionConfig SuccessionConfig { get; private set; }

        public ILoAArtworkCache Artworks { get; private set; }

        public AssetBundleCache AssetBundles { get; private set; }

        public static LoAConfigs Create(ILoAMod mod)
        {
            if (mod is null) return null;

            var config = new LoAConfigs();

            config.mod = mod;
            config.Assembly = mod.GetType().Assembly;

            if (mod is ILoACustomArtworkMod m1)
            {
                config.ArtworkConfig = m1.ArtworkConfig;
                m1.Artworks = config.Artworks;
            }
            if (mod is ILoACustomAssetBundleMod m2)
            {
                config.AssetBundleConfig = m2.AssetBundleConfig;
                m2.AssetBundles = config.AssetBundles;
            }
            if (mod is ILoACorePageMod m3) config.CorePageConfig = m3.CorePageConfig;
            if (mod is ILoABattlePageMod m4) config.BattlePageConfig = m4.BattlePageConfig;
            if (mod is ILoACustomStoryInvitationMod m5) config.StoryConfig = m5.StoryConfig;
            if (mod is ILoACustomEmotionMod m6) config.EmotionConfig = m6.EmotionConfig;
            if (mod is ILoASuccessionMod m7) config.SuccessionConfig = m7.SuccessionConfig;
            if (mod is ILoACustomMapMod m8) config.MapConfig = m8.MapConfig;

            return config;
        }

        public void Init(LoAConfig config)
        {
            if (config is null) return;
            config.packageId = packageId;
            config.Init();
            if (config == ArtworkConfig)
            {
                Artworks = new LoAArtworkCache(packageId);
                (mod as ILoACustomArtworkMod).Artworks = Artworks;
            }
            else if (config == AssetBundleConfig)
            {
                AssetBundles = new AssetBundleCache(packageId);
                (mod as ILoACustomAssetBundleMod).AssetBundles = AssetBundles;
            }
        }

    }
}
