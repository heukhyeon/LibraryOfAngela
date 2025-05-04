// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Model;
using UnityEngine;

namespace LibraryOfAngela.SD
{
    class MovePatch
    {
        public static void Initialize()
        {
            InternalExtension.SetRange(typeof(MovePatch));
        }

        /// <summary>
        /// SingletonBehavior<HexagonalMapManager>.Instance.IsWall(viewPos)
        /// ->
        /// MovePatch.CheckIsHoldUnit(SingletonBehavior<HexagonalMapManager>.Instance.IsWall(viewPos))
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch(typeof(RencounterManager), "MoveForward")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(HexagonalMapManager), nameof(HexagonalMapManager.IsWall), parameters: new Type[] { typeof(Vector3) });
            foreach (var c in instructions)
            {
                yield return c;
                if (c.Calls(target))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MovePatch), nameof(CheckIsHoldUnit)));
                }
            }
        }

        // 다른 이동 관련 메소드에 대한 Transpiler (같은 코드를 사용)
        [HarmonyPatch(typeof(RencounterManager), "MoveBack")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck_MoveBack(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_HoldUnitCheck(instructions);
        }

        [HarmonyPatch(typeof(RencounterManager), "MoveToOponnent")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck_MoveToOponnent(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_HoldUnitCheck(instructions);
        }

        [HarmonyPatch(typeof(RencounterManager), "MoveToOponnent_Near")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck_MoveToOponnent_Near(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_HoldUnitCheck(instructions);
        }

        [HarmonyPatch(typeof(RencounterManager), "MoveToOponnent_Near2")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck_MoveToOponnent_Near2(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_HoldUnitCheck(instructions);
        }

        [HarmonyPatch(typeof(RencounterManager), "TeleportFront")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck_TeleportFront(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_HoldUnitCheck(instructions);
        }

        [HarmonyPatch(typeof(RencounterManager), "TeleportBack")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_HoldUnitCheck_TeleportBack(IEnumerable<CodeInstruction> instructions)
        {
            return Trans_HoldUnitCheck(instructions);
        }

        [HarmonyPatch(typeof(HexagonalTileMover), "Move", new Type[] { typeof(BattleUnitModel), typeof(float), typeof(bool) })]
        [HarmonyPostfix]
        private static void After_Move(HexagonalTileMover __instance)
        {
            try
            {
                var t = LoASdTargetDictionary.Instance[__instance._self.view.charAppearance];
                if (t?.component is null) return;
                var type = new LoAMoveType.MoveToUnit();
                if (!t.component.IsMoveable(type))
                {
                    __instance.Stop();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(HexagonalTileMover), "Knockback")]
        [HarmonyPostfix]
        private static void After_Knockback(HexagonalTileMover __instance)
        {
            After_Move(__instance);
        }

        private static HexagonalMapManager.WallDirection CheckIsHoldUnit(HexagonalMapManager.WallDirection origin, BattleUnitView self)
        {
            if (origin != HexagonalMapManager.WallDirection.NONE) return origin;
            try
            {
                var target = LoASdTargetDictionary.Instance[self.charAppearance];
                if (target?.component is null) return origin;
                var type = new LoAMoveType.Rencounter();
                if (target.component.IsMoveable(type))
                {
                    return origin;
                }
                return self.WorldPosition.x < 0 ? HexagonalMapManager.WallDirection.LEFT : HexagonalMapManager.WallDirection.RIGHT;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return origin;
            }
        }
    }
}
