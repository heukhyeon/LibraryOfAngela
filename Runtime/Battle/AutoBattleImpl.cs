using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LibraryOfAngela.Interface_External;
using LOR_DiceSystem; // CardRange, BehaviourType 등을 위해 추가

namespace LibraryOfAngela.Battle
{
    class ParryableTarget
    {
        public CustomSetterOwner owner;
        public int index;
    }


    class CustomSetterOwner
    {
        public BattleUnitModel owner;
        public ICustomCardSetter setter;
    }

    class PriorityInfo
    {
        public BattleUnitModel target;
        public BattleDiceCardModel card;
        public int index;
        public int priority;
    }


    class AutoBattleImpl
    {
        enum LoggingLevel
        {
            None,
            Simple,
            Full,
        }

        private LoggingLevel level = LoggingLevel.Full;
        private StringBuilder totalLogger = new StringBuilder();
        private List<CustomSetterOwner> allyOwners = new List<CustomSetterOwner>();
        private List<BattlePlayingCardDataInUnitModel> enemyCards = new List<BattlePlayingCardDataInUnitModel>();
        private Dictionary<BattlePlayingCardDataInUnitModel, List<ParryableTarget>> parryTargets = new Dictionary<BattlePlayingCardDataInUnitModel, List<ParryableTarget>>();
        private Dictionary<BattleUnitModel, float> dmgDic = new Dictionary<BattleUnitModel, float>();

        public void Execute()
        {
            totalLogger = new StringBuilder("전투 매칭 로깅\n");
            foreach (var card in GetEnemyCards())
            {
                enemyCards.Add(card);
                // 롤랑 같은 경우처럼 마지막 다이스에만 지정불가가 걸릴때 그것도 합하는 경우가 있음
                if (!card.owner.view.speedDiceSetterUI.GetSpeedDiceByIndex(card.slotOrder).CheckBlockDice())
                {
                    parryTargets[card] = new List<ParryableTarget>();
                }
            }

            allyOwners.AddRange(GetCustomSetterOwners());

            // 아군 제어가능 아군이 없을땐 판단 생략
            if (allyOwners.Count == 0) return;

            foreach (var allyOwner in allyOwners)
            {
                ExecuteInstance(allyOwner, allyOwner.owner.allyCardDetail._cardInHand);
                ExecuteInstance(allyOwner, allyOwner.owner.personalEgoDetail._cardInHand);
            }

            ExecuteParrying();

            ExecuteOneSide();

            if (level >= LoggingLevel.Simple)
            {
                Logger.Log(totalLogger.ToString());
            }
        }

        /// <summary>
        /// 현재 활동중인 적들이 사용하는 전체 카드의 정보
        /// </summary>
        /// <returns></returns>
        private IEnumerable<BattlePlayingCardDataInUnitModel> GetEnemyCards()
        {
            foreach (var enemy in BattleObjectManager.instance.GetList())
            {
                if (enemy.IsDead() || enemy.faction == Faction.Player) continue;
                foreach (var card in enemy.cardSlotDetail.cardAry)
                {
                    if (card != null) yield return card;
                }
                // 남은 hp
                var minHp = enemy.GetMinHp();
                if (minHp < 0) minHp = 0;
                dmgDic[enemy] = enemy.hp - minHp;
            }
        }

        /// <summary>
        /// <see cref="ICustomCardSetter"/> 를 보유한 사서 목록을 조회하고, 각 사서의 속도 주사위가 어떤 책장과 합할수 있는지를 처리
        /// </summary>
        /// <returns></returns>
        private IEnumerable<CustomSetterOwner> GetCustomSetterOwners()
        {
            foreach (var unit in BattleObjectManager.instance.GetList())
            {
                if (unit.faction == Faction.Enemy || unit.turnState == BattleUnitTurnState.BREAK || unit.IsDead() || !unit.IsActionable() || unit.IsControlable()) continue;
                var setter = BattleInterfaceCache.Of<ICustomCardSetter>(unit).FirstOrDefault();
                if (setter is null) continue;
                var owner = new CustomSetterOwner { owner = unit, setter = setter };
                for (int i = 0; i < unit.speedDiceCount; i++)
                {
                    foreach (var card in enemyCards)
                    {
                        if (!parryTargets.ContainsKey(card)) continue;
                        if (!card.owner.IsTargetable(unit)) continue;

                        if (card.target == unit && card.targetSlotOrder == i)
                        {
                            parryTargets[card].Add(new ParryableTarget { owner = owner, index = i });
                        }
                        else if (unit.CanChangeAttackTarget(card.owner, i, card.slotOrder))
                        {
                            parryTargets[card].Add(new ParryableTarget { owner = owner, index = i });
                        }
                    }
                }
                yield return owner;
            }
        }
    
