using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IUseInstanceListener : ILoABattleEffect
    {
        void OnUseInstance(BattleDiceCardModel card, BattleUnitModel targetUnit);


    }
}
