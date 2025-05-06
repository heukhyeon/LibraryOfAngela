using LibraryOfAngela;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using Mod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    public class LoA
    {
        public static SceneBuf CurrentSceneBuf { get => ServiceLocator.Instance.GetInstance<ILoARoot>().CurrentSceneBuf; }
        
        public static void Init()
        {
            // 구현 없음
        }

        public static void AddMapUpdateCallback(Action<MapManager> action)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().AddMapUpdateCallback(action);
        }

        public static void LoadAssetBundleManally(string packageId, string path)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().LoadAssetBundleManally(packageId, path);
        }

        public static void UpdateMap(Sprite background, Sprite floor, bool showEffect)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().UpdateMap(background, floor, showEffect);
        }
        public static void UpdateMap(string packageId, string mapName, bool showEffect)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().UpdateMap(packageId, mapName, showEffect);
        }

        public static List<EmotionCardXmlInfo> CreateValidEmotionCardListByEmotionRate(List<EmotionCardXmlInfo> cardFool, int emotionLevel, int count)
        {
            return ServiceLocator.Instance.GetInstance<ILoARoot>().CreateValidEmotionCardListByEmotionRate(cardFool, emotionLevel, count);
        }

        public static void ShowCustomSelector(CustomSelectorModel model)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().ShowCustomSelector(model);
        }

        public static void AddPhaseCallback(StageController.StagePhase? phase, Action callback, bool onlyOnce)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().AddPhaseCallback(phase, callback, onlyOnce);
        }

        // 서비스가 하모니 의존성을 명확히 걸때, 어트리뷰트 기반으로 자동 하모니를 겁니다.
        public static void PatchAll(Type targetType)
        {
            ServiceLocator.Instance.CreateInstance<IPatcher>(targetType.Assembly).PatchAll(targetType);
        }


        static LoA()
        {

        }

        /// <summary>
        /// 버전 업그레이드의 유효성을 위해, LoARuntime.dll 은 LibraryOfRuina 의 기본적인 dll 로드에 포함되선 안됩니다.
        /// 만약 LoAFramework.dll 이 LibraryOfRuina 의 dll 로드에 포함된경우 에러를 발행합니다.
        /// </summary>
        private static void checkLoAFrameworkInvalid()
        {
            var frameworkType = Type.GetType("LibraryOfAngela.ModLoader,LoARuntime");
            if (frameworkType != null)
            {
                var path = Path.GetDirectoryName(frameworkType.Assembly.Location);
                var builder = new StringBuilder();
                builder.AppendLine("From LibraryOfAngela Framework ::");
                builder.AppendLine("Invalid LoARuntime.dll Path Detected, Please Update Valid Path");
                builder.AppendLine(path);
                throw new Exception(builder.ToString());
            }
        }

        /// <summary>
        /// <see cref="ILoAMod"/> 를 구현한 모드들이 최소한의 파일 배치 룰을 준수했을때, LoAFramework 를 불러옵니다.
        /// </summary>
        private static void loadLoAFrameworkDll()
        {
            var loAFrameworkDlls = GetLoADirs();

            if (loAFrameworkDlls.Count() == 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine("From LibraryOfAngela Framework ::");
                builder.AppendLine("LoARuntime.dll Not Found, Please Check Your Mod Files");
                throw new Exception(builder.ToString());
            }

            var latestLoAFrameworkDll = loAFrameworkDlls.OrderByDescending(x =>
            {
                FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(x);
                return Version.Parse(myFileVersionInfo.FileVersion);
            }).First();

            UnityEngine.Debug.Log($"LoA Runtime Load :: {FileVersionInfo.GetVersionInfo(latestLoAFrameworkDll).FileVersion} // {latestLoAFrameworkDll}");
            var latestLoAAsset = Path.Combine(Path.GetDirectoryName(latestLoAFrameworkDll), "LoAAsset");
            var asm = Assembly.LoadFile(latestLoAFrameworkDll);
            var frameworkType = asm.GetType("LibraryOfAngela.LoAFramework");
            frameworkType.GetMethod("Initialize", CommonExtension.allFlag).Invoke(null, null);
            frameworkType.GetMethod("InjectAsset", CommonExtension.allFlag).Invoke(null, new object[] { latestLoAAsset });
        }

        private static IEnumerable<string> GetLoADirs()
        {
            IEnumerable<string> ret = AssemblyManager.Instance
                ._initializer
                .Where(x => x is ILoAMod)
                .Select(x => x.GetType().Assembly.Location)
                // 후에 LoAFramework.dll 을 찾는다.
                .SelectMany(x =>
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(x);
                        if (Directory.Exists(dir)) return Directory.GetFiles(dir, "LoARuntime.dll", SearchOption.AllDirectories);
                        else return new string[0];
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                        return new string[0];
                    }
                });

            // Sort 모드와 섞일경우, 활성화된 모드 검사 자체가 잘못될 수 있다. 
            // 백업 코드로서, 그냥 모든 폴더를 검사하고, 이것이 실제로 발견되었을때 경고 로그를 남긴다.
            if (ret.Count() == 0)
            {
                ret = ModContentManager.Instance.GetAllMods()
                // 모드 경로를 획득해서
                .Select(x => x.dirInfo.FullName)
                // 어셈블리 폴더의 경로로 매핑
                .Select(x => Path.Combine(x, "Assemblies"))
                // 후에 LoAFramework.dll 을 찾는다.
                .SelectMany(x =>
                {
                    try
                    {
                        if (Directory.Exists(x)) return Directory.GetFiles(x, "LoARuntime.dll", SearchOption.AllDirectories);
                        else return new string[0];
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e);
                        return new string[0];
                    }
                });

                if (ret.Count() == 0)
                {
                    var path = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LazyDll"), "LoARuntime.dll");
                    if (File.Exists(path))
                    {
                        ret = new string[] { path };
                    }
                }
                if (ret.Count() > 0)
                {
                    var logger = new StringBuilder("LoA :: Warning 1 - ");
                    logger.AppendLine("The mode is not enabled properly, which is probably an effect of the https://steamcommunity.com/sharedfiles/filedetails/?id=3055406169 mode.");
                    logger.AppendLine("This mod is intended to be compatible with that mod, but other mods that use this mod's LoAInterface.dll may not work properly.");
                    logger.AppendLine("Therefore, if you are experiencing this issue, please follow the instructions at https://steamcommunity.com/sharedfiles/filedetails/?id=3055406169 to 'completely' uninstall the mod and see if the issue is reproduced.");
                    UnityEngine.Debug.Log(logger.ToString());
                }
            }

            return ret;
        }
    }

    public interface ILoARoot
    {
        SceneBuf CurrentSceneBuf { get; }

        T LoadAsset<T>(string packageId, string name, bool expectNotLoaded) where T : UnityEngine.Object;

        void AddMapUpdateCallback(Action<MapManager> action);

        void UpdateMap(Sprite background, Sprite floor, bool showEffect);

        void UpdateMap(string packageId, string mapName, bool showEffect);

        void UpdateRarity(RarityModel model);

        void LoadAssetBundleManally(string packageId, string path);
        void InjectAddtionalLog(BattleUnitModel unit, string title, string desc, EffectTypoCategory category);

        void ShowEmotionSelectUI(EmotionPannelInfo selector);

        void ReservePassive(BattleUnitPassiveDetail owner, LorId id);

        void AddPhaseCallback(StageController.StagePhase? phase, Action callback, bool onlyOnce);
        void ShowCustomSelector(CustomSelectorModel model);
        List<EmotionCardXmlInfo> CreateValidEmotionCardListByEmotionRate(List<EmotionCardXmlInfo> cardFool, int emotionLevel, int count);

        ILoACardListController GetCardListController(BattleAllyCardDetail detail);

        ILoACardListController GetCardListController(BattlePersonalEgoCardDetail detail);

        ILoACardListController GetCardListControllerByCard(BattleDiceCardModel card, BattleUnitModel owner);
    }
}
