using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI;
using UnityEngine;

namespace LibraryOfAngela.Model
{

    public class LoAConfig
    {
        public string packageId;

        public virtual void Init()
        {

        }
    }

    /// <summary>
    /// 일반적인 모드의 방식인 .jpg, .png 파일을 직접 모드에 포함하고 File IO를 통해 런타임에 Sprite를 만들어내는 형태입니다.
    /// </summary>
    public class FileArtworkInfo : ILoAArtworkData
    {
        public string packageId { get; set; }
        public string path { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="artworkDirPath">불러올 이미지 파일의 경로입니다. 디렉토리명 만 적은경우 자동으로 해당 모드의 Assemblies/파라미터 의 경로로 지정합니다.</param>
        public FileArtworkInfo(string artworkDirPath)
        {
            path = artworkDirPath;
        }
    }

    /// <summary>
    /// AssetBundle 에 직접 Sprite 를 적재한경우, AssetBundle 을 통해 Sprite를 만들어냅니다.
    /// 모드의 크기 및 로드 속도에 있어 에셋번들이 더 빠르므로 이 쪽을 권장드립니다.
    /// </summary>
    public class AssetBundleArtworkInfo : ILoAArtworkData
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imagePath">에셋 번들내 이미지가 존재하는 디렉토리의 경로입니다.</param>
        public AssetBundleArtworkInfo(string imagePath)
        {
            path = imagePath;
        }

        public string packageId { get; set; }

        public string path { get; set; }
    }

    public class CustomStoryTimeline
    {
        public string id;
        public string artwork;
        public string name;
        public Vector2 returnButtonPosition = new Vector2(-360f, 300f);
    }

    public class CustomStoryIconInfo
    {
        public string packageId = null;
        public string storyTimeline = null;
        public string artwork;
        public List<CustomStoryInfo> stageIds;
        public UIStoryLine relatedStoryLinePosition;
        public Vector2 position;
        public List<CustomStoryLine> lines = null;
        public KeyCode replaceKeyCode = KeyCode.None;
    }

    public class CustomStoryLine
    {
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.Euler(0f, 0f, 90f);
        public Vector3 scale = Vector3.one;
    }

    public class CustomStoryInfo
    {
        // 나중에 넣어줍니다.
        public string packageId;
        public int id;
        public Func<bool> visibleCondition = () => true;
        public Func<UnitDataModel , BattleDialogueModel> overrideDialog = (unit) => null;
        public List<ClearReward> rewards = new List<ClearReward>();
        public bool skipResult = false;
        public string storyArtwork;
        public string invitationArtwork;
        public string invitationMessage;
        // 전투 준비시에 대응 유닛에 대한 상태를 변경합니다.
        public Func<int, UnitBattleDataModel, LoAUnitReadyState> overrideUnit = null;
        public CustomStorySettingInfo[] settingInfos;
    }

    public class LoAUnitReadyState
    {
        public class Required : LoAUnitReadyState
        {

        }
        public class Locked : Required
        {

        }

        public class Replaced : Required
        {
            public readonly UnitBattleDataModel replaceUnit;
            public Replaced(UnitBattleDataModel replaceUnit)
            {
                this.replaceUnit = replaceUnit;
            }
        }

        public class Removed : LoAUnitReadyState
        {

        }
    }

    public class CustomStoryHandle
    {
        // 접대의 접대 후 스토리가 진행에 따라 분기되는 등의 요소가 필요할때 사용합니다.
        public string replaceStory;
        public Action<int> onEvent;
        public Func<int, bool> onClick;
        public Func<int, IEnumerator> onTransitionCoroutine;
    }

    public class ClearReward
    {
        public string packageId;
        public int id;
        public int count;
        public DropItemType type;
        public ClearReward(int id, int count, DropItemType type)
        {
            this.id = id;
            this.count = count;
            this.type = type;
        }
    }

    public class CustomStorySettingInfo
    {
        public int wave;
    }

    public class CustomEquipBookCategory
    {
        //public string packageId;
        public string visibleName;
        public string artwork;
        public int level;
        /// <summary>
        /// 이 값이 null 인경우 현재 모드 내 모든 핵심책장을 넣습니다.
        /// </summary>
        public List<LorId> matchedBookIds;
        // 여러 모드간 책장을 합칠때 고유한 값을 넣습니다.
        // 이 값이 동일하고 다른 설정이 다르다면 랜덤한 값이 적용될 수 있습니다.
        public string uniqueId;
    }



