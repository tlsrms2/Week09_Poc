using System;

/// <summary>
/// 글로벌 이벤트 버스 — 시스템 간 결합도를 최소화하기 위한 정적 이벤트 허브.
/// 
/// <para>
/// <b>설계 원칙:</b>
/// <list type="bullet">
///   <item>모듈 간 직접 참조를 피하고, 이벤트 구독/발행으로만 통신한다.</item>
///   <item>이벤트 인자는 가능한 한 값 타입(struct) 또는 불변 객체를 사용한다.</item>
///   <item>구독 해제 누락을 방지하기 위해 OnDestroy에서 반드시 -= 해야 한다.</item>
/// </list>
/// </para>
/// </summary>
public static class GameEvents
{
    // ─── 전투 상태 ───
    /// <summary> 전투 시작 시 발행 </summary>
    public static event Action OnCombatStarted;
    /// <summary> 전투 종료 시 발행 (승리/패배 결과 포함) </summary>
    public static event Action<bool> OnCombatEnded;  // true = win, false = lose
    /// <summary> 전투 상태 전이 시 발행 (새 상태) </summary>
    public static event Action<CombatState> OnCombatStateChanged;

    // ─── 턴 흐름 ───
    /// <summary> 플레이어 드로우 페이즈 시작 </summary>
    public static event Action<int> OnDrawPhaseStarted;  // drawCount
    /// <summary> 배치 페이즈 시작 </summary>
    public static event Action OnPlacementPhaseStarted;
    /// <summary> 플레이어가 턴 종료 버튼을 누름 </summary>
    public static event Action OnTurnEndRequested;
    /// <summary> 결산 페이즈 시작 </summary>
    public static event Action OnResolutionPhaseStarted;
    /// <summary> 적 턴 시작 </summary>
    public static event Action OnEnemyTurnStarted;

    // ─── 덱 & 카드 ───
    /// <summary> 카드가 드로우되었을 때 (드로우된 카드 데이터) </summary>
    public static event Action<CardData> OnCardDrawn;
    /// <summary> 카드가 핸드에서 제거(사용)되었을 때 </summary>
    public static event Action<CardData> OnCardUsed;

    // ─── 그리드 & 블록 배치 ───
    /// <summary> 블록이 그리드에 배치되었을 때 (카드 데이터, 그리드 좌표) </summary>
    public static event Action<CardData, int, int> OnBlockPlaced;  // cardData, gridX, gridY

    // ─── 결산 ───
    /// <summary> 결산 결과 발행 </summary>
    public static event Action<ResolutionResult> OnResolutionResult;
    /// <summary> 적에게 데미지가 적용되었을 때 (적용된 데미지) </summary>
    public static event Action<int> OnDamageDealtToEnemy;
    /// <summary> 플레이어에게 데미지가 적용되었을 때 (적용된 데미지) </summary>
    public static event Action<int> OnDamageDealtToPlayer;

    // ─── 체력 변동 ───
    /// <summary> 플레이어 HP 변경 시 (현재HP, 최대HP) </summary>
    public static event Action<int, int> OnPlayerHpChanged;
    /// <summary> 적 HP 변경 시 (현재HP, 최대HP) </summary>
    public static event Action<int, int> OnEnemyHpChanged;
    /// <summary> 플레이어 방어도 변경 시 (현재 방어도) </summary>
    public static event Action<int> OnPlayerDefenseChanged;

    // ═══════════════════════════════════════════
    //  Invoke Methods (이벤트 발행 전용)
    // ═══════════════════════════════════════════

    public static void RaiseCombatStarted() => OnCombatStarted?.Invoke();
    public static void RaiseCombatEnded(bool win) => OnCombatEnded?.Invoke(win);
    public static void RaiseCombatStateChanged(CombatState state) => OnCombatStateChanged?.Invoke(state);

    public static void RaiseDrawPhaseStarted(int drawCount) => OnDrawPhaseStarted?.Invoke(drawCount);
    public static void RaisePlacementPhaseStarted() => OnPlacementPhaseStarted?.Invoke();
    public static void RaiseTurnEndRequested() => OnTurnEndRequested?.Invoke();
    public static void RaiseResolutionPhaseStarted() => OnResolutionPhaseStarted?.Invoke();
    public static void RaiseEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

    public static void RaiseCardDrawn(CardData card) => OnCardDrawn?.Invoke(card);
    public static void RaiseCardUsed(CardData card) => OnCardUsed?.Invoke(card);

    public static void RaiseBlockPlaced(CardData card, int x, int y) => OnBlockPlaced?.Invoke(card, x, y);

    public static void RaiseResolutionResult(ResolutionResult result) => OnResolutionResult?.Invoke(result);
    public static void RaiseDamageDealtToEnemy(int damage) => OnDamageDealtToEnemy?.Invoke(damage);
    public static void RaiseDamageDealtToPlayer(int damage) => OnDamageDealtToPlayer?.Invoke(damage);

    public static void RaisePlayerHpChanged(int current, int max) => OnPlayerHpChanged?.Invoke(current, max);
    public static void RaiseEnemyHpChanged(int current, int max) => OnEnemyHpChanged?.Invoke(current, max);
    public static void RaisePlayerDefenseChanged(int defense) => OnPlayerDefenseChanged?.Invoke(defense);

    /// <summary>
    /// 모든 이벤트 구독을 해제한다.
    /// 씬 전환 시 호출하여 좀비 리스너를 방지한다.
    /// </summary>
    public static void ClearAll()
    {
        OnCombatStarted = null;
        OnCombatEnded = null;
        OnCombatStateChanged = null;
        OnDrawPhaseStarted = null;
        OnPlacementPhaseStarted = null;
        OnTurnEndRequested = null;
        OnResolutionPhaseStarted = null;
        OnEnemyTurnStarted = null;
        OnCardDrawn = null;
        OnCardUsed = null;
        OnBlockPlaced = null;
        OnResolutionResult = null;
        OnDamageDealtToEnemy = null;
        OnDamageDealtToPlayer = null;
        OnPlayerHpChanged = null;
        OnEnemyHpChanged = null;
        OnPlayerDefenseChanged = null;
    }
}
