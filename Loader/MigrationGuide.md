# LoAInteface 사용가이드


기존 모드를 LoAInterface를 적용하거나, 혹은 신규 모드를 개발하는 이에게 적용방법을 안내하는 문서입니다.

### 주의사항

이 문서는 이미 기존에 스토리 접대 모드를 직접 구현해본 (라오루의 어지간한 작동은 이해하는) 코더를 상정하고 작성되었습니다.


## 제공하는 기능
- CustomArtwork 제공 (레거시 방식 및 AssetBundle 모두 지원)
- AssetBundle 을 활용한 에셋 로딩 제공
- Custom DiceAttackEffect
- 비동기 리소스 할당 및 해제
- 초대장 목록에서의 스토리 아이콘 추가 기능
- 핵심책장에 대한 섬네일 표시 기능
- 접대에서의 커스텀 맵 활성화
- 커스텀 환상체 책장
- 모드 핵심 책장에 대한 전용 책장 지정
- 모드 핵심 책장들에 대한 커스텀 카테고리 제공
- 기존 패시브를 계승을 통한 덮어쓰기
- SD S모션 오류 수정
- 전투 책장 홀더 커스텀
- 간단한 하모니 패치 Wrapper
- Localize

### 아직 미지원하는 기능
- 외형투영에서 모드의 핵심 책장을 워크샵 스킨처럼 투영 가능하게 제공하는 기능


## 심화
- 일부 일반적인 니즈로 요구되나 라오루에서 자체 제공하지 않아 하모니를 사용해야 하는 기능들에 대한 Interface 제공 (예 : 캐릭터에게 부여되는 위력 조절. 합 승패 조정)
- CustomData 폴더 로딩

