using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 체력이나 흐트러짐을 회복하지 못했을때 호출되는 함수
    /// </summary>
    public interface IPreventRecover : ILoABattleEffect
    {
        void OnRecoverHpFailed(int v);

        void OnRecoverBreakFailed(int v);
    }
}
