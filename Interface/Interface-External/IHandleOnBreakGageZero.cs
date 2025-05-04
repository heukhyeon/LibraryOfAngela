using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitBuf"/> 에 정의하면 흐트러짐 상태가 될때 해당 흐트러짐을 무효화할수있다.
    /// </summary>
    public interface IHandleOnBreakGageZero : ILoABattleEffect
    {
        bool OnBreakGageZero();
    }
}
