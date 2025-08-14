using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleAllyCardDetail.DisCardACardRandom"/> 따위의 메소드가 불렸을때, 버릴 책장이 부족한 경우를 감지합니다.
    /// </summary>
    public interface INotEnoughDiscardListener : ILoABattleEffect
    {
        /// <summary>
        /// <see cref="BattleAllyCardDetail.DisCardACardRandom"/> 따위의 메소드가 불렸을때, 버릴 책장이 부족한 경우 호출됩니다.
        /// 한명의 캐릭터에게 동시에 이 인터페이스가 여러개 존재할경우, 앞의 인터페이스에서 적절히 버릴 수 있도록 <see cref="BattleAllyCardDetail.DrawCards(int)"/>등을 호출한경우 후속 인터페이스는 호출되지 않습니다
        /// </summary>
        /// <param name="expectedCount">버리려고 하는 책장 수입니다. <see cref="BattleAllyCardDetail.DisCardACardRandom"/>같은 경우 1입니다.</param>
        /// <param name="actualCount">현재 손의 책장 수입니다.</param>
        void OnNotEnoughDiscard(int expectedCount, int actualCount);
    }
}