    public class CustomSkinRenderData
    {
        public bool isAssetBundle = false;
        public readonly ActionDetail action;
        public readonly string skinName;
        /// <summary>
        /// 이 값이 true 인경우 '_front' 파일을 추가로 로드합니다. 
        /// </summary>
        public bool hasFront = false;

        public CustomSkinRenderData(ActionDetail action, string skinName)
        {
            this.action = action;
            this.skinName = skinName;
        }
    }

    public class CustomMapData
    {
        public string packageId;
        public string mapName;
        public string backgroundArtwork;
        public string floorArtwork;
        public bool isFixedPosition = true;
        public string[] bgmSource;
        public string[] mapDialogs = null;
        /// <summary>
        /// 명시적으로 LoAMapManager를 상속하는 클래스를 넣습니다. 아니라면 제대로 작동하지 않습니다.
        /// </summary>
        public Type managerType = null;
        /// <summary>
        /// 바닥이 x축으로 90도 기울어져있는지 여부를 설정합니다. 이걸 설정할경우 바닥의 scale 및 position이 복잡해지지만 전투 진행중 바닥이 좀 더 입체적으로 보입니다. 바닐라의 맵은 기본적으로 기울어져있습니다.
        /// </summary>
        public bool isXRotatedFloor = false;
        /// <summary>
        /// 특정 접대의 시작 배경. 이 값이 not null 인 경우 <see cref="themeStageWave"/> 도 같이 지정해줘야 제대로 작동한다.
        /// </summary>
        public LorId themeStageId;
        /// <summary>
        /// <see cref="themeStageId"/>에서 몇번째 무대가 해당 맵이 사용되어야하는지 값. 지정되지 않은경우 첫번째 무대에 사용한다.
        /// </summary>
        public int themeStageWave;
        public Vector3 bgScale = Vector3.one;
        public Vector3 bgPosition = Vector3.zero;
        public Vector3 floorPosition = Vector3.zero;
        public Vector3 floorScale = Vector3.zero;
        /// <summary>
        /// 테두리 주변의 비치는 색상을 지정합니다. 기본값은 총류의 층에서의 해당 값입니다.
        /// </summary>
        public Color frameVignetteColor = new Color(0.725f, 0.725f, 0.725f, 1f);
    }

    public abstract class CustomCardHandEffect
    {
        public string packageId;
        public LorId targetCardId;
        // 로드할 에셋키입니다.
        public string assetKey;
        public Vector3 scale;
        public Func<bool> effectExistable;

        public abstract bool isValidCard(BattleDiceCardModel card, BattleUnitModel owner);

        public class ID : CustomCardHandEffect
        {
            private LorId id;

            public ID(LorId targetCardId, Func<bool> effectExistable)
            {
                this.id = targetCardId;
                this.effectExistable = effectExistable;
            }

            public override bool isValidCard(BattleDiceCardModel card, BattleUnitModel owner)
            {
                return card.GetID() == id;
            }
        }

