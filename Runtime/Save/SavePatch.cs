using GameSave;
using LibraryOfAngela.Battle;
using LibraryOfAngela.BattleUI;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LibraryOfAngela.Story;
using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace LibraryOfAngela.Save
{
    class SavePatch
    {
        private const string KEY_LAST_CLEAR_PACKAGE = "KEY_LAST_CLEAR_PACKAGE";
        private const string KEY_LAST_CLEAR_STAGE = "KEY_LAST_CLEAR_STAGE";
        private static LorId latestClear = null;
        private static bool isLibraryLoaded = false;

        public void Initialize()
        {
            // 아래 Patch 호출은 제거합니다. 이미 각 메소드에 HarmonyPatch 어트리뷰트가 추가되어 있습니다.
            // typeof(LibraryModel).Patch("GetSaveData", "LibraryModel_GetSaveData");
            // typeof(LibraryModel).Patch("LoadFromSaveData", "LibraryModel_LoadFromSaveData");
            // typeof(StageModel).Patch("WinStage");
            // typeof(StageController).Patch("GameOver");
        }

        [HarmonyPatch(typeof(LibraryModel), "GetSaveData")]
        [HarmonyPostfix]
        public static void After_LibraryModel_GetSaveData(LibraryModel __instance, ref SaveData __result)
        {
            try
            {
                BattleUIPatch.keywordCountDic.Clear();
                SkinInfoProvider.Instance.SaveSkinProperties(__result);
                int cnt = 0;
                var wrapper = new SaveData();
                foreach (var m in LoAModCache.Instance.Where(d => d.SaveConfig != null))
                {
                    try
                    {
                        m.SaveConfig.packageId = m.packageId;
                        var data = m.SaveConfig.GetSaveData(__instance);
                        wrapper.AddData(m.packageId, data);
                        cnt++;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
                if (cnt > 0)
                {
                    __result.AddData("LoASaveDatas", wrapper);
                }

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(LibraryModel), "LoadFromSaveData")]
        [HarmonyPrefix]
        public static void Before_LibraryModel_LoadFromSaveData(SaveData data)
        {
            LoA.IsSaveInitialized = false;
        }

        [HarmonyPatch(typeof(LibraryModel), "LoadFromSaveData")]
        [HarmonyPostfix]
        public static void After_LibraryModel_LoadFromSaveData(SaveData data)
        {
            LoA.IsSaveInitialized = true;
            try
            {
                Logger.Log("Load Complete, Inject Test");
                InjectReward(GetRewards());
                SkinInfoProvider.Instance.LoadSkinProperties(data);
                SkinInfoProvider.Instance.Initialize();
                foreach(var x in AssemblyManager.Instance._initializer.OfType<ILoAMod>())
                {
                    try
                    {
                        x.OnSaveLoaded();
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"SaveLoad Error in {x.packageId}");
                        Logger.LogError(e);
                    }
                }
                CustomSelectorUIManager.IsSaveLoaded = true;
                if (CustomSelectorUIManager.IsAssetLoaded)
                {
                    CustomSelectorUIManager.Instance.LazyInitialize();
                }
                var customSaveData = data.GetData("LoASaveDatas");
                if (customSaveData != null)
                {
                    foreach (var pair in customSaveData._dic)
                    {
                        try
                        {
                            var mod = LoAModCache.Instance[pair.Key]?.SaveConfig;
                            if (mod != null)
                            {
                                mod.packageId = pair.Key;
                            }
                            mod?.LoadFromSaveData(pair.Value);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e);
                        }
                    }
                }
                // InjectAllClear();

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static void InjectAllClear()
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    LibraryModel.Instance.PlayHistory.SetAllClear();
                }
                catch (Exception)
                {

                }
            }
            LibraryModel.Instance._currentChapter = 7;
            LibraryModel.Instance.GetOpenedFloorList().ForEach(x =>
            {
                if (x.Sephirah == SephirahType.Hokma || x.Sephirah == SephirahType.Binah)
                {
                    x.SetLevel(4);
                    x.UpdateOpenedCount(4);
                }
                else
                {
                    x.SetLevel(5);
                    x.UpdateOpenedCount(5);
                }
            });
            StageClassInfoList.Instance.GetAllDataList().ForEach(x =>
            {
                if (LibraryModel.Instance.ClearInfo.GetClearCount(x.id) == 0)
                {
                    LibraryModel.Instance.ClearInfo.AddClearCount(x.id);
                }
            });
            DropBookXmlList.Instance.GetList().ForEach(x =>
            {
                DropBookInventoryModel.Instance.AddBook(x.id, 50);
            });
        }

        private static List<ClearReward> GetRewards()
        {
            var result = new List<ClearReward>();
            foreach (var mod in LoAModCache.StoryConfigs)
            {
                var packageId = mod.packageId;
                foreach (var modInfo in mod.GetStoryIcons().SelectMany(x => x.stageIds))
                {
                    var stageId = new LorId(packageId, modInfo.id);
                    // Debug.Log($"Target Stage :: {stageId} // {LibraryModel.Instance.ClearInfo.GetClearCount(stageId)} // {modInfo.rewards != null}");
                    if (LibraryModel.Instance.ClearInfo.GetClearCount(stageId) > 0 && modInfo.rewards != null)
                    {
                        foreach (var reward in modInfo.rewards)
                        {
                            reward.packageId = packageId;
                            result.Add(reward);
                        }
                    }
                }
            }


            return result;
        }
    

        [HarmonyPatch(typeof(StageController), "GameOver")]
        [HarmonyPostfix]
        private static void After_GameOver(bool iswin, bool isbackbutton)
        {
            if (isbackbutton) return;

            var model = StageController.Instance.GetStageModel();
            var packageId = model.ClassInfo.id.packageId;
            if (string.IsNullOrEmpty(packageId)) return;
            var mod = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == packageId);
            if (mod == null) return;
            bool flag = false;
            foreach (var stage in mod.GetStoryIcons())
            {
                foreach (var info in stage.stageIds)
                {
                    if (info.id == model.ClassInfo._id)
                    {
                        if (info.skipResult) RewardUIPatch.ReserveSkipResult(iswin);
                        flag = true;
                        break;
                    }
                }
                if (flag) break;
            }
        }

        [HarmonyPatch(typeof(StageModel), "WinStage")]
        [HarmonyPostfix]
        private static void After_WinStage(StageModel __instance)
        {
            try
            {
                var packageId = __instance.ClassInfo.id.packageId;
                if (string.IsNullOrEmpty(packageId)) return;
                var mod = LoAModCache.StoryConfigs.FirstOrDefault(x => x.packageId == packageId);
                if (mod == null) return;
                bool flag = false;
                foreach (var stage in mod.GetStoryIcons())
                {
                    foreach (var info in stage.stageIds)
                    {
                        if (info.id == __instance.ClassInfo._id)
                        {
                            RewardUIPatch.ReserveRewardUI(__instance.ClassInfo, info.rewards);
                            InjectReward(info.rewards);
                            flag = true;
                            break;
                        }
                    }
                    if (flag) break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static void InjectReward(List<ClearReward> rewards)
        {
            var bookCards = Singleton<BookInventoryModel>.Instance.GetBookListAll().SelectMany(x => x.GetDeckAll_nocopy())
         .SelectMany(x => x.GetAllCardList()).Select(x => x.id).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

            // Debug.Log($"Reward Check Count :: {rewards.Count}");
            foreach (var reward in rewards)
            {
                var dropCardId = new LorId(reward.packageId, reward.id);
                // Debug.Log($"Reward Check :: {reward.type} // {dropCardId}");
                if (reward.type == DropItemType.Card)
                {
                    var cardInfo = ItemXmlDataList.instance.GetCardItem(dropCardId, true);
                    if (cardInfo is null)
                    {
                        Logger.Log($"Reward Expected {dropCardId} But Card Info Null, Please Check");
                        continue;
                    }
                    var max = reward.count;
                    int cnt = bookCards.ContainsKey(dropCardId) ? max - bookCards[dropCardId] : max;
                    cnt -= Singleton<InventoryModel>.Instance.GetCardCount(dropCardId);
                   
                    if (cnt > 0)
                    {
                        InventoryModel.Instance.AddCard(dropCardId, cnt);
                    }
                }
                else
                {
                    var info = BookXmlList.Instance.GetData(dropCardId);
                    int cnt = BookInventoryModel.Instance.GetBookCount(dropCardId);

                    while (cnt < reward.count)
                    {
                        BookInventoryModel.Instance.CreateBook(info);
                        cnt++;
                    }
                }

            }
        }

    }
}
