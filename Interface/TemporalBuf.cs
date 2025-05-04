using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    /// <summary>
    /// 버프로 처리하는 효과 구현을 위한 임시 버프
    /// 
    /// 막 종료시 자동으로 파괴.
    /// </summary>
    public class TemporalBuf : BattleUnitBuf
    {
        public override bool Hide => true;

        /// <summary>
        /// <see cref="BattleUnitBuf.OnRollDice(BattleDiceBehavior)"/> 시에 자동으로 주입할 스탯 보너스
        /// </summary>
        public DiceStatBonus statBonus;


        public Action<BattleDiceBehavior> onRollDice = null;

        public KeywordBuf multiplierBufType = KeywordBuf.None;

        public int multiplierBufStack = 1;

        public override int GetMultiplierOnGiveKeywordBufByCard(BattleUnitBuf cardBuf, BattleUnitModel target)
        {
            if (multiplierBufType != KeywordBuf.None && cardBuf.bufType == multiplierBufType) return multiplierBufStack;

            return base.GetMultiplierOnGiveKeywordBufByCard(cardBuf, target);
        }

        public override void OnRollDice(BattleDiceBehavior behavior)
        {
            base.OnRollDice(behavior);
            if (statBonus != null) behavior.ApplyDiceStatBonus(statBonus);
            onRollDice?.Invoke(behavior);
        }

        public override void OnRoundEnd()
        {
            base.OnRoundEnd();
            Destroy();
        }
    }
}
