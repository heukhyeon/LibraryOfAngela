using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// 이 인터페이스를 구현시 감정 레벨이 최대여도 감정 코인을 획득할 수 있다.
    /// </summary>
    public interface IHandleEmotionLevelUp : ILoABattleEffect
    {
        bool IsOverGetEmotionCoin { get; }

        /// <summary>
        /// 기존에는 <see cref="PassiveAbilityBase"/> 만 감정 레벨업 효과를 받을 수 있지만 이 인터페이스를 구현시 Buf등 다른 곳에서도 받을 수 있다.
        /// </summary>
        void OnLevelUpEmotion();
    }
}
