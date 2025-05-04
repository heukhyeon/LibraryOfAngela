using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 합이나 일방 공격 발동시 그것을 구독하고 필요시 강제로 다른 결과로 바꿀수 있다.
    /// </summary>
    public interface IHandleParryingOnesideController : ILoABattleEffect
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <returns>null 이나 current를 그대로 반환시 그냥 그대로 실행한다. 그 외의 유효한 값을 발행시 실제로 합을 진행시킨다.</returns>
        ParryingOneSideAction HandleParryingOneside(ParryingOneSideAction current);
    }
}
