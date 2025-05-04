using LibraryOfAngela.Extension;
using LibraryOfAngela.Extension.Framework;
using LibraryOfAngela.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace LibraryOfAngela.Buf
{
    class DummyBuf : BattleUnitBuf
    {
        private string _keywordId;
        private string _keywordIconId;

        public override string keywordId => _keywordId;

        public override string keywordIconId => _keywordIconId;


        public DummyBuf(string keywordId, string keywordIconId)
        {
            this._keywordId = keywordId;
            this._keywordIconId = keywordIconId;
            stack = 0;
        }
    }

    class SceneBufComponent : MonoBehaviour
    {
        public void Update()
        {
            transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
        }
    }


    class SceneBufPatch
    {
        internal static SceneBuf currentBuf { get; private set; }
        private static Dictionary<BattleUnitBuf, int> buffer = new Dictionary<BattleUnitBuf, int>();
        private static SceneBufImpl bufContainer;

        [HarmonyPatch(typeof(EnemyTeamStageManager), "OnRoundStart")]
        [HarmonyPostfix]
        private static void After_OnRoundStart(EnemyTeamStageManager __instance)
        {
            SceneBuf b = null;
            if (__instance is ISceneBufProvider p)
            {
                b = FrameworkExtension.GetSafeAction(() => p.CurrentScaneBuf);
            }
            if (currentBuf != b)
            {
                if (bufContainer == null)
                {
                    var parent = GameObject.Find("[Script]BattleScene/[Singleton][Script]UI_Manager/[Canvas]EmotionInfoUI/[Transform]EmotionBar_ROOT/[Rect]CenterRoot");
                    var bd = SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.allyarray[0].playerinfo.bufflistmanager.BuffIconSlot[0];
                    var obj = UnityEngine.Object.Instantiate(bd.gameObject, parent.transform);
                    var impl = obj.AddComponent<SceneBufImpl>();
                    obj.layer = parent.gameObject.layer;
                    obj.transform.localPosition = new Vector3(0f, -150f, 0f);
                    bufContainer = impl;
                }
                bufContainer.Current = b;
            }
            currentBuf = b;
            currentBuf?.OnRoundStart();
        }

        [HarmonyPatch(typeof(StageController), "RoundEndPhase_ExpandMap")]
        [HarmonyPrefix]
        private static bool Before_RoundEndPhase_ExpandMap()
        {
            currentBuf?.OnRoundEnd();
            return true;
        }

        [HarmonyPatch(typeof(BattleUnitModel), "GetDamageReduction")]
        [HarmonyPostfix]
        private static void After_GetDamageReduction(BattleUnitModel __instance, ref int __result)
        {
            __result -= currentBuf?.GetDamageReduction(__instance) ?? 0;
        }

        [HarmonyPatch(typeof(BattleUnitModel), "OnTakeDamageByAttack")]
        [HarmonyPostfix]
        private static void After_OnTakeDamageByAttack(BattleUnitModel __instance, BattleDiceBehavior atkDice, int dmg)
        {
            currentBuf?.OnTakeDamageByAttack(__instance, atkDice, dmg);
        }

        [HarmonyPatch(typeof(BattleUnitBuf), "Destroy")]
        [HarmonyPrefix]
        private static bool Before_Destroy(BattleUnitBuf __instance)
        {
            buffer[__instance] = __instance.stack;
            return true;
        }

        [HarmonyPatch(typeof(BattleUnitBuf), "Destroy")]
        [HarmonyPostfix]
        private static void After_Destroy(BattleUnitBuf __instance, ref bool ____destroyed)
        {
            if (currentBuf?.PreventBufDestroy(__instance) == true && buffer.ContainsKey(__instance))
            {
                __instance.stack = buffer[__instance];
                ____destroyed = false;
            }
            buffer.Remove(__instance);
        }
    
        [HarmonyPatch(typeof(StageController), nameof(StageController.RoundEndPhase_TheLast))]
        [HarmonyPostfix]
        private static void After_RoundEndPhase_TheLast()
        {
            try
            {
                currentBuf?.OnRoundEndTheLast();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
