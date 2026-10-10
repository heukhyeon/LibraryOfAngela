using HarmonyLib;
using LibraryOfAngela.Interface_External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LibraryOfAngela.Battle
{
    internal static class AutoBattlePatch
    {
        [ThreadStatic] private static AutoBattleImpl enemyPlan;
        [ThreadStatic] private static AutoBattleImpl playerPlan;
        [ThreadStatic] private static SelectionScope current;

        private sealed class PhaseScope
        {
            public AutoBattleImpl enemy;
            public AutoBattleImpl player;
        }
        private sealed class SelectionScope
        {
            public SelectionScope previous;
            public AutoBattleImpl session;
            public AutoBattleImpl.Plan plan;
            public BattleUnitModel owner;
            public int slot;
            public BattleDiceCardModel resolvingCard;
        }

        [HarmonyPatch(typeof(StageController), "ApplyEnemyCardPhase")]
        [HarmonyPrefix]
        private static void Before_ApplyEnemyCardPhase(out PhaseScope __state)
        {
            __state = new PhaseScope { enemy = enemyPlan, player = playerPlan };
            enemyPlan = new AutoBattleImpl(Faction.Enemy);
            playerPlan = null;
        }

        [HarmonyPatch(typeof(StageController), "ApplyEnemyCardPhase")]
        [HarmonyFinalizer]
        private static void Finalize_ApplyEnemyCardPhase(PhaseScope __state)
        {
            if (__state == null) return;
            enemyPlan = __state.enemy;
            playerPlan = __state.player;
        }

        [HarmonyPatch(typeof(StageController), "SetAutoCardForNonControlablePlayer")]
        [HarmonyPrefix]
        private static void Before_SetAutoCardForNonControlablePlayer(out AutoBattleImpl __state)
        {
            __state = playerPlan;
            playerPlan = new AutoBattleImpl(Faction.Player);
        }

        [HarmonyPatch(typeof(StageController), "SetAutoCardForNonControlablePlayer")]
        [HarmonyFinalizer]
        private static void Finalize_SetAutoCardForNonControlablePlayer(AutoBattleImpl __state)
        {
            playerPlan = __state;
        }

        [HarmonyPatch(typeof(StageController), "SetAutoCardForPlayer")]
        [HarmonyPrefix]
        private static void Before_SetAutoCardForPlayer(out AutoBattleImpl __state)
            => Before_SetAutoCardForNonControlablePlayer(out __state);

        [HarmonyPatch(typeof(StageController), "SetAutoCardForPlayer")]
        [HarmonyFinalizer]
        private static void Finalize_SetAutoCardForPlayer(AutoBattleImpl __state)
            => Finalize_SetAutoCardForNonControlablePlayer(__state);

        private static bool Begin(BattleAllyCardDetail detail, int slot, out SelectionScope state)
        {
            state = new SelectionScope { previous = current, owner = detail._self, slot = slot };
            // Nested auto-selection must not inherit another unit's suggestions.
            current = null;
            if (!AutoBattleImpl.Handles(detail._self)) return true;
            state.session = (detail._self.faction == Faction.Player ? playerPlan : enemyPlan) ?? new AutoBattleImpl(detail._self.faction);
            state.plan = state.session.GetPlan(detail._self, slot);
            if (state.plan == null) return false;
            current = state;
            return true;
        }

        private static void End(SelectionScope state)
        {
            if (state == null) return;
            try { state.session?.Complete(state.owner, state.slot); }
            finally { current = state.previous; }
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForPlayer")]
        [HarmonyPrefix]
        private static bool Before_PlayTurnAutoForPlayer(BattleAllyCardDetail __instance, int idx, out SelectionScope __state)
            => Begin(__instance, idx, out __state);

        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForPlayer")]
        [HarmonyFinalizer]
        private static void Finalize_PlayTurnAutoForPlayer(SelectionScope __state) => End(__state);

        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForEnemy")]
        [HarmonyPrefix]
        private static bool Before_PlayTurnAutoForEnemy(BattleAllyCardDetail __instance, int currentDiceSlotIdx, out SelectionScope __state)
            => Begin(__instance, currentDiceSlotIdx, out __state);

        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForEnemy")]
        [HarmonyFinalizer]
        private static void Finalize_PlayTurnAutoForEnemy(SelectionScope __state) => End(__state);

        [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), "AddCard")]
        [HarmonyFinalizer]
        private static void Finalize_AddCard()
        {
            enemyPlan?.Invalidate();
            playerPlan?.Invalidate();
            current?.session?.Invalidate();
        }

        // The player path already allows uncontrollable units. Only relax the enemy
        // path's control check for the unit currently executing a cached selection.
        [HarmonyPatch(typeof(BattleUnitModel), "CheckCardAvailable")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_CheckCardAvailable(IEnumerable<CodeInstruction> instructions)
        {
            var check = AccessTools.Method(typeof(BattleUnitModel), nameof(BattleUnitModel.IsControlable));
            foreach (var code in instructions)
            {
                yield return code;
                if (code.Calls(check))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AutoBattlePatch), nameof(AllowAutoSelection)));
                }
            }
        }
        private static bool AllowAutoSelection(bool original, BattleUnitModel owner) => original || current?.owner == owner;

        private static bool AvailableForPlayer(BattleUnitModel owner, BattleDiceCardModel card)
        {
            return owner.CheckCardAvailableForPlayer(card) && (current?.owner != owner || owner.IsCardChoosable(card));
        }

        private static void SortCandidates(List<KeyValuePair<int, BattleDiceCardModel>> candidates,
            Comparison<KeyValuePair<int, BattleDiceCardModel>> comparison)
        {
            var selected = current?.plan?.card;
            // The native availability checks ran first. If the forecast is no longer
            // available, leave the native candidates alone and reconcile afterwards.
            if (selected != null && candidates.Any(x => x.Value == selected))
                for (var i = 0; i < candidates.Count; i++)
                    candidates[i] = new KeyValuePair<int, BattleDiceCardModel>(candidates[i].Value == selected ? 1 : 0, candidates[i].Value);
            candidates.Sort(comparison);
        }

        private static BattleUnitModel ResolvePlayerTarget(BattleObjectManager manager, BattleUnitModel owner,
            BattleDiceCardModel card, int slot, bool teamkill)
            => ResolveTarget(manager, owner, card, slot, teamkill, true);
        private static BattleUnitModel ResolveEnemyTarget(BattleObjectManager manager, BattleUnitModel owner,
            BattleDiceCardModel card, int slot, bool teamkill)
            => ResolveTarget(manager, owner, card, slot, teamkill, false);
        private static BattleUnitModel ResolveTarget(BattleObjectManager manager, BattleUnitModel owner,
            BattleDiceCardModel card, int slot, bool teamkill, bool player)
        {
            var scope = current;
            var previous = scope?.resolvingCard;
            if (scope != null) scope.resolvingCard = card;
            try
            {
                // Keep ChangeAttackTarget and all prefixes/postfixes authoritative.
                return player ? manager.GetTargetByCardForPlayer(owner, card, slot, teamkill) : manager.GetTargetByCard(owner, card, slot, teamkill);
            }
            finally { if (scope != null) scope.resolvingCard = previous; }
        }

        private static BattleUnitModel SelectSuggestedTarget(List<BattleUnitModel> candidates)
        {
            var plan = current?.plan;
            // Replace only the base setter's random choice. Its full candidate list,
            // revive filtering, subclass overrides and Harmony hooks stay intact.
            if (plan != null && current.resolvingCard == plan.card && candidates.Contains(plan.target) &&
                AutoBattleImpl.CanTarget(plan.owner, plan.card, plan.target, plan.targetSlot)) return plan.target;
            return RandomUtil.SelectOne(candidates);
        }

        private static int ResolveTargetSlot(BattleUnitModel owner, BattleDiceCardModel card, BattleUnitModel target,
            int slot, int suggestedSlot, bool teamkill)
        {
            var plan = current?.plan;
            if (plan != null && plan.owner == owner && plan.card == card && plan.target == target &&
                AutoBattleImpl.CanTarget(owner, card, target, plan.targetSlot)) suggestedSlot = plan.targetSlot;
            // Feed the cached slot INTO the existing hook; never overwrite its result.
            return owner.ChangeTargetSlot(card, target, slot, suggestedSlot, teamkill);
        }

        [HarmonyPatch(typeof(EnemyUnitTargetSetter), "SelectTargetUnit")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_SelectTargetUnit(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                var method = code.operand as MethodInfo;
                if (method != null && method.DeclaringType == typeof(RandomUtil) && method.Name == "SelectOne" &&
                    method.IsGenericMethod && method.GetGenericArguments().SequenceEqual(new[] { typeof(BattleUnitModel) }) &&
                    method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(List<BattleUnitModel>))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = AccessTools.Method(typeof(AutoBattlePatch), nameof(SelectSuggestedTarget));
                }
                yield return code;
            }
        }

        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForPlayer")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_PlayTurnAutoForPlayer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            => AutoInstructions(instructions, generator);
        [HarmonyPatch(typeof(BattleAllyCardDetail), "PlayTurnAutoForEnemy")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Trans_PlayTurnAutoForEnemy(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            => AutoInstructions(instructions, generator);
        private static IEnumerable<CodeInstruction> AutoInstructions(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var sort = AccessTools.Method(typeof(List<KeyValuePair<int, BattleDiceCardModel>>), "Sort",
                new[] { typeof(Comparison<KeyValuePair<int, BattleDiceCardModel>>) });
            var player = AccessTools.Method(typeof(BattleObjectManager), nameof(BattleObjectManager.GetTargetByCardForPlayer));
            var enemy = AccessTools.Method(typeof(BattleObjectManager), nameof(BattleObjectManager.GetTargetByCard));
            var slot = AccessTools.Method(typeof(BattleUnitModel), nameof(BattleUnitModel.ChangeTargetSlot));
            var available = AccessTools.Method(typeof(BattleUnitModel), nameof(BattleUnitModel.CheckCardAvailableForPlayer));
            foreach (var code in instructions)
            {
                string replacement = null;
                var target = code.Calls(player) || code.Calls(enemy);
                if (code.Calls(sort)) replacement = nameof(SortCandidates);
                else if (code.Calls(player)) replacement = nameof(ResolvePlayerTarget);
                else if (code.Calls(enemy)) replacement = nameof(ResolveEnemyTarget);
                else if (code.Calls(slot)) replacement = nameof(ResolveTargetSlot);
                else if (code.Calls(available)) replacement = nameof(AvailableForPlayer);
                if (replacement != null)
                {
                    code.opcode = OpCodes.Call;
                    code.operand = AccessTools.Method(typeof(AutoBattlePatch), replacement);
                }
                yield return code;
                if (target)
                {
                    // Vanilla's player path dereferences the target before checking
                    // null. A buff may remove every target after the plan was made.
                    var found = generator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Brtrue, found);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ret);
                    var next = new CodeInstruction(OpCodes.Nop);
                    next.labels.Add(found);
                    yield return next;
                }
            }
        }
    }
}
