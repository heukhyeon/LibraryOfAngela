using System.Collections.Generic;

namespace LibraryOfAngela.Model
{
    public abstract class SuccessionConfig : LoAConfig
    {
        /// <summary>
        /// 해당 핵심 책장에 대해서만 
        /// </summary>
        /// <param name="currentBookId"></param>
        /// <returns></returns>
        public virtual Dictionary<LorId, List<LorId>> GetOnlySuccessionBookId() => null;

        /// <summary>
        /// 전용으로 매핑되는 패시브에 대한 정보를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<LorId, LorId> GetMappingOnlySuccessionPassives() => null;

        /// <summary>
        /// 계승 책장 목록에서 보이지 않는 목록을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<LorId> GetInvalidSuccessionBookId()
        {
            yield break;
        }
    }
}
