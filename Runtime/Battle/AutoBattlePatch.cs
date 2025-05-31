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
    public enum CustomClashType { None, Reactive, Proactive }

    // Helper structure to store a potential action and its priority
    public class PotentialAction
    {
        public BattleUnitModel Caster { get; set; }
        public BattleDiceCardModel CardToUse { get; set; }
        public int CasterDiceSlotIndex { get; set; }
        public SpeedDice CasterSpeedDice { get; set; } // 아군 속도 주사위 값 저장
        public BattleUnitModel TargetEnemy { get; set; }
        public int TargetEnemyDiceSlotIndex { get; set; }
        public SpeedDice TargetEnemySpeedDice { get; set; } // 적 속도 주사위 값 (알 수 있다면)
        public BattleDiceCardModel TargetEnemyCard { get; set; } // 적이 해당 슬롯에 사용한 카드
        public bool IsClashAttemptFromAI { get; set; } // AI가 합을 의도했는가 (GetCustomizedCardPriority에 전달된 값)
        public bool IsClashPossibleByGameRules { get; set; } // 게임 규칙상 실제 합이 가능한가
        public CustomClashType ClashType { get; set; } // 합의 종류
        public int Priority { get; set; }
        public bool IsInstanceCard { get; set; }
        public bool IsEgoCard { get; set; }

        public PotentialAction(
            BattleUnitModel caster, BattleDiceCardModel card, int casterSlot, SpeedDice casterSpeedDice,
            BattleUnitModel targetEnemy, int targetSlot, SpeedDice targetEnemySpeedDice, BattleDiceCardModel targetEnemyCard,
            bool isClashAttempt, bool isClashPossible, CustomClashType clashType, 
            int priority, bool isInstance, bool isEgo)
        {
            Caster = caster;
            CardToUse = card;
            CasterDiceSlotIndex = casterSlot;
            CasterSpeedDice = casterSpeedDice;
            TargetEnemy = targetEnemy;
            TargetEnemyDiceSlotIndex = targetSlot;
            TargetEnemySpeedDice = targetEnemySpeedDice;
            TargetEnemyCard = targetEnemyCard;
            IsClashAttemptFromAI = isClashAttempt;
            IsClashPossibleByGameRules = isClashPossible;
            ClashType = clashType;
            Priority = priority;
            IsInstanceCard = isInstance;
            IsEgoCard = isEgo;
        }
    }

    class AutoBattlePatch
    {
        public void Initialize()
        {
            var harmony = new Harmony("LibraryOfAngela.AutoBattlePatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

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

        // ڵ ó 
        [HarmonyPatch(typeof(StageController), "SetAutoCardForNonControlablePlayer")]
        [HarmonyPostfix]
        private static void After_SetAutoCardForNonControlablePlayer(StageController __instance)
        {
            IsForceInjecting = true;
            StringBuilder sbLog = new StringBuilder();
            sbLog.AppendLine("[AutoBattlePatch] After_SetAutoCardForNonControlablePlayer - START");

            var battleObjectManager = BattleObjectManager.instance;
            if (battleObjectManager == null)
            {
                sbLog.AppendLine("[AutoBattlePatch] BattleObjectManager.instance is null. Exiting.");
                Debug.Log(sbLog.ToString());
                IsForceInjecting = false;
                return;
            }

            var initialAliveAllies = battleObjectManager.GetAliveList(Faction.Player).Where(u => !u.IsDead()).ToList();
            var initialAliveEnemies = battleObjectManager.GetAliveList(Faction.Enemy).Where(u => !u.IsDead()).ToList();

            var customAISetterEnemies = initialAliveEnemies
                .Where(u => u.IsActionable() && BattleInterfaceCache.Of<ICustomCardSetter>(u).Any())
                .ToList();
            var customAISetterAllies = initialAliveAllies
                .Where(u => u.IsActionable() && BattleInterfaceCache.Of<ICustomCardSetter>(u).Any())
                .ToList();

            if (!customAISetterAllies.Any() && !customAISetterEnemies.Any())
            {
                sbLog.AppendLine("[AutoBattlePatch] No units with ICustomCardSetter found. Exiting custom AI logic.");
                Debug.Log(sbLog.ToString());
                IsForceInjecting = false;
                return;
            }

            Dictionary<BattleUnitModel, int> accumulatedDamageOnTargets = new Dictionary<BattleUnitModel, int>();
            HashSet<Tuple<BattleUnitModel, int>> usedAllySlots = new HashSet<Tuple<BattleUnitModel, int>>();
            HashSet<Tuple<BattleUnitModel, int>> clashedEnemySlots = new HashSet<Tuple<BattleUnitModel, int>>();

            Func<BattleUnitModel, string> GetUnitName = (BattleUnitModel unit) => 
            {
                if (unit == null) return "Unit_Null";
                string bookName = unit.Book?.GetName();
                if (!string.IsNullOrEmpty(bookName)) return bookName;
                // BattleUnitModel.UnitData (UnitBattleDataModel) -> .unitData (UnitDataModel) -> .name (string)
                string unitDataName = unit.UnitData?.unitData?.name;
                if (!string.IsNullOrEmpty(unitDataName)) return unitDataName;
                return $"Unit_{unit.index}";
            };

            sbLog.AppendLine("[AutoBattlePatch] --- Starting Enemy Action Phase (Custom AI) ---");
            foreach (var enemyUnit in customAISetterEnemies)
            {
                string enemyName = GetUnitName(enemyUnit);
                var customSetter = BattleInterfaceCache.Of<ICustomCardSetter>(enemyUnit).FirstOrDefault();
                if (customSetter == null) continue;

                if (enemyUnit.cardSlotDetail == null || enemyUnit.speedDiceResult == null || !enemyUnit.speedDiceResult.Any())
                {
                    sbLog.AppendLine($"[AutoBattlePatch] Enemy {enemyName} has null cardSlotDetail or no speed dice. Skipping.");
                    continue;
                }

                var enemyHand = enemyUnit.allyCardDetail.GetHand();
                var availableCards = enemyHand.Where(c => c.GetSpec().Ranged != CardRange.Instance).ToList();

                for (int casterSlotIdx = 0; casterSlotIdx < enemyUnit.speedDiceCount; casterSlotIdx++)
                {
                    if (enemyUnit.cardSlotDetail.cardAry.Count > casterSlotIdx && enemyUnit.cardSlotDetail.cardAry[casterSlotIdx] != null)
                    {
                        continue;
                    }
                    
                    PotentialAction bestActionForEnemySlot = null;

                    foreach (var card in availableCards)
                    {
                        bool isCardUsableByEnemy = enemyUnit.CheckCardAvailable(card);
                        if (!isCardUsableByEnemy) continue;

                        foreach (var targetAlly in initialAliveAllies)
                        {
                            if (!IsCardActuallyTargetable(enemyUnit, card, targetAlly)) continue;
                            
                            int targetAllySlotForLogic = 0;
                            BattleDiceCardModel targetAllyCardOnSlot = (targetAlly.cardSlotDetail.cardAry.Count > targetAllySlotForLogic && targetAlly.cardSlotDetail.cardAry[targetAllySlotForLogic] != null) ? targetAlly.cardSlotDetail.cardAry[targetAllySlotForLogic].card : null;

                            int priority = customSetter.GetCustomizedCardPriority(
                                enemyUnit, card, enemyUnit.speedDiceResult[casterSlotIdx].value, casterSlotIdx, 
                                targetAlly, targetAllySlotForLogic, targetAllyCardOnSlot, false, 
                                initialAliveAllies, initialAliveEnemies
                            );

                            if (CanPassDamageThreshold(enemyUnit, card, targetAlly, accumulatedDamageOnTargets)) 
                            {
                                if (priority >= 0 && (bestActionForEnemySlot == null || priority > bestActionForEnemySlot.Priority))
                                {
                                    SpeedDice targetAllySpeedDice = (targetAlly.speedDiceResult != null && targetAlly.speedDiceResult.Count > targetAllySlotForLogic) ? targetAlly.speedDiceResult[targetAllySlotForLogic] : null;
                                    bestActionForEnemySlot = new PotentialAction(enemyUnit, card, casterSlotIdx, enemyUnit.speedDiceResult[casterSlotIdx], targetAlly, targetAllySlotForLogic, targetAllySpeedDice, targetAllyCardOnSlot, false, false, CustomClashType.None, priority, false, false);
                                }
                            }
                        }
                    }

                    if (bestActionForEnemySlot != null)
                    {
                        string targetName = GetUnitName(bestActionForEnemySlot.TargetEnemy);
                        BattleUnitModel currentCaster = bestActionForEnemySlot.Caster;
                        BattleDiceCardModel currentCard = bestActionForEnemySlot.CardToUse;
                        int currentCasterSlotIdx = bestActionForEnemySlot.CasterDiceSlotIndex;
                        BattleUnitModel currentTarget = bestActionForEnemySlot.TargetEnemy;
                        int currentTargetSlotIdx = bestActionForEnemySlot.TargetEnemyDiceSlotIndex;

                        bool isTargetSlotValidForAddCard = currentTarget.speedDiceResult != null &&
                                                           currentTargetSlotIdx >= 0 &&
                                                           currentTargetSlotIdx < currentTarget.speedDiceResult.Count;
                        
                        if (isTargetSlotValidForAddCard)
                        {
                            try
                            {
                                sbLog.AppendLine($"[AutoBattlePatch DEBUG] Attempting Enemy AddCard: Caster: {GetUnitName(currentCaster)} (SlotIdx: {currentCasterSlotIdx}), Card: {currentCard.GetName()}, Target: {targetName} (SlotIdx: {currentTargetSlotIdx})");
                                currentCaster.SetCurrentOrder(currentCasterSlotIdx);
                                currentCaster.cardSlotDetail.AddCard(currentCard, currentTarget, currentTargetSlotIdx, true); 
                                
                                sbLog.AppendLine($"[AutoBattlePatch] Enemy {enemyName} (Slot {currentCasterSlotIdx + 1}) uses {currentCard.GetName()} on {targetName} (Slot {currentTargetSlotIdx +1}, Prio: {bestActionForEnemySlot.Priority})");
                                int estimatedDamage = EstimateCardDamage(currentCard);
                                if (!accumulatedDamageOnTargets.ContainsKey(currentTarget))
                                    accumulatedDamageOnTargets[currentTarget] = 0;
                                accumulatedDamageOnTargets[currentTarget] += estimatedDamage;
                                availableCards.Remove(currentCard); 
                            }
                            catch (Exception ex)
                            {
                                sbLog.AppendLine($"[AutoBattlePatch ERROR] Exception during Enemy AddCard:");
                                sbLog.AppendLine($"  Phase: Enemy Action");
                                sbLog.AppendLine($"  Caster: {GetUnitName(currentCaster)}, Index: {currentCaster.index}, SlotIdxToSetOrder: {currentCasterSlotIdx}");
                                sbLog.AppendLine($"  Card: {currentCard?.GetName() ?? "NULL_CARD"}, ID: {currentCard?.GetID()?.ToString() ?? "NULL_ID"}");
                                sbLog.AppendLine($"  Target: {GetUnitName(currentTarget)}, Index: {currentTarget.index}, TargetSlotForAddCard: {currentTargetSlotIdx}");
                                sbLog.AppendLine($"  AddCard Params: card={currentCard?.GetName()}, target={GetUnitName(currentTarget)}, targetSlot={currentTargetSlotIdx}, isEnemy=true");
                                sbLog.AppendLine($"  Exception: {ex.GetType().Name} - {ex.Message}");
                                sbLog.AppendLine($"  StackTrace: {ex.StackTrace}");
                            }
                        }
                        else
                        {
                            sbLog.AppendLine($"[AutoBattlePatch] SKIPPED AddCard (Enemy Action): Enemy {enemyName} target {targetName}. Target slot {currentTargetSlotIdx + 1} is invalid or target has no speed dice for this slot. (Target Speed Dice Count: {currentTarget.speedDiceResult?.Count ?? 0})");
                        }
                    }
                }
            }
            sbLog.AppendLine("[AutoBattlePatch] --- Finished Enemy Action Phase (Custom AI) ---");

            sbLog.AppendLine("[AutoBattlePatch] --- Starting Ally Instance/EGO Card Phase ---");
            foreach (var allyUnit in customAISetterAllies)
            {
                string allyName = GetUnitName(allyUnit);
                var customSetter = BattleInterfaceCache.Of<ICustomCardSetter>(allyUnit).FirstOrDefault();
                if (customSetter == null) continue;
                if (allyUnit.cardSlotDetail == null || allyUnit.speedDiceResult == null || !allyUnit.speedDiceResult.Any())
                {
                    sbLog.AppendLine($"[AutoBattlePatch] Ally {allyName} has null cardSlotDetail or no speed dice for Instance/EGO. Skipping.");
                    continue;
                }

                var allyHand = allyUnit.allyCardDetail.GetHand();
                var egoHand = allyUnit.personalEgoDetail?.GetHand() ?? new List<BattleDiceCardModel>();

                var instanceAndEgoCards = allyHand.Where(c => c.GetSpec().Ranged == CardRange.Instance).ToList();
                instanceAndEgoCards.AddRange(egoHand); 
                instanceAndEgoCards = instanceAndEgoCards.Distinct().ToList();

                foreach (var card in instanceAndEgoCards)
                {
                    bool isEgo = card.XmlData.IsPersonal();
                    bool isInstance = card.GetSpec().Ranged == CardRange.Instance;
                    string cardType = isEgo ? "EGO" : (isInstance ? "Instance" : "NormalDeck"); 

                    bool isCardUsableByAlly;
                    if (isEgo)
                    {
                        bool canAddEgo = card.CanAddedEgoCard();
                        bool checkAvailable = allyUnit.CheckCardAvailable(card);
                        isCardUsableByAlly = canAddEgo && checkAvailable;
                    }
                    else 
                    {
                        isCardUsableByAlly = allyUnit.CheckCardAvailable(card);
                    }

                    if (!isCardUsableByAlly) continue;

                    PotentialAction bestActionForInstanceEgo = null;
                    int allySlotForInstanceEgo = -1;
                    for (int i = 0; i < allyUnit.speedDiceCount; i++)
                    {
                        if (!usedAllySlots.Contains(Tuple.Create(allyUnit, i)) && (allyUnit.cardSlotDetail.cardAry.Count <= i || allyUnit.cardSlotDetail.cardAry[i] == null))
                        {
                            allySlotForInstanceEgo = i;
                            break;
                        }
                    }

                    if (allySlotForInstanceEgo == -1) continue;
                    
                    foreach (var enemyTarget in initialAliveEnemies)
                    {
                        if (!IsCardActuallyTargetable(allyUnit, card, enemyTarget)) continue;

                        int currentSpeedForInstance = (allyUnit.speedDiceResult != null && allyUnit.speedDiceResult.Count > allySlotForInstanceEgo) ? allyUnit.speedDiceResult[allySlotForInstanceEgo].value : 0;
                        int priority = customSetter.GetCustomizedCardPriority(
                            allyUnit, card, currentSpeedForInstance, allySlotForInstanceEgo, 
                            enemyTarget, 0, null, false, 
                            initialAliveEnemies, initialAliveAllies
                        );
                        
                        if (isEgo && card.GetCost() > 0 && priority < 0) continue;
                        if (isInstance && !isEgo && card.GetCost() > 0 && priority < 0) continue;
                        if (card.GetCost() == 0 && priority < 0) continue;

                        if (priority >= 0 && (bestActionForInstanceEgo == null || priority > bestActionForInstanceEgo.Priority))
                        {
                            SpeedDice speedDiceToUse = (allyUnit.speedDiceResult != null && allyUnit.speedDiceResult.Count > allySlotForInstanceEgo) ? allyUnit.speedDiceResult[allySlotForInstanceEgo] : null;
                            bestActionForInstanceEgo = new PotentialAction(allyUnit, card, allySlotForInstanceEgo, speedDiceToUse, enemyTarget, 0, null, null, false, false, CustomClashType.None, priority, isInstance, isEgo);
                        }
                    }

                    if (bestActionForInstanceEgo != null)
                    {
                        string targetName = GetUnitName(bestActionForInstanceEgo.TargetEnemy);
                        BattleUnitModel currentCaster = bestActionForInstanceEgo.Caster;
                        BattleDiceCardModel currentCard = bestActionForInstanceEgo.CardToUse;
                        int currentCasterSlotIdx = bestActionForInstanceEgo.CasterDiceSlotIndex;
                        BattleUnitModel currentTarget = bestActionForInstanceEgo.TargetEnemy;
                        int currentTargetSlotIdx = bestActionForInstanceEgo.TargetEnemyDiceSlotIndex; // This is 0 for instance/ego

                        bool isTargetSlotValidForAddCard = currentTarget.speedDiceResult != null &&
                                                           currentTargetSlotIdx >= 0 &&
                                                           currentTargetSlotIdx < currentTarget.speedDiceResult.Count;

                        if (isTargetSlotValidForAddCard)
                        {
                            try
                            {
                                sbLog.AppendLine($"[AutoBattlePatch DEBUG] Attempting Ally Instance/EGO AddCard: Caster: {GetUnitName(currentCaster)} (SlotIdx: {currentCasterSlotIdx}), Card: {currentCard.GetName()}, Target: {targetName} (SlotIdx: {currentTargetSlotIdx})");
                                currentCaster.SetCurrentOrder(currentCasterSlotIdx);
                                currentCaster.cardSlotDetail.AddCard(currentCard, currentTarget, currentTargetSlotIdx, false); 

                                sbLog.AppendLine($"[AutoBattlePatch] Ally {allyName} ({cardType}, Slot {currentCasterSlotIdx + 1}) uses {currentCard.GetName()} on {targetName} (Target Slot {currentTargetSlotIdx + 1}, Prio: {bestActionForInstanceEgo.Priority})");
                                usedAllySlots.Add(Tuple.Create(currentCaster, currentCasterSlotIdx));
                                break; 
                            }
                            catch (Exception ex)
                            {
                                sbLog.AppendLine($"[AutoBattlePatch ERROR] Exception during Ally Instance/EGO AddCard:");
                                sbLog.AppendLine($"  Phase: Ally Instance/EGO");
                                sbLog.AppendLine($"  Caster: {GetUnitName(currentCaster)}, Index: {currentCaster.index}, SlotIdxToSetOrder: {currentCasterSlotIdx}");
                                sbLog.AppendLine($"  Card: {currentCard?.GetName() ?? "NULL_CARD"}, ID: {currentCard?.GetID()?.ToString() ?? "NULL_ID"}");
                                sbLog.AppendLine($"  Target: {GetUnitName(currentTarget)}, Index: {currentTarget.index}, TargetSlotForAddCard: {currentTargetSlotIdx}");
                                sbLog.AppendLine($"  AddCard Params: card={currentCard?.GetName()}, target={GetUnitName(currentTarget)}, targetSlot={currentTargetSlotIdx}, isEnemy=false");
                                sbLog.AppendLine($"  Exception: {ex.GetType().Name} - {ex.Message}");
                                sbLog.AppendLine($"  StackTrace: {ex.StackTrace}");
                            }
                        }
                        else
                        {
                            sbLog.AppendLine($"[AutoBattlePatch] SKIPPED AddCard (Ally Instance/EGO): Ally {allyName} target {targetName}. Target slot {currentTargetSlotIdx + 1} is invalid or target has no speed dice for this slot. (Target Speed Dice Count: {currentTarget.speedDiceResult?.Count ?? 0})");
                        }
                    }
                }
            }
            sbLog.AppendLine("[AutoBattlePatch] --- Finished Ally Instance/EGO Card Phase ---");

            sbLog.AppendLine("[AutoBattlePatch] --- Starting Ally Clash Planning Phase ---");
            List<PotentialAction> allPotentialAllyActions = new List<PotentialAction>(); 
            var bestClashActionPerEnemySlot = new Dictionary<Tuple<BattleUnitModel, int>, PotentialAction>();

            foreach (var allyUnit in customAISetterAllies)
            {
                string allyName = GetUnitName(allyUnit);
                var customSetter = BattleInterfaceCache.Of<ICustomCardSetter>(allyUnit).FirstOrDefault();
                if (customSetter == null) continue;
                 if (allyUnit.cardSlotDetail == null || allyUnit.speedDiceResult == null || !allyUnit.speedDiceResult.Any())
                {
                    sbLog.AppendLine($"[AutoBattlePatch] Ally {allyName} has null cardSlotDetail or no speed dice for Clash. Skipping.");
                    continue;
                }

                var allyHand = allyUnit.allyCardDetail.GetHand().Where(c => c.GetSpec().Ranged != CardRange.Instance && !c.XmlData.IsPersonal()).ToList();

                for (int casterSlotIdx = 0; casterSlotIdx < allyUnit.speedDiceCount; casterSlotIdx++)
                {
                    var allySpeedDice = allyUnit.speedDiceResult[casterSlotIdx];

                    if (usedAllySlots.Contains(Tuple.Create(allyUnit, casterSlotIdx))) continue;
                    if (allyUnit.cardSlotDetail.cardAry.Count > casterSlotIdx && allyUnit.cardSlotDetail.cardAry[casterSlotIdx] != null) continue;

                    foreach (var card in allyHand)
                    {
                        bool isCardUsableForClash = allyUnit.CheckCardAvailable(card);
                        if (!isCardUsableForClash) continue;

                        foreach (var enemyTarget in initialAliveEnemies)
                        {
                            string enemyTargetName = GetUnitName(enemyTarget);
                            if (!IsCardActuallyTargetable(allyUnit, card, enemyTarget)) continue;
                            if (card.GetSpec().Ranged == CardRange.FarArea || card.GetSpec().Ranged == CardRange.FarAreaEach) continue;

                            for (int enemySlotIdx = 0; enemySlotIdx < enemyTarget.speedDiceCount; enemySlotIdx++)
                            {
                                BattlePlayingCardDataInUnitModel enemyCardDataOnSlot = (enemyTarget.cardSlotDetail.cardAry.Count > enemySlotIdx) ? enemyTarget.cardSlotDetail.cardAry[enemySlotIdx] : null;
                                BattleDiceCardModel enemyCardOnSlot = enemyCardDataOnSlot?.card;
                                SpeedDice enemySpeedDiceOnSlot = (enemyTarget.speedDiceResult.Count > enemySlotIdx) ? enemyTarget.speedDiceResult[enemySlotIdx] : null;
                                
                                bool isClashAttemptByAI = true; 
                                bool actualClashPossible = false;
                                CustomClashType clashType = CustomClashType.None;

                                if (enemyCardOnSlot != null) 
                                {
                                    actualClashPossible = true; 
                                    if (enemyCardDataOnSlot.target == allyUnit && enemyCardDataOnSlot.targetSlotOrder == casterSlotIdx) 
                                    {
                                        clashType = CustomClashType.Reactive;
                                    }
                                    else 
                                    {
                                        clashType = CustomClashType.Proactive;
                                    }
                                }
                                else 
                                {
                                    continue; 
                                }
                                
                                int priority = customSetter.GetCustomizedCardPriority(
                                    allyUnit, card, allySpeedDice.value, casterSlotIdx,
                                    enemyTarget, enemySlotIdx, enemyCardOnSlot, 
                                    isClashAttemptByAI, 
                                    initialAliveEnemies, initialAliveAllies
                                );

                                var enemySlotKey = Tuple.Create(enemyTarget, enemySlotIdx);
                                if (priority >= 0) 
                                {
                                    var potentialNewClash = new PotentialAction(
                                        allyUnit, card, casterSlotIdx, allySpeedDice,
                                        enemyTarget, enemySlotIdx, enemySpeedDiceOnSlot, enemyCardOnSlot,
                                        isClashAttemptByAI, actualClashPossible, clashType,
                                        priority, false, false 
                                    );

                                    if (!bestClashActionPerEnemySlot.TryGetValue(enemySlotKey, out var existingBestClash) || priority > existingBestClash.Priority)
                                    {
                                        bestClashActionPerEnemySlot[enemySlotKey] = potentialNewClash;
                                    }
                                }
                                else if (priority < 0) 
                                {
                                    bool isEnemyCardLowThreat = enemyCardOnSlot != null && 
                                                                (enemyCardOnSlot.GetBehaviourList().All(b => b.Type == BehaviourType.Def || b.Type == BehaviourType.Standby) || 
                                                                 EstimateCardDamage(enemyCardOnSlot) < 5); 

                                    if (isClashAttemptByAI && actualClashPossible && (clashType == CustomClashType.Reactive || isEnemyCardLowThreat))
                                    {
                                        sbLog.AppendLine($"[AutoBattlePatch] Ally {allyName}'s card {card.GetName()} (Prio {priority}) considered for FORCED CLASH against {enemyTargetName} slot {enemySlotIdx+1} (Enemy card: {enemyCardOnSlot?.GetName() ?? "None"}).");
                                        var forcedClashAction = new PotentialAction(
                                            allyUnit, card, casterSlotIdx, allySpeedDice,
                                            enemyTarget, enemySlotIdx, enemySpeedDiceOnSlot, enemyCardOnSlot,
                                            isClashAttemptByAI, actualClashPossible, clashType,
                                            priority, false, false 
                                        );
                                        allPotentialAllyActions.Add(forcedClashAction);
                                    }
                                }
                            } 
                        } 
                    } 
                } 
            }
            sbLog.AppendLine("[AutoBattlePatch] --- Finished Ally Clash Planning Phase ---");

            sbLog.AppendLine("[AutoBattlePatch] --- Starting Ally Clash Finalization & Execution Phase ---");
            var confirmedClashes = new List<PotentialAction>();
            foreach (var kvp in bestClashActionPerEnemySlot.Where(x => x.Value.Priority >=0).OrderByDescending(x => x.Value.Priority)) 
            {
                var bestClashAction = kvp.Value;
                var allyUnit = bestClashAction.Caster;
                var enemyUnit = bestClashAction.TargetEnemy;
                int allySlotIdx = bestClashAction.CasterDiceSlotIndex;
                int enemySlotIdx = bestClashAction.TargetEnemyDiceSlotIndex;
                string currentAllyName = GetUnitName(allyUnit);
                string currentEnemyName = GetUnitName(enemyUnit);

                bool allySlotAlreadyUsedForConfirmedClash = confirmedClashes.Any(c => c.Caster == allyUnit && c.CasterDiceSlotIndex == allySlotIdx);

                if (!usedAllySlots.Contains(Tuple.Create(allyUnit, allySlotIdx)) && !clashedEnemySlots.Contains(Tuple.Create(enemyUnit, enemySlotIdx)) && !allySlotAlreadyUsedForConfirmedClash)
                {
                    BattleUnitModel currentTarget = enemyUnit; // Clash target is enemyUnit
                    int currentTargetSlotIdx = enemySlotIdx;   // Clash target slot is enemySlotIdx

                    bool isTargetSlotValidForAddCard = currentTarget.speedDiceResult != null &&
                                                       currentTargetSlotIdx >= 0 &&
                                                       currentTargetSlotIdx < currentTarget.speedDiceResult.Count;
                    if (isTargetSlotValidForAddCard)
                    {
                        try
                        {
                            sbLog.AppendLine($"[AutoBattlePatch DEBUG] Attempting Ally CLASH AddCard: Caster: {GetUnitName(allyUnit)} (SlotIdx: {allySlotIdx}), Card: {bestClashAction.CardToUse.GetName()}, Target: {GetUnitName(currentTarget)} (SlotIdx: {currentTargetSlotIdx})");
                            allyUnit.SetCurrentOrder(allySlotIdx);
                            allyUnit.cardSlotDetail.AddCard(bestClashAction.CardToUse, currentTarget, currentTargetSlotIdx, false); 
                            
                            sbLog.AppendLine($"[AutoBattlePatch] Confirming CLASH: Ally {currentAllyName} (Card {bestClashAction.CardToUse.GetName()}, Slot {allySlotIdx + 1}) vs Enemy {currentEnemyName} (Card {bestClashAction.TargetEnemyCard?.GetName() ?? "None"}, Slot {currentTargetSlotIdx + 1}). Prio: {bestClashAction.Priority}");
                            usedAllySlots.Add(Tuple.Create(allyUnit, allySlotIdx));
                            clashedEnemySlots.Add(Tuple.Create(enemyUnit, enemySlotIdx)); 
                            confirmedClashes.Add(bestClashAction); 
                        }
                        catch (Exception ex)
                        {
                            sbLog.AppendLine($"[AutoBattlePatch ERROR] Exception during Ally CLASH AddCard:");
                            sbLog.AppendLine($"  Phase: Ally Clash Finalization");
                            sbLog.AppendLine($"  Caster: {GetUnitName(allyUnit)}, Index: {allyUnit.index}, SlotIdxToSetOrder: {allySlotIdx}");
                            sbLog.AppendLine($"  Card: {bestClashAction.CardToUse?.GetName() ?? "NULL_CARD"}, ID: {bestClashAction.CardToUse?.GetID()?.ToString() ?? "NULL_ID"}");
                            sbLog.AppendLine($"  Target: {GetUnitName(currentTarget)}, Index: {currentTarget.index}, TargetSlotForAddCard: {currentTargetSlotIdx}");
                            sbLog.AppendLine($"  Enemy Card on Slot: {bestClashAction.TargetEnemyCard?.GetName() ?? "None"}");
                            sbLog.AppendLine($"  AddCard Params: card={bestClashAction.CardToUse?.GetName()}, target={GetUnitName(currentTarget)}, targetSlot={currentTargetSlotIdx}, isEnemy=false");
                            sbLog.AppendLine($"  Exception: {ex.GetType().Name} - {ex.Message}");
                            sbLog.AppendLine($"  StackTrace: {ex.StackTrace}");
                            allPotentialAllyActions.Add(bestClashAction); // 실패한 합 시도는 일방 공격 후보로 다시 추가
                        }
                    }
                    else
                    {
                        allPotentialAllyActions.Add(bestClashAction); 
                    }
                }
                else
                {
                    allPotentialAllyActions.Add(bestClashAction); 
                }
            }
            sbLog.AppendLine("[AutoBattlePatch] --- Finished Ally Clash Finalization & Execution Phase ---");

            sbLog.AppendLine("[AutoBattlePatch] --- Starting Ally Unilateral Attack Phase ---");
            List<PotentialAction> unilateralAttackCandidates = new List<PotentialAction>();
            unilateralAttackCandidates.AddRange(allPotentialAllyActions);
            allPotentialAllyActions.Clear();

            foreach (var allyUnit in customAISetterAllies)
            {
                string allyName = GetUnitName(allyUnit);
                var customSetter = BattleInterfaceCache.Of<ICustomCardSetter>(allyUnit).FirstOrDefault();
                if (customSetter == null) continue;
                if (allyUnit.cardSlotDetail == null || allyUnit.speedDiceResult == null || !allyUnit.speedDiceResult.Any()) continue;

                var allyHand = allyUnit.allyCardDetail.GetHand().Where(c => c.GetSpec().Ranged != CardRange.Instance && !c.XmlData.IsPersonal()).ToList();
                var cardsUsedInClashesByThisAlly = confirmedClashes.Where(c => c.Caster == allyUnit).Select(c => c.CardToUse).ToList();
                allyHand = allyHand.Except(cardsUsedInClashesByThisAlly).ToList(); 

                for (int casterSlotIdx = 0; casterSlotIdx < allyUnit.speedDiceCount; casterSlotIdx++)
                {
                    if (usedAllySlots.Contains(Tuple.Create(allyUnit, casterSlotIdx))) continue;
                    if (allyUnit.cardSlotDetail.cardAry.Count > casterSlotIdx && allyUnit.cardSlotDetail.cardAry[casterSlotIdx] != null) continue;

                    var allySpeedDice = allyUnit.speedDiceResult[casterSlotIdx];

                    foreach (var card in allyHand)
                    {
                        bool isCardUsableForUnilateral = allyUnit.CheckCardAvailable(card);
                        if (!isCardUsableForUnilateral) continue;

                        foreach (var enemyTarget in initialAliveEnemies)
                        {
                            if (!IsCardActuallyTargetable(allyUnit, card, enemyTarget)) continue;
                            
                            int priority = customSetter.GetCustomizedCardPriority(
                                allyUnit, card, allySpeedDice.value, casterSlotIdx,
                                enemyTarget, 0, null, false, 
                                initialAliveEnemies, initialAliveAllies
                            );

                            if (priority >= 0)
                            {
                                unilateralAttackCandidates.Add(new PotentialAction(
                                    allyUnit, card, casterSlotIdx, allySpeedDice,
                                    enemyTarget, 0, null, null, 
                                    false, false, CustomClashType.None, priority, false, false
                                ));
                            }
                        }
                    }
                }
            }
            
            var sortedUnilateralAttacks = unilateralAttackCandidates
                .Where(a => !usedAllySlots.Contains(Tuple.Create(a.Caster, a.CasterDiceSlotIndex))) 
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => EstimateCardDamage(a.CardToUse))
                .ToList();

            HashSet<BattleDiceCardModel> usedCardsInUnilateralPhase = new HashSet<BattleDiceCardModel>(); 

            foreach (var action in sortedUnilateralAttacks)
            {
                string casterName = GetUnitName(action.Caster);
                string targetName = GetUnitName(action.TargetEnemy);

                if (usedAllySlots.Contains(Tuple.Create(action.Caster, action.CasterDiceSlotIndex))) continue;
                
                bool isActionCardRangeAttack = (action.CardToUse.GetSpec().Ranged == CardRange.FarArea || action.CardToUse.GetSpec().Ranged == CardRange.FarAreaEach);
                if (isActionCardRangeAttack)
                {
                    if (usedCardsInUnilateralPhase.Contains(action.CardToUse)) continue;
                }

                BattleUnitModel currentTarget = action.TargetEnemy;
                int currentTargetSlotIdx = action.TargetEnemyDiceSlotIndex;

                bool isTargetSlotValidForAddCard = currentTarget.speedDiceResult != null &&
                                                   currentTargetSlotIdx >= 0 &&
                                                   currentTargetSlotIdx < currentTarget.speedDiceResult.Count;
                if (isTargetSlotValidForAddCard)
                {
                    try
                    {
                        sbLog.AppendLine($"[AutoBattlePatch DEBUG] Attempting Ally UNILATERAL AddCard: Caster: {GetUnitName(action.Caster)} (SlotIdx: {action.CasterDiceSlotIndex}), Card: {action.CardToUse.GetName()}, Target: {GetUnitName(currentTarget)} (SlotIdx: {currentTargetSlotIdx})");
                        action.Caster.SetCurrentOrder(action.CasterDiceSlotIndex);
                        action.Caster.cardSlotDetail.AddCard(action.CardToUse, currentTarget, currentTargetSlotIdx, false); 
                        
                        sbLog.AppendLine($"[AutoBattlePatch] Executing UNILATERAL ATTACK: Ally {casterName} (Card {action.CardToUse.GetName()}, Slot {action.CasterDiceSlotIndex + 1}, Prio {action.Priority}) -> Enemy {targetName} (Target Slot {currentTargetSlotIdx+1})");
                        usedAllySlots.Add(Tuple.Create(action.Caster, action.CasterDiceSlotIndex));
                        if (isActionCardRangeAttack)
                        {
                            usedCardsInUnilateralPhase.Add(action.CardToUse); 
                        }

                        int estimatedDamage = EstimateCardDamage(action.CardToUse);
                        if (!accumulatedDamageOnTargets.ContainsKey(currentTarget))
                            accumulatedDamageOnTargets[currentTarget] = 0;
                        accumulatedDamageOnTargets[currentTarget] += estimatedDamage;
                    }
                    catch (Exception ex)
                    {
                        sbLog.AppendLine($"[AutoBattlePatch ERROR] Exception during Ally UNILATERAL AddCard:");
                        sbLog.AppendLine($"  Phase: Ally Unilateral Attack");
                        sbLog.AppendLine($"  Caster: {GetUnitName(action.Caster)}, Index: {action.Caster.index}, SlotIdxToSetOrder: {action.CasterDiceSlotIndex}");
                        sbLog.AppendLine($"  Card: {action.CardToUse?.GetName() ?? "NULL_CARD"}, ID: {action.CardToUse?.GetID()?.ToString() ?? "NULL_ID"}");
                        sbLog.AppendLine($"  Target: {GetUnitName(currentTarget)}, Index: {currentTarget.index}, TargetSlotForAddCard: {currentTargetSlotIdx}");
                        sbLog.AppendLine($"  AddCard Params: card={action.CardToUse?.GetName()}, target={GetUnitName(currentTarget)}, targetSlot={currentTargetSlotIdx}, isEnemy=false");
                        sbLog.AppendLine($"  Exception: {ex.GetType().Name} - {ex.Message}");
                        sbLog.AppendLine($"  StackTrace: {ex.StackTrace}");
                    }
                }
                else
                {
                    sbLog.AppendLine($"[AutoBattlePatch] SKIPPED AddCard (Ally Unilateral): Ally {casterName} target {targetName}. Target slot {currentTargetSlotIdx + 1} is invalid or target has no speed dice for this slot. (Target Speed Dice Count: {currentTarget.speedDiceResult?.Count ?? 0})");
                }
            }
            sbLog.AppendLine("[AutoBattlePatch] --- Finished Ally Unilateral Attack Phase ---");

            sbLog.AppendLine("[AutoBattlePatch] After_SetAutoCardForNonControlablePlayer - END");
            Debug.Log(sbLog.ToString());

            IsForceInjecting = false;
        }

        private static bool IsCardActuallyTargetable(BattleUnitModel caster, BattleDiceCardModel card, BattleUnitModel target)
        {
            if (card == null || target == null || target.IsDead()) return false;
            
            if (!BattleUnitModel.IsTargetableUnit(card, caster, target)) return false;

            return true; 
        }
        
        private static int EstimateCardDamage(BattleDiceCardModel card)
        {
            if (card == null || card.XmlData == null || card.XmlData.DiceBehaviourList == null) return 0;
            int totalEstimatedDamage = 0;
            foreach (var behaviourXml in card.XmlData.DiceBehaviourList) 
            {
                if (behaviourXml.Type == LOR_DiceSystem.BehaviourType.Atk) 
                {
                    totalEstimatedDamage += (behaviourXml.Min + behaviourXml.Dice) / 2; 
                }
            }
            return totalEstimatedDamage;
        }

        private static bool CanPassDamageThreshold(BattleUnitModel caster, BattleDiceCardModel card, BattleUnitModel target, Dictionary<BattleUnitModel, int> accumulatedDamage)
        {
            if (caster.cardSlotDetail == null || card == null || caster.cardSlotDetail.PlayPoint < card.GetCost()) return false;
            return true; 
        }

        public static List<BattleUnitModel> FilterUnitsForCustomAI(List<BattleUnitModel> originalList)
        {
            if (originalList == null) return new List<BattleUnitModel>();
            return originalList.Where(unit => BattleInterfaceCache.Of<ICustomCardSetter>(unit).FirstOrDefault() == null).ToList();
        }
    }
}

