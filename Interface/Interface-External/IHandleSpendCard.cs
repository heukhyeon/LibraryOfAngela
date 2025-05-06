using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 책장의 비용이 지불되었을때 그것을 감지합니다.
    /// 여러가지 형태로 사용할수 있습니다.
    /// 
    /// 1. 특수 책장을 사용시 즉시 다시 원래대로 되돌리기
    /// 2. 빛 지불을 감지해 사용 등
    /// 
    /// costSpended에 의존하므로 다시 추가하는경우 다시 costSpended를 false로 설정해줘야 합니다.
    /// </summary>
    public interface IHandleSpendCard : ILoABattleEffect
    {
        void OnSpendCard(BattleUnitModel owner, BattleDiceCardModel card, ILoACardListController controller);
    }
}
