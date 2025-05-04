using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela
{
    /// <summary>
    /// LibraryOfAngela Framework 를 활용하는 모드임을 명시하는 인터페이스입니다.
    /// 이 인터페이스를 <see cref="ModInitializer"/> 외의 클래스에 구현하지 마세요.
    /// 
    /// 이 인터페이스를 구현한경우, 
    /// 
    /// 절대로 LoARuntime.dll 이 Assemblies 에 직접 위치하지 않게 하세요.
    /// (Assemblies/LazyDll 등 하위 폴더를 별도로 구성해주세요)
    /// 만약 dll이 해당 위치에 존재하는경우 이 모드는 에러를 던집니다.
    /// 
    /// </summary>
    public interface ILoAMod
    {
        /// <summary>
        /// 이 값을 모드에서 직접 수정하지 마세요. LibraryOfAngela Framework 에서 자동으로 할당해줍니다.
        /// </summary>
        string packageId { get; set; }

        /// <summary>
        /// 이 값을 모드에서 직접 수정하지 마세요. LibraryOfAngela Framework 에서 자동으로 할당해줍니다.
        /// </summary>
        string path { get; set; }
        /// <summary>
        /// 모든 모드의 초기화 작업이 시작되기 전에 호출됩니다.
        /// </summary>
        void OnPreLoad();

        /// <summary>
        /// 모든 모드의 초기화 작업이 완료된 이후에 호출됩니다.
        /// </summary>
        void OnCompleteLoad();

        /// <summary>
        /// LibraryModel.LoadFromSaveData 호출 이후에 호출됩니다.
        /// </summary>
        void OnSaveLoaded();

        /// <summary>
        /// <see cref="ModInitializer"/> 에 구현한경우 기본구현되므로 따로 수정해주지 않으셔도 됩니다.
        /// 이 함수의 호출 타이밍은 모든 모드의 리소스의 초기화가 완료된 이후입니다.
        /// 
        /// <see cref="OnPreLoad"/> -> <see cref="OnInitializeMod"/> -> <see cref="OnCompleteLoad"/>
        /// </summary>
        void OnInitializeMod();
    }

    /// <summary>
    /// 모드가 CardInfo, EquipPage 등, Data 폴더에 정의되어야할 내용을 직접 제어하고싶을때 구현합니다.
    /// 이 인터페이스가 구현된경우, LibraryOfAngela Framework 는 외부 스레드에서 <see cref="customDataPath"/> 경로의 파일들을 불러와 모드의 전투책장 등의 정보를 초기화합니다.
    /// </summary>
    public interface ILoACustomDataMod : ILoAMod
    {
        /// <summary>
        /// null로 리턴하는 경우, Assemblies/Data 폴더를 불러옵니다.
        /// </summary>
        string customDataPath { get; }
        /// <summary>
        /// LibraryOfAngela Framework 를 사용하는 모든 모드의 Data 정보가 성공적으로 초기화되었을때 호출됩니다. 이 함수는 여전히 외부 스레드에서 호출됩니다.
        /// </summary>
        void OnXmlDataLoadComplete();
    }

    /// <summary>
    /// 모드가 Resources 폴더 외의 아트요소 (예 : 버프 아이콘 등) 를 사용할때 구현합니다.
    /// </summary>
    public interface ILoACustomArtworkMod : ILoAMod
    {
        /// <summary>
        /// 기존 모드들에서 사용하는 Dictionary<string, Sprite> 의 Wrapper 입니다.
        /// 주의 : 이 값을 직접 수정하지마세요. LibraryOfAngela Framework 에서 자동으로 할당 해줍니다.
        /// </summary>
        ILoAArtworkCache Artworks { get; set; }

        ArtworkConfig ArtworkConfig { get; }
    }

    /// <summary>
    /// 모드가 자체적인 AssetBundle을 소유하고 있으며, 그것을 통해 리소스를 불러올때 구현합니다.
    /// </summary>
    public interface ILoACustomAssetBundleMod : ILoAMod
    {
        /// <summary>
        /// 기존 모드들에서 사용하는 Dictionary<string, GameObject> 의 Wrapper 입니다.
        /// 주의 : 이 값을 직접 수정하지마세요. LibraryOfAngela Framework 에서 자동으로 할당 해줍니다.
        /// </summary>
        AssetBundleCache AssetBundles { get; set; }

        AssetBundleConfig AssetBundleConfig { get; }

    }

    /// <summary>
    /// 모드가 Localize 폴더를 가지고 있을때 적용.
    /// </summary>
    public interface ILoALocalizeMod : ILoAMod
    {

    }

    /// <summary>
    /// 모드의 핵심 책장을 장착시 유닛의 이름을 변경하는 등, 핵심책장의 작동을 좀 더 세분화할때 구현합니다.
    /// </summary>
    public interface ILoACorePageMod : ILoAMod
    {
        CorePageConfig CorePageConfig { get; }
    }

    /// <summary>
    /// 모드의 전투 책장의 배경, 아이콘등을 세분화할때 구현합니다.
    /// 이 인터페이스는 <see cref="ILoACustomArtworkMod"/> 를 포함합니다.
    /// </summary>
    public interface ILoABattlePageMod : ILoACustomArtworkMod
    {
        BattlePageConfig BattlePageConfig { get; }
    }

    /// <summary>
    /// 모드에서 커스텀 환상체 카드 및 Ego 카드를 제공할때 구현합니다.
    /// 환상체 책장 이미지를 위해 이 인터페이스는 <see cref="ILoACustomArtworkMod"/> 를 포함합니다.
    /// </summary>
    public interface ILoACustomEmotionMod : ILoACustomArtworkMod
    {
        EmotionConfig EmotionConfig { get; }
    }
    public interface ILoACustomStoryInvitationMod : ILoACustomArtworkMod
    {
        StoryConfig StoryConfig { get; }
    }

    /// <summary>
    /// 모드가 커스텀 맵을 사용할때 사용
    /// </summary>
    public interface ILoACustomMapMod : ILoACustomArtworkMod
    {
        MapConfig MapConfig { get; }
    }

    /// <summary>
    /// 패시브 계승을 제어할때 사용합니다.
    /// </summary>
    public interface ILoASuccessionMod : ILoAMod
    {
        SuccessionConfig SuccessionConfig { get; }
    }
}
