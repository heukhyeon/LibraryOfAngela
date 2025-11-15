using Battle.DiceAttackEffect;
using GameSave;
using HarmonyLib;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LibraryOfAngela.Util;
using LOR_DiceSystem;
using LOR_XML;
using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Workshop;
using static LibraryOfAngela.Extension.CommonExtension;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;

namespace LibraryOfAngela.EquipBook
{
    internal class AdvancedEquipBookPatch : Singleton<AdvancedEquipBookPatch>
    {
        private Dictionary<string, Type> effects;
        internal List<CorePageConfig> configs;
        public Dictionary<LorId, Model.AdvancedEquipBookInfo> infos;
        private HashSet<LorId> hidePassives = new HashSet<LorId>();
        private IEnumerable<string> equipModPackages;
        private Dictionary<LorId, List<DiceCardXmlInfo>> additionalOnlyCards;

        public void Initialize()
        {
            var equipBookMods = LoAModCache.Instance.Where(x => x.CorePageConfig != null).ToList();
            if (equipBookMods.Count > 0)
            {
                Logger.Log("EquipBook Patch Start");
                equipModPackages = equipBookMods.Select(x => x.packageId);
                configs = new List<CorePageConfig>();
                equipBookMods.ForEach(x =>
                {
                    var config = x.CorePageConfig;
                    config.packageId = x.packageId;
                    configs.Add(config);
                });
                additionalOnlyCards = new Dictionary<LorId, List<DiceCardXmlInfo>>();
                effects = new Dictionary<string, Type>();
                effects["loa_purpletear_H"] = typeof(DiceAttackEffect_loa_purpletear_H);
                effects["loa_purpletear_Z"] = typeof(DiceAttackEffect_loa_purpletear_Z);
                effects["loa_purpletear_J"] = typeof(DiceAttackEffect_loa_purpletear_J);
                try
                {
                    InternalExtension.SetRange(GetType());
                    // HarmonyPatch 어트리뷰트로 대체되므로 제거
                    // typeof(UnitDataModel).PatchInternal("EquipBook", flag: PatchInternalFlag.PREFIX).PatchInternal("EquipCustomCoreBook", flag: PatchInternalFlag.POSTFIX).PatchInternal("LoadFromSaveData", flag: PatchInternalFlag.POSTFIX);
                    // typeof(BookModel).PatchInternal("SetXmlInfo", flag: PatchInternalFlag.POSTFIX);
                    // typeof(UIEquipPageCustomizeSlot).PatchInternal("SetData", PatchInternalFlag.POSTFIX, "UIEquipPageCustomizeSlot_SetData", typeof(BookModel));
                    // typeof(DiceEffectManager).PatchInternal("CreateBehaviourEffect", flag: PatchInternalFlag.POSTFIX);

                    // Patcher.PatchTranspiler(typeof(UILibrarianEquipInfoSlot), GetType(), "SetData");
                    // Patcher.PatchTranspiler(typeof(UILibrarianEquipDeckPanel), GetType(), "SetData", "UILibrarianEquipDeckPanel_SetData");
                    // Patcher.PatchTranspiler(typeof(UILibrarianEquipDeckPanel), GetType(), "SetDeckButton");
                    // Patcher.PatchTranspiler(typeof(UISetInfoSlotListSc), GetType(), "SetStatsDataInEquipBook");
                    // Patcher.PatchTranspiler(typeof(UIInvenEquipPageSlot), GetType(), "SetOperatingPanel");

                    infos = new Dictionary<LorId, Model.AdvancedEquipBookInfo>();

                    hidePassives = new HashSet<LorId>();
                    foreach (var mod in configs)
                    {
                       
                        try
                        {
                            var infos = mod.GetAdvancedEquipBookInfos();
                            if (infos == null) continue;
                            foreach (var info in infos)
                            {
                                this.infos[info.targetId] = info;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log("Error in EquipBookInfo Init : " + mod.packageId);
                            Logger.LogError(e);
                        }

                        try
                        {
                            mod.GetHideCostPassives().ForEach(x => hidePassives.Add(x));
                        }
                        catch (Exception)
                        {

                        }
                    }
                    Logger.Log("EquipBook Patch Complete");
                }
                catch (Exception e)
                {
                    Logger.Log("EquipBook Patch Fail");
                    Logger.LogError(e);
                }
            }
            // typeof(BookModel).PatchInternal("GetThumbSprite", flag: PatchInternalFlag.POSTFIX);
        }

        public Task EnqueueEffect()
        {
            var equipBookMods = LoAModCache.Instance.Where(x => x.CorePageConfig != null).ToList();

            if (equipBookMods.Count == 0) return Task.CompletedTask;

            return Task.Run(() =>
            {
                foreach (var t in equipBookMods.SelectMany(x => x.Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(DiceAttackEffect)) && type.Name.StartsWith("DiceAttackEffect"))))
                {
                    var key = t.Name.Substring("DiceAttackEffect_".Length);
                    effects[key] = t;
                }
            });
        }

