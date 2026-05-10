/// <summary>
/// 카드(스킬 블록)의 효과 유형.
/// 결산 시 데미지 적용 대상이나 방어도 부여 등 분기에 사용된다.
/// </summary>
public enum CardType
{
    Attack,     // 적에게 데미지
    Defense,    // 자신에게 방어도 부여
    Heal,       // 자신 HP 회복
    Drain,      // 적에게 데미지 + 자신 HP 회복
    Draw,       // 다음 턴 추가 드로우 (power = 드로우 매수)
    DrawNow,    // 즉시 드로우 (power = 드로우 매수)
}
