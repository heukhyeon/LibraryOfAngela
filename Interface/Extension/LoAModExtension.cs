using LibraryOfAngela.Interface_Internal;
using LibraryOfAngela.Model;
using LOR_XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Extension
{
    /// <summary>
    /// 프레임워크에서는 사용할일이 없고, 개별 모드들의 편의성을 위해 구현하는 확장함수의 모음입니다.
    /// 
    /// 이 함수의 모든 확장함수는 인터페이스의 구현객체 내부여도 this 키워드를 명시적으로 붙여야만 인식됩니다.
    /// 
    /// class MyMod : ILoACustomEmotionMod {
    /// 
    ///   void Something() {
    ///       GetMyEmotionCards(); // 컴파일 에러
    ///       this.GetMyEmotionCards(); // 작동
    ///   }
    /// }
    /// </summary>
    public static class LoAModExtension
    {
        /// <summary>
        /// 자신의 모드 내의 환상체 카드 목록을 반환합니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static List<EmotionCardXmlInfo> GetMyEmotionCards(this ILoACustomEmotionMod owner, Predicate<EmotionCardXmlInfo> condition = null)
        {
            return ServiceLocator.Instance.GetInstance<ILoAEmotionDictionary>().GetEmotionCardListByMod(owner, condition);
        }

        /// <summary>
        /// 자신의 모드 내의 환상체 카드 설명 목록을 반환합니다.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static List<AbnormalityCard> GetMyEmotionDescs(this ILoACustomEmotionMod owner)
        {
            return ServiceLocator.Instance.GetInstance<ILoAEmotionDictionary>().GetEmotionCardDescListByMod(owner);
        }

        public static bool IsMyModCard(this ILoACustomEmotionMod owner, EmotionCardXmlInfo info)
        {
            return ServiceLocator.Instance.GetInstance<ILoAEmotionDictionary>().IsModCard(owner, info);
        }
    
        /// <summary>
        /// 현재 생존한 아군중 특정 책을 장착한 사서를 반환합니다.
        /// </summary>
        /// <param name="id">현재 모드의 책 아이디입니다. packageId 는 owner의 값으로 고정됩니다.</param>
        /// <returns>대응하는 핵심책장을 장착한 아군이 있으면 해당 아군을, 없다면 null을 반환합니다.</returns>
        public static BattleUnitModel FindUnitByBookId(this ILoACustomEmotionMod owner, int id)
        {
            var key = new LorId(owner.packageId, id);
            return BattleObjectManager.instance.GetAliveList(Faction.Player).Find(x => x.Book.GetBookClassInfoId() == key);
        }

        public static void UpdateRarity(this CorePageConfig config, RarityModel model)
        {
            ServiceLocator.Instance.GetInstance<ILoARoot>().UpdateRarity(model);
        }
    
    }
}
