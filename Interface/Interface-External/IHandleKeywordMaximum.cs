using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 키워드 버프의 최대 수치가 걸릴때 제어합니다.
    /// </summary>
    public interface IHandleKeywordMaximum : ILoABattleEffect
    {
        /// <summary>
        /// 서로 다른 두 모드가 같은 키워드에 대해 서로 다른 최대값을 반환한다면 높은쪽 사용함
        /// </summary>
        /// <returns></returns>
        int OnKeywordStackMaximum(KeywordBuf buf, int currentMax);

    }
}
