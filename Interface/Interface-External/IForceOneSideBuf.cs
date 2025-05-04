using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 조건을 만족하는 경우 합을 강제로 처리하지 않는다.
    /// </summary>
    public interface IForceOneSideBuf : ILoABattleEffect
    {
        bool IsForceOneSideAction(BattlePlayingCardDataInUnitModel myCard, BattlePlayingCardDataInUnitModel targetCard);
    }
}
