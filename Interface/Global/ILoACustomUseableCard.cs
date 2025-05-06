using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ILoACustomUseableCard
{
    bool IsUseable(BattleUnitModel owner);

    void OnHandle(BattleDiceCardUI ui, BattleUnitModel owner, BattleDiceCardModel card);
}
