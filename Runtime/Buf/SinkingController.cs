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
    class SinkingControllerImpl : SinkingController, BufControllerImpl
    {
        public string keywordId => "LoASinking";

        public string keywordIconId => "loa_sinking_icon";


        public void OnTakeDamageByAttackSinking(BattleUnitBuf_loaSinking buf, BattleDiceBehavior atkDice, int dmg) {

        }

        public void OnRoundEndSinking(BattleUnitBuf_loaSinking buf) {
            var reducedValue = (buf.stack * 2) / 3;
            var reduceValue = buf.stack - reducedValue;
            SinkingBreakDmg(null, buf)
            RunCatching("ReduceStack", () => {
                var value = reduceValue;
                foreach (var listener in BattleInterfaceCache.Of<IHandleTakeSinking>(buf._owner)) {
                    listener.OnTakeSinkingReduceStack(buf, ref value, reduceValue);
                }
                buf.stack -= value;
                buf.OnAddBuf(-value);
                if (buf.stack <= 0) buf.Destroy();
            });
            EffectSinking(buf);
        }

        public void OnAddBufSinking(BattleUnitBuf_loaSinking buf, int addedStack) {

        }

        private void SinkingBreakDmg(BattleUnitModel actor, BattleUnitBuf_loaSinking buf) {
            var listeners = BattleInterfaceCache.Of<IHandleTakeSinking>(buf._owner).ToList();
            var dmg = buf.stack;
            var originDmg = dmg;
            var isBreaked = buf._owner.breakDetail.isBreakLifeZero();
            RunCatching("BeforeTakeBreakDamage", () => {
                foreach (var listener in listeners) {
                    listener.BeforeTakeSinkingBreakDamage(buf, ref dmg, originDmg);
                }
            });
            buf._owner.TakeBreakDamage(dmg, DamageType.Buf, buf._owner, keyword: LoAKeywordBuf.Sinking);
            RunCatching("OnTakeBreakDamage", () => {
                foreach (var listener in listeners) {
                    listener.OnTakeSinkingBreakDamage(buf, dmg);
                }
            });
            if (!isBreaked && buf._owner.breakDetail.isBreakLifeZero()) {
                RunCatching("BreakState", () => {
                    foreach (var listener in listeners) {
                        listener.OnBreakStateBySinking(actor, dmg);
                    }
                });
            }
        }

        private void RunCatching(string key, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.Log("Sinking Error in " + key);
                Logger.LogError(e);
            }
        }

        private void EffectSinking(BattleUnitBuf_loaSinking buf) {
            var b = BufAssetLoader.LoadObject("loa_debuff_sinking", buf._owner.view.atkEffectRoot, 2f);
            b.transform.localPosition = Vector3.zero;
            b.transform.localScale = Vector3.one * 0.3f;
        }
    }
}