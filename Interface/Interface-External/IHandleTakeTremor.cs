using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 자신에게 부여된 진동에 대한 제어를 수행할때 사용합니다.
    /// 주의 : 커스텀 진동을 구현한다면 그 진동에 이 인터페이스를 구현하지 마세요. 이미 <see cref="BattleUnitBuf_loaTremor"/>에는 이 인터페이스에 대응하는 모든 메소드가 존재합니다.
    /// </summary>
    public interface IHandleTakeTremor : ILoABattleEffect
    {
        /// <summary>
        /// 자신에게 진동폭발이 발생한 경우 때 진동 폭발 흐트러짐 피해를 적용하기 전에 호출
        /// </summary>
        void BeforeTakeTremorBurst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, ref int dmg, int originDmg);

        /// <summary>
        /// 자신에게 진동폭발이 발생한 경우 호출
        /// </summary>
        void OnTakeTremorBurst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, int dmg, bool isCard);

        /// <summary>
        /// 자신에게 진폭변환이 발생할 경우 호출
        /// </summary>
        void OnTakeTremorTransform(BattleUnitModel actor, BattleUnitBuf_loaTremor previous, BattleUnitBuf_loaTremor next);

        /// <summary>
        /// 자신의 진동의 수치감소가 발생할 경우 호출
        /// </summary>
        void OnTakeTremorReduceStack(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, ref int value, int originValue, bool isFromRoundEnd);

        /// <summary>
        /// 자신이 진동폭발의 처리도중 흐트러진경우 호출
        /// </summary>
        void OnBreakStateByTremorBurst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, bool isCard);

        /// <summary>
        /// 자신이 진동폭발의 처리도중 사망한경우 호출
        /// </summary>
        void OnDieByTremorBurst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, bool isCard);
    }
}