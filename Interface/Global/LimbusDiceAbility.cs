// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryOfAngela;
using LibraryOfAngela.Extension;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using UnityEngine;

public sealed class LimbusDiceAbility : DiceCardAbilityBase, ICustomFinalValueDice, IHandleParryingResult
{
    public const int LEVEL_NONE = 0;
    public const int LEVEL_PARRYING = 1;
    public const int LEVEL_ALL = 2;

    public int calculateLevel = LEVEL_NONE;
    public bool isForceReuse = false;
    private bool isCalculating = false;
    internal bool isWin = false;
    internal int originDelta = 0;

    public override void AfterAction()
    {
        base.AfterAction();
        isCalculating = false;
        ServiceLocator.Instance.GetInstance<ILoAInternal>().RegisterLimbusParryingWinDice(this);
    }

    void ICustomFinalValueDice.OnUpdateFinalValue(BattleDiceBehavior behaviour, ref int currentValue, ref int vanillaValue, int originUpdateValue)
    {
        Debug.Log($"림버스 작동 0 : {behavior.owner.UnitData.unitData.name} : {behavior.Index} // {calculateLevel == LEVEL_NONE} // {calculateLevel == LEVEL_PARRYING && behaviour.TargetDice is null} // {isCalculating}");
        if (calculateLevel == LEVEL_NONE) return;
        if (calculateLevel == LEVEL_PARRYING && behaviour.TargetDice is null) return;
        if (isCalculating) return;
        isCalculating = true;

        int[] origins = new int[] { vanillaValue, currentValue };
        if (!(behavior.TargetDice is null))
        {
            foreach (var d in owner.currentDiceAction.GetDiceBehaviorList())
            {
                if (d == behaviour) continue;
                var ab = d.abilityList.FindType<LimbusDiceAbility>();
                if (ab != null) ab.isCalculating = true;
                card.currentBehavior = d;
                card.currentBehavior.BeforeRollDice(behaviour.TargetDice);
                card.currentBehavior.RollDice();
                card.currentBehavior.UpdateDiceFinalValue();
                owner.battleCardResultLog.SetVanillaDiceValue(vanillaValue);
                currentValue += card.currentBehavior.DiceVanillaValue;
                vanillaValue += card.currentBehavior.DiceVanillaValue;
                if (ab != null) ab.isCalculating = false;
            }
        }
        else
        {
            vanillaValue += originDelta;
            currentValue += originDelta;
            foreach (var d in owner.currentDiceAction.GetDiceBehaviorList())
            {
                if (d == behaviour) continue;
                var ab = d.abilityList.FindType<LimbusDiceAbility>();
                if (ab != null) ab.originDelta = originDelta + vanillaValue;
            }
        }

        owner.battleCardResultLog.SetVanillaDiceValue(vanillaValue);
        card.currentBehavior = behaviour;
        Debug.Log($"림버스 작동 1 : {behavior.owner.UnitData.unitData.name} : {behavior.Index} // {origins[0]},{origins[1]} -> {vanillaValue},{currentValue}");
    }

    Result? IHandleParryingResult.OnDecisionResult(Result currentResult)
    {
        if (currentResult == Result.Win)
        {
            isWin = true;
            ServiceLocator.Instance.GetInstance<ILoAInternal>().RegisterLimbusParryingWinDice(this);
        }
        return null;
    }
}
