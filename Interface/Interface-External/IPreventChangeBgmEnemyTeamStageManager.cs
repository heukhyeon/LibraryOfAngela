using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IPreventChangeBgmEnemyTeamStageManager
    {

        bool IsPreventChangeBgm(ChangeBgmType type);
    }

    public enum ChangeBgmType
    {
        Ally = 0,
        Enemy = 1,
        Current = 2
    }
}
