using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LibraryOfAngela.Interface_External
{
    /// <summary>
    /// <see cref="LoAKeywordBuf.Shield"/> 보호막 버프에 대한 처리를 수행합니다.
    /// </summary>
    public interface IHandleTakeShield : ILoABattleEffect
    {
        /// <summary>
        /// 보호막이 소멸후 최초 생성될때 호출됩니다.
        /// </summary>
        /// <param name="buf">대상 버프</param>
        void OnCreateShield(BattleUnitBuf_loaShield buf);

        /// <summary>
        /// 보호막의 값이 증가될때 호출됩니다.
        /// </summary>
        /// <param name="buf">대상 버프</param>
        /// <param name="addedStack">추가된 값</param>
        void OnAddShield(BattleUnitBuf_loaShield buf, int addedStack);

        /// <summary>
        /// 보호막이 있는 상태에서 체력 피해를 받을시 호출됩니다.
        /// 기본적으로 <see cref="DamageType.Attack"/>이 아닐때 보호막은 피해 경감 처리를 하지 않습니다. 버프 데미지등 다른 타입에 대해 처리를 하려는 경우 이 효과에서 자체적으로 <paramref name="resultDmg"/> 와 <paramref name="reducedStack"/>를 제어해야합니다.
        /// 이 메소드에서 <paramref name="reducedStack"/>를 변경해도 실제 스택 변경에 대해서는 <see cref="OnTakeShieldReduceStack(BattleUnitModel, BattleUnitBuf_loaShield, ref int, LoAShieldReduceRequest)"/>가 추가로 호출됩니다.
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="originDmg">보호막에서 피해 처리 수행시 최초의 피해량</param>
        /// <param name="type"></param>
        /// <param name="attacker"></param>
        /// <param name="bufType"></param>
        /// <param name="resultDmg">공격 타입. <paramref name="type"/>이 <see cref="DamageType.Attack"/>이 아니면 기본적으로 <paramref name="originDmg"/>와 동일합니다.</param>
        /// <param name="reducedStack">감소될 스택. <paramref name="type"/>이 <see cref="DamageType.Attack"/>이 아니면 기본적으로 0입니다. 이 값이 최종적으로 1 이상이라면 <see cref="OnTakeShieldReduceStack(BattleUnitModel, BattleUnitBuf_loaShield, ref int, LoAShieldReduceRequest)"/> 호출 후 스택을 수정합니다.</param>
        void BeforeHandleDamageInShield(BattleUnitBuf_loaShield buf, int originDmg, DamageType type, BattleUnitModel attacker, KeywordBuf bufType, ref int resultDmg, ref int reducedStack);

        /// <summary>
        /// 보호막이 있는 상태에서 흐트러짐 피해를 받을시 호출됩니다.
        /// 기본적으로 보호막은 흐트러짐 피해 경감 처리를 하지 않습니다. 버프 데미지등 다른 타입에 대해 처리를 하려는 경우 이 효과에서 자체적으로 <paramref name="resultDmg"/> 와 <paramref name="reducedStack"/>를 제어해야합니다.
        /// 이 메소드에서 <paramref name="reducedStack"/>를 변경해도 실제 스택 변경에 대해서는 <see cref="OnTakeShieldReduceStack(BattleUnitModel, BattleUnitBuf_loaShield, ref int, LoAShieldReduceRequest)"/>가 추가로 호출됩니다.
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="originDmg">보호막에서 피해 처리 수행시 최초의 피해량</param>
        /// <param name="type"></param>
        /// <param name="attacker"></param>
        /// <param name="bufType"></param>
        /// <param name="resultDmg">공격 타입. <paramref name="type"/>이 <see cref="DamageType.Attack"/>이 아니면 기본적으로 <paramref name="originDmg"/>와 동일합니다.</param>
        /// <param name="reducedStack">감소될 스택. <paramref name="type"/>이 <see cref="DamageType.Attack"/>이 아니면 기본적으로 0입니다. 이 값이 최종적으로 1 이상이라면 <see cref="OnTakeShieldReduceStack(BattleUnitModel, BattleUnitBuf_loaShield, ref int, LoAShieldReduceRequest)"/> 호출 후 스택을 수정합니다.</param>
        void BeforeHandleBreakDamageInShield(BattleUnitBuf_loaShield buf, int originDmg, DamageType type, BattleUnitModel attacker, KeywordBuf bufType, ref int resultDmg, ref int reducedStack);

        /// <summary>
        /// 보호막이 있는 상태에서 피해를 경감받은 경우 호출됩니다. 피해가 경감되지 않은 경우 호출되지 않습니다.
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="originDmg">보호막이 없을때 받았을 피해</param>
        /// <param name="type">공격 타입</param>
        /// <param name="attacker">공격자</param>
        /// <param name="bufType">버프로 피해 부여시 해당 버프 피해</param>
        /// <param name="resultDmg">보호막 처리로 인해 경감된 최종 피해량</param>
        void AfterHandleDamageInShield(BattleUnitBuf_loaShield buf, int originDmg, DamageType type, BattleUnitModel attacker, KeywordBuf bufType, int resultDmg);

        /// <summary>
        /// 보호막이 있는 상태에서 흐트러짐 피해를 경감받은 경우 호출됩니다. 피해가 경감되지 않은 경우 호출되지 않습니다.
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="originDmg">보호막이 없을때 받았을 피해</param>
        /// <param name="type">공격 타입</param>
        /// <param name="attacker">공격자</param>
        /// <param name="bufType">버프로 피해 부여시 해당 버프 피해</param>
        /// <param name="resultDmg">보호막 처리로 인해 경감된 최종 피해량</param>
        void AfterHandleBreakDamageInShield(BattleUnitBuf_loaShield buf, int originDmg, DamageType type, BattleUnitModel attacker, KeywordBuf bufType, int resultDmg);


        /// <summary>
        /// 보호막 소멸시 호출됩니다. 개별 모드등에서 임의로 Destroy를 호출한경우 호출되지 않습니다.
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="reason">소멸된 이유 (공격, 턴 종료 등)</param>
        void OnDestroyShield(BattleUnitBuf_loaShield buf, LoAShieldDestroyReason reason);
        
        /// <summary>
        /// 기본적으로 보호막은 막 종료시 소멸합니다. 막 종료시 소멸 여부를 관리합니다.
        /// </summary>
        /// <param name="buf">대상 버프</param>
        /// <param name="callDestroy">해당 값이 false고, 보호막 수치가 0 이상이면 버프를 소멸시키지 않습니다.</param>
        void OnRoundEndInShield(BattleUnitBuf_loaShield buf, ref bool callDestroy);


        /// <summary>
        /// 자신에게 부여된 보호막의 수치가 감소되기 전에 호출
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="value">최종 감소될 수치</param>
        /// <param name="request">감소 행위</param>
        void BeforeTakeShieldReduce( BattleUnitBuf_loaShield buf, ref int value, LoAShieldReduceRequest request);

        /// <summary>
        /// 자신에게 부여된 보호막의 수치가 감소된 후에 호출
        /// </summary>
        /// <param name="buf">대상 보호막 버프</param>
        /// <param name="value">최종 감소된 수치</param>
        /// <param name="request">감소 행위</param>
        void AfterTakeShieldReduce(BattleUnitBuf_loaShield buf, int value, LoAShieldReduceRequest request);
    }
}
