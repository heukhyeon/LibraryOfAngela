using System;
using System.Collections.Generic;
using System.Linq;
using LibraryOfAngela.Interface_External;
using LOR_DiceSystem;

namespace LibraryOfAngela.Battle
{
    // Projected state only; planning never equips cards or changes live light/targets.
    internal sealed class CustomSetterOwner
    {
        public BattleUnitModel owner;
        public IHandleAutoCardUse setter;
        public List<BattleDiceCardModel> hand;
        public List<int> slots;
        public int light;
    }

    internal sealed class AutoBattleImpl
    {
        internal sealed class Plan
        {
            public BattleUnitModel owner;
            public BattleDiceCardModel card;
            public BattleUnitModel target;
            public int slot;
            public int targetSlot;
            public bool clash;
            public long priority;
        }

        private readonly Faction faction;
        private readonly Dictionary<BattleUnitModel, HashSet<int>> completed = new Dictionary<BattleUnitModel, HashSet<int>>();
        private readonly List<Plan> plans = new List<Plan>();
        private readonly Dictionary<BattleUnitModel, float> dmgDic = new Dictionary<BattleUnitModel, float>();
        private List<BattleUnitModel> units;
        private bool dirty = true;
        private bool prepared;

        internal AutoBattleImpl(Faction faction) { this.faction = faction; }
        internal static IHandleAutoCardUse Policy(BattleUnitModel unit) => BattleInterfaceCache.Of<IHandleAutoCardUse>(unit).FirstOrDefault();
        internal static bool Handles(BattleUnitModel unit) => unit != null &&
            (unit.faction != Faction.Player || !unit.IsControlable()) && Policy(unit) != null;
        private static bool CanAct(BattleUnitModel unit) => !unit.IsDead() && unit.IsActionable() &&
            unit.turnState != BattleUnitTurnState.BREAK && !unit.IsBreakLifeZero() && !unit.IsKnockout();
        internal static bool EmptySlot(BattleUnitModel unit, int slot) => slot >= 0 &&
            slot < unit.speedDiceResult.Count && slot < unit.cardSlotDetail.cardAry.Count &&
            !unit.speedDiceResult[slot].breaked && unit.cardSlotDetail.cardAry[slot] == null;
        private bool IsCompleted(BattleUnitModel owner, int slot) => completed.TryGetValue(owner, out var slots) && slots.Contains(slot);

        internal void Complete(BattleUnitModel owner, int slot)
        {
            if (!completed.TryGetValue(owner, out var slots)) completed[owner] = slots = new HashSet<int>();
            slots.Add(slot);
            // Reconcile all remaining choices from the real board, including changes
            // to other cards, costs and buffs made by OnApplyCard or other mods.
            dirty = true;
        }
        internal void Invalidate() { dirty = true; }
        internal Plan GetPlan(BattleUnitModel owner, int slot)
        {
            if (!CanAct(owner) || !EmptySlot(owner, slot) || IsCompleted(owner, slot)) return null;
            if (!prepared) { prepared = true; UseInstanceCards(); }
            if (dirty) Rebuild();
            return plans.Find(x => x.owner == owner && x.slot == slot);
        }

