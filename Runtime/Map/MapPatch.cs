using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Map
{
    internal class MapPatch : Singleton<MapPatch>
    {
        private List<CustomMapData> maps = new List<CustomMapData>();
        private IPreventChangeBgmEnemyTeamStageManager currentBgmManager;

        public void Initialize()
        {
            foreach (var x in LoAModCache.Instance.Select(x => x.MapConfig).Where(x => x != null)) 
            {
                x.GetMaps().ForEach(map =>
                {
                    map.packageId = x.packageId;
                    if (map.managerType != null && !typeof(LoAMapManager).IsAssignableFrom(map.managerType))
                    {
                        Logger.Log($"Type {map.managerType.FullName} is Not Inherit LoAMapManager, Please Check");
                        throw new Exception("MapManager Invalid");
                        map.managerType = null;
                    }
                    maps.Add(map);
                });
            }

            if (maps.Count > 0)
            {
                InternalExtension.SetRange(GetType());
            }
        }

        [HarmonyPatch(typeof(BattleSceneRoot), "InitFloorMap")]
        [HarmonyPrefix]
        private static void Before_InitFloorMap(SephirahType sephirah)
        {
            var stageId = StageController.Instance.GetStageModel().ClassInfo.id;
            var wave = StageController.Instance.CurrentWave;
            var targetMap = Instance.maps.Find(x => x.themeStageId == stageId && ((wave == 1 && x.themeStageWave == 0) || wave == x.themeStageWave) );
            if (targetMap != null)
            {
                var replaceMap = LoAMapManager.Create(sephirah, targetMap, true, (ILoACustomMapMod) LoAModCache.Instance[targetMap.packageId].mod);
                replaceMap.transform.SetParent(BattleSceneRoot.Instance.transform);
                var currentSephiraMap = BattleSceneRoot.Instance.mapList.Find(x => x.sephirahType == sephirah);
                if (currentSephiraMap != null)
                {
                    BattleSceneRoot.Instance.mapList.Remove(currentSephiraMap);
                    UnityEngine.Object.Destroy(currentSephiraMap.gameObject);
                }
                BattleSceneRoot.Instance.mapList.Add(replaceMap);
            }
        }

        [HarmonyPatch(typeof(BattleSceneRoot), "ChangeToSpecialMap")]
        [HarmonyPrefix]
        private static void Before_ChangeToSpecialMap(string mapName, BattleSceneRoot __instance)
        {
            if (string.IsNullOrEmpty(mapName) || __instance._addedMapList.Any(x => x.name.Contains(mapName)))
            {
                return;
            }
            var logger = new StringBuilder("Requested Map Change :" + mapName + "\n");
            var target = Instance.maps.Find(x => x.mapName == mapName);
            if (target != null)
            {
                var replaceMap = LoAMapManager.Create(StageController.Instance.CurrentFloor, target, true, (ILoACustomMapMod)LoAModCache.Instance[target.packageId].mod);
                replaceMap.transform.SetParent(BattleSceneRoot.Instance.transform);
                replaceMap.isSpecialPick = true;
                BattleSceneRoot.Instance._addedMapList.Add(replaceMap);
                logger.AppendLine("Create Success");
                Logger.Log(logger.ToString());
            }
            else
            {
                logger.AppendLine("Not Exists Map");
                Logger.Log(logger.ToString());
            }
        }

        /// <summary>
        /// Util.LoadPrefab("InvitationMaps/InvitationMap_" + text, SingletonBehavior<BattleSceneRoot>.Instance.transform)
        /// ->
        /// MapPatch.HandleInitMap(Util.LoadPrefab("InvitationMaps/InvitationMap_" + text, SingletonBehavior<BattleSceneRoot>.Instance.transform));
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(StageController), "InitializeMap")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_InitializeMap(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(global::Util), "LoadPrefab", parameters: new Type[] { typeof(string), typeof(Transform) });
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Calls(target))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapPatch), nameof(HandleInitMap)));
                }
            }
        }

        private static GameObject HandleInitMap(GameObject origin, string text)
        {
            if (!(origin is null)) return origin;

            try 
            {
                var target = Instance.maps.Find(x => x.mapName == text);
                if (target != null)
                {
                    var replaceMap = LoAMapManager.Create(StageController.Instance.CurrentFloor, target, true, (ILoACustomMapMod)LoAModCache.Instance[target.packageId].mod);
                    replaceMap.transform.SetParent(BattleSceneRoot.Instance.transform);
                    replaceMap.isSpecialPick = true;
                    return replaceMap.gameObject;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            return origin;
        }

        public void ReplaceMap(string packageId, string mapName, bool showEffect)
        {
            var targetMap = Instance.maps.Find(x => x.packageId == packageId && x.mapName == mapName);
            if (targetMap != null)
            {
                var sephirah = StageController.Instance.CurrentFloor;
                var replaceMap = LoAMapManager.Create(sephirah, targetMap, true, (ILoACustomMapMod)LoAModCache.Instance[targetMap.packageId].mod);
                replaceMap.transform.SetParent(BattleSceneRoot.Instance.transform);
                var currentSephiraMap = BattleSceneRoot.Instance.mapList.Find(x => x.sephirahType == sephirah);
                if (currentSephiraMap != null)
                {
                    BattleSceneRoot.Instance.mapList.Remove(currentSephiraMap);
                    UnityEngine.Object.Destroy(currentSephiraMap.gameObject);
                }
                if (BattleSceneRoot.Instance.currentMapObject?.isCreature == true)
                {
                    UnityEngine.Object.Destroy(BattleSceneRoot.Instance.currentMapObject.gameObject);
                }
                BattleSceneRoot.Instance.mapList.Add(replaceMap);
                replaceMap.ActiveMap(true);
                replaceMap.InitializeMap();
                BattleSceneRoot.Instance.currentMapObject = replaceMap;
                if (showEffect) BattleSceneRoot.Instance._mapChangeFilter.StartMapChangingEffect(Direction.LEFT, true);
            }
        }

        [HarmonyPatch(typeof(BattleSoundManager), "ChangeAllyTheme")]
        [HarmonyPatch(typeof(BattleSoundManager), "ChangeEnemyTheme")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ChangeTheme(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var target = AccessTools.Method(typeof(BattleSoundManager), "IsBlackSilenceBattle");
            var t = OpCodes.Ldc_I4_0;
            if (original.Name == "ChangeCurrentTheme") t = OpCodes.Ldc_I4_2;
            else if (original.Name == "ChangeEnemyTheme") t = OpCodes.Ldc_I4_1;

            foreach (var code in instructions)
            {
                yield return code;
                if (code.Is(OpCodes.Call, target))
                {
                    yield return new CodeInstruction(t);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapPatch), nameof(IsBgmChangePrevent)));
                }
            }
        }

        [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        [HarmonyPostfix]
        private static void After_StartBattle(StageController __instance)
        {
            Instance.currentBgmManager = __instance.EnemyStageManager as IPreventChangeBgmEnemyTeamStageManager;
        }

        private static bool IsBgmChangePrevent(bool origin, ChangeBgmType type)
        {
            if (origin || Instance.currentBgmManager is null) return origin;
            try
            {
                return Instance.currentBgmManager.IsPreventChangeBgm(type);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return origin;
        }

    }
}
