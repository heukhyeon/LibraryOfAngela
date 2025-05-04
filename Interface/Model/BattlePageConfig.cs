using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public abstract class BattlePageConfig : LoAConfig
    {

        public virtual Dictionary<LorId, string> CustomIcons { get => null; }

        public virtual LorId[][] SharedIds { get => null; }

        public virtual CustomBattlePageHolder[] CustomHolder { get => null; }

        /// <summary>
        /// 특정 전투책장에 대해 개별 이펙트를 적용할 목록을 반환합니다.
        /// </summary>
        /// <returns>에러나 null인경우 empty로 취급합니다.</returns>
        public virtual List<CustomCardHandEffect> GetCardHandEffects()
        {
            return new List<CustomCardHandEffect>();
        }

        /// <summary>
        /// 특정 전투책장의 전투책장 배경을 바꿀때 사용합니다.
        /// </summary>
        /// <returns>에러나 null인경우 empty로 취급합니다.</returns>
        public virtual List<CustomCardHolder> GetCardHolders()
        {
            return new List<CustomCardHolder>();
        }

        public virtual IEnumerable<BattlePageInfo> GetBattlePageInfos()
        {
            yield break;
        }
    }

    public struct BattlePageInfo
    {
        public abstract class VisibleCondition
        {
            // 전용 책장 자체는 그냥 OnlyPage 쓸것

            public class MultiDeck : VisibleCondition
            {
                // null 이면 그 모드의 패키지 값을 덮어씌움. 그 외의 값이면 직접 대응
                public string packageId;
                public int corePageId;
                public int deckIndex;

                public MultiDeck(int corePageId, int deckIndex) : base()
                {
                    this.corePageId = corePageId;
                    this.deckIndex = deckIndex;
                }
            }
        }

        public string packageId;
        public int id;
        public VisibleCondition visibleCondition;
        // 이 값이 0이 아니라면 책장 목록에서 책장 제거시 제거되지도, 다시 책장 목록에 넣을때 추가되지도 않는다.
        public int fixedCount;

        public BattlePageInfo(int id)
        {
            this.id = id;
            packageId = null;
            visibleCondition = null;
            fixedCount = 0;
        }
    }
}
