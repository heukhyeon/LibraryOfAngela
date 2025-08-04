using LibraryOfAngela;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using UnityEngine;

/// <summary>
/// 막 종료시 2/3로 감소, 진동폭발 발생시 수치만큼 흐트러짐 피해를 받음. 대응 키워드는 LoATremor_Keyword
/// </summary>
public class BattleUnitBuf_loaTremor : BattleUnitBuf, IHandleAddNewKeywordBufInList
{
    private TremorController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;
    
    public override KeywordBuf bufType => LoAKeywordBuf.Tremor;

    public override BufPositiveType positiveType => controller.positiveType;

    /// <summary>
    /// 진동 폭발시 이펙트 색상
    /// 기본 진동은 외부에서 색상을 지정해주면 해당 색상을 따라가지만
    /// 개별 상속된 진동은 자신만의 색상으로 고정할 수 있음
    /// </summary>
    public virtual Color BurstColor { get; set; } = Color.white;


    public BattleUnitBuf_loaTremor()
    {
        controller = ServiceLocator.Instance.GetInstance<TremorController>();
    }

    /// <summary>
    /// 막 종료시 2/3 으로 감소한다.
    /// </summary>
    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEndTremor(this);
    }

    /// <summary>
    /// 전투 책장에 의해 진동폭발을 실행한다. 기본적으로 수치만큼 흐트러짐 피해를 받고 진폭변환된경우 추가적인 효과를 발생한다.
    /// </summary>
    /// <param name="actor">진동폭발을 발생시키는 공격자</param>
    public virtual void BurstByCard(BattleUnitModel actor)
    {
        controller.Burst(actor, this, true);
    }

    /// <summary>
    /// 전투 책장 이외의 방법으로 진동폭발을 실행한다. 기본적으로 수치만큼 흐트러짐 피해를 받고 진폭변환된경우 추가적인 효과를 발생한다.
    /// </summary>
    /// <param name="actor">진동폭발을 발생시키는 공격자</param>
    public virtual void BurstByEtc(BattleUnitModel actor)
    {
        controller.Burst(actor, this, false);
    }

    /// <summary>
    /// 진동의 수치를 감소시킴.
    /// </summary>
    /// <param name="request">줄여지는 사유. '진동 폭발 후 대상의 진동을 n 감소'와 같은 효과가 있을때, 이 값은 <see cref="LoATremorReduceRequest.TremorBurst"/>를 사용하길 권합니다.</param>
    public virtual void ReduceStack(LoAKeywordBufReduceRequest request)
    {
        controller.ReduceStack(this, request);
    }

    /// <summary>
    /// 전투 책장에 의해 진폭변환을 실행한다. 현재 버프가 파괴되고 해당 타입의 버프가 추가된다.
    /// </summary>
    /// <typeparam name="T">진동을 상속하는 클래스 타입</typeparam>
    /// <param name="actor">진폭변환을 발생시키는 공격자</param>
    /// <returns>현재 진동을 대체하는 해당 클래스 객체</returns>
    public virtual T TransformByCard<T>(BattleUnitModel actor) where T : BattleUnitBuf_loaTremor, new()
    {
        return controller.TremorTransform<T>(actor, this, true);
    }

    /// <summary>
    /// 전투 책장이외 (버프, 패시브 등)에 의해 진폭변환을 실행한다. 현재 버프가 파괴되고 해당 타입의 버프가 추가된다.
    /// </summary>
    /// <typeparam name="T">진동을 상속하는 클래스 타입</typeparam>
    /// <param name="actor">진폭변환을 발생시키는 공격자</param>
    /// <returns>현재 진동을 대체하는 해당 클래스 객체</returns>
    public virtual T TransformByEtc<T>(BattleUnitModel actor) where T : BattleUnitBuf_loaTremor, new()
    {
        return controller.TremorTransform<T>(actor, this, false);
    }

    /// <summary>
    /// 자신에게 진동폭발이 발생한 경우 때 진동 폭발 흐트러짐 피해를 적용하기 전에 호출
    /// </summary>
    public virtual void BeforeTakeTremorBurst(BattleUnitModel actor, ref int dmg, int originDmg) { }

    /// <summary>
    /// 자신에게 진동폭발이 발생한 경우 호출
    /// </summary>
    public virtual void OnTakeTremorBurst(BattleUnitModel actor, int dmg, bool isCard) { }

    /// <summary>
    /// 자신에게 진폭변환이 발생할 경우 호출
    /// </summary>
    /// <param name="actor">진폭변환을 발생시킨 공격자</param>
    /// <param name="next">치환될 진동 버프</param>
    /// <returns>반환값이 false라면 진폭 변환을 허용하지않고 무시시킵니다.</returns>
    public virtual bool OnTakeTremorTransform(BattleUnitModel actor, BattleUnitBuf_loaTremor next) 
    {
        return true;
    }

    /// <summary>
    /// 자신의 진동의 수치감소가 발생할 경우 호출
    /// </summary>
    public virtual void OnTakeTremorReduceStack(ref int value, LoAKeywordBufReduceRequest request) { }

    /// <summary>
    /// 자신이 진동폭발의 처리도중 흐트러진경우 호출
    /// </summary>
    public virtual void OnBreakStateByTremorBurst(BattleUnitModel actor, bool isCard) { }

    /// <summary>
    /// 자신이 진동폭발의 처리도중 사망한경우 호출
    /// </summary>
    public virtual void OnDieByTremorBurst(BattleUnitModel actor, bool isCard) { }

    /// <summary>
    /// 자신에게 진동이 부여될때 자신의 진동 타입에 맞게 부여될 버프를 변환
    /// </summary>
    /// <param name="bufType"><see cref="LoAKeywordBuf.Tremor"/>가 아니면 무시</param>
    /// <param name="current">현재 버프. <see cref="BattleUnitBuf_loaTremor"/>.</param>
    /// <param name="readyType">현재 버프가 부여될 범위</param>
    /// <returns></returns>
    public BattleUnitBuf OnAddNewKeywordBufInList(KeywordBuf bufType, BattleUnitBuf current, BufReadyType readyType)
    {
        if (bufType != this.bufType) return current;
        return controller.FixValidCreateTremor(this, current, readyType);
    }
}

public class LoATremorReduceRequest : LoAKeywordBufReduceRequest
{
    private LoATremorReduceRequest()
    {

    }

    public class TremorBurst : LoATremorReduceRequest
    {
        public readonly bool isCard;
        public TremorBurst(BattleUnitModel attacker, int stack, bool card)
        {
            Attacker = attacker;
            Stack = stack;
            isCard = card;
        }
    }
}

namespace LibraryOfAngela.Interface_Internal
{
    internal interface TremorController
    {
        string keywordId { get; }
        string keywordIconId { get; }

        BufPositiveType positiveType { get; }

        void OnRoundEndTremor(BattleUnitBuf_loaTremor buf);
        void Burst(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, bool isCard);

        void ReduceStack(BattleUnitBuf_loaTremor buf, LoAKeywordBufReduceRequest request);

        T TremorTransform<T>(BattleUnitModel actor, BattleUnitBuf_loaTremor current, bool isCard) where T : BattleUnitBuf_loaTremor, new();

        BattleUnitBuf FixValidCreateTremor(BattleUnitBuf_loaTremor buf, BattleUnitBuf current, BufReadyType readyType);
    }
} 