        /// <summary>
        /// 손에 있는 장착시 발동 책장을 모두 사용한다. 한장이라도 사용했다면 다시 호출해서 추가 발동이 있는지 확인한다.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        private void ExecuteInstance(CustomSetterOwner owner, List<BattleDiceCardModel> cards)
        {
            List<PriorityInfo> priorities = new List<PriorityInfo>();
            var speed = owner.owner.GetSpeed(0);

            foreach (var card in cards)
            {
                if (card._script?.OnChooseCard(owner.owner) == false) continue;
                if (card.GetSpec().Ranged != CardRange.Instance) continue;
                if (card.GetCost() > owner.owner.PlayPoint) continue;

                var targetableAllys = new List<BattleUnitModel>();
                var targetableEnemys = new List<BattleUnitModel>();

                foreach (var unit in BattleObjectManager.instance.GetList())
                {
                    if (unit.IsDead()) continue;
                    if (!BattleUnitModel.IsTargetableUnit(card, owner.owner, unit)) continue;
                    if (unit.faction != owner.owner.faction) targetableEnemys.Add(unit);
                    else targetableAllys.Add(unit);
                }

                foreach (var unit in targetableEnemys)
                {
                    var priority = owner.setter.GetCustomizedCardPriority(owner.owner, card, speed, 0, unit, 0, null, false, targetableEnemys, targetableAllys);
                    priorities.Add(new PriorityInfo { target = unit, card = card, priority = priority });
                }
            }

            if (priorities.Count > 0)
            {
                var target = priorities.OrderByDescending(d => (d.priority * 10000) + RandomUtil.Range(1, 5)).FirstOrDefault();
                owner.owner.SetCurrentOrder(0);
                owner.owner.cardSlotDetail.AddCard(target.card, target.target, 0);
                ExecuteInstance(owner, cards);
                return;
            }
        }
    
        /// <summary>
        /// 합 처리
        /// </summary>
        private void ExecuteParrying()
        {
            PriorityInfo current = new PriorityInfo();

            var keys = parryTargets.Keys.OrderByDescending(d => d.speedDiceResultValue).ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                var targetCard = keys[i];
                foreach (var parry in parryTargets[targetCard])
                {
                    foreach (var card in parry.owner.owner.allyCardDetail._cardInHand)
                    {
                        var priority = GetParryingPriority(card, parry.owner, parry.index, targetCard);
                        if (current.card is null || priority > current.priority)
                        {
                            current = new PriorityInfo { card = card, priority = priority, target = targetCard.owner, index = parry.index };
                        }
                    }
                }
                if (current.card is null)
                {
                    totalLogger.AppendLine($"대상 합 불가 : {targetCard.owner.index}.{targetCard.owner.UnitData.unitData.name}->{targetCard.slotOrder}:{targetCard.card.GetName()}");
                }
                else
                {
                    current.card.owner.SetCurrentOrder(current.index);
                    current.card.owner.cardSlotDetail.AddCard(current.card, targetCard.owner, targetCard.slotOrder);
                    totalLogger.AppendLine($"대상 합 실행 : {targetCard.owner.index}.{targetCard.owner.UnitData.unitData.name}->{targetCard.slotOrder}:{targetCard.card.GetName()} vs {current.card.owner.index}.{current.card.owner.UnitData.unitData.name}->{current.index}:{current.card.GetName()}");
                    parryTargets.Remove(targetCard);
                    var nextHp = GetRemainHp(targetCard.owner, dmgDic[targetCard.owner], current.card, true, out int overusedDice);
                    if (nextHp <= 0) nextHp = 0;
                    dmgDic[targetCard.owner] = nextHp;

                    for (int j = i + 1; j < keys.Count; j++)
                    {
                        parryTargets[keys[j]].RemoveAll(target => target.index == current.index && target.owner.owner == current.card.owner);
                    }

                }
                current = new PriorityInfo();
            }
        }