        // 데이터 초기화 이후에 처리해야 문제 안됨
        public void OnlyCardInit()
        {
            foreach (var mod in configs)
            {
                var onlyCard = GetSafeAction(() => mod.AddtionalOnlyCards) ?? new List<Model.AdditionalOnlyCardModel>();
                onlyCard.ForEach(x =>
                {
                    if (!additionalOnlyCards.ContainsKey(x.bookId)) additionalOnlyCards[x.bookId] = new List<DiceCardXmlInfo>();
                    additionalOnlyCards[x.bookId].Add(ItemXmlDataList.instance.GetCardItem(x.cardId, false));
                });
            }
        }

        /**
         * 핵심책장 섬네일 바꿔치기
         */
        [HarmonyPatch(typeof(BookModel), "GetThumbSprite")]
        [HarmonyPostfix]
        private static void After_GetThumbSprite(BookModel __instance, ref Sprite __result)
        {
            var key = __instance.GetBookClassInfoId().packageId;
            try
            {
                var owner = LoAModCache.Instance[key];
                if (owner?.ArtworkConfig is null) return;
                var value = owner.ArtworkConfig.ConvertThumbnail(__instance);
                if (!(value is null)) __result = owner.Artworks[value];
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// in UIInvenLeftEquipPageSlot
        /// 
        /// as-is
        /// if (LibraryModel.Instance.PlayHistory.Start_TheBlueReverberationPrimaryBattle == 1)
        /// 
        /// 
        /// to-be
        /// if (EquipBookPatch.CheckEquipable(LibraryModel.Instance.PlayHistory.Start_TheBlueReverberationPrimaryBattle, ref id , this) == 1)
        /// if 에 넣는 이유는 if statement 자체를 변동시키는게 이유가 아닌 최종 처리 이전에 넣을만한 코드가 if 밖에 없었음
        /// </summary>
        /// <param name="instructions"></param>
        [HarmonyPatch(typeof(UIInvenEquipPageSlot), "SetOperatingPanel")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetOperatingPanel(IEnumerable<CodeInstruction> instructions)
        {
            bool fired = false;
            var targets = new object[]
            {
                AccessTools.Method(typeof(LibraryModel), "get_Instance"),
                AccessTools.Method(typeof(LibraryModel), "get_PlayHistory"),
                AccessTools.Field(typeof(PlayHistoryModel), "Start_TheBlueReverberationPrimaryBattle")
            };

            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i< codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (!fired && code.Is(OpCodes.Ldfld, targets[2]) && codes[i - 1].operand == targets[1] && codes[i - 2].operand == targets[0])
                {
                    fired = true;
                   
                    yield return new CodeInstruction(OpCodes.Ldloca_S, (byte) 0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), "CheckEquipable"));
                    
                }
         
            }
        }

        [HarmonyPatch(typeof(UnitDataModel), "EquipBook")]
        [HarmonyPrefix]
        private static void Before_EquipBook(UnitDataModel __instance, ref BookModel newBook, ref bool force, out BookModel __state)
        {
            __state = __instance.bookItem;
            var id = newBook?.BookId;
            if (id == null) return;

            var info = Instance.infos.SafeGet(id)?.equipCondition?.Invoke(__instance);

            if (info == true)
            {
                force = true;
            }
            else if (info == false)
            {
                newBook = null;
            }
        }

        [HarmonyPatch(typeof(UIEquipPageCustomizeSlot), "SetData", new Type[] { typeof(BookModel) })]
        [HarmonyPostfix]
        private static void After_UIEquipPageCustomizeSlot_SetData(BookModel book, UIEquipPageCustomizeSlot __instance)
        {
            var target = __instance.panel.panel.Parent.SelectedUnit.bookItem.BookId;
            if (!__instance.isLocked || Instance.infos.SafeGet(target) == null) return;
            __instance.SetColorFrame(UIEquipPageSlotState.None);
            __instance.isLocked = false;
        }

        [HarmonyPatch(typeof(UnitDataModel), "EquipCustomCoreBook")]
        [HarmonyPostfix]
        private static void After_EquipCustomCoreBook(BookModel custombook, UnitDataModel __instance)
        {
            if (__instance._CustomBookItem == custombook || custombook == null) return;
            if (Instance.infos.SafeGet(__instance.bookItem.BookId) == null) return;
            __instance._CustomBookItem = custombook;
        }

