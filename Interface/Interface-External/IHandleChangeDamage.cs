using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 피해를 주기 전에 데미지를 변경합니다.
    /// 
    /// </summary>
    public interface IHandleChangeDamage : ILoABattleEffect
    {
        void HandleDamage(int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf buf);

        void HandleBreakDamage(int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf buf);
    }
}
