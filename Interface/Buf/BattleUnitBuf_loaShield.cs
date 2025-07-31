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
public sealed class BattleUnitBuf_loaShield : BattleUnitBuf, IHandleChangeDamage
{
    ShieldController controller;

    /// <summary>
    /// 버프 타입. <see cref="LoAKeywordBuf.Shield"/>
    /// </summary>
    public override KeywordBuf bufType => LoAKeywordBuf.Shield;

    public BattleUnitBuf_loaShield()
    {
        controller = ServiceLocator.Instance.GetInstance<ShieldController>();
    }

    bool isCreated = false;
    /// <summary>
    /// <see cref="BattleUnitBufListDetail.AddKeywordBufByCard(KeywordBuf, int, BattleUnitModel)"/> 따위로 증가되거나,
    /// <see cref="ReduceStack(LoAShieldReduceRequest)"/> 에 의해 감소될때 호출됩니다.
    /// </summary>
    /// <param name="addedStack">변동값입니다. 음수일수 있습니다</param>
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

    /// <summary>
    /// 보호막을 소멸시킵니다.
    /// </summary>
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

    /// <summary>
    /// 보호막 수치를 감소시킵니다.
    /// </summary>
    /// <param name="request">공격, 효과 피해등 감소를 시키는 행위</param>
    public void ReduceStack(LoAShieldReduceRequest request)
    {
        controller.OnReduceStack(this, request);
    }

}

/// <summary>
/// 보호막의 수치를 경감시킬때 경감시키는 목적으로 사용되는 클래스
/// 피격 등 <see cref="IHandleTakeShield"/> 의 메소드에서 경감스택을 제어하는경우 프레임워크에서 자체적으로 정의한 목적을 사용합니다.
/// 대부분의 경우 일반 모드에서 사용하지마세요. 특정한 목적이 명확히 있을때만 <see cref="Etc"/>를 사용하세요.
/// </summary>
public class LoAShieldReduceRequest
{
    /// <summary>
    /// 감소수치 절댓값
    /// </summary>
    public int Stack { get; internal set; }

    /// <summary>
    /// 공격자
    /// </summary>
    public BattleUnitModel Attacker { get; internal set; }

    internal LoAShieldReduceRequest()
    {

    }

    /// <summary>
    /// 공격에 의해 보호막이 감소되는 경우 호출
    /// </summary>
    internal sealed class AttackDamage : LoAShieldReduceRequest
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
    internal sealed class AbilityDamage : LoAShieldReduceRequest
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
    internal sealed class AttackBreakDamage : LoAShieldReduceRequest
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
    internal sealed class AbilityBreakDamage : LoAShieldReduceRequest
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
    public class Etc : LoAShieldReduceRequest
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
    internal interface ShieldController
    {
        void OnCreate(BattleUnitBuf_loaShield buf);
        void OnAddBuf(BattleUnitBuf_loaShield buf, int addedStack);

        void OnDestroy(BattleUnitBuf_loaShield buf);

        void OnHandleDamage(BattleUnitBuf_loaShield buf, int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword);

        void OnHandleBreakDamage(BattleUnitBuf_loaShield buf, int originDmg, ref int resultDmg, DamageType type, BattleUnitModel attacker, KeywordBuf keyword);

        void OnReduceStack(BattleUnitBuf_loaShield buf, LoAShieldReduceRequest request);
    }
}