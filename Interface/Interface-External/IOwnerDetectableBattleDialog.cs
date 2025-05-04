using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleDialogueModel"/> 에 정의시 현재 주인을 알 수 있다.
    /// </summary>
    public interface IOwnerDetectableBattleDialog
    {
        UnitDataModel Owner { get; set; }
    }
}
