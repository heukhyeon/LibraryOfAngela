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
    class RuptureControllerImpl : RuptureController, BufControllerImpl
    {
        public string keywordId => "LoARupture";

        public string keywordIconId => "loa_rupture_icon";
        public BufPositiveType positiveType => BufPositiveType.Negative;
        public void OnRoundEndRupture(BattleUnitBuf_loaRupture buf) {
            bool isDestroy = true;

            RunCatching("OnRoundEndRupture", () => {
                var takeListener = BattleInterfaceCache.Of<IHandleTakeRupture>(buf._owner).ToList();
                foreach (var listener in takeListener)
                {
                    try
                    {
                        listener.OnRoundEndInRupture(buf, ref isDestroy);
                    }
                    catch (MissingMethodException)
                    {

                    }
                }
            });

            if (isDestroy || buf.stack <= 0)
            {
                buf.Destroy();
            }
        }

        public void OnTakeDamageByAttackRupture(BattleUnitBuf_loaRupture buf, BattleDiceBehavior atkDice, int dmg) {
            Damage(atkDice.owner, buf);
            if (StageController.Instance.IsLogState()) {
                buf._owner.battleCardResultLog.SetTakeDamagedEvent(() => {
                    EffectRupture(buf);
                });
            } else {
                EffectRupture(buf);
            }
        }

        private void Damage(BattleUnitModel attacker, BattleUnitBuf_loaRupture buf) {
            var takeListener = BattleInterfaceCache.Of<IHandleTakeRupture>(buf._owner).ToList();
            var giveListener = BattleInterfaceCache.Of<IHandleGiveRupture>(attacker).ToList();
            var dmg = buf.stack;
            var originDmg = dmg;
            var isDead = buf._owner.IsDead();
            RunCatching("BeforeTakeDamage", () => {
                buf.BeforeTakeRuptureDamage(ref dmg, originDmg);
                foreach (var listener in takeListener) {
                    listener.BeforeTakeRuptureDamage(buf, originDmg, ref dmg);
                }
                foreach (var listener in giveListener) {
                    listener.BeforeGiveRuptureDamage(buf, originDmg, ref dmg);
                }
            });
            buf._owner.TakeDamage(dmg, DamageType.Buf, attacker, keyword: LoAKeywordBuf.Rupture);
            RunCatching("OnTakeDamage", () => {
                buf.OnTakeRuptureDamage(dmg);
                foreach (var listener in takeListener) {
                    listener.OnTakeRuptureDamage(buf, dmg);
                }
                foreach (var listener in giveListener) {
                    listener.OnGiveRuptureDamage(buf, dmg);
                }
            });
            if (!isDead && buf._owner.IsDead()) {
                RunCatching("Die", () => {
                    buf.OnDieByRupture(attacker);
                    foreach (var listener in takeListener) {
                        listener.OnDieByRupture(attacker, buf);
                    }
                    foreach (var listener in giveListener) {
                        listener.OnKillByRupture(buf);
                    }
                });
            }
            buf.ReduceStack(new LoAKeywordBufReduceRequest.Attack(attacker.currentDiceAction.currentBehavior, (buf.stack) - ((buf.stack * 2) / 3)));
        }

        private void RunCatching(string key, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.Log("Rupture Error in " + key);
                Logger.LogError(e);
            }
        }

        private void EffectRupture(BattleUnitBuf_loaRupture buf) {
            var b = BufAssetLoader.LoadObject("loa_debuff_rupture", buf._owner.view.atkEffectRoot, 2f);
            b.transform.localPosition = Vector3.zero;
            b.transform.localScale = Vector3.one * 0.3f;
        }

        public string GetBufActivatedText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "一幕内自身被击中时将受到{0}点伤害并使“破裂”层数减少1/3。（向上取整）";
                case "en":
                    return "For this Scene, when hit, take {0} damage and subtract 2/3rd of the Rupture stack. (Rounds down)";
                default:
                    return "한 막동안 피격시 피해 {0}을 받고 파열 수치가 2/3로 감소한다.(소수점 이하 버림)";
            }
        }

        public string GetBufName()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "kr":
                    return "파열";
                case "cn":
                case "trcn":
                    return "破裂";
                default:
                    return "Rupture";
            }
        }

        public string GetKeywordText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "一幕内自身被击中时将受到X点伤害并使“破裂”层数减少1/3。（向上取整）";
                case "en":
                    return "For the Scene, when hit, take X damage and subtract 2/3rd of the Rupture stack. (Rounds down)";
                default:
                    return "한 막 동안 피격시 피해 X를 받고 파열 수치가 2/3로 감소한다.(소수점 이하 버림)";
            }
        }

        public void AddAdditionalKeywordDesc()
        {

        }

        void RuptureController.OnReduceStack(BattleUnitBuf_loaRupture buf, LoAKeywordBufReduceRequest request)
        {
            RunCatching("ReduceStack", () => {
                var takeListener = BattleInterfaceCache.Of<IHandleTakeRupture>(buf._owner).ToList();
                var giveListener = BattleInterfaceCache.Of<IHandleGiveRupture>(request.Attacker).ToList();
                var value = request.Stack;
                buf.OnTakeRuptureReduceStack(request, ref value);
                foreach (var listener in takeListener)
                {
                    listener.OnTakeRuptureReduceStack(buf, request, ref value);
                }
                foreach (var listener in giveListener)
                {
                    listener.OnGiveRuptureReduceStack(buf, request, ref value);
                }
                buf.stack -= value;
                buf.OnAddBuf(-value);
                if (buf.stack <= 0) buf.Destroy();
            });
        }
    }
}