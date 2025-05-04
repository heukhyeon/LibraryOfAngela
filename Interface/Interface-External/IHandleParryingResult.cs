using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 합의 결과를 강제로 조정,
    /// 
    /// 1. 한 명이 여러 효과에 이 인터페이스를 정의한경우, 가장 유리한 결과를 적용합니다. (승리 -> 무승부 -> 패배)
    /// 2. 양 쪽 모두 이 인터페이스를 구현하고, 모두 원본의 결과와 다를 경우 적쪽을 우선시한다.
    /// </summary>
    public interface IHandleParryingResult : ILoABattleEffect
    {
        Result? OnDecisionResult(Result currentResult);
    }
}
