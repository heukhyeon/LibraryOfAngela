using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LibraryOfAngela.Interface_External;

namespace LibraryOfAngela.Battle
{
    class AutoBattleImpl
    {
        // 계획된 행동을 나타내는 클래스
        private class PlannedAction
        {
            public BattleDiceCardModel Card { get; set; }
            public BattleUnitModel Target { get; set; }
            public int TargetSlotIndex { get; set; }
            public int CasterSlotIndex { get; set; }
            public int Priority { get; set; }
            public bool IsClash { get; set; }
        }

        // 행동 세트와 그 총 우선순위
        private class ActionSet
        {
            public List<PlannedAction> Actions { get; set; } = new List<PlannedAction>();
            public int TotalPriority { get; set; }
            public int RemainingLight { get; set; }
        }

        public static void Execute()
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("=== 커스텀 AI 자동 전투 시작 ===");
            
            try
            {
                // 1. 대상 유닛 수집
                var customAIUnits = GetCustomAIUnits(logBuilder);
                if (customAIUnits.Count == 0)
                {
                    logBuilder.AppendLine("커스텀 AI 유닛이 없음");
                    return;
                }

                logBuilder.AppendLine($"커스텀 AI 유닛 수: {customAIUnits.Count}");

                // 2. 각 유닛에 대한 최적 행동 계획 수립
                var allPlannedActions = new Dictionary<BattleUnitModel, ActionSet>();
                
                foreach (var unit in customAIUnits)
                {
                    logBuilder.AppendLine($"\n[{unit.UnitData.unitData.name}] 행동 계획 수립 중...");
                    var bestActionSet = FindBestActionSetForUnit(unit, logBuilder);
                    
                    if (bestActionSet != null && bestActionSet.Actions.Count > 0)
                    {
                        allPlannedActions[unit] = bestActionSet;
                        logBuilder.AppendLine($"  - 계획된 행동 수: {bestActionSet.Actions.Count}, 총 우선순위: {bestActionSet.TotalPriority}");
                    }
                    else
                    {
                        logBuilder.AppendLine($"  - 사용 가능한 행동 없음");
                    }
                }

                // 3. 계획된 행동들을 실제로 실행
                logBuilder.AppendLine("\n=== 행동 실행 단계 ===");
                
                foreach (var kvp in allPlannedActions)
                {
                    var unit = kvp.Key;
                    var actionSet = kvp.Value;
                    
                    logBuilder.AppendLine($"\n[{unit.UnitData.unitData.name}] 행동 실행:");
                    
                    foreach (var action in actionSet.Actions.OrderBy(a => a.CasterSlotIndex))
                    {
                        ExecutePlannedAction(unit, action, logBuilder);
                    }
                }
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine($"\n오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                logBuilder.AppendLine("\n=== 커스텀 AI 자동 전투 종료 ===");
                Debug.Log(logBuilder.ToString());
            }
        }

        // 커스텀 AI 유닛들을 찾아 반환
        private static List<BattleUnitModel> GetCustomAIUnits(StringBuilder log)
        {
            var result = new List<BattleUnitModel>();
            var allUnits = BattleObjectManager.instance.GetAliveList(false);
            
            foreach (var unit in allUnits)
            {
                if (!unit.IsActionable())
                    continue;
                    
                // 플레이어가 직접 조작하는 유닛은 제외
                if (unit.faction == Faction.Player && unit.IsControlable())
                    continue;
                
                // ICustomCardSetter 인터페이스를 가진 PassiveAbility가 있는지 확인
                if (BattleInterfaceCache.Of<ICustomCardSetter>(unit).Any())
                {
                    result.Add(unit);
                    log.AppendLine($"  - {unit.UnitData.unitData.name} (Faction: {unit.faction})");
                }
            }
            
            return result;
        }

