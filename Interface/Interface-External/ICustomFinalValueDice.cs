using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="DiceBehavior"/> 또는 <see cref="BattleUnitBuf"/> 에 구현시 현재 주사위의 최종값이 업데이트될때 해당 값을 추가로 수정할 수 있다.
    /// </summary>
    public interface ICustomFinalValueDice : ILoABattleEffect
    {
        void OnUpdateFinalValue(BattleDiceBehavior behaviour,ref int currentValue, ref int vanillaValue, int originUpdateValue);
    }
}
