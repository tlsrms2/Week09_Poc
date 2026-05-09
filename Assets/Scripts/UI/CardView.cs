using TMPro;
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
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private Image cardTypeIndicator;

    private static readonly Color attackColor  = new(0.85f, 0.25f, 0.25f, 1f);
    private static readonly Color defenseColor = new(0.25f, 0.45f, 0.85f, 1f);

    // ── Runtime ──
    private CardData cardData;
    private GridView gridView;
    private GridManager gridManager;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    private BlockGhostView ghost;

    // ═══════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════

    private void Awake()
    {
        rootCanvas  = GetComponentInParent<Canvas>().rootCanvas;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(CardData data, GridView grid, GridManager manager)
    {
        cardData    = data;
        gridView    = grid;
        gridManager = manager;

        cardNameText.text = data.CardName;
        powerText.text    = data.BasePower.ToString();

        if (cardTypeIndicator != null)
            cardTypeIndicator.color = data.Type == CardType.Attack ? attackColor : defenseColor;
    }

    // ═══════════════════════════════════════════
    //  Drag Handlers
    // ═══════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
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
            bool canPlace = gridManager.CanPlaceBlock(cardData, gx, gy);
            ghost.SetValidity(canPlace);
            gridView.ShowPreview(cardData, gx, gy);
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
        bool placed  = gx >= 0 && gridManager.TryPlaceBlock(cardData, gx, gy);

        if (placed)
        {
            Destroy(gameObject);
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
        if (cardData.GetOccupiedCells().Length == 0) return;

        var go = new GameObject("BlockGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(BlockGhostView));
        go.transform.SetParent(rootCanvas.transform, false);

        ghost = go.GetComponent<BlockGhostView>();
        ghost.Setup(cardData, gridView.CellSize);
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
}
