using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 책장 사용 종료시의 콜백을 받을 수 있다.
    /// 
    /// <see cref="DiceCardSelfAbilityBase"/> 는 자체 콜백이 있으니 의미가 없고, 시점상 <see cref="BattleDiceBehavior"/> 는 이 콜백을 받을 수 없다.
    /// </summary>
    public interface IEndUseCard : ILoABattleEffect
    {
        void OnEndBattle(BattlePlayingCardDataInUnitModel card);
    }
}