        /// <summary>
        /// 합 이후 일방공격 처리
        /// </summary>
        private void ExecuteOneSide()
        {
            foreach (var ally in allyOwners)
            {
                for (int i = 0; i < ally.owner.speedDiceCount; i++)
                {
                    if (ally.owner.cardSlotDetail.cardAry[i] != null || ally.owner.speedDiceResult[i].breaked) continue;
                    PriorityInfo current = new PriorityInfo();
                    current.priority = 0;

                    // 실제로는 책장이 지정되지 않은 슬롯이라던가 그런걸 감안해야겠지만 일단 0으로 고정
                    var targetSlotOrder = 0;
                    // 타겟이 불가능한데도 공격할수 있음
                    var targetables = dmgDic.Keys.Where(d => d.IsTargetable(ally.owner)).ToList();

                    var maxPlayPoint = ally.owner.MaxPlayPoint;
                    foreach (var card in ally.owner.allyCardDetail._cardInHand)
                    {
                        foreach (var enemy in targetables)
                        {
                            var p = GetOneSidePriority(card, ally, i, enemy, targetSlotOrder, maxPlayPoint);
                            if (current.priority < p)
                            {
                                current = new PriorityInfo { target = enemy, index = i, priority = p, card = card };
                            }
                        }
                    }
                    if (current.card != null)
                    {
                        var targetCard = current.card;
                        BattleUnitModel originTarget = null;
                        int originTargetSlotOrder = 0;
                        var originTargetCard = current.target.cardSlotDetail.cardAry[targetSlotOrder];
                        if (originTargetCard != null)
                        {
                            originTarget = originTargetCard.target;
                            originTargetSlotOrder = originTargetCard.targetSlotOrder;
                        }
                        ally.owner.SetCurrentOrder(i);
                        ally.owner.cardSlotDetail.AddCard(current.card, current.target, targetSlotOrder);
                        if (originTargetCard != null)
                        {
                            originTargetCard.target = originTarget;
                            originTargetCard.targetSlotOrder = originTargetSlotOrder;
                        }

                        var currentHp = dmgDic[current.target];
                        var nextHp = GetRemainHp(current.target, currentHp, current.card, true, out int overusedDice);
                        if (nextHp <= 0) nextHp = 0;
                        totalLogger.AppendLine($"대상 일방 공격 : {targetCard.owner.index}.{targetCard.owner.UnitData.unitData.name}->{i}번 슬롯. {current.card.GetName()} --> {current.target.index}.{current.target.UnitData.unitData.name}.{targetSlotOrder}번 슬롯, Hp 변동 예상 : {currentHp} --> {nextHp}");
                        dmgDic[current.target] = nextHp;
                    }
                    else
                    {
                        totalLogger.AppendLine($"대상 일방 공격 스킵 : {ally.owner.index}.{ally.owner.UnitData.unitData.name}->{i}번 슬롯");
                    }
                }
            }
        }


        private int GetParryingPriority(BattleDiceCardModel card, CustomSetterOwner owner, int myIndex, BattlePlayingCardDataInUnitModel targetCard)
        {
            var initialCustomPriority = owner.setter.GetCustomizedCardPriority(owner.owner, card, owner.owner.GetSpeed(myIndex), myIndex, targetCard.owner, targetCard.slotOrder, targetCard.card, true, null, null);
            var priority = initialCustomPriority * 100;
            var cost = card.GetCost();
            var remainPlayPoint = owner.owner.PlayPoint - owner.owner.cardSlotDetail.ReservedPlayPoint;

            // 상세 로깅 시작
            LoggingIfFull($"  [GetParryingPriority] 카드: {card.GetName()} ({owner.owner.UnitData.unitData.name} S{myIndex}) vs {targetCard.card.GetName()} ({targetCard.owner.UnitData.unitData.name} S{targetCard.slotOrder})");
            LoggingIfFull($"    - 초기 커스텀 우선도: {initialCustomPriority} (적용값: {priority})");
            LoggingIfFull($"    - 카드 비용: {cost}, 남은 빛: {remainPlayPoint}");

            if (cost > remainPlayPoint)
            {
                LoggingIfFull($"    - 빛 부족! 최종 우선도: -10000 (조기 반환)");
                return -10000;
            }

            var flag = false;
            var lightLogicReason = "";
            var remainCount = owner.owner.speedDiceCount - owner.owner.cardSlotDetail.cardAry.Count(d => d != null);
            if (cost == 0 || remainCount == 1)
            {
                flag = true;
                lightLogicReason = remainCount == 1 ? "마지막 슬롯" : "0코스트 카드";
            }
            else
            {
                var index = owner.owner.allyCardDetail._cardInHand.IndexOf(card);
                bool foundNextCard = false;
                for (int i = 0; i < owner.owner.allyCardDetail._cardInHand.Count; i++)
                {
                    if (i == index) continue;

                    if (owner.owner.allyCardDetail._cardInHand[i].GetCost() + cost <= remainPlayPoint)
                    {
                        flag = true;
                        foundNextCard = true;
                        lightLogicReason = $"다음 카드 ({owner.owner.allyCardDetail._cardInHand[i].GetName()}) 사용 가능";
                        break;
                    }
                }
                if (!foundNextCard) lightLogicReason = "다음 카드 사용 불가능";
            }
            LoggingIfFull($"    - 빛 관리 flag: {flag} (사유: {lightLogicReason})");

            var lightPriorityAdjustment = 0;
            if (!flag)
            {
                priority -= 550;
                lightPriorityAdjustment = -550;
            }
            else
            {
                var delta = (owner.owner.speedDiceCount - myIndex) * 20;
                var costBonus = cost * delta;
                priority += costBonus;
                lightPriorityAdjustment = costBonus;
                LoggingIfFull($"    - Delta 값: {delta}");
            }
            LoggingIfFull($"    - 빛 관리로 인한 우선도 변경: {lightPriorityAdjustment}");

            var diceDiffPriorityAdjustment = GetAdjustmantPostiveRoll(card, targetCard.card);
            priority += diceDiffPriorityAdjustment;

            LoggingIfFull($"    - (우선도 변경: {diceDiffPriorityAdjustment})");
            LoggingIfFull($"    - 최종 우선도 (랜덤 제외): {priority}");

            return priority + RandomUtil.Range(1, 5);
        }

