using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public abstract class CorePageConfig : LoAConfig
    {
        /// <summary>
        /// 핵심 책장 목록의 카테고리를 커스텀합니다.
        /// </summary>
        /// <returns>count가 0 이상인경우 해당 모드의 핵심책장들에 대해 카테고리 커스텀을 적용합니다.</returns>
        public virtual List<CustomEquipBookCategory> GetEquipBookCategories() => null;

        public virtual List<AdvancedEquipBookInfo> GetAdvancedEquipBookInfos() => null;

        public virtual List<AdvancedSkinInfo> GetAdvancedSkinInfos() => null;

        public virtual List<RarityModel> GetRarityModels() => null;

        /// <summary>
        /// 자신의 모드 이외에, 기존 바닐라 등 다른 핵심책장에 자신의 전투책장을 추가할때 사용합니다.
        /// null로 주거나 에러가 발생시 Empty List 로 취급됩니다.
        /// </summary>
        public virtual List<AdditionalOnlyCardModel> AddtionalOnlyCards { get => null; }

        public virtual List<MultiDeckInfo> MultiDecks { get => null; }

        [Obsolete("Using BattlePageConfig.GetBattlePageInfos()")]
        public virtual List<LorId> JustSettingCards { get => null; }


        public virtual List<LorId> GetHideCostPassives() => null;

        [Obsolete("Use Instead OnCorePageStateChange")]
        public virtual void OnCreateCorePage(BookModel book)
        {

        }

        /// <summary>
        /// 유닛의 스킨 정보를 런타임에 바꾸려고 할때 사용합니다.
        /// </summary>
        /// <param name="skinName">현재 책장 스킨입니다.</param>
        /// <param name="unit">대상 유닛 정보입니다.</param>
        /// <returns></returns>
        public virtual string ChangeCharacterSkinByUnitInfo(string skinName, UnitDataModel unit) => null;

        /// <summary>
        /// 책이 생성되거나, 장착 상태가 변경될때 호출됩니다.
        /// </summary>
        /// <param name="book"></param>
        /// <param name="id">해당 책의 id</param>
        /// <param name="ev">변경 이벤트 타입</param>
        /// <param name="owner">해당 책의 장착 주인. <see cref="EquipStateEvent.Loaded"/> 나 <see cref="EquipStateEvent.Created"/> 시에는 null일 수 있다.</param>
        public virtual void OnCorePageStateChange(BookModel book, LorId id, EquipStateEvent ev, UnitDataModel owner)
        {

        }
    }

    public enum EquipStateEvent
    {
        Created,
        Loaded,
        Equip,
        UnEquip,
    }
}
