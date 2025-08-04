using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleTakeSinking : ILoABattleEffect
    {
        /// <summary>
        /// 자신에게 부여된 침잠이 턴 종료에 의해 수치 감소가 발생할때 호출
        /// </summary>
        void OnTakeSinkingReduceStack(BattleUnitBuf_loaSinking buf, LoAKeywordBufReduceRequest request, ref int value);

        /// <summary>
        /// 자신에게 부여된 침잠에 의해 흐트러짐 피해를 받을때 피해량 제어
        /// </summary>
        void BeforeTakeSinkingBreakDamage(BattleUnitBuf_loaSinking buf, int originDmg, ref int dmg);

        /// <summary>
        /// 자신에게 부여된 침잠에 의해 흐트러짐 피해를 받은 경우 호출
        /// </summary>
        void OnTakeSinkingBreakDamage(BattleUnitBuf_loaSinking buf, int dmg);

        /// <summary>
        /// 자신에게 부여된 침잠 흐트러짐 피해에 의해 흐트러질경우 호출
        /// </summary>
        void OnBreakStateBySinking(BattleUnitModel actor, BattleUnitBuf_loaSinking buf);
    }
}