using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 외부에서 LoA 공용 버프를 사용할때 참조할 키워드 목록
/// 사용 예시 : card.target.bufListDetail.AddKeywordBufByCard(LoAKeywordBuf.Sinking, 2, card.owner);
/// </summary>
public static class LoAKeywordBuf
{
    /// <summary>
    /// 침잠
    /// </summary>
    public static KeywordBuf Sinking { get; set; }

    /// <summary>
    /// 진동
    /// </summary>
    public static KeywordBuf Tremor { get; set; }

    /// <summary>
    /// 파열
    /// </summary>
    public static KeywordBuf Rupture { get; set; }

    /// <summary>
    /// 호흡
    /// </summary>
    public static KeywordBuf Poise { get; set; }

    /// <summary>
    /// 차원 균열
    /// </summary>
    public static KeywordBuf DimensionRift { get; set; }

    /// <summary>
    /// 보호막
    /// </summary>
    public static KeywordBuf Shield { get; set; }
    
    // 침잠쇄도는 '버프'가 아니므로 제외
}


/// <summary>
/// 버프를 감소시킬 때 보내는 요청
/// </summary>
public class LoAKeywordBufReduceRequest
{
    /// <summary>
    /// 요청을 보낸 유닛. null일 수 있습니다 (예: 라운드 종료)
    /// </summary>
    public BattleUnitModel Attacker { get; internal set; }

    /// <summary>
    /// 감소되는 값
    /// </summary>
    public int Stack { get; internal set; }

    /// <summary>
    /// 외부에서 직접 생성을 막기 위한 internal 생성자
    /// </summary>
    internal LoAKeywordBufReduceRequest()
    {

    }

    /// <summary>
    /// trigger의 타입에 따라 적절한 BufReduceRequest를 생성하여 반환하는 팩토리 메서드입니다.
    /// </summary>
    /// <param name="trigger">버프 감소를 유발한 객체입니다.</param>
    /// <param name="value">감소시킬 스택 값입니다.</param>
    /// <returns>생성된 LoAKeywordBufReduceRequest 인스턴스</returns>
    public static LoAKeywordBufReduceRequest Create(object trigger, int value)
    {
        // C# 7.0 이상의 타입 스위치를 사용하여 코드를 간결하게 만듭니다.
        switch (trigger)
        {
            case BattleDiceBehavior b:
                return new Attack(b, value);
            case EmotionCardAbilityBase e:
                return new Emotion(e, value);
            case BattleUnitBuf b:
                return new Buf(b, value);
            case PassiveAbilityBase p:
                return new Passive(p, value);
            case DiceCardSelfAbilityBase d:
                return new CardAbility(d, value);
            case DiceCardAbilityBase d:
                return new DiceAbility(d, value);
            case BattleUnitModel m:
                return new Etc(m, value);
            case null:
                return new RoundEnd(value);
            default:
                return new Etc(null, value);
        }
    }

    // --- Inner Classes for different trigger sources ---

    /// <summary>
    /// 주사위 효과로 인해 버프가 감소될 때의 요청
    /// </summary>
    internal class Attack : LoAKeywordBufReduceRequest
    {
        public readonly BattleDiceBehavior trigger;

        public Attack(BattleDiceBehavior behavior, int value)
        {
            Attacker = behavior.owner;
            Stack = value;
            trigger = behavior;
        }
    }

    /// <summary>
    /// 환상체 책장 효과로 인해 버프가 감소될 때의 요청
    /// </summary>
    internal class Emotion : LoAKeywordBufReduceRequest
    {
        public readonly EmotionCardAbilityBase trigger;

        public Emotion(EmotionCardAbilityBase ability, int value)
        {
            // 가정: EmotionCardAbilityBase에 'owner' 속성이 존재함
            Attacker = ability._owner;
            Stack = value;
            trigger = ability;
        }
    }

    /// <summary>
    /// 다른 버프의 효과로 인해 버프가 감소될 때의 요청
    /// </summary>
    internal class Buf : LoAKeywordBufReduceRequest
    {
        public readonly BattleUnitBuf trigger;

        public Buf(BattleUnitBuf buf, int value)
        {
            Attacker = buf._owner;
            Stack = value;
            trigger = buf;
        }
    }

    /// <summary>
    /// 패시브 능력으로 인해 버프가 감소될 때의 요청
    /// </summary>
    internal class Passive : LoAKeywordBufReduceRequest
    {
        public readonly PassiveAbilityBase trigger;

        public Passive(PassiveAbilityBase ability, int value)
        {
            Attacker = ability.owner;
            Stack = value;
            trigger = ability;
        }
    }

    /// <summary>
    /// 카드 자체의 능력(SelfAbility)으로 인해 버프가 감소될 때의 요청
    /// </summary>
    internal class CardAbility : LoAKeywordBufReduceRequest
    {
        public readonly DiceCardSelfAbilityBase trigger;

        public CardAbility(DiceCardSelfAbilityBase ability, int value)
        {
            Attacker = ability.owner;
            Stack = value;
            trigger = ability;
        }
    }

    /// <summary>
    /// 카드 내 주사위의 능력(Ability)으로 인해 버프가 감소될 때의 요청
    /// </summary>
    internal class DiceAbility : LoAKeywordBufReduceRequest
    {
        public readonly DiceCardAbilityBase trigger;

        public DiceAbility(DiceCardAbilityBase ability, int value)
        {
            // 가정: DiceCardAbilityBase에 'owner' 속성이 존재함
            Attacker = ability.owner;
            Stack = value;
            trigger = ability;
        }
    }

    /// <summary>
    /// 라운드 종료 시점에 버프가 감소될 때의 요청
    /// </summary>
    internal class RoundEnd : LoAKeywordBufReduceRequest
    {
        public RoundEnd(int value)
        {
            Attacker = null; // 라운드 종료는 특정 공격자가 없음
            Stack = value;
        }
    }

    /// <summary>
    /// 기타 명시되지 않은 이유로 버프가 감소될 때의 요청 (주로 특정 유닛이 원인)
    /// </summary>
    internal class Etc : LoAKeywordBufReduceRequest
    {
        public readonly BattleUnitModel trigger;

        public Etc(BattleUnitModel unit, int value)
        {
            Attacker = unit;
            Stack = value;
            trigger = unit;
        }
    }
}
