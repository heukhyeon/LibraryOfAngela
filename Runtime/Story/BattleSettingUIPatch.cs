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

namespace LibraryOfAngela.Story
{
    class BattleSettingUIPatch
    {
        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(BattleSettingUIPatch));
        }

        [HarmonyPatch(typeof(UICharacterList), "InitUnitListFromBattleData")]
        [HarmonyPostfix]
        private static void After_InitUnitListFromBattleData(List<UnitBattleDataModel> dataList, UICharacterList __instance)
        {
            try
            {
                var stage = StageController.Instance.GetStageModel()?.ClassInfo?.id;
                var config = StoryPatch.Instance.storyInfos.SafeGet(stage);
                if (config?.overrideUnit is null) return;
                var wave = StageController.Instance.CurrentWave;
                int i = 0;
                bool flag = false;
                int removedCount = 0;
                UIBattleSettingPanel uibattleSettingPanel = UI.UIController.Instance.GetUIPanel(UIPanelType.BattleSetting) as UIBattleSettingPanel;
                var floor = StageController.Instance.GetCurrentStageFloorModel();
                while (i < dataList.Count)
                {
                    var unit = dataList[i];
                    var state = config.overrideUnit(wave, unit);
                    if (removedCount > 0)
                    {
                        __instance.slotList[i].SetBattleCharacter(unit);
                    }
                    if (state is LoAUnitReadyState.Required d1)
                    {
                        flag = true;
                        __instance.slotList[i].SetYesToggleState();
                        __instance.slotList[i].SetToggle(false);
                    }
                    if (state is LoAUnitReadyState.Locked d2)
                    {
                        __instance.slotList[i].SetNoToggleState();
                        __instance.slotList[i].SetLockSlot(true);
                        uibattleSettingPanel.currentAvailbleUnitslots.Remove(__instance.slotList[i]);
                    }
                    else if (state is LoAUnitReadyState.Replaced d3)
                    {
                        var index = floor.addedunitList.IndexOf(unit);
                        if (index >= 0) floor.addedunitList[index] = d3.replaceUnit;
                        floor._unitList[i] = d3.replaceUnit;
                    }
                    else if (state is LoAUnitReadyState.Removed d4)
                    {
                        flag = true;
                        removedCount++;
                        i--;
                        __instance.slotList[__instance.slotList.Count - removedCount].SetDisabledSlot();
                        floor.addedunitList.Remove(unit);
                        floor._unitList.Remove(unit);
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
