using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LibraryOfAngela.Extension.Framework.FrameworkExtension;
using LOR_DiceSystem; // CardRange, BehaviourType 등을 위해 추가

namespace LibraryOfAngela.Battle
{
    class AutoBattlePatch
    {
        [HarmonyPatch(typeof(BattleUnitModel), "CheckCardAvailable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CheckCardAvailable(IEnumerable<CodeInstruction> instructions)
        {
            var method = AccessTools.Method(typeof(BattleUnitModel), nameof(BattleUnitModel.IsControlable));
            foreach (var c in instructions)
            {
                yield return c;
                if (c.Calls(method))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoBattlePatch), nameof(CheckIsForceInjecting)));
                }
            }
        }

        private static bool CheckIsForceInjecting(bool origin)
        {
            return origin || IsForceInjecting;
        }

        // ApplyEnemyCardAuto  ̹ Ʈѷ ִٸθ ʰ Ѵ.
        [HarmonyPatch(typeof(StageController), "ApplyEnemyCardPhase")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_ApplyEnemyCardPhase(IEnumerable<CodeInstruction> instructions)
        {
            var method = AccessTools.Method(typeof(StageController), nameof(StageController.GetActionableEnemyList));
            foreach (var c in instructions)
            {
                yield return c;
                if (c.Calls(method))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoBattlePatch), nameof(FilterUnitsForCustomAI)));
                }
            }
        }

        private static bool IsForceInjecting = false;

        // ApplyEnemyCardAuto �� �̹� ��Ʈ�ѷ��� �ִٸ� �θ��� �ʰ� �Ѵ�.
        [HarmonyPatch(typeof(StageController), "SetAutoCardForNonControlablePlayer")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetAutoCardForNonControlablePlayer(IEnumerable<CodeInstruction> instructions)
        {
            // This transpiler ensures that the original method does nothing for units handled by ICustomCardSetter.
            // It effectively makes the original method skip our custom AI units.
            // The actual logic for our custom AI units will be in the Postfix.
            // A simple way: find the start of the loop over units, and if the unit has ICustomCardSetter, skip the loop body.
            // However, a more robust way provided previously was to filter the list of units the original method processes.
            var stloc0Instruction = instructions.FirstOrDefault(instr => instr.opcode == OpCodes.Stloc_0); // Assuming stloc.0 is the list of units
            if (stloc0Instruction != null)
            {
                var newInstructions = new List<CodeInstruction>();
                foreach (var instruction in instructions)
                {
                    newInstructions.Add(instruction);
                    if (instruction.opcode == OpCodes.Stloc_0) // After the list is stored
                    {
                        // Insert call to filter the list
                        newInstructions.Add(new CodeInstruction(OpCodes.Ldloc_0)); // Load the list
                        newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoBattlePatch), nameof(FilterUnitsForCustomAI))));
                        newInstructions.Add(new CodeInstruction(OpCodes.Stloc_0)); // Store the filtered list
                    }
                }
                return newInstructions;
            }
            return instructions; // If pattern not found, return original
        }


        public static List<BattleUnitModel> FilterUnitsForCustomAI(List<BattleUnitModel> originalList)
        {
            if (originalList == null) return new List<BattleUnitModel>();
            return originalList.Where(unit => BattleInterfaceCache.Of<ICustomCardSetter>(unit).FirstOrDefault() == null).ToList();
        }

        [HarmonyPatch(typeof(StageController), "SetAutoCardForNonControlablePlayer")]
        [HarmonyPostfix]
        private static void After_SetAutoCardForNonControlablePlayer()
        {
            IsForceInjecting = true;
            new AutoBattleImpl().Execute();
            IsForceInjecting = false;
        }


    }
}