        [HarmonyPatch(typeof(UnitDataModel), "EquipBook")]
        [HarmonyPostfix]
        private static void After_EquipBook(UnitDataModel __instance, BookModel newBook, BookModel __state)
        {

            if (__instance._bookItem != __state)
            {
                if (__state != null)
                {
                    try
                    {
                        var id = __state.GetBookClassInfoId();
                        var config = Instance.configs.Find(d => d.packageId == id.packageId);
                        if (config != null) config.OnCorePageStateChange(__state, id, EquipStateEvent.UnEquip, __instance);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }

                }
                if (newBook != null && __instance._bookItem == newBook)
                {
                    try
                    {
                        var id = newBook.GetBookClassInfoId();
                        var config = Instance.configs.Find(d => d.packageId == id.packageId);
                        if (config != null) config.OnCorePageStateChange(newBook, id, EquipStateEvent.Equip, __instance);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitDataModel), "LoadFromSaveData")]
        [HarmonyPostfix]
        private static void After_LoadFromSaveData(UnitDataModel __instance, SaveData data)
        {
            if (!__instance.isSephirah) return;
            var instanceId = data.GetInt("bookInstanceId");
            if (instanceId > 0 && __instance.bookItem.instanceId != instanceId)
            {
                try
                {
                    var book = BookInventoryModel.Instance.GetBookByInstanceId(instanceId);
                    if (book != null && Instance.infos.ContainsKey(book.GetBookClassInfoId()))
                    {
                        __instance.EquipBook(book);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }

/*        [HarmonyPatch(typeof(DiceEffectManager), "CreateBehaviourEffect")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CreateBehaviourEffect(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var fired = false;
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                if (!fired && codes[i].opcode == OpCodes.Call)
                {
                    var methodInfo = codes[i].operand as MethodInfo;
                    if (methodInfo?.Name == "Load" && methodInfo?.DeclaringType == typeof(Resources))
                    {
                        fired = true;
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), "CreateValidDiceAttackEffect"));
                    }
                }
            }
        }*/

        /// <summary>
        ///  case 1
        ///  as-is : if (i < ____slotList.Count)
        ///  to-be : if (i < ____slotList.Count && EquipBookPatch.CheckVisibleRange(Bdata))
        ///  
        /// case 2
        /// 
        /// as-is : Bdata.GetPassiveInfoList(false)
        /// to-be : EquipBookPatch.GetSettingPassiveInfoList(Bdata.GetPassiveInfoList(false))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UISetInfoSlotListSc), "SetStatsDataInEquipBook")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetStatsDataInEquipBook(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(BookModel), "GetPassiveInfoList");
            var codes = new List<CodeInstruction>(instructions);
            var fired = false;
            var fired2 = false;
            
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].opcode == OpCodes.Bge && !fired)
                {
                    var label = codes[i].operand;
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), "CheckVisibleRange"));
                    yield return new CodeInstruction(OpCodes.Brfalse_S, label);
                }
                // GetPassiveInfoList 는 실제 순회 이전에도 호출하므로 근거리 여부 판정 이후부터 체크하게 한다.
                if (fired && !fired2 && codes[i].Is(OpCodes.Callvirt, target))
                {
                    fired2 = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), "GetSettingPassiveInfoList"));
                }
            }
        }

        /// <summary>
        /// as-is 
        /// if (this._unitdata.IsChangeItemLock())
        /// 
        /// to-be
        ///  if (EquipBookPatch.CheckDeckChangeable(this._unitdata.IsChangeItemLock()))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UILibrarianEquipDeckPanel), "SetData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_UILibrarianEquipDeckPanel_SetData(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(UnitDataModel), "IsChangeItemLock");
            var fired = false;
            foreach(var code in AdvancedCorePageRarityPatch.mappingBookColor(instructions, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(UILibrarianEquipDeckPanel), "_unitdata")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnitDataModel), "get_bookItem"))
            }).ToList())
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), "CheckDeckChangeable"));
                }
            }
        }

        /// <summary>
        /// as-is 
        /// if (this._unitdata.IsChangeItemLock())
        /// 
        /// to-be
        ///  if (EquipBookPatch.CheckDeckChangeable(this._unitdata.IsChangeItemLock()))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UILibrarianEquipDeckPanel), "SetDeckButton")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetDeckButton(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(UnitDataModel), "IsChangeItemLock");
            var fired = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!fired && code.Is(OpCodes.Callvirt, target))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), "CheckDeckChangeable"));
                }
            }
        }

        /// <summary>
        /// UILibrarianEquipInfoSlot.SetData
        /// if (noCost) -> if(EquipBookPatch.isNoCostPassive(noCost, passive)))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UILibrarianEquipInfoSlot), "SetData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                
                if (codes[i].Is(OpCodes.Ldarg_S, 5))
                {
                    yield return codes[i];
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedEquipBookPatch), nameof(IsCostHidePassive)));
                }
                else yield return codes[i];
            }
        }

        [HarmonyPatch(typeof(BookModel), "SetXmlInfo")]
        [HarmonyPostfix]
        private static void After_SetXmlInfo(BookModel __instance)
        {
            try
            {
                var id = __instance?._classInfo?.id;
                if (id == null) return;

                if (Instance.equipModPackages.Contains(id.packageId))
                {
                    foreach(var onlyId in __instance._classInfo.EquipEffect.OnlyCard)
                    {
                        // 어차피 주입 불가능한 경우는 패스
                        var replace = ItemXmlDataList.instance.GetCardItem(new LorId(id.packageId, onlyId), true);
                        if (replace == null) continue;

                        var cnt = __instance._onlyCards.Count;
                        var added = false;
                        for (int i = 0; i < cnt; i++)
                        {
                            var card = __instance._onlyCards[i];
                            if (card._id == onlyId)
                            {
                                __instance._onlyCards[i] = replace;
                                added = true;
                                break;
                            }
                        }
                        if (!added)
                        {
                            __instance._onlyCards.Add(replace);
                        }
                    }
           
                }
                var target = Instance.additionalOnlyCards?.SafeGet(id);
                if (target != null)
                {
                    __instance._onlyCards.AddRange(target);
                }
                
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

        }

        [HarmonyPatch(typeof(DiceEffectManager), "CreateBehaviourEffect")]
        [HarmonyPostfix]
        private static void After_CreateBehaviourEffect(ref DiceAttackEffect __result, 
            string resource, 
            float scaleFactor, 
            BattleUnitView self, 
            BattleUnitView target, 
            float time
            ) {
            if (__result != null) return;
            var t = Instance.effects.SafeGet(resource);
            if (t == null) return;
            var obj = new GameObject();
            try
            {
                __result = obj.AddComponent(t) as DiceAttackEffect;
                __result.Initialize(self, target, time);
                __result.SetScale(scaleFactor);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static DiceCardXmlInfo ConvertValidOnlyCardInfo(DiceCardXmlInfo origin, int cardID, LorId xmlId)
        {
            if (origin != null && origin.workshopID == xmlId.packageId) return origin;

            if (Instance.equipModPackages.Contains(xmlId.packageId))
            {
                return ItemXmlDataList.instance.GetCardItem(new LorId(xmlId.packageId, cardID));
            }
            return origin;
        }

        private static bool CheckDeckChangeable(bool origin)
        {
            if (!origin) return origin;
            
            var id = UI.UIController.Instance.CurrentUnit.bookItem.BookId;
            var info = Instance.infos.SafeGet(id);
            if (info == null) return origin;
            return false;
        }

        private static int CheckEquipable(int originBlueReveration, ref string current, UIInvenEquipPageSlot slot)
        {
            if (originBlueReveration == 1) return originBlueReveration;

            var id = slot._bookDataModel.BookId;
            var info = Instance.infos.SafeGet(id);
            if (info == null) return originBlueReveration;
            var result = info.equipCondition?.Invoke(UI.UIController.Instance.CurrentUnit);

            if (result != null)
            {
                if (current == "ui_bookinventory_equipbook" && result == false)
                {
                    current = "ui_equippage_notequip";
                    slot.button_Equip.interactable = false;
                }
                else if (current == "ui_equippage_notequip" && result == true)
                {
                    current = "ui_bookinventory_equipbook";
                    slot.button_Equip.interactable = true;
                }
            }

            return originBlueReveration;
        }

        private static DiceAttackEffect CreateValidDiceAttackEffect(DiceAttackEffect origin, string resource)
        {
            if (origin != null) return origin;
            var target = Instance.effects.SafeGet(resource);
            if (target == null) return origin;
            var obj = new GameObject();
            return obj.AddComponent(target) as DiceAttackEffect;
        }

        private static bool CheckVisibleRange(BookModel target)
        {
            var result= Instance.infos.SafeGet(target.GetBookClassInfoId())?.hideRangePassive != true;
            return result;
        }
        private static List<BookPassiveInfo> GetSettingPassiveInfoList(List<BookPassiveInfo> origin, BookModel target)
        {
            var result = Instance.infos.SafeGet(target.GetBookClassInfoId())?.settingPassive;
            if (result != null)
            {
                origin.InsertRange(0, result.Select(x => new BookPassiveInfo
                {
                    passive = PassiveXmlList.Instance.GetData(x)
                }));
            }
            return origin;
        }
        private static bool IsCostHidePassive(bool origin, BookPassiveInfo info)
        {
            try
            {
                return origin || Instance.hidePassives.Contains(info.passive.id);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }
        }
    }
}
