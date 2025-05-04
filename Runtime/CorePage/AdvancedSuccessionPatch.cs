using GameSave;
using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfAngela.CorePage
{
    class AdvancedSuccessionPatch : Singleton<AdvancedSuccessionPatch>
    {
        private Dictionary<string, SuccessionConfig> configs;
        private string[] packageIds;
        // 계승불가 책장 목록 Id
        private HashSet<LorId> invalidSuccessionIds = new HashSet<LorId>();
        private Dictionary<LorId, List<LorId>> onlyIdChilds = new Dictionary<LorId, List<LorId>>();
        private Dictionary<LorId, LorId> passiveMappings = new Dictionary<LorId, LorId>();
        private Dictionary<LorId, List<LorId>> onlyIdParents = new Dictionary<LorId, List<LorId>>();
        
        public void Initialize()
        {
            configs = LoAModCache.Instance.Select(x => x.SuccessionConfig).Where(x => x != null).ToDictionary(x => x.packageId);

            packageIds = configs.Keys.ToArray();

            if (packageIds.Length > 0)
            {
                invalidSuccessionIds = configs.Values.SelectMany(x => x.GetInvalidSuccessionBookId()).ToHashSet();

                foreach(var c in configs.Values)
                {
                    var dic = c.GetOnlySuccessionBookId();
                    if (dic != null) { 
                        foreach(var pair in dic)
                        {
                            if (onlyIdParents.ContainsKey(pair.Key))
                            {
                                onlyIdParents[pair.Key].AddRange(pair.Value);
                            }
                            else
                            {
                                onlyIdParents[pair.Key] = pair.Value;
                            }
  
                            pair.Value.ForEach(x =>
                            {
                                if (onlyIdChilds.ContainsKey(x)) onlyIdChilds[x].Add(pair.Key);
                                else onlyIdChilds[x] = new List<LorId> { pair.Key };
                            });
                        }
                    }
                    var dic2 = c.GetMappingOnlySuccessionPassives();
                    if (dic2 != null)
                    {
                        foreach (var pair in dic2) passiveMappings[pair.Key] = pair.Value;
                    }
                }
                InternalExtension.SetRange(GetType());
            }
        }

        /// <summary>
        /// 귀속 전용 책장이 안보이게 한다.
        /// </summary>
        /// <param name="__result"></param>
        [HarmonyPatch(typeof(UIEquipPageScrollList), "FilterBookModels")]
        [HarmonyPostfix]
        private static void After_FilterBookModels(ref List<BookModel> __result)
        {
            try
            {
                __result.RemoveAll(x =>
                {
                    var id = x._classInfo.workshopID;
                    return Instance.packageIds.Contains(id) && x._classInfo.canNotEquip;
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        /// <summary>
        /// list.AddRange(books.FindAll((BookModel x) => x.ClassInfo.Chapter == (int)c));
        /// ->
        /// list.AddRange(books.FindAll(IsInvalidSuccessionBook((BookModel x) => x.ClassInfo.Chapter == (int)c)));
        /// </summary>
        /// <param name="__instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIPassiveSuccessionBookListPanel), "GetBooksByGradeFilterUI")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetBooksByGradeFilterUI(IEnumerable<CodeInstruction> __instructions)
        {
            var target = AccessTools.Method(typeof(List<BookModel>), "FindAll");
            var flag = false;
            foreach (var code in __instructions)
            {
                if (!flag && code.Is(OpCodes.Callvirt, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "IsInvalidSuccessionBook"));
                }
                if (code.opcode == OpCodes.Ret)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "RemoveInvalidBook"));
                }
                yield return code;
            }
        }
        
        [HarmonyPatch(typeof(UIPassiveSuccessionBookListPanel), "SortProcess")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SortProcess(IEnumerable<CodeInstruction> __instructions)
        {
            var target = AccessTools.Constructor(typeof(BookModelPriority));
            var flag = false;
            foreach (var code in __instructions)
            {
                yield return code;
                if (!flag && code.Is(OpCodes.Newobj, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "GetValidSort"));
                }
            }
        }

        [HarmonyPatch(typeof(UIPassiveSuccessionPopup), "ChangePassive")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ChangePassive(IEnumerable<CodeInstruction> __instructions)
        {
            var target = AccessTools.Method(typeof(List<UIPassiveSuccessionSlot>), "Find");
            var flag = false;
            foreach (var code in __instructions)
            {
                yield return code;
                if (!flag && code.Is(OpCodes.Callvirt, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "FindMatchedSlot"));
                }
         
            }
        }

        [HarmonyPatch(typeof(BookModel), "CheckOverlapPassive")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CheckOverlapPassive(IEnumerable<CodeInstruction> __instructions)
        {
            var target = AccessTools.Method(typeof(List<PassiveModel>), "FindAll");
            var cnt = 0;
            foreach (var code in __instructions)
            {
                yield return code;
                if (cnt < 2 && code.Is(OpCodes.Callvirt, target))
                {
                    if (++cnt == 2)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), nameof(GetValidFilter)));
                    }
                } 
            }
        }

        [HarmonyPatch(typeof(UIPassiveSuccessionList), "SetEquipModelData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetEquipModelData(IEnumerable<CodeInstruction> __instructions)
        {
            var target = AccessTools.Field(typeof(PassiveXmlInfo), "CanReceivePassive");
            var flag = false;
            foreach (var code in __instructions)
            {
                yield return code;
                if (!flag && code.Is(OpCodes.Ldfld, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "FixValidReleasable"));
                }
            }
        }

        /// <summary>
        /// flag2 = (passiveModel.reservedData.currentpassive.InnerTypeId == targetpassive.originpassive.InnerTypeId);
        /// ->
        /// flag2 = FilterValidInnerType((passiveModel.reservedData.currentpassive.InnerTypeId == targetpassive.originpassive.InnerTypeId));
        /// </summary>
        /// <param name="__instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BookModel), "CanSuccessionPassive")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CanSuccessionPassive(IEnumerable<CodeInstruction> __instructions)
        {
            var flagCeq = false;
            var flag = false;
            foreach(var code in __instructions)
            {
             
                if (code.opcode == OpCodes.Ceq)
                {
                    flagCeq = true;
                }
                else if (flagCeq && !flag && code.opcode == OpCodes.Stloc_2)
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "FilterValidInnerType"));
                }
                yield return code;
            }
        }

        [HarmonyPatch(typeof(BookInventoryModel), "GetBookList_PassiveEquip")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetBookList_PassiveEquip(IEnumerable<CodeInstruction> __instructions)
        {
            var target = AccessTools.Method(typeof(BookModel), "IsEquipedPassiveBook");
            var flag = false;
            foreach (var code in __instructions)
            {
                yield return code;
                if (!flag && code.Is(OpCodes.Callvirt, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "FilterInvalidOwner"));
                }
            }
        }

        /// <summary>
        /// !list.Exists ->
        /// !(FilterSuccessionOnly(list.Exists(), i, bookModels))
        /// </summary>
        /// <param name="__instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(UIStoryArchivesPanel), "InitData")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_InitData(IEnumerable<CodeInstruction> __instructions)
        {
            FieldInfo targetInfo = null;
            FieldInfo targetInfo2 = null;
            var target = AccessTools.Method(typeof(List<BookXmlInfo>), "Exists");
            var flag = false;
            foreach (var code in __instructions)
            {
                yield return code;
                if (targetInfo == null && code.opcode == OpCodes.Stfld && (code.operand as FieldInfo)?.Name == "bookModels")
                {
                    targetInfo = code.operand as FieldInfo;
                }
                else if (targetInfo2 == null && code.opcode == OpCodes.Stfld && (code.operand as FieldInfo)?.Name == "i")
                {
                    targetInfo2 = code.operand as FieldInfo;
                }
                else if (!flag && targetInfo != null && code.Is(OpCodes.Callvirt, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Ldfld, targetInfo2);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, targetInfo);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSuccessionPatch), "FilterSuccessionOnly"));
                }
            }
        }

        // 속주 커스텀 패시브에 대해 커스텀 패시브는 적용하면서 다른 속주 패시브는 차단하는 기능
        private static bool FilterValidInnerType(bool origin, PassiveModel model, PassiveModel target)
        {
            if (!origin) return origin;
            var reserveId = model.reservedData.currentpassive.id;
            var targetId = target.originpassive.id;
            if (Instance.passiveMappings.SafeGet(targetId) == reserveId)
            {
                return false;
            }
            return origin;
        }

        private static bool FilterSuccessionOnly(bool origin, int i, List<BookModel> all)
        {
            if (origin) return origin;
            try
            {
                var target = all[i]?.ClassInfo;
                if (target == null) return origin;
                return target.canNotEquip == true && Instance.packageIds.Contains(target.workshopID);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }

        }
        private static List<BookModelPriority.FactorMethod> GetValidSort(List<BookModelPriority.FactorMethod> origin)
        {
            var unit = UI.UIController.Instance.CurrentUnit.bookItem.BookId;
            var onlyIds = Instance.onlyIdParents.SafeGet(unit);
            int max = onlyIds?.Count ?? 0;
            if (max > 0)
            {
                origin.Insert(0, (a, b) =>
                {
                    var aHas = onlyIds.Contains(a.BookId);
                    var bHas = onlyIds.Contains(b.BookId);
                    return aHas.CompareTo(bHas);
                });
            }
            return origin;
        }

       
        private static List<PassiveModel> GetValidFilter(List<PassiveModel> origin, BookModel book)
        {
            try
            {
                if (book.BookId.IsBasic() || Instance.packageIds.Contains(book.BookId.packageId))
                {
                    origin.RemoveAll(x =>
                    {
                        if (x.originpassive is null) return true;

                        return Instance.passiveMappings.Values.Contains(x.originpassive.id);
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return origin;
        }

        private static Predicate<BookModel> IsInvalidSuccessionBook(Predicate<BookModel> origin)
        {
            var currentId = UI.UIController.Instance.CurrentUnit.bookItem.BookId;
            return delegate (BookModel x)
            {
                if (!origin(x)) return false;
                var check1 = Instance.invalidSuccessionIds.Contains(x.BookId);
                if (check1) return false;
                if (Instance.onlyIdChilds.ContainsKey(x.BookId)) return !Instance.onlyIdChilds[x.BookId].Contains(currentId);
                return true;
            };
        }
        
        private static UIPassiveSuccessionSlot FindMatchedSlot(UIPassiveSuccessionSlot origin, UIPassiveSuccessionPopup instance, UIPassiveSuccessionCenterPassiveSlot selectedcenterSlot)
        {
            if (selectedcenterSlot is null)
            {
                return origin;
            }
            try
            {
                var passive = selectedcenterSlot.Passivemodel.originpassive.id;
                var match = Instance.passiveMappings.SafeGet(passive);
                if (match is null)
                {
                    return origin;
                }
                return instance.equipPassiveList.slotlist.Find(x => x.passivemodel.originpassive.id == match);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }

        }

        [HarmonyPatch(typeof(PassiveModel), "LoadFromSaveData")]
        [HarmonyPostfix]
        private static void After_LoadFromSaveData(SaveData data, PassiveModel __instance)
        {
            if (__instance.originData.currentpassive is null)
            {
                Logger.Log($"Invalid Succession Passive Detect :: {__instance.originpassive?.id} <-- ?? // ({__instance.originData.receivepassivebookId})");
                __instance.originData.givePassiveBookId = -1;
                __instance.originData.receivepassivebookId = -1;
                __instance.originData.currentpassive = __instance.originpassive;
            }
        }

        private static bool FixValidReleasable(bool origin, List<PassiveModel> passives, int i)
        {
            if (origin) return origin;

            var book = UI.UIController.Instance.CurrentUnit.bookItem;
      
            if (book.BookId.IsBasic() || Instance.packageIds.Contains(book.BookId.packageId))
            {
                var originPassives = book.equipeffect.PassiveList;
                var cnt = originPassives.Count;
                if (i >= cnt) return origin;
                return passives[i].reservedData.currentpassive.id != originPassives[i];
            }
            return origin;
        }

        /// <summary>
        /// 다른 핵심책장에서 전용 귀속 책장이 보이지 않게한다.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private static bool FilterInvalidOwner(bool origin, BookModel model)
        {
            if (origin) return origin;
            var id = model.GetBookClassInfoId();
            // 바닐라 책장이면 해당 사항 자체가 없음
            if (id.IsBasic()) return origin;
            if (!Instance.onlyIdChilds.ContainsKey(id)) return origin;
            try
            {
                return !Instance.onlyIdChilds[id].Contains(UI.UIController.Instance.CurrentUnit.bookItem.BookId);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }
        }

        private static List<BookModel> RemoveInvalidBook(List<BookModel> origin, List<Grade> grade)
        {
            if (grade.Count == 0)
            {
                var currentId = UI.UIController.Instance.CurrentUnit.bookItem.BookId;
                origin.RemoveAll(x =>
                {
                    var check1 = Instance.invalidSuccessionIds.Contains(x.BookId);
                    if (check1) return true;
                    if (Instance.onlyIdChilds.ContainsKey(x.BookId)) return !Instance.onlyIdChilds[x.BookId].Contains(currentId);
                    return false;
                });
            }
            return origin;
        }

        [HarmonyPatch(typeof(BookModel), "GetCurrentPassiveCost")]
        [HarmonyPostfix]
        private static void After_GetCurrentPassiveCost(BookModel __instance, ref int __result)
        {
            try
            {
                if (__instance.BookId.IsBasic() || Instance.packageIds.Contains(__instance.BookId.packageId))
                {
                    var originPassives = __instance.equipeffect.PassiveList;
                    var cnt = originPassives.Count;
                    for (int i = 0; i < cnt; i++)
                    {
                        var current = __instance._activatedAllPassives[i];
                        var origin = originPassives[i];
                        var target = current.reservedData ?? current.originData;
                        if (target.currentpassive?.id != origin)
                        {
                            __result -= current.originpassive.cost;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    

    }
}
