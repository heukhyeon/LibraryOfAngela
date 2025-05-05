using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleCreateDiceCardBehaviourList : ILoABattleEffect
    {
        /// <summary>
        /// 전투책장의 주사위가 만들어질때 호출됩니다. Lor은 광역 책장에 대응하는경우 여러번 이 함수가 호출될수 있습니다.
        /// </summary>
        /// <param name="card">대상 속도 주사위에 장착된 전투책장 카드 정보입니다.</param>
        /// <param name="dices">현재 만들어진 주사위 목록입니다. 반격 주사위를 포함합니다.</param>
        void OnCreateDiceCardBehaviorList(BattlePlayingCardDataInUnitModel card, List<BattleDiceBehavior> dices);
    }
}
