using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitBuf"/> 에 구현시 자신에게 부여되는 효과에 대한 수치를 제어한다.
    /// </summary>
    public interface IHandleModifyBufStack : ILoABattleEffect
    {
        /// <summary>
        /// 제어하고싶지 않은 버프라면 currentResult 를 반환한다.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="stack"></param>
        /// <param name="currentResult"></param>
        /// <returns></returns>
        int ModifyStack(BattleUnitBuf buf, int stack, int currentResult);
    }
}
