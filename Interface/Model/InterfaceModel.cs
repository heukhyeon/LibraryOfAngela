using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Model
{

    /// <summary>
    /// 개별 환상체 카드에 대해 컨테이너 및 틴트를 개별 설정하고싶은경우 사용합니다.
    /// </summary>
    public class EmotionCardUIConfig
    {
        public string artwork;
        // 환상체 책장에 마우스가 오버됬을때 펼쳐지는 우측 영역에 대한 아트워크입니다. artwork 가 있고 해당값이 없는경우, artwork+"_Detail"을 기본값으로 사용하려 합니다.
        public string detailArtwork;
        // 글씨 등의 전반적인 색깔을 설정합니다. 이것은 컨테이너 자체의 tint 로는 작동하지 않습니다.
        public Color color;

    }


    public class EmotionPannelInfo
    {
        public string title;
        public string artwork;
        public string packageId;
        public int level = -1;
        public List<BattleUnitModel> matchedTarget;
        public List<EmotionCardXmlInfo> cards;
        public Action<BattleUnitModel, EmotionCardXmlInfo> onSelect = null;
    }

    public class EgoPanelInfo
    {
        public string title;
        public string artwork;
        public string packageId;
        public int level = -1;
        public List<LorId> cards;
    }

    public delegate void HandleBeforeMoveRoutineListener(ref RencounterManager.ActionAfterBehaviour my, ref RencounterManager.ActionAfterBehaviour opponent);

    /// <summary>
    /// 특정 핵심책장을 장착했을때 해당 책장의 장착 제한, 임의의 출력용 패시브 추가 등을 지원합니다.
    /// </summary>
    public class AdvancedEquipBookInfo
    {
        public readonly LorId targetId;
        // 원거리 사용 가능 책장일때 원거리 책장 사용 여부를 패시브에서 숨깁니다.
        public bool hideRangePassive;
        // 설정 화면에서만 보이는 패시브입니다 (계승 및 실제 접대에서는 보여지지 않습니다)
        public List<LorId> settingPassive;

        // 해당 핵심 책장을 장착시 사서의 이름을 변경합니다.
        // 스킨의 경우도 둘다 존재합니다. 스킨의 값을 우선시합니다.
        public Func<UnitDataModel, string> customOwnerName;

        // 해당 스킨을 장착시 커스텀 다이얼로그를 생성합니다.
        // 스킨의 경우도 둘다 존재합니다. 스킨의 값을 우선시합니다.
        public Func<BattleDialogueModel> customDialog;
        // public CustomRairtyColor rarityColor;

        // 해당 핵심 책장의 장착 가능 여부를 설정합니다. 기본적으로 활성화이되, 기본 장착 불가 캐릭터에겐 장착 불가 상태입니다.
        public Func<UnitDataModel, bool> equipCondition = null;

        // 현재 스킨, 캐릭터 설정시의 기존 스킨, 기존 커스터마이징 데이터, 결과값
        public Func<string, string, UnitCustomizingData, LoACustomFaceData> overrideFace = null;

        // 움직이기 전 대상 변경등 전투 결과를 출력하기 전 바꿔칠 콜백을 지정합니다.
        public HandleBeforeMoveRoutineListener handleBeforeMoveRoutine;

        public AdvancedEquipBookInfo(LorId id)
        {
            targetId = id;
        }
    }

    /// <summary>
    /// 특정 스킨을 구현했을 때 해당 스킨의 높이, 액션 스크립트 등을 고정적으로 지원합니다.
    /// </summary>
    public class AdvancedSkinInfo
    {
        /// <summary>
        /// 내부적으로 사용됩니다. 모드에서 지정해도 내부적으로 다시 할당됩니다.
        /// </summary>
        public string packageId;

        /// <summary>
        /// 대상이 되는 스킨입니다. 반드시 지정되어야 합니다.
        /// </summary>
        public readonly string skinName;

        /// <summary>
        /// 스킨을 아예 프리팹으로 불러오는 경우 사용합니다.
        /// 대상 프리팹은 <see cref="CharacterAppearance"/>를 루트에 가지고 있어야하며, 스킨 렌더링에 필요한 모든 상태 
        /// (다수의 <see cref="CharacterMotion"/> 등)을 프리팹 내에 모두 보유하고 있어야합니다. 
        /// 이 값이 존재한다면 Resources 내에 CharacterSkin이 있어도 새로 생성합니다. 따라서 Resources 내에 대상 CharacterSkin을 정의하지 않는걸 권합니다.
        /// 이 값이 존재한다면 <see cref="audioReplace"/>, <see cref="hasSkinSprite"/> 을 무시합니다.
        /// </summary>
        public string prefabKey;

        /// <summary>
        /// 해당 핵심 책장을 장착 시 키 값을 고정합니다.
        /// </summary>
        public int fixedHeight = -1;

        /// <summary>
        /// 스페셜 모션을 포함합니다.
        /// </summary>
        public bool hasSpecialSkin;

        /// <summary>
        /// 해당 스킨 장착 시 사서의 이름을 변경합니다.
        /// 핵심 책장의 경우도 둘 다 존재할 수 있으며, 스킨의 값을 우선시합니다.
        /// </summary>
        public Func<UnitDataModel, string> customOwnerName;

        /// <summary>
        /// 해당 스킨 장착 시 커스텀 다이얼로그를 생성합니다.
        /// 핵심 책장의 경우도 둘 다 존재할 수 있으며, 스킨의 값을 우선시합니다.
        /// </summary>
        public Func<BattleDialogueModel> customDialog;

        /// <summary>
        /// 별도의 액션 스크립트가 없을 때 기본 액션 스크립트를 주입할지 여부입니다.
        /// </summary>
        public Func<BehaviourActionBase> defaultActionScript = null;

        /// <summary>
        /// 기존의 액션 스크립트를 오버라이드합니다.
        /// </summary>
        public Func<BattleCardBehaviourResult, BehaviourActionBase, BehaviourActionBase> overrideActionScript = null;

        /// <summary>
        /// 모션에 대한 SFX를 교체할지 여부입니다. 기본은 null입니다.
        /// 반환 값이 null인 경우에도 교체하지 않습니다.
        /// </summary>
        public Func<ActionDetail, string> audioReplace = null;

        /// <summary>
        /// 책장 사용 전에 움직일 수 있는지 여부를 결정합니다.
        /// </summary>
        public Func<BattlePlayingCardDataInUnitModel, bool> isStartMoveable = null;

        /// <summary>
        /// SD 이미지가 별도의 스킨 스프라이트('_skin' postfix)를 가집니다.
        /// 이 경우 기존 스프라이트 위에 추가로 스킨 스프라이트를 덮어씁니다.
        /// </summary>
        public bool hasSkinSprite;

        /// <summary>
        /// 계승 창에 보이는 사서 얼굴을 별도의 스프라이트로 변경합니다.
        /// </summary>
        [Obsolete("Use Instead CustomFaceData")]
        public string overrideFaceSprite;

        /// <summary>
        /// E.G.O 사용 시 사서의 얼굴을 덮어씁니다.
        /// </summary>
        [Obsolete("Use Instead CustomFaceData")]
        public List<ActionDetail> overrideFaceTypes = new List<ActionDetail> { ActionDetail.Default };

        /// <summary>
        /// 현재 스킨, 기존 커스터마이징 데이터, 결과값을 받아 커스텀 얼굴 데이터를 반환합니다.
        /// </summary>
        public Func<string, string, UnitCustomizingData, LoACustomFaceData> overrideFace = null;

        /// <summary>
        /// 스킨 렌더링 시 스킨의 이펙트 등을 제어하는 컴포넌트의 타입을 반환합니다.
        /// LoASkinComponent를 상속한 타입이어야 합니다.
        /// </summary>
        public Type skinComponentType;

        /// <summary>
        /// Workshop skin list에 올릴 때 대응할 책장을 주입합니다.
        /// 없다면 Workshop skin list에 표시되지 않습니다.
        /// </summary>
        public LorId exportWorkshopSkinMatchedId = null;

        /// <summary>
        /// 움직이기 전 대상 변경 등 전투 결과를 출력하기 전 바꿔칠 콜백을 지정합니다.
        /// </summary>
        public HandleBeforeMoveRoutineListener handleBeforeMoveRoutine = null;

        /// <summary>
        /// 생성자: 스킨 이름을 지정합니다.
        /// </summary>
        /// <param name="skinName">스킨의 이름</param>
        public AdvancedSkinInfo(string skinName)
        {
            this.skinName = skinName;
        }
    }


    public struct CustomRairtyColor
    {
        public Color textColor;
        public Color outlineColor;
    }
}