        private static bool Available(BattleUnitModel owner, BattleDiceCardModel card) =>
            owner.IsCardChoosable(card) && owner.CheckCardAvailableForPlayer(card);
        internal static bool CanTarget(BattleUnitModel owner, BattleDiceCardModel card, BattleUnitModel target, int slot) =>
            target != null && !target.IsDead() && slot >= 0 && slot < target.speedDiceResult.Count &&
            slot < target.cardSlotDetail.cardAry.Count &&
            (slot != target.speedDiceResult.Count - 1 || slot == 0 || target.IsTargetable_theLast()) &&
            BattleUnitModel.IsTargetableUnit(card, owner, target, slot) && target != owner &&
            // TeamKill permits friendly targeting; it is not an instruction for
            // this policy to attack allies. Only an explicit fixed target changes
            // the legacy enemy-only forecast. Native target hooks still run later.
            (target.faction != owner.faction || owner.GetFixedTargets().Contains(target));
        private static bool OrdinaryAttack(BattleDiceCardModel card)
        {
            var spec = card.GetSpec();
            return spec.Ranged != CardRange.Instance && spec.Ranged != CardRange.FarArea &&
                spec.Ranged != CardRange.FarAreaEach && spec.affection != CardAffection.TeamNear;
        }
        private static bool ExpectedClash(BattleUnitModel owner, BattleDiceCardModel card, int slot, BattleUnitModel target, int targetSlot)
        {
            var other = target.cardSlotDetail.cardAry[targetSlot];
            return other != null && OrdinaryAttack(card) && OrdinaryAttack(other.card) &&
                ((other.target == owner && other.targetSlotOrder == slot) || owner.CanChangeAttackTarget(target, slot, targetSlot));
        }
        private List<BattleUnitModel> Enemies(CustomSetterOwner state, BattleDiceCardModel card) => units.Where(x =>
            x.faction != state.owner.faction && Enumerable.Range(0, x.speedDiceResult.Count).Any(i => CanTarget(state.owner, card, x, i))).ToList();
        private List<BattleUnitModel> Allies(CustomSetterOwner state) => units.Where(x => x.faction == state.owner.faction).ToList();

