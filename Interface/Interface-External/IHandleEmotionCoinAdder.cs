using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 감정 코인 수급이 예정되었을때 그것의 갯수나 타입을 제어한다.
    /// </summary>
    public interface IHandleEmotionCoinAdder : ILoABattleEffect
    {
        /// <summary>
        /// <see cref="BattleUnitEmotionDetail.CreateEmotionCoin(EmotionCoinType, int)"/> 호출시 감정 지급 전에 호출
        /// </summary>
        /// <param name="type">지급할 감정 코인 타입</param>
        /// <param name="current">현재 예정된 감정 지급 수</param>
        /// <param name="origin">최초 <see cref="BattleUnitEmotionDetail.CreateEmotionCoin(EmotionCoinType, int)"/> 호출시 포함된 감정 지급 수</param>
        void OnHandleEmotionCoinAdder(ref EmotionCoinType type, ref int current, int origin);
    }
}
