using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleTakeRupture : ILoABattleEffect
    {
        /// <summary>
        /// 자신에게 부여된 파열의 수치 감소가 발생할때 호출
        /// </summary>
        void OnTakeRuptureReduceStack(BattleUnitModel actor, BattleUnitBuf_loaRupture buf, ref int value, int originValue);

        /// <summary>
        /// 자신에게 부여된 파열에 의해 피해를 받을때 피해량 제어
        /// </summary>
        void BeforeTakeRuptureDamage(BattleUnitBuf_loaRupture buf, ref int dmg, int originDmg);

        /// <summary>
        /// 자신에게 부여된 파열에 의해 피해를 받은 경우 호출
        /// </summary>
        void OnTakeRuptureDamage(BattleUnitBuf_loaRupture buf, int dmg);

        /// <summary>
        /// 자신에게 부여된 파열에 의해 사망한경우 호출
        /// </summary>
        void OnDieByRupture(BattleUnitModel actor, BattleUnitBuf_loaRupture buf);
    }
}