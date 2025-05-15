using LibraryOfAngela;
using LibraryOfAngela.Interface_Internal;

// 차원 균열
public class BattleUnitBuf_loaDimensionRift : BattleUnitBuf, IHandleTakeRupture
{
    private DimentionLiftController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;

    public override KeywordBuf bufType => LoAKeywordBuf.DimensionRift; 

    public BattleUnitBuf_loaDimensionRift()
    {
        controller = ServiceLocator.Instance.GetInstance<DimentionLiftController>();
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
        controller.OnTakeRuptureReduceStack(actor, buf, this, ref value, originValue);
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
    internal interface DimentionLiftController
    {
        string keywordId { get; }
        string keywordIconId { get; }

        void OnRoundEndDimensionRift(BattleUnitBuf_loaDimensionRift buf);

        void OnTakeRuptureReduceStack(BattleUnitModel actor, BattleUnitBuf_loaRupture rupture, BattleUnitBuf_loaDimensionRift buf, ref int value, int originValue);
    }
} 