using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="DiceCardSelfAbilityBase"/> 에 구현시 층의 에고가 추가되었을때 해당 에고가 추가되었음을 전달 받을 수 있다.
    /// </summary>
    public interface IAddToEgoCard
    {
        void OnAddToFloorEgo(BattleDiceCardModel self);
    }
}
