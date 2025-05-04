using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workshop;

namespace LibraryOfAngela.Extension
{
    public static class BattleExtension
    {
        /// <summary>
        /// 다음 막 시작시 빛을 회복합니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="playPoint"></param>
        public static void ReservePlayPoint(this BattlePlayingCardSlotDetail owner, int playPoint)
        {
            if (owner == null) return;

            owner._self.bufListDetail.AddBuf(new ReserveRecoverBuf
            {
                playPoint = playPoint,
                isRoundEnd = false
            });
        }

        /// <summary>
        /// 이번 막 종료시 빛을 회복합니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="playPoint"></param>
        public static void ReservePlayPointAtEnd(this BattlePlayingCardSlotDetail owner, int playPoint)
        {
            if (owner == null) return;

            owner._self.bufListDetail.AddBuf(new ReserveRecoverBuf
            {
                playPoint = playPoint,
                isRoundEnd = true
            });
        }

        /// <summary>
        /// 다음 막 시작시 책장을 뽑습니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="card"></param>
        public static void ReserveDrawCard(this BattleAllyCardDetail owner, int card)
        {
            if (owner == null) return;

            owner._self.bufListDetail.AddBuf(new ReserveRecoverBuf
            {
                draw = card,
                isRoundEnd = false
            });
        }

        /// <summary>
        /// 이번 막 종료시 책장을 뽑습니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="card"></param>
        public static void ReserveDrawCardAtEnd(this BattleAllyCardDetail owner, int card)
        {
            if (owner == null) return;

            owner._self.bufListDetail.AddBuf(new ReserveRecoverBuf
            {
                draw = card,
                isRoundEnd = true
            });
        }

        public static void SafeRecoverBreak(this BattleUnitModel owner, int value)
        {
            try
            {
                owner.breakDetail.RecoverBreak(value);
            }
            catch
            {

            }
        }

        public static void SafeDestroyDice(this BattlePlayingCardDataInUnitModel owner, Predicate<DiceMatch> match, DiceUITiming timing)
        {
            var remainDice = owner.cardBehaviorQueue.Count > 0;
            owner.DestroyDice(match, timing == DiceUITiming.AttackAfter && !remainDice ? DiceUITiming.Start : timing);
        }
    
        public static void InjectAddtionalResult(this BattleCardTotalResult owner, string title, string desc, EffectTypoCategory category)
        {
            var target = owner?.playingCard?.owner ?? StageController.Instance._oneSideList.Find(x => x?.target?.battleCardResultLog == owner)?.target;
            if (target == null) return;
            ServiceLocator.Instance.GetInstance<ILoARoot>().InjectAddtionalLog(target, title, desc, category);
        }

        public static bool HasEmotion(this BattleUnitEmotionDetail owner, string script)
        {
            return owner.PassiveList.Any(x => x.XmlInfo.Script.FirstOrDefault() == script);
        }

        public static T InjectBuf<T>(this BattleUnitBufListDetail owner, int stack = 0, BufReadyType type = BufReadyType.ThisRound) where T : BattleUnitBuf, new()
        {
            if (owner is null) return null;

            var targetBufList = type == BufReadyType.ThisRound ? owner._bufList : type == BufReadyType.NextRound ? owner._readyBufList :
                owner._readyReadyBufList;

            foreach(var buf in targetBufList)
            {
                if (buf.IsDestroyed()) continue;
                if (buf is T)
                {
                    if (stack != 0)
                    {
                        var s = owner.ModifyStack(buf, stack);
                        buf.stack += s;
                        buf.OnAddBuf(s);
                    }
                    return buf as T;
                }
            }

            var b = new T();
            targetBufList.Add(b);
            b.Init(owner._self);
            b.stack = owner.ModifyStack(b, stack);
            b.OnAddBuf(b.stack);
            if (stack != 0 && b.stack == 0)
            {
                b.Destroy();
                targetBufList.Remove(b);
            }
            return b;
        }
    
