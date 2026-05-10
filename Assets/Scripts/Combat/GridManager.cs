using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3×3 그리드의 블록 배치 및 겹치기 연산을 담당하는 시스템.
/// 순수 데이터 로직만 처리하며 UI/시각화는 포함하지 않는다.
///
/// <para>
/// <b>배치 규칙:</b>
/// <list type="bullet">
///   <item>블록이 그리드를 벗어나면 배치 불가.</item>
///   <item>이미 점유된 셀에 겹치려면 SymbolType이 완전히 일치해야 한다.</item>
///   <item>겹친 셀 수만큼 해당 카드의 배율이 +1 증가한다.</item>
/// </list>
/// </para>
/// </summary>
public class GridManager : MonoBehaviour
{
    public const int GridSize = 3;

    // ── Grid State ──
    private readonly SymbolType[,] grid = new SymbolType[GridSize, GridSize];
    private readonly int[,] overlapCount = new int[GridSize, GridSize];
    private readonly List<PlacedBlock> placedBlocks = new();

    private struct PlacedBlock
    {
        public CardData card;
        public int originX, originY;
    }

    // ── Public Read-Only ──

    /// <summary> 해당 셀의 SymbolType을 반환한다. UI 미리보기 및 시각화에 활용. </summary>
    public SymbolType GetCell(int x, int y) => grid[x, y];

    /// <summary> 해당 셀에 쌓인 블록 수를 반환한다. 배율 표시에 활용. </summary>
    public int GetOverlapCount(int x, int y) => overlapCount[x, y];

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void OnEnable()
    {
        GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted;
        GameEvents.OnResolutionPhaseStarted += HandleResolutionPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
        GameEvents.OnResolutionPhaseStarted -= HandleResolutionPhaseStarted;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    /// <summary>
    /// 블록이 해당 위치에 놓일 수 있는지 검증한다.
    /// UI의 실시간 배치 미리보기(초록/빨강 표시)에서도 호출된다.
    /// </summary>
    /// <param name="block">배치하려는 블록 형태 데이터</param>
    /// <param name="originX">그리드 기준 좌측 열 인덱스 (0~2)</param>
    /// <param name="originY">그리드 기준 하단 행 인덱스 (0~2)</param>
    public bool CanPlaceBlock(CardData card, int originX, int originY)
    {
        if (card == null) return false;

        foreach (var (col, row, symbol) in card.GetOccupiedCells())
        {
            int gx = originX + col;
            int gy = originY + row;

            // 그리드 범위 초과
            if (gx < 0 || gx >= GridSize || gy < 0 || gy >= GridSize)
                return false;

            // 이미 점유된 셀이면 문양이 100% 일치해야만 겹칠 수 있음
            if (grid[gx, gy] != SymbolType.None && grid[gx, gy] != symbol)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 블록을 그리드에 배치한다.
    /// 성공 시 GameEvents.OnBlockPlaced를 발행하고 true를 반환한다.
    /// </summary>
    public bool TryPlaceBlock(CardData card, int originX, int originY)
    {
        if (card == null) return false;
        if (!CanPlaceBlock(card, originX, originY)) return false;

        foreach (var (col, row, symbol) in card.GetOccupiedCells())
        {
            int gx = originX + col;
            int gy = originY + row;

            grid[gx, gy] = symbol;
            overlapCount[gx, gy]++;
        }

        placedBlocks.Add(new PlacedBlock { card = card, originX = originX, originY = originY });
        GameEvents.RaiseBlockPlaced(card, originX, originY);

        Debug.Log($"[GridManager] {card.CardName} 배치 완료 ({originX}, {originY})");
        return true;
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    /// <summary>
    /// 현재 배치된 블록들의 예상 데미지/방어도를 반환한다. 이벤트는 발행하지 않는다.
    /// UI 미리보기에서 블록을 놓을 때마다 호출한다.
    /// </summary>
    public (int damage, int defense) GetPreview()
    {
        var (damage, defense, _) = Calculate(); 
        return (damage, defense);
    }

    /// <summary>
    /// 배치된 모든 블록을 순회해 최종 데미지/방어도를 계산하고 이벤트로 발행한다.
    /// </summary>
    private void CalculateAndRaiseResolution()
    {
        var (totalDamage, totalDefense, totalBonusDraw) = Calculate();
        Debug.Log($"[GridManager] 결산 — 공격 {totalDamage}, 방어 {totalDefense}, 추가드로우 {totalBonusDraw}");
    
        ClearGrid();
        
        GameEvents.RaiseResolutionResult(totalDamage, totalDefense, totalBonusDraw);
    }

    private (int damage, int defense, int bonusDraw) Calculate()
    {
        int totalDamage = 0;
        int totalDefense = 0;
        int totalBonusDraw = 0; // [추가됨]

        foreach (var pb in placedBlocks)
        {
            int overlapBonus = 0;
            foreach (var (col, row, _) in pb.card.GetOccupiedCells())
            {
                int gx = pb.originX + col;
                int gy = pb.originY + row;
                overlapBonus += overlapCount[gx, gy] - 1;
            }

            int multiplier = 1 + overlapBonus;
            int effectivePower = pb.card.BasePower * multiplier;

            if (pb.card.Type == CardType.Attack)
                totalDamage += effectivePower;
            else if (pb.card.Type == CardType.Defense)
                totalDefense += effectivePower;
            else if (pb.card.Type == CardType.Utility_NextTurnDraw)
                totalBonusDraw += effectivePower; // [추가됨]
        }

        return (totalDamage, totalDefense, totalBonusDraw);
    }

    private void ClearGrid()
    {
        for (int x = 0; x < GridSize; x++)
        for (int y = 0; y < GridSize; y++)
        {
            grid[x, y] = SymbolType.None;
            overlapCount[x, y] = 0;
        }

        placedBlocks.Clear();
        Debug.Log("[GridManager] 그리드 초기화");
    }

    // ─── Event Handlers ───

    private void HandleDrawPhaseStarted(int _) => ClearGrid();
    private void HandleResolutionPhaseStarted() => CalculateAndRaiseResolution();
}
