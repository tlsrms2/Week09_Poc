/// <summary>
/// 턴 결산 결과. 새 효과 추가 시 필드만 늘리면 된다.
/// </summary>
public struct ResolutionResult
{
    public int damage;   // 적에게 줄 데미지
    public int defense;  // 플레이어에게 줄 방어도
    public int heal;     // 플레이어 HP 회복량
    public int draw;     // 추가 드로우 매수
}
