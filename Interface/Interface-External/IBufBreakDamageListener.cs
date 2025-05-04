using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitBuf"/> 에 구현시 흐트러짐 피해를 입을때 콜백을 받을 수 있다.
    /// </summary>
    public interface IBufBreakDamageListener : ILoABattleEffect
    {
        void OnTakeBreakDamageByAttack(BattleDiceBehavior atkDice, int breakdmg);
    }
}
