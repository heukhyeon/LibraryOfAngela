using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// <see cref="DiceCardSelfAbilityBase"/> 에 정의할시 해당 효과가 존재하는 전투책장이 손, 또는 특수책장 더미에 표시될때 그에 대응하는 UI 객체를 제어할 수 있습니다.
/// </summary>
public interface ILoACardUIBinder
{
    /// <summary>
    /// 대상 책장이 바인딩된 <see cref="BattleDiceCardUI"/> 가 표시될때 호출됩니다. 
    /// <see cref="BattleDiceCardUI.SetEgoLock"/> 을 호출해 전투 책장이 비활성화된것처럼 표시하거나,
    /// <see cref="LoADiceCardUIKeyDetect.Create(BattleDiceCardUI, BattleUnitModel, BattleDiceCardModel, UnityEngine.KeyCode, OnLoACardKeyPressListener)"/> 을 호출해
    /// 해당 전투 책장에 대한 키 입력등을 받는 작업등을 할 수 있습니다.
    /// </summary>
    /// <param name="ui">전투 책장과 연결된 UI 객체입니다.</param>
    /// <param name="owner">현재 해당 전투 책장을 보유한 자기 자신입니다.</param>
    /// <param name="card">현재 <see cref="DiceCardSelfAbilityBase"/>와 연결된 전투 책장입니다.</param>
    void OnHandle(BattleDiceCardUI ui, BattleUnitModel owner, BattleDiceCardModel card);
}