        /// <summary>
        /// 합 승률에 따른 우선도 처리
        /// </summary>
        /// <param name="my"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private int GetAdjustmantPostiveRoll(BattleDiceCardModel my, BattleDiceCardModel target)
        {
            var myDices = my.XmlData.DiceBehaviourList.Where(d => d.Type != BehaviourType.Standby).ToList();
            var targetDices = target.XmlData.DiceBehaviourList.Where(d => d.Type != BehaviourType.Standby).ToList();
            var max = myDices.Count > targetDices.Count ?
                myDices.Count : targetDices.Count;

            var priority = 0;

            for (int i = 0; i< max; i++)
            {
                // 내가 일방을 당할 상황이면 우선도 감소
                if (myDices.Count <= i && targetDices.Count > i && targetDices[i].Type == BehaviourType.Atk) priority -= 80;
                // 내가 일방을 할 상황이면 우선도 증가
                if (targetDices.Count <= i && myDices.Count > i && myDices[i].Type == BehaviourType.Atk) priority += 80;
                
                // 둘다 이 인덱스에서 주사위를 굴릴 수 있을때
                if (myDices.Count > i && targetDices.Count > i)
                {
                    var myValue = (myDices[i].Min + myDices[i].Dice) / 2;
                    var targetValue = (targetDices[i].Min + targetDices[i].Dice) / 2;

                    // 내가 이길거같으면 우선도 증가
                    if (myValue > targetValue) priority += 30;
                    // 내가 질거같으면 우선도 감소
                    else if (myValue < targetValue) priority -= 30;
                    // 비길거같은데 나랑 상대 둘다 가드라 감정 못벌거같으면 우선도 감소
                    else if (myDices[i].Detail == BehaviourDetail.Guard || myDices[i].Detail == BehaviourDetail.Guard) priority -= 30;
                } 
            }

            return priority;
        }

