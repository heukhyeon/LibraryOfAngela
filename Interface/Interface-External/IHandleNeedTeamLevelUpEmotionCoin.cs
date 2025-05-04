using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 팀 레벨업에 대한 감정 코인 총량을 컨트롤한다.
    /// </summary>
    public interface IHandleNeedTeamLevelUpEmotionCoin : ILoABattleEffect
    {
        int HandleTeamCoin(int currentTeamCoinCount, EmotionBattleTeamModel myTeam);
    }
}
