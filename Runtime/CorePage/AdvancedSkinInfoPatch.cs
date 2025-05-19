using HarmonyLib;
using LibraryOfAngela.Battle;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Model;
using LibraryOfAngela.SD;
using LibraryOfAngela.Story;
using LoALoader.Model;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using Workshop;

namespace LibraryOfAngela.CorePage
{
    class AdvancedSkinInfoPatch : Singleton<AdvancedSkinInfoPatch>
    {
        public Dictionary<string, CorePageConfig> configs { get; private set; }
        public Dictionary<SkinComponentKey, Type> skinComponentTypes { get; private set; }
        public Dictionary<string, AdvancedSkinInfo> infos { get; private set; }
        private Dictionary<BattleDialogueModel, BattleDialogueModel> mappingDialogs;
        private Regex namePattern;

        public void Initialize()
        {
            infos = new Dictionary<string, AdvancedSkinInfo>();
            mappingDialogs = new Dictionary<BattleDialogueModel, BattleDialogueModel>();
            configs = new Dictionary<string, CorePageConfig>();

            foreach (var mod in AdvancedEquipBookPatch.Instance.configs)
            {
                configs[mod.packageId] = mod;
                foreach (var info in FrameworkExtension.GetSafeAction(() => mod.GetAdvancedSkinInfos()) ?? new List<AdvancedSkinInfo>())
                {
                    if (info.skinComponentType != null)
                    {
                        if (skinComponentTypes is null)
                        {
                            skinComponentTypes = new Dictionary<SkinComponentKey, Type>();
                        }
                        skinComponentTypes[new SkinComponentKey { packageId = mod.packageId, skinName = info.skinName }] = info.skinComponentType;
                    }
                    info.packageId = mod.packageId;
                    infos[info.skinName] = info;
                    if (!string.IsNullOrEmpty(info.prefabKey))
                    {
                        var skinList = CustomizingBookSkinLoader.Instance._bookSkinData.SafeGet(mod.packageId);
                        if (skinList is null)
                        {
                            skinList = new List<WorkshopSkinData>();
                            CustomizingBookSkinLoader.Instance._bookSkinData[mod.packageId] = skinList;
                        }
                        bool flag = false;
                        for (int i = 0; i < skinList.Count; i++)
                        {
                            var s = skinList[i];
                            if (s.dataName == info.skinName)
                            {
                                flag = true;
                                skinList.RemoveAt(i);
                                s = new LoAWorkshopSkinData
                                {
                                    contentFolderIdx = mod.packageId,
                                    dataName = info.skinName,
                                    id = 0,
                                    prefab = info.prefabKey,
                                    dic = new Dictionary<ActionDetail, ClothCustomizeData>()
                                };
                                skinList.Insert(i, s);
                                break;
                            }
                        }
                        if (!flag)
                        {
                            skinList.Add(new LoAWorkshopSkinData
                            {
                                contentFolderIdx = mod.packageId,
                                dataName = info.skinName,
                                id = 0,
                                prefab = info.prefabKey,
                                dic = new Dictionary<ActionDetail, ClothCustomizeData>()
                            });
                        }
                        // Logger.Log($"Inject AdditionalSkin : {info.packageId}//{info.skinName}");
                    }
                }
            }
            InternalExtension.SetRange(GetType());
            SkinRenderPatch.Instance.Initialize();
            var skins = infos.Values.ToList();
            FacePatch.Initialize(skins);

            var language = GlobalGameManager.Instance.CurrentOption.language;
            if (language == "kr")
            {
                namePattern = new Regex("[a-z|A-Z|0-9|ㄱ-ㅎ|ㅏ-ㅣ|가-힣| ]*(?=의 책장)");
            }
            else if (language == "en")
            {
                namePattern = new Regex(@"[a-zA-Z0-9ㄱ-ㅎㅏ-ㅣ가-힣 ]*(?=(\x27s Page|\x27 Page))");
            }
            else if (language == "jp")
            {
                namePattern = new Regex("[a-z|A-Z|0-9|一-龯|一-龠|[ぁ-ゔ|ァ-ヴー| ]*(?=のページ)");
            }
        }

