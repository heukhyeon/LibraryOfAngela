// A small executable model of the native selection pipeline. Production planner
// and Harmony patches are compiled unchanged; no Unity player is required.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LibraryOfAngela.Interface_External;
using LOR_DiceSystem;
namespace LOR_DiceSystem
{
    public enum CardRange { Near, FarArea, FarAreaEach, Instance }
    public enum CardAffection { One, TeamNear }
    public enum BehaviourType { Atk, Def, Standby }
    public enum BehaviourDetail { Slash, Guard }
    public class DiceBehaviour { public BehaviourType Type; public BehaviourDetail Detail; public int Min = 5, Dice = 5; }
}
namespace LibraryOfAngela.Interface_External { public interface ILoABattleEffect { } }
namespace LibraryOfAngela.Battle
{
    internal static class BattleInterfaceCache
    {
        public static IEnumerable<T> Of<T>(BattleUnitModel unit) => unit.Policy == null ? Enumerable.Empty<T>() : new[] { (T)(object)unit.Policy };
    }
}
public enum Faction { Enemy, Player }
public enum BattleUnitTurnState { WAIT_CARD, BREAK }
public sealed class CardSpec { public CardRange Ranged; public CardAffection affection; public int emotionLimit; }
public sealed class CardXml { public List<DiceBehaviour> DiceBehaviourList = new List<DiceBehaviour> { new DiceBehaviour() }; public string Script = ""; }
public sealed class BattleDiceCardModel
{
    public string name; public int cost = 1, priority; public bool choosable = true, available = true;
    public bool allyOnly; public Action<BattleUnitModel> instance;
    public CardSpec spec = new CardSpec(); public CardXml XmlData = new CardXml();
    public BattleDiceCardModel(string name) { this.name = name; }
    public int GetCost() => cost;
    public CardSpec GetSpec() => spec;
    public int GetPriority(int speed = 0) => priority;
    public string GetName() => name;
}
public sealed class SpeedDice { public int value = 5; public bool breaked; }
public sealed class Emotion { public int EmotionLevel = 5; }
public sealed class UnitInfo { public UnitName unitData = new UnitName(); }
public sealed class UnitName { public string name = "test"; }
public sealed class PersonalCards { public List<BattleDiceCardModel> _cardInHand = new List<BattleDiceCardModel>(); }
public sealed class BattlePlayingCardDataInUnitModel
{
    public BattleUnitModel owner, target; public BattleDiceCardModel card;
    public int slotOrder, targetSlotOrder, speedDiceResultValue;
}
public sealed class BattleUnitModel
{
    public Faction faction; public bool controlled, dead, actionable = true, broken, knockout, targetable = true, lastTargetable = true;
    public int cardOrder, light = 9, maxLight = 9; public float hp = 100; public bool teamKill;
    public BattleUnitTurnState turnState; public IHandleAutoCardUse Policy;
    public List<SpeedDice> speedDiceResult = new List<SpeedDice>();
    public BattlePlayingCardSlotDetail cardSlotDetail; public BattleAllyCardDetail allyCardDetail;
    public PersonalCards personalEgoDetail = new PersonalCards(); public Emotion emotionDetail = new Emotion();
    public UnitInfo UnitData = new UnitInfo(); public EnemyUnitTargetSetter targetSetter = new EnemyUnitTargetSetter();
    public Func<BattleDiceCardModel, BattleUnitModel> forceTarget;
    public Func<BattleDiceCardModel, int, int> forceTargetSlot;
    public Func<BattleDiceCardModel, BattleDiceCardModel> forceCard;
    public Action<BattlePlayingCardDataInUnitModel> onApply;
    public List<BattleUnitModel> fixedTargets = new List<BattleUnitModel>();
    public BattleUnitModel(Faction f, int slots = 2)
    {
        faction = f; for (int i = 0; i < slots; i++) speedDiceResult.Add(new SpeedDice());
        cardSlotDetail = new BattlePlayingCardSlotDetail(this, slots); allyCardDetail = new BattleAllyCardDetail(this);
    }
    public int PlayPoint => light; public int MaxPlayPoint => maxLight; public int speedDiceCount => speedDiceResult.Count;
    public bool IsDead() => dead; public bool IsActionable() => actionable; public bool IsBreakLifeZero() => broken;
    public bool IsKnockout() => knockout; public bool IsControlable() => controlled;
    public int GetMinHp() => 0; public float GetResistHP(BehaviourDetail d) => 1;
    public int GetSpeed(int i) => speedDiceResult[i].value; public bool IsTargetable_theLast() => lastTargetable;
    public bool IsCardChoosable(BattleDiceCardModel card) => card.choosable;
    public void SetCurrentOrder(int i) { cardOrder = i; }
    public bool TeamKill() => teamKill;
    public bool IsTargetable(BattleUnitModel actor) => targetable;
    public bool CanChangeAttackTarget(BattleUnitModel target, int mine, int theirs) => GetSpeed(mine) > target.GetSpeed(theirs);
    public List<BattleUnitModel> GetFixedTargets() => fixedTargets;
    public static bool IsTargetableUnit(BattleDiceCardModel card, BattleUnitModel actor, BattleUnitModel target, int slot = -1) =>
        target.targetable && target != actor && (card.allyOnly ? target.faction == Faction.Player :
            actor.faction != target.faction || !target.controlled || actor.TeamKill() || target.TeamKill()) &&
        (actor.fixedTargets.Count == 0 || actor.fixedTargets.Contains(target));
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool CheckCardAvailable(BattleDiceCardModel card) => CheckCardAvailableForPlayer(card) && IsControlable() && IsCardChoosable(card);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool CheckCardAvailableForPlayer(BattleDiceCardModel card) => card.available && card.GetCost() <= PlayPoint - cardSlotDetail.ReservedPlayPoint && emotionDetail.EmotionLevel >= card.spec.emotionLimit;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ChangeTargetSlot(BattleDiceCardModel card, BattleUnitModel target, int mine, int theirs, bool teamkill) => forceTargetSlot?.Invoke(card, theirs) ?? theirs;
    public BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int slot) => forceTarget?.Invoke(card);
    public BattleDiceCardModel OnSelectCardAuto(BattleDiceCardModel card, int slot) => forceCard?.Invoke(card) ?? card;
}
public sealed class BattlePlayingCardSlotDetail
{
    public BattleUnitModel _self; public int ReservedPlayPoint; public List<BattlePlayingCardDataInUnitModel> cardAry;
    public int addCount, releaseCount;
    public BattlePlayingCardSlotDetail(BattleUnitModel owner, int count) { _self = owner; cardAry = Enumerable.Repeat<BattlePlayingCardDataInUnitModel>(null, count).ToList(); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AddCard(BattleDiceCardModel card, BattleUnitModel target, int targetSlot, bool isEnemyAuto = false)
    {
        addCount++;
        if (card.spec.Ranged == CardRange.Instance)
        {
            _self.allyCardDetail._cardInHand.Remove(card); _self.personalEgoDetail._cardInHand.Remove(card);
            _self.light -= card.cost; card.instance?.Invoke(_self); return;
        }
        if (isEnemyAuto) _self.cardOrder = cardAry.FindIndex(x => x == null);
        var slot = _self.cardOrder;
        if (cardAry[slot] != null) releaseCount++;
        var played = new BattlePlayingCardDataInUnitModel { owner = _self, card = card, target = target, targetSlotOrder = targetSlot, slotOrder = slot, speedDiceResultValue = _self.GetSpeed(slot) };
        cardAry[slot] = played; ReservedPlayPoint += card.cost; _self.allyCardDetail._cardInHand.Remove(card);
        _self.onApply?.Invoke(played);
        var other = target.cardSlotDetail.cardAry[targetSlot];
        if (other != null && _self.CanChangeAttackTarget(target, slot, targetSlot)) { other.target = _self; other.targetSlotOrder = slot; }
    }
}
public sealed class BattleAllyCardDetail
{
    public BattleUnitModel _self; public List<BattleDiceCardModel> _cardInHand = new List<BattleDiceCardModel>();
    public BattleAllyCardDetail(BattleUnitModel owner) { _self = owner; }
    public List<BattleDiceCardModel> GetHand() => new List<BattleDiceCardModel>(_cardInHand);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PlayTurnAutoForPlayer(int idx)
    {
        if (_self.broken || _self.knockout || _self.turnState == BattleUnitTurnState.BREAK) return;
        var candidates = new List<KeyValuePair<int, BattleDiceCardModel>>();
        foreach (var c in _cardInHand.ToArray())
            if (c.spec.Ranged != CardRange.Instance && _self.CheckCardAvailableForPlayer(c)) candidates.Add(new KeyValuePair<int, BattleDiceCardModel>(c.GetPriority(), c));
        if (candidates.Count == 0) return;
        candidates.Sort((a,b) => b.Key - a.Key);
        var card = candidates[0].Value;
        var target = BattleObjectManager.instance.GetTargetByCardForPlayer(_self, card, idx, _self.TeamKill());
        var max = target.speedDiceResult.Count; // Deliberately mirrors native null dereference.
        int targetSlot = _self.ChangeTargetSlot(card, target, idx, 0, false);
        _self.cardSlotDetail.AddCard(card, target, targetSlot, false);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void PlayTurnAutoForEnemy(int currentDiceSlotIdx, int speed)
    {
        var candidates = new List<KeyValuePair<int, BattleDiceCardModel>>();
        foreach (var c in _cardInHand.ToArray())
            if (c.spec.Ranged != CardRange.Instance && _self.CheckCardAvailable(c) && c.GetPriority(speed) > -10) candidates.Add(new KeyValuePair<int, BattleDiceCardModel>(c.GetPriority(speed), c));
        if (candidates.Count == 0) return;
        candidates.Sort((a,b) => b.Key - a.Key);
        var card = _self.OnSelectCardAuto(candidates[0].Value, currentDiceSlotIdx);
        var target = BattleObjectManager.instance.GetTargetByCard(_self, card, currentDiceSlotIdx, _self.TeamKill());
        if (target == null) return;
        int targetSlot = _self.ChangeTargetSlot(card, target, currentDiceSlotIdx, 0, false);
        _self.cardSlotDetail.AddCard(card, target, targetSlot, true);
    }
}
public class EnemyUnitTargetSetter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual BattleUnitModel SelectTargetUnit(List<BattleUnitModel> candidates) => RandomUtil.SelectOne(candidates);
}
public sealed class BattleObjectManager
{
    public static BattleObjectManager instance = new BattleObjectManager();
    public List<BattleUnitModel> units = new List<BattleUnitModel>();
    public List<BattleUnitModel> GetList() => units;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public BattleUnitModel GetTargetByCardForPlayer(BattleUnitModel actor, BattleDiceCardModel card, int idx, bool teamkill = false)
    {
        var candidates = Candidates(actor, teamkill);
        var forced = actor.forceTarget?.Invoke(card);
        return forced ?? actor.targetSetter.SelectTargetUnit(candidates);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public BattleUnitModel GetTargetByCard(BattleUnitModel actor, BattleDiceCardModel card, int idx, bool teamkill = false)
    {
        var candidates = Candidates(actor, teamkill);
        var forced = actor.forceTarget?.Invoke(card);
        return forced ?? actor.targetSetter.SelectTargetUnit(candidates);
    }
    private List<BattleUnitModel> Candidates(BattleUnitModel actor, bool teamkill)
    {
        var fixedTargets = actor.GetFixedTargets().Where(x => x.IsTargetable(actor)).ToList();
        return fixedTargets.Count > 0 ? fixedTargets : units.Where(x => x != actor &&
            (teamkill || x.faction != actor.faction) && x.targetable && !x.dead).ToList();
    }
}
public sealed class StageController
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetAutoCardForPlayer()
    {
        foreach (var unit in BattleObjectManager.instance.units.Where(x => x.faction == Faction.Player).ToArray())
            for (var i = 0; i < unit.speedDiceCount; i++) if (!unit.speedDiceResult[i].breaked && unit.cardSlotDetail.cardAry[i] == null)
            { unit.cardOrder = i; unit.allyCardDetail.PlayTurnAutoForPlayer(i); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ApplyEnemyCardPhase()
    {
        foreach (var unit in BattleObjectManager.instance.units.Where(x => x.faction == Faction.Enemy).ToArray())
            for (var i = 0; i < unit.speedDiceCount; i++) if (!unit.speedDiceResult[i].breaked) unit.allyCardDetail.PlayTurnAutoForEnemy(i, unit.GetSpeed(i));
        SetAutoCardForNonControlablePlayer();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetAutoCardForNonControlablePlayer()
    {
        foreach (var unit in BattleObjectManager.instance.units.Where(x => x.faction == Faction.Player && !x.controlled).ToArray())
            for (var i = 0; i < unit.speedDiceCount; i++) if (!unit.speedDiceResult[i].breaked && unit.cardSlotDetail.cardAry[i] == null)
            { unit.cardOrder = i; unit.allyCardDetail.PlayTurnAutoForPlayer(i); }
    }
}
public static class RandomUtil { public static int Range(int a, int b) => a; public static T SelectOne<T>(List<T> list) => list.FirstOrDefault(); }
public static class BookModel { public static float GetResistRate(float value) => value; }
public sealed class BattleCardAbilityDescXmlList
{
    public static BattleCardAbilityDescXmlList Instance = new BattleCardAbilityDescXmlList();
    public List<string> GetAbilityKeywords_byScript(string script) => script.Split(',').ToList();
}
