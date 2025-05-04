using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 이 인터페이스를 정의하는 개별 인터페이스를 아래의 클래스를 상속하는 클래스들에 정의시 대응하는 효과를 얻을 수 있습니다.
    /// - <see cref="PassiveAbilityBase"/>
    /// - <see cref="BattleUnitBuf"/>
    /// - <see cref="EmotionCardAbilityBase"/>
    /// - <see cref="DiceCardSelfAbilityBase"/>
    /// - <see cref="DiceCardAbilityBase"/>
    /// 
    /// 단, 각각의 클래스를 무엇으로 정의하느냐에 따라서 각각의 효과를 호출받을수있는 유효 생명주기가 다릅니다.
    /// 
    /// - <see cref="PassiveAbilityBase"/> : 해당 패시브가 유효한 동안 (생성 ~ 파괴)
    /// - <see cref="BattleUnitBuf"/> : 해당 버프가 유효한 동안 (Ready, Destroy 상태일때는 호출되지 않습니다)
    /// - <see cref="EmotionCardAbilityBase"/> : 해당 환상체 카드 효과가 유효한 동안 (거의 항상이라 생각해주셔도 됩니다)
    /// - <see cref="DiceCardSelfAbilityBase"/> : 해당 책장을 사용하는 동안
    /// - <see cref="DiceCardAbilityBase"/> : 해당 주사위를 굴리는동안 (BeforeRollDice ~ AfterAction)
    /// </summary>
    public interface ILoABattleEffect
    {
    }
}