        public static AdvancedSkinInfo GetInfo(BattleUnitModel model)
        {
            return Instance.infos.SafeGet(GetCurrentSkinName(model.view));
        }

        [HarmonyPatch(typeof(UICustomizeMainTap), "init")]
        [HarmonyPostfix]
        private static void After_init(UICustomizeMainTap __instance)
        {
            var toggle = __instance.GetComponent<SkinInfoToggle>();
            if (toggle == null) toggle = __instance.gameObject.AddComponent<SkinInfoToggle>();
            toggle.UpdateTarget();
        }

        /// <summary>
        /// 특정 핵심책장을 장착한 사서의 표기 이름을 변경합니다.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        [HarmonyPatch(typeof(UnitDataModel), "get_name")]
        [HarmonyPostfix]
        private static void After_get_name(UnitDataModel __instance, ref string __result)
        {
            try
            {
                if (!string.IsNullOrEmpty(__instance._tempName) || __instance._enemyUnitId != null) return;

                var bookId = __instance.CustomBookItem._characterSkin;
                var name = Instance.infos?.SafeGet(bookId)?.customOwnerName?.Invoke(__instance);
                if (string.IsNullOrEmpty(name)) name = AdvancedEquipBookPatch.Instance.infos.SafeGet(__instance.bookItem.BookId)?.customOwnerName?.Invoke(__instance);
                if (string.IsNullOrEmpty(name))
                {
                    if (SkinInfoProvider.Instance.isPropertyEnabled(__instance, SkinProperty.KEY_ORIGIN_SKIN, SkinProperty.KEY_ORIGIN_SKIN_EXP))
                    {
                        var value = Instance.namePattern?.Match(__instance.CustomBookItem.Name)?.Value;
                        __result = string.IsNullOrEmpty(value) ? __result : value;
                    }
                }
                else
                {
                    __result = name;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        /// <summary>
        /// 특정 핵심책장을 장착한 사서의 다이얼로그 출력을 변경합니다.
        /// </summary>
        /// <param name="dlgType"></param>
        /// <param name="targets"></param>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        [HarmonyPatch(typeof(BattleDialogueModel), "GetBattleDlg", new Type[] { typeof(DialogType), typeof(List<BattleUnitModel>) })]
        [HarmonyPostfix]
        private static void After_GetBattleDlg(DialogType dlgType, List<BattleUnitModel> targets, BattleDialogueModel __instance, ref string __result)
        {
            var dlg = Instance.mappingDialogs.SafeGet(__instance);
            if (dlg != null) __result = dlg.GetBattleDlg(dlgType , targets);
            
        }

        /// <summary>
        /// 무대가 시작될때마다 다이얼로그 매핑을 초기화합니다.
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(StageController), "StartBattle")]
        [HarmonyPrefix]
        private static void Before_StartBattle(StageController __instance)
        {
            try
            {
                foreach (var p in Instance.mappingDialogs.Values)
                {
                    if (p is IOwnerDetectableBattleDialog e)
                    {
                        e.Owner = null;
                    }
                }
                Instance.mappingDialogs.Clear();

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void RegisterDialog(UnitDataModel unit)
        {
            try
            {
                if (unit.battleDialogModel is null) return;

                var stage = StageController.Instance.GetStageModel();
                var stageId = stage.ClassInfo.id;
                CustomStoryInfo info = StoryPatch.Instance.storyInfos.SafeGet(stageId);

                BattleDialogueModel dialog = FrameworkExtension.GetSafeAction(() => info?.overrideDialog?.Invoke(unit));
                if (dialog is null)
                {
                    dialog = Instance.infos.SafeGet(unit.CustomBookItem._characterSkin)?.customDialog?.Invoke();
                    if (dialog == null)
                        dialog = AdvancedEquipBookPatch.Instance.infos.SafeGet(unit.bookItem.BookId)?.customDialog?.Invoke();
                }
                if (dialog != null)
                {
                    if (dialog is IOwnerDetectableBattleDialog p)
                    {
                        p.Owner = unit;
                    }
                    Instance.mappingDialogs[unit.battleDialogModel] = dialog;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

        }

        [HarmonyPatch(typeof(BattleUnitView), "ChangeHeight")]
        [HarmonyPrefix]
        private static bool Before_ChangeHeight(BattleUnitView __instance, ref int height)
        {
            var fixedHeight = Instance.infos.SafeGet(GetCurrentSkinName(__instance))?.fixedHeight ?? -1;

            height = fixedHeight > 0 ? fixedHeight : height;
            return true;
        }

        [HarmonyPatch(typeof(RencounterManager), "GetBehaviourAction")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetBehaviourAction(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(RencounterManager), "CreateBehaviourAction");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Call, target))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSkinInfoPatch), nameof(ConvertDefaultActionScript)));
                }
            }
        }

        /// <summary>
        /// (float)uicharacter.unitModel.customizeData.height
        /// ->
        /// (float) EquipBookPatch.ChangeFixedHeightInCharacterRenderer(uicharacter.unitModel.customizeData.height, uicharacter);
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UICharacterRenderer), "GetRenderTextureByIndexAndSize")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetRenderTextureByIndexAndSize(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(UnitCustomizingData), "get_height");
            var fired = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Callvirt, target))
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSkinInfoPatch), "ChangeFixedHeightInCharacterRenderer"));
                }
            }
        }

        private static void UICharacterRenderer_GetRenderTextureByIndexAndSize_Post(UICharacterRenderer __instance, ref Texture __result, int index)
        {
            if (index >= 0 && index <= 10) return;
            if (!(__result is null)) return;
            if (index > __instance.characterList.Count - 1) return;

            Vector2 v = Vector2.one;
            UICharacter uicharacter = __instance.characterList[index];
            if (uicharacter.unitModel != null)
            {
                float d = (float)uicharacter.unitModel.customizeData.height;
                if (uicharacter.unitAppearance != null)
                {
                    v = Vector2.one * d * 0.005f;
                    uicharacter.unitAppearance.transform.localScale = v;
                }
                __result = __instance.cameraList[index].targetTexture;
            }
        }

        private static int ChangeFixedHeightInCharacterRenderer(int origin, UI.UICharacter characterSlot)
        {
            var unit = characterSlot?.unitModel;
            var height = Instance.infos.SafeGet(characterSlot?.unitModel?.CustomBookItem?._characterSkin)?.fixedHeight ?? -1;
            if (height > 0) return height;
            return origin;
        }

        /// <summary>
        /// if (flag)
        /// ->
        /// if (AdvancedSkinInfoPatch.IsUnitStartMoveHold(flag, list[j]))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(StageController), "MoveUnitPhase")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_MoveUnitPhase(IEnumerable<CodeInstruction> instructions)
        {
            bool fired = false;
            foreach(var code in instructions)
            {
                yield return code;
                if (!fired && code.GetIndex() == 11)
                {
                    fired = true;
                    // list
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 0x07);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<BattleUnitModel>), "get_Item"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSkinInfoPatch), "IsUnitStartMoveHold"));
                }
            }
        }

        /// <summary>
        /// UnityEngine.Object.Instantiate<GameObject>(gameObject, this.characterRotationCenter) ->
        /// FixValidChangeSkin(UnityEngine.Object.Instantiate<GameObject>(gameObject, this.characterRotationCenter))
        /// 
        /// 모드에서 ChangeSkin 사용시 prefix 없이 자연스럽게 흘러가기 위한 코드
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitView), "ChangeEgoSkin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ChangeEgoSkin(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                yield return code;
                if (code.opcode == OpCodes.Call && (code.operand as MethodInfo)?.Name == "Instantiate")
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSkinInfoPatch), nameof(FixValidChangeSkin)));
                }
            }
        }

        /// <summary>
        /// UnityEngine.Object.Instantiate<GameObject>(gameObject, this.characterRotationCenter) ->
        /// FixValidChangeSkin(UnityEngine.Object.Instantiate<GameObject>(gameObject, this.characterRotationCenter))
        /// 
        /// 모드에서 ChangeSkin 사용시 prefix 없이 자연스럽게 흘러가기 위한 코드
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleUnitView), "ChangeSkin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ChangeSkin(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                yield return code;
                if (code.opcode == OpCodes.Call && (code.operand as MethodInfo)?.Name == "Instantiate")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSkinInfoPatch), nameof(FixValidChangeSkin)));
                }
            }
        }

        bool isSkinChanged = false;

        [HarmonyPatch(typeof(BattleUnitView), "ChangeSkin")]
        [HarmonyPostfix]
        private static void After_ChangeSkin(BattleUnitView __instance, string charName)
        {

            try
            {
                if (Instance.isSkinChanged)
                {
                    Instance.isSkinChanged = false;
                    var com = __instance?.charAppearance?.GetComponent<WorkshopSkinDataSetter>();
                    if (com?.Appearance?.CustomAppearance is null) return;
                    com.LateInit();
                }
                foreach (var eff in BattleInterfaceCache.Of<IHandleChangeSkin>(__instance.model))
                {
                    eff.OnChangeSkin(charName);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(BattleUnitView), "ChangeEgoSkin")]
        [HarmonyPostfix]
        private static void After_ChangeEgoSkin(BattleUnitView __instance, string egoName)
        {
            After_ChangeSkin(__instance, egoName);
        }

        /// <summary>
        /// 유닛이 합 시작시 움직일지 여부를 반환한다.
        /// </summary>
        /// <param name="origin">원거리 책장, 스폐셜 책장등을 쓰는경우 ( = 별다른 코드없이도 원래부터 안움직이는경우) true, 그 외에는 false </param>
        /// <param name="unit">현재 유닛</param>
        /// <returns></returns>
        private static bool IsUnitStartMoveHold(bool origin, BattleUnitModel unit)
        {
            if (origin) return true;

            try
            {
                // 현재 대상이 원거리 반격 (공격) 주사위를 가진 경우
                if (TargetIsStandbyRangeAttack(unit) || TargetIsStandbyRangeAttack(unit.currentDiceAction.target))
                {
                    unit.moveDetail.Stop();
                    unit.currentDiceAction.target.moveDetail.Stop();
                    return true;
                }
                if (!string.IsNullOrEmpty(unit.currentDiceAction?.card?.XmlData?.SkinChange)) return origin;
                var info = Instance.infos.SafeGet(GetCurrentSkinName(unit.view));
                if (info == null) return origin;

                if (info?.isStartMoveable?.Invoke(unit.currentDiceAction) == false)
                {
                    if (unit.view?.charAppearance?.GetCurrentMotionDetail() == ActionDetail.Move)
                    {
                        unit.view?.charAppearance?.ChangeMotion(ActionDetail.Default);
                    }
                    unit.moveDetail.Stop();
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return origin;
        }

        private static bool TargetIsStandbyRangeAttack(BattleUnitModel target)
        {
            if (target.currentDiceAction?.isKeepedCard == false) return false;
            return BattlePatch.isStandbyAttackDice(target.cardSlotDetail.keepCard.cardBehaviorQueue?.FirstOrDefault());
        }

        public static BehaviourActionBase ConvertDefaultActionScript(BehaviourActionBase origin, string script, ref RencounterManager.ActionAfterBehaviour self)
        {
            try
            {
                var view = self.view;
                var skin = GetCurrentSkinName(view);
                var result = self.behaviourResultData;
                var action = FrameworkExtension.GetSafeAction(() => Instance.infos.SafeGet(skin)?.overrideActionScript)?.Invoke(result, origin);
                if (action != null) return action;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            if (string.IsNullOrEmpty(script))
            {
                var view = self.view;
                var skin = GetCurrentSkinName(view);
                UnitDataModel unit = view?.model?.UnitData?.unitData;
                if (skin == "Argalia2" && SkinInfoProvider.Instance.isPropertyEnabled(unit, SkinProperty.KEY_ARGALIA_CUSTOM_ACTION))
                {
                    return new BehaviourAction_loa_argalia_default_action();
                }
                else if (skin?.StartsWith("ThePurpleTear") == true && SkinInfoProvider.Instance.isPropertyEnabled(unit, SkinProperty.KEY_PURPLE_VFX))
                {
                    return new BehaviourAction_loa_purpletear();
                }
                return Instance.infos.SafeGet(skin)?.defaultActionScript?.Invoke() ?? origin;
            }
            return origin;
        }

        public static BehaviourActionBase ConvertDefaultActionScript2(BehaviourActionBase origin, BattleCardBehaviourResult result)
        {
            if (string.IsNullOrEmpty(result?.behaviourRawData?.ActionScript) && result.hasBehaviour)
            {
                var view = result.behaviour?.card?.owner?.view;
                var skin = GetCurrentSkinName(view);
                UnitDataModel unit = view?.model?.UnitData?.unitData;
                if (skin == "Argalia2" && SkinInfoProvider.Instance.isPropertyEnabled(unit, SkinProperty.KEY_ARGALIA_CUSTOM_ACTION))
                {
                   return new BehaviourAction_loa_argalia_default_action();
                }
                else if (skin?.StartsWith("ThePurpleTear") == true && SkinInfoProvider.Instance.isPropertyEnabled(unit, SkinProperty.KEY_PURPLE_VFX))
                {
                    return new BehaviourAction_loa_purpletear();
                }
                var info = Instance.infos.SafeGet(skin);
                if (info is null) return origin;
                var sc = info.defaultActionScript?.Invoke() ?? info.overrideActionScript?.Invoke(result, origin);
                if (sc != null) return sc;
                return origin;
            }
            return origin;
        }

        /// <summary>
        /// BehaviourAction_vibrateAddition
        /// 
        /// 진동 강제 적용
        /// </summary>
        /// <param name="self"></param>
        /// <param name="__result"></param>
        [HarmonyPatch(typeof(BehaviourAction_vibrateAddition), "GetMovingAction")]
        [HarmonyPostfix]
        public static void After_GetMovingAction(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent, ref List<RencounterManager.MovingAction> __result)
        {
            var view = self.view;
            var skin = GetCurrentSkinName(view);
            if (skin == "Argalia2" && SkinInfoProvider.Instance.isPropertyEnabled(self.view.model.UnitData.unitData, SkinProperty.KEY_ARGALIA_CUSTOM_ACTION))
            {
                __result = new BehaviourAction_loa_argalia_default_action().GetMovingAction(ref self, ref opponent);
            }
        }

        /// <summary>
        /// 현재 유닛에 대한 스킨 정보를 반환. E.G.O 발현일땐 그 값을 반환하고 아닐땐 장착한 책장의 정보를 반환한다.
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public static string GetCurrentSkinName(BattleUnitView view)
        {
            if (view == null) return "";

            if (!string.IsNullOrEmpty(view._skinInfo?.skinName) && view._skinInfo?.state != BattleUnitView.SkinState.Default)
            {
                return view._skinInfo.skinName;
            }

            if (!string.IsNullOrEmpty(view._altSkinInfo?.skinName) && view._altSkinInfo?.state != BattleUnitView.SkinState.Default)
            {
                return view._altSkinInfo.skinName;
            }

            return SkinInfoProvider.ConvertValidSkinName(view.model?.customBook?._characterSkin ?? "", view.model.UnitData.unitData);
        }
    
        private static GameObject FixValidChangeSkin(GameObject origin, string name)
        {
            if (origin is null)
            {
                if (LoAFramework.DEBUG)
                {
                    Logger.Log("ChangeSkin Called, But Object is null ...? :" + name);
                }
                return origin;
            }

            try
            {
                name = name.StartsWith("[Prefab]") ? name.Substring(8) : name;
                var data = Instance.infos.SafeGet(name);
                if (data != null)
                {
                    WorkshopSkinData workshopBookSkinData = Singleton<CustomizingBookSkinLoader>.Instance.GetWorkshopBookSkinData(
        data.packageId,
        name
    );
                    WorkshopSkinDataSetter component = origin.GetComponent<WorkshopSkinDataSetter>();
                    if (workshopBookSkinData != null)
                    {
                        component.SetData(workshopBookSkinData);
                    }
                    Instance.isSkinChanged = true;
                }
                else if (LoAFramework.DEBUG)
                {
                    Logger.Log("ChangeSkin Called, But Info Not Found :" + name);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }


            return origin;
        }
    }
}
