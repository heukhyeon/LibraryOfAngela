using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 책장이 패에 들어온 경우 감지하는 리스너
    /// 이 인터페이스는 <see cref="DiceCardSelfAbilityBase"/> 에 정의할경우 스스로에 한해서 호출된다.
    /// </summary>
    public interface IAddCardToHandListener : ILoABattleEffect
    {
        /// <summary>
        /// <see cref="BattleAllyCardDetail.AddCardToHand(BattleDiceCardModel, bool)"/> 처리후 호출된다.
        /// </summary>
        /// <param name="card">손에 추가된 카드</param>
        /// <param name="owner">대상 유닛</param>
        /// <param name="front">책장이 앞에 추가됬는지 여부</param>
        void OnAddToHand(BattleDiceCardModel card, BattleUnitModel owner, bool front);
    }
}
