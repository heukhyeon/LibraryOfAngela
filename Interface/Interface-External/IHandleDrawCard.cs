using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleDrawCard : ILoABattleEffect
    {
        void OnHandleDrawCards(ref int count);
    }
}
