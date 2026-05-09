using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3×3 그리드의 시각 표현을 담당한다.
/// GridManager의 데이터를 읽어 셀을 갱신하고, 드래그 미리보기(초록/빨강)를 제공한다.
///
/// <para>
/// <b>씬 설정:</b> 이 컴포넌트가 붙은 GameObject에 GridLayoutGroup(3열)을 추가하고,
/// cellPrefab을 Inspector에 연결한다.
/// </para>
/// </summary>
public class GridView : MonoBehaviour
{
    [SerializeField] private GridCellView cellPrefab;
    [SerializeField] private GridManager gridManager;

    private GridCellView[,] cells;

    /// <summary> BlockGhostView 타일 크기 계산에 사용. GridLayoutGroup의 cellSize.x를 반환한다. </summary>
    public float CellSize => GetComponent<GridLayoutGroup>().cellSize.x;

    // ═══════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════

    private void Awake()
    {
        cells = new GridCellView[GridManager.GridSize, GridManager.GridSize];
        BuildCells();
    }

    private void OnEnable()
    {
        GameEvents.OnBlockPlaced    += HandleBlockPlaced;
        GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted;
        GameEvents.OnResolutionResult += HandleResolutionResult;
    }

    private void OnDisable()
    {
        GameEvents.OnBlockPlaced    -= HandleBlockPlaced;
        GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
        GameEvents.OnResolutionResult -= HandleResolutionResult;
    }

    // ═══════════════════════════════════════════
    //  Public API (Dev B 내부 / CardView에서 호출)
    // ═══════════════════════════════════════════

    /// <summary>
    /// 화면 좌표를 그리드 인덱스(x, y)로 변환한다. 그리드 밖이면 (-1, -1).
    /// </summary>
    public (int x, int y) ScreenToGridCoords(Vector2 screenPos)
    {
        for (int y = 0; y < GridManager.GridSize; y++)
        for (int x = 0; x < GridManager.GridSize; x++)
        {
            var rt = cells[x, y].GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                return (x, y);
        }
        return (-1, -1);
    }

    /// <summary>
    /// 드래그 중 배치 미리보기를 표시한다. 배치 가능하면 초록, 불가면 빨강.
    /// </summary>
    public void ShowPreview(CardData card, int originX, int originY)
    {
        RefreshGrid(); // 기존 상태 먼저 복원

        if (card == null) return;

        bool canPlace = gridManager.CanPlaceBlock(card, originX, originY);

        foreach (var (col, row, symbol) in card.GetOccupiedCells())
        {
            int gx = originX + col;
            int gy = originY + row;

            if (gx < 0 || gx >= GridManager.GridSize || gy < 0 || gy >= GridManager.GridSize)
                continue;

            SymbolType currentSymbol = gridManager.GetCell(gx, gy);
            int currentOverlapCount = currentSymbol == symbol ? gridManager.GetOverlapCount(gx, gy) : 0;
            cells[gx, gy].SetPreview(symbol, canPlace, currentOverlapCount);
        }
    }

    /// <summary> 미리보기를 지우고 현재 그리드 상태로 복원한다. </summary>
    public void ClearPreview() => RefreshGrid();

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void BuildCells()
    {
        // GridLayoutGroup이 열 우선(행 순서)으로 채우기 때문에
        // y=0을 상단, y=2를 하단으로 삽입한다.
        for (int y = 0; y < GridManager.GridSize; y++)
        for (int x = 0; x < GridManager.GridSize; x++)
        {
            var cell = Instantiate(cellPrefab, transform);
            cell.Setup(x, y);
            cells[x, y] = cell;
        }
    }

    private void RefreshGrid()
    {
        for (int y = 0; y < GridManager.GridSize; y++)
        for (int x = 0; x < GridManager.GridSize; x++)
        {
            cells[x, y].Refresh(
                gridManager.GetCell(x, y),
                gridManager.GetOverlapCount(x, y)
            );
        }
    }

    private void HandleBlockPlaced(CardData _, int x, int y) => RefreshGrid();
    private void HandleDrawPhaseStarted(int _) => RefreshGrid();
    private void HandleResolutionResult(ResolutionResult _) => RefreshGrid();
}
