using LibraryOfAngela;
using LibraryOfAngela.Interface_Internal;

public class BattleUnitBuf_loaRupture : BattleUnitBuf
{
    private RuptureController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;

    public override KeywordBuf bufType => LoAKeywordBuf.Rupture; 

    public BattleUnitBuf_loaRupture()
    {
        controller = ServiceLocator.Instance.GetInstance<RuptureController>();
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEndRupture(this);
    }

    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);
        controller.OnTakeDamageByAttackRupture(this, atkDice, dmg);
    }

    /// <summary>
    /// 자신에게 부여된 파열의 수치 감소가 발생할때 호출
    /// </summary>
    public virtual void OnTakeRuptureReduceStack(BattleUnitModel actor, ref int value, int originValue) { }

    /// <summary>
    /// 자신에게 부여된 파열에 의해 피해를 받을때 피해량 제어
    /// </summary>
    public virtual void BeforeTakeRuptureDamage(ref int dmg, int originDmg) { }

    /// <summary>
    /// 자신에게 부여된 파열에 의해 피해를 받은 경우 호출
    /// </summary>
    public virtual void OnTakeRuptureDamage(int dmg) { }

    /// <summary>
    /// 자신에게 부여된 파열에 의해 사망한경우 호출
    /// </summary>
    public virtual void OnDieByRupture(BattleUnitModel actor) { }
}

namespace LibraryOfAngela.Interface_Internal
{
    internal interface RuptureController
    {
        string keywordId { get; }
        string keywordIconId { get; }

        void OnRoundEndRupture(BattleUnitBuf buf);
        void OnTakeDamageByAttackRupture(BattleUnitBuf buf, BattleDiceBehavior atkDice, int dmg);
    }
} 