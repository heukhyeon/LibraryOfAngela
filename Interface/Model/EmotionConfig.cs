using LOR_XML;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryOfAngela.Model
{
    public abstract class EmotionConfig : LoAConfig
    {

        public string emotionPath;

        public string descPath;

        public abstract Type GetAbnornailityScriptType(EmotionCardXmlInfo xmlInfo, string script);

        /// <summary>
        /// 메인 화면에서 환상체 책장 목록을 커스텀할때 사용합니다.
        /// </summary>
        /// <param name="cards"></param>
        /// <param name="sephirah"></param>
        /// <param name="level"></param>
        public virtual void OnReturnEmotionCardListForPreview(List<EmotionCardXmlInfo> cards, SephirahType sephirah, int level) { }

        /// <summary>
        /// 접대 내에서 환상체 책장 목록을 커스텀할때 사용합니다.
        /// </summary>
        /// <param name="cards">기본 null입니다. (not null 인경우는 다른 모드에서 환상체 책장을 건드린 경우) </param>
        /// <param name="emotionLevel"></param>
        public virtual void OnReturnEmotionCard(SephirahType sephirah, int floorLevel, int emotionLevel, List<EmotionCardXmlInfo> result) { }

        /// <summary>
        /// 접대 내에서 감정 레벨 상승시 선택 가능한 환상체 책장 목록의 갯수를 변경하려 할때 사용합니다.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="sephirah"></param>
        /// <param name="floorLevel"></param>
        /// <param name="emotionLevel"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual int HandleSelectEmotionCardListCount(int current, SephirahType sephirah, int floorLevel, int emotionLevel, List<EmotionCardXmlInfo> result)
        {
            return -1;
        }

        public virtual EgoPanelInfo GetModEgoSelector(List<LorId> idList) => null;

        // 해당 사서에 에고가 보일지 말지를 결정합니다. 두가지 용도로 쓰일 수 있습니다.
        // 1. 특정 사서가 공용 에고를 사용할 수 없음.
        // 2. 특정 에고를 특정 사서에게만 활성화
        public virtual bool IsValidEgoOwner(BattleUnitModel owner, BattleDiceCardModel card) => true;

        public virtual EmotionPannelInfo CreateModAbnormalitySelectList(List<BattleUnitModel> alivedAllys, List<EmotionCardXmlInfo> modCards) => null;

        /// <summary>
        /// 환상체 책장 선택시 해당 책장의 소유 가능한 대상을 지정합니다.
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <returns>
        /// null 인경우 기존 바닐라의 처리를 따릅니다.
        /// Count == 1 인경우 해당 대상에게 바로 책장을 부여합니다.
        /// 그 외인 경우 해당 대상들에게만 환상체 책장 획득 표시를 보여줍니다.
        /// </returns>
        public virtual LoAEmotionSelectTarget GetMatchedAbnormalityCardOwner(EmotionCardXmlInfo info) => null;

        public virtual EmotionCardUIConfig GetCustomEmotionConfig(EmotionCardXmlInfo info) => null;

        /// <summary>
        /// 기존 환상체 5개에서 개별 환상체를 추가해 프리뷰 환상체 슬롯 자체를 늘려야하는 경우 사용합니다.
        /// </summary>
        /// <param name="currentAbnormalityCount">현재 환상체 갯수입니다. (기본 5개)</param>
        public virtual void HandleFloorAbnormalityCount(SephirahType floor, ref int currentAbnormalityCount) { }

        /// <summary>
        /// 층의 에고 목록에 추가로 에고를 주입할때 사용합니다.
        /// </summary>
        /// <param name="cardIdList"></param>
        /// <returns>
        /// 오버라이드 할경우 not null로 반환합니다. not null 인경우 id를 대조해 기존에 없던 아이템을 제거합니다.
        /// </returns>
        public virtual List<LorId> HandleFloorEgoList(SephirahType sephirah, List<EmotionEgoXmlInfo> current) { return null; }

        /// <summary>
        /// 기존 환상체 책장의 '설명'만 교체할때 (아이디 등을 유지함) 설명을 교체할지 여부를 결정합니다.
        /// </summary>
        /// <param name="cardID"></param>
        /// <param name="origin"></param>
        /// <param name="myModCard"></param>
        /// <returns></returns>
        public virtual bool IsOverrideDesc(string cardID, AbnormalityCard origin, AbnormalityCard myModCard)
        {
            return false;
        }

        /// <summary>
        /// 접대에서 환상체 책장을 사용 불가로 만들때 사용합니다.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wave"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public virtual bool IsEmotionEnableReception(int id, int wave, int round, Faction faction)
        {
            return true;
        }
    }

    public abstract class LoAEmotionSelectTarget
    {
        public class Fixed : LoAEmotionSelectTarget
        {
            public readonly List<BattleUnitModel> targets;

            public Fixed(BattleUnitModel target)
            {
                targets = new List<BattleUnitModel> { target };
            }

            public Fixed(IEnumerable<BattleUnitModel> targets)
            {
                this.targets = targets.ToList();
            }
        }

        public class Selectable : LoAEmotionSelectTarget
        {
            public readonly List<BattleUnitModel> targets;

            public Selectable(BattleUnitModel target)
            {
                targets = new List<BattleUnitModel> { target };
            }

            public Selectable(IEnumerable<BattleUnitModel> targets)
            {
                this.targets = targets.ToList();
            }
        }
    }
}
