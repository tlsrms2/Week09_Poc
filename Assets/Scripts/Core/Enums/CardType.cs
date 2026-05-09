/// <summary>
/// 카드(스킬 블록)의 효과 유형.
/// 결산 시 데미지 적용 대상이나 방어도 부여 등 분기에 사용된다.
/// </summary>
public enum CardType
{
    Attack,     // 적에게 데미지
    Defense,    // 자신에게 방어도 부여
}
