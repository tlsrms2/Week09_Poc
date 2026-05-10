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
        public bool overlapEffectFired;  // 이 블록의 겹침 효과가 이미 발동됐으면 true
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

        var cells = card.GetOccupiedCells();

        // 1. 겹치는 기존 블록 인덱스 수집
        bool hadOverlap = false;
        var overlappedIndices = new HashSet<int>();
        foreach (var (col, row, _) in cells)
        {
            int gx = originX + col;
            int gy = originY + row;
            if (grid[gx, gy] == SymbolType.None) continue;

            hadOverlap = true;
            for (int i = 0; i < placedBlocks.Count; i++)
            {
                var pb = placedBlocks[i];
                foreach (var (c, r, _) in pb.card.GetOccupiedCells())
                {
                    if (pb.originX + c == gx && pb.originY + r == gy)
                    {
                        overlappedIndices.Add(i);
                        break;
                    }
                }
            }
        }

        // 2. 그리드 업데이트
        foreach (var (col, row, symbol) in cells)
        {
            int gx = originX + col;
            int gy = originY + row;
            grid[gx, gy] = symbol;
            overlapCount[gx, gy]++;
        }

        // 3. 겹쳐진 기존 블록들의 겹침 효과 발동 (미발동 블록만)
        foreach (int idx in overlappedIndices)
        {
            if (!placedBlocks[idx].overlapEffectFired)
            {
                TriggerOverlapEffects(placedBlocks[idx].card);
                var pb = placedBlocks[idx];
                pb.overlapEffectFired = true;
                placedBlocks[idx] = pb;
            }
        }

        // 4. 새로 놓인 카드의 겹침 효과 발동
        if (hadOverlap)
            TriggerOverlapEffects(card);

        placedBlocks.Add(new PlacedBlock
        {
            card = card,
            originX = originX,
            originY = originY,
            overlapEffectFired = hadOverlap
        });
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
    public ResolutionResult GetPreview() => Calculate();

    private void CalculateAndRaiseResolution()
    {
        ResolutionResult result = Calculate();
        Debug.Log($"[GridManager] 결산 — 공격 {result.damage}, 방어 {result.defense}, 회복 {result.heal}, 드로우 +{result.draw}");
        ClearGrid();
        GameEvents.RaiseResolutionResult(result);
    }

    private void TriggerOverlapEffects(CardData card)
    {
        var result = new ResolutionResult();
        foreach (var effect in card.OverlapEffects)
            ApplyEffect(ref result, effect);

        Debug.Log($"[GridManager] {card.CardName} 겹침 효과 즉시 발동 — 공격 {result.damage}, 방어 {result.defense}, 회복 {result.heal}, 드로우 +{result.draw}");
        GameEvents.RaiseOverlapEffectTriggered(result);
    }

    private ResolutionResult Calculate()
    {
        var result = new ResolutionResult();

        foreach (var pb in placedBlocks)
        {
            foreach (var effect in pb.card.Effects)
                ApplyEffect(ref result, effect);
        }

        return result;
    }

    private static void ApplyEffect(ref ResolutionResult result, CardEffect effect)
    {
        switch (effect.effectType)
        {
            case CardType.Attack:  result.damage  += effect.power; break;
            case CardType.Defense: result.defense += effect.power; break;
            case CardType.Heal:    result.heal    += effect.power; break;
            case CardType.Draw:    result.draw    += effect.power; break;
            case CardType.DrawNow: result.drawNow += effect.power; break;
            case CardType.Drain:   result.damage  += effect.power;
                                   result.heal    += effect.power; break;
        }
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
