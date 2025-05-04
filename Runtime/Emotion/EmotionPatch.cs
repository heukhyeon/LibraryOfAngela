using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;
using UnityEngine;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Interface_External;
using System.Reflection.Emit;
using LibraryOfAngela.Extension.Framework;
using TMPro;

namespace LibraryOfAngela.Emotion
{
    class EmotionPatch : Singleton<EmotionPatch>
    {
        internal bool isSelecting = false;
        internal EmotionPannelInfo currentPanelInfo;
        private EmotionDirectionObserver observer = null;

        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(EmotionPatch));
            if (LoAEmotionDictionary.Instance.Initialize())
            {
                typeof(LevelUpUI).GetNestedTypes(AccessTools.all).FirstOrDefault(x => x.Name.Contains("OnSelectRoutine")).PatchInternal("MoveNext", flag: PatchInternalFlag.TRANSPILER);

                EmotionUIPatch.Instance.Initialize();
            }
        }

        public bool IsMySelecting()
        {
            return Instance.currentPanelInfo != null;
        }

        [HarmonyPatch(typeof(LevelUpUI), "Init")]
        [HarmonyPostfix]
        private static void After_Init(List<EmotionCardXmlInfo> cardList, LevelUpUI __instance)
        {
            if (Instance.observer is null)
            {
                Instance.observer = __instance.hideTextRect.gameObject.AddComponent<EmotionDirectionObserver>();
                Instance.observer.levelUpUI = __instance;
                Instance.observer.text = __instance.hideText;
                Instance.observer.enabled = false;
            }

            var info = Instance.currentPanelInfo;
            if (info == null) return;
            if (info.cards != cardList)
            {
                Instance.currentPanelInfo = null;
            }
        }

        [HarmonyPatch(typeof(EmotionCardXmlList), "GetDataListByLevel")]
        [HarmonyPostfix]
        private static void After_GetDataListByLevel(SephirahType sephirah, int level, ref List<EmotionCardXmlInfo> __result)
        {
            foreach (var mod in LoAModCache.EmotionConfigs)
            {
                mod.OnReturnEmotionCardListForPreview(__result, sephirah, level);
            }
        }

        private static bool isCalling = false;

        [HarmonyPatch(typeof(EmotionCardXmlList), "GetDataList", new Type[] { typeof(SephirahType), typeof(int), typeof(int) })]
        [HarmonyPostfix]
        private static void After_GetDataList(SephirahType sephirah, int floorLevel, int emotionLevel, List<EmotionCardXmlInfo> __result)
        {
            if (!isCalling)
            {
                isCalling = true;
                foreach (var config in LoAModCache.EmotionConfigs)
                {
                    try
                    {
                        config.OnReturnEmotionCard(sephirah, floorLevel, emotionLevel, __result);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Test Error");
                        Logger.LogError(e);
                    }
                }
                isCalling = false;
            }

        }

        [HarmonyPatch(typeof(StageController), "StartBattle")]
        [HarmonyPostfix]
        private static void After_StartBattle()
        {
            Instance.currentPanelInfo = default(EmotionPannelInfo);
        }

        [HarmonyPatch(typeof(LevelUpUI), "OnSelectPassive")]
        [HarmonyPostfix]
        private static void After_OnSelectPassive(EmotionCardXmlInfo ____selectedCard, LevelUpUI __instance)
        {
            if (Instance.currentPanelInfo?.matchedTarget != null && Instance.currentPanelInfo.matchedTarget.Count == 1)
            {
                __instance._needUnitSelection = false;
                __instance.OnClickTargetUnit(Instance.currentPanelInfo.matchedTarget[0]);
            }
            else
            {
                Instance.observer?.TextUpdate();
                var packageId = LoAEmotionDictionary.Instance.infoPackageIdDictionary.SafeGet(____selectedCard);
                if (packageId is null) return;
                var target = GetSafeAction(() => LoAModCache.Instance[packageId]?.EmotionConfig?.GetMatchedAbnormalityCardOwner(____selectedCard));
                if (target is LoAEmotionSelectTarget.Fixed t && t.targets.Count > 0)
                {
                    __instance._needUnitSelection = false;
                    __instance.OnClickTargetUnit(t.targets[0]);
                    if (t.targets.Count > 1)
                    {
                        foreach (var p in t.targets.Skip(1))
                        {
                            p.emotionDetail.ApplyEmotionCard(____selectedCard);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "OnPickPassiveCard")]
        [HarmonyPostfix]
        private static void After_OnPickPassiveCard(EmotionCardXmlInfo card, StageLibraryFloorModel __instance, BattleUnitModel target)
        {
            if (__instance?.team is null) return;

            var selectedList = __instance._selectedList;

            try
            {
                bool isCustomPanelSelect = Instance.isSelecting && Instance.currentPanelInfo?.cards?.Contains(card) == true;
                if (isCustomPanelSelect)
                {
                    try
                    {
                        Instance.currentPanelInfo.onSelect?.Invoke(target, card);
                    }
                    catch (MissingFieldException)
                    {
                        // ignore
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                    Instance.isSelecting = false;
                    Instance.currentPanelInfo = null;
                    __instance.team.currentSelectEmotionLevel--;
                    selectedList?.Remove(card);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "ApplyEmotionCard")]
        [HarmonyPostfix]
        private static void After_ApplyEmotionCard(BattleUnitEmotionDetail __instance, EmotionCardXmlInfo card, BattleUnitModel ____self)
        {
            if (____self.faction != Faction.Player) return;
            try
            {
                BattleObjectManager.instance._unitList.ForEach(x =>
                {
                    if (!x.IsDead())
                    {
                        foreach (var p in BattleInterfaceCache.Of<IEmotionListenerPassive>(x))
                        {
                            p.OnSelectEmotionCard(____self, card);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(BattleEmotionCardModel), MethodType.Constructor, new Type[] { typeof(EmotionCardXmlInfo), typeof(BattleUnitModel)})]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_Constructor(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getTypeMethod = AccessTools.Method(typeof(Type), nameof(Type.GetType), new Type[] { typeof(string) });

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].Is(OpCodes.Call, getTypeMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionPatch), nameof(GetValidType)));
                }
            }
        }

        // Transpiler 로 고쳐진 부분에서 호출됨.
        private static Type GetValidType(Type originType, EmotionCardXmlInfo info, string script)
        {
            if (originType != null) return originType;

            // Obtain packageId of the mode currently defined EmotionCardXmlInfo
            var key = LoAEmotionDictionary.Instance.infoPackageIdDictionary.SafeGet(info); // return Mod PackageId

            var modType = LoAModCache.Instance[key]?.EmotionConfig?.GetAbnornailityScriptType(info, script);

            if (modType != null) return modType;

            // 모드에서 바닐라 환상체 책장을 생성하는경우 타입을 못찾을수있다. 바닐라 어셈블리를 명시적으로 지정해서 재생성
            var defaultType = info.GetType().Assembly.GetType("EmotionCardAbility_" + script.Trim());

            return defaultType;
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "OnPickPassiveCard")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_MoveNext(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BattleObjectManager), "GetAliveList", parameters: new Type[] { typeof(Faction) });
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EmotionPatch), "ConvertValidUnits"));
                }
            }
        }

        private static List<BattleUnitModel> ConvertValidUnits(List<BattleUnitModel> units)
        {
            try
            {
                var currentCard = SingletonBehavior<BattleManagerUI>.Instance.ui_levelup._selectedCard;

                if (currentCard is null) return units;


                if (Instance.currentPanelInfo?.matchedTarget != null && Instance.currentPanelInfo.matchedTarget.Count > 1)
                {
                    units.Clear();
                    units.AddRange(Instance.currentPanelInfo.matchedTarget);
                }
                else
                {
                    var packageId = LoAEmotionDictionary.Instance.infoPackageIdDictionary.SafeGet(currentCard);
                    if (packageId is null) return units;
                    var target = GetSafeAction(() => LoAModCache.Instance[packageId]?.EmotionConfig?.GetMatchedAbnormalityCardOwner(currentCard));
                    if (target is LoAEmotionSelectTarget.Selectable t && t.targets.Count > 0)
                    {
                        units.Clear();
                        units.AddRange(t.targets);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            return units;
        }

        [HarmonyPatch(typeof(StageLibraryFloorModel), "CreateSelectableList")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CreateSelectableList(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var found = false;
            var target = AccessTools.Method(typeof(List<EmotionCardXmlInfo>), "AddRange");
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (code.opcode == OpCodes.Stloc_S && code.operand is LocalBuilder d && d.LocalIndex == 10)
                {
                    found = true;
                }
                if (found && code.opcode == OpCodes.Ldc_I4_3)
                {
                    // floorLevel (local var)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 3);

                    // emotionLevel2 (local var)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);

                    // dataList (GetDataList의 반환값)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);

                    // dataList (GetDataList의 반환값)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    // GetSelectCardCount 메서드 호출
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(EmotionPatch), nameof(GetSelectCardCount)));
                }
                if ((code.opcode == OpCodes.Leave || code.opcode == OpCodes.Leave_S) && codes[i + 1].opcode != OpCodes.Nop)
                {
                    Logger.Log("In CreateSelectableList : Invalid Leave Code. Try Fix");
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Nop).MoveBlocksFrom(code));
                }
            }
        }

        private static void FixDuplicateEmotionCards(List<EmotionCardXmlInfo> list2, List<EmotionCardXmlInfo> list)
        {
            list2.RemoveAll(d => list.Contains(d));
        }

        private static int GetSelectCardCount(int origin, int floorLevel, int emotionLevel, List<EmotionCardXmlInfo> result, List<EmotionCardXmlInfo> current)
        {
            int res = origin;
            var sephirah = StageController.Instance.CurrentFloor;
            foreach (var config in LoAModCache.EmotionConfigs)
            {
                try
                {
                    int cnt = -1;
                    cnt = config.HandleSelectEmotionCardListCount(res, sephirah, floorLevel, emotionLevel, result);
                    if (cnt > 0)
                    {
                        res = cnt;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            return res;
        }

    }

    class EmotionDirectionObserver : MonoBehaviour
    {
        private const string KEY = "creaturebookshelf_needselectunit";
        private string originText;
        public LevelUpUI levelUpUI;
        public TextMeshProUGUI text;
        private string applyText;


        void OnDisable()
        {
            if (!string.IsNullOrEmpty(originText))
            {
                text.text = originText;
                text.transform.localPosition = text.transform.localPosition.Copy(y: 0f);
                originText = null;
            }
            if (enabled)
            {
                enabled = false;

            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (levelUpUI.selectedEmotionCard.transform.localPosition.x == -410f)
                {
                    levelUpUI.selectedEmotionCard.transform.localPosition = new Vector3(410f, -510f);
                    levelUpUI.selectedEmotionCardBg.transform.localScale = new Vector3(-1f, 1f, 1f);
                }
                else
                {
                    levelUpUI.selectedEmotionCard.transform.localPosition = new Vector3(-410f, -510f);
                    levelUpUI.selectedEmotionCardBg.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
        }

        public void TextUpdate()
        {
            try
            {
                var id = StageController.Instance.GetStageModel().ClassInfo.workshopID;
                if (string.IsNullOrEmpty(id)) return;
                if (LoAModCache.Instance[id] is null) return;
                var isLeft = false;
                var isRight = false;
                foreach (var unit in BattleObjectManager.instance.GetList(Faction.Player))
                {
                    var x = unit.formationCellPos.x;
                    isRight = isRight || x > 0;
                    isLeft = isLeft || x <= 0;
                }

                if (Singleton<StageController>.Instance.AllyFormationDirection == Direction.RIGHT && isLeft && !isRight)
                {
                    levelUpUI.selectedEmotionCard.transform.localPosition = new Vector3(410f, -510f);
                    levelUpUI.selectedEmotionCardBg.transform.localScale = new Vector3(-1f, 1f, 1f);
                }
                if (isRight && isLeft)
                {
                    if (applyText is null)
                    {
                        switch (TextDataModel.CurrentLanguage)
                        {
                            case "kr":
                                applyText = "(Tab:좌우 변환)";
                                break;
                            case "jp":
                                applyText = "(Tab:表示位置変更)(左右)";
                                break;
                            case "cn":
                                applyText = "";
                                break;
                            default:
                                applyText = "(Tab:Change UI position to left/right)";
                                break;
                        }
                    }
                    enabled = true;
                    originText = text.text;
                    text.text = TextDataModel.GetText(KEY) + "\n" + applyText;
                    text.transform.localPosition = text.transform.localPosition.Copy(y: -35f);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
