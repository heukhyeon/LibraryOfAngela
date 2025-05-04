using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 아군이 환상체 카드를 획득했을때 콜백을 받을 수 있다.
    /// </summary>
    public interface IEmotionListenerPassive : ILoABattleEffect
    {
        void OnSelectEmotionCard(BattleUnitModel originOwner, EmotionCardXmlInfo card);

    }
}