        private void Rebuild()
        {
            plans.Clear();
            dmgDic.Clear();
            units = BattleObjectManager.instance.GetList().Where(x => !x.IsDead()).ToList();
            foreach (var unit in units) dmgDic[unit] = Math.Max(0, unit.hp - Math.Max(0, unit.GetMinHp()));
            // Committed choices (including vanilla/other mods) are immutable inputs.
            foreach (var unit in units.Where(x => x.faction == faction))
                foreach (var played in unit.cardSlotDetail.cardAry.Where(x => x != null))
                    if (played.target != null && dmgDic.ContainsKey(played.target))
                    {
                        var other = played.target.cardSlotDetail.cardAry.ElementAtOrDefault(played.targetSlotOrder);
                        var clash = other != null && other.target == unit && other.targetSlotOrder == played.slotOrder &&
                            OrdinaryAttack(played.card) && OrdinaryAttack(other.card);
                        dmgDic[played.target] = Math.Max(0, GetRemainHp(played.target, dmgDic[played.target], played.card, clash, out _));
                    }
            var owners = new List<CustomSetterOwner>();
            foreach (var unit in units.Where(x => x.faction == faction && Handles(x) && CanAct(x)))
                owners.Add(new CustomSetterOwner {
                    owner = unit, setter = Policy(unit), light = unit.PlayPoint - unit.cardSlotDetail.ReservedPlayPoint,
                    slots = Enumerable.Range(0, unit.speedDiceResult.Count).Where(i => EmptySlot(unit, i) && !IsCompleted(unit, i)).ToList(),
                    hand = unit.allyCardDetail.GetHand().Where(c => c.GetSpec().Ranged != CardRange.Instance && Available(unit, c)).ToList()
                });

            var claimed = new HashSet<BattlePlayingCardDataInUnitModel>();
            foreach (var owner in units.Where(x => x.faction == faction))
                foreach (var played in owner.cardSlotDetail.cardAry.Where(x => x != null && x.target != null))
                {
                    var other = played.target.cardSlotDetail.cardAry.ElementAtOrDefault(played.targetSlotOrder);
                    if (other != null && other.target == owner && other.targetSlotOrder == played.slotOrder &&
                        OrdinaryAttack(played.card) && OrdinaryAttack(other.card)) claimed.Add(other);
                }
            var attacks = units.Where(x => x.faction != faction).SelectMany(x => x.cardSlotDetail.cardAry)
                .Where(x => x != null).OrderByDescending(x => x.speedDiceResultValue).ToList();
            foreach (var attack in attacks)
            {
                if (claimed.Contains(attack)) continue;
                Plan best = null;
                CustomSetterOwner bestOwner = null;
                foreach (var state in owners)
                    foreach (var slot in state.slots)
                        foreach (var card in state.hand)
                        {
                            if (!Eligible(state, card, slot) || !CanTarget(state.owner, card, attack.owner, attack.slotOrder) ||
                                !ExpectedClash(state.owner, card, slot, attack.owner, attack.slotOrder)) continue;
                            var score = GetParryingPriority(card, state, slot, attack);
                            if (best == null || score > best.priority)
                            {
                                best = NewPlan(state, card, slot, attack.owner, attack.slotOrder, true, score);
                                bestOwner = state;
                            }
                        }
                if (best != null) { Reserve(bestOwner, best); claimed.Add(attack); }
            }
            foreach (var state in owners)
                foreach (var slot in state.slots.ToArray())
                {
                    Plan best = null;
                    // Prefer an unclaimed target, but do not leave a usable slot
                    // empty when every remaining attack would naturally redirect a
                    // clash. AddCard's resulting redirection remains authoritative.
                    for (var allowRedirect = 0; allowRedirect < 2 && best == null; allowRedirect++)
                    foreach (var card in state.hand)
                    {
                        if (!Eligible(state, card, slot)) continue;
                        foreach (var target in units)
                            for (var targetSlot = 0; targetSlot < target.speedDiceResult.Count; targetSlot++)
                            {
                                if (!CanTarget(state.owner, card, target, targetSlot)) continue;
                                var clash = ExpectedClash(state.owner, card, slot, target, targetSlot);
                                var targetCard = target.cardSlotDetail.cardAry[targetSlot];
                                if (allowRedirect == 0 && clash && claimed.Contains(targetCard)) continue;
                                var score = clash ? GetParryingPriority(card, state, slot, targetCard) :
                                    GetOneSidePriority(card, state, slot, target, targetSlot, state.owner.MaxPlayPoint, false);
                                if (best == null || score > best.priority)
                                    best = NewPlan(state, card, slot, target, targetSlot, clash, score);
                            }
                    }
                    if (best != null)
                    {
                        Reserve(state, best);
                        if (best.clash) claimed.Add(best.target.cardSlotDetail.cardAry[best.targetSlot]);
                    }
                }
            dirty = false;
        }
        private static bool Eligible(CustomSetterOwner state, BattleDiceCardModel card, int slot) => card.GetCost() <= state.light &&
            (state.owner.faction != Faction.Enemy || card.GetPriority(state.owner.GetSpeed(slot)) > -10);
        private static Plan NewPlan(CustomSetterOwner state, BattleDiceCardModel card, int slot, BattleUnitModel target, int targetSlot, bool clash, long score) =>
            new Plan { owner = state.owner, card = card, slot = slot, target = target, targetSlot = targetSlot, clash = clash, priority = score };
        private void Reserve(CustomSetterOwner state, Plan plan)
        {
            plans.Add(plan);
            state.hand.Remove(plan.card);
            state.slots.Remove(plan.slot);
            state.light -= plan.card.GetCost();
            dmgDic[plan.target] = Math.Max(0, GetRemainHp(plan.target, dmgDic[plan.target], plan.card, plan.clash, out _));
        }

