using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public const int GridSize = 3;

    [Header("연출")]
    [Tooltip("블록 하나 결산 후 다음 블록까지 대기 시간")]
    [SerializeField] private float blockResolveDelay = 0.4f;

    // ── Grid State ──
    private readonly SymbolType[,] grid = new SymbolType[GridSize, GridSize];
    private readonly int[,] overlapCount = new int[GridSize, GridSize];
    private readonly List<PlacedBlock> placedBlocks = new();

    private struct PlacedBlock
    {
        public CardData card;
        public int originX, originY;
        /// <summary>BoostResolutionDamage 겹침 효과로 쌓인 추가 피해량</summary>
        public int bonusDamage;
        /// <summary>BoostResolutionDefense 겹침 효과로 쌓인 추가 방어도</summary>
        public int bonusDefense;
        /// <summary>DoubleOverlapEffect 적용 여부 (중첩 불가)</summary>
        public bool doubleOverlapApplied;
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
        GameEvents.OnDrawPhaseStarted       += HandleDrawPhaseStarted;
        GameEvents.OnResolutionPhaseStarted += HandleResolutionPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnDrawPhaseStarted       -= HandleDrawPhaseStarted;
        GameEvents.OnResolutionPhaseStarted -= HandleResolutionPhaseStarted;
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    public bool CanPlaceBlock(CardData card, int originX, int originY)
    {
        if (card == null) return false;

        foreach (var (col, row, symbol) in card.GetOccupiedCells())
        {
            int gx = originX + col;
            int gy = originY + row;

            if (gx < 0 || gx >= GridSize || gy < 0 || gy >= GridSize)
                return false;

            if (grid[gx, gy] != SymbolType.None && grid[gx, gy] != symbol)
                return false;
        }

        return true;
    }

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

        // 새로 추가될 블록의 초기 상태
        var newBlock = new PlacedBlock { card = card, originX = originX, originY = originY };

        // 3. 겹쳐진 기존 블록들의 겹침 효과 발동
        foreach (int idx in overlappedIndices)
        {
            var targetBlock = placedBlocks[idx];
            ProcessOverlapEffects(targetBlock.card, card, ref targetBlock, ref newBlock);
            placedBlocks[idx] = targetBlock;  // struct이므로 재할당
        }

        // 4. 새로 놓인 카드의 겹침 효과 발동
        if (hadOverlap)
        {
            // newBlock의 겹침 효과는 아래에 깔린 블록들에 영향을 줌
            foreach (int idx in overlappedIndices)
            {
                var targetBlock = placedBlocks[idx];
                ProcessOverlapEffects(card, targetBlock.card, ref newBlock, ref targetBlock);
                placedBlocks[idx] = targetBlock;
            }
        }

        placedBlocks.Add(newBlock);
        GameEvents.RaiseBlockPlaced(card, originX, originY);

        Debug.Log($"[GridManager] {card.CardName} 배치 완료 ({originX}, {originY})");
        return true;
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    /// <summary> UI 미리보기용 — 현재 배치 기준 합산 결과 반환. 이벤트 발행 없음. </summary>
    public ResolutionResult GetPreview() => Calculate();

    /// <summary> 블록별 (카드, 보너스 피해, 보너스 방어) 반환 — description 실시간 표시용. </summary>
    public (CardData card, int bonusDamage, int bonusDefense)[] GetPlacedBlockPreviews()
    {
        var arr = new (CardData, int, int)[placedBlocks.Count];
        for (int i = 0; i < placedBlocks.Count; i++)
            arr[i] = (placedBlocks[i].card, placedBlocks[i].bonusDamage, placedBlocks[i].bonusDefense);
        return arr;
    }

    private void CalculateAndRaiseResolution()
    {
        StartCoroutine(BlockResolutionRoutine());
    }

    private IEnumerator BlockResolutionRoutine()
    {
        foreach (var pb in placedBlocks)
        {
            var result = new ResolutionResult();
            foreach (var effect in pb.card.Effects)
                ApplyEffect(ref result, effect);

            // BoostResolution 겹침 효과로 누적된 보너스 적용
            result.damage  += pb.bonusDamage;
            result.defense += pb.bonusDefense;

            Debug.Log($"[GridManager] {pb.card.CardName} 결산 — 공격 {result.damage}(+{pb.bonusDamage}보너스), 방어 {result.defense}(+{pb.bonusDefense}보너스), 회복 {result.heal}, 드로우 +{result.draw}");
            GameEvents.RaiseResolutionResult(result);

            yield return new WaitForSeconds(blockResolveDelay);
        }

        ClearGrid();
        GameEvents.RaiseResolutionComplete();
    }

    /// <summary>
    /// source 카드가 target 카드 위에 겹쳐졌을 때 source의 겹치기 효과를 처리한다.
    /// 즉시 발동 효과(Instant)는 GameEvents로 방출하고,
    /// 결산 보너스 효과(Boost)는 sourceBlock/targetBlock 구조체에 기록한다.
    /// </summary>
    private void ProcessOverlapEffects(
        CardData sourceCard, CardData targetCard,
        ref PlacedBlock sourceBlock, ref PlacedBlock targetBlock)
    {
        var instantResult = new ResolutionResult();
        bool hasInstant = false;

        foreach (var effect in sourceCard.OverlapEffects)
        {
            switch (effect.effectType)
            {
                // ── 즉시 발동 효과 (기존) ──
                case CardType.Attack:
                case CardType.Drain:
                    ApplyEffect(ref instantResult, effect);
                    hasInstant = true;
                    break;
                case CardType.Defense:
                case CardType.Heal:
                case CardType.DrawNow:
                    ApplyEffect(ref instantResult, effect);
                    hasInstant = true;
                    break;

                // ── 겹친 공격 카드의 결산 피해량 증폭 ──
                case CardType.OverlapBoostAttack:
                {
                    int attackCount = targetCard.Effects.Count(e => e.effectType == CardType.Attack || e.effectType == CardType.Drain);
                    if (attackCount > 0)
                    {
                        int bonus = effect.power * attackCount;
                        targetBlock.bonusDamage += bonus;
                        Debug.Log($"[GridManager] {sourceCard.CardName} → {targetCard.CardName} 결산 피해 +{bonus} (Attack {attackCount}개 × {effect.power})");
                    }
                    break;
                }

                // ── 겹친 방어 카드의 결산 방어도 증폭 ──
                case CardType.OverlapBoostDefense:
                {
                    int defenseCount = targetCard.Effects.Count(e => e.effectType == CardType.Defense);
                    if (defenseCount > 0)
                    {
                        int bonus = effect.power * defenseCount;
                        targetBlock.bonusDefense += bonus;
                        Debug.Log($"[GridManager] {sourceCard.CardName} → {targetCard.CardName} 결산 방어도 +{bonus} (Defense {defenseCount}개 × {effect.power})");
                    }
                    break;
                }

                // ── 겹친 카드의 겹치기 효과 두 번 발동 (중첩 불가) ──
                case CardType.DoubleOverlapEffect:
                    if (!targetBlock.doubleOverlapApplied)
                    {
                        targetBlock.doubleOverlapApplied = true;
                        Debug.Log($"[GridManager] {sourceCard.CardName} → {targetCard.CardName} 겹치기 효과 2회 발동");
                        // target 카드의 즉시 효과를 한 번 더 발동
                        var doubleResult = new ResolutionResult();
                        foreach (var te in targetCard.OverlapEffects)
                            ApplyEffect(ref doubleResult, te);
                        if (HasInstantEffect(doubleResult))
                            GameEvents.RaiseOverlapEffectTriggered(doubleResult);
                    }
                    break;

                // ── 겹친 카드의 결산 효과를 즉시 발동 ──
                case CardType.TriggerResolutionNow:
                {
                    var triggerResult = new ResolutionResult();
                    foreach (var te in targetCard.Effects)
                        ApplyEffect(ref triggerResult, te);
                    triggerResult.damage  += targetBlock.bonusDamage;
                    triggerResult.defense += targetBlock.bonusDefense;
                    Debug.Log($"[GridManager] {sourceCard.CardName} → {targetCard.CardName} 결산 효과 즉시 발동!");
                    GameEvents.RaiseOverlapEffectTriggered(triggerResult);
                    break;
                }

                // ── 이 카드 자신의 결산 피해 보너스 ──
                case CardType.BoostResolutionDamage:
                    sourceBlock.bonusDamage += effect.power;
                    Debug.Log($"[GridManager] {sourceCard.CardName} 자신의 결산 피해 +{effect.power}");
                    break;

                // ── 이 카드 자신의 결산 방어도 보너스 ──
                case CardType.BoostResolutionDefense:
                    sourceBlock.bonusDefense += effect.power;
                    Debug.Log($"[GridManager] {sourceCard.CardName} 자신의 결산 방어도 +{effect.power}");
                    break;
            }
        }

        if (hasInstant)
        {
            Debug.Log($"[GridManager] {sourceCard.CardName} 겹침 즉시 효과 — 공격 {instantResult.damage}, 방어 {instantResult.defense}, 회복 {instantResult.heal}");
            GameEvents.RaiseOverlapEffectTriggered(instantResult);
        }
    }

    private static bool HasInstantEffect(ResolutionResult r)
        => r.damage > 0 || r.defense > 0 || r.heal > 0 || r.drawNow > 0;

    private ResolutionResult Calculate()
    {
        var result = new ResolutionResult();
        foreach (var pb in placedBlocks)
        {
            foreach (var effect in pb.card.Effects)
                ApplyEffect(ref result, effect);
            result.damage  += pb.bonusDamage;
            result.defense += pb.bonusDefense;
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
            case CardType.DrawNow:             result.drawNow += effect.power; break;
            case CardType.Drain:               result.damage  += effect.power;
                                               result.heal    += effect.power; break;
            case CardType.OverlapBoostAttack:   result.damage  += effect.power; break;
            case CardType.OverlapBoostDefense:  result.defense += effect.power; break;
            case CardType.BoostResolutionDamage:  result.damage  += effect.power; break;
            case CardType.BoostResolutionDefense: result.defense += effect.power; break;
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
