using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="BattleUnitBuf_warpCharge.UseStack(int, bool)"/> 호출시 그것을 감지할수 있는 인터페이스. 기존에는 패시브만 감지하고 얼마 감소하는지를 알수 없었음
    /// </summary>
    public interface IUseChargeStackExtension : ILoABattleEffect
    {
        /// <summary>
        /// <see cref="BattleUnitBuf_warpCharge.UseStack(int, bool)"/> 호출시 파라미터를 그대로 받는 메소드. 처리가 다 된 후에 호출된다.
        /// </summary>
        /// <param name="buf">현재 충전 클래스</param>
        /// <param name="v">현재 소모값</param>
        /// <param name="isCard">전투 책장으로 소모하는지 유무</param>
        /// <param name="spended">충전이 소모됬는지 유무. 바닐라의 대부분의 로직은 UseStack전에 if에서 체크해 호출 자체를 하므로 이 값 변경은 보통 크게 의미 없음</param>
        void OnUseChargeStack(BattleUnitBuf_warpCharge buf, int v, bool isCard, ref bool spended);
    }
}
