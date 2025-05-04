using HarmonyLib;
using LibraryOfAngela.Emotion;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Map;
using LibraryOfAngela.Model;
using LOR_XML;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;

namespace LibraryOfAngela.Battle
{
    class EgoPatch
    {



        public class LoAEgoInfo : EmotionEgoXmlInfo
        {
            public LorId fullId;
            
            public LoAEgoInfo(LorId id)
            {
                fullId = id;
                this.id = id.id;
            }

            public override bool Equals(object obj)
            {
                if (obj is LoAEgoInfo o)
                {
                    return fullId == o.fullId;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode() + fullId.GetHashCode();
            }
        }

        public static EgoPatch instance;
        public List<EmotionConfig> configs = new List<EmotionConfig>();
        private Dictionary<string, CustomMapData> mapDictionary;
        public EgoPanelInfo egoInfo;
        private Dictionary<string, GameObject> preloadedMap;
        
        public static void Initialize()
        {
            instance = new EgoPatch();
            var mods = LoAModCache.Instance.Where(x => x.EmotionConfig != null).ToList();
            if (mods.Count > 0)
            {
                instance.mapDictionary = new Dictionary<string, CustomMapData>();
                instance.preloadedMap = new Dictionary<string, GameObject>();
                instance.configs = new List<EmotionConfig>();
                foreach(var mod in mods)
                {
                    var config = mod.EmotionConfig;
                    config.packageId = mod.packageId;
                    instance.configs.Add(config);
                    if (mod.MapConfig != null)
                    {
                        mod.MapConfig.GetMaps().ForEach(m =>
                        {
                            m.packageId = mod.packageId;
                            instance.mapDictionary[m.mapName] = m;
                        });
                    }
                }
                InternalExtension.SetRange(typeof(EgoPatch));
            }
     
        }

        [HarmonyPatch(typeof(StageWaveModel), "HasSkillPoint")]
        [HarmonyPostfix]
        private static void After_HasSkillPoint_Enemy(ref bool __result)
        {
            if (__result && !IsEmotionEnable(Faction.Enemy))
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "HasSkillPoint")]
        [HarmonyPostfix]
        private static void After_HasSkillPoint(ref bool __result)
        {
            __result = __result || EmotionPatch.Instance.currentPanelInfo != null || instance.egoInfo != null;
            if (__result)
            {
                if (!IsEmotionEnable(Faction.Player))
                {
                    __result = false;
                }
                return;
            }

            if (EmotionPatch.Instance.currentPanelInfo == null)
            {
                var allys = BattleObjectManager.instance.GetAliveList(Faction.Player);
                var alreadySelected = allys.SelectMany(x => x.emotionDetail.PassiveList).Select(x => x.XmlInfo).Where(x => x != null).ToList();

                foreach (var mod in LoAModCache.EmotionConfigs)
                {
                    var info = GetSafeAction(() => mod.CreateModAbnormalitySelectList(allys, alreadySelected));
                    if (info != null && info?.cards?.Count > 0)
                    {
                        info.level = info.level > 4 ? 4 : info.level;
                        info.packageId = mod.packageId;
                        EmotionPatch.Instance.isSelecting = true;
                        EmotionPatch.Instance.currentPanelInfo = info;
                        __result = true;
                        return;
                    }
                }
            }
            if (instance.egoInfo == null)
            {
                var selectedEgoList = StageController.Instance.GetCurrentStageFloorModel()._selectedEgoList;

                foreach (var config in EgoPatch.instance.configs)
                {
                    var selector = config.GetModEgoSelector(selectedEgoList.Select(x => x.CardId).ToList());
                    if (selector != null)
                    {
                        instance.egoInfo = selector;
                        __result = true;
                        return;
                    }
                }
            }
        }

