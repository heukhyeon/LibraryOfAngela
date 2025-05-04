using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 이 인터페이스는 <see cref="ILoABattleEffect"/> 를 상속하지 않습니다.
    /// </summary>
    public interface IRepeatPersonalCard
    {
        /// <summary>
        /// 책장 사용후 패에 바로 다시 추가할지 여부
        /// </summary>
        /// <returns></returns>
        bool isReturnToHandImmediately(BattleUnitModel owner, BattleDiceCardModel card);
    }
}
