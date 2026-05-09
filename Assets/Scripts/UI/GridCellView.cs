using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One visual cell in the battle grid.
/// The fill color represents the symbol, while outlines represent preview validity.
/// </summary>
public class GridCellView : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI symbolLabel;
    [SerializeField] private TextMeshProUGUI overlapLabel;
    [SerializeField, Min(0f)] private float previewAlpha = 0.62f;
    [SerializeField, Min(0f)] private float occupiedAlpha = 0.85f;
    [SerializeField, Min(1f)] private float outlineThickness = 4f;

    private static readonly Color colorEmpty = new(0.15f, 0.15f, 0.15f, 0.6f);
    private static readonly Color colorOccupied = new(0.30f, 0.30f, 0.60f, 0.8f);
    private static readonly Color colorValid = new(0.10f, 0.95f, 0.35f, 1f);
    private static readonly Color colorInvalid = new(1.00f, 0.20f, 0.15f, 1f);
    private static readonly Color colorOverlap = new(1.00f, 0.80f, 0.10f, 1f);

    private readonly Image[] outlineEdges = new Image[4];

    public int GridX { get; private set; }
    public int GridY { get; private set; }

    private void Awake()
    {
        EnsureReferences();
    }

    public void Setup(int x, int y)
    {
        EnsureReferences();
        GridX = x;
        GridY = y;
        SetState(CellHighlight.Empty);

        if (symbolLabel != null) symbolLabel.text = "";
        if (overlapLabel != null) overlapLabel.text = "";
    }

    public void SetState(CellHighlight highlight)
    {
        EnsureReferences();
        ClearOutline();

        background.color = highlight switch
        {
            CellHighlight.Valid => colorEmpty,
            CellHighlight.Invalid => colorEmpty,
            CellHighlight.Occupied => colorOccupied,
            _ => colorEmpty,
        };

        if (highlight == CellHighlight.Valid)
            ApplyOutline(colorValid);
        else if (highlight == CellHighlight.Invalid)
            ApplyOutline(colorInvalid);
    }

    public void SetPreview(SymbolType previewSymbol, bool canPlace, int currentOverlapCount)
    {
        EnsureReferences();

        Color previewColor = SymbolVisuals.GetColor(previewSymbol);
        previewColor.a = previewAlpha;
        background.color = previewColor;

        if (symbolLabel != null)
        {
            symbolLabel.text = previewSymbol == SymbolType.None ? "" : previewSymbol.ToString()[..2];
            symbolLabel.color = SymbolVisuals.GetColor(previewSymbol);
        }

        bool willOverlap = canPlace && currentOverlapCount > 0;
        if (overlapLabel != null)
        {
            overlapLabel.text = willOverlap ? $"x{currentOverlapCount + 1}" : "";
            overlapLabel.color = willOverlap ? colorOverlap : Color.white;
        }

        ApplyOutline(canPlace ? (willOverlap ? colorOverlap : colorValid) : colorInvalid);
    }

    public void Refresh(SymbolType symbol, int overlapCount)
    {
        EnsureReferences();

        if (symbol == SymbolType.None)
        {
            SetState(CellHighlight.Empty);
            if (symbolLabel != null) symbolLabel.text = "";
            if (overlapLabel != null) overlapLabel.text = "";
            return;
        }

        Color symbolColor = SymbolVisuals.GetColor(symbol);
        background.color = new Color(
            symbolColor.r,
            symbolColor.g,
            symbolColor.b,
            overlapCount > 1 ? 0.95f : occupiedAlpha
        );

        if (overlapCount > 1)
            ApplyOutline(colorOverlap);
        else
            ClearOutline();

        if (symbolLabel != null)
        {
            symbolLabel.text = symbol.ToString()[..2];
            symbolLabel.color = Color.white;
        }

        if (overlapLabel != null)
        {
            overlapLabel.text = overlapCount > 1 ? $"x{overlapCount}" : "";
            overlapLabel.color = overlapCount > 1 ? colorOverlap : Color.white;
        }
    }

    private void EnsureReferences()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (background == null)
            return;

        var legacyOutline = background.GetComponent<Outline>();
        if (legacyOutline != null)
            legacyOutline.enabled = false;

        EnsureOutlineEdges();
    }

    private void ApplyOutline(Color color)
    {
        EnsureOutlineEdges();

        foreach (Image edge in outlineEdges)
        {
            if (edge == null)
                continue;

            edge.color = color;
            edge.enabled = true;
        }
    }

    private void ClearOutline()
    {
        foreach (Image edge in outlineEdges)
        {
            if (edge != null)
                edge.enabled = false;
        }
    }

    private void EnsureOutlineEdges()
    {
        if (background == null || outlineEdges[0] != null)
            return;

        outlineEdges[0] = CreateOutlineEdge("Outline_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, outlineThickness));
        outlineEdges[1] = CreateOutlineEdge("Outline_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, outlineThickness));
        outlineEdges[2] = CreateOutlineEdge("Outline_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(outlineThickness, 0f));
        outlineEdges[3] = CreateOutlineEdge("Outline_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(outlineThickness, 0f));
    }

    private Image CreateOutlineEdge(string edgeName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        var edgeObject = new GameObject(edgeName, typeof(RectTransform), typeof(Image));
        edgeObject.transform.SetParent(background.transform, false);
        edgeObject.transform.SetAsLastSibling();

        var rect = edgeObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        var image = edgeObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }
}

public enum CellHighlight { Empty, Occupied, Valid, Invalid }
