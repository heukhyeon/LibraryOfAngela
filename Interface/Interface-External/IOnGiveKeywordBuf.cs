using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 효과를 부여했을때 actor 에게 호출되는 효과
    /// 
    /// </summary>
    public interface IOnGiveKeywordBuf : ILoABattleEffect
    {
        void OnGiveKeywordBuf(BattleUnitModel target, BattleUnitBuf buf, int stack, BufReadyType readyType, bool isCard);

    }
}
