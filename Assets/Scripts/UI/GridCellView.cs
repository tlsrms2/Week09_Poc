using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 3×3 그리드의 셀 하나를 담당하는 UI 컴포넌트.
/// 배치 가능 여부에 따라 배경색을 바꾸고, 심볼 및 겹침 수를 표시한다.
/// </summary>
public class GridCellView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI symbolLabel;
    [SerializeField] private TextMeshProUGUI overlapLabel;

    private static readonly Color colorEmpty    = new(0.15f, 0.15f, 0.15f, 0.6f);
    private static readonly Color colorOccupied = new(0.30f, 0.30f, 0.60f, 0.8f);
    private static readonly Color colorValid    = new(0.00f, 0.75f, 0.20f, 0.6f);
    private static readonly Color colorInvalid  = new(0.80f, 0.10f, 0.10f, 0.6f);

    public int GridX { get; private set; }
    public int GridY { get; private set; }

    public void Setup(int x, int y)
    {
        GridX = x;
        GridY = y;
        SetState(CellHighlight.Empty);
        if (symbolLabel != null) symbolLabel.text = "";
        if (overlapLabel != null) overlapLabel.text = "";
    }

    public void SetState(CellHighlight highlight)
    {
        background.color = highlight switch
        {
            CellHighlight.Valid    => colorValid,
            CellHighlight.Invalid  => colorInvalid,
            CellHighlight.Occupied => colorOccupied,
            _                      => colorEmpty,
        };
    }

    /// <summary> 심볼 텍스트와 겹침 배율을 갱신한다. </summary>
    public void Refresh(SymbolType symbol, int overlapCount)
    {
        if (symbol == SymbolType.None)
        {
            SetState(CellHighlight.Empty);
            if (symbolLabel != null) symbolLabel.text = "";
            if (overlapLabel != null) overlapLabel.text = "";
        }
        else
        {
            SetState(CellHighlight.Occupied);
            if (symbolLabel != null) symbolLabel.text = symbol.ToString()[..2]; // 앞 두 글자
            if (overlapLabel != null) overlapLabel.text = overlapCount > 1 ? $"×{overlapCount}" : "";
        }
    }
}

public enum CellHighlight { Empty, Occupied, Valid, Invalid }
