using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 키워드 버프가 초기 생성될때 그 버프 자체를 바꿔치기합니다.
    /// </summary>
    public interface IHandleAddNewKeywordBufInList : ILoABattleEffect
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufType"></param>
        /// <param name="current"></param>
        /// <param name="readyType"></param>
        /// <returns>리턴값이 not null이고, 기존과 다르면 바꿔치기합니다. 충돌방지를 위해 바꿔치는 버프는 원래 KeywordBuf에 대응하는 
        /// BattleUnitBuf (예 : <see cref="BattleUnitBuf_warpCharge"/> ) 를 상속해주세요.</returns>
        BattleUnitBuf OnAddNewKeywordBufInList(KeywordBuf bufType, BattleUnitBuf current, BufReadyType readyType);
    }
}
