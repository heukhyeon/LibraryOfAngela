using LibraryOfAngela;
using LibraryOfAngela.Interface_Internal;

public class BattleUnitBuf_loaPoise : BattleUnitBuf
{
    private PoiseController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;
    
    public override KeywordBuf bufType => LoAKeywordBuf.Poise; 

    public BattleUnitBuf_loaPoise()
    {
        controller = ServiceLocator.Instance.GetInstance<PoiseController>();
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEndPoise(this);
    }

    public override void BeforeGiveDamage(BattleDiceBehavior behavior) 
    {
         base.BeforeGiveDamage(behavior);
         controller.BeforeGiveDamagePoise(this, behavior);
    }

    /// <summary>
    /// 자신에게 부여된 호흡의 수치 감소가 발생할때 호출
    /// </summary>
    public virtual void OnTakePoiseReduceStack(int originValue, ref int value) { }

    /// <summary>
    /// 자신에게 부여된 호흡의 크리티컬 발생 확률을 계산할때 호출. 수비주사위에 대해서도 호출되며 이 경우는 기본 chance가 0임
    /// </summary>
    public virtual void BeforeJudgingCritical(BattleDiceBehavior behaviour, float originalChance, ref float criticalChance) { }

    /// <summary>
    /// 크리티컬 발생시 호출
    /// </summary>
    public virtual void OnCriticalActivated(BattleDiceBehavior behaviour, float originalDmgRate, float originalBreakDmgRate, ref float currentDmgRate, ref float currentBreakDmgRate) { }

}

namespace LibraryOfAngela.Interface_Internal
{
    internal interface PoiseController
    {
        string keywordId { get; }
        string keywordIconId { get; }

        void OnRoundEndPoise(BattleUnitBuf buf);
        void BeforeGiveDamagePoise(BattleUnitBuf buf, BattleDiceBehavior behavior); 
    }
} 