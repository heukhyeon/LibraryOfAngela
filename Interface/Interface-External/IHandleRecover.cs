using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="EmotionCardAbilityBase"/> 에 구현시 아군의 체력이 회복될때 해당 회복값을 증폭시킬 수 있다.
    /// 
    /// <see cref="BattleUnitBuf"/> 에 구현시 자신의 체력이 회복될때 해당 회복값을 증폭시킬 수 있다.
    /// </summary>
    public interface IHandleRecover : ILoABattleEffect
    {
        /// <summary>
        /// 이 속성이 true 이고, 회복값의 총합이 음수인경우 회복 대신 피해를 준다.
        /// </summary>
        bool isDamageIfNegative { get; }

        /// <summary>
        /// GetHpRecoverBonus(unit, 10) 일때 반환값으로 2가 주어진다면
        /// 해당 유닛은 10 + 2 = 12 의 체력을 회복한다.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        int GetHpRecoverBonus(BattleUnitModel target, int value);

        /// <summary>
        /// GetBreakRecoverBonus(unit, 10) 일때 반환값으로 2가 주어진다면
        /// 해당 유닛은 10 + 2 = 12 의 흐트러짐을 회복한다.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        int GetBreakRecoverBonus(BattleUnitModel target, int value);
    }
}