        private static bool IsEmotionEnable(Faction faction)
        {
            var stage = StageController.Instance.GetStageModel()?.ClassInfo?.id;
            if (stage is null || stage.IsBasic()) return true;
            var config = instance?.configs?.Find(x => x.packageId == stage.packageId);
            if (config != null && !config.IsEmotionEnableReception(stage.id, StageController.Instance.CurrentWave, StageController.Instance.RoundTurn, faction))
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "StartPickEmotionCard")]
        [HarmonyPostfix]
        private static void After_StartPickEmotionCard()
        {
            if (EmotionPatch.Instance.currentPanelInfo != null)
            {
                var cards = EmotionPatch.Instance.currentPanelInfo.cards;
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.Init(cards.Count, cards);
            }
            else if (instance.egoInfo != null)
            {
                var cards = instance.egoInfo.cards.Select(x => new LoAEgoInfo(x)).OfType<EmotionEgoXmlInfo>().ToList();
                SingletonBehavior<BattleManagerUI>.Instance.ui_levelup.InitEgo(cards.Count, cards);
            }
        }

        [HarmonyPatch(typeof(LevelUpUI), "InitEgo")]
        [HarmonyPostfix]
        private static void After_InitEgo(List<EmotionEgoXmlInfo> egoList)
        {
            var info = instance.egoInfo;
            if (info == null) return;
            foreach(var card in egoList.OfType<LoAEgoInfo>().Select(x => x.CardId).ToList())
            {
                if (!info.cards.Contains(card))
                {
                    instance.egoInfo = null;
 
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(EmotionEgoXmlList), "GetDataList")]
        [HarmonyPostfix]
        private static void After_GetDataList_Ego(SephirahType sephirah, ref List<EmotionEgoXmlInfo> __result)
        {
            var cardList = __result.Select(x => x.CardId).ToList();
            var items = __result;
            bool flag = false;
            foreach (var config in LoAModCache.EmotionConfigs)
            {
                var cards = GetSafeAction(() => config.HandleFloorEgoList(sephirah, items));
                
                if (cards != null)
                {
                    flag = true;
                    var temp = new List<EmotionEgoXmlInfo>();
                    foreach(var card in cards)
                    {
                        var p = items.Find(x => x.CardId == card);
                        if (p != null) temp.Add(p);
                        else temp.Add(new LoAEgoInfo(card));
                    }
                    items = temp;
                }
            }
            if (flag) __result = items;
        }

        [HarmonyPatch(typeof(EmotionEgoXmlInfo), "get_CardId")]
        [HarmonyPostfix]
        public static void After_get_CardId(EmotionEgoXmlInfo __instance, ref LorId __result)
        {
            if (__instance is LoAEgoInfo info)
            {
                __result = info.fullId;
            }
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "OnPickEgoCard")]
        [HarmonyPrefix]
        private static void Before_OnPickEgoCard(StageLibraryFloorModel __instance, EmotionEgoXmlInfo egoCard)
        {
            if (egoCard is LoAEgoInfo)
            {
                if (instance.egoInfo is null) return;
                // 층 에고가 아닌 개별 에고 선택시에는 selectionPoint 를 증가시켜줘야 제대로 추가됨
                if (instance.egoInfo.cards.Contains(egoCard.CardId))
                {
                    __instance.team.egoSelectionPoint++;
                }
            }
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "OnPickEgoCard")]
        [HarmonyPostfix]
        public static void After_OnPickEgoCard(StageLibraryFloorModel __instance, EmotionEgoXmlInfo egoCard)
        {
            if (egoCard is LoAEgoInfo)
            {
                try
                {
                    instance.egoInfo = null;
                    __instance._selectedEgoList.Add(egoCard);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(SpecialCardListModel), "AddCard")]
        [HarmonyPostfix]
        private static void After_AddCard(LorId cardId)
        {
            if (cardId.IsBasic() || !instance.configs.Any(x => x.packageId == cardId.packageId)) return;
            var targetCard = Singleton<SpecialCardListModel>.Instance.GetHand().Find(x => x.GetID() == cardId);
            var script = targetCard?._script as IAddToEgoCard;
            if (script != null) script.OnAddToFloorEgo(targetCard);
            var team = StageController.Instance.GetCurrentStageFloorModel().team;

            team.SetMaxEgoCooltimeCoin();
            team.UpdateEgoCooltimeCoin();

            LoAAssetBundles.Instance.LoadAssetBundle(new AssetBundleType.Ego(cardId), onComplete: (isOriginASync, loaded) =>
            {
                Logger.Log($"Ego AssetBundle Load For Target : {cardId} // {loaded}");
                if (loaded)
                {
                    var item = ItemXmlDataList.instance.GetCardItem(cardId).MapChange;
                    var preloadRequire = !instance.preloadedMap.ContainsKey(item) || instance.preloadedMap[item] != null;
                    if (!string.IsNullOrEmpty(item) && preloadRequire && instance.mapDictionary.ContainsKey(item))
                    {
                        BattleSceneRoot.Instance.StartCoroutine(CreateEgoMap(item));
                    }
                }
            });
        }

        private static IEnumerator CreateEgoMap(string item)
        {
            yield return null;
            var data = instance.mapDictionary[item];
            var manager = LoAMapManager.Create(StageController.Instance.CurrentFloor, data, false, (ILoACustomMapMod)LoAModCache.Instance[data.packageId].mod);
            manager.enabled = false;
            instance.preloadedMap[item] = manager.gameObject;
        }

        private static GameObject CheckEgoMap(GameObject origin, string mapName)
        {
            if (origin != null) return origin;
            if (instance.preloadedMap.ContainsKey(mapName) && instance.preloadedMap[mapName] != null)
            {
                var obj = instance.preloadedMap[mapName];
                var manager = obj.GetComponent<LoAMapManager>();
                manager.enabled = true;
                return obj;
            }
            if (!instance.mapDictionary.ContainsKey(mapName)) return origin;
            var map = instance.mapDictionary[mapName];
            return LoAMapManager.Create(StageController.Instance.CurrentFloor, map, false, (ILoACustomMapMod)LoAModCache.Instance[map.packageId].mod).gameObject;
        }

        private static void UpdateEgoHand(BattleUnitCardsInHandUI ui, List<BattleDiceCardModel> card)
        {
            if (ui.CurrentHandState != BattleUnitCardsInHandUI.HandState.EgoCard) return;
            var unit = ui._hOveredUnit ?? ui._selectedUnit;
            if (unit is null) return;
            int i = 0;
            while (i < card.Count)
            {
                var target = card[i];
                foreach(var config in instance.configs)
                {
                    if (config?.IsValidEgoOwner(unit, target) == false)
                    {
                        card.Remove(target);
                        i--;
                        break;
                    }
                }
                i++;
            }
        }

        /// <summary>
        /// as-is
        /// Util.LoadPrefab("CreatureMaps/CreatureMap_" + mapName, SingletonBehavior<BattleSceneRoot>.Instance.transform)
        /// 
        /// to-be
        /// EgoPatch.CheckEgoMap(Util.LoadPrefab("CreatureMaps/CreatureMap_" + mapName, SingletonBehavior<BattleSceneRoot>.Instance.transform), mapName)
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleSceneRoot), "ChangeToEgoMap")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ChangeToEgoMap(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(global::Util), "LoadPrefab", parameters: new Type[] { typeof(string), typeof(Transform) });
            bool fired = false;
            foreach(var code in instructions)
            {
                yield return code; 
                if (!fired && code.Is(OpCodes.Call, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EgoPatch), "CheckEgoMap"));
                }
            } 
        }
    
        [HarmonyPatch(typeof(BattleUnitCardsInHandUI), "UpdateCardList")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_UpdateCardList(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var fired = false;

            for (int i = 0; i< codes.Count; i++)
            {
                var code = codes[i];
                if (!fired && code.opcode == OpCodes.Ldloc_0 && codes[i + 1].opcode == OpCodes.Stloc_1)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EgoPatch), "UpdateEgoHand"));

                }
                yield return code;
            }
        }
    }
}
