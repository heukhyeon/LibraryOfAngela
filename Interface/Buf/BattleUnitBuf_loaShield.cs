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
    /// <see cref="ReduceStack(LoAKeywordBufReduceRequest)"/> 에 의해 감소될때 호출됩니다.
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
    /// 막 종료시 기본적으로 소멸합니다.
    /// </summary>
    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEnd(this);
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
    public void ReduceStack(LoAKeywordBufReduceRequest request)
    {
        controller.OnReduceStack(this, request);
    }

    /// <summary>
    /// 보호막을 수치 감소등의 이유 외의 이유로 소멸시킬때 호출합니다.
    /// </summary>
    /// <param name="attacker">호출자</param>
    public void DestroyManually(BattleUnitModel attacker) {
        controller.OnDestroyManually(this, attacker);
    }
}

public class LoAShieldReduceRequest : LoAKeywordBufReduceRequest
{
    internal LoAShieldReduceRequest()
    {

    }

    /// <summary>
    /// 흐트러짐 피해에 의해 보호막이 감소. 일반 공격에 의한 감소는 <see cref="LoAKeywordBufReduceRequest.Attack"/>을 사용
    /// </summary>
    public class AttackBreakDamage : LoAShieldReduceRequest
    {
        public readonly BattleDiceBehavior trigger;
        internal AttackBreakDamage(BattleDiceBehavior behaviour, int value)
        {
            Attacker = behaviour.owner;
            trigger = behaviour;
            Stack = value;
        }
    }

    /// <summary>
    /// 효과에 의한 피해에 의해 보호막이 감소
    /// </summary>
    public class AbilityDamage : LoAShieldReduceRequest
    {
        public readonly DamageType type;
        public readonly KeywordBuf keyword;
        internal AbilityDamage(BattleUnitModel attacker, int value, DamageType type, KeywordBuf keyword)
        {
            Attacker = attacker;
            Stack = value;
            this.type = type;
            this.keyword = keyword;
        }
    }

    /// <summary>
    /// 효과에 의한 흐트러짐 피해에 의해 보호막이 감소
    /// </summary>
    public class AbilityBreakDamage : LoAShieldReduceRequest
    {
        public readonly DamageType type;
        public readonly KeywordBuf keyword;
        internal AbilityBreakDamage(BattleUnitModel attacker, int value, DamageType type, KeywordBuf keyword)
        {
            Attacker = attacker;
            Stack = value;
            this.type = type;
            this.keyword = keyword;
        }
    }
}

/// <summary>
/// 보호막의 소멸의 이유를 알리는 클래스
/// 대부분의 경우 일반 모드에서 사용하지마세요. 특정한 목적이 명확히 있을때만 <see cref="Etc"/>를 사용하세요.
/// </summary>
public class LoAShieldDestroyReason {
    internal LoAShieldDestroyReason() {

    }

    /// <summary>
    /// 피해로 인해 보호막 수치가 0이 되어서 파괴된 경우
    /// </summary>
    public class StackZero : LoAShieldDestroyReason {
        /// <summary>
        /// 스택이 0이 될때 마지막으로 피해를 입힌 대상
        /// </summary>
        public readonly BattleUnitModel attacker;

        internal StackZero(BattleUnitModel attacker) {
            this.attacker = attacker;
        }
    }

    /// <summary>
    /// 피해로 인해 보호막 수치가 0이 되어서 파괴된 경우
    /// </summary>
    public class RoundEnd : LoAShieldDestroyReason {
        /// <summary>
        /// 소멸 당시 기준 남은 보호막 수치. 보호막은 파괴될시 수치가 0이 되고 개별 인터페이스에 호출되는건 파괴 이후에 호출되므로, 파괴전 마지막 수치를 이 필드에 보존합니다.
        /// </summary>
        public readonly int remainStack;

        internal RoundEnd(int remainStack) {
            this.remainStack = remainStack;
        }
        
    }

    /// <summary>
    /// 모드 등에서 임의로 파괴한경우. 프레임워크에서는 직접 부르지 않습니다.
    /// </summary>
    public class Etc : LoAShieldDestroyReason {
        /// <summary>
        /// 소멸을 호출한 대상
        /// </summary>
        public readonly BattleUnitModel attacker;

        public Etc(BattleUnitModel attacker) {
            this.attacker = attacker;
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

        void OnReduceStack(BattleUnitBuf_loaShield buf, LoAKeywordBufReduceRequest request);

        void OnRoundEnd(BattleUnitBuf_loaShield buf);

        void OnDestroyManually(BattleUnitBuf_loaShield buf, BattleUnitModel attacker);
    }
}
