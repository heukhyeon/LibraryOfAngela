using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace LibraryOfAngela.BattleUI
{
    class EnemyOver5
    {
        private const int MAX_COUNT = 15;

        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(EnemyOver5));
        }

        [HarmonyPatch(typeof(BattleUnitInfoManagerUI), "Initialize")]
        [HarmonyPrefix]
        public static bool Before_Initialize(BattleUnitInfoManagerUI __instance, IList<BattleUnitModel> unitList)
        {
            UpdateInfoUI(__instance, unitList, Faction.Enemy);
            UpdateInfoUI(__instance, unitList, Faction.Player);
            return true;
        }

        private static void UpdateInfoUI(BattleUnitInfoManagerUI __instance, IList<BattleUnitModel> unitList, Faction faction)
        {
            var enemyCount = unitList.Count(x => x.faction == faction);
            Direction allyFormationDirection = Singleton<StageController>.Instance.AllyFormationDirection;
            var arr = faction == Faction.Enemy ? __instance.enemyProfileArray : __instance.allyProfileArray;
            if (enemyCount > arr.Length && arr.Length < MAX_COUNT)
            {
                var list = arr.ToList();
                while (list.Count < enemyCount && list.Count < MAX_COUNT)
                {
                    list.Add(UnityEngine.Object.Instantiate(list[4].gameObject, list[4].transform.parent).GetComponent<BattleCharacterProfile.BattleCharacterProfileUI>());
                    var index = list.Count - 1;
                    list[index].transform.localPosition += new Vector3(0f, (float)(index - 4) * 64f, 0f);
                }
                if (faction == Faction.Enemy)
                {
                    __instance.enemyProfileArray = list.ToArray();
                }
                else
                {
                    __instance.allyProfileArray = list.ToArray();
                }
            }
        }

        [HarmonyPatch(typeof(BattleEmotionCoinUI), "Init")]
        [HarmonyPrefix]
        public static bool Before_Init(BattleEmotionCoinUI __instance)
        {
            UpdateEmotionCoinUI(__instance, Faction.Enemy);
            UpdateEmotionCoinUI(__instance, Faction.Player);
            return true;
        }

        private static void UpdateEmotionCoinUI(BattleEmotionCoinUI __instance, Faction faction)
        {
            var enemyCount = BattleObjectManager.instance.GetAliveList(false).Count(x => x.faction == faction);
            var arr = faction == Faction.Enemy ? __instance.enermy : __instance.librarian;
            var arrayLength = arr.Length;
            if (enemyCount > arrayLength && arrayLength < MAX_COUNT)
            {
                var list = arr.ToList();
                while (list.Count < enemyCount && list.Count < MAX_COUNT)
                {
                    list.Add(new BattleEmotionCoinUI.BattleEmotionCoinData
                    {
                        cosFactor = 1f,
                        sinFactor = 1f,
                        target = UnityEngine.Object.Instantiate<RectTransform>(arr[4].target, arr[4].target.parent)
                    });
                    var index = list.Count - 1;
                    list[index].target.localPosition += new Vector3(0f, (float)(index - 4) * 64f, 0f);
                }
                if (faction == Faction.Enemy)
                {
                    __instance.enermy = list.ToArray();
                }
                else
                {
                    __instance.librarian = list.ToArray();
                }
            }
        }
    }
}
