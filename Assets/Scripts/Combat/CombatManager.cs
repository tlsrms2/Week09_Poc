using UnityEngine;

/// <summary>
/// 전투 흐름을 제어하는 상태 기계(State Machine).
/// PlayerDraw → Placement → Resolution → EnemyTurn 순환을 관리한다.
/// 
/// <para>
/// <b>의존 관계:</b>
/// <list type="bullet">
///   <item><see cref="DeckManager"/> — 카드 드로우/버리기</item>
///   <item><see cref="CombatUnit"/> — HP/방어도 적용</item>
///   <item><see cref="GameEvents"/> — 이벤트 기반 통신</item>
/// </list>
/// </para>
/// </summary>
public class CombatManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private CombatUnit player;
    [SerializeField] private CombatUnit enemy;

    [Header("적 설정")]
    [Tooltip("적의 턴 당 기본 공격력")]
    [SerializeField] private int enemyBaseDamage = 8;

    // ── Runtime State ──
    private CombatState currentState = CombatState.None;
    public CombatState CurrentState => currentState;

    private int turnCount;
    public int TurnCount => turnCount;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnTurnEndRequested += HandleTurnEndRequested;
        GameEvents.OnResolutionResult += HandleResolutionResult;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnEndRequested -= HandleTurnEndRequested;
        GameEvents.OnResolutionResult -= HandleResolutionResult;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    /// <summary>
    /// 전투를 시작한다. 씬 로드 후 또는 전투 시작 UI에서 호출.
    /// </summary>
    public void StartCombat()
    {
        turnCount = 0;
        deckManager.Initialize();
        GameEvents.RaiseCombatStarted();

        TransitionTo(CombatState.PlayerDraw);
    }

    // ═══════════════════════════════════════════
    //  State Machine
    // ═══════════════════════════════════════════

    private void TransitionTo(CombatState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        GameEvents.RaiseCombatStateChanged(newState);

        Debug.Log($"[CombatManager] State → {newState}");

        switch (newState)
        {
            case CombatState.PlayerDraw:
                EnterPlayerDraw();
                break;
            case CombatState.Placement:
                EnterPlacement();
                break;
            case CombatState.Resolution:
                EnterResolution();
                break;
            case CombatState.EnemyTurn:
                EnterEnemyTurn();
                break;
            case CombatState.Win:
            case CombatState.Lose:
                EnterCombatEnd(newState == CombatState.Win);
                break;
        }
    }

    // ─── State Enter Methods ───

    private void EnterPlayerDraw()
    {
        turnCount++;
        player.ResetDefense();

        deckManager.DrawCards();
        GameEvents.RaiseDrawPhaseStarted(deckManager.DrawCountPerTurn);

        // 드로우 후 바로 배치 페이즈로 전이
        TransitionTo(CombatState.Placement);
    }

    private void EnterPlacement()
    {
        GameEvents.RaisePlacementPhaseStarted();
        // 플레이어의 블록 배치를 기다림 — 턴 종료 요청 이벤트로 탈출
    }

    private void EnterResolution()
    {
        GameEvents.RaiseResolutionPhaseStarted();
        // Dev A의 GridManager가 결산 결과를 계산하고
        // GameEvents.RaiseResolutionResult()를 호출하면
        // HandleResolutionResult()에서 다음 상태로 전이
    }

    private void EnterEnemyTurn()
    {
        GameEvents.RaiseEnemyTurnStarted();

        // 적의 공격 — POC에서는 단순 고정 데미지
        player.TakeDamage(enemyBaseDamage);

        // 승패 판정
        if (player.IsDead)
        {
            TransitionTo(CombatState.Lose);
            return;
        }

        // 다음 턴 시작
        TransitionTo(CombatState.PlayerDraw);
    }

    private void EnterCombatEnd(bool win)
    {
        GameEvents.RaiseCombatEnded(win);
        Debug.Log($"[CombatManager] 전투 종료 — {(win ? "승리" : "패배")}");
    }

    // ─── Event Handlers ───

    /// <summary>
    /// 플레이어가 턴 종료 버튼을 눌렀을 때 호출.
    /// 배치 페이즈에서만 반응한다.
    /// </summary>
    private void HandleTurnEndRequested()
    {
        if (currentState != CombatState.Placement)
        {
            Debug.LogWarning("[CombatManager] 배치 페이즈가 아닌데 턴 종료 요청이 옴 — 무시");
            return;
        }

        TransitionTo(CombatState.Resolution);
    }

    /// <summary>
    /// 결산 결과를 받아서 데미지/방어도를 적용하고 적 턴으로 전이한다.
    /// Dev A의 GridManager가 결산을 마치고 이 이벤트를 발행한다.
    /// </summary>
    private void HandleResolutionResult(int totalDamage, int totalDefense)
    {
        if (currentState != CombatState.Resolution) return;

        // 공격 적용
        if (totalDamage > 0)
            enemy.TakeDamage(totalDamage);

        // 방어 적용
        if (totalDefense > 0)
            player.AddDefense(totalDefense);

        // 핸드에 남은 카드 버리기
        deckManager.DiscardHand();

        // 승패 판정
        if (enemy.IsDead)
        {
            TransitionTo(CombatState.Win);
            return;
        }

        TransitionTo(CombatState.EnemyTurn);
    }
}