        // 유닛에 대한 최적의 행동 세트를 찾음
        private static ActionSet FindBestActionSetForUnit(BattleUnitModel unit, StringBuilder log)
        {
            // 사용 가능한 카드들 수집
            var availableCards = new List<BattleDiceCardModel>();
            availableCards.AddRange(unit.allyCardDetail.GetHand());
            availableCards.AddRange(unit.personalEgoDetail.GetHand());
            
            // 사용 가능한 속도 주사위 슬롯 확인
            var availableSlots = new List<int>();
            for (int i = 0; i < unit.speedDiceResult.Count; i++)
            {
                if (!unit.speedDiceResult[i].breaked)
                {
                    availableSlots.Add(i);
                }
            }
            
            log.AppendLine($"  - 사용 가능한 카드: {availableCards.Count}장");
            log.AppendLine($"  - 사용 가능한 슬롯: {availableSlots.Count}개");
            log.AppendLine($"  - 현재 빛: {unit.cardSlotDetail.PlayPoint}");
            
            if (availableCards.Count == 0 || availableSlots.Count == 0)
            {
                return null;
            }
            
            // 백트래킹을 사용하여 최적의 조합 찾기
            var bestActionSet = new ActionSet();
            var currentActionSet = new ActionSet { RemainingLight = unit.cardSlotDetail.PlayPoint };
            var usedCards = new HashSet<BattleDiceCardModel>(new CardReferenceComparer());
            
            FindBestCombination(
                unit, 
                availableCards, 
                availableSlots, 
                0, // 현재 슬롯 인덱스
                currentActionSet, 
                bestActionSet, 
                usedCards,
                log
            );
            
            return bestActionSet;
        }

        // 백트래킹을 사용한 최적 조합 탐색
        private static void FindBestCombination(
            BattleUnitModel unit,
            List<BattleDiceCardModel> availableCards,
            List<int> availableSlots,
            int slotIndex,
            ActionSet currentSet,
            ActionSet bestSet,
            HashSet<BattleDiceCardModel> usedCards,
            StringBuilder log)
        {
            // 모든 슬롯을 확인했거나 더 이상 사용할 카드가 없으면 종료
            if (slotIndex >= availableSlots.Count || currentSet.RemainingLight <= 0)
            {
                if (currentSet.TotalPriority > bestSet.TotalPriority)
                {
                    bestSet.Actions.Clear();
                    bestSet.Actions.AddRange(currentSet.Actions);
                    bestSet.TotalPriority = currentSet.TotalPriority;
                    bestSet.RemainingLight = currentSet.RemainingLight;
                }
                return;
            }
            
            var currentSlot = availableSlots[slotIndex];
            var allPossibleActions = new List<PlannedAction>();
            
            // 가능한 모든 행동 생성
            foreach (var card in availableCards)
            {
                if (usedCards.Contains(card))
                    continue;
                    
                if (!CanUseCard(unit, card, currentSet.RemainingLight))
                    continue;
                
                // 모든 가능한 대상에 대해 행동 생성
                var possibleTargets = GetPossibleTargets(unit, card);
                
                foreach (var targetInfo in possibleTargets)
                {
                    var action = CreatePlannedAction(
                        unit, 
                        card, 
                        currentSlot, 
                        targetInfo.Target, 
                        targetInfo.SlotIndex
                    );
                    
                    if (action != null)
                    {
                        allPossibleActions.Add(action);
                    }
                }
            }
            
            // 우선순위로 정렬
            allPossibleActions.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // 각 행동에 대해 시도
            foreach (var action in allPossibleActions)
            {
                // 행동 추가
                currentSet.Actions.Add(action);
                currentSet.TotalPriority += action.Priority;
                currentSet.RemainingLight -= action.Card.GetCost();
                usedCards.Add(action.Card);
                
                // 다음 슬롯으로 재귀 호출
                FindBestCombination(
                    unit, 
                    availableCards, 
                    availableSlots, 
                    slotIndex + 1, 
                    currentSet, 
                    bestSet, 
                    usedCards,
                    log
                );
                
                // 행동 제거 (백트래킹)
                currentSet.Actions.RemoveAt(currentSet.Actions.Count - 1);
                currentSet.TotalPriority -= action.Priority;
                currentSet.RemainingLight += action.Card.GetCost();
                usedCards.Remove(action.Card);
            }
            
            // 이 슬롯을 건너뛰는 경우도 고려
            FindBestCombination(
                unit, 
                availableCards, 
                availableSlots, 
                slotIndex + 1, 
                currentSet, 
                bestSet, 
                usedCards,
                log
            );
        }

        // 카드 사용 가능 여부 확인
        private static bool CanUseCard(BattleUnitModel unit, BattleDiceCardModel card, int remainingLight)
        {
            if (card.GetCost() > remainingLight)
                return false;
                
            if (!unit.CheckCardAvailable(card))
                return false;
                
            if (card.XmlData.IsPersonal() && !card.CanAddedEgoCard())
                return false;
                
            return true;
        }

