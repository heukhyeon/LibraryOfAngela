using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="PassiveAbilityBase.GetBreakDamageReductionAll(int, DamageType, BattleUnitModel)"/> 에 대해 버프등 다른 곳에서도 적용시킬수있게 하는 인터페이스
    /// </summary>
    public interface IGetBreakDamageReductionAll : ILoABattleEffect
    {
        int GetBreakDamageReductionAll(int dmg, DamageType dmgType, BattleUnitModel attacker);
    }
}