        private int GetOneSidePriority(BattleDiceCardModel card, CustomSetterOwner owner, int myIndex, BattleUnitModel target, int targetIndex, int maxPlayPoint)
        {
            // 기본 스크립트 우선도
            var priority = owner.setter.GetCustomizedCardPriority(owner.owner, card, owner.owner.GetSpeed(myIndex), myIndex, target, targetIndex, null, false, null, null) * 100;
            var cost = card.GetCost();
            var remainPlayPoint = owner.owner.PlayPoint - owner.owner.cardSlotDetail.ReservedPlayPoint;

            if (cost > remainPlayPoint) return -10000;


            // 이미 죽었을것같은 대상이라면
            var currentRemainHp = dmgDic[target];
            if (currentRemainHp <= 0f)
            {
                // 소모된 빛이 2 미만이라면 그냥 책장을 사용하지 않는다.
                // 이게 0, 1번째여도 무시한다.
                if (maxPlayPoint - remainPlayPoint < 2 || myIndex <= 1) return -10000;
 
                var keywords = BattleCardAbilityDescXmlList.Instance.GetAbilityKeywords_byScript(card.XmlData.Script);
                foreach (var sc in keywords)
                {
                    if (sc == "Energy_Keyword")
                    {
                        // 다른 괜찮은 선택지가 있다면 바로 대체될 정도의 우선도 지정
                        return 10;
                    }
                }
                // 빛 회복도 없다면 딱히 쓸 이유가 없다
                return -10000;
            }

            var flag = false;
            // 마지막 속도 주사위 슬롯이라면 비용을 특별히 고려하지 않는다.
            if (myIndex == owner.owner.speedDiceCount - 1 || cost == 0)
            {
                flag = true;
            }
            else
            {
                var index = owner.owner.allyCardDetail._cardInHand.IndexOf(card);
                for (int i = index + 1; i < owner.owner.allyCardDetail._cardInHand.Count; i++)
                {
                    if (owner.owner.allyCardDetail._cardInHand[i].GetCost() + cost < remainPlayPoint)
                    {
                        flag = true;
                        break;
                    }
                }
            }

            // 이 책장 사용시 다른 책장을 사용하지 못하는 경우,
            if (!flag)
            {
                priority -= 150;
            }
            // 책장 사용해도 다른 책장을 사용할수 있다면 비용만큼 가산점을 붙인다.
            else
            {
                var delta = (owner.owner.speedDiceCount - myIndex) * 20;
                priority += (cost * delta);
            }

            // 감정 레벨이 3부터는 슬슬 빛이 쪼들리므로 여기서부터는 후발 사용에 빛 가산점을 준다.
            if (owner.owner.emotionDetail.EmotionLevel >= 3 && !string.IsNullOrEmpty(card.XmlData.Script))
            {
                var keywords = BattleCardAbilityDescXmlList.Instance.GetAbilityKeywords_byScript(card.XmlData.Script);
                foreach (var sc in keywords)
                {
                    if (sc == "Energy_Keyword")
                    {
                        // 이미 빛을 너무 많이 썻다면 가산점
                        if (maxPlayPoint - owner.owner.PlayPoint >= 5)
                        {
                            priority += 80;
                        }
                        // 후발 주사위라면 가산점
                        if (myIndex >= 2)
                        {
                            priority += 50;
                        }
                    }
                    else if (sc == "DrawCard_Keyword")
                    {
                        // 후발 주사위라면 가산점
                        if (myIndex >= 2)
                        {
                            priority += 50;
                        }
                    }
                }
            }

        
            var expectedRemainHp = GetRemainHp(target, currentRemainHp, card, false, out int overusedDice);

            // 주사위가 낭비될것같다면 우선도를 감소시킨다.
            if (overusedDice > 0)
            {
                priority -= (overusedDice) * 50;
            }

            // 이걸로 킬을 딸 수 있을것같다면 우선도를 증가시킨다.
            if (currentRemainHp > 0f && expectedRemainHp <= 0f)
            {
                priority += 120;
            }

            // 체력이 60 이하라면 죽일수 있는 상황이라 판단하고 죽이는거에 가산점을 둔다.
            else if (currentRemainHp <= 60f)
            {
                var dmg = currentRemainHp - expectedRemainHp;
                priority += (int)(dmg * 2);
            }


            return priority + RandomUtil.Range(1, 5);
        }

        /// <summary>
        /// 현재 책장의 공격 주사위를 모두 맞았을때를 기준으로 대상의 남은 체력을 계산한다.
        /// 주사위 값은 중간값으로 계산한다.
        /// </summary>
        /// <param name="currentHp"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        private float GetRemainHp(BattleUnitModel target, float currentHp, BattleDiceCardModel card, bool isParry, out int overusedDice)
        {
            overusedDice = 0;
            foreach (var b in card.XmlData.DiceBehaviourList)
            {
                
                if (b.Type == BehaviourType.Atk)
                {
                    if (currentHp <= 0)
                    {
                        overusedDice++;
                    }
                    var rate = BookModel.GetResistRate(target.GetResistHP(b.Detail));
                    // 합이라면 적당히 못때릴 가능성을 감안해 비율을 약화시킨다.
                    if (isParry) rate *= 0.7f;
                    currentHp -= ((b.Min + b.Dice) / 2) * rate;
                }
            }
            return currentHp;
        }

        private void LoggingIfFull(string log)
        {
            if (level < LoggingLevel.Full) return;
            totalLogger.AppendLine(log);
        }
    }
}
