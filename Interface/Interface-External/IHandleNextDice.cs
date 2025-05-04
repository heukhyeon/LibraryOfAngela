using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattlePlayingCardDataInUnitModel.NextDice"/> 의 타이밍에 다음 굴릴 주사위를 제어할 수 있다.
    /// </summary>
    public interface IHandleNextDice : ILoABattleEffect
    {
        IEnumerable<BattleDiceBehavior> BeforeNextDice(BattlePlayingCardDataInUnitModel card);
    }
}
