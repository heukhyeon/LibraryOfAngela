using LibraryOfAngela;
using LibraryOfAngela.Interface_Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 막 종료시 수치만큼 흐트러짐 피해를 받음. 대응하는 전투책장 키워드는 LoASinking_Keyword
/// </summary>
public class BattleUnitBuf_loaSinking : BattleUnitBuf
{
    private SinkingController controller;
    public override string keywordId => controller.keywordId;
    public override string keywordIconId => controller.keywordIconId;
    public override BufPositiveType positiveType => controller.positiveType;
    public override KeywordBuf bufType => LoAKeywordBuf.Sinking;

    public BattleUnitBuf_loaSinking()
    {
        controller = ServiceLocator.Instance.GetInstance<SinkingController>();
    }

    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        base.OnTakeDamageByAttack(atkDice, dmg);
        controller.OnTakeDamageByAttackSinking(this, atkDice, dmg);
    }

    public override void OnAddBuf(int addedStack)
    {
        base.OnAddBuf(addedStack);
        controller.OnAddBufSinking(this, addedStack);
    }

    public override void OnRoundEnd()
    {
        base.OnRoundEnd();
        controller.OnRoundEndSinking(this);
    }

    /// <summary>
    /// 침잠쇄도
    /// </summary>
    /// <param name="attacker"></param>
    public void Deluge(BattleUnitModel attacker)
    {
        controller.OnDeluge(this, attacker);
    }

    /// <summary>
    /// 자신에게 부여된 침잠이 턴 종료에 의해 수치 감소가 발생할때 호출
    /// </summary>
    public virtual void OnTakeSinkingReduceStack(ref int value, int originValue) { }

    /// <summary>
    /// 자신에게 부여된 침잠에 의해 흐트러짐 피해를 받을때 피해량 제어
    /// </summary>
    public virtual void BeforeTakeSinkingBreakDamage(ref int dmg, int originDmg) { }

    /// <summary>
    /// 자신에게 부여된 침잠에 의해 흐트러짐 피해를 받은 경우 호출
    /// </summary>
    public virtual void OnTakeSinkingBreakDamage(int dmg) { }

    /// <summary>
    /// 자신에게 부여된 침잠 흐트러짐 피해에 의해 흐트러질경우 호출
    /// </summary>
    public virtual void OnBreakStateBySinking(BattleUnitModel actor) { }

}

namespace LibraryOfAngela.Interface_Internal
{
    internal interface SinkingController
    {
        string keywordId { get; }
        string keywordIconId { get; }

        BufPositiveType positiveType { get; }


        void OnTakeDamageByAttackSinking(BattleUnitBuf_loaSinking buf, BattleDiceBehavior atkDice, int dmg);

        void OnRoundEndSinking(BattleUnitBuf_loaSinking buf);

        void OnAddBufSinking(BattleUnitBuf_loaSinking buf, int addedStack);

        void OnDeluge(BattleUnitBuf_loaSinking buf, BattleUnitModel attacker);
    }
}

