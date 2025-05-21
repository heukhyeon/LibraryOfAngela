using LibraryOfAngela.Battle;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Buf
{
    class TremorControllerImpl : TremorController, BufControllerImpl
    {
        public string keywordId => "LoATremor";

        public string keywordIconId => "loa_tremor_icon";

        public BufPositiveType positiveType => BufPositiveType.Negative;

        public void Burst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, bool isCard)
        {
            var originValue = buf.stack;
            var value = buf.stack;
            var giveList = GetGiveList(actor);
            var takeList = GetTakeList(buf);
            var isBreaked = buf._owner.breakDetail.IsBreakLifeZero();
            var isDead = buf._owner.IsDead();

            RunCatching("BeforeTremorBurst", () =>
            {
                buf.BeforeTakeTremorBurst(actor, ref value, originValue);
                foreach (var eff in giveList) eff.BeforeGiveTremorBurst(buf, ref value, originValue);
                foreach (var eff in takeList) eff.BeforeTakeTremorBurst(actor, buf, ref value, originValue);
            });

            if (value > 0)
            {
                buf._owner.TakeBreakDamage(value, DamageType.Buf, actor, keyword: LoAKeywordBuf.Tremor);
            }
            else
            {
                value = 0;
            }
            
            RunCatching("OnTakeTremorBurst", () =>
            {
                buf.OnTakeTremorBurst(actor, value, isCard);
                foreach (var eff in giveList) eff.OnGiveTremorBurst(buf, value, isCard);
                foreach (var eff in takeList) eff.OnTakeTremorBurst(actor, buf, value, isCard);
            });


            RunCatching("OnBreakStateByTremorBurst", () =>
            {
                if (isBreaked != buf._owner.breakDetail.IsBreakLifeZero())
                {
                    buf.OnBreakStateByTremorBurst(actor, isCard);
                    foreach (var eff in giveList) eff.OnMakeBreakStateByTremorBurst(buf, isCard);
                    foreach (var eff in takeList) eff.OnBreakStateByTremorBurst(actor, buf, isCard);
                }
            });


            RunCatching("OnDieByTremorBurst", () =>
            {
                if (isDead != buf._owner.IsDead())
                {
                    buf.OnDieByTremorBurst(actor, isCard);
                    foreach (var eff in giveList) eff.OnKillByTremorBurst(buf, isCard);
                    foreach (var eff in takeList) eff.OnDieByTremorBurst(actor, buf, isCard);
                }
            });


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
            var b = BufAssetLoader.LoadObject("loa_debuff_tremorburst", buf._owner.view.atkEffectRoot, 2f);
            b.transform.localPosition = Vector3.zero;
            b.transform.localScale = Vector3.one * 0.3f;
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

        public T TremorTransform<T>(BattleUnitModel attacker, BattleUnitBuf_loaTremor current, bool isCard) where T : BattleUnitBuf_loaTremor, new()
        {
            var now = BufReplace<T>(attacker, current._owner.bufListDetail._bufList, current, isCard);
            if (now is null)
            {
                return null;
            }
            var ready = current._owner.bufListDetail._readyBufList.Find(d => d.keywordId == current.keywordId) as BattleUnitBuf_loaTremor;
            if (ready != null)
            {
                BufReplace<T>(attacker, current._owner.bufListDetail._readyBufList, ready, isCard);
            }
            ready = current._owner.bufListDetail._readyReadyBufList.Find(d => d.keywordId == current.keywordId) as BattleUnitBuf_loaTremor;
            if (ready != null)
            {
                BufReplace<T>(attacker, current._owner.bufListDetail._readyReadyBufList, ready, isCard);
            }

            if (StageController.Instance.IsLogState())
            {
                now._owner.battleCardResultLog.SetAfterActionEvent(() =>
                {
                    EffectTremorTransform(current, now);
                });
            }
            else
            {
                EffectTremorTransform(current, now);
            }
            return now;
        }

        private void EffectTremorTransform(BattleUnitBuf_loaTremor current, BattleUnitBuf_loaTremor next)
        {

        }

        T BufReplace<T>(
            BattleUnitModel actor, 
            List<BattleUnitBuf> bufList, 
            BattleUnitBuf_loaTremor current,
            bool isCard
        ) where T : BattleUnitBuf_loaTremor, new()
        {
            var index = bufList.IndexOf(current);
            if (index == -1) return null;
            var stack = current.stack;
            var owner = current._owner;
            var newBuf = new T();
            if (!current.OnTakeTremorTransform(actor, newBuf))
            {
                return null;
            }
            current.Destroy();
            bufList.Remove(current);
            bufList.Insert(index, newBuf);
            newBuf.Init(owner);
            newBuf.stack = stack;
            newBuf.OnAddBuf(stack);
            foreach (var eff in GetGiveList(actor)) eff.OnGiveTremorTransform(current, newBuf, isCard);
            foreach (var eff in GetTakeList(current)) eff.OnTakeTremorTransform(actor, current, newBuf, isCard);
            return newBuf;
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

        public BattleUnitBuf FixValidCreateTremor(BattleUnitBuf_loaTremor buf, BattleUnitBuf current, BufReadyType readyType)
        {
            if (buf.keywordId == current.keywordId) return current;

            return (BattleUnitBuf)Activator.CreateInstance(buf.GetType());
        }

        private void RunCatching(string key, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.Log("Tremor Error in " + key);
                Logger.LogError(e);
            }
        }

        public string GetBufActivatedText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "每一幕结束时减少1/3的层数。(向上取整)";
                default:
                    return "막 종료시 진동 수치가 2/3로 감소한다.(소수점 이하 버림)";
            }
        }

        public string GetBufName()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "kr":
                    return "진동";
                case "cn":
                case "trcn":
                    return "震颤";
                default:
                    return "Tremor";
            }
        }

        public string GetKeywordText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "触发“震颤爆发”效果时受到X点混乱伤害。\n每一幕结束时减少1 / 3的层数。(向上取整)";
                default:
                    return "진동 폭발시 흐트러짐 피해 X를 받음.\n막 종료시 진동 수치가 2 / 3로 감소한다.(소수점 이하 버림)";
            }
        }
    }
}
