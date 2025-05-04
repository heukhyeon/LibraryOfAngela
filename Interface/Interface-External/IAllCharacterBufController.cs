using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 캐릭터가 생존해있는한 모든 캐릭터에게 버프를 제공해야하는 경우에 사용합니다.
    /// 호출 횟수의 최적화를 위해 이 효과는 <see cref="PassiveAbilityBase"/>, <see cref="EmotionCardAbilityBase"/> 에만 적용됩니다.
    /// </summary>
    [Obsolete("Try Instead IHandleNewCharacter")]
    public interface IAllCharacterBufController : ILoABattleEffect
    {
        List<BattleUnitBuf> bufs { get; }
        BattleUnitBuf CreateBuf(BattleUnitModel target);
    }
}
