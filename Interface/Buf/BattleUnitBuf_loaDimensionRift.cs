using LibraryOfAngela;
using LibraryOfAngela.Interface_External;
using LibraryOfAngela.Interface_Internal;
using System;
using UnityEngine;

// 차원 균열
public class BattleUnitBuf_loaDimensionRift : BattleUnitBuf, IHandleTakeRupture
{
    private DimensionRiftController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;

    public override KeywordBuf bufType => LoAKeywordBuf.DimensionRift;

    public override BufPositiveType positiveType => controller.positiveType;

    private bool isActivated = false;

    public override int paramInBufDesc => controller.GetParamInBufDesc(this);
    public BattleUnitBuf_loaDimensionRift()
    {
        controller = ServiceLocator.Instance.GetInstance<DimensionRiftController>();
    }

    public override void OnRoundStart()
    {
        base.OnRoundStart();
        isActivated = true;
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEndDimensionRift(this);
    }

    /// <summary>
    /// 자신에게 부여된 파열의 수치 감소가 발생할때 호출
    /// </summary>
    public virtual void OnTakeRuptureReduceStack(BattleUnitModel actor, BattleUnitBuf_loaRupture buf, ref int value, int originValue) {
        if (isActivated)
        {
            controller.OnTakeRuptureReduceStack(actor, buf, this, ref value, originValue);
        }
    }

    /// <summary>
    /// 자신에게 부여된 파열에 의해 피해를 받을때 피해량 제어
    /// </summary>
    public virtual void BeforeTakeRuptureDamage(BattleUnitBuf_loaRupture buf, ref int dmg, int originDmg) {

    }

    /// <summary>
    /// 자신에게 부여된 파열에 의해 피해를 받은 경우 호출
    /// </summary>
    public virtual void OnTakeRuptureDamage(BattleUnitBuf_loaRupture buf, int dmg) {

    }

    /// <summary>
    /// 자신에게 부여된 파열에 의해 사망한경우 호출
    /// </summary>
    public virtual void OnDieByRupture(BattleUnitModel actor, BattleUnitBuf_loaRupture buf) {

    }
}

namespace LibraryOfAngela.Interface_Internal
{
    internal interface DimensionRiftController
    {
        string keywordId { get; }
        string keywordIconId { get; }
        int GetParamInBufDesc(BattleUnitBuf_loaDimensionRift buf);
        BufPositiveType positiveType { get; }
        void OnRoundEndDimensionRift(BattleUnitBuf_loaDimensionRift buf);

        void OnTakeRuptureReduceStack(BattleUnitModel actor, BattleUnitBuf_loaRupture rupture, BattleUnitBuf_loaDimensionRift buf, ref int value, int originValue);
    }
} 