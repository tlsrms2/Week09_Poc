using UnityEngine;

/// <summary>
/// Visual hand UI. Spawns a card prefab whenever DeckManager raises OnCardDrawn.
/// </summary>
public class HandView : MonoBehaviour
{
    [Header("Prefab and References")]
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform handContainer;

    private void Awake()
    {
        if (handContainer == null)
            handContainer = transform;
    }

    private void OnEnable()
    {
        GameEvents.OnCardDrawn += HandleCardDrawn;
        GameEvents.OnDrawPhaseStarted += HandleDrawPhaseStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnCardDrawn -= HandleCardDrawn;
        GameEvents.OnDrawPhaseStarted -= HandleDrawPhaseStarted;
    }

    private void HandleCardDrawn(CardData cardData)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[HandView] CardPrefab is not assigned.");
            return;
        }

        CardView newCard = Instantiate(cardPrefab, handContainer);
        newCard.Setup(cardData);
    }

    private void HandleDrawPhaseStarted(int _)
    {
        ClearHandUI();
    }

    private void ClearHandUI()
    {
        foreach (Transform child in handContainer)
            Destroy(child.gameObject);
    }
}
