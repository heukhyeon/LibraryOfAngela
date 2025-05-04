using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Model
{
    public abstract class ArtworkConfig : LoAConfig
    {

        /// <summary>
        /// 모드내에 정의된 Artwork 폴더의 종류를 반환합니다.
        /// <see cref="ILoACustomArtworkMod"/> 를 구현했고, 이 함수의 반환값이 null 인경우, LibraryOfAngela 프레임워크는 해당 모드의 Assemblies/Artwork 폴더를 탐색합니다.
        /// 이 함수의 반환값 요소로 <see cref="AssetBundleArtworkInfo"/> 를 반환하는경우, <see cref="ModInitializer"/> 에 <see cref="ILoACustomAssetBundleMod"/> 를 구현해주셔야 합니다.
        /// </summary>
        /// <returns></returns>
        public virtual List<ILoAArtworkData> GetArtworkDatas()
        {
            return new List<ILoAArtworkData>();
        }

        /// <summary>
        /// 섬네일은 기본적으로 캐릭터 스프라이트와 크기가 달라야만 제대로 렌더링됩니다.
        /// </summary>
        /// <param name="target">현재 대상이 되는 책장입니다.</param>
        /// <returns>null 이거나 에러를 발행하는 경우 별도 처리를 하지 않습니다. 반환값이 not null 인경우 <see cref="Artworks"/> 에서 해당 값을 조회해 올바른 섬네일로 교체합니다.</returns>
        public virtual string ConvertThumbnail(BookModel target)
        {
            return null;
        }

        /// <summary>
        /// 캐릭터의 모션을 기본 CharacterSkin 외의 다른 방법으로 로드하고자 할때 사용합니다.
        /// </summary>
        /// <param name="skinName"></param>
        /// <returns>null 이거나 오류를 발행하는경우 따로 처리하지 않습니다. </returns>
        public virtual CustomSkinRenderData ConvertMotionSprite(string skinName, ActionDetail motion)
        {
            return null;
        }

        public virtual float HandlePixelPerUnitFileArtwork(string skinName, string fileName, Texture texture) 
        {
            return 50f;
        }

        /// <summary>
        /// 다른 모드나 또는 바닐라의 전투책장 이미지를 바로 사용할때 사용합니다.
        /// 
        /// 다른 모드나 바닐라의 전투책장 이미지를 끌어다쓰지 않는경우 null 이나 Exception 발생하게 구현해주셔도 무관합니다.
        /// </summary>
        /// <param name="name">현재 모드의 전투 책장 Artwork 값입니다.</param>
        /// <returns>해당 Artwork 값을 가진 원본 컨텐츠의 package id 입니다. null 인경우 처리를 무시하며, ""인경우 바닐라 책장을 참조합니다. 그 외의 값은 해당 값의 모드를 참조하려 시도합니다.</returns>
        public virtual string ConvertValidCombatPagePackage(string name)
        {
            return null;
        }
    }
}
