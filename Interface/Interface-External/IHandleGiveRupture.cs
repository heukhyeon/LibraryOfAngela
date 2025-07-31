using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 자신이 대상의 파열을 발동시킬때 주요 효과들을 제어.
    /// </summary>
    public interface IHandleGiveRupture : ILoABattleEffect
    {
        /// <summary>
        /// 자신의 공격에 의해 파열 수치가 감소할때 호출
        /// </summary>
        void OnGiveRuptureReduceStack(BattleUnitBuf_loaRupture buf, ref int value, int originValue);

        /// <summary>
        /// 자신의 공격에 의해 파열 피해를 줄 때 피해량 제어
        /// </summary>
        void BeforeGiveRuptureDamage(BattleUnitBuf_loaRupture buf, ref int dmg, int originDmg);

        /// <summary>
        /// 자신의 공격에 의해 파열 피해를 준 경우 호출
        /// </summary>
        void OnGiveRuptureDamage(BattleUnitBuf_loaRupture buf, int dmg);

        /// <summary>
        /// 자신의 공격에 의해 대상의 파열이 발동해 사망한경우 호출
        /// </summary>
        void OnKillByRupture(BattleUnitBuf_loaRupture buf);
    }
}