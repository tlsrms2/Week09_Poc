using UnityEngine;

/// <summary>
/// 패(Hand)의 시각적 UI를 관리하는 클래스.
/// 카드가 드로우될 때 이벤트를 받아 CardView 프리팹을 생성하고 화면에 표시한다.
/// </summary>
public class HandView : MonoBehaviour
{
    [Header("프리팹 및 참조")]
    [Tooltip("생성할 범용 카드 프리팹")]
    [SerializeField] private CardView cardPrefab;
    
    [Tooltip("카드가 생성될 부모 Transform (주로 HorizontalLayoutGroup이 있는 패널)")]
    [SerializeField] private Transform handContainer;

    private void Awake()
    {
        if (handContainer == null)
            handContainer = transform; // 지정하지 않았다면 자기 자신을 컨테이너로 사용
    }

    private void OnEnable()
    {
        GameEvents.OnCardDrawn += HandleCardDrawn;
        GameEvents.OnResolutionPhaseStarted += ClearHandUI; // 결산 시 남은 패를 화면에서 지움
        GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted; // 턴 시작 시 혹시 남아있는 UI 정리
    }

    private void OnDisable()
    {
        GameEvents.OnCardDrawn -= HandleCardDrawn;
        GameEvents.OnResolutionPhaseStarted -= ClearHandUI;
        GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
    }

    /// <summary>
    /// 카드가 뽑혔을 때 호출되어 카드 프리팹을 생성하고 데이터를 주입한다.
    /// </summary>
    private void HandleCardDrawn(CardData cardData)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[HandView] CardPrefab이 할당되지 않았습니다!");
            return;
        }

        CardView newCard = Instantiate(cardPrefab, handContainer);
        // Setup을 호출해 SO 데이터를 UI에 밀어넣음
        newCard.Setup(cardData);
    }

    private void HandleDrawPhaseStarted(int _)
    {
        ClearHandUI();
    }

    /// <summary>
    /// 턴 종료 시 혹은 화면 정리가 필요할 때 패널 하위의 모든 카드를 삭제한다.
    /// </summary>
    private void ClearHandUI()
    {
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
