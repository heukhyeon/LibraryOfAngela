using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Util
{
    class LoADebugger
    {
        [HarmonyPatch(typeof(LibraryModel), "LoadFromSaveData")]
        [HarmonyPostfix]
        private static void After_LoadFromSaveData(LibraryModel __instance)
        {
            if (!LoAFramework.DEBUG) return;
            if (__instance.PlayHistory.currentchapterLevel >= 7) return;

            __instance.PlayHistory.currentchapterLevel = 7;
            __instance._currentChapter = 7;
            __instance.PlayHistory.Clear_EndcontentsAllStage = 1;
            foreach (var info in StageClassInfoList.Instance.GetAllDataList())
            {
                if (info.id.IsBasic())
                {
                    __instance.ClearInfo._stageUnlocked.Add(info.id);
                    __instance.ClearInfo._stageInfoList[info.id] = new StageClearInfoListModel.StageInfo { stageId = info.id, clearCount = 1 };
                }
            }
            foreach (var floor in __instance._floorList)
            {
                floor._level = floor.Maxlevel;
                floor.UpdateOpenedCount();
            }
            foreach (var book in DropBookXmlList.Instance.GetList())
            {
                if (book.id.IsBasic())
                {
                    DropBookInventoryModel.Instance.AddBook(book.id, 999);
                }
            }
        }
       
    }
}
