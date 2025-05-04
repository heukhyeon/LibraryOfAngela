using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 페이즈 변경시 해당 페이즈를 임의로 변경 할 수 있다.
    /// 이 인터페이스는 막 종료시마다 다시 호출된다. 따라서 이 인터페이스를 구현시 적절히 Disable 해줄수 있어야한다.
    /// </summary>
    public interface IHandleCustomPhase : ILoABattleEffect
    {
        StageController.StagePhase HandleCustomPhase(StageController.StagePhase current);
    }
}
