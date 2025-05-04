using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="SceneBuf"/> 에 구현시 버프의 스택 감소를 무시한다. 
    /// </summary>
    public interface IPreventBufStack : ILoABattleEffect
    {
        bool IsStackImmune(BattleUnitBuf buf, BattleUnitModel owner);
    }
}
