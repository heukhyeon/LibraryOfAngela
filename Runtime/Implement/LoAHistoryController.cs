using HarmonyLib;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Implement
{
    class LoAHistoryController : Singleton<LoAHistoryController>, ILoAHistoryController
    {
        private List<UnitDataModel> totalUnits = new List<UnitDataModel>();
        private List<UnitDataModel> totalDieUnits = new List<UnitDataModel>();
        private int currentWaveDieCount = 0;
        private Dictionary<UnitDataModel, List<LoAHistoryModel>> histories = new Dictionary<UnitDataModel, List<LoAHistoryModel>>();
        public void Initialize()
        {

        }

        int ILoAHistoryController.GetTotalAddedUnitCount()
        {
            return totalUnits.Count;
        }

        int ILoAHistoryController.GetTotalDieUnitCount()
        {
            return totalDieUnits.Count;
        }
        int ILoAHistoryController.GetCurrentWaveDieCount()
        {
            return currentWaveDieCount;
        }

        [HarmonyPatch(typeof(StageController), "StartBattle")]
        [HarmonyPrefix]
        private static void Before_StartBattle()
        {
            Instance.currentWaveDieCount = 0;
            BattlePhasePatch.ClearResource();
        }

        [HarmonyPatch(typeof(StageController), "InitCommon")]
        [HarmonyPrefix]
        private static void Before_InitCommon()
        {
            BattlePhasePatch.ClearResource();
        }

        // GameOver가 호출이 아예 안되는 경우도 가끔 있음
        [HarmonyPatch(typeof(StageController), "InitCommon")]
        [HarmonyPostfix]
        private static void After_InitCommon()
        {
            Instance.totalUnits.Clear();
            Instance.totalDieUnits.Clear();
            foreach (var pair in Instance.histories) pair.Value.Clear();
            Instance.histories.Clear();
        }

        [HarmonyPatch(typeof(StageController), "GameOver")]
        [HarmonyPostfix]
        public static void After_GameOver()
        {
            RealClearResource(true);
        }

        [HarmonyPatch(typeof(StageController), "ClearResources")]
        [HarmonyPostfix]
        public static void After_ClearResources()
        {
            RealClearResource(false);
        }

        private static void RealClearResource(bool isGameOver)
        {
            var target = isGameOver ? "GameOver" : "ClearResource";
            if (Instance.totalUnits.Count == 0)
            {
                Logger.Log($"Clear Target Empty from {target}"); ;
                return;
            }

            var logger = new StringBuilder($"Reception End from {target}\n");

            if (isGameOver)
            {
                Instance.totalUnits.Clear();
                Instance.totalDieUnits.Clear();
                foreach (var pair in Instance.histories) pair.Value.Clear();
                Instance.histories.Clear();
            }

            BattlePhasePatch.ClearResource();

            try
            {
                var manager = StageController.Instance.EnemyStageManager as IHandleClearResourcesEnemyTeamStageManager;
                if (manager != null)
                {
                    logger.AppendLine($"Reception Clearable :: {StageController.Instance.GetStageModel()?.ClassInfo?.id} // {manager.GetType().Name}");
                    manager.OnClearResources();
                }
                else
                {
                    logger.AppendLine($"Reception Not Clearable :: {StageController.Instance.GetStageModel()?.ClassInfo?.id}");
                }
            }
            catch (Exception e)
            {
                Logger.Log("Exception during ClearResources");
                Logger.LogError(e);
            }
            Logger.Log(logger.ToString());
        }

        [HarmonyPatch(typeof(BattleObjectManager), "RegisterUnit")]
        [HarmonyPostfix]
        public static void After_RegisterUnit(BattleUnitModel unit)
        {
            if (Instance.totalUnits.Contains(unit.UnitData.unitData)) return;
            Instance.totalUnits.Add(unit.UnitData.unitData);
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnDie")]
        [HarmonyPostfix]
        public static void After_OnDie(BattleUnitModel __instance)
        {
            if (!__instance.IsDeadReal()) return;
            if (Instance.totalDieUnits.Contains(__instance.UnitData.unitData)) return;
  
            Instance.totalDieUnits.Add(__instance.UnitData.unitData);
            Instance.currentWaveDieCount++;
        }

        T ILoAHistoryController.GetHistory<T>(UnitDataModel owner, bool force)
        {
            if (!histories.ContainsKey(owner)) histories[owner] = new List<LoAHistoryModel>();
            var result = histories[owner].FindType<T>();
            if (result == null && force)
            {
                result = new T();
                histories[owner].Add(result);
            }
            return result;
        }
    }
}
