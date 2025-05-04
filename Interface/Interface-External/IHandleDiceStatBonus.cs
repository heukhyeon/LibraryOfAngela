using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleDiceBehavior.ApplyDiceStatBonus(DiceStatBonus)"/> 에 대한 파라미터 값을 조정 할 수 있다.
    /// </summary>
    public interface IHandleDiceStatBonus : ILoABattleEffect
    {
        DiceStatBonus ConvertDiceStatBonus(BattleDiceBehavior behaviour, DiceStatBonus origin);
    }
}
