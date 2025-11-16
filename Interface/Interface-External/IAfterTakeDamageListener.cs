using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    [Obsolete("Please Use Instead IHandleTakeDamage", false)]
    public interface IAfterTakeDamageListener : ILoABattleEffect
    {
        void AfterTakeDamage(BattleUnitModel attacker, int dmg);
    }
}
