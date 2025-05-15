using LibraryOfAngela;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;

public class BattleUnitBuf_loaTremor : BattleUnitBuf, IHandleAddNewKeywordBufInList
{
    private TremorController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;
    
    public override KeywordBuf bufType => LoAKeywordBuf.Tremor;

    public override BufPositiveType positiveType => controller.positiveType;

    public BattleUnitBuf_loaTremor()
    {
        controller = ServiceLocator.Instance.GetInstance<TremorController>();
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEndTremor(this);
    }

    public virtual void BurstByCard(BattleUnitModel actor)
    {
        controller.Burst(actor, this, true);
    }

    public virtual void BurstByEtc(BattleUnitModel actor)
    {
        controller.Burst(actor, this, false);
    }

    public virtual void ReduceStack(bool isCard, int value, BattleUnitModel actor = null)
    {
        controller.ReduceStack(actor, this, value, isCard);
    }

    public virtual T TransformByCard<T>(BattleUnitModel actor) where T : BattleUnitBuf_loaTremor, new()
    {
        return controller.TremorTransform<T>(actor, this, true);
    }

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
    public virtual void OnTakeTremorTransform(BattleUnitModel actor, BattleUnitBuf_loaTremor next) { }

    /// <summary>
    /// 자신의 진동의 수치감소가 발생할 경우 호출
    /// </summary>
    public virtual void OnTakeTremorReduceStack(BattleUnitModel actor, ref int value, int originValue, bool isFromRoundEnd) { }

    /// <summary>
    /// 자신이 진동폭발의 처리도중 흐트러진경우 호출
    /// </summary>
    public virtual void OnBreakStateByTremorBurst(BattleUnitModel actor, bool isCard) { }

    /// <summary>
    /// 자신이 진동폭발의 처리도중 사망한경우 호출
    /// </summary>
    public virtual void OnDieByTremorBurst(BattleUnitModel actor, bool isCard) { }

    public BattleUnitBuf OnAddNewKeywordBufInList(KeywordBuf bufType, BattleUnitBuf current, BufReadyType readyType)
    {
        if (bufType != this.bufType) return current;
        return controller.FixValidCreateTremor(this, current, readyType);
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

        void ReduceStack(BattleUnitModel actor, BattleUnitBuf_loaTremor buf, int value, bool isRoundEnd);

        T TremorTransform<T>(BattleUnitModel actor, BattleUnitBuf_loaTremor current, bool isCard) where T : BattleUnitBuf_loaTremor, new();

        BattleUnitBuf FixValidCreateTremor(BattleUnitBuf_loaTremor buf, BattleUnitBuf current, BufReadyType readyType);
    }
} 