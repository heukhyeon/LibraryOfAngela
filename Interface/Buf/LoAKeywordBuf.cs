using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 외부에서 LoA 공용 버프를 사용할때 참조할 키워드 목록
/// 사용 예시 : card.target.bufListDetail.AddKeywordBufByCard(LoAKeywordBuf.Sinking, 2, card.owner);
/// </summary>
public static class LoAKeywordBuf
{
    /// <summary>
    /// 침잠
    /// </summary>
    public static KeywordBuf Sinking { get; set; }

    /// <summary>
    /// 진동
    /// </summary>
    public static KeywordBuf Tremor { get; set; }

    /// <summary>
    /// 파열
    /// </summary>
    public static KeywordBuf Rupture { get; set; }

    /// <summary>
    /// 호흡
    /// </summary>
    public static KeywordBuf Poise { get; set; }

    // 침잠쇄도는 '버프'가 아니므로 제외
}
