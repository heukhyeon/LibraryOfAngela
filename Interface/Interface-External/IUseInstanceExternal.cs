using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="DiceCardSelfAbilityBase"/> 에 정의시 기존에는 획득하지 못한
    /// 
    /// <see cref="DiceCardSelfAbilityBase.IsValidTarget(BattleUnitModel, BattleDiceCardModel, BattleUnitModel)"/> 및
    /// 
    /// <see cref="DiceCardSelfAbilityBase.OnUseInstance(BattleUnitModel, BattleDiceCardModel, BattleUnitModel)"/> 에서 대상의 속도 주사위가 어디인지 알수 있다.
    /// 
    /// 각각의 함수는 원래 함수를 호출한뒤 targetSpeedDiceIndex 가 붙은 버전을 추가로 호출한다.
    /// </summary>
    public interface IExtensionUseInstance
    {
        bool IsValidTarget(BattleUnitModel unit, BattleDiceCardModel card, BattleUnitModel targetUnit, int targetSpeedDiceIndex);

        void OnUseInstance(BattleUnitModel unit, BattleDiceCardModel card, BattleUnitModel targetUnit, int targetSpeedDiceIndex);
    }
}
