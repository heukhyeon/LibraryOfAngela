using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleGiveTremor : ILoABattleEffect
    {
        /// <summary>
        /// 자신이 진동폭발을 발생시켰을 때 진동 폭발 흐트러짐 피해를 적용하기 전에 호출
        /// </summary>
        void BeforeGiveTremorBurst(BattleUnitBuf_loaTremor buf, ref int dmg, int originDmg);

        /// <summary>
        /// 자신이 진동폭발을 발생시켰을 경우 호출
        /// </summary>
        void OnGiveTremorBurst(BattleUnitBuf_loaTremor buf, int dmg, bool isCard);

        /// <summary>
        /// 자신이 진폭변환을 발생시켰을 경우 호출
        /// </summary>
        void OnGiveTremorTransform(BattleUnitBuf_loaTremor previous, BattleUnitBuf_loaTremor next, bool isCard);

        /// <summary>
        /// 자신이 대상 진동의 수치감소를 발생시켰을 경우 호출
        /// </summary>
        void OnGiveTremorReduceStack(BattleUnitBuf_loaTremor buf, LoAKeywordBufReduceRequest request, ref int value);

        /// <summary>
        /// 자신이 발생시킨 진동폭발의 처리도중 대상이 흐트러진경우 호출
        /// </summary>
        void OnMakeBreakStateByTremorBurst(BattleUnitBuf_loaTremor buf, bool isCard);

        /// <summary>
        /// 자신이 발생시킨 진동폭발의 처리도중 대상이 사망한경우 호출
        /// </summary>
        void OnKillByTremorBurst(BattleUnitBuf_loaTremor buf, bool isCard);
    }
}