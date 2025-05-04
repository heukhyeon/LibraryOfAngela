using HarmonyLib;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using LibraryOfAngela.SD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;
using Workshop;

namespace LibraryOfAngela.CorePage
{
    class WorkshopSkinExportPatch  :Singleton<WorkshopSkinExportPatch>
    {
        private Dictionary<string, BookXmlInfo> matchedInfos = new Dictionary<string, BookXmlInfo>();

        public static string GetModSkinDataName(string packageId, string originSkin)
        {
            return packageId + "_" + originSkin;
        }

        public void Initialize(List<AdvancedSkinInfo> infos)
        {
            var loader = CustomizingResourceLoader.Instance;
            var books = BookXmlList.Instance._workshopBookDict;
            var id = loader._skinData.Count > 0 ? loader._skinData.Max(d => d.Value.id) + 1 : 0;
            foreach (var c in infos
                .Where(d => FrameworkExtension.GetSafeAction(() => d.exportWorkshopSkinMatchedId) != null)
                .GroupBy(d => d.packageId))
            {
                var packageId = c.Key;
                var xmlList = books.SafeGet(packageId);
                var datas = CustomizingBookSkinLoader.Instance.GetWorkshopBookSkinData(packageId);
                if (xmlList is null)
                {
                    Logger.Log($"Mod Skin Data is Null...? Please Check : {packageId}");
                    continue;
                }
                if (datas is null)
                {
                    Logger.Log($"Mod Skin is Null...? Please Check : {packageId}");
                    continue;
                }
                foreach (var skinInfo in c)
                {
                    var targetBook = BookXmlList.Instance._dictionary.SafeGet(skinInfo.exportWorkshopSkinMatchedId);
                    var name = GetModSkinDataName(packageId, skinInfo.skinName);
                    var t = datas.Find(d => d.dataName == skinInfo.skinName);
                    if (t is null)
                    {
                        Logger.Log($"Mod Skin Not Found, PleaseCheck : {packageId} // {skinInfo.skinName}");
                    }
                    loader._skinData[skinInfo.skinName] = new LoAWorkshopImportSkinData
                    {
                        id = id++,
                        contentFolderIdx = skinInfo.skinName,
                        dataName = name,
                        originPackageId = packageId,
                        originSkinName = skinInfo.skinName,
                        dic = t?.dic
                    };
                    matchedInfos[skinInfo.skinName] = targetBook;
                }
            }
            if (matchedInfos.Count > 0)
            {
                InternalExtension.SetRange(GetType());
            }
        }

        [HarmonyPatch(typeof(UIEquipPageCustomizeSlot), "SetData", new Type[] { typeof(WorkshopSkinData) })]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            var target1 = AccessTools.Method(typeof(BookModel), "CreateBookForWorkshop");
            var target2 = AccessTools.Field(typeof(WorkshopSkinData), "dataName");
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Call, target1))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorkshopSkinExportPatch), nameof(ConvertValidWorkshopSkinBook)));
                }
                else if (code.Is(OpCodes.Ldfld, target2))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorkshopSkinExportPatch), nameof(ConvertValidWorkshopSkinName)));
                }
            }
        }

        private static BookModel ConvertValidWorkshopSkinBook(BookModel origin, WorkshopSkinData data)
        {
            if (data is LoAWorkshopImportSkinData d)
            {
                try
                {
                    var matchedBookId = Instance.matchedInfos.SafeGet(d.originSkinName);
                    if (matchedBookId != null)
                    {
                        origin._classInfo = matchedBookId;
                        origin._characterSkin = matchedBookId.CharacterSkin[0];
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }

            return origin;
            

        }

        private static string ConvertValidWorkshopSkinName(string origin, WorkshopSkinData data)
        {
            if (data is LoAWorkshopImportSkinData d)
            {
                try
                {
                    var matchedBookId = Instance.matchedInfos.SafeGet(d.originSkinName);
                    return matchedBookId?.Name ?? origin;
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
            return origin;
        }
    
        [HarmonyPatch(typeof(UICustomizeClothsPanel), "SetPreviewPortrait", new Type[] { typeof(WorkshopSkinData) })]
        [HarmonyPrefix]
        private static void Before_SetPreviewPortrait(WorkshopSkinData data)
        {
            if (data is LoAWorkshopImportSkinData d)
            {
                try
                {
                    var matchedBookId = Instance.matchedInfos.SafeGet(d.originSkinName);
                    if (matchedBookId != null)
                    {
                        SkinRenderPatch.RemoveCheck(d.originSkinName);
                        var sp = LoASpriteLoader.LoadDefaultMotion(d.originPackageId, d.originSkinName);
                        data.dic[ActionDetail.Default]._sprite = sp;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
    }
}
