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

namespace LibraryOfAngela.Battle
{
    class AutoBattlePatch
    {
        public void Initialize()
        {
            InternalExtension.SetRange(GetType());
        }

        [HarmonyPatch(typeof(StageController), "SetAutoCardForNonControlablePlayer")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetAutoCardForNonControlablePlayer(IEnumerable<CodeInstruction> instructions)
        {
            var flag = false;
            foreach (var c in instructions)
            {
                yield return c;
                if (!flag && c.opcode == OpCodes.Callvirt && (c.operand as MethodInfo)?.Name == "Exists")
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoBattlePatch), nameof(FilterAutoControllerUnit)));
                }
            }
        }

        private static bool FilterAutoControllerUnit(bool origin, BattleUnitModel unit)
        {
            if (!origin) return origin;
            if (unit.turnState == BattleUnitTurnState.BREAK) return origin;

            foreach (var e in BattleInterfaceCache.Of<IAutoController>(unit))
            {
                if (e.IsManuallyCardSet)
                {
                    e.OnManualCardSet();
                    return false;
                }
            }
            return origin;
        }
    }
}
