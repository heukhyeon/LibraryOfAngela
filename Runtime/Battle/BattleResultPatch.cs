using HarmonyLib;
using LibraryOfAngela.CorePage;
using LibraryOfAngela.EquipBook;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Global;
using LibraryOfAngela.Implement;
using LibraryOfAngela.Interface_External;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Battle
{
    class BattleResultPatch
    {
        struct LimbusLog
        {
            public bool IsValid => !(result is null);
            public BattleCardBehaviourResult result;
            public BattleCardBehaviourResult targetResult;
            public LimbusDiceAbility ability;
        }

        private static List<LimbusLog> limbusDiceResults = new List<LimbusLog>();
        private static bool isExistLimbusResult = false;

        public static void Initialize()
        {
            // StageController의 중첩 타입 패치가 필요하다면 추가
            
            Logger.Log("BattleResultPatch Start");
            Logger.Log("LoA ::: BattleResultPatch Complete");
        }

        public static void RegisterLibmusDice(LimbusDiceAbility ability)
        {
            var res = ability.owner.battleCardResultLog.CurbehaviourResult;
            if (limbusDiceResults.Any(d => d.result == res)) return;
            isExistLimbusResult = true;
            limbusDiceResults.Add(new LimbusLog { result = res, ability = ability, targetResult = ability.isWin ? ability.behavior.card.target.battleCardResultLog?.CurbehaviourResult : null });
        }

        public static void SaveLibmusDice(BattleDiceBehavior behaviour)
        {
            if (!isExistLimbusResult) return;
            var result = behaviour.owner.battleCardResultLog.CurbehaviourResult;
            var match = limbusDiceResults.Find(d => d.result == result);
            if (!match.IsValid || !match.ability.isWin || !match.ability.isForceReuse) return;
            match.ability.isWin = false;
            // 원거리는 카피하므로 어빌리티의 현 주인이 다른거라면 복제된 주사위가 들어간것
            if (behaviour.card.cardBehaviorQueue.Contains(behaviour) || match.ability.behavior != behaviour)
            {
                return;
            }

            if (LoAFramework.DEBUG)
            {
                Logger.Log($"다이스 저장 : {behaviour.card.owner.UnitData.unitData.name} // {behaviour.card.card.GetName()} // {behaviour.Index} // {behaviour.isBonusAttack} // {behaviour.card.cardBehaviorQueue.Contains(behaviour)}");
            }

            behaviour.owner.currentDiceAction.AddDiceFront(behaviour);
        }


        /// <summary>
        /// 캐릭터의 IsMoveable이 false 일때 강제로 정지시키는 기능
        /// 
        /// if (flag4)
        /// 
        /// ->
        /// 
        /// if (BattlePatch.IsEnemyMoveable(flag4, behaviourActionBase, ref flag2)),
        /// 
        /// if (flag7)
        /// 
        /// if (BattlePatch.IsAllyMoveable(flag7, behaviourActionBase2, ref flag5))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(RencounterManager), "CheckMovingStateBeforeRoll")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CheckMovingStateBeforeRoll(IEnumerable<CodeInstruction> instructions)
        {
            var enemyFired = false;
            var allyFired = false;
            var flagCount = new int[2] { 0, 0 };
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                var index = code.GetIndex();
                /**
                 * flag3 = (this._currentEnemyBehaviourResult.behaviourRawData.Type == BehaviourType.Atk)
                 * 
                 * flag6 = (this._currentLibrarianBehaviourResult.behaviourRawData.Type == BehaviourType.Atk)
                 * 
                 * ->
                 * 
                 * flag3 = BattlePatch.WrapValidRangeDice((this._currentEnemyBehaviourResult.behaviourRawData.Type == BehaviourType.Atk), this._currentEnemyBehaviourResult)
                 * 
                 */
                if (code.opcode == OpCodes.Stloc_S)
                {
                    var targetIndex = (code.operand as LocalBuilder)?.LocalIndex ?? -1;
                    if (targetIndex == 6)
                    {
                        if (flagCount[0] == 0) flagCount[0]++;
                        else if (flagCount[0] == 1)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RencounterManager), "_currentEnemyBehaviourResult"));
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), "WrapValidRangeDice"));
                        }
                    }
                    else if (targetIndex == 10)
                    {
                        if (flagCount[1] == 0) flagCount[1]++;
                        else if (flagCount[1] == 1)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RencounterManager), "_currentLibrarianBehaviourResult"));
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), "WrapValidRangeDice"));
                        }
                    }
                }
                yield return code;
                if (!enemyFired && index == 7)
                {
                    enemyFired = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 0x05);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0x03);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), "IsEnemyMoveable"));
                }
                else if (!allyFired && index == 11)
                {
                    allyFired = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 0x09);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 0x02);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), "IsAllyMoveable"));
                }
            }
        }

        /// <summary>
        /// if (flag) { ... StartParryign() }
        /// ->
        /// if (BattlePatch.OnCheckParryingable(flag)) { StartParrying() }
        /// 
        /// if (list2.Exists) 
        /// ->
        /// if (AdvancedSkinInfoPatch.IsUnitStartMoveHold(list2.Exists, list[j]))
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(StageController), "WaitUnitArrivePhase")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_WaitUnitArrivePhase(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var existstarget = typeof(List<BattleUnitModel>).GetMethod("Exists");
            var distanceFired = false;
            var moveMethod = AccessTools.Method(typeof(HexagonalTileMover), "Move", new Type[] { typeof(BattleUnitModel), typeof(float), typeof(bool) });
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (code.GetIndex() == 20)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 19);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), "OnCheckParryingable"));
                }
                // BattleUnitModel unit = list[j]; 에서 list[j] 캐치
                if (!distanceFired && code.Is(OpCodes.Callvirt, existstarget))
                {
                    distanceFired = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<BattleUnitModel>), "get_Item"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AdvancedSkinInfoPatch), "IsUnitStartMoveHold"));
                }
                /**
                 * 				target2.moveDetail.Move(arrivedUnit, 15f, true);
                 *               this.StartParrying(arrivedUnit.currentDiceAction, target2.cardSlotDetail.keepCard);
                 *               
                 *               target2.moveDetail.Move(arrivedUnit, 15f, true);
                 *               BattlePatch.CheckStopStandbyFarDice(target2);
                 *               this.StartParrying(arrivedUnit.currentDiceAction, target2.cardSlotDetail.keepCard);
                 * 
                 */
                if (code.Is(OpCodes.Callvirt, moveMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 17);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), "CheckStopStandbyFarDice"));
                }
            }
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "GiveDamage")]
        [HarmonyPrefix]
        private static bool Before_GiveDamage(BattleDiceBehavior __instance)
        {
            if (HandleSkipDamage(__instance)) return false;
            return true;
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "GiveDeflectDamage")]
        [HarmonyPrefix]
        private static bool Before_GiveDeflectDamage(BattleDiceBehavior __instance)
        {
            if (HandleSkipDamage(__instance)) return false;
            return true;
        }


        /// <summary>
        /// timer.ChangeWaitTime(0.3f);
        /// ->
        /// timer.ChangeWaitTime(BattleResultPatch.HandleLimbusParryingDiceDelay(0.3f));
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(RencounterManager), "PrintEnemyVanillaDice")]
        [HarmonyPatch(typeof(RencounterManager), "PrintLibrarianVanillaDice")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_PrintVanillaDice(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var target = AccessTools.Method(typeof(RencounterStateTimer), nameof(RencounterStateTimer.ChangeWaitTime));
            var target2 = original.Name == nameof(RencounterManager.PrintEnemyVanillaDice) ? nameof(RencounterManager._currentEnemyBehaviourResult)
                : nameof(RencounterManager._currentLibrarianBehaviourResult);

            var flag = false;
            foreach (var code in instructions)
            {
                if (!flag && code.Is(OpCodes.Callvirt, target))
                {
                    flag = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(RencounterManager), target2));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), nameof(HandleLimbusParryingDiceDelay)));
                }
                yield return code;
            }
        }

        private static float HandleLimbusParryingDiceDelay(float origin, BattleCardBehaviourResult result)
        {
            if (!isExistLimbusResult) return origin;
            var index = limbusDiceResults.FindIndex(d => d.result == result);
            if (index == -1) return origin;
            var target = limbusDiceResults[index];
            RencounterManager.Instance.PlaySound(RencounterManager.Instance.Dice_Rolled);
            var targetResultIndex = limbusDiceResults.FindIndex(d => d.targetResult == result);
            if (targetResultIndex >= 0 || target.targetResult != null)
            {
                var targetResult = targetResultIndex >= 0 ? limbusDiceResults[targetResultIndex].result : target.targetResult;
                if (targetResult.vanillaDiceValueList.Count > 0)
                {
                    var num = targetResult.vanillaDiceValueList[0];
                    targetResult.vanillaDiceValueList.RemoveAt(0);
                    targetResult.behaviour.owner.view.diceActionUI.SetDiceNew(
                    targetResult.resultDiceMax, num, num == targetResult.resultDiceValue, BattleDiceValueColor.Normal);
                }
            }
            return Time.deltaTime * 5.5f;
        }

        [HarmonyPatch(typeof(RencounterManager), "EndRencounter")]
        [HarmonyPostfix]
        private static void After_EndRencounter()
        {
            if (isExistLimbusResult)
            {
                isExistLimbusResult = false;
                limbusDiceResults.Clear();
            }
        }

        // 합 강제 불가 처리
        private static bool OnCheckParryingable(bool origin, BattlePlayingCardDataInUnitModel targetCard)
        {
            if (!origin) return false;
            var target = targetCard.owner;
            var arriveUnit = targetCard.target;
            var arriveUnitCard = arriveUnit.currentDiceAction;
            if (BattleInterfaceCache.Of<IForceOneSideBuf>(target).Any(x =>
            {
                return x?.IsForceOneSideAction(targetCard, arriveUnitCard) == true;
            })) return false;

            if (BattleInterfaceCache.Of<IForceOneSideBuf>(arriveUnit).Any(x =>
            {
                return x?.IsForceOneSideAction(arriveUnitCard, targetCard) == true;
            })) return false;

            return true;
        }


        private static bool IsEnemyMoveable(bool origin, BehaviourActionBase action, RencounterManager instance, ref bool stop)
        {
            if (action is IForceStopMotion)
            {
                stop = origin;
            }
            else
            {
                action = AdvancedSkinInfoPatch.ConvertDefaultActionScript2(action, instance._currentLibrarianBehaviourResult);
                if (action is IForceStopMotion)
                {
                    var ret = action.IsOpponentMovable(instance._currentLibrarianBehaviourResult, instance._currentEnemyBehaviourResult);
                    if (!ret)
                    {
                        origin = ret;
                        stop = true;
                    }
                }
                else
                {
                    action = AdvancedSkinInfoPatch.ConvertDefaultActionScript2(action, instance._currentEnemyBehaviourResult);
                    if (action is IForceStopMotion)
                    {
                        origin = action.IsMovable(instance._currentEnemyBehaviourResult, instance._currentLibrarianBehaviourResult);
                        stop = !origin;
                    }
                }
            }

            return origin;
        }

        private static bool IsAllyMoveable(bool origin, BehaviourActionBase action, RencounterManager instance, ref bool stop)
        {
            if (action is IForceStopMotion)
            {
                stop = origin;
            }
            else
            {
                action = AdvancedSkinInfoPatch.ConvertDefaultActionScript2(action, instance._currentLibrarianBehaviourResult);
                if (action is IForceStopMotion)
                {
                    origin = action.IsMovable(instance._currentLibrarianBehaviourResult, instance._currentEnemyBehaviourResult);
                    stop = !origin;
                }
                else
                {
                    action = AdvancedSkinInfoPatch.ConvertDefaultActionScript2(action, instance._currentLibrarianBehaviourResult);
                    if (action is IForceStopMotion)
                    {
                        var ret = action.IsOpponentMovable(instance._currentLibrarianBehaviourResult, instance._currentEnemyBehaviourResult);
                        if (!ret)
                        {
                            origin = ret;
                            stop = true;
                        }
                    }
                }
            }
            return origin;
        }

        private static bool HandleSkipDamage(BattleDiceBehavior behavior)
        {
            try
            {
                if (!isExistLimbusResult) return false;
                var result = limbusDiceResults.Find(d => d.ability.behavior == behavior && d.ability.isWin);
                if (result.IsValid)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            return false;
        }

        private static bool WrapValidRangeDice(bool origin, BattleCardBehaviourResult result)
        {
            if (origin) return true;
            if (result.behaviourRawData.Type == BehaviourType.Def) return false;
            return result.behaviour.IsAttackDice(result.behaviour.Detail);
        }

        private static void CheckStopStandbyFarDice(BattleUnitModel target)
        {
            var flag = false;
            if (target.cardSlotDetail?.keepCard?.card?.GetSpec().Ranged == CardRange.Far)
            {
                var behaviour = target.cardSlotDetail.keepCard.cardBehaviorQueue.FirstOrDefault();
                flag = behaviour != null && behaviour.IsAttackDice(behaviour.Detail) == true;
            }
            if (!flag && target.IsBreakLifeZero())
            {
                flag = true;
            }
            if (flag)
            {
                target.moveDetail.Stop();
                if (target.view.charAppearance.GetCurrentMotionDetail() == ActionDetail.Move)
                {
                    target.view.charAppearance.ChangeMotion(target.IsBreakLifeZero() ? ActionDetail.Damaged : ActionDetail.Default);
                }
            }
        }

        /// <summary>
        /// actionAfterBehaviour.infoList = this.GetBehaviourAction(ref actionAfterBehaviour, ref actionAfterBehaviour2, false);
        /// ->
        /// BattleResultPatch.HandleBeforeMoveRoutine(ref actionAfterBehaviour, ref actionAfterBehaviour2)
        /// actionAfterBehaviour.infoList = this.GetBehaviourAction(ref actionAfterBehaviour, ref actionAfterBehaviour2, false);
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(RencounterManager), nameof(RencounterManager.SetMovingStateByActionResult))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SetMovingStateByActionResult(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var target = AccessTools.Field(typeof(RencounterManager.ActionAfterBehaviour), "preventOverlap");
            var myHandler = generator.DeclareLocal(typeof(LoAMovingStateHandler));
            var enemyHandler = generator.DeclareLocal(typeof(LoAMovingStateHandler));
            var myField = AccessTools.Field(typeof(RencounterManager), nameof(RencounterManager._currentLibrarianBehaviourResult));
            var enemyField = AccessTools.Field(typeof(RencounterManager), nameof(RencounterManager._currentEnemyBehaviourResult));
            foreach (var c in EmitPreCreateAction(myField, enemyField, myHandler)) yield return c;
            foreach (var c in EmitPreCreateAction(enemyField, myField, enemyHandler)) yield return c;

            var final = AccessTools.Method(typeof(RencounterManager), nameof(RencounterManager.MoveRoutine));

            var codes = new List<CodeInstruction>(instructions);
            var index = codes.FindLastIndex(d => d.opcode == OpCodes.Stfld);

            for (int i = index + 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0)
                {
                    codes.InsertRange(i + 1, new CodeInstruction[] {
                    new CodeInstruction(OpCodes.Ldloc, 2),
                     new CodeInstruction(OpCodes.Ldloca_S, 3),
                    new CodeInstruction(OpCodes.Ldloca_S, 4),
                    new CodeInstruction(OpCodes.Ldloc, myHandler),
                    new CodeInstruction(OpCodes.Ldloc, enemyHandler),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), nameof(HandlePostCreateAction)))
                    });
                    break;
                }
            }

            foreach (var code in codes)
            {
                yield return code;
                
            }
        }

        private static IEnumerable<CodeInstruction> EmitPreCreateAction(FieldInfo my, FieldInfo enemy, LocalBuilder field)
        {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, my);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, enemy);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BattleResultPatch), nameof(HandlePreCreateAction)));
            yield return new CodeInstruction(OpCodes.Stloc, field);
        }


        private static LoAMovingStateHandler HandlePreCreateAction(BattleCardBehaviourResult my, BattleCardBehaviourResult enemy)
        {
            try
            {
                var isEnemy = my == RencounterManager.Instance._currentEnemyBehaviourResult;

                var unit = my == RencounterManager.Instance._currentEnemyBehaviourResult ? RencounterManager.Instance._enemy :
                    RencounterManager.Instance._librarian;

                var target = unit == RencounterManager.Instance._enemy ? RencounterManager.Instance._librarian :
                    RencounterManager.Instance._enemy;

                var coreInfo = AdvancedEquipBookPatch.Instance.infos.SafeGet(unit.model.Book.BookId)?.movingStateHandler?.Invoke();

                coreInfo?.OnPreMoveRoutine(unit, my, target, enemy);

                return coreInfo;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return null;
        }


        private static RencounterManager HandlePostCreateAction(
            RencounterManager target,
            Result result,
            ref RencounterManager.ActionAfterBehaviour action1, 
            ref RencounterManager.ActionAfterBehaviour action2, 
            LoAMovingStateHandler my, 
            LoAMovingStateHandler enemy)
        {
            try
            {
                RencounterManager.ActionAfterBehaviour myAction;
                RencounterManager.ActionAfterBehaviour enemyAction;
                if (result == Result.Lose)
                {
                    myAction = action1;
                    enemyAction = action2;
                    my?.OnStartMoveRoutine(ref action1, ref action2);
                    enemy?.OnStartMoveRoutine(ref action2, ref action1);
                }
                else
                {
                    myAction = action2;
                    enemyAction = action1;
                    my?.OnStartMoveRoutine(ref action2, ref action1);
                    enemy?.OnStartMoveRoutine(ref action1, ref action2);
                }
                action1.endMoveRoutineEvent = (RencounterManager.ActionAfterBehaviour.EffectEvent)
                    Delegate.Combine(action1.endMoveRoutineEvent, new RencounterManager.ActionAfterBehaviour.EffectEvent(() =>
                    {
                        my?.OnEndRoutine(myAction, enemyAction);
                        enemy?.OnEndRoutine(enemyAction, myAction);
                    }));
                return target;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            return target;
        }
    }
}
