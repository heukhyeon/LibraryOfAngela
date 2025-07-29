using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitBuf"/> 는 전투 책장을 버렸을때 감지가 되지 않는다. 
    /// 해당 메소드를 추가한다.
    /// </summary>
    public interface IHandleDiscardByAbility : ILoABattleEffect
    {
        /// <summary>
        /// <see cref="PassiveAbilityBase.OnDiscardByAbility(List{BattleDiceCardModel})"/>와 동일
        /// </summary>
        /// <param name="cards"></param>
        void OnDiscardByAbility(List<BattleDiceCardModel> cards);
    }
}
