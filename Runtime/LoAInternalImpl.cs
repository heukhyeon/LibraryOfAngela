using LibraryOfAngela.Battle;
using LibraryOfAngela.Buf;
using LibraryOfAngela.Global;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    class LoAInternalImpl : Singleton<LoAInternalImpl>, ILoAInternal
    {
        void ILoAInternal.RegisterLimbusParryingWinDice(LimbusDiceAbility ability)
        {
            BattleResultPatch.RegisterLibmusDice(ability);
        }
    }
}
