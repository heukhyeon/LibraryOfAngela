using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Model
{
    public abstract class StoryConfig : LoAConfig
    {
        /// <summary>
        /// 접대 목록에 표시되어야할 접대 아이콘들의 정보를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public abstract List<CustomStoryIconInfo> GetStoryIcons();

        /// <summary>
        /// 스토리 타임라인을 추가합니다. 같은 키값이라면 먼저 온쪽이 사용됩니다.
        /// </summary>
        /// <returns></returns>
        public virtual List<CustomStoryTimeline> GetTimelines()
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wave"></param>
        public virtual void BattlePrepare(LorId id, StageLibraryFloorModel floor, int wave)
        {

        }

        /// <summary>
        /// 접대 조건에 따라 다음 스테이지를 아예 다른 접대로 바꿔치기하려고 할때 사용합니다.
        /// </summary>
        /// <param name="id"></param>
        /// <returns> null이거나 기존과 같은경우 유지됩니다. 다른 경우 스테이지 정보를 초기화합니다.</returns>
        public virtual StageClassInfo HandleStageRoute(LorId id, int wave)
        {
            return null;
        }

        /// <summary>
        /// 스토리 대본 정보를 로딩했을때 호출됩니다.
        /// 
        /// 이 함수는 해당 스토리 대본이 해당 모드의 스토리 대본일때만 호출됩니다.
        /// </summary>
        /// <param name="currentStory">현재 스토리 대본 정보입니다.</param>
        public virtual void OnStoryLoaded(StageStoryInfo currentStory)
        {

        }

        /// <summary>
        /// 무대 시작시 출력할 스토리를 반환합니다.
        /// </summary>
        /// <param name="info">현재 진행중인 접대 정보입니다.</param>
        /// <param name="wave">현재의 무대 값입니다.</param>
        /// <returns>not null 이면 스토리를 재생합니다.</returns>
        public virtual string GetWaveStartStory(StageClassInfo info, int wave)
        {
            return null;
        }

        /// <summary>
        /// 무대 종료시 출력할 스토리를 반환합니다.
        /// </summary>
        /// <param name="info">현재 진행중인 접대 정보입니다.</param>
        /// <param name="wave">현재의 무대 값입니다.</param>
        /// <returns>not null 이면 스토리를 재생합니다.</returns>
        public virtual string GetWaveEndStory(StageClassInfo info, int wave)
        {
            return null;
        }

        public virtual LoAStoryEnd HandleStoryEnd(string story, bool isReadOnly)
        {
            return null;
        }

        /// <summary>
        /// <see cref="GetStoryBranchs"/> 로 정의한 분기문에서 유저가 선택했을때 호출됩니다.
        /// <see cref="GetStoryBranchs"/> 를 구현하지 않은경우 그냥 에러로 유지해주셔도 됩니다.
        /// </summary>
        /// <param name="branchId"> <see cref="GetStoryBranchs"/> 에서 정의한 <see cref="CustomStoryBranchData.uniqueId"/> 값입니다. </param>
        /// <param name="selectedIndex">선택한 분기문의 인덱스 값입니다. 0 아니면 1입니다.</param>
        public virtual void OnStoryBranchSelect(string branchId, int selectedIndex)
        {

        }

        public virtual CustomStoryHandle HandleStoryOpen(string story) 
        {
            return null;
        }

        public virtual string GetStoryStandingCharacterName(string standingFileName)
        {
            return standingFileName.Split('_')[0];
        }

        public virtual string GetStoryStandingPortrait(string standingCharacterName)
        {
            return "StoryPortraits_" + standingCharacterName;
        }
    }

    public class LoAStoryEnd
    {
        internal LoAStoryEnd()
        {

        }

        public class Next : LoAStoryEnd
        {
            public readonly string story;
            public readonly Action onEnd;
            public Next(string story, Action onEnd)
            {
                this.story = story;
                this.onEnd = onEnd;
            }
        } 

        public class Branch : LoAStoryEnd
        {
            // 분기문에서 첫번째 버튼의 텍스트입니다.
            public readonly string firstBranchText;
            // 분기문에서 두번째 버튼의 텍스트입니다.
            public readonly string secondBranchText;
            // 분기문의 두 버튼을 모두 빨갛게 표시합니다.
            public bool hideAnswer;
            public Action<int> onSelect;

            public Branch(string firstBranchText, string secondBranchText, Action<int> onSelect)
            {
                this.firstBranchText = firstBranchText;
                this.secondBranchText = secondBranchText;
                this.onSelect = onSelect;
            }

        }
    }
}