        // Vanilla auto selection excludes Instance cards, so retain this small prepass.
        private void UseInstanceCards()
        {
            foreach (var owner in BattleObjectManager.instance.GetList().Where(x => x.faction == faction && Handles(x) && CanAct(x)).ToArray())
            {
                if (owner.speedDiceResult.Count == 0) continue;
                var attempted = new HashSet<BattleDiceCardModel>();
                for (var count = 0; count < 128 && CanAct(owner); count++)
                {
                    var instanceSlot = Enumerable.Range(0, owner.speedDiceResult.Count)
                        .Where(i => EmptySlot(owner, i) && !IsCompleted(owner, i)).DefaultIfEmpty(-1).First();
                    if (instanceSlot < 0) break;
                    Plan best = null;
                    var alive = BattleObjectManager.instance.GetList().Where(x => !x.IsDead()).ToList();
                    var hand = owner.allyCardDetail.GetHand().Concat(owner.personalEgoDetail._cardInHand).Distinct().ToArray();
                    foreach (var card in hand)
                    {
                        if (attempted.Contains(card) || card.GetSpec().Ranged != CardRange.Instance || !Available(owner, card)) continue;
                        var targets = alive.Where(x => BattleUnitModel.IsTargetableUnit(card, owner, x)).ToList();
                        var enemies = targets.Where(x => x.faction != faction).ToList();
                        var allies = targets.Where(x => x.faction == faction).ToList();
                        foreach (var target in enemies)
                        {
                            var score = (long)Policy(owner).GetCustomizedCardPriority(owner, card, owner.GetSpeed(instanceSlot), instanceSlot, target, 0, null,
                                false, enemies, allies) * 10000 + RandomUtil.Range(1, 5);
                            if (best == null || score > best.priority) best = new Plan { owner = owner, card = card, target = target, priority = score };
                        }
                    }
                    if (best == null) break;
                    // Effects may return a card to hand; do not recursively retry it.
                    attempted.Add(best.card);
                    var order = owner.cardOrder;
                    try
                    {
                        owner.SetCurrentOrder(instanceSlot);
                        var forcedTarget = owner.ChangeAttackTarget(best.card, instanceSlot);
                        owner.cardSlotDetail.AddCard(best.card, forcedTarget ?? best.target, 0);
                    }
                    finally { owner.SetCurrentOrder(order); }
                }
            }
        }

        private long GetParryingPriority(BattleDiceCardModel card, CustomSetterOwner owner, int myIndex, BattlePlayingCardDataInUnitModel targetCard)
        {
            var initialCustomPriority = owner.setter.GetCustomizedCardPriority(owner.owner, card, owner.owner.GetSpeed(myIndex), myIndex, targetCard.owner, targetCard.slotOrder, targetCard.card, true, Enemies(owner, card), Allies(owner));
            var priority = (long)initialCustomPriority * 100;
            var cost = card.GetCost();
            var remainPlayPoint = owner.light;


            if (cost > remainPlayPoint)
            {
                return int.MinValue;
            }

            var flag = false;
            var remainCount = owner.slots.Count;
            if (cost == 0 || remainCount == 1)
            {
                flag = true;
            }
            else
            {
                var index = owner.hand.IndexOf(card);
                for (int i = 0; i < owner.hand.Count; i++)
                {
                    if (i == index) continue;

                    if (owner.hand[i].GetCost() + cost <= remainPlayPoint)
                    {
                        flag = true;
                        break;
                    }
                }
            }

            if (!flag)
            {
                priority -= 550;
            }
            else
            {
                var delta = (owner.owner.speedDiceCount - myIndex) * 20;
                var costBonus = cost * delta;
                priority += costBonus;
            }

            var diceDiffPriorityAdjustment = GetAdjustmantPostiveRoll(card, targetCard.card);
            priority += diceDiffPriorityAdjustment;


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

        private long GetOneSidePriority(BattleDiceCardModel card, CustomSetterOwner owner, int myIndex, BattleUnitModel target, int targetIndex, int maxPlayPoint, bool forceUseEvenIfEnemyZero)
        {
            // 기본 스크립트 우선도
            var priority = (long)owner.setter.GetCustomizedCardPriority(owner.owner, card, owner.owner.GetSpeed(myIndex), myIndex, target, targetIndex, target.cardSlotDetail.cardAry[targetIndex]?.card, false, Enemies(owner, card), Allies(owner)) * 100;
            var cost = card.GetCost();
            var remainPlayPoint = owner.light;

            if (cost > remainPlayPoint) return int.MinValue;


            // 이미 죽었을것같은 대상이라면 건너뛰는걸 고려한다. 단, 그 막에 아예 주사위가 비려고 한다면 죽을거같은 여부를 생략한다.
            var currentRemainHp = dmgDic[target];
            if (currentRemainHp <= 0f && !forceUseEvenIfEnemyZero)
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
            if (owner.slots.Count == 1 || cost == 0)
            {
                flag = true;
            }
            else
            {
                var index = owner.hand.IndexOf(card);
                for (int i = 0; i < owner.hand.Count; i++)
                {
                    if (i != index && owner.hand[i].GetCost() + cost <= remainPlayPoint)
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

    }
}
