using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 덱 관리 시스템 — 셔플, 드로우, 핸드, 무덤(Discard) 관리.
/// 
/// <para>
/// <b>라이프사이클:</b>
/// <list type="number">
///   <item>Initialize()로 초기 덱 구성</item>
///   <item>DrawCards()로 드로우 → 핸드에 추가</item>
///   <item>DiscardCard()로 사용/버린 카드를 무덤으로</item>
///   <item>드로우 파일이 비면 무덤을 자동 셔플하여 덱에 복귀</item>
/// </list>
/// </para>
/// </summary>
public class DeckManager : MonoBehaviour
{
    [Header("초기 덱 구성")]
    [Tooltip("전투 시작 시 덱에 포함될 카드 목록 (중복 가능)")]
    [SerializeField] private List<CardData> starterDeck = new();

    [Header("드로우 설정")]
    [Tooltip("턴 시작 시 드로우할 카드 수")]
    [SerializeField] private int drawCountPerTurn = 5;
    
    // [추가] 다음 턴에 추가로 뽑을 카드 수
    private int bonusDrawForNextTurn = 0;

    // ── Runtime State ──
    private readonly List<CardData> drawPile = new();
    private readonly List<CardData> hand = new();
    private readonly List<CardData> discardPile = new();

    // ── Public Read-Only Access ──
    public IReadOnlyList<CardData> Hand => hand;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;
    public int DrawCountPerTurn => drawCountPerTurn;

    private void OnEnable()
    {
        GameEvents.OnBlockPlaced += HandleBlockPlaced;
    }

    private void OnDisable()
    {
        GameEvents.OnBlockPlaced -= HandleBlockPlaced;
    }

    private void HandleBlockPlaced(CardData card, int x, int y)
    {
        UseCard(card);
    }

    /// <summary>
    /// 덱을 초기화하고 셔플한다. 전투 시작 시 CombatManager가 호출한다.
    /// </summary>
    public void Initialize()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        drawPile.AddRange(starterDeck);
        Shuffle(drawPile);
    }

    /// <summary>
    /// 지정된 수만큼 카드를 드로우하여 핸드에 추가한다.
    /// 드로우 파일이 부족하면 무덤을 셔플하여 보충한다.
    /// </summary>
    public void DrawCards(int count = -1)
    {
        // 기본 드로우 수에 보너스 드로우를 합산 (count가 지정되지 않았을 때만)
        int drawCount = count > 0 ? count : (drawCountPerTurn + bonusDrawForNextTurn);
    
        // 합산 후 보너스 드로우 초기화
        bonusDrawForNextTurn = 0; 

        for (int i = 0; i < drawCount; i++)
        {
            // 드로우 파일이 비었으면 무덤 → 드로우 파일로 셔플
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.LogWarning("[DeckManager] 드로우 파일과 무덤 모두 비어 있음 — 드로우 중단");
                    break;
                }

                RecycleDiscardPile();
            }

            CardData card = drawPile[^1];
            drawPile.RemoveAt(drawPile.Count - 1);
            hand.Add(card);

            GameEvents.RaiseCardDrawn(card);
        }
    }

    /// <summary>
    /// 핸드에서 특정 카드를 사용(소비)하고 무덤으로 보낸다.
    /// </summary>
    public bool UseCard(CardData card)
    {
        if (!hand.Remove(card))
        {
            Debug.LogWarning($"[DeckManager] 핸드에 없는 카드를 사용하려 함: {card.CardName}");
            return false;
        }

        discardPile.Add(card);
        GameEvents.RaiseCardUsed(card);
        return true;
    }

    /// <summary>
    /// 핸드에 남은 카드를 전부 무덤으로 보낸다. 턴 종료 시 호출.
    /// </summary>
    public void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }

    // ─── Private Helpers ───

    /// <summary>
    /// 무덤의 모든 카드를 드로우 파일로 옮기고 셔플한다.
    /// </summary>
    private void RecycleDiscardPile()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
        Debug.Log("[DeckManager] 무덤을 셔플하여 드로우 파일에 보충");
    }

    /// <summary>
    /// Fisher-Yates 셔플 알고리즘.
    /// </summary>
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    
    // [추가] CombatManager가 결산 때 호출할 메서드
    public void AddBonusDraw(int count)
    {
        bonusDrawForNextTurn += count;
    }
}
