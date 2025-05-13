using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleTakePoise : ILoABattleEffect
    {
        /// <summary>
        /// 자신에게 부여된 호흡의 수치 감소가 발생할때 호출
        /// </summary>
        void OnTakePoiseReduceStack(BattleUnitBuf_loaPoise buf, ref int value, int originValue);

        /// <summary>
        /// 자신에게 부여된 호흡의 크리티컬 발생 확률을 계산할때 호출. 수비주사위에 대해서도 호출되며 이 경우는 기본 chance가 0임
        /// </summary>
        void BeforeJudgingCritical(BattleDiceBehaviour behaviour, BattleUnitBuf_loaPoise buf, ref float criticalChance, float originalChance);

        /// <summary>
        /// 크리티컬 발생시 호출
        /// </summary>
        void OnCriticalActivated(BattleDiceBehaviour behaviour, BattleUnitBuf_loaPoise buf, ref float currentDmgRate, float originalDmgRate, ref float currentBreakDmgRate, float originalBreakDmgRate) {

        }
    }
}