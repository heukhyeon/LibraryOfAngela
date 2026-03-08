using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleAfterGiveBreakDamage : ILoABattleEffect
    {
        /// <summary>
        /// 흐트러짐 게이지가 감소된 직후 호출됩니다. <see cref="BattleUnitModel.IsStraighten"/>나 <see cref="BattleUnitBreakDetail.LoseBreakLife(BattleUnitModel)"/>의 호출 전에 호출됩니다.
        /// </summary>
        /// <param name="originDmg"><see cref="BattleUnitModel.TakeBreakDamage(int, DamageType, BattleUnitModel, AtkResist, KeywordBuf)"/> 최초 호출시 줄 예정이었던 피해량입니다. </param>
        /// <param name="resultDmg"><see cref="BattleUnitModel.GetBreakDamageReductionAll(int, DamageType, BattleUnitModel)"/> 따위로 증감된 최종 피해량입니다. </param>
        /// <param name="type">피해 타입입니다.</param>
        /// <param name="target">피해를 받는 피격자입니다</param>
        /// <param name="keyword">피해 타입이 <see cref="DamageType.Buf"/>인 경우 그 버프의 타입 (화상 등)입니다.</param>
        void AfterGiveBreakDamage(int originDmg, int resultDmg, DamageType type, BattleUnitModel target, KeywordBuf keyword);
    }
}
