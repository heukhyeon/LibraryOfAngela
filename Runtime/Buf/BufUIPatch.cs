using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace LibraryOfAngela.Buf
{
    internal class BufUIPatch
    {
        // if (i - num > @this.BuffIconSlot.Count)
        // ->
        // if (i - num >= @this.BuffIconSlot.Count)
        [HarmonyPatch(typeof(BattleUnitInformationUI_BuffList), nameof(BattleUnitInformationUI_BuffList.SetData))]
        private static IEnumerable<CodeInstruction> Trans_SetData(IEnumerable<CodeInstruction> instructions)
        {
            bool fire = false;
            foreach (var d in instructions)
            {
                if (d.opcode == OpCodes.Ble_S && !fire)
                {
                    fire = true;
                    yield return d.Clone(OpCodes.Blt_S);
                }
                else
                {
                    yield return d;
                }
            }
        }
    }
}
