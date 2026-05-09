using UnityEngine;

/// <summary>
/// 전투 유닛(플레이어 또는 적)의 런타임 상태를 캡슐화하는 컴포넌트.
/// HP와 방어도를 관리하며, 변경 시 이벤트를 발행한다.
/// </summary>
public class CombatUnit : MonoBehaviour
{
    [Header("유닛 설정")]
    [SerializeField] private bool isPlayer = true;

    [SerializeField] private int maxHp = 50;

    [SerializeField] private int currentHp;
    private int defense;

    // ── Public Properties ──
    public bool IsPlayer => isPlayer;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int Defense => defense;
    public bool IsDead => currentHp <= 0;

    private void Awake()
    {
        currentHp = maxHp;
        defense = 0;
        BroadcastHpChanged();
    }

    /// <summary>
    /// 방어도를 설정(추가)한다. 이전 방어도를 덮어쓰는 게 아닌 누적이다.
    /// </summary>
    public void AddDefense(int amount)
    {
        if (amount <= 0) return;
        defense += amount;

        if (isPlayer)
            GameEvents.RaisePlayerDefenseChanged(defense);
    }

    /// <summary>
    /// 방어도를 초기화한다. 새 턴 시작 시 호출한다.
    /// </summary>
    public void ResetDefense()
    {
        defense = 0;

        if (isPlayer)
            GameEvents.RaisePlayerDefenseChanged(defense);
    }

    /// <summary>
    /// 데미지를 적용한다. 방어도 → HP 순으로 감산한다.
    /// </summary>
    /// <returns>실제로 HP에 적용된 순수 데미지량</returns>
    public int TakeDamage(int rawDamage)
    {
        if (rawDamage <= 0) return 0;

        // 방어도로 먼저 흡수
        int absorbed = Mathf.Min(defense, rawDamage);
        defense -= absorbed;
        int remainingDamage = rawDamage - absorbed;

        // 남은 데미지를 HP에 적용
        currentHp = Mathf.Max(0, currentHp - remainingDamage);

        BroadcastHpChanged();

        if (isPlayer)
        {
            GameEvents.RaisePlayerDefenseChanged(defense);
            if (remainingDamage > 0)
                GameEvents.RaiseDamageDealtToPlayer(remainingDamage);
        }
        else
        {
            if (remainingDamage > 0)
                GameEvents.RaiseDamageDealtToEnemy(remainingDamage);
        }

        return remainingDamage;
    }

    /// <summary>
    /// HP를 회복한다. 최대 HP를 초과하지 않는다.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        BroadcastHpChanged();
    }

    private void BroadcastHpChanged()
    {
        if (isPlayer)
            GameEvents.RaisePlayerHpChanged(currentHp, maxHp);
        else
            GameEvents.RaiseEnemyHpChanged(currentHp, maxHp);
    }
}
