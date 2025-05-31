using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    public interface ICustomCardSetter : ILoABattleEffect
    {
        /// <summary>
        /// AI가 특정 카드를 특정 대상의 특정 속도 주사위 슬롯에 특정 속도 주사위를 사용하여 플레이하려고 할 때,
        /// 그 행동의 우선순위를 계산합니다.
        /// </summary>
        /// <param name="caster">현재 행동을 결정하는 아군 유닛입니다.</param>
        /// <param name="cardToUse">사용을 고려 중인 아군의 카드입니다.</param>
        /// <param name="casterSpeedDiceValue">아군이 해당 카드를 사용하려고 하는 속도 주사위의 값입니다.</param>
        /// <param name="casterDiceSlotIndex">아군이 해당 카드를 사용하려고 하는 속도 주사위의 슬롯 인덱스입니다.</param>
        /// <param name="potentialTarget">카드의 대상이 될 수 있는 유닛입니다.</param>
        /// <param name="targetSpeedDiceSlotIndex">아군이 공격 대상으로 삼는 'potentialTarget'의 속도 주사위 슬롯 인덱스입니다.</param>
        /// <param name="targetCardOnSlot"> 'potentialTarget'이 'targetSpeedDiceSlotIndex'에 사용할 예정인 카드입니다. 카드가 없다면 null일 수 있습니다.</param>
        /// <param name="isClashExpected">이 카드를 사용했을 때 'potentialTarget'과 '합'이 발생할 것으로 예상되는지 여부입니다.</param>
        /// <param name="allPossibleEnemies">현재 상황에서 공격 가능한 모든 적 유닛의 리스트입니다.</param>
        /// <param name="casterAllyUnits">시전자를 포함한 모든 아군 유닛의 리스트입니다.</param>
        /// <returns>
        /// 해당 행동 조합에 대한 우선순위 점수를 정수(int)로 반환합니다.
        /// 점수가 높을수록 해당 행동을 할 확률이 높아집니다.
        /// </returns>
        int GetCustomizedCardPriority(
            BattleUnitModel caster,
            BattleDiceCardModel cardToUse,
            int casterSpeedDiceValue,
            int casterDiceSlotIndex,
            BattleUnitModel potentialTarget,
            int targetSpeedDiceSlotIndex,
            BattleDiceCardModel targetCardOnSlot,
            bool isClashExpected,
            List<BattleUnitModel> allPossibleEnemies, // 파라미터 이름 명확화
            List<BattleUnitModel> casterAllyUnits
        );
    }
}
