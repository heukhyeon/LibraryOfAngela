using GameSave;
using HarmonyLib;
using Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoALoader
{
    public class LatestClearRecord
    {
        private const string KEY_LAST_CLEAR_PACKAGE = "KEY_LAST_CLEAR_PACKAGE";
        private const string KEY_LAST_CLEAR_STAGE = "KEY_LAST_CLEAR_STAGE";
        private static bool isLibraryLoaded = false;
        private static LorId latestClear = null;

        [HarmonyPatch(typeof(LatestDataModel), nameof(LatestDataModel.LoadFromSaveData))]
        [HarmonyPostfix]
        private static void After_LatestDataModel_LoadFromSaveData(SaveData data)
        {
            try
            {
                var stage = data.GetData(KEY_LAST_CLEAR_STAGE);
                if (stage == null) return;
                latestClear = new LorId(data.GetData(KEY_LAST_CLEAR_PACKAGE).GetStringSelf(), stage.GetIntSelf());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [HarmonyPatch(typeof(LibraryModel), nameof(LibraryModel.LoadFromSaveData))]
        [HarmonyPostfix]
        private static void After_LibraryModel_LoadFromSaveData()
        {
            isLibraryLoaded = true;
        }

        [HarmonyPatch(typeof(LatestDataModel), nameof(LatestDataModel.GetSaveData))]
        [HarmonyPostfix]
        private static void After_GetSaveData(ref SaveData __result)
        {
            if (latestClear == null) return;
            __result.AddData(KEY_LAST_CLEAR_PACKAGE, new SaveData(latestClear.packageId));
            __result.AddData(KEY_LAST_CLEAR_STAGE, new SaveData(latestClear.id));
            Debug.Log($"LoA :: Latest Clear Id Save : {latestClear}");
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveLatestData))]
        [HarmonyPrefix]
        private static bool Before_SaveLatestData(LatestDataModel data)
        {
            if (isLibraryLoaded)
            {
                var model = StageController.Instance.GetStageModel();
                if (model != null && model.GetFrontAvailableWave() == null)
                {
                    latestClear = model.ClassInfo?.id;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(EntryScene), nameof(EntryScene.SetCG))]
        [HarmonyPostfix]
        private static void After_SetCG(EntryScene __instance)
        {
            if (latestClear == null || latestClear?.IsBasic() == true) return;
            var modPath = ModContentManager.Instance.GetModPath(latestClear.packageId);
            var filePath = Path.Combine(modPath, "Assemblies", "ClearCG", $"{latestClear.id}.png");
            if (File.Exists(filePath))
            {
                Texture2D texture2D = new Texture2D(2, 2);
                texture2D.LoadImage(File.ReadAllBytes(filePath));
                __instance.CGImage.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
}
