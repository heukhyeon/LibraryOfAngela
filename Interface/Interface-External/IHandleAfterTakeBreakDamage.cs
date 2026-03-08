using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleAfterTakeBreakDamage : ILoABattleEffect
    {
        /// <summary>
        /// 흐트러짐 게이지가 감소된 직후 호출됩니다. <see cref="BattleUnitModel.IsStraighten"/>나 <see cref="BattleUnitBreakDetail.LoseBreakLife(BattleUnitModel)"/>의 호출 전에 호출됩니다.
        /// </summary>
        /// <param name="originDmg"><see cref="BattleUnitModel.TakeBreakDamage(int, DamageType, BattleUnitModel, AtkResist, KeywordBuf)"/> 최초 호출시 받을 예정이었던 피해량입니다. </param>
        /// <param name="resultDmg"><see cref="BattleUnitModel.GetBreakDamageReductionAll(int, DamageType, BattleUnitModel)"/> 따위로 증감된 최종 피해량입니다. </param>
        /// <param name="type">피해 타입입니다.</param>
        /// <param name="attacker">피해를 입히는 공격자입니다. null 일수 있습니다.</param>
        /// <param name="keyword">피해 타입이 <see cref="DamageType.Buf"/>인 경우 그 버프의 타입 (화상 등)입니다.</param>
        void AfterTakeBreakDamage(int originDmg, int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword);
    }
}
