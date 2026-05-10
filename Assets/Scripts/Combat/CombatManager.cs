using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private CombatUnit player;
    [SerializeField] private CombatUnit enemy;

    [Header("적 설정")]
    [SerializeField] private int enemyBaseDamage = 8;
    public int EnemyBaseDamage => enemyBaseDamage;

    [Header("페이즈 딜레이")]
    [Tooltip("턴 종료 후 결산 시작까지 대기")]
    [SerializeField] private float resolutionDelay = 0.5f;
    [Tooltip("결산 결과 보여주는 시간")]
    [SerializeField] private float resolutionDisplayTime = 1f;
    [Tooltip("적 공격 전 대기")]
    [SerializeField] private float enemyAttackDelay = 1f;
    [Tooltip("적 공격 후 다음 턴까지 대기")]
    [SerializeField] private float postAttackDelay = 0.5f;

    // ── Runtime State ──
    private CombatState currentState = CombatState.None;
    public CombatState CurrentState => currentState;

    private int turnCount;
    public int TurnCount => turnCount;

    private int extraDrawNextTurn;
    private bool enemyDeadAfterResolution;
    private bool resolutionComplete;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnTurnEndRequested       += HandleTurnEndRequested;
        GameEvents.OnResolutionResult       += HandleResolutionResult;
        GameEvents.OnResolutionComplete     += HandleResolutionComplete;
        GameEvents.OnOverlapEffectTriggered += HandleOverlapEffectTriggered;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnEndRequested       -= HandleTurnEndRequested;
        GameEvents.OnResolutionResult       -= HandleResolutionResult;
        GameEvents.OnResolutionComplete     -= HandleResolutionComplete;
        GameEvents.OnOverlapEffectTriggered -= HandleOverlapEffectTriggered;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

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
            case CombatState.PlayerDraw: EnterPlayerDraw(); break;
            case CombatState.Placement:  EnterPlacement();  break;
            case CombatState.Resolution: EnterResolution(); break;
            case CombatState.EnemyTurn:  EnterEnemyTurn();  break;
            case CombatState.Win:
            case CombatState.Lose:       EnterCombatEnd(newState == CombatState.Win); break;
        }
    }

    // ─── State Enter Methods ───

    private void EnterPlayerDraw()
    {
        turnCount++;
        player.ResetDefense();

        int drawCount = deckManager.DrawCountPerTurn + extraDrawNextTurn;
        extraDrawNextTurn = 0;

        GameEvents.RaiseDrawPhaseStarted(drawCount);
        deckManager.DrawCards(drawCount);
        TransitionTo(CombatState.Placement);
    }

    private void EnterPlacement()
    {
        GameEvents.RaisePlacementPhaseStarted();
    }

    private void EnterResolution()
    {
        StartCoroutine(ResolutionRoutine());
    }

    private void EnterEnemyTurn()
    {
        StartCoroutine(EnemyTurnRoutine());
    }

    private void EnterCombatEnd(bool win)
    {
        GameEvents.RaiseCombatEnded(win);
        Debug.Log($"[CombatManager] 전투 종료 — {(win ? "승리" : "패배")}");
    }

    // ─── Coroutines ───

    private IEnumerator ResolutionRoutine()
    {
        yield return new WaitForSeconds(resolutionDelay);

        resolutionComplete = false;
        GameEvents.RaiseResolutionPhaseStarted();

        // 블록들이 하나씩 결산 완료될 때까지 대기
        yield return new WaitUntil(() => resolutionComplete);

        yield return new WaitForSeconds(resolutionDisplayTime);

        if (enemyDeadAfterResolution)
            TransitionTo(CombatState.Win);
        else
            TransitionTo(CombatState.EnemyTurn);
    }

    private IEnumerator EnemyTurnRoutine()
    {
        GameEvents.RaiseEnemyTurnStarted();

        yield return new WaitForSeconds(enemyAttackDelay);

        player.TakeDamage(enemyBaseDamage);

        if (player.IsDead)
        {
            TransitionTo(CombatState.Lose);
            yield break;
        }

        yield return new WaitForSeconds(postAttackDelay);

        TransitionTo(CombatState.PlayerDraw);
    }

    // ─── Event Handlers ───

    private void HandleOverlapEffectTriggered(ResolutionResult result)
    {
        if (result.damage > 0)  enemy.TakeDamage(result.damage);
        if (result.defense > 0) player.AddDefense(result.defense);
        if (result.heal > 0)    player.Heal(result.heal);
        if (result.draw > 0)    extraDrawNextTurn += result.draw;
        if (result.drawNow > 0) deckManager.DrawCards(result.drawNow);

        if (enemy.IsDead) TransitionTo(CombatState.Win);
    }

    private void HandleTurnEndRequested()
    {
        if (currentState != CombatState.Placement)
        {
            Debug.LogWarning("[CombatManager] 배치 페이즈가 아닌데 턴 종료 요청이 옴 — 무시");
            return;
        }

        TransitionTo(CombatState.Resolution);
    }

    // 블록 하나씩 결산 결과 수신 — 수치만 반영
    private void HandleResolutionResult(ResolutionResult result)
    {
        if (currentState != CombatState.Resolution) return;

        if (result.damage > 0)  enemy.TakeDamage(result.damage);
        if (result.defense > 0) player.AddDefense(result.defense);
        if (result.heal > 0)    player.Heal(result.heal);
        if (result.draw > 0)    extraDrawNextTurn += result.draw;
    }

    // 전체 결산 완료 — 손패 버리기 및 승패 기록
    private void HandleResolutionComplete()
    {
        deckManager.DiscardHand();
        enemyDeadAfterResolution = enemy.IsDead;
        resolutionComplete = true;
    }
}
