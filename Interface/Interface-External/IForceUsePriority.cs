using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="DiceCardSelfAbilityBase"/> 에 정의시 속도 주사위 값에 관계없이 책장 사용 우선도를 정할 수 있다. (광역보단 느리게 사용)
    /// 반환 값을 음수로 줄 경우, 가장 마지막에 사용한다. (정의하지 않은 책장의 반환값을 0으로 가정한다.)
    /// </summary>
    public interface IForceUsePriority
    {
        int UsePriority { get; }
    }
}
