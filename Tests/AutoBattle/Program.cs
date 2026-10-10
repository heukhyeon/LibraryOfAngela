using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LibraryOfAngela.Battle;
using LibraryOfAngela.Interface_External;
using LOR_DiceSystem;
internal static class Program
{
    private sealed class Policy : IHandleAutoCardUse
    {
        public Func<BattleDiceCardModel, BattleUnitModel, int, int, bool, int> score = (c,t,s,ts,clash) => c.name == "A" ? 100 : 1;
        public bool IsParryigSwitchable => true;
        public int GetCustomizedCardPriority(BattleUnitModel a, BattleDiceCardModel c, int speed, int slot, BattleUnitModel t,
            int ts, BattleDiceCardModel tc, bool clash, List<BattleUnitModel> enemies, List<BattleUnitModel> allies)
        {
            Assert(enemies != null && allies != null, "callback receives candidate lists");
            return score(c,t,slot,ts,clash);
        }
    }
    private sealed class LastSetter : EnemyUnitTargetSetter
    { public override BattleUnitModel SelectTargetUnit(List<BattleUnitModel> candidates) => candidates.LastOrDefault(); }
    private sealed class NullSetter : EnemyUnitTargetSetter
    { public override BattleUnitModel SelectTargetUnit(List<BattleUnitModel> candidates) => null; }
    private static int passed;
    private static BattleUnitModel forcedFromPostfix;
    private static int candidateCount;
    private static void ObserveCandidates(List<BattleUnitModel> candidates) { candidateCount = candidates.Count; }
    private static void ForceTargetPostfix(ref BattleUnitModel __result) { __result = forcedFromPostfix; }
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static BattleUnitModel Unit(Faction f, int slots = 2)
    {
        var u = new BattleUnitModel(f, slots); BattleObjectManager.instance.units.Add(u); return u;
    }
    private static BattleDiceCardModel Card(BattleUnitModel u, string name, int cost = 1)
    {
        var c = new BattleDiceCardModel(name) { cost = cost }; u.allyCardDetail._cardInHand.Add(c); return c;
    }
    private static void Run(string name, Action test)
    {
        BattleObjectManager.instance.units.Clear(); test(); passed++; Console.WriteLine("PASS " + name);
    }
    private static void Players() => new StageController().SetAutoCardForNonControlablePlayer();
    private static void Main()
    {
        new Harmony("loa.autobattle.tests").PatchAll(typeof(AutoBattlePatch));
        Run("native selection follows cached card, target and target slot", () => {
            var p = Unit(Faction.Player, 1); var t1 = Unit(Faction.Enemy); var t2 = Unit(Faction.Enemy);
            p.Policy = new Policy { score = (c,t,s,ts,cl) => (c.name == "A" ? 100 : 0) + (t == t2 ? 20 : 0) + ts * 5 };
            Card(p,"B").priority = 999; var a = Card(p,"A"); Players();
            var used = p.cardSlotDetail.cardAry[0]; Assert(used.card == a && used.target == t2 && used.targetSlotOrder == 1, "cache not followed");
        });
        Run("forecast is read-only and reserves cards and light", () => {
            var p = Unit(Faction.Player); Unit(Faction.Enemy); p.Policy = new Policy(); p.light = 3;
            var a = Card(p,"A",2); Card(p,"B",2); var plan = new AutoBattleImpl(Faction.Player);
            var first = plan.GetPlan(p,0); var next = plan.GetPlan(p,1);
            Assert(first.card == a && next == null, "forecast overspends");
            Assert(p.light == 3 && p.cardSlotDetail.ReservedPlayPoint == 0 && p.allyCardDetail._cardInHand.Count == 2 && p.cardSlotDetail.addCount == 0, "planning mutated battle");
            Players(); Assert(p.cardSlotDetail.cardAry.Count(x=>x!=null) == 1, "native overspends");
        });
        Run("forced target is kept and future targets are recalculated", () => {
            var p = Unit(Faction.Player); var t1 = Unit(Faction.Enemy); var t2 = Unit(Faction.Enemy);
            p.Policy = new Policy { score = (c,t,s,ts,cl) => c.name == "A" ? 200 + (t == t1 ? 50 : 0) :
                (t == (p.cardSlotDetail.cardAry.Any(x=>x?.target==t2) ? t1 : t2) ? 100 : 0) };
            var a = Card(p,"A"); var b = Card(p,"B"); p.forceTarget = c => c == a ? t2 : null; Players();
            Assert(p.cardSlotDetail.cardAry[0].card == a && p.cardSlotDetail.cardAry[0].target == t2, "forced target overwritten");
            Assert(p.cardSlotDetail.cardAry[1].card == b && p.cardSlotDetail.cardAry[1].target == t1, "remaining cache stale");
            Assert(p.cardSlotDetail.releaseCount == 0, "earlier choice rolled back");
        });
        Run("forced target slot overrides suggestion", () => {
            var p = Unit(Faction.Player,1); Unit(Faction.Enemy); p.Policy = new Policy { score = (c,t,s,ts,cl) => ts*100 };
            Card(p,"A"); p.forceTargetSlot = (c,s) => { Assert(s == 1, "cache not provided to hook"); return 0; }; Players();
            Assert(p.cardSlotDetail.cardAry[0].targetSlotOrder == 0, "hook result overwritten");
        });
        Run("moved source slot remains committed without backfilling", () => {
            var p = Unit(Faction.Player,3); Unit(Faction.Enemy); p.Policy = new Policy(); var a=Card(p,"A"); var b=Card(p,"B");
            p.onApply = played => { if (played.card == a) { p.cardSlotDetail.cardAry[0]=null; p.cardSlotDetail.cardAry[2]=played; played.slotOrder=2; } };
            Players(); Assert(p.cardSlotDetail.cardAry[0] == null && p.cardSlotDetail.cardAry[2].card == a && p.cardSlotDetail.cardAry[1].card == b, "source move not respected");
            Assert(p.cardSlotDetail.addCount == 2 && p.cardSlotDetail.releaseCount == 0, "committed slot overwritten");
        });
        Run("actual cost changes constrain remaining plan", () => {
            var p=Unit(Faction.Player); Unit(Faction.Enemy); p.Policy=new Policy(); p.light=3; Card(p,"A"); Card(p,"B",2);
            p.onApply = played => p.cardSlotDetail.ReservedPlayPoint++;
            Players(); Assert(p.cardSlotDetail.cardAry[0]!=null && p.cardSlotDetail.cardAry[1]==null, "ignored actual reserve cost");
        });
        Run("preexisting selections are retained", () => {
            var p=Unit(Faction.Player); var t=Unit(Faction.Enemy); p.Policy=new Policy(); var fixedCard=Card(p,"fixed");
            p.cardSlotDetail.AddCard(fixedCard,t,0); var committed=p.cardSlotDetail.cardAry[0]; Card(p,"A"); Players();
            Assert(ReferenceEquals(committed,p.cardSlotDetail.cardAry[0]) && p.cardSlotDetail.releaseCount==0, "preexisting selection changed");
        });
        Run("enemy OnSelectCardAuto replacement wins", () => {
            var e=Unit(Faction.Enemy); Unit(Faction.Player); e.Policy=new Policy(); var a=Card(e,"A"); var b=Card(e,"B");
            bool once=false; e.forceCard=c=> { if(once) return c; once=true; return b; };
            new StageController().ApplyEnemyCardPhase();
            Assert(e.cardSlotDetail.cardAry[0].card==b && e.cardSlotDetail.cardAry[1].card==a, "forced card replacement lost");
        });
        Run("custom target setter wins over cached target", () => {
            var p=Unit(Faction.Player,1); var t1=Unit(Faction.Enemy); var t2=Unit(Faction.Enemy);
            p.Policy=new Policy { score=(c,t,s,ts,cl)=>t==t1?100:0 }; Card(p,"A"); p.targetSetter=new LastSetter(); Players();
            Assert(p.cardSlotDetail.cardAry[0].target==t2, "custom target setter overwritten");
        });
        Run("missing final target exits native player path safely", () => {
            var p=Unit(Faction.Player,1); Unit(Faction.Enemy); p.Policy=new Policy(); Card(p,"A"); p.targetSetter=new NullSetter(); Players();
            Assert(p.cardSlotDetail.addCount==0, "null target equipped");
        });
        Run("exception restores selection context", () => {
            var p=Unit(Faction.Player,1); Unit(Faction.Enemy); p.Policy=new Policy(); Card(p,"A"); p.forceTarget=c=>throw new InvalidOperationException("forced failure");
            try { Players(); throw new Exception("missing expected failure"); } catch(InvalidOperationException) { }
            Assert(typeof(AutoBattlePatch).GetField("current",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)==null,"context leaked");
            Assert(typeof(AutoBattlePatch).GetField("playerPlan",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)==null,"phase leaked");
        });
        Run("controllable player keeps native priorities", () => {
            var p=Unit(Faction.Player,1); Unit(Faction.Enemy); p.controlled=true; p.Policy=new Policy(); Card(p,"A"); var b=Card(p,"B"); b.priority=999;
            p.allyCardDetail.PlayTurnAutoForPlayer(0); Assert(p.cardSlotDetail.cardAry[0].card==b,"affected controllable player");
        });
        Run("unchoosable and emotion-locked cards are excluded", () => {
            var p=Unit(Faction.Player,1); Unit(Faction.Enemy); p.Policy=new Policy(); Card(p,"A").choosable=false;
            Card(p,"locked").spec.emotionLimit=10; var b=Card(p,"B"); Players(); Assert(p.cardSlotDetail.cardAry[0].card==b,"invalid card used");
        });
        Run("large custom priorities do not overflow", () => {
            var p=Unit(Faction.Player,1); Unit(Faction.Enemy); p.Policy=new Policy { score=(c,t,s,ts,cl)=>c.name=="A"?int.MaxValue:int.MinValue };
            Card(p,"B"); var a=Card(p,"A"); Players(); Assert(p.cardSlotDetail.cardAry[0].card==a,"priority overflow");
        });
        Run("personal instant cards can target allies and draw a normal card", () => {
            var p=Unit(Faction.Player,1); Unit(Faction.Enemy); p.Policy=new Policy();
            var instant=new BattleDiceCardModel("instant") { allyOnly=true, instance=u=>Card(u,"A") }; instant.spec.Ranged=CardRange.Instance;
            p.personalEgoDetail._cardInHand.Add(instant); Players();
            Assert(p.personalEgoDetail._cardInHand.Count==0 && p.cardSlotDetail.addCount==2 && p.cardSlotDetail.cardAry[0].card.name=="A","instant prepass failed");
        });
        Run("global clash allocation considers other allies", () => {
            var slow=Unit(Faction.Player,1); var fast=Unit(Faction.Player,1); var e=Unit(Faction.Enemy,1);
            slow.Policy=new Policy(); fast.Policy=new Policy(); slow.speedDiceResult[0].value=1; fast.speedDiceResult[0].value=8;
            Card(slow,"A"); Card(fast,"A"); var attack=Card(e,"enemy"); e.cardSlotDetail.AddCard(attack,slow,0);
            // Prefer the fast ally through the policy while both are legal clash candidates.
            fast.Policy=new Policy { score=(c,t,s,ts,cl)=>10000 };
            var plan=new AutoBattleImpl(Faction.Player); Assert(plan.GetPlan(fast,0).clash,"global parry allocation lost");
            Assert(fast.cardSlotDetail.addCount==0,"forecast equipped a card");
        });
        Run("other target-setter patches see all candidates and override cache", () => {
            var p=Unit(Faction.Player,1); var t1=Unit(Faction.Enemy); var t2=Unit(Faction.Enemy);
            p.Policy=new Policy { score=(c,t,s,ts,cl)=>t==t1?100:0 }; Card(p,"A"); forcedFromPostfix=t2;
            var other=new Harmony("loa.autobattle.other-mod-test");
            var method=AccessTools.Method(typeof(EnemyUnitTargetSetter),"SelectTargetUnit");
            other.Patch(method,new HarmonyMethod(AccessTools.Method(typeof(Program),"ObserveCandidates")),
                new HarmonyMethod(AccessTools.Method(typeof(Program),"ForceTargetPostfix")));
            try { Players(); Assert(candidateCount==2 && p.cardSlotDetail.cardAry[0].target==t2,"other target patch was constrained"); }
            finally { other.Unpatch(method,HarmonyPatchType.All,other.Id); }
        });
        Run("manual auto button shares remaining plan after a source move", () => {
            var p=Unit(Faction.Player,3); Unit(Faction.Enemy); p.Policy=new Policy(); var a=Card(p,"A"); var b=Card(p,"B");
            p.onApply = played => { if(played.card==a) { p.cardSlotDetail.cardAry[0]=null; p.cardSlotDetail.cardAry[2]=played; played.slotOrder=2; } };
            new StageController().SetAutoCardForPlayer();
            Assert(p.cardSlotDetail.cardAry[0]==null && p.cardSlotDetail.cardAry[1].card==b && p.cardSlotDetail.cardAry[2].card==a,"auto-button session lost committed move");
        });
        Run("single enemy slot does not leave usable faster slots empty", () => {
            var p=Unit(Faction.Player); var e=Unit(Faction.Enemy,1); p.Policy=new Policy();
            p.speedDiceResult.ForEach(x=>x.value=8); Card(p,"A"); Card(p,"B");
            var enemyCard=Card(e,"enemy"); e.cardSlotDetail.AddCard(enemyCard,p,0);
            Players(); Assert(p.cardSlotDetail.cardAry.All(x=>x!=null),"claimed clash starved the remaining slot");
            Assert(e.cardSlotDetail.cardAry[0].targetSlotOrder==1,"native redirection was rolled back");
        });
        Run("instant prepass uses an empty slot beside an existing selection", () => {
            var e=Unit(Faction.Enemy); var p=Unit(Faction.Player); e.controlled=true; e.Policy=new Policy();
            var existing=Card(e,"existing"); e.cardSlotDetail.AddCard(existing,p,0); var committed=e.cardSlotDetail.cardAry[0];
            var instant=new BattleDiceCardModel("instant") { instance=u=> { Assert(u.cardOrder==1,"instant used occupied slot"); Card(u,"A"); } };
            instant.spec.Ranged=CardRange.Instance; e.personalEgoDetail._cardInHand.Add(instant);
            new StageController().ApplyEnemyCardPhase();
            Assert(ReferenceEquals(committed,e.cardSlotDetail.cardAry[0]) && e.cardSlotDetail.cardAry[1].card.name=="A" && e.cardSlotDetail.releaseCount==0,"instant disturbed existing selection");
        });
        Console.WriteLine("PASS " + passed + " scenarios");
    }
}
