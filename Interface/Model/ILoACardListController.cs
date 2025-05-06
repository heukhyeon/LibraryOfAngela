using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryOfAngela.Interface_External;
namespace LibraryOfAngela.Model
{
    /// <summary>
    /// <see cref="IHandleSpendCard.OnSpendCard(BattleUnitModel, BattleDiceCardModel)"/> 따위에서 별도의 리플렉션 없이 책장의 제거 및 삽입을 수행하기 위한 래핑 클래스입니다.
    /// </summary>
    public interface ILoACardListController
    {
        List<BattleDiceCardModel> GetList(LoACardListScope scope);

        /// <summary>
        /// 대상 책장 목록에 책장을 추가합니다.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="scope"></param>
        /// <param name="index">0 미만인경우 끝에다가 추가합니다.</param>
        void Insert(LoACardListScope scope, BattleDiceCardModel card, int index = -1);

        /// <summary>
        /// 대상 책장 목록에서 책장을 제거합니다.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="card"></param>
        bool Remove(LoACardListScope scope, BattleDiceCardModel card);

        /// <summary>
        /// 대상 책장 목록 내 대응 인덱스의 책장을 제거합니다.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="index"></param>
        bool Remove(LoACardListScope scope, int index);
    }

    public enum LoACardListScope
    {
        USED,
        HAND,
        RESERVED,
        // personalCardDetail 에서 해당 스코프를 줄 경우 used로 체크합니다.
        DISCARDED
    }
}