## Migration Plan Example
- [R사 까마귀팀 모드](https://steamcommunity.com/sharedfiles/filedetails/?id=2611197051&searchtext=) 의 코드를 예시로 설명합니다.
- R사 까마귀팀 모드의 주요 (= 다른 모드들도 일반적으로 사용하는) 기능들은 아래와 같습니다.
  - CustomArtwork 사용
  - Custom DiceAttackEffect
  - 초대장 목록에서의 스토리 아이콘 추가 기능
  - 접대에서의 커스텀 맵 활성화
  - SD S모션 오류 수정
  - Localize

먼저 ModInitializer에서 단순히 제거되어야하는 메소드들을 나열합니다. 제거되는 사유는 단순히 LoAInterface에서 기본 제공하므로, 로딩 시간만 잡아먹는 요소이기때문입니다.

### GetArtWorks
- 일반적으로 모드들은 Assemblies 하위에 Artworks 폴더를 만들고, 그 폴더안에 png로 버프등의 이미지 파일을 관리합니다.
- 이런 일반적인 니즈를 취합해 LoAInterface는 해당 기능을 내장하므로 이 기능을 직접 구현할 필요가 없습니다.

### RemoveError
- already Exists 로그 제거는 LoAInterface가 알아서합니다.

### AddLocalize
- GetArtworks와 동일하게 인터페이스가 자체 제공합니다. 제거해주셔도 무방합니다.

### LoadCustomSkinSMotion
- 하위 메소드 : LoadCustomAppearanceSMotion, LoadCustomAppearanceInfoSMotion, 
- 마찬가지로 인터페이스에서 알아서 합니다.

### BookModel_SetXmlInfo
- 일반적으로 전용책장을 삽입할때 사용하는데, 인터페이스에서 제공하므로 제거합니다.

### StageClassInfo_GetStartStory
### StageClassInfo_GetEndStory
- Localize 적용때문에 하모니가 추가되는 경우인데, 인터페이스가 제공합니다.

### StageController_CheckStoryAfterBattle
- 위와는 별개로 기능 자체가 제공되지 않는 경우인데, 이것도 인터페이스가 제공합니다.

### BookModel_GetThumbSprite
- 인터페이스가 알아서 합니다.

### UISettingInvenEquipPageListSlot_SetBooksData
### UIInvenEquipPageListSlot_SetBooksData
- 핵심 책장 목록에서 스토리 아이콘 표시 + 단순히 모드 아이디로 표시되는걸 수정하기 위해 보통 하모니하는데, 인터페이스가 알아서 합니다.

### UISpriteDataManager_GetStoryIcon
- 스토리 아이콘 표시 용도인데, 인터페이스의 artwork 에서 알아서 대응합니다.

### BattleSceneRoot_InitInvitationMap
- 맵 커스텀을 위해 검은침묵등의 기존 맵을 오버라이드하는경우인데, 인터페이스가 알아서합니다.

### PhilipPhaseOneMapManager_OnRoundStart
### BlackSilence2ndMapManager_InitializeMap
### BlackSilence4thMapManager_InitializeMap
- 위의 기존 맵을 덮어씌우다보니 기존 MapManager를 강제로 건드려줘야하기때문에 존재합니다. 기존 MapManager를 사용하는게 아니므로 제거합니다.

### UIStoryProgressPanel_SetStoryLine
- 초대장 목록에 해당 모드 접대를 추가하기위해 하모니합니다. 인터페이스가 알아서합니다.

### StageClassInfo_get_currentState
- 이전 모드 접대를 클리어해야 다음 모드 접대가 보이게하는 용도입니다. 인터페이스가 알아서 합니다.

### UIInvitationRightMainPanel_SendInvitation
- 모드의 초대장 조합으로 정상적으로 모드 접대가 시작되도록 하기 위해 하모니합니다. 인터페이스가 알아서 합니다.

### IsUnlocked
### CreateRaven
### CreateKaras
### UIStoryProgressPanel_SelectedSlot
### UIInvitationRightMainPanel_SetCustomInvToggle
### SlotCopying
- 마찬가지로 위 커스텀 모드 초대장을 위해 부가적으로 존재하는 기능들입니다.

### UIBattleStoryInfoPanel_SetData
- 서고에서 해당 스토리에 대한 일러스트를 보여주기위해 존재합니다. 제거합니다.


### DiceEffectManager_CreateBehaviourEffect
- 모드의 DiceAttackEffect (흔히 vfx로 사용)를 정상적으로 출력하기위해 하모니합니다. 제거합니다.

### SdCharacterUtil_CreateSkin
- 여러가지 용도가 있는데, 일단 대부분의 기능은 인터페이스가 제공하므로 제거합니다.

### LoadAppearance
- 바닐라 코드 ctrl c +v 입니다. 제거합니다.

### ChangeAtkSound
- 인터페이스가 제공합니다.

### WorkshopSkinDataSetter_SetData
- S모션에 대한 오브젝트 추가 때문에 사용됩니다. 제거합니다.

### CopyCharacterMotion
- 위 메소드때문에 사용됩니다. 제거합니다.

### UIBookStoryPanel_OnSelectEpisodeSlot
### UIBookStoryChapterSlot_SetEpisodeSlots
- 서고에서 모드 카테고리를 정상적으로 분리하기 위해 사용합니다. 제거합니다.

### EntryScene_SetCG
- 클리어 이후 재접속시 마지막 클리어의 스토리 컷씬을 보여주기 위해 사용합니다. 제거합니다.

### TextDataModel_InitTextData
- 로컬라이즈 대응에 대한 부분입니다. 제거합니다.



제거할 메소드를 모두 제거했으므로 이제 제거한 기능에 대해 인터페이스에 알릴 수 있게끔 기존 코드를 수정합니다.
ModInitializer에 다음 인터페이스들을 추가합니다.
```c-sharp
using LibraryOfAngela;

namespace Raven_MOD
{

	public class RavenInitializer : ModInitializer, 
		// 커스텀 스토리 접대가 있으므로 정의
		IILoACustomStoryInvitationMod,
		// 커스텀 맵을 사용하므로 정의
		ILoACustomMapMod,
		// 로컬라이즈 기능을 사용하므로 정의
		ILoALocalizeMod,
		// S모션을 사용하고, 다이얼로그를 상황에 따라 커스텀하므로 정의
		ILoACorePageMod
		 {

		public ArtworkConfig ArtworkConfig { get; } = new RavenArtworkConfig();
		public StoryConfig StoryConfig { get; } = new RavenStoryConfig();
		public MapConfig MapConfig { get; } = new RavenMapConfig();
		public CorePageConfig CorePageConfig { get; } = new RavenCorePageConfig();
	

		// 이렇게 복붙해두기만 해주세요. 인터페이스에서 알아서 할당합니다.
		public string packageId { get; set; }
		public sring path { get; set; }
		public ILoAArtworkCache Artworks { get; set; }

		override void OnPreLoad() {
			// 인터페이스 초기화를 시작하기 전에 할게 있다면 추가 코드를 구성합니다. 없다면 빈 칸으로 유지합니다.
		}

		override void OnCompleteLoad() {
			// 인터페이스 초기화가 완료된 뒤 (Localize 등) 실행할 로직이 있다면 추가합니다.
			// 일반적으로 기존 OnInitializeMod 에 추가되었던 코드들이 이 함수 안에 들어갑니다.
		}

		override void OnSaveLoaded() {
			// 유저가 이어하기를 누른 뒤에 호출됩니다. 별도로 실행할게 없다면 빈칸으로 유지합니다.
		}

	}

}
```

Localize 는 정의만 해도 알아서 하므로 특별히 대응해줄게 없습니다.

이제 각 Config을 구성해줘야합니다. 

### Migration - ArtworkConfig

```
using LibraryOfAngela.Model

namespace Raven_MOD {
	public class RavenArtworkConfig : ArtworkConfig {

		// 단순히 Assemblies/Artwork 안에 모든 커스텀 아트워크가 있다면 오버라이드할 필요 없습니다.
		// public override GetArtworkDatas()		


		// 대응하는 핵심책장의 섬네일 이름 (파일인경우 파일이름. 에셋번들을 통할시 에셋 이름)
		public override string ConvertThumbnail(BookModel target) {
			switch (target.BookId.id) {
				case 10000001:
					return "KarasThumb";
				case 10000002:
					return "KuinaThumb";
				case 10000003:
					return "RavenThumb";
			}
		}

		// 만약 모드가 바닐라의 전투 책장을 사용하는경우, 해당 함수를 오버라이드하고 리턴값을 ""로 주면 바닐라의 전투책장 아트를 참조할 수 있습니다.
		// public override ConvertValidCombatPagePackage()
	}
}
```