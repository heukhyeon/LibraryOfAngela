using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="EmotionCardAbilityBase.OnGetEmotionCoin(EmotionCoinType)"/> 과 같은 타이밍에 다른 효과들이 수신될 수 있게한다.
    /// 환상체 책장은 기본 구현이므로 의미가 없다.
    /// </summary>
    public interface IHandleOnGetEmotionCoin : ILoABattleEffect
    {
        void OnGetEmotionCoin(EmotionCoinType coin);
    }
}
