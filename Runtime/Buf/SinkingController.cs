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

        public BufPositiveType positiveType => BufPositiveType.Negative;
        public void OnTakeDamageByAttackSinking(BattleUnitBuf_loaSinking buf, BattleDiceBehavior atkDice, int dmg) {

        }

        public void OnRoundEndSinking(BattleUnitBuf_loaSinking buf) {
            var reducedValue = (buf.stack * 2) / 3;
            var reduceValue = buf.stack - reducedValue;
            SinkingBreakDmg(null, buf);
            RunCatching("ReduceStack", () => {
                var value = reduceValue;
                buf.OnTakeSinkingReduceStack(ref value, reduceValue);
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

        private int SinkingBreakDmg(BattleUnitModel actor, BattleUnitBuf_loaSinking buf) {
            var listeners = BattleInterfaceCache.Of<IHandleTakeSinking>(buf._owner).ToList();

            var isBreaked = buf._owner.breakDetail.IsBreakLifeZero();

            var dmg = GetSinkingDmg(buf);
            buf._owner.TakeBreakDamage(dmg, DamageType.Buf, buf._owner, keyword: LoAKeywordBuf.Sinking);
            RunCatching("OnTakeBreakDamage", () => {
                buf.OnTakeSinkingBreakDamage(dmg);
                foreach (var listener in listeners) {
                    listener.OnTakeSinkingBreakDamage(buf, dmg);
                }
            });
            if (!isBreaked && buf._owner.breakDetail.IsBreakLifeZero()) {
                RunCatching("BreakState", () => {
                    buf.OnBreakStateBySinking(actor);
                    foreach (var listener in listeners) {
                        listener.OnBreakStateBySinking(actor, buf);
                    }
                });
            }
            return dmg;
        }

        private int GetSinkingDmg(BattleUnitBuf_loaSinking buf)
        {
            var dmg = buf.stack;
            var originDmg = dmg;
            RunCatching("BeforeTakeBreakDamage", () => {
                buf.BeforeTakeSinkingBreakDamage(ref dmg, originDmg);
                foreach (var listener in BattleInterfaceCache.Of<IHandleTakeSinking>(buf._owner))
                {
                    listener.BeforeTakeSinkingBreakDamage(buf, ref dmg, originDmg);
                }
            });
            return dmg;
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
            if (StageController.Instance.IsLogState())
            {
                bool flag = false;
                buf._owner.battleCardResultLog.SetPrintEffectEvent(() =>
                {
                    if (flag) return;
                    flag = true;
                    var b = BufAssetLoader.LoadObject("loa_debuff_sinking", buf._owner.view.atkEffectRoot, 2f);
                    b.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                    b.transform.localScale = Vector3.one * 0.13f;
                });
            }
            else
            {
                var b = BufAssetLoader.LoadObject("loa_debuff_sinking", buf._owner.view.atkEffectRoot, 2f);
                b.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                b.transform.localScale = Vector3.one * 0.13f;
            }

        }

        public string GetBufActivatedText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "一幕内自身被击中时将受到{0}点混乱伤害并使“沉沦”层数减少1/3。（向上取整）";
                case "en":
                    return "At the end of the Scene, take {0} Stagger damage and subtract 1/3rd of the Sinking stack. (Rounds down)";
                default:
                    return "막 종료시 흐트러짐 피해 {0}을 받고 침잠 수치가 2/3로 감소한다.(소수점 이하 버림)";
            }
        }

        public string GetBufName()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "kr":
                    return "침잠";
                case "cn":
                case "trcn":
                    return "沉沦";
                default:
                    return "Sinking";
            }
        }

        public string GetKeywordText()
        {
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    return "一幕内自身被击中时将受到X点混乱伤害并使“沉沦”层数减少1/3。（向上取整）";
                case "en":
                    return "At the end of the Scene, take {0} Stagger damage and subtract 1/3rd of the Sinking stack. (Rounds down)";
                default:
                    return "막 종료시 흐트러짐 피해 X를 받고 침잠 수치가 2/3로 감소한다.(소수점 이하 버림)";
            }
        }

        public void AddAdditionalKeywordDesc()
        {
            string name = "";
            string desc = "";
            switch (TextDataModel.CurrentLanguage)
            {
                case "cn":
                case "trcn":
                    name = "沉沦泛滥";
                    desc = "重复触发目标的“沉沦”效果直至消耗所有“沉沦”。\n若目标无法受到混乱伤害或无混乱抗性可减少则改为受到伤害。";
                    break;
                case "en":
                    name = "Sinking Deluge";
                    desc = "Activate all target's Sinking instantly.\nIf target's Stagger Resist drops to 0, convert overflowing Stagger damage to physical damage.";
                    break;
                default:
                    name = "침잠 쇄도";
                    desc = "대상에게 있는 침잠을 침잠 수치만큼 반복하여 발동하고 침잠 제거.\n대상이 흐트러짐 피해를 받을 수 없다면 대신 피해로 받는다.";
                    break;
            }
            BattleEffectTextsXmlList.Instance._dictionary["LoASinkingDeluge_Keyword"] = new LOR_XML.BattleEffectText
            {
                ID = "LoASinkingDeluge_Keyword",
                Name = name,
                Desc = desc
            };
        }

        void SinkingController.OnDeluge(BattleUnitBuf_loaSinking buf, BattleUnitModel attacker)
        {
            try
            {
                bool flag = true;
                int totalDmg = 0;

                while (!buf.IsDestroyed())
                {
                    if (flag)
                    {
                        var bp = buf._owner.breakDetail.breakGauge;
                        OnRoundEndSinking(buf);
                        if (bp == buf._owner.breakDetail.breakGauge)
                        {
                            flag = false;
                        }
                    }

                    if (!flag)
                    {
                        var reducedValue = (buf.stack * 2) / 3;
                        var reduceValue = buf.stack - reducedValue;
                        totalDmg += GetSinkingDmg(buf);
                        RunCatching("ReduceStack", () => {
                            var value = reduceValue;
                            buf.OnTakeSinkingReduceStack(ref value, reduceValue);
                            foreach (var listener in BattleInterfaceCache.Of<IHandleTakeSinking>(buf._owner))
                            {
                                listener.OnTakeSinkingReduceStack(buf, ref value, reduceValue);
                            }
                            buf.stack -= value;
                            buf.OnAddBuf(-value);
                            if (buf.stack <= 0) buf.Destroy();
                        });
                    }
                }

                if (totalDmg > 0)
                {
                    buf._owner.TakeDamage(totalDmg, DamageType.Buf, attacker, buf.bufType);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}