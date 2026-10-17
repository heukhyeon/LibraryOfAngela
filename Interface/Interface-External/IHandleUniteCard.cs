using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 자신의 전투 책장을 결속 책장으로 취급할지 여부를 오버라이드한다. 전투 책장의 효과가 정의되지 않았다면 호출되지 않는다
    /// </summary>
    public interface IHandleUniteCard : ILoABattleEffect
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="card">대상 전투 책장</param>
        /// <param name="ability">대상 전투 책장 효과</param>
        /// <param name="origin">원래 결속 책장 여부</param>
        /// <param name="result">최종 결속 책장 여부</param>
        void OnCheckUniteCard(BattleDiceCardModel card, DiceCardSelfAbilityBase ability, bool origin, ref bool result);
    }
}
