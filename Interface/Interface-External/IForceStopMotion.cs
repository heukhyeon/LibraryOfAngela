using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BehaviourActionBase"/> 에 구현시 주사위를 굴리기전 캐릭터가 이동하지 않습니다.
    /// 
    /// 이 인터페이스는 정의만 처리하며, 실제 정지 여부 유무는 <see cref="BehaviourActionBase.IsMovable(BattleCardBehaviourResult, BattleCardBehaviourResult)"/> 을 따릅니다.
    /// </summary>
    public interface IForceStopMotion
    {

    }
}