        public static BattleUnitBuf InjectBuf(this BattleUnitBufListDetail owner, Type type, int stack = 0, BufReadyType readyType = BufReadyType.ThisRound)
        {
            if (owner is null) return null;

            var targetBufList = readyType == BufReadyType.ThisRound ? owner._bufList : readyType == BufReadyType.NextRound ? owner._readyBufList :
                owner._readyReadyBufList;

            foreach (var buf in targetBufList)
            {
                if (buf.IsDestroyed()) continue;
                if (buf.GetType() == type)
                {
                    if (stack != 0)
                    {
                        var s = owner.ModifyStack(buf, stack);
                        buf.stack += s;
                        buf.OnAddBuf(s);
                    }
                    return buf;
                }
            }

            var b = Activator.CreateInstance(type) as BattleUnitBuf;
            targetBufList.Add(b);
            b.Init(owner._self);
            b.stack = owner.ModifyStack(b, stack);
            b.OnAddBuf(b.stack);
            if (stack != 0 && b.stack == 0)
            {
                b.Destroy();
                targetBufList.Remove(b);
            }
            return b;
        }
        /// <summary>
        /// 파라미터로 넘긴 책장을 위치에 상관없이 뽑아 패에 추가합니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="cards">실제로 뽑은 책장입니다.</param>
        /// <returns></returns>
        public static List<BattleDiceCardModel> DrawSpecificCards(this BattleAllyCardDetail owner, List<BattleDiceCardModel> cards)
        {
            var ret = new List<BattleDiceCardModel>();
            try
            {
                foreach (var c in cards)
                {
                    bool flag = DrawAdd(false, owner._cardInDiscarded, ret, c);
                    if (!flag) flag = DrawAdd(flag, owner._cardInDeck, ret, c);
                    if (!flag) DrawAdd(flag, owner._cardInUse, ret, c);
                }

                int i = 0;
                while (i < ret.Count)
                {
                    if (owner._cardInHand.Count >= owner.maxHandCount)
                    {
                        ret.RemoveAt(i);
                        continue;
                    }
                    owner._cardInHand.Add(ret[i]);
                    i++;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            } 
            return ret;
        }

        /// <summary>
        /// 광역 사용 등을 고려해 현재 내가 주사위를 굴릴때 대응하는 "대상"을 반환합니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static KeyValuePair<BattleUnitModel, BattlePlayingCardDataInUnitModel> FindMyCurrentTarget(this BattleUnitModel owner)
        {
            if (BattleFarAreaPlayManager.Instance.isRunning)
            {
                if (BattleFarAreaPlayManager.Instance.attacker == owner)
                {
                    var card = owner.currentDiceAction;
                    var targetCard = BattleFarAreaPlayManager.Instance.victims.Find(d => d.unitModel == card.target);
                    return new KeyValuePair<BattleUnitModel, BattlePlayingCardDataInUnitModel>(card.target, targetCard?.playingCard);
                }
                else
                {
                    var unit = BattleFarAreaPlayManager.Instance.attacker;
                    return new KeyValuePair<BattleUnitModel, BattlePlayingCardDataInUnitModel>(unit, unit.currentDiceAction);
                }
            }
            if (StageController.Instance.phase == StageController.StagePhase.ExecuteParrying)
            {
                var my = BattleParryingManager.Instance._teamLibrarian.unit == owner ? BattleParryingManager.Instance._teamLibrarian :
                    BattleParryingManager.Instance._teamEnemy;

                return new KeyValuePair<BattleUnitModel, BattlePlayingCardDataInUnitModel>(my.opponent.unit, my.opponent.playingCard);
            }
            if (BattleOneSidePlayManager.Instance.attacker == owner)
            {
                var card = owner.currentDiceAction;
                return new KeyValuePair<BattleUnitModel, BattlePlayingCardDataInUnitModel>(card.target, null);
            }
            else
            {
                return new KeyValuePair<BattleUnitModel, BattlePlayingCardDataInUnitModel>(BattleOneSidePlayManager.Instance.attacker, BattleOneSidePlayManager.Instance.attacker.currentDiceAction);
            }
        }

        private static bool DrawAdd(bool flag, List<BattleDiceCardModel> deck, List<BattleDiceCardModel> ret, BattleDiceCardModel target)
        {
            if (flag) return flag;
            foreach (var card in deck)
            {
                if (target == card)
                {
                    deck.Remove(card);
                    ret.Add(card);
                    return true;
                }
            }
            return false;
        }

        public static void GiveOtherUnitGiveEmotion(this BattleUnitModel owner, BattleUnitModel target, EmotionCoinType type, int cnt = 1)
        {
            if (cnt <= 0 || owner is null || target is null) return;
            var i = target.emotionDetail.CreateEmotionCoin(type, cnt);

            if (StageController.Instance.IsLogState())
            {
                bool flag = false;
                owner?.battleCardResultLog?.SetAfterActionEvent(() =>
                {
                    if (flag) return;
                    flag = true;
                    SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(target, type, i);
                });
            }
            else
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(target, type, i);
            }
        }

    }

    internal class ReserveRecoverBuf : BattleUnitBuf
    {
        public override bool Hide => true;

        public int playPoint = 0;
        public int draw = 0;
        public bool isRoundEnd = false;

        public override void OnRoundStartAfter()
        {
            base.OnRoundStartAfter();
            if (!isRoundEnd)
            {
                Recover();
            }
        }

        public override void OnRoundEnd()
        {
            base.OnRoundEnd();
            if (isRoundEnd)
            {
                Recover();
            }
        }

        private void Recover()
        {
            if (playPoint <= 0 && draw <= 0) return;
            if (playPoint > 0)
            {
                _owner.cardSlotDetail.RecoverPlayPoint(playPoint);
                playPoint = -1;
            }
            if (draw > 0)
            {
                _owner.allyCardDetail.DrawCards(draw);
                draw = -1;
            }
            Destroy();
        }
    }
}
