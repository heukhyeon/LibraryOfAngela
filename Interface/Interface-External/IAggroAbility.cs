using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 합을 강제로 가져올 수 있습니다.
    /// 
    /// 예외적으로 이 책장은 현재 선택된 책장에 한해 <see cref="DiceCardSelfAbilityBase"/>에 대해서도 호출됩니다.
    /// </summary>
    public interface IAggroAbility : ILoABattleEffect
    {
        bool CanForcelyAggro(BattleUnitModel target, int myIndex, int targetIndex);
    }
}
