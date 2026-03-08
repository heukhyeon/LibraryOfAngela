using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleBeforeTakeDamage : ILoABattleEffect
    {
        /// <summary>
        /// 피해를 받을때 그 처리전에 호출됩니다.
        /// </summary>
        /// <param name="originDamage">현재 받을 피해량입니다. <see cref="BattleUnitModel.GetDamageReductionAll"/> 까지 처리된 피해량입니다.</param>
        /// <param name="resultDamage">이 인터페이스를 거쳐 증가되거나 감량된 최종 피해량입니다.</param>
        /// <param name="type">피해 타입입니다.</param>
        /// <param name="attacker">피해를 입히는 공격자입니다. null 일수 있습니다.</param>
        /// <param name="keyword">피해 타입이 <see cref="DamageType.Buf"/>인 경우 그 버프의 타입 (화상 등)입니다.</param>
        void BeforeTakeDamage(int originDamage, ref int resultDamage, DamageType type, BattleUnitModel attacker, KeywordBuf keyword);
    }
}
