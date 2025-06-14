using LibraryOfAngela.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// <see cref="AdvancedSkinInfo"/>에 추가할시 
/// <see cref="RencounterManager.SetMovingStateByActionResult"/> 에서 액션이 정해지기 이전, 이후를 컨트롤할수 있음
/// </summary>
public abstract class LoAMovingStateHandler
{
    /// <summary>
    /// 액션 스크립트를 변환하기 전 호출된다. 전투 타겟 대상 자체를 변경하려고 할때 사용한다.
    /// </summary>
    /// <param name="my"></param>
    /// <param name="myResult"></param>
    /// <param name="enemy"></param>
    /// <param name="enemyResult"></param>
    public virtual void OnPreMoveRoutine(BattleUnitView my, BattleCardBehaviourResult myResult, BattleUnitView enemy, BattleCardBehaviourResult enemyResult)
    {

    }

    /// <summary>
    /// 액션 스크립트를 변환한 후 호출된다. <see cref="RencounterManager.ActionAfterBehaviour.startMoveRoutineEvent"/> 와 호출 타이밍이 거의 동일하므로 해당 처리와
    /// </summary>
    /// <param name="my"></param>
    /// <param name="enemy"></param>
    public virtual void OnStartMoveRoutine(ref RencounterManager.ActionAfterBehaviour my, ref RencounterManager.ActionAfterBehaviour enemy)
    {

    }

    /// <summary>
    /// 액션 스크립트가 모두 종료된 이후 호출된다.
    /// </summary>
    public virtual void OnEndRoutine(RencounterManager.ActionAfterBehaviour my, RencounterManager.ActionAfterBehaviour enemy)
    {

    }
}
