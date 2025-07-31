using LibraryOfAngela;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 보호막 클래스
/// 보호막간 충돌을 막기 위해 이 버프는 상속이 불가능하며, UI 버프 목록에 표시되지 않습니다.
/// </summary>
public sealed class BattleUnitBuf_loaBarrier : BattleUnitBuf, IHandleChangeDamage
{
    BarrierController controller;

    /// <summary>
    /// 버프 타입. <see cref="LoAKeywordBuf.Barrier"/>
    /// </summary>
    public override KeywordBuf bufType => LoAKeywordBuf.Barrier;

    public BattleUnitBuf_loaBarrier()
    {
        controller = ServiceLocator.Instance.GetInstance<BarrierController>();
    }

    bool isCreated = false;
    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        if (!isCreated)
        {
            isCreated = true;
            controller.OnCreate(this);
        }
        controller.OnAddBuf(this, addedStack);
    }

    public override void Destroy()
    {
        controller.OnDestroy(this);
        base.Destroy();
    }

    void IHandleChangeDamage.HandleDamage(int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf buf)
    {
        controller.OnHandleDamage(this, originDmg, ref resultDmg, type, attacker, buf);
    }

    void IHandleChangeDamage.HandleBreakDamage(int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf buf)
    {
        controller.OnHandleBreakDamage(this, originDmg, ref resultDmg, type, attacker, buf);
    }

    public void ReduceStack(LoABarrierReduceRequest request)
    {
        controller.OnReduceStack(this, request);
    }

}

/// <summary>
/// 보호막의 수치를 경감시킬때 경감시키는 목적으로 사용되는 클래스
/// 피격 등 <see cref="IHandleTakeShield"/> 의 메소드에서 경감스택을 제어하는경우 프레임워크에서 자체적으로 정의한 목적을 사용합니다.
/// 대부분의 경우 일반 모드에서 사용하지마세요. 특정한 목적이 명확히 있을때만 <see cref="Etc"/>를 사용하세요.
/// </summary>
public class LoABarrierReduceRequest
{
    /// <summary>
    /// 감소수치 절댓값
    /// </summary>
    public int Stack { get; internal set; }

    /// <summary>
    /// 공격자
    /// </summary>
    public BattleUnitModel Attacker { get; internal set; }

    internal LoABarrierReduceRequest()
    {

    }

    /// <summary>
    /// 공격에 의해 보호막이 감소되는 경우 호출
    /// </summary>
    internal sealed class AttackDamage : LoABarrierReduceRequest
    {
        /// <summary>
        /// 공격 주사위
        /// </summary>
        public readonly BattleDiceBehavior behaviour;

        public AttackDamage(BattleDiceBehavior behaviour, int dmg)
        {
            Attacker = behaviour?.owner;
            this.behaviour = behaviour;
            Stack = dmg;
        }
    }

    /// <summary>
    /// 공격에 의해 보호막이 감소되는 경우 호출
    /// </summary>
    internal sealed class AbilityDamage : LoABarrierReduceRequest
    {
        /// <summary>
        /// 데미지 발생 타입 (전투 책장 효과, 패시브 등)
        /// </summary>
        public readonly DamageType type;

        /// <summary>
        /// 버프로 피해를 준경우 해당 버프 키워드 타입
        /// </summary>
        public readonly KeywordBuf buf;

        public AbilityDamage(BattleUnitModel attacker, int dmg, DamageType type, KeywordBuf buf = KeywordBuf.None)
        {
            Attacker = attacker;
            Stack = dmg;
            this.type = type;
            this.buf = buf;
        }
    }

    /// <summary>
    /// 공격에 의해 보호막이 감소되는 경우 호출
    /// </summary>
    internal sealed class AttackBreakDamage : LoABarrierReduceRequest
    {
        /// <summary>
        /// 공격 주사위
        /// </summary>
        public readonly BattleDiceBehavior behaviour;
        public AttackBreakDamage(BattleDiceBehavior behaviour, int dmg)
        {
            Attacker = behaviour?.owner;
            this.behaviour = behaviour;
            Stack = dmg;
        }
    }

    /// <summary>
    /// 공격에 의해 보호막이 감소되는 경우 호출
    /// </summary>
    internal sealed class AbilityBreakDamage : LoABarrierReduceRequest
    {

        /// <summary>
        /// 데미지 발생 타입 (전투 책장 효과, 패시브 등)
        /// </summary>
        public readonly DamageType type;

        /// <summary>
        /// 버프로 피해를 준경우 해당 버프 키워드 타입
        /// </summary>
        public readonly KeywordBuf buf;

        public AbilityBreakDamage(BattleUnitModel attacker, int dmg, DamageType type, KeywordBuf buf = KeywordBuf.None)
        {
            Attacker = attacker;
            Stack = dmg;
            this.type = type;
            this.buf = buf;
        }
    }

    /// <summary>
    /// 피해 이외의 수단으로 감소되는 경우 호출
    /// </summary>
    public class Etc : LoABarrierReduceRequest
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="stack"></param>
        public Etc(BattleUnitModel attacker, int stack)
        {
            Attacker = attacker;
            Stack = stack;
        }
    }
}

namespace LibraryOfAngela.Interface_Internal
{
    internal interface BarrierController
    {
        void OnCreate(BattleUnitBuf_loaBarrier buf);
        void OnAddBuf(BattleUnitBuf_loaBarrier buf, int addedStack);

        void OnDestroy(BattleUnitBuf_loaBarrier buf);

        void OnHandleDamage(BattleUnitBuf_loaBarrier buf, int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword);

        void OnHandleBreakDamage(BattleUnitBuf_loaBarrier buf, int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword);

        void OnReduceStack(BattleUnitBuf_loaBarrier buf, LoABarrierReduceRequest request);
    }
}