using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface IHandleNewCharacter : ILoABattleEffect
    {
        /// <summary>
        /// 무대에 새로운 캐릭터가 등장할때 호출됩니다.
        /// 무대 시작시 모든 캐릭터에 대해 호출됩니다. (이 경우 OnWaveStart 전에 호출)
        /// </summary>
        /// <param name="unit"></param>
        void OnNewCharacterRegister(BattleUnitModel unit);
    }
}