        public class Manual : CustomCardHandEffect
        {
            private Func<BattleDiceCardModel, BattleUnitModel, bool> func;
            public Manual(Func<BattleDiceCardModel, BattleUnitModel, bool> func, Func<bool> effectExistable)
            {
                this.func = func;
                this.effectExistable = effectExistable;
            }
            public override bool isValidCard(BattleDiceCardModel card, BattleUnitModel owner)
            {
                try
                {
                    return func(card, owner);
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public class CustomCardHolder
    {
        public LorId targetCardId;
        public Color commonColor;
        public Color linearColor = new Color(1f, 1f, 1f, 0f);


        public class Color
        {
            public UnityEngine.Color color;

            public Color(float r, float g, float b, float a)
            {
                this.color = new UnityEngine.Color(r, g, b, a);
            }

            public Color(int r, int g, int b, int a) : this(r / 255f, g / 255f, b / 255f, a / 255f)
            {

            }
        }
    }

    public class AdditionalOnlyCardModel
    {
        public LorId bookId;
        public LorId cardId;
    }

    public class DebugStageInfo
    {
        public LorId stageId;
        public SephirahType sephira;
        public int wave;
        public bool enter = false;
    }

    public class MultiDeckInfo
    {
        public struct DeckInfo
        {
            public string name;
            public Func<List<DiceCardXmlInfo>, List<LorId>> visibleCards;
            public Func<UnitDataModel, DeckModel, List<DiceCardXmlInfo>, List<DiceCardXmlInfo>> onCardChange;
            /// <summary>
            /// 현재 유닛, 현재 덱, 삽입하려는 카드, 기존의 삽입 가능 여부를 통해 최종 삽입 가능 여부를 변경해 반환한다.
            /// </summary>
            public Func<UnitDataModel, DeckModel, LorId, CardEquipState, CardEquipState> onCardInsert;
            /// <summary>
            /// 현재 유닛, 현재 덱, 제거하려는 카드를 통해 최종 제거 가능 여부를 변경해 반환한다.
            /// null 반환시 그냥 무시하고, 그 외의 값이라면 그 값으로 반환값을 강제 조정한다.
            /// </summary>
            public Func<UnitDataModel, DeckModel, LorId, bool?> onCardRemove;
        }

        public LorId targetId;

        public List<DeckInfo> infos;
    }

    public class RarityModel
    {
        public enum Type
        {
            PASSIVE,
            BOOK_OUTLINE,
            BOOK_INNERLINE
        }

        public LorId targetId;
        public Type type;
        public Color32 color;
    }

    public class CustomBattlePageHolder
    {
        public string artwork;
        public Color32 color;
        public LorId[] targetIds;
        public Vector3 rangeHsv;

        // 프레임워크에서 자동으로 할당합니다.
        public bool rangeCustom;
        public ILoAArtworkCache cache;
    }

    public class ParryingOneSideAction
    {
        protected readonly BattlePlayingCardDataInUnitModel card1;
        protected readonly BattlePlayingCardDataInUnitModel card2;

        public BattlePlayingCardDataInUnitModel GetAllyCard(Faction faction)
        {
            if (card1 != null && card1.owner.faction == faction) return card1;
            if (card2 != null && card2.owner.faction == faction) return card2;
            return null;
        }

        public BattlePlayingCardDataInUnitModel GetEnemyCard(Faction faction)
        {
            if (card1 != null && card1.owner.faction != faction) return card1;
            if (card2 != null && card2.owner.faction != faction) return card2;
            return null;
        }

        internal ParryingOneSideAction(BattlePlayingCardDataInUnitModel card1, BattlePlayingCardDataInUnitModel card2)
        {
            this.card1 = card1;
            this.card2 = card2;
        }

        public class Parrying : ParryingOneSideAction
        {
            public new readonly BattlePlayingCardDataInUnitModel card1;
            public new readonly BattlePlayingCardDataInUnitModel card2;

            public Parrying(BattlePlayingCardDataInUnitModel card1, BattlePlayingCardDataInUnitModel card2) : base(card1, card2)
            {
                this.card1 = base.card1;
                this.card2 = base.card2;
            }

            public Parrying(BattleUnitModel unit1, BattleUnitModel unit2, int unit1Slot, int unit2Slot) :
                this(
                    unit1Slot >= 0 && unit1Slot <= unit1.cardSlotDetail.cardAry.Count - 1 ? unit1.cardSlotDetail.cardAry[unit1Slot] : null,
                    unit2Slot >= 0 && unit2Slot <= unit2.cardSlotDetail.cardAry.Count - 1 ? unit2.cardSlotDetail.cardAry[unit2Slot] : null
                    )
            {

            }




        }

        public class OneSide : ParryingOneSideAction
        {
            public readonly BattlePlayingCardDataInUnitModel card;
            public readonly BattleUnitModel victim;
            public readonly int victimSlot;

            public OneSide(BattleUnitModel attacker, int attackerSlot, BattleUnitModel victim, int victimSlot) : base(
                attackerSlot >= 0 && attackerSlot <= attacker.cardSlotDetail.cardAry.Count - 1 ? attacker.cardSlotDetail.cardAry[attackerSlot] : null,
                null
                )
            {
                card = card1;
                this.victim = victim;
                this.victimSlot = victimSlot;
            }

            public OneSide(BattlePlayingCardDataInUnitModel card) : base(card, null)
            {
                this.card = card;
                this.victim = card.target;
                this.victimSlot = card.targetSlotOrder;
            }
        }


    }
}
