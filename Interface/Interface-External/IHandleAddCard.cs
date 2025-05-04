using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleAddCard : ILoABattleEffect
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentCard">장착시 발동 등의 경우 null이 될 수 있음</param>
        /// <param name="releasedCard">처음 장착하는 경우 null이 될 수 있음</param>
        void OnAddCard(BattlePlayingCardDataInUnitModel currentCard, BattlePlayingCardDataInUnitModel releasedCard);
    }
}
