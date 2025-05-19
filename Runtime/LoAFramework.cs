using HarmonyLib;
using LibraryOfAngela;
using LibraryOfAngela.Battle;
using LibraryOfAngela.BattleUI;
using LibraryOfAngela.Buf;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Map;
using LibraryOfAngela.Model;
using LibraryOfAngela.Save;
using LibraryOfAngela.Util;
using Mod;
using Opening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using TMPro;
using UI;
using UI.Title;
using UnityEngine;
using UnityEngine.UI;

namespace LibraryOfAngela
{
    public class LoAFramework : Singleton<LoAFramework>, ILoARoot
    {
        private static LoAModLoader loader;
        private static string assetPath { get; set; }

        private static AssetBundle _bundle;

        private static AssetBundle _uiBundle;

        private static AssetBundle _battleUIBundle;

        public const bool DEBUG = false;

        internal static AssetBundle UiBundle
        {
            get
            {
                if (_uiBundle == null && !string.IsNullOrEmpty(assetPath))
                {
                    _uiBundle = AssetBundle.LoadFromFile(assetPath + "_UI");
                }
                return _uiBundle;
            }
        }

        internal static string battleUiAssetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var dir = Path.GetDirectoryName(assetPath);
                    var path = Path.Combine(dir, "loa_ui");
                    return path;
                }
                return null;
            }
        }

        internal static AssetBundle BattleUiBundle
        {
            get
            {
                if (_battleUIBundle == null && !string.IsNullOrEmpty(assetPath))
                {
                    Logger.Log($"Battle UI Sync Load : {assetPath}");
                    var dir = Path.GetDirectoryName(assetPath);
                    var path = Path.Combine(dir, "loa_ui");
                    _battleUIBundle = AssetBundle.LoadFromFile(path);
                }
                return _battleUIBundle;
            }
            set
            {
                _battleUIBundle = value;
            }
        }

        SceneBuf ILoARoot.CurrentSceneBuf => SceneBufPatch.currentBuf;

        internal static void Initialize(List<Assembly> assemblys)
        {
            var asm = Assembly.GetExecutingAssembly();
            var initLogger = new StringBuilder($"LoA :: Framework Created\n ({asm.Location}) // {asm.GetName().Version}");
            Logger.Open();

            var stack = new System.Diagnostics.StackTrace().GetFrames();
            Type loaderType = null;
            for (int i = 1; i < 10; i++)
            {
                var t = stack[i].GetMethod().DeclaringType;
                if (t.Name.Contains("LoA"))
                {
                    loaderType = t;
                    break;
                }
            }

            initLogger.AppendLine($"- Is From LoALoader {loaderType.Assembly.GetName().Version} ({loaderType.Assembly.Location})");

            LoALoaderWrapper.loaderAssemlby = loaderType.Assembly;
            loader = new LoAModLoader(assemblys);
            // loader.InitHarmony();
            ServiceLocator.Instance.inject<IPatcher>((k) =>
            {
#pragma warning disable CS0252 // 의도하지 않은 참조 비교가 있을 수 있습니다. 왼쪽을 캐스팅해야 합니다.
                if (k.additionalKey == typeof(LoAFramework).Assembly) return InternalExtension.internalPatcher;
#pragma warning restore CS0252 // 의도하지 않은 참조 비교가 있을 수 있습니다. 왼쪽을 캐스팅해야 합니다.
                else return new PatcherImpl(k.additionalKey);
            });
            ServiceLocator.Instance.inject<ILoAArtworkGetter>((k) => LoAArtworks.Instance);
            ServiceLocator.Instance.inject<ILoAEmotionDictionary>((k) => LoAEmotionDictionary.Instance);
            ServiceLocator.Instance.inject<ILoARoot>((k) => Instance);
            ServiceLocator.Instance.inject<ILoAHistoryController>((k) => LoAHistoryController.Instance);
            ServiceLocator.Instance.inject<ILoAInternal>((k) => LoAInternalImpl.Instance);
            ServiceLocator.Instance.inject<TremorController>((k) => new TremorControllerImpl());
            ServiceLocator.Instance.inject<SinkingController>((k) => new SinkingControllerImpl());
            ServiceLocator.Instance.inject<RuptureController>((k) => new RuptureControllerImpl());
            ServiceLocator.Instance.inject<DimensionRiftController>((k) => new DimensionRiftControllerImpl());
     
            typeof(UI.UIController).Patch("CallUIPhase", typeof(UIPhase));

            if (CheckLoaderCompatible(initLogger))
            {
                loader.Start();
                new SavePatch().Initialize();
            }


            Debug.Log(initLogger.ToString());
        }

        private static bool isBattleUiPreloading = false;

        private static bool CheckLoaderCompatible(StringBuilder initLogger)
        {
            var loadProgressType = Type.GetType("LoALoader.LoadingProgress, LoALoader");
            var targetAsm = loadProgressType?.Assembly;
            if (targetAsm is null)
            {
                Debug.Log("LoA :: LoadingProgress Not Found, Manually Search");
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.GetName().Name.Equals("LoALoader", StringComparison.OrdinalIgnoreCase))
                    {
                        targetAsm = a;
                        break;
                    }
                }
            }
            var targetVersion = new Version(1, 1, 2, 0);
            if (targetAsm is null)
            {
                Debug.Log("LoA :: LoALoader Not Detected... What? Version Conflict?");
                return false;
            }
            else if (targetAsm.GetName().Version < targetVersion)
            {
                var log = new StringBuilder("LoA :: An incompatible LoALoader was loaded. Please replace LoALoader.dll in the path below with a dll of at least version ");
                log.AppendLine(targetVersion.ToString());
                log.AppendLine($"- Path : {targetAsm.Location}");
                log.AppendLine($"- Current Version : {targetAsm.GetName().Version}");
                log.AppendLine("- If you don't know where to find the appropriate latest version of LoALoader.dll, use the LoALoader.dll in Distort Xiao mode");
                log.AppendLine("Be sure to get it from the Steam workshop, not from somewhere that doesn't update like SkyMod. (https://steamcommunity.com/sharedfiles/filedetails/?id=2616101789)");
                Debug.Log(log);
                if (loadProgressType != null)
                {
                    AccessTools.Field(loadProgressType, "modLoadingProgress")?.SetValue(null, 1f);
                }
                return false;
            }
            else
            {
                initLogger.AppendLine("Valid Loader Version Detect");
                return true;
            }
        }

        internal static void PreloadBattleUiBundle()
        {
            if (_battleUIBundle == null && !string.IsNullOrEmpty(assetPath) && !isBattleUiPreloading)
            {
                Logger.Log("BattleUIBundle Preload");
                var dir = Path.GetDirectoryName(assetPath);
                var path = Path.Combine(dir, "loa_ui");
                var req = AssetBundle.LoadFromFileAsync(path);
                isBattleUiPreloading = true;
                req.completed += (per) =>
                {
                    if (per.isDone)
                    {
                        Logger.Log("BattleUIBundle Preload Success");
                        _battleUIBundle = req.assetBundle;
                        _battleUIBundle.LoadAssetAsync<GameObject>("Assets/CustomSelector/LoATimelineButton.prefab");
                        CustomSelectorUIManager.IsAssetLoaded = true;
                        if (CustomSelectorUIManager.IsSaveLoaded)
                        {
                            CustomSelectorUIManager.Instance.LazyInitialize();
                        }
                    }
                };
                var req2 = AssetBundle.LoadFromFileAsync(Path.Combine(dir, "loa_asset"));
                req2.completed += (per) =>
                {
                    if (per.isDone)
                    {
                        Logger.Log("ResourceBundle Preload Success");
                        new BufAssetLoader(req2.assetBundle);
                    }
                };
            }
        }

        internal static void InjectAsset(string path)
        {
            assetPath = path;
        }

        private static void After_CallUIPhase(UIPhase phase)
        {
            if (phase != UIPhase.Invitation && phase != UIPhase.BattleSetting && phase != UIPhase.DUMMY)
            {
                PatcherImpl.patchers.ForEach(x => x.ClearInvitationPatch());
            }
            if (phase == UIPhase.BattleResult)
            {
                RewardUIPatch.CheckSkipResult();
            }
            else if (phase == UIPhase.Sephirah) RewardUIPatch.CheckShowRewardUI();
 
        }

        void ILoARoot.AddMapUpdateCallback(Action<MapManager> action)
        {
            (BattleSceneRoot.Instance.currentMapObject as LoAMapManager)?.AddMapUpdateCallback(action);
        }

        void ILoARoot.UpdateMap(Sprite background, Sprite floor, bool showEffect)
        {
            (BattleSceneRoot.Instance.currentMapObject as LoAMapManager)?.UpdateMap(background, floor, showEffect);
        }

        void ILoARoot.UpdateMap(string packageId, string mapName, bool showEffect)
        {
            MapPatch.Instance.ReplaceMap(packageId, mapName, showEffect);
        }

        void ILoARoot.LoadAssetBundleManally(string packageId, string path)
        {
            LoAAssetBundles.Instance.LoadAssetBundleAll(packageId, path);
        }

        void ILoARoot.InjectAddtionalLog(BattleUnitModel unit, string title, string desc, EffectTypoCategory category)
        {
            BattleLogPatch.InjectResultLog(unit, title, desc, category);
        }

        T ILoARoot.LoadAsset<T>(string packageId, string name, bool expectNotExists)
        {
            return LoAAssetBundles.Instance.LoadAsset<T>(packageId, name, expectNotExists);
        }

        void ILoARoot.ShowEmotionSelectUI(EmotionPannelInfo info)
        {
            LoAEmotionDictionary.Instance.ShowEmotionSelectUI(info);
        }

        void ILoARoot.ReservePassive(BattleUnitPassiveDetail owner, LorId id)
        {
            BattlePhasePatch.EnqueueReservePassive(owner, id);
        }

        void ILoARoot.AddPhaseCallback(StageController.StagePhase? phase, Action callback, bool onlyOnce)
        {
            BattlePhasePatch.AddPhaseCallback(phase, callback, onlyOnce);
        }

        List<EmotionCardXmlInfo> ILoARoot.CreateValidEmotionCardListByEmotionRate(List<EmotionCardXmlInfo> cardFool, int emotionLevel, int count)
        {
            int num = 0;
            int num2 = 0;
            var currentFloor = StageController.Instance.GetCurrentStageFloorModel();
            foreach (UnitBattleDataModel unitBattleDataModel in currentFloor._unitList)
            {
                if (unitBattleDataModel.IsAddedBattle)
                {
                    num += unitBattleDataModel.emotionDetail.totalPositiveCoins.Count;
                    num2 += unitBattleDataModel.emotionDetail.totalNegativeCoins.Count;
                }
            }
            int floorLevel = 0;
            LibraryFloorModel floor = currentFloor._floorModel;
            if (floor != null)
            {
                if (Singleton<StageController>.Instance.IsRebattle)
                {
                    floorLevel = floor.TemporaryLevel;
                }
                else
                {
                    floorLevel = floor.Level;
                }
            }
            int emotionLevel2;
            if (emotionLevel <= 2)
            {
                emotionLevel2 = 1;
            }
            else if (emotionLevel <= 4)
            {
                emotionLevel2 = 2;
            }
            else
            {
                emotionLevel2 = 3;
            }
            List<EmotionCardXmlInfo> dataList = cardFool.Where(x => x.EmotionLevel == emotionLevel2).ToList();
            foreach (EmotionCardXmlInfo item in currentFloor._selectedList)
            {
                dataList.Remove(item);
            }
            int center = 0;
            int num3 = num + num2;
            float num4 = 0.5f;
            if (num3 > 0)
            {
                num4 = (float)(num - num2) / (float)num3;
            }
            float num5 = num4 / ((11f - (float)emotionLevel) / 10f);
            if ((double)Mathf.Abs(num5) < 0.1)
            {
                center = 0;
            }
            else if ((double)Mathf.Abs(num5) < 0.3)
            {
                if (num5 > 0f)
                {
                    center = 1;
                }
                else
                {
                    center = -1;
                }
            }
            else if (num5 > 0f)
            {
                center = 2;
            }
            else
            {
                center = -2;
            }
            dataList.Sort((EmotionCardXmlInfo x, EmotionCardXmlInfo y) => Mathf.Abs(x.EmotionRate - center) - Mathf.Abs(y.EmotionRate - center));
            List<EmotionCardXmlInfo> list = new List<EmotionCardXmlInfo>();
            while (dataList.Count > 0 && list.Count < count)
            {
                int er = Mathf.Abs(dataList[0].EmotionRate - center);
                List<EmotionCardXmlInfo> list2 = dataList.FindAll((EmotionCardXmlInfo x) => Mathf.Abs(x.EmotionRate - center) == er);
                if (list2.Count + list.Count <= count)
                {
                    list.AddRange(list2);
                    using (List<EmotionCardXmlInfo>.Enumerator enumerator2 = list2.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            EmotionCardXmlInfo item2 = enumerator2.Current;
                            dataList.Remove(item2);
                        }
                        continue;
                    }
                }
                int num6 = count - list.Count;
                int num7 = 0;
                while (num7 < num6 && list2.Count != 0)
                {
                    EmotionCardXmlInfo item3 = RandomUtil.SelectOne<EmotionCardXmlInfo>(list2);
                    list2.Remove(item3);
                    dataList.Remove(item3);
                    list.Add(item3);
                    num7++;
                }
            }
            return list;
        }

        void ILoARoot.ShowCustomSelector(CustomSelectorModel model)
        {
            CustomSelectorUIManager.Instance.Init(model);
        }

        void ILoARoot.UpdateRarity(RarityModel model)
        {
            AdvancedCorePageRarityPatch.UpdateRarityModel(model);
        }

        ILoACardListController ILoARoot.GetCardListController(BattleAllyCardDetail detail)
        {
            return new LoACardListControllerImpl(detail);
        }

        ILoACardListController ILoARoot.GetCardListController(BattlePersonalEgoCardDetail detail)
        {
            return new LoACardListControllerImpl(detail);
        }

        ILoACardListController ILoARoot.GetCardListControllerByCard(BattleDiceCardModel card, BattleUnitModel owner)
        {
            if (card?.XmlData?.IsPersonal() == true)
            {
                return new LoACardListControllerImpl(owner.personalEgoDetail);
            }
            else
            {
                return new LoACardListControllerImpl(owner.allyCardDetail);
            }
        }
    }
}
