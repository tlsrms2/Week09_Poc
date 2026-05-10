/// <summary>
/// 카드(스킬 블록)의 효과 유형.
/// 결산 시 데미지 적용 대상이나 방어도 부여 등 분기에 사용된다.
/// </summary>
public enum CardType
{
    // ── 결산 효과 (턴 종료 시 발동) ──
    Attack,     // 적에게 데미지
    Defense,    // 자신에게 방어도 부여
    Heal,       // 자신 HP 회복
    Drain,      // 적에게 데미지 + 자신 HP 회복
    Draw,       // 다음 턴 추가 드로우 (power = 드로우 매수)
    DrawNow,    // 즉시 드로우 (power = 드로우 매수)

    // ── 겹치기 효과 전용 (OverlapEffects 리스트에서만 사용) ──

    /// <summary>겹친 공격 카드의 결산 피해량을 power만큼 증가시킨다.</summary>
    OverlapBoostAttack,

    /// <summary>겹친 방어 카드의 결산 방어도를 power만큼 증가시킨다.</summary>
    OverlapBoostDefense,

    /// <summary>겹친 카드의 겹치기 효과가 한 번 더 발동된다. (중첩 불가)</summary>
    DoubleOverlapEffect,

    /// <summary>겹친 카드의 결산 효과를 즉시 발동시킨다.</summary>
    TriggerResolutionNow,

    /// <summary>이 카드의 결산 피해량이 power만큼 추가된다. (겹침 시 발동)</summary>
    BoostResolutionDamage,

    /// <summary>이 카드의 결산 방어도가 power만큼 추가된다. (겹침 시 발동)</summary>
    BoostResolutionDefense,
}