        // 가능한 대상들 반환
        private static List<(BattleUnitModel Target, int SlotIndex)> GetPossibleTargets(
            BattleUnitModel caster, 
            BattleDiceCardModel card)
        {
            var result = new List<(BattleUnitModel, int)>();
            var targetFaction = caster.faction == Faction.Player ? Faction.Enemy : Faction.Player;
            var possibleTargets = BattleObjectManager.instance.GetAliveList(targetFaction);
            
            foreach (var target in possibleTargets)
            {
                if (!BattleUnitModel.IsTargetableUnit(card, caster, target))
                    continue;
                    
                for (int slot = 0; slot < target.speedDiceResult.Count; slot++)
                {
                    if (!target.speedDiceResult[slot].breaked)
                    {
                        result.Add((target, slot));
                    }
                }
            }
            
            return result;
        }

        // PlannedAction 생성 및 우선순위 계산
        private static PlannedAction CreatePlannedAction(
            BattleUnitModel caster,
            BattleDiceCardModel card,
            int casterSlot,
            BattleUnitModel target,
            int targetSlot)
        {
            // 대상의 해당 슬롯에 사용된 카드 확인
            BattleDiceCardModel targetCard = null;
            bool isClash = false;
            
            if (targetSlot < target.cardSlotDetail.cardAry.Count)
            {
                var targetCardData = target.cardSlotDetail.cardAry[targetSlot];
                if (targetCardData != null)
                {
                    targetCard = targetCardData.card;
                    // 합 여부 확인: 상대가 나를 대상으로 하고 있는지
                    isClash = targetCardData.target == caster && 
                             targetCardData.targetSlotOrder == casterSlot;
                }
            }
            
            // ICustomCardSetter를 통해 우선순위 계산
            var customSetters = BattleInterfaceCache.Of<ICustomCardSetter>(caster);
            if (!customSetters.Any())
                return null;
                
            var priority = 0;
            var allEnemies = BattleObjectManager.instance.GetAliveList(
                caster.faction == Faction.Player ? Faction.Enemy : Faction.Player
            );
            var allAllies = BattleObjectManager.instance.GetAliveList(caster.faction);
            
            foreach (var setter in customSetters)
            {
                priority += setter.GetCustomizedCardPriority(
                    caster,
                    card,
                    caster.speedDiceResult[casterSlot].value,
                    casterSlot,
                    target,
                    targetSlot,
                    targetCard,
                    isClash,
                    allEnemies,
                    allAllies
                );
            }
            
            return new PlannedAction
            {
                Card = card,
                Target = target,
                TargetSlotIndex = targetSlot,
                CasterSlotIndex = casterSlot,
                Priority = priority,
                IsClash = isClash
            };
        }

        // 계획된 행동 실행
        private static void ExecutePlannedAction(
            BattleUnitModel caster, 
            PlannedAction action, 
            StringBuilder log)
        {
            // 최종 검증
            if (!CanUseCard(caster, action.Card, caster.cardSlotDetail.PlayPoint))
            {
                log.AppendLine($"  X 슬롯 {action.CasterSlotIndex}: {action.Card.GetName()} - 사용 불가 (빛 부족 또는 제약)");
                return;
            }
            
            if (!BattleUnitModel.IsTargetableUnit(action.Card, caster, action.Target))
            {
                log.AppendLine($"  X 슬롯 {action.CasterSlotIndex}: {action.Card.GetName()} - 대상 지정 불가");
                return;
            }
            
            // cardOrder 설정
            caster.cardOrder = action.CasterSlotIndex;
            
            // 카드 사용
            caster.cardSlotDetail.AddCard(
                action.Card,
                action.Target,
                action.TargetSlotIndex,
                caster.faction == Faction.Enemy
            );
            
            log.AppendLine($"  O 슬롯 {action.CasterSlotIndex}: {action.Card.GetName()} " +
                          $"→ {action.Target.UnitData.unitData.name}의 슬롯 {action.TargetSlotIndex} " +
                          $"(우선순위: {action.Priority}, {(action.IsClash ? "합" : "일방공격")})");
        }

        // 카드 참조 비교를 위한 커스텀 Comparer
        private class CardReferenceComparer : IEqualityComparer<BattleDiceCardModel>
        {
            public bool Equals(BattleDiceCardModel x, BattleDiceCardModel y)
            {
                return ReferenceEquals(x, y);
            }
            
            public int GetHashCode(BattleDiceCardModel obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}
