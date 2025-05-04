using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 광역 책장에 지정당할때 호출됨
    /// </summary>
    public interface IOnStartTargetedByAreaAtk : ILoABattleEffect
    {
        void OnStartTargetedByAreaAtk(BattlePlayingCardDataInUnitModel attackerCard);
    }
}
