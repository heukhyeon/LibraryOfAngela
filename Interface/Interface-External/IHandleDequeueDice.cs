using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattlePlayingCardDataInUnitModel.DequeueAbilityTest"/> 의 결과값을 제어한다. 흐트러진 상태에게 강제로 주사위를 굴리거나 혹은 자신의 다음 주사위를 강제로 조작할때 사용한다.
    /// </summary>
    public interface IHandleAfterNextDice : ILoABattleEffect
    {
        /// <summary>
        /// <see cref="BattlePlayingCardDataInUnitModel.DequeueAbilityTest"/> 반환후 호출됨
        /// </summary>
        /// <param name="card"> 현재 교전 책장. <see cref="BattleKeepedCardDataInUnitModel"/>일수 있음</param>
        /// <param name="origin">원래 <see cref="BattlePlayingCardDataInUnitModel.DequeueAbilityTest"/>에서 반환된 최초 값</param>
        void OnNextDice(BattlePlayingCardDataInUnitModel card, BattleDiceBehavior origin);
    }
}
