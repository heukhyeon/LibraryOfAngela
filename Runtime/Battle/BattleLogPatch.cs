using HarmonyLib;
using LibraryOfAngela.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static StageController;

namespace LibraryOfAngela.Battle
{
    class BattleLogPatch : Singleton<BattleLogPatch>
    {
        private Dictionary<BattleCardBehaviourResult, List<EffectTypoData>> addtionalResults = new Dictionary<BattleCardBehaviourResult, List<EffectTypoData>>();

        public void Initialize()
        {
            InternalExtension.SetRange(GetType());
            StageController.Instance.onChangePhase = 
                (OnChangePhaseDelegate)Delegate.Combine(StageController.Instance.onChangePhase, new OnChangePhaseDelegate(OnChangeStagePhase));
        }

        public static void InjectResultLog(BattleUnitModel unit, string title, string desc, EffectTypoCategory category)
        {
            var result = unit.battleCardResultLog.CurbehaviourResult;
            if (!Instance.addtionalResults.ContainsKey(result))
            {
                Instance.addtionalResults[result] = new List<EffectTypoData>();
            }
            Instance.addtionalResults[result].Add(new EffectTypoData
            {
                category = category,
                Title = title,
                Desc = desc
            });
        }

        private static void FillAdditionalLog(BattleCardBehaviourResult result, Dictionary<EffectTypoCategory, List<EffectTypoData>> dictionary)
        {
            if (Instance.addtionalResults.ContainsKey(result))
            {
                foreach(var typo in Instance.addtionalResults[result])
                {
                    dictionary[typo.category].Add(typo);
                }
            }
        }

        /// <summary>
        /// 	List<EffectTypoData> list = new List<EffectTypoData>();
        /// 	
        /// ->
        ///     BattleResultPatch.FillAddtionalLog(this, dictionary);
        /// 	List<EffectTypoData> list = new List<EffectTypoData>();
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BattleCardBehaviourResult), "GetAbilityDataAfterRoll")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_GetAbilityDataAfterRoll(IEnumerable<CodeInstruction> instructions)
        {
            var fired = false;
            foreach(var code in instructions)
            {
                yield return code;
                if (!fired && code.opcode == OpCodes.Stloc_1)
                {
                    fired = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleLogPatch), "FillAdditionalLog"));
                }
            }
        }

        private void OnChangeStagePhase(StagePhase prevPhase, StagePhase nextPhase)
        {
            if (nextPhase == StagePhase.RoundStartPhase_System)
            {
                Instance.addtionalResults.Clear();
            }
        }
    }
}
