using TMPro;
using Cyg.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 패(Hand)에 있는 카드 한 장의 UI 및 드래그 인터랙션을 담당한다.
///
/// <para>
/// <b>드래그 방식:</b> 카드 자체는 패에 반투명으로 유지되고,
/// 블록 모양의 고스트(BlockGhostView)가 마우스를 따라다닌다.
/// </para>
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CardView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Inspector 직접 설정 (테스트용)")]
    [SerializeField] private CardData initialCardData;

    private GridView gridView;
    private GridManager gridManager;

    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private Image cardTypeIndicator;
    [SerializeField] private RectTransform blockPreviewRoot;
    [SerializeField, Min(1f)] private float blockPreviewTileSize = 18f;
    [SerializeField, Min(0f)] private float blockPreviewTileGap = 2f;
    [SerializeField, Range(0f, 1f)] private float blockPreviewAlpha = 1f;

    private static readonly Color attackColor  = new(0.85f, 0.25f, 0.25f, 1f);
    private static readonly Color defenseColor = new(0.25f, 0.45f, 0.85f, 1f);

    // ── Runtime ──
    [Header("Runtime Debug")]
    [SerializeField] private CardData currentCardData;
    public CardData CurrentCardData => currentCardData;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private CygHandHoverAnimator handHoverAnimator;

    private BlockGhostView ghost;

    // ═══════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════

    private void Awake()
    {
        rootCanvas  = GetComponentInParent<Canvas>().rootCanvas;
        canvasGroup = GetComponent<CanvasGroup>();
        handHoverAnimator = GetComponentInParent<CygHandHoverAnimator>();

        // 프리팹 인스턴스화를 위해 런타임에 동적으로 매니저와 뷰를 탐색
        gridView = FindAnyObjectByType<GridView>();
        gridManager = FindAnyObjectByType<GridManager>();
    }

    private void Start()
    {
        // Inspector에 SO가 연결된 경우 자동 초기화
        if (initialCardData != null)
            Setup(initialCardData);
    }

    public void Setup(CardData data)
    {
        currentCardData = data;

        if (data == null)
        {
            if (cardNameText != null) cardNameText.text = string.Empty;
            if (descriptionText != null) descriptionText.text = string.Empty;
            if (powerText != null) powerText.text = string.Empty;
            ClearBlockPreview();
            return;
        }

        if (cardNameText != null) cardNameText.text = data.CardName;
        if (descriptionText != null) descriptionText.text = data.FormattedDescription;
        if (powerText != null)    powerText.text    = data.BasePower.ToString();

        if (cardTypeIndicator != null)
            cardTypeIndicator.color = data.Type == CardType.Attack ? attackColor : defenseColor;

        BuildBlockPreview(data);
    }

    // ═══════════════════════════════════════════
    //  Drag Handlers
    // ═══════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        handHoverAnimator?.ClearHover();

        // 카드는 패에 그대로, 반투명 처리
        canvasGroup.alpha          = 0.35f;
        canvasGroup.blocksRaycasts = false;

        SpawnGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost == null) return;

        ghost.UpdatePosition(eventData.position);

        var (gx, gy) = gridView.ScreenToGridCoords(eventData.position);

        if (gx >= 0)
        {
            bool canPlace = gridManager.CanPlaceBlock(currentCardData, gx, gy);
            ghost.SetValidity(canPlace);
            gridView.ShowPreview(currentCardData, gx, gy);
        }
        else
        {
            ghost.SetValidity(false);
            gridView.ClearPreview();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyGhost();
        gridView.ClearPreview();

        var (gx, gy) = gridView.ScreenToGridCoords(eventData.position);
        bool placed  = gx >= 0 && gridManager.TryPlaceBlock(currentCardData, gx, gy);

        if (placed)
        {
            canvasGroup.alpha          = 1f;
            canvasGroup.blocksRaycasts = true;

            if (handHoverAnimator != null)
                handHoverAnimator.MarkCardInactive(transform);
            else
                gameObject.SetActive(false);
        }
        else
        {
            // 배치 실패 → 카드 복원
            canvasGroup.alpha          = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    // ═══════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════

    private void SpawnGhost(Vector2 startPos)
    {
        if (currentCardData == null || currentCardData.GetOccupiedCells().Length == 0) return;

        var go = new GameObject("BlockGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(BlockGhostView));
        go.transform.SetParent(rootCanvas.transform, false);

        ghost = go.GetComponent<BlockGhostView>();
        ghost.Setup(currentCardData, gridView.CellSize);
        ghost.UpdatePosition(startPos);
    }

    private void DestroyGhost()
    {
        if (ghost != null)
        {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }

    private void BuildBlockPreview(CardData data)
    {
        EnsureBlockPreviewRoot();
        ClearBlockPreview();

        if (blockPreviewRoot == null)
            return;

        var occupiedCells = data.GetOccupiedCells();
        if (occupiedCells.Length == 0)
            return;

        float width = Mathf.Max(1, data.Width);
        float height = Mathf.Max(1, data.Height);
        Vector2 rootSize = blockPreviewRoot.rect.size;
        if (rootSize.x <= 0f || rootSize.y <= 0f)
            rootSize = blockPreviewRoot.sizeDelta;
        if (rootSize.x <= 0f || rootSize.y <= 0f)
            rootSize = new Vector2(100f, 100f);

        float maxTileWidth = (rootSize.x - blockPreviewTileGap * (width - 1f)) / width;
        float maxTileHeight = (rootSize.y - blockPreviewTileGap * (height - 1f)) / height;
        float tileSize = Mathf.Max(1f, Mathf.Min(blockPreviewTileSize, maxTileWidth, maxTileHeight));
        float totalWidth = width * tileSize + (width - 1f) * blockPreviewTileGap;
        float totalHeight = height * tileSize + (height - 1f) * blockPreviewTileGap;

        foreach (var (col, row, symbol) in occupiedCells)
        {
            var tile = new GameObject($"PreviewTile_{col}_{row}", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(blockPreviewRoot, false);

            var rect = tile.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(tileSize, tileSize);

            float x = -totalWidth * 0.5f + tileSize * 0.5f + col * (tileSize + blockPreviewTileGap);
            float y = totalHeight * 0.5f - tileSize * 0.5f - row * (tileSize + blockPreviewTileGap);
            rect.anchoredPosition = new Vector2(x, y);

            var image = tile.GetComponent<Image>();
            Color color = SymbolVisuals.GetColor(symbol);
            color.a = blockPreviewAlpha;
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private void ClearBlockPreview()
    {
        if (blockPreviewRoot == null)
            return;

        for (int i = blockPreviewRoot.childCount - 1; i >= 0; i--)
            Destroy(blockPreviewRoot.GetChild(i).gameObject);
    }

    private void EnsureBlockPreviewRoot()
    {
        if (blockPreviewRoot != null)
            return;

        var children = GetComponentsInChildren<RectTransform>(true);
        foreach (var child in children)
        {
            if (child.name == "BlockPreviewRoot")
            {
                blockPreviewRoot = child;
                return;
            }
        }
    }
}
