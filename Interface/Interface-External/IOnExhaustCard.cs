using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 책장 소멸시 호출됨
    /// </summary>
    public interface IOnExhaustCard: ILoABattleEffect
    {
        void OnExhaustCard(BattleDiceCardModel card);
    }
}
