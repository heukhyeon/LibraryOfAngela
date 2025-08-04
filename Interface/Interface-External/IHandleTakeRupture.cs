using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 자신에게 부여된 파열에 대한 제어를 수행할때 사용합니다.
    /// 주의 : 커스텀 파열을 구현한다면 그 진동에 이 인터페이스를 구현하지 마세요. 이미 <see cref="BattleUnitBuf_loaRupture"/>에는 이 인터페이스에 대응하는 모든 메소드가 존재합니다.
    /// </summary>
    public interface IHandleTakeRupture : ILoABattleEffect
    {
        /// <summary>
        /// 자신에게 부여된 파열의 수치 감소가 발생할때 호출
        /// </summary>
        void OnTakeRuptureReduceStack(BattleUnitBuf_loaRupture buf, LoAKeywordBufReduceRequest request, ref int value);

        /// <summary>
        /// 자신에게 부여된 파열에 의해 피해를 받을때 피해량 제어
        /// </summary>
        void BeforeTakeRuptureDamage(BattleUnitBuf_loaRupture buf, int originDmg, ref int dmg);

        /// <summary>
        /// 자신에게 부여된 파열에 의해 피해를 받은 경우 호출
        /// </summary>
        void OnTakeRuptureDamage(BattleUnitBuf_loaRupture buf, int dmg);

        /// <summary>
        /// 자신에게 부여된 파열에 의해 사망한경우 호출
        /// </summary>
        void OnDieByRupture(BattleUnitModel actor, BattleUnitBuf_loaRupture buf);

        /// <summary>
        /// 자신에게 부여된 파열에 대해 막 종료시 소멸할지, 혹은 다른 수단을 처리할지 여부입니다
        /// </summary>
        /// <param name="buf">대상 파열 버프입니다.</param>
        /// <param name="setDestroy">모든 이 메소드를 처리후 파열을 소멸시킬지 결정합니다. 이 값이 false여도 파열이 0 이하라면 소멸합니다.</param>
        void OnRoundEndInRupture(BattleUnitBuf_loaRupture buf, ref bool setDestroy);
    }
}