using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 반격 주사위가 생성되었을때 해당 주사위에 대한 제어를 추가할 수 있습니다.
    /// - 시점상 <see cref="DiceCardAbilityBase"/> 는 이 인터페이스를 정의해도 호출되지 못합니다.
    /// </summary>
    public interface IHandleStandbyDice : ILoABattleEffect
    {

        void OnStandbyBehaviour(List<BattleDiceBehavior> behaviours, BattlePlayingCardDataInUnitModel card);
    }
}
