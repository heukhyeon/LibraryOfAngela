using LOR_BattleUnit_UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela
{
    /// <summary>
    /// 기본적으로 버프 목록에 노출되지 않고, 지정한 속도 주사위에 마우스가 올라갔을때 설명이 노출되는 버프
    /// </summary>
    public abstract class SpeedDiceBindingBuf : BattleUnitBuf
    {
        public abstract int TargetSpeedDiceIndex { get; }

        public virtual string Artwork { get => null; }

        public virtual bool DiceTargetable { get => true; }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            base.OnTakeDamageByAttack(atkDice, dmg);
            if (isMyDiceTarget(atkDice)) OnTakeDamageByAttackMySpeedDice(atkDice, dmg);
        }

        protected virtual void OnTakeDamageByAttackMySpeedDice(BattleDiceBehavior atkDice, int dmg)
        {

        }

        /// <summary>
        /// 속도 주사위가 기믹등으로 인해 존재하지 않게되었을때 대체 전략 정의
        /// </summary>
        public virtual void OnCheckSpeedDiceNotExists()
        {

        }

        /// <summary>
        /// 해당 주사위가 자신을 타겟팅할때 그 타겟팅의 주사위가 현재 버프가 바인딩된 속도 주사위인지 여부를 반환한다.
        /// </summary>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        protected bool isMyDiceTarget(BattleDiceBehavior behaviour)
        {
            var range = behaviour?.card?.card?.GetSpec()?.Ranged;
            if (range == LOR_DiceSystem.CardRange.FarArea || range == LOR_DiceSystem.CardRange.FarAreaEach)
            {
                return behaviour.card.targetSlotOrder == TargetSpeedDiceIndex || behaviour.card.subTargets.Any(x => x.targetSlotOrder == TargetSpeedDiceIndex);
            }
            return behaviour?.card?.targetSlotOrder == TargetSpeedDiceIndex;
        }
    }
}
