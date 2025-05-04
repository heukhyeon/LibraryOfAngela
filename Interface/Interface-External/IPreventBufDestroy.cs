using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="EmotionCardAbilityBase"/> 에 정의시 버프의 파괴 여부를 컨트를 할 수 있다.
    /// </summary>
    public interface IPreventBufDestroy : ILoABattleEffect
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <returns>true 인 경우 해당 버프의 파괴 여부를 무시한다.
        /// 함수 호출중 에러가 발생 할 경우 false 로 처리된다.</returns>
        bool IsPreventBufDestroyed(BattleUnitBuf buf);
    }
}
