using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중 블록 모양을 마우스 위치에 시각화하는 고스트(Ghost) 컴포넌트.
/// CardView가 드래그 시작 시 동적으로 생성하고 종료 시 파괴한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BlockGhostView : MonoBehaviour
{
    private static readonly Color validColor   = new(0.10f, 0.90f, 0.25f, 0.65f);
    private static readonly Color invalidColor = new(0.90f, 0.15f, 0.15f, 0.65f);

    private Image[] tiles;

    // ═══════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════

    /// <summary>
    /// 블록 형태에 맞게 타일을 생성한다.
    /// </summary>
    /// <param name="card">카드 데이터 (블록 형태 포함)</param>
    /// <param name="cellSize">그리드 셀 한 칸의 픽셀 크기</param>
    public void Setup(CardData card, float cellSize)
    {
        // 레이캐스트를 통과시켜 그리드 셀 감지가 막히지 않도록
        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;

        var occupied = card.GetOccupiedCells();
        tiles = new Image[occupied.Length];

        float tileSize = cellSize - 6f; // 셀보다 약간 작게 (여백 표현)

        for (int i = 0; i < occupied.Length; i++)
        {
            var (col, row, _) = occupied[i];

            var tileGo = new GameObject($"Tile_{col}_{row}", typeof(RectTransform), typeof(Image));
            tileGo.transform.SetParent(transform, false);

            var rt = tileGo.GetComponent<RectTransform>();
            rt.sizeDelta        = Vector2.one * tileSize;
            rt.anchoredPosition = new Vector2(col * cellSize, -row * cellSize);

            tiles[i] = tileGo.GetComponent<Image>();
        }

        SetValidity(true);
    }

    // ═══════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════

    /// <summary> 고스트를 해당 화면 좌표로 이동한다. </summary>
    public void UpdatePosition(Vector2 screenPos)
    {
        transform.position = screenPos;
    }

    /// <summary> 배치 가능 여부에 따라 타일 색을 바꾼다. </summary>
    public void SetValidity(bool canPlace)
    {
        Color color = canPlace ? validColor : invalidColor;
        foreach (var tile in tiles)
            tile.color = color;
    }
}
