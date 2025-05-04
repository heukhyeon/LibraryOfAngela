using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitView.ChangeSkin(string)"/> 에 대한 리스너 인터페이스
    /// </summary>
    public interface IHandleChangeSkin : ILoABattleEffect
    {
        void OnChangeSkin(string skinName);
    }
}
