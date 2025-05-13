using LibraryOfAngela.Battle;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Buf
{
    class TremorControllerImpl : TremorController
    {
        public string keywordId => "LoATremor";

        public string keywordIconId => "LoATremor";

        public void Burst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, bool isCard)
        {
            var originValue = buf.stack;
            var value = buf.stack;
            var giveList = GetGiveList(actor);
            var takeList = GetTakeList(buf);
            var isBreaked = buf._owner.breakDetail.IsBreakLifeZero();
            var isDead = buf._owner.IsDead();

            buf.BeforeTakeTremorBurst(actor, ref value, originValue);
            foreach (var eff in giveList) eff.BeforeGiveTremorBurst(buf, ref value, originValue);
            foreach (var eff in takeList) eff.BeforeTakeTremorBurst(actor, buf, ref value, originValue);

            buf._owner.TakeBreakDamage(value, DamageType.Buf, actor, keyword: LoAKeywordBuf.Tremor);

            buf.OnTakeTremorBurst(actor, value, isCard);
            foreach (var eff in giveList) eff.OnGiveTremorBurst(buf, value, isCard);
            foreach (var eff in takeList) eff.OnTakeTremorBurst(actor, buf, value, isCard);

            if (isBreaked != buf._owner.breakDetail.IsBreakLifeZero())
            {
                buf.OnBreakStateByTremorBurst(actor, isCard);
                foreach (var eff in giveList) eff.OnMakeBreakStateByTremorBurst(buf, isCard);
                foreach (var eff in takeList) eff.OnBreakStateByTremorBurst(actor, buf, isCard);
            }

            if (isDead != buf._owner.IsDead())
            {
                buf.OnDieByTremorBurst(actor, isCard);
                foreach (var eff in giveList) eff.OnKillByTremorBurst(buf, isCard);
                foreach (var eff in takeList) eff.OnDieByTremorBurst(actor, buf, isCard);
            }

            if (StageController.Instance.IsLogState())
            {
                buf._owner.battleCardResultLog.SetAfterActionEvent(() =>
                {
                    EffectTremorBurst(buf);
                });
            }
            else
            {
                EffectTremorBurst(buf);
            }
        }

        private void EffectTremorBurst(BattleUnitBuf_loaTremor buf)
        {

        }

        public void ReduceStack(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, int value, bool isRoundEnd)
        {
            var originValue = value;
            buf.OnTakeTremorReduceStack(actor, ref value, originValue, isRoundEnd);
            foreach (var eff in GetTakeList(buf))
            {
                eff.OnTakeTremorReduceStack(actor, buf, ref value, originValue, isRoundEnd);
            }
            buf.stack -= value;
            if (buf.stack <= 0) buf.Destroy();
        }

        public void OnRoundEndTremor(BattleUnitBuf_loaTremor buf)
        {
            var nextStack = (buf.stack * 2) / 3;
            var reduceValue = buf.stack - nextStack;
            ReduceStack(null, buf, reduceValue, false);
        }

        public T TremorTransform<T>(BattleUnitModel attacker, BattleUnitBuf_loaTremor current) where T : BattleUnitBuf_loaTremor, new()
        {
            var newBuf = new T();
            current.OnTakeTremorTransform(attacker, newBuf);
            foreach (var eff in GetGiveList(attacker)) eff.OnGiveTremorTransform(current, newBuf);
            foreach (var eff in GetTakeList(current)) eff.OnTakeTremorTransform(attacker, current, newBuf);

        }

        void BufReplace(List<BattleUnitBuf> bufList, Type previousType, BattleUnitBuf next)
        {
            for (int i = 0; i < bufList.Count; i++)
            {
                var b = bufList.Count;


            }
        }

        private List<IHandleTakeTremor> GetTakeList(BattleUnitBuf_loaTremor buf)
        {
            return BattleInterfaceCache.Of<IHandleTakeTremor>(buf._owner).ToList();
        }

        private List<IHandleGiveTremor> GetGiveList(BattleUnitModel actor)
        {
            if (actor is null) return new List<IHandleGiveTremor>();
            return BattleInterfaceCache.Of<IHandleGiveTremor>(actor).ToList();
        }


    }
}
