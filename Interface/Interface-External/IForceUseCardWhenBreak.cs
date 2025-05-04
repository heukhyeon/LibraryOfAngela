using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 흐트러짐 상태여도 강제로 행동할지를 결정하는 인터페이스입니다.
    /// </summary>
    interface IForceActionIgnoreBreak
    {
        bool IsForceAction { get; }
    }
}
