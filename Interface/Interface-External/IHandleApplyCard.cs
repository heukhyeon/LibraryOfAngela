using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 전투책장이 장착되거나 장착 해제 여부를 컨트롤합니다.
    /// </summary>
    public interface IHandleApplyCard : ILoABattleEffect
    {
        void OnApplyCard(BattlePlayingCardDataInUnitModel card, int slotOrder);
    }
}
