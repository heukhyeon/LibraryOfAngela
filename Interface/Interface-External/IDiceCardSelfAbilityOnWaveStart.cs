using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="DiceCardSelfAbilityBase"/> 에 구현시 무대 시작시 콜백을 받을 수 있다.
    /// 
    /// 이 호출은 다른 OnWaveStart 호출 이전에 이루어진다.
    /// </summary>
    public interface IDiceCardSelfAbilityOnWaveStart
    {
        void OnWaveStart(BattleUnitModel owner, BattleDiceCardModel card);
    }
}
