using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitModel.Die(BattleUnitModel, bool)"/> 에서 자신을 죽인 유닛은 바닐라에서 환상체 책장만 확인할 수 있다. 패시브나 버프에서도 확인할 수 있게 한다.
    /// 환상체 책장에는 구현해도 의미가 없다.
    /// </summary>
    public interface IOnDieExtension : ILoABattleEffect
    {
        void OnDie(BattleUnitModel killer);
    }
}
