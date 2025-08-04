using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 자신에게 부여된 진동에 대한 제어를 수행할때 사용합니다.
    /// 주의 : 커스텀 진동을 구현한다면 그 진동에 이 인터페이스를 구현하지 마세요. 이미 <see cref="BattleUnitBuf_loaPoise"/>에는 이 인터페이스에 대응하는 모든 메소드가 존재합니다.
    /// </summary>
    public interface IHandleTakePoise : ILoABattleEffect
    {
        /// <summary>
        /// 자신에게 부여된 호흡의 수치 감소가 발생할때 호출
        /// </summary>
        void OnTakePoiseReduceStack(BattleUnitBuf_loaPoise buf, LoAKeywordBufReduceRequest request, ref int value);

        /// <summary>
        /// 자신에게 부여된 호흡의 크리티컬 발생 확률을 계산할때 호출. 수비주사위에 대해서도 호출되며 이 경우는 기본 chance가 0임
        /// </summary>
        void BeforeJudgingCritical(BattleDiceBehavior behaviour, BattleUnitBuf_loaPoise buf, ref float criticalChance, float originalChance);

        /// <summary>
        /// 크리티컬 발생시 호출
        /// </summary>
        void OnCriticalActivated(BattleDiceBehavior behaviour, BattleUnitBuf_loaPoise buf, ref float currentDmgRate, float originalDmgRate, ref float currentBreakDmgRate, float originalBreakDmgRate);
    }
